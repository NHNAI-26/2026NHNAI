using System.Collections;
using Border.Audio;
using Border.UI;
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
        [SerializeField] private Transform rocketSoundTarget;

        private bool loading;
        private readonly SoundHandle[] rocketLoops = new SoundHandle[4];
        private AudioListener titleListener;

        private void Awake()
        {
            if (Border.Audio.SoundManager.Instance == null && soundManagerPrefab != null)
                Instantiate(soundManagerPrefab);
            ConfigureClickSound(newGameButton);
            ConfigureClickSound(settingsButton);
            ConfigureClickSound(quitButton);
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (newGameButton == null || settingsButton == null || quitButton == null || settingsMenu == null)
                Debug.LogError("TitleScreen prefab has missing menu references.", this);
        }

        private static void ConfigureClickSound(Button button)
        {
            if (button == null) return;
            // These actions play before loading a scene or quitting, so skip the global hook.
            if (button.GetComponent<UIManualClickSound>() == null)
                button.gameObject.AddComponent<UIManualClickSound>();
            UISelectableSoundHook.Bind(button);
        }

        private void OnEnable()
        {
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null)
                titleListener = Camera.main.gameObject.AddComponent<AudioListener>();

            if (rocketSoundTarget == null)
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    if (root.name != "TitleRocket") continue;
                    rocketSoundTarget = root.transform;
                    break;
                }
            }

            if (rocketSoundTarget == null || SoundManager.Instance == null) return;
            for (int i = 0; i < rocketLoops.Length; i++)
                rocketLoops[i] = SoundManager.Instance.PlaySfxAttached("RocketLoop", rocketSoundTarget);
        }

        private void OnDisable()
        {
            foreach (SoundHandle loop in rocketLoops) loop.Stop();
        }

        private void OnDestroy()
        {
            if (titleListener != null) Destroy(titleListener);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        public void NewGame()
        {
            if (loading) return;
            loading = true;
            SoundManager.Instance?.PlaySfx("click");
            ResearchFlowSession.PrepareNewGame();
            SceneManager.LoadScene(mainSceneName);
        }

        private void OpenSettings()
        {
            if (loading || settingsMenu == null) return;
            SoundManager.Instance?.PlaySfx("click");
            settingsMenu.Open();
        }

        // PauseMenuController.QuitGame 과 같은 관용구다. 에디터에서 Application.Quit 은 무동작이라
        // 플레이 모드를 직접 내려야 종료가 확인된다.
        private void QuitGame()
        {
            if (loading) return;
            loading = true;
            StartCoroutine(QuitAfterClick());
        }

        private IEnumerator QuitAfterClick()
        {
            SoundHandle click = SoundManager.Instance != null
                ? SoundManager.Instance.PlaySfx("click") : SoundHandle.Invalid;
            while (click.IsPlaying) yield return null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
