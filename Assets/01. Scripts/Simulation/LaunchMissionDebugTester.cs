#if UNITY_EDITOR
using Border.Research;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulation
{
    /// <summary>
    /// Editor-only shortcut for jumping straight into a launch mission while testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaunchMissionDebugTester : MonoBehaviour
    {
        private const LaunchMissionId MissionThree = LaunchMissionId.TargetZone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isPlaying || FindFirstObjectByType<LaunchMissionDebugTester>() != null)
            {
                return;
            }

            var host = new GameObject("Launch Mission Debug Tester");
            host.AddComponent<LaunchMissionDebugTester>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            if (Keyboard.current?.f3Key.wasPressedThisFrame == true)
            {
                JumpToMissionThree();
            }
        }

        public static bool JumpToMissionThree()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Launch Mission Debug Tester: Play Mode에서만 3단계 설계 화면으로 이동할 수 있습니다.");
                return false;
            }

            ResearchActionResult result = PrepareMissionDesignEntry(MissionThree);
            if (result != ResearchActionResult.Success)
            {
                Debug.LogWarning($"Launch Mission Debug Tester: 3단계 설계 진입 준비 실패 ({result}).");
                return false;
            }

            bool opened = SimulationStageHost.OpenDesignStage();
            if (!opened)
            {
                Debug.LogWarning("Launch Mission Debug Tester: 설계 화면을 열지 못했습니다.");
            }

            return opened;
        }

        public static ResearchActionResult PrepareMissionDesignEntry(LaunchMissionId missionId, EnginePresetId presetId = EnginePresetId.Engine01)
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.PrepareDebugDesignEntryState(missionId, presetId);
            return session.TryEnterDesign(missionId, presetId, out _);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Border/Debug/Jump To Mission 3 Target Zone")]
        private static void JumpToMissionThreeFromMenu()
        {
            JumpToMissionThree();
        }
#endif
    }
}
#endif
