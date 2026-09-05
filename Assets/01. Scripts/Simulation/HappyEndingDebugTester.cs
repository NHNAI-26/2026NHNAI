#if UNITY_EDITOR
using Border.Research;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulation
{
    /// <summary>
    /// 해피엔딩을 6개 미션 클리어 없이 바로 보기 위한 에디터 전용 단축키. <b>F8</b>.
    /// <see cref="LaunchMissionDebugTester"/> 와 같은 방식으로 설치된다.
    ///
    /// 실제 엔딩과 다른 점은 하나뿐이다 — 확인 대기 중인 발사 결과가 없으면 신문 비트를 건너뛴다.
    /// 로켓은 시뮬레이션 씬이 떠 있으면 그 로켓을, 아니면 대역 형상을 쓴다.
    /// 연출이 끝나면 실제 엔딩과 똑같이 `00_Title` 로 나간다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HappyEndingDebugTester : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isPlaying || FindFirstObjectByType<HappyEndingDebugTester>() != null)
            {
                return;
            }

            var host = new GameObject("Happy Ending Debug Tester");
            host.AddComponent<HappyEndingDebugTester>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            if (Keyboard.current?.f8Key.wasPressedThisFrame == true)
            {
                PlayHappyEnding();
            }
        }

        public static bool PlayHappyEnding()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Happy Ending Debug Tester: Play Mode 에서만 엔딩을 재생할 수 있습니다.");
                return false;
            }

            if (FindFirstObjectByType<HappyEndingSequence>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("Happy Ending Debug Tester: 엔딩이 이미 재생 중입니다.");
                return false;
            }

            // 연구 화면은 설계 단계 동안 꺼져 있다 — 꺼진 것도 찾아야 신문 비트가 산다.
            var research = FindFirstObjectByType<ResearchOperationUIController>(FindObjectsInactive.Include);
            if (research == null)
            {
                Debug.LogWarning("Happy Ending Debug Tester: 연구 화면을 찾지 못했습니다. 01_Main 에서 눌러주세요.");
                return false;
            }

            GameObject rocketVisual = HappyEndingSequence.PreserveRocket(FindFirstObjectByType<Rocket>());
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            HappyEndingSequence.Play(rocketVisual, research, session.LastLaunchResult, null);
            return true;
        }

        [UnityEditor.MenuItem("Tools/Border/Debug/Play Happy Ending")]
        private static void PlayHappyEndingFromMenu()
        {
            PlayHappyEnding();
        }
    }
}
#endif
