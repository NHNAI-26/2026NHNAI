using System;
using System.Collections.Generic;
using System.Reflection;
using Border.Research;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Border.Editor
{
    /// <summary>
    /// 프리팹 스테이지에서 연구 화면을 실제 데이터가 채워진 상태로 미리 본다.
    /// 편집 중인 원본 루트는 건드리지 않고, 그 사본을 프리뷰 씬 옆자리에 세운다.
    /// 프리팹 스테이지는 prefabContentsRoot 하나만 저장하므로 이 사본은 절대 프리팹에 들어가지 않는다.
    /// </summary>
    public static class UiPrefabStagePreview
    {
        private const string PreviewRootName = "__UI Preview (not saved)";
        private const string CanvasName = "ResearchOperationCanvas";
        private static readonly Vector3 PreviewOffset = new Vector3(1400f, 0f, 0f);

        // GetOrCreate 를 우회하기 위한 통로. 아래 PreviewResearchScreen 주석 참고.
        private static readonly FieldInfo SessionInstance =
            typeof(ResearchFlowSession).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);

        [InitializeOnLoadMethod]
        private static void HookStageClose()
        {
            PrefabStage.prefabStageClosing += stage => ClearIn(stage.scene);
        }

        [MenuItem("Border/UI/Debug/Preview Research Screen")]
        public static void PreviewResearchScreen()
        {
            PrefabStage stage = RequireStage();
            ClearIn(stage.scene);

            var host = new GameObject(PreviewRootName);
            SceneManager.MoveGameObjectToScene(host, stage.scene);

            // ResearchFlowSession.GetOrCreate 는 FindFirstObjectByType 을 쓰는데 이 API 는 프리뷰 씬을
            // 보지 못한다. 미리 심어두지 않으면 열려 있는 실제 씬에 세션 오브젝트가 생겨 씬이 더러워진다.
            var sessionHost = new GameObject("Research Flow Session (preview)");
            sessionHost.transform.SetParent(host.transform, false);
            SessionInstance.SetValue(null, sessionHost.AddComponent<ResearchFlowSession>());

            var controller = host.AddComponent<ResearchOperationUIController>();
            // 저장 안 된 현재 편집본을 그대로 복제한다 — Instantiate 는 씬 오브젝트도 받는다.
            controller.ConfigureScreenPrefabForTests(stage.prefabContentsRoot);

            List<GameObject> before = ActiveSceneRoots();
            controller.InitializeForTests();
            AdoptStrayEventSystem(before, host, stage.scene);

            Transform preview = host.transform.Find(CanvasName);
            if (preview == null)
            {
                Object.DestroyImmediate(host);
                SessionInstance.SetValue(null, null);
                throw new InvalidOperationException(
                    "연구 화면 사본을 만들지 못했다. Console 의 프리팹 검증 오류를 먼저 확인해라.");
            }

            // 프리뷰 씬에서는 스테이지 루트만 오버레이로 그려진다. 사본은 월드 스페이스로 돌려 옆에 세운다.
            var canvas = preview.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = preview.GetComponent<CanvasScaler>();
            var rect = (RectTransform)preview;
            rect.sizeDelta = scaler != null ? scaler.referenceResolution : new Vector2(1280f, 720f);
            rect.localScale = Vector3.one;
            rect.position = PreviewOffset;

            // 에디트 모드에서는 DOTween 이 틱하지 않아 진입 연출이 alpha 0 인 채로 멈춘다.
            var animator = preview.GetComponent<ResearchOperationTransitionAnimator>();
            if (animator != null) animator.CompleteActiveSequenceForTests();

            Selection.activeGameObject = preview.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }

        [MenuItem("Border/UI/Debug/Toggle Part Development View")]
        public static void TogglePartDevelopmentView()
        {
            Transform preview = RequirePreview();
            GameObject hub = FindChild(preview, "HubActionBar");
            GameObject engineColumn = FindChild(preview, "EnginePresetColumn");
            GameObject detailColumn = FindChild(preview, "DetailColumn");

            bool showHub = hub != null && !hub.activeSelf;
            if (hub != null) hub.SetActive(showHub);
            if (engineColumn != null) engineColumn.SetActive(!showHub);
            if (detailColumn != null) detailColumn.SetActive(!showHub);

            var animator = preview.GetComponent<ResearchOperationTransitionAnimator>();
            if (animator != null) animator.ResetToFinalPositions();
        }

        [MenuItem("Border/UI/Debug/Clear Preview")]
        public static void ClearPreview()
        {
            ClearIn(RequireStage().scene);
        }

        private static PrefabStage RequireStage()
        {
            if (Application.isPlaying) throw new InvalidOperationException("플레이 모드를 먼저 빠져나와라.");
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) throw new InvalidOperationException("프리팹 스테이지를 먼저 열어라.");
            return stage;
        }

        private static Transform RequirePreview()
        {
            Transform host = FindPreviewHost(RequireStage().scene);
            Transform preview = host != null ? host.Find(CanvasName) : null;
            if (preview == null) throw new InvalidOperationException("Preview Research Screen 을 먼저 실행해라.");
            return preview;
        }

        private static Transform FindPreviewHost(Scene scene)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == PreviewRootName) return root.transform;
            }

            return null;
        }

        private static void ClearIn(Scene scene)
        {
            Transform host = FindPreviewHost(scene);
            if (host != null) Object.DestroyImmediate(host.gameObject);
            SessionInstance.SetValue(null, null);
        }

        private static List<GameObject> ActiveSceneRoots()
        {
            var roots = new List<GameObject>();
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid()) roots.AddRange(active.GetRootGameObjects());
            return roots;
        }

        // BuildInterface 의 EnsureEventSystem 도 프리뷰 씬을 보지 못해 실제 씬에 EventSystem 을 새로 만든다.
        // 새로 생긴 것만 프리뷰 씬으로 회수한다.
        private static void AdoptStrayEventSystem(List<GameObject> before, GameObject host, Scene scene)
        {
            foreach (GameObject root in ActiveSceneRoots())
            {
                if (before.Contains(root)) continue;
                if (root.GetComponentInChildren<EventSystem>(true) == null) continue;
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(host.transform, false);
            }
        }

        private static GameObject FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child.gameObject;
            }

            return null;
        }
    }
}
