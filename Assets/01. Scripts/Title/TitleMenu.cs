using Border.Settings;
using Border.Research;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Border.Title
{
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SimpleSettingsMenuController settingsMenu;
        [SerializeField] private string mainSceneName = "01_Main";

        [SerializeField] private Border.Audio.SoundManager soundManagerPrefab;

        private bool loading;

        private void Awake()
        {
            if (Border.Audio.SoundManager.Instance == null && soundManagerPrefab != null)
                Instantiate(soundManagerPrefab);
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (newGameButton == null || settingsButton == null || quitButton == null || settingsMenu == null)
                Debug.LogError("TitleScreen prefab has missing menu references.", this);
        }

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        public void NewGame()
        {
            if (loading) return;
            loading = true;
            ResearchFlowSession.PrepareNewGame();
            SceneManager.LoadScene(mainSceneName);
        }

        private void OpenSettings()
        {
            if (!loading && settingsMenu != null) settingsMenu.Open();
        }

        // PauseMenuController.QuitGame 과 같은 관용구다. 에디터에서 Application.Quit 은 무동작이라
        // 플레이 모드를 직접 내려야 종료가 확인된다.
        private void QuitGame()
        {
            if (loading) return;
            loading = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
