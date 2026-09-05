using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class UberShaderVariantCollectionGenerator
{
    [MenuItem("Tools/Uber Shader/Verify Variant Collection")]
    private static void VerifyMenu()
    {
        Verify();
        Debug.Log("Uber Shader variant collection matches the reviewed manifest.");
    }

    [MenuItem("Tools/Uber Shader/Rebuild Variant Collection")]
    private static void RebuildMenu() => Debug.Log(Rebuild()
        ? "Uber Shader variant collection rebuilt."
        : "Uber Shader variant collection already matches the reviewed manifest.");

    internal static void Verify()
    {
        ShaderVariantCollection candidate = CreateValidatedCandidate();
        try
        {
            ValidateCollection(LoadLive(), UberShaderVariantManifest.Rows, "Live");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(candidate);
        }
    }

    internal static bool Rebuild()
    {
        ShaderVariantCollection candidate = CreateValidatedCandidate();
        try
        {
            ShaderVariantCollection live = LoadLive();
            if (IsExact(live, UberShaderVariantManifest.Rows))
                return false;
            ShaderVariantCollection backup = new ShaderVariantCollection();
            EditorUtility.CopySerialized(live, backup);
            try
            {
                EditorUtility.CopySerialized(candidate, live);
                ValidateCollection(live, UberShaderVariantManifest.Rows, "Copied live");
                EditorUtility.SetDirty(live);
                AssetDatabase.SaveAssetIfDirty(live);
                ValidateCollection(live, UberShaderVariantManifest.Rows, "Saved live");
                return true;
            }
            catch
            {
                EditorUtility.CopySerialized(backup, live);
                EditorUtility.SetDirty(live);
                AssetDatabase.SaveAssetIfDirty(live);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(backup);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(candidate);
        }
    }

    internal static ShaderVariantCollection CreateValidatedCandidate() => CreateValidatedCandidate(UberShaderVariantManifest.Rows);

    internal static ShaderVariantCollection CreateValidatedCandidate(
        IReadOnlyList<UberShaderVariantSpec> rows)
    {
        UberShaderVariantManifest.ValidateRows(rows);
        var candidate = new ShaderVariantCollection { name = UberShaderVariantManifest.CollectionName };
        try
        {
            for (int index = 0; index < rows.Count; ++index)
                if (!candidate.Add(rows[index].ToVariant()))
                    throw new InvalidOperationException("Could not add manifest row " + index + ".");
            ValidateCollection(candidate, rows, "Transient candidate");
            return candidate;
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(candidate);
            throw;
        }
    }

    internal static void ValidateCollection(ShaderVariantCollection collection,
        IReadOnlyList<UberShaderVariantSpec> rows, string context)
    {
        Require(collection != null, context + " collection is missing.");
        UberShaderVariantManifest.ValidateRows(rows);
        Require(collection.name == UberShaderVariantManifest.CollectionName,
            context + " name differs.");
        Require(collection.shaderCount == rows.Select(row => row.ShaderName).Distinct().Count() && collection.variantCount == rows.Count,
            context + " has an unexpected shader count or " + rows.Count +
            " variants.");
        for (int index = 0; index < rows.Count; ++index)
            Require(collection.Contains(rows[index].ToVariant()),
                context + " is missing manifest row " + index + ".");
        var serialized = new SerializedObject(collection);
        serialized.UpdateIfRequiredOrScript();
        SerializedProperty groups = serialized.FindProperty("m_Shaders");
        Require(groups != null && groups.arraySize == collection.shaderCount,
            context + " serialized shader groups differ.");
        int rowIndex = 0;
        for (int groupIndex = 0; groupIndex < groups.arraySize; ++groupIndex)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(groupIndex);
            Shader shader = group.FindPropertyRelative("first").objectReferenceValue as Shader;
            Require(shader != null && shader.name == rows[rowIndex].ShaderName,
                context + " shader group order differs at " + groupIndex + ".");
            SerializedProperty variants = group.FindPropertyRelative("second")
                .FindPropertyRelative("variants");
            int firstRow = rowIndex;
            while (rowIndex < rows.Count && rows[rowIndex].ShaderName == shader.name)
                ++rowIndex;
            Require(variants.arraySize == rowIndex - firstRow,
                context + " row count differs for " + shader.name + ".");
            for (int local = 0; local < variants.arraySize; ++local)
            {
                UberShaderVariantSpec expected = rows[firstRow + local];
                SerializedProperty variant = variants.GetArrayElementAtIndex(local);
                Require(variant.FindPropertyRelative("keywords").stringValue ==
                        string.Join(" ", expected.Keywords) &&
                    variant.FindPropertyRelative("passType").intValue ==
                        (int)expected.PassType,
                    context + " serialized row order differs at " + (firstRow + local) + ".");
            }
        }
        Require(rowIndex == rows.Count, context + " serialized rows are incomplete.");
    }

    private static bool IsExact(ShaderVariantCollection collection,
        IReadOnlyList<UberShaderVariantSpec> rows)
    {
        try { ValidateCollection(collection, rows, "Live"); return true; }
        catch (InvalidOperationException) { return false; }
    }

    private static ShaderVariantCollection LoadLive()
    {
        Require(AssetDatabase.AssetPathToGUID(
                UberShaderVariantManifest.CollectionPath) ==
            UberShaderVariantManifest.CollectionGuid,
            "Uber Shader variant collection GUID changed.");
        ShaderVariantCollection live = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(
            UberShaderVariantManifest.CollectionPath);
        Require(live != null, "Live Uber Shader variant collection is missing.");
        return live;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
