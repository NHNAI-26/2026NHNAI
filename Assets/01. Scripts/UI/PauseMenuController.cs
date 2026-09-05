using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Border.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        private const string TitleSceneName = "00_Title";
        private const float DefaultGameplayTimeScale = 1f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button quitButton;
        private float previousTimeScale = 1f;
        private bool paused;

        private void Awake()
        {
            if (panelRoot == null || resumeButton == null || titleButton == null || quitButton == null)
            {
                Debug.LogError("PauseMenu prefab has missing UI references.", this);
                enabled = false;
                return;
            }
            UiEventSystemUtility.Ensure();
            resumeButton.onClick.AddListener(Resume);
            titleButton.onClick.AddListener(GoToTitle);
            quitButton.onClick.AddListener(QuitGame);
            panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (!WasEscapePressed()) return;
            // The research part development panel closes on escape first; it stands down after one frame.
            if (Border.Research.ResearchOperationUIController.IsPartDevelopmentOpen) return;
            if (paused) Resume();
            else Open();
        }

        public void Open()
        {
            if (paused || panelRoot == null) return;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : DefaultGameplayTimeScale;
            Time.timeScale = 0f;
            paused = true;
            UiEventSystemUtility.Ensure();
            panelRoot.SetActive(true);
            resumeButton.Select();
        }

        public void Resume()
        {
            if (!paused) return;
            Time.timeScale = previousTimeScale;
            paused = false;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDisable() => Resume();

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (titleButton != null) titleButton.onClick.RemoveListener(GoToTitle);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        private void GoToTitle()
        {
            Time.timeScale = 1f;
            paused = false;
            SceneManager.LoadScene(TitleSceneName);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        internal static bool WasEscapePressed()
        {
            if (WasEscapePressedByInputSystem())
            {
                return true;
            }

            try
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool WasEscapePressedByInputSystem()
        {
            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (keyboardType == null)
            {
                return false;
            }

            PropertyInfo currentProperty = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            object keyboard = currentProperty?.GetValue(null);
            if (keyboard == null)
            {
                return false;
            }

            PropertyInfo escapeKeyProperty = keyboardType.GetProperty("escapeKey", BindingFlags.Public | BindingFlags.Instance);
            object escapeKey = escapeKeyProperty?.GetValue(keyboard);
            if (escapeKey == null)
            {
                return false;
            }

            PropertyInfo pressedProperty = escapeKey.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
            return pressedProperty != null && pressedProperty.GetValue(escapeKey) is bool pressed && pressed;
        }


    }
}
