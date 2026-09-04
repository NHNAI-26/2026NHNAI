using Border.Core;
using Simulation;
using UnityEditor;
using UnityEngine;

namespace Simulation.Editor
{
    /// <summary>
    /// 테스트 전용 툴. 엔진 프리셋 열 개와 라이브러리를 만들어 테스트 씬을 바로 발사 가능한 상태로 만든다.
    /// 여기 값은 확정 밸런스가 아니라 실험용이다 — 기획 근거는 <c>docs/specs/engine-preset-stats-spec.md</c>.
    /// </summary>
    internal static class EnginePresetTestFiller
    {
        private const string FolderRoot = "Assets/02. ScriptableObjects";
        private const string FolderName = "Simulation";
        private const string Folder = FolderRoot + "/" + FolderName;
        private const string LibraryPath = Folder + "/EnginePresetLibrary.asset";

        // 가격, 연료(kg), 냉각(°C/s), 최대 출력(N), 점화 신뢰도(%).
        // 발열은 출력 × 0.05 °C/s 이므로 냉각보다 크면 순증이 생기고 300 °C 에서 과열로 끝난다.
        private static readonly (string Name, int Price, float Fuel, float Cooling, float Output, float Ignition)[] Presets =
        {
            ("Baseline",     350, 100f,  60f, 1200f, 100f), // 발열 60 = 냉각. 기존 프로토타입과 동등한 기준값
            ("HotRod",       620,  90f,  55f, 1800f,  92f), // 순증 +35 °C/s, 약 8.6초 뒤 과열
            ("BigTank",      480, 220f,  62f, 1150f,  96f), // 연소는 길지만 무겁다
            ("Light",        260,  55f,  45f,  850f,  90f),
            ("Monster",      900, 120f,  80f, 2400f,  80f), // 순증 +40 °C/s, 약 7.5초
            ("Steady",       540, 130f,  90f, 1400f,  99f),
            ("Cheap",        180,  70f,  38f,  900f,  65f), // 순증 +7 °C/s, 약 43초. 점화도 자주 실패한다
            ("Balanced",     430, 110f,  68f, 1300f,  94f),
            ("IceCooled",    700,  95f, 130f, 1600f,  97f), // 과열이 사실상 없다
            ("MisfireProne", 320,  85f,  52f, 1050f,  40f), // 열은 버티지만 절반 넘게 점화에 실패한다
        };

        [MenuItem("Tools/Engine Preset/Fill Test Presets")]
        private static void FillTestPresets()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(FolderRoot, FolderName);

            EnginePresetLibrarySO library = LoadOrCreate<EnginePresetLibrarySO>(LibraryPath);
            SerializedObject librarySo = new(library);
            SerializedProperty slots = librarySo.FindProperty("slots");
            slots.arraySize = Presets.Length;

            for (int i = 0; i < Presets.Length; i++)
                slots.GetArrayElementAtIndex(i).objectReferenceValue = WritePreset(Presets[i]);

            librarySo.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log.D($"Filled {Presets.Length} test engine presets into {Folder}");
        }

        /// <summary>
        /// 씬에 이미 놓인 엔진 중 프리셋이 비어 있는 것에 기준 프리셋을 꽂는다. 씬을 건드리므로
        /// 프리셋 생성과 분리된 별도 메뉴다.
        /// </summary>
        [MenuItem("Tools/Engine Preset/Assign Baseline To Scene Engines")]
        private static void AssignBaselineToSceneEngines()
        {
            string baselinePath = $"{Folder}/EngineStats_{Presets[0].Name}.asset";
            EngineStatsSO baseline = AssetDatabase.LoadAssetAtPath<EngineStatsSO>(baselinePath);
            if (baseline == null)
            {
                Log.W($"{baselinePath} not found — run Tools/Engine Preset/Fill Test Presets first");
                return;
            }

            RocketPart[] parts = Object.FindObjectsByType<RocketPart>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int assigned = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].HasStats) continue;

                SerializedObject partSo = new(parts[i]);
                partSo.FindProperty("stats").objectReferenceValue = baseline;
                partSo.ApplyModifiedProperties(); // Undo 에 기록되고 씬이 dirty 로 표시된다
                assigned++;
            }

            Log.D($"Assigned baseline preset to {assigned}/{parts.Length} scene engine(s)");
        }

        private static EngineStatsSO WritePreset(
            (string Name, int Price, float Fuel, float Cooling, float Output, float Ignition) preset)
        {
            EngineStatsSO asset = LoadOrCreate<EngineStatsSO>($"{Folder}/EngineStats_{preset.Name}.asset");

            SerializedObject so = new(asset);
            so.FindProperty("price").intValue = preset.Price;
            so.FindProperty("fuelCapacity").floatValue = preset.Fuel;
            so.FindProperty("cooling").floatValue = preset.Cooling;
            so.FindProperty("maxOutput").floatValue = preset.Output;
            so.FindProperty("ignitionReliability").floatValue = preset.Ignition;
            so.ApplyModifiedPropertiesWithoutUndo();

            return asset;
        }

        // 이미 있으면 덮어쓰지 않고 재사용한다 — 에셋을 지웠다 만들면 참조와 GUID 가 끊긴다.
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
