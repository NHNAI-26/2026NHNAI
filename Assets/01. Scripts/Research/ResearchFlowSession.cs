using System;
using UnityEngine;

namespace Border.Research
{
    public sealed class ResearchFlowSession : MonoBehaviour
    {
        public const string MainSceneName = "01_Main";
        public const string ResearchScreenName = "Research";
        public const string DesignScreenName = "Design";

        private static ResearchFlowSession instance;

        [SerializeField] private ResearchBalanceConfigSO balanceConfig;
        [SerializeField] private ResearchLaunchOutcomeEventChannelSO outcomeChannel;
        private ResearchLaunchOutcomeData pendingOutcome;
        public bool HasPendingOutcomeNotification { get; private set; }

        private ResearchPrototypeModel model;
        private ResearchDesignEntryData pendingDesignEntry;
        private ResearchLaunchResultData lastLaunchResult;
        private bool hasPendingDesignEntry;
        private bool hasLastLaunchResult;

        public ResearchPrototypeModel Model => model ??= CreateModel();
        public bool HasPendingDesignEntry => hasPendingDesignEntry;
        public bool HasLastLaunchResult => hasLastLaunchResult;
        public bool HasUnacknowledgedLaunchResult { get; private set; }
        public bool HasActiveLaunch => Model.HasActiveLaunch;

        public ResearchDesignEntryData PendingDesignEntry
        {
            get
            {
                if (!hasPendingDesignEntry)
                {
                    throw new InvalidOperationException("No pending design entry data exists.");
                }

                return pendingDesignEntry;
            }
        }

        public static ResearchFlowSession GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<ResearchFlowSession>();
            if (instance != null)
            {
                return instance;
            }

            var host = new GameObject("Research Flow Session");
            return host.AddComponent<ResearchFlowSession>();
        }

        public ResearchLaunchResultData LastLaunchResult
        {
            get
            {
                if (!hasLastLaunchResult)
                {
                    throw new InvalidOperationException("No launch result data exists.");
                }

                return lastLaunchResult;
            }
        }

        public ResearchActionResult TryEnterDesign(LaunchMissionId missionId, out ResearchDesignEntryData data)
        {
            return TryEnterDesign(missionId, EnginePresetId.Engine01, out data);
        }

        public ResearchActionResult TryEnterDesign(LaunchMissionId missionId, EnginePresetId presetId, out ResearchDesignEntryData data)
        {
            return TryEnterDesign(missionId, presetId, missionId == LaunchMissionId.LowPowerZoneHold ? TestVisibility.FinalMission : TestVisibility.Private, out data);
        }

        public ResearchActionResult TryEnterDesign(LaunchMissionId missionId, EnginePresetId presetId, TestVisibility visibility, out ResearchDesignEntryData data)
        {
            ResearchActionResult result = Model.TryEnterDesign(missionId, presetId, visibility, out data);
            if (result == ResearchActionResult.Success)
            {
                StoreDesignEntry(data);
            }
            else
            {
                ClearPendingDesignEntry();
            }

            return result;
        }

        public void StoreDesignEntry(ResearchDesignEntryData data)
        {
            pendingDesignEntry = data;
            hasPendingDesignEntry = true;
        }

        public void UpdatePendingDesignEntry(ResearchDesignEntryData data)
        {
            StoreDesignEntry(data);
        }

        public ResearchActionResult TryBeginPendingDesignLaunch()
        {
            if (HasActiveLaunch) return ResearchActionResult.LaunchInProgress;
            if (!hasPendingDesignEntry) return ResearchActionResult.NoPendingDesignEntry;
            ResearchActionResult action = Model.BeginLaunch(pendingDesignEntry);
            if (action == ResearchActionResult.Success)
            {
                hasLastLaunchResult = false;
                ClearPendingDesignEntry();
            }
            return action;
        }

        public ResearchActionResult CompleteActiveLaunch(bool succeeded, out ResearchLaunchResultData result)
        {
            return CompleteActiveLaunch(succeeded, null, out result);
        }

        public ResearchActionResult CompleteActiveLaunch(bool succeeded, string reason, out ResearchLaunchResultData result)
        {
            return CompleteActiveLaunch(succeeded, reason, succeeded ? LaunchTerminationReason.Succeeded : LaunchTerminationReason.Unknown, out result);
        }

        public ResearchActionResult CompleteActiveLaunch(bool succeeded, string reason, LaunchTerminationReason terminationReason, out ResearchLaunchResultData result)
        {
            ResearchActionResult action = Model.CompleteLaunch(succeeded, terminationReason, out result);
            if (action == ResearchActionResult.Success)
            {
                lastLaunchResult = result;
                hasLastLaunchResult = true;
                HasUnacknowledgedLaunchResult = true;
                QueueOutcome(result, reason);
            }
            return action;
        }

        public ResearchActionResult CommitPendingDesignLaunch(out ResearchLaunchResultData result)
        {
            result = default;
            if (!hasPendingDesignEntry)
            {
                return ResearchActionResult.NoPendingDesignEntry;
            }

            ResearchActionResult actionResult = Model.CommitLaunch(pendingDesignEntry, out result);
            if (actionResult == ResearchActionResult.Success)
            {
                lastLaunchResult = result;
                hasLastLaunchResult = true;
                HasUnacknowledgedLaunchResult = true;
                QueueOutcome(result, null);
                ClearPendingDesignEntry();
            }

            return actionResult;
        }

        public void ClearPendingDesignEntry()
        {
            pendingDesignEntry = default;
            hasPendingDesignEntry = false;
        }

        public void AcknowledgeLaunchResult()
        {
            HasUnacknowledgedLaunchResult = false;
        }

        private void QueueOutcome(ResearchLaunchResultData result, string reason)
        {
            pendingOutcome = new ResearchLaunchOutcomeData(result, reason);
            HasPendingOutcomeNotification = true;
        }

        public void PublishPendingLaunchOutcome()
        {
            if (!HasPendingOutcomeNotification) return;
            ResearchLaunchOutcomeData outcome = pendingOutcome;
            HasPendingOutcomeNotification = false;
            pendingOutcome = default;
            if (outcomeChannel != null) outcomeChannel.RaiseEvent(outcome);
        }

        public void ResetResearch()
        {
            model = CreateModel();
            ClearPendingDesignEntry();
            lastLaunchResult = default;
            hasLastLaunchResult = false;
            HasUnacknowledgedLaunchResult = false;
            HasPendingOutcomeNotification = false;
            pendingOutcome = default;
        }

        public static void PrepareNewGame()
        {
            ResearchFlowSession existing = instance != null ? instance : FindFirstObjectByType<ResearchFlowSession>();
            if (existing != null) existing.ResetResearch();
        }

        public static void ResetForTests()
        {
            foreach (ResearchFlowSession session in FindObjectsByType<ResearchFlowSession>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                DestroySessionObject(session.gameObject);
            }

            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                DestroySessionObject(gameObject);
                return;
            }

            instance = this;
            model ??= CreateModel();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private static void DestroySessionObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private ResearchPrototypeModel CreateModel()
        {
            return new ResearchPrototypeModel(balanceConfig: balanceConfig != null ? balanceConfig.ToRuntimeConfig() : null);
        }
    }
}
