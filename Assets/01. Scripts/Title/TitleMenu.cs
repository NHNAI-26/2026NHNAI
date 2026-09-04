using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Border.Title
{
    /// <summary>
    /// 타이틀 화면의 NEW GAME 동작을 담당한다.
    /// 세이브/로드는 제공하지 않으며 실행할 때마다 항상 새 게임이다.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private string mainSceneName = "01_Main";

        // 씬 로드를 요청한 뒤의 추가 입력을 무시하기 위한 플래그. 버튼 연타로 LoadScene이 중복 호출되는 것을 막는다.
        private bool loading;

        /// <summary>
        /// 누락된 참조를 자동 보정하고 버튼 클릭을 구독한다.
        /// </summary>
        private void Awake()
        {
            if (newGameButton == null)
            {
                newGameButton = GetComponentInChildren<Button>(true);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(NewGame);
            }
        }

        /// <summary>
        /// 구독을 해제해 파괴된 인스턴스가 호출되지 않도록 한다.
        /// </summary>
        private void OnDestroy()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(NewGame);
            }
        }

        /// <summary>
        /// 새 게임을 시작한다. 본편 씬을 로드한다.
        /// </summary>
        public void NewGame()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            SceneManager.LoadScene(mainSceneName);
        }
    }

}
