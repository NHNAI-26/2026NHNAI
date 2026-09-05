using System;
using Simulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Border.Editor
{
    public sealed class ParachuteDebugWindow : EditorWindow
    {
        private const string LayoutPath = "Assets/06. Packages/Editor/Parachute/ParachuteDebugWindow.uxml";
        [SerializeField] private GameObject rocketSource;
        [SerializeField] private float missionAltitude = 200f;
        [SerializeField] private float descentSeconds = 10f;
        private ParachutePreviewSession session;
        private Image viewport;
        private Slider timeSlider;
        private Label status;
        private Label clock;
        private Button pause;
        private Button finish;
        private ObjectField sourceField;
        private double lastUpdate;
        private bool needsRender;

        [MenuItem("Tools/Border/Debug/Parachute Preview")]
        public static void Open()
        {
            var window = GetWindow<ParachuteDebugWindow>();
            window.titleContent = new GUIContent("낙하산 연출");
            window.minSize = new Vector2(500f, 580f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += UpdatePreview;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.playModeStateChanged += HandlePlayModeChange;
            lastUpdate = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            AssemblyReloadEvents.beforeAssemblyReload -= Release;
            EditorApplication.playModeStateChanged -= HandlePlayModeChange;
            Release();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (layout == null)
            {
                rootVisualElement.Add(new Label("낙하산 미리보기 레이아웃을 찾을 수 없어요."));
                return;
            }
            layout.CloneTree(rootVisualElement);
            rootVisualElement.AddToClassList("parachute-window");
            viewport = rootVisualElement.Q<Image>("viewport");
            viewport.scaleMode = ScaleMode.ScaleToFit;
            status = rootVisualElement.Q<Label>("status");
            clock = rootVisualElement.Q<Label>("clock");
            sourceField = rootVisualElement.Q<ObjectField>("rocketSource");
            sourceField.objectType = typeof(GameObject);
            sourceField.allowSceneObjects = true;
            if (rocketSource == null) rocketSource = FindFirstObjectByType<Rocket>()?.gameObject;
            sourceField.SetValueWithoutNotify(rocketSource);
            sourceField.RegisterValueChangedCallback(change => { rocketSource = change.newValue as GameObject; Rebuild(false); });
            rootVisualElement.Q<Button>("useCurrent").clicked += UseCurrentRocket;
            var altitudeField = rootVisualElement.Q<FloatField>("altitude");
            altitudeField.SetValueWithoutNotify(missionAltitude);
            altitudeField.RegisterValueChangedCallback(change =>
            {
                missionAltitude = Mathf.Clamp(change.newValue, 0f, 100000f);
                altitudeField.SetValueWithoutNotify(missionAltitude);
                Rebuild(false);
            });
            var durationField = rootVisualElement.Q<Slider>("duration");
            durationField.SetValueWithoutNotify(descentSeconds);
            durationField.RegisterValueChangedCallback(change => { descentSeconds = change.newValue; Rebuild(false); });
            rootVisualElement.Q<Button>("restart").clicked += () => Rebuild(true);
            pause = rootVisualElement.Q<Button>("pause");
            pause.clicked += () => { session?.TogglePause(); lastUpdate = EditorApplication.timeSinceStartup; RefreshControls(); };
            finish = rootVisualElement.Q<Button>("finish");
            finish.clicked += () => { session?.FinishReward(); RefreshControls(); };
            timeSlider = rootVisualElement.Q<Slider>("time");
            timeSlider.RegisterValueChangedCallback(change => { session?.Seek(change.newValue); needsRender = true; RefreshControls(); });
            viewport.RegisterCallback<GeometryChangedEvent>(_ => needsRender = true);
            Rebuild(false);
        }

        private void UseCurrentRocket()
        {
            rocketSource = FindFirstObjectByType<Rocket>()?.gameObject;
            sourceField.SetValueWithoutNotify(rocketSource);
            Rebuild(false);
        }

        private void Rebuild(bool play)
        {
            Release();
            try
            {
                session = new ParachutePreviewSession(rocketSource, missionAltitude, descentSeconds);
                timeSlider.highValue = session.Duration;
                if (play) session.Restart();
                lastUpdate = EditorApplication.timeSinceStartup;
                needsRender = true;
                RefreshControls();
            }
            catch (Exception exception)
            {
                status.text = exception.Message;
                pause.SetEnabled(false);
                finish.SetEnabled(false);
                timeSlider.SetEnabled(false);
                Debug.LogException(exception);
            }
        }

        private void UpdatePreview()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastUpdate < 1d / 30d || session == null || viewport?.panel == null) return;
            float delta = (float)(now - lastUpdate);
            lastUpdate = now;
            if (session.Playing)
            {
                session.Tick(Mathf.Min(delta, 0.1f));
                needsRender = true;
            }
            if (!needsRender || viewport.contentRect.width < 64f || viewport.contentRect.height < 64f) return;
            viewport.image = session.Render(Mathf.RoundToInt(viewport.contentRect.width), Mathf.RoundToInt(viewport.contentRect.height));
            viewport.MarkDirtyRepaint();
            needsRender = false;
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (session == null) return;
            timeSlider.SetEnabled(true);
            timeSlider.SetValueWithoutNotify(session.Time);
            status.text = session.Status;
            clock.text = $"{session.Time:0.00} / {session.Duration:0.00}초";
            pause.text = session.Playing ? "일시정지" : "계속 재생";
            pause.SetEnabled(!session.WaitingForReward && !session.Completed);
            finish.SetEnabled(session.WaitingForReward);
        }

        private void HandlePlayModeChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) Release();
            else if (viewport?.panel != null) Rebuild(false);
        }

        private void Release()
        {
            if (viewport != null) viewport.image = null;
            session?.Dispose();
            session = null;
        }
    }
}
