using System.IO;
using Border.Audio.Editor;
using Border.Core;
using UnityEditor;
using UnityEngine;

namespace Border.Voice.Editor
{
    /// <summary>
    /// 대사 음성을 귀로 확인하고 .wav 로 굽는 창. Tools/Voice Baker.
    /// 구운 클립은 SoundDatabase 에 직접 끌어다 놓으면 기존 SoundManager.PlaySfx 경로를 그대로 탄다 —
    /// 여기서 자동 등록하지 않는 이유는 SoundDatabase.asset 이 팀 전체가 공유하는 단일 YAML 이라서다.
    /// </summary>
    public sealed class VoiceBakerWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/04. Audios/SFX/Voice";

        [SerializeField] private VoicePresetSO preset;
        [SerializeField] private string line = "안녕 반가워";
        [SerializeField] private int seed = 1;
        [SerializeField] private string clipName = "VO_Sample";
        [SerializeField] private string outputFolder = DefaultOutputFolder;

        // 에셋이 아닌 UnityEngine.Object 는 직렬화하지 않는다. 도메인 리로드에서 유령 참조로 남는다.
        private AudioClip previewClip;
        private UnityEditor.Editor presetEditor;
        private Vector2 scroll;
        private string status = string.Empty;

        [MenuItem("Tools/Voice Baker")]
        private static void Open() => GetWindow<VoiceBakerWindow>("Voice Baker");

        private void OnDisable() => ClearPreview();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            using (new EditorGUILayout.HorizontalScope())
            {
                preset = (VoicePresetSO)EditorGUILayout.ObjectField("프리셋", preset, typeof(VoicePresetSO), false);
                if (GUILayout.Button("새로 만들기", GUILayout.Width(80f)))
                {
                    CreatePreset();
                }
            }

            if (preset != null)
            {
                UnityEditor.Editor.CreateCachedEditor(preset, null, ref presetEditor);
                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    presetEditor.OnInspectorGUI();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("대사");
            line = EditorGUILayout.TextArea(line, GUILayout.MinHeight(48f));

            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField("시드", seed);
                if (GUILayout.Button("굴리기", GUILayout.Width(60f)))
                {
                    seed = Random.Range(1, int.MaxValue);
                    Preview();
                }
            }

            EditorGUILayout.HelpBox(
                "시드가 같으면 같은 소리가 나온다. 0 이면 매번 달라져 구울 때마다 결과가 바뀐다.",
                MessageType.None);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(preset == null))
                {
                    if (GUILayout.Button("미리듣기", GUILayout.Height(24f)))
                    {
                        Preview();
                    }
                }

                if (GUILayout.Button("정지", GUILayout.Height(24f), GUILayout.Width(60f)))
                {
                    ClearPreview();
                }
            }

            EditorGUILayout.Space(12f);
            clipName = EditorGUILayout.TextField("파일 이름", clipName);
            outputFolder = EditorGUILayout.TextField("출력 폴더", outputFolder);

            using (new EditorGUI.DisabledScope(preset == null))
            {
                if (GUILayout.Button("굽기 (.wav)", GUILayout.Height(28f)))
                {
                    Bake();
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Preview()
        {
            if (!VoiceBaker.TryBake(preset, line, seed, out float[] samples, out string error))
            {
                ClearPreview();
                status = error;
                return;
            }

            ClearPreview();

            // lengthSamples 는 인터리브 개수가 아니라 프레임 수다. 모노라 지금은 같지만 의미를 지켜 쓴다.
            previewClip = AudioClip.Create("VoicePreview", samples.Length, 1, VoiceBaker.OutputFrequency, false);
            previewClip.SetData(samples, 0);

            // 배속은 이미 샘플에 구워져 있다. AudioSource 피치는 1 이어야 미리듣기와 구운 결과가 같다.
            SoundDatabaseSOEditor.PreviewClip(previewClip, 1f, 1f, false);
            status = $"{samples.Length / (float)VoiceBaker.OutputFrequency:0.00}초";
        }

        private void Bake()
        {
            if (!VoiceBaker.TryBake(preset, line, seed, out float[] samples, out string error))
            {
                EditorUtility.DisplayDialog("Voice Baker", error, "확인");
                return;
            }

            string safeName = string.IsNullOrWhiteSpace(clipName) ? "VO_Untitled" : clipName.Trim();
            EnsureFolder(outputFolder);
            string assetPath = $"{outputFolder}/{safeName}.wav";

            File.WriteAllBytes(
                Path.Combine(Directory.GetCurrentDirectory(), assetPath),
                VoiceBaker.EncodeWav16(samples, 1, VoiceBaker.OutputFrequency));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            DisableNormalize(assetPath);

            var baked = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            EditorGUIUtility.PingObject(baked);
            Selection.activeObject = baked;

            status = $"{assetPath} 로 구웠다. SoundDatabase 의 SFX 목록에 끌어다 놓으면 PlaySfx 로 재생된다.";
            Log.D($"Baked voice line to {assetPath} ({samples.Length} samples, seed {seed})");
        }

        /// <summary>
        /// 오디오 임포터는 기본으로 피크 노멀라이즈를 켠다. 그대로 두면 구운 파일 음량이 미리듣기와 달라진다.
        /// </summary>
        private static void DisableNormalize(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            var serialized = new SerializedObject(importer);
            SerializedProperty normalize = serialized.FindProperty("m_Normalize");
            if (normalize == null || !normalize.boolValue)
            {
                return;
            }

            normalize.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        private void CreatePreset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "새 목소리 프리셋", "VoicePreset", "asset", string.Empty, "Assets/02. ScriptableObjects/Audio");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var created = CreateInstance<VoicePresetSO>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            preset = created;
        }

        private void ClearPreview()
        {
            SoundDatabaseSOEditor.StopPreview();
            if (previewClip != null)
            {
                DestroyImmediate(previewClip);
            }

            previewClip = null;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
