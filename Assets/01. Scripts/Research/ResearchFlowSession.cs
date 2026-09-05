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
            ResearchActionResult result = Model.TryEnterDesign(missionId, presetId, out data);
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
            ResearchActionResult action = Model.CompleteLaunch(succeeded, out result);
            if (action == ResearchActionResult.Success)
            {
                lastLaunchResult = result;
                hasLastLaunchResult = true;
                HasUnacknowledgedLaunchResult = true;
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

        public void ResetResearch()
        {
            model = CreateModel();
            ClearPendingDesignEntry();
            lastLaunchResult = default;
            hasLastLaunchResult = false;
            HasUnacknowledgedLaunchResult = false;
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
