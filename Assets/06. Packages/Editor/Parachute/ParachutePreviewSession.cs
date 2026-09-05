using System;
using System.Collections.Generic;
using Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Border.Editor
{
    public sealed class ParachutePreviewSession : IDisposable
    {
        public const string PrefabPath = "Assets/03. Prefabs/Simulation/Success/MissionSuccessParachute.prefab";
        public const string SwayPath = "Assets/03. Prefabs/Simulation/Success/ParachuteSway.anim";
        private const string BodyPath = "Assets/03. Prefabs/3D/RocketBody.prefab";
        private const float LeadIn = 0.75f;
        private readonly List<Object> ownedAssets = new();
        private PreviewRenderUtility preview;
        private PlayableDirector director;
        private GameObject host;
        private GameObject rig;
        private float altitude;
        private float halfHeight;
        private float frameCenter;
        private float cameraDistance;

        public float Duration { get; private set; }
        public float Time { get; private set; }
        public bool Playing { get; private set; }
        public bool Completed { get; private set; }
        public bool WaitingForReward => !Completed && Time >= Duration;
        public Transform Socket { get; private set; }
        public GameObject RocketVisual { get; private set; }
        public Bounds AssemblyBounds => CalculateBounds(rig);
        public Camera Camera => preview.camera;

        public ParachutePreviewSession(GameObject rocketSource, float missionAltitude, float descentSeconds)
        {
            try
            {
                altitude = Mathf.Clamp(missionAltitude, 0f, 100000f);
                Duration = LeadIn + Mathf.Clamp(descentSeconds, 4f, 30f);
                preview = new PreviewRenderUtility();
                host = EditorUtility.CreateGameObjectWithHideFlags("Parachute Debug Preview", HideFlags.HideAndDontSave);
                preview.AddSingleGO(host);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var sway = AssetDatabase.LoadAssetAtPath<AnimationClip>(SwayPath);
                if (prefab == null || sway == null) throw new InvalidOperationException("낙하산 프리팹 또는 흔들림 클립을 찾을 수 없어요.");
                rig = preview.InstantiatePrefabInScene(prefab);
                rig.name = "MissionSuccessParachute";
                rig.transform.SetParent(host.transform, false);
                Socket = rig.transform.Find("DescentRoot/SwayPivot/RocketSwingPivot/RocketSocket");
                if (Socket == null) throw new InvalidOperationException("프리팹에 RocketSocket이 없어요.");

                bool fallback = rocketSource == null;
                if (fallback) rocketSource = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);
                if (rocketSource == null) throw new InvalidOperationException("미리 볼 로켓을 선택해 주세요.");
                RocketVisual = CopyVisual(rocketSource.transform, host.transform);
                RocketVisual.name = "Rocket Visual Preview";
                RocketVisual.transform.localPosition = Vector3.zero;
                RocketVisual.transform.localRotation = rocketSource.GetComponent<Rocket>() != null
                    ? Quaternion.identity : rocketSource.transform.rotation;
                RocketVisual.transform.localScale = rocketSource.transform.lossyScale;
                Bounds rocketBounds = CalculateBounds(RocketVisual);
                if (rocketBounds.size.y < 0.001f) throw new InvalidOperationException("선택한 로켓에 표시할 메시가 없어요.");
                if (fallback)
                {
                    RocketVisual.transform.localScale *= 6f / rocketBounds.size.y;
                    rocketBounds = CalculateBounds(RocketVisual);
                }
                RocketVisual.transform.SetParent(Socket, false);
                RocketVisual.transform.localPosition = -new Vector3(rocketBounds.center.x, rocketBounds.max.y, rocketBounds.center.z);
                Bounds assembly = CalculateBounds(rig);
                halfHeight = Mathf.Max(4f, assembly.size.y * 0.7f, assembly.size.x * 0.9f);
                frameCenter = altitude + assembly.center.y;
                cameraDistance = Mathf.Max(30f, assembly.size.z * 3f);
                float margin = Mathf.Max(1f, assembly.size.y * 0.2f);
                float startY = frameCenter + halfHeight - assembly.min.y + margin;
                float endY = frameCenter - halfHeight - assembly.max.y - margin;
                ConfigureCamera();
                BuildTimeline(sway, startY, endY);
                Seek(LeadIn + (Duration - LeadIn) * 0.5f);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Restart()
        {
            Completed = false;
            Seek(0f);
            Playing = true;
        }

        public void TogglePause()
        {
            if (WaitingForReward || Completed) return;
            Playing = !Playing;
        }

        public void Seek(float seconds)
        {
            Playing = false;
            Completed = false;
            Time = Mathf.Clamp(seconds, 0f, Duration);
            Evaluate();
        }

        public void Tick(float seconds)
        {
            if (!Playing) return;
            Time = Mathf.Min(Duration, Time + Mathf.Max(0f, seconds));
            Evaluate();
            if (Time >= Duration) Playing = false;
        }

        public void FinishReward()
        {
            if (!WaitingForReward) return;
            Completed = true;
            Playing = false;
        }

        public string Status => Completed ? "연출 종료" : WaitingForReward ? "신문 / 보상 종료 대기"
            : Time < LeadIn ? "카메라 전환 후 진입 대기" : Playing ? "낙하산 하강 중" : "일시정지 / 구간 미리보기";

        public Texture Render(int width, int height)
        {
            preview.BeginPreview(new Rect(0, 0, Mathf.Clamp(width, 64, 1600), Mathf.Clamp(height, 64, 1200)), GUIStyle.none);
            preview.camera.aspect = (float)Mathf.Max(width, 1) / Mathf.Max(height, 1);
            preview.Render(true, false);
            return preview.EndPreview();
        }

        public void Dispose()
        {
            Playing = false;
            if (director != null) director.Stop();
            if (preview != null) preview.Cleanup();
            preview = null;
            for (int i = ownedAssets.Count - 1; i >= 0; i--)
                if (ownedAssets[i] != null) Object.DestroyImmediate(ownedAssets[i]);
            ownedAssets.Clear();
        }

        private void Evaluate()
        {
            director.time = Time;
            director.Evaluate();
        }

        private void ConfigureCamera()
        {
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = halfHeight;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = cameraDistance * 3f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, frameCenter, -cameraDistance), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.18f, 0.27f);
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            preview.lights[1].intensity = 1.1f;
        }

        private void BuildTimeline(AnimationClip sway, float startY, float endY)
        {
            var timeline = Own(ScriptableObject.CreateInstance<TimelineAsset>());
            var descentClip = Own(new AnimationClip { name = "Debug Parachute Descent", frameRate = 60f });
            var curve = new AnimationCurve(new Keyframe(0f, startY), new Keyframe(LeadIn, startY), new Keyframe(Duration, endY));
            AnimationUtility.SetEditorCurve(descentClip,
                EditorCurveBinding.FloatCurve("MissionSuccessParachute", typeof(Transform), "m_LocalPosition.y"), curve);
            var rootAnimator = host.AddComponent<Animator>();
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            director = host.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.playableAsset = timeline;
            AddTrack(timeline, "하강", descentClip, rootAnimator);
            AddTrack(timeline, "좌우 흔들림", sway, rig.transform.Find("DescentRoot").GetComponent<Animator>());
        }

        private void AddTrack(TimelineAsset timeline, string name, AnimationClip clip, Animator animator)
        {
            var track = Own(timeline.CreateTrack<AnimationTrack>(null, name));
            track.trackOffset = TrackOffset.ApplySceneOffsets;
            var item = track.CreateClip<AnimationPlayableAsset>();
            var playable = Own((AnimationPlayableAsset)item.asset);
            playable.clip = clip;
            item.duration = Duration;
            director.SetGenericBinding(track, animator);
        }

        private T Own<T>(T value) where T : Object
        {
            value.hideFlags = HideFlags.HideAndDontSave;
            ownedAssets.Add(value);
            return value;
        }

        private GameObject CopyVisual(Transform source, Transform parent)
        {
            var copy = EditorUtility.CreateGameObjectWithHideFlags(source.name, HideFlags.HideAndDontSave);
            copy.transform.SetParent(parent, false);
            copy.transform.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            copy.transform.localScale = source.localScale;
            var renderer = source.GetComponent<Renderer>();
            Mesh mesh = null;
            if (renderer != null && renderer.enabled)
            {
                if (renderer is MeshRenderer) mesh = source.GetComponent<MeshFilter>()?.sharedMesh;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    mesh = Own(new Mesh());
                    skinned.BakeMesh(mesh);
                }
            }
            if (mesh != null)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                var target = copy.AddComponent<MeshRenderer>();
                target.sharedMaterials = renderer.sharedMaterials;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                target.SetPropertyBlock(block);
            }
            foreach (Transform child in source)
                if (child.gameObject.activeSelf) CopyVisual(child, copy.transform);
            return copy;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
