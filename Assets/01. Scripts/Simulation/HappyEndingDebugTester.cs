#if UNITY_EDITOR
using System.Collections;
using Border.Research;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Simulation
{
    /// <summary>
    /// 엔딩을 6개 미션 클리어 없이 바로 보기 위한 에디터 전용 단축키. 해피엔딩 <b>F8</b>, 배드엔딩 <b>F9</b>.
    /// <see cref="LaunchMissionDebugTester"/> 와 같은 방식으로 설치된다.
    ///
    /// 해피엔딩이 실제와 다른 점은 둘이다 — 확인 대기 중인 발사 결과가 없으면 최종 미션 성공 결과를
    /// 지어내 신문을 띄우고, 발사대가 필요하므로 `SimulationTest` 씬이 없으면 직접 additive 로 올린다.
    /// 배드엔딩도 마찬가지로, 확인 대기 중인 결과가 없으면 마감 실패 결과를 지어내 실패 신문부터 띄운다.
    /// 둘 다 연출이 끝나면 실제 엔딩과 똑같이 `00_Title` 로 나간다.
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

            if (Keyboard.current?.f9Key.wasPressedThisFrame == true)
            {
                PlaySadEnding();
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

            var host = FindFirstObjectByType<HappyEndingDebugTester>();
            if (host == null)
            {
                Debug.LogWarning("Happy Ending Debug Tester: 테스터 인스턴스를 찾지 못했습니다.");
                return false;
            }

            host.StartCoroutine(host.PlayHappyEndingRoutine(research));
            return true;
        }

        /// <summary>
        /// 연출은 `SimulationTest` 씬의 발사대에서 로켓을 올린다. 연구 화면에서 눌렀다면 그 씬이
        /// 아직 없으므로 여기서 올린다 — 3D 구간이 끝나면 엔딩이 스스로 내린다.
        /// </summary>
        private IEnumerator PlayHappyEndingRoutine(ResearchOperationUIController research)
        {
            Scene simulation = SceneManager.GetSceneByName(SimulationStageHost.SimulationSceneName);
            if (!simulation.isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(
                    SimulationStageHost.SimulationSceneName, LoadSceneMode.Additive);
            }

            HappyEndingSequence.Play(research, ResolveResult(), null);
        }

        /// <summary>
        /// 진행 중인 세션에 확인 대기 중인 결과가 있으면 그것을, 없으면 최종 미션 성공 결과를 지어낸다.
        /// <see cref="ResearchFlowSession.LastLaunchResult"/> 는 결과가 없으면 예외를 던지므로
        /// 무조건 읽어서는 안 된다.
        /// </summary>
        private static ResearchLaunchResultData ResolveResult()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            if (session.HasUnacknowledgedLaunchResult) return session.LastLaunchResult;

            // 최종 미션이면 매체가 신문으로 고정되고, OutcomeEvent 가 없으면 "발사 성공 확인" 이 나온다.
            return new ResearchLaunchResultData(
                LaunchMissionId.LowPowerZoneHold,
                EnginePresetId.Engine01,
                ResearchPrototypeModel.EndYear,
                ResearchPrototypeModel.EndQuarter,
                0, 0,
                TestVisibility.Public,
                100, 0, 0,
                90, 5, 5, 10,
                ResearchGrade.A,
                0, 0,
                finalMissionWon: true,
                deadlineMissed: false);
        }

        public static bool PlaySadEnding()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Happy Ending Debug Tester: Play Mode 에서만 엔딩을 재생할 수 있습니다.");
                return false;
            }

            if (FindFirstObjectByType<SadEndingSequence>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("Happy Ending Debug Tester: 엔딩이 이미 재생 중입니다.");
                return false;
            }

            var research = FindFirstObjectByType<ResearchOperationUIController>(FindObjectsInactive.Include);
            if (research == null)
            {
                Debug.LogWarning("Happy Ending Debug Tester: 연구 화면을 찾지 못했습니다. 01_Main 에서 눌러주세요.");
                return false;
            }

            // 실제 마감 실패와 같은 기사를 쓴다 — 최종 미션·공개 발사로 고정돼 매체가 신문이 된다.
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            ResearchLaunchResultData result = session.HasUnacknowledgedLaunchResult
                ? session.LastLaunchResult
                : session.Model.CreateDeadlineFailureResult();

            SadEndingSequence.Play(research, result);
            return true;
        }

        [UnityEditor.MenuItem("Tools/Border/Debug/Play Happy Ending")]
        private static void PlayHappyEndingFromMenu()
        {
            PlayHappyEnding();
        }

        [UnityEditor.MenuItem("Tools/Border/Debug/Play Sad Ending")]
        private static void PlaySadEndingFromMenu()
        {
            PlaySadEnding();
        }
    }
}
#endif
