using System;
using System.Collections.Generic;
using Border.Audio;
using UnityEditor;
using UnityEngine;

namespace Border.Audio.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(SoundDatabaseSO))]
    public sealed class SoundDatabaseSOEditor : UnityEditor.Editor
    {
        private const string BgmPropertyName = "bgmEntries";
        private const string SfxPropertyName = "sfxEntries";
        private const string PreviewObjectName = "Sound Database Audio Preview";

        private static GameObject previewObject;
        private static AudioSource previewSource;

        private SerializedProperty bgmEntries;
        private SerializedProperty sfxEntries;

        static SoundDatabaseSOEditor()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnEnable()
        {
            bgmEntries = serializedObject.FindProperty(BgmPropertyName);
            sfxEntries = serializedObject.FindProperty(SfxPropertyName);
        }

        private void OnDisable() => StopPreview();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawList("BGM", bgmEntries, false);
            EditorGUILayout.Space(8f);
            DrawList("SFX", sfxEntries, true);
            serializedObject.ApplyModifiedProperties();
        }

        public static bool SetIdFromClipName(SoundDatabaseSO database, bool isSfx, int index)
        {
            if (database == null)
            {
                return false;
            }

            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty(isSfx ? SfxPropertyName : BgmPropertyName);
            if (entries == null || index < 0 || index >= entries.arraySize)
            {
                return false;
            }

            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            SerializedProperty clip = element.FindPropertyRelative("clip");
            SerializedProperty id = element.FindPropertyRelative("id");
            if (clip?.objectReferenceValue is not AudioClip audioClip || id == null)
            {
                return false;
            }

            Undo.RecordObject(database, "Set Sound ID From Clip Name");
            id.stringValue = audioClip.name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.RebuildLookup();
            EditorUtility.SetDirty(database);
            return true;
        }

        public static string FormatRowLabel(string category, int index, string id)
        {
            return $"{category} {index} - {id}";
        }

        public static bool DuplicateEntry(SoundDatabaseSO database, bool isSfx, int index)
        {
            if (database == null) return false;

            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty(isSfx ? SfxPropertyName : BgmPropertyName);
            if (entries == null || index < 0 || index >= entries.arraySize) return false;

            Undo.RecordObject(database, "Duplicate Sound Entry");
            entries.InsertArrayElementAtIndex(index);
            entries.GetArrayElementAtIndex(index + 1).isExpanded = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.RebuildLookup();
            EditorUtility.SetDirty(database);
            return true;
        }

        public static AudioSource ActivePreviewSource => previewSource;

        public static bool PreviewClip(AudioClip clip, float volume, float pitch, bool loop)
        {
            StopPreview();
            if (clip == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            previewObject = EditorUtility.CreateGameObjectWithHideFlags(
                PreviewObjectName, HideFlags.HideAndDontSave, typeof(AudioSource));
            previewSource = previewObject.GetComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.clip = clip;
            previewSource.volume = Mathf.Clamp01(volume);
            previewSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
            previewSource.loop = loop;
            previewSource.spatialBlend = 0f;
            previewSource.spatialize = false;
            previewSource.Play();
            return true;
        }

        public static void StopPreview()
        {
            if (previewSource != null)
            {
                previewSource.Stop();
            }

            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }

            previewSource = null;
            previewObject = null;
        }

        private void DrawList(string title, SerializedProperty entries, bool isSfx)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (int index = 0; index < entries.arraySize; index++)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (DrawHeader(entries, index, title, isSfx))
                    {
                        break;
                    }

                    SerializedProperty element = entries.GetArrayElementAtIndex(index);
                    if (!element.isExpanded)
                    {
                        continue;
                    }

                    if (IsNullEntry(index, isSfx))
                    {
                        EditorGUILayout.HelpBox("Entry is null and will be skipped.", MessageType.Warning);
                        continue;
                    }

                    SerializedProperty id = element.FindPropertyRelative("id");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(id);
                        if (GUILayout.Button("ID = Clip Name", GUILayout.Width(120f)))
                        {
                            serializedObject.ApplyModifiedProperties();
                            SetIdFromClipName((SoundDatabaseSO)target, isSfx, index);
                            serializedObject.Update();
                            GUIUtility.ExitGUI();
                        }
                    }

                    DrawEntryFields(element, isSfx);
                    string warning = GetWarning(entries, index, isSfx);
                    if (!string.IsNullOrEmpty(warning))
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }
            }

            if (GUILayout.Button($"Add {title}"))
            {
                int index = entries.arraySize++;
                Initialize(entries.GetArrayElementAtIndex(index), isSfx);
            }
        }

        private bool DrawHeader(SerializedProperty entries, int index, string title, bool isSfx)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SerializedProperty element = entries.GetArrayElementAtIndex(index);
                SerializedProperty id = element.FindPropertyRelative("id");
                element.isExpanded = EditorGUILayout.Foldout(
                    element.isExpanded, FormatRowLabel(title, index, id?.stringValue ?? string.Empty), true);
                GUI.enabled = index > 0;
                if (GUILayout.Button("▲", GUILayout.Width(28f)))
                {
                    entries.MoveArrayElement(index, index - 1);
                }
                GUI.enabled = index < entries.arraySize - 1;
                if (GUILayout.Button("▼", GUILayout.Width(28f)))
                {
                    entries.MoveArrayElement(index, index + 1);
                }
                GUI.enabled = true;
                if (GUILayout.Button("Duplicate", GUILayout.Width(72f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    DuplicateEntry((SoundDatabaseSO)target, isSfx, index);
                    serializedObject.Update();
                    return true;
                }
                if (GUILayout.Button("Remove", GUILayout.Width(64f)))
                {
                    entries.DeleteArrayElementAtIndex(index);
                    return true;
                }
            }

            return false;
        }

        private static void DrawEntryFields(SerializedProperty element, bool isSfx)
        {
            SerializedProperty clip = element.FindPropertyRelative("clip");
            SerializedProperty volume = element.FindPropertyRelative("volume");
            SerializedProperty pitch = element.FindPropertyRelative("pitch");
            SerializedProperty loop = element.FindPropertyRelative("loop");
            EditorGUILayout.PropertyField(clip);
            EditorGUILayout.PropertyField(volume);
            EditorGUILayout.PropertyField(pitch);
            EditorGUILayout.PropertyField(loop);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    clip.objectReferenceValue == null || EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Preview"))
                    {
                        PreviewClip((AudioClip)clip.objectReferenceValue,
                            volume.floatValue, pitch.floatValue, loop.boolValue);
                    }
                }

                using (new EditorGUI.DisabledScope(ActivePreviewSource == null))
                {
                    if (GUILayout.Button("Stop"))
                    {
                        StopPreview();
                    }
                }
            }

            if (!isSfx)
            {
                return;
            }

            EditorGUILayout.PropertyField(element.FindPropertyRelative("useSpatialAudio"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("minDistance"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("maxDistance"));
        }

        private string GetWarning(SerializedProperty entries, int index, bool isSfx)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            var warnings = new List<string>();
            string id = element.FindPropertyRelative("id").stringValue;
            bool hasClip = element.FindPropertyRelative("clip").objectReferenceValue != null;
            bool validDistance = !isSfx || HasValidDistance(element);

            if (string.IsNullOrWhiteSpace(id)) warnings.Add("ID is blank; this row will be skipped.");
            if (!hasClip) warnings.Add("Audio Clip is missing; this row will be skipped.");
            if (!validDistance) warnings.Add("Distance must be finite and satisfy 0 <= Min <= Max.");
            if (!string.IsNullOrWhiteSpace(id) && hasClip && validDistance && HasEarlierDuplicate(entries, index, id, isSfx))
            {
                warnings.Add("A previous valid row has this ID; this row will be skipped.");
            }

            return string.Join("\n", warnings);
        }

        private bool HasEarlierDuplicate(SerializedProperty entries, int index, string id, bool isSfx)
        {
            for (int previous = 0; previous < index; previous++)
            {
                SerializedProperty candidate = entries.GetArrayElementAtIndex(previous);
                if (!IsNullEntry(previous, isSfx)
                    && candidate.FindPropertyRelative("clip").objectReferenceValue != null
                    && string.Equals(candidate.FindPropertyRelative("id").stringValue, id, StringComparison.Ordinal)
                    && (!isSfx || HasValidDistance(candidate)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasValidDistance(SerializedProperty element)
        {
            float min = element.FindPropertyRelative("minDistance").floatValue;
            float max = element.FindPropertyRelative("maxDistance").floatValue;
            return !float.IsNaN(min) && !float.IsInfinity(min)
                && !float.IsNaN(max) && !float.IsInfinity(max)
                && min >= 0f && max >= min;
        }

        private bool IsNullEntry(int index, bool isSfx)
        {
            var database = (SoundDatabaseSO)target;
            return isSfx
                ? index < database.SfxEntries.Count && database.SfxEntries[index] == null
                : index < database.BgmEntries.Count && database.BgmEntries[index] == null;
        }

        private static void Initialize(SerializedProperty element, bool isSfx)
        {
            element.isExpanded = true;
            element.FindPropertyRelative("id").stringValue = string.Empty;
            element.FindPropertyRelative("clip").objectReferenceValue = null;
            element.FindPropertyRelative("volume").floatValue = 1f;
            element.FindPropertyRelative("pitch").floatValue = 1f;
            element.FindPropertyRelative("loop").boolValue = false;
            if (!isSfx) return;
            element.FindPropertyRelative("useSpatialAudio").boolValue = false;
            element.FindPropertyRelative("minDistance").floatValue = 1f;
            element.FindPropertyRelative("maxDistance").floatValue = 50f;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
        }
    }
}
