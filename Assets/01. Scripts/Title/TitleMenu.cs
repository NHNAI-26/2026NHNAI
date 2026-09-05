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
            if (newGameButton == null || settingsButton == null || settingsMenu == null)
                Debug.LogError("TitleScreen prefab has missing menu references.", this);
        }

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
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
    }
}
