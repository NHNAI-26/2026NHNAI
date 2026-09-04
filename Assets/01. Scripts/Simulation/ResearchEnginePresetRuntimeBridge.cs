using System.Collections.Generic;
using Border.Research;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class ResearchEnginePresetRuntimeBridge : MonoBehaviour
    {
        private const float DefaultFuelCapacity = 100f;
        private const float DefaultCooling = 60f;
        private const float DefaultMaxOutput = 1200f;
        private const float DefaultIgnitionReliability = 100f;

        [SerializeField] private RocketBuilder rocketBuilder;
        [SerializeField] private EnginePresetLibrarySO basePresetLibrary;

        private EnginePresetLibrarySO originalPresetLibrary;
        private EnginePresetLibrarySO runtimePresetLibrary;
        private int lastAppliedChecksum = int.MinValue;
        private bool runtimeApplied;

        public EnginePresetLibrarySO RuntimePresetLibrary => runtimePresetLibrary;
        public EnginePresetLibrarySO BasePresetLibrary => basePresetLibrary;

        /// <summary>
        /// <see cref="SimulationStageHost"/> 과 같은 이유로 씬 로드마다 다시 본다 —
        /// <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> 는 세션당 한 번뿐이라 타이틀에서
        /// 시작하면 이 다리가 아예 안 생겨 연구 프리셋이 시뮬레이션으로 넘어가지 않았다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SpawnInMainScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SpawnInMainScene();

        private static void SpawnInMainScene()
        {
            if (SceneManager.GetActiveScene().name != ResearchFlowSession.MainSceneName
                || FindFirstObjectByType<ResearchEnginePresetRuntimeBridge>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var host = new GameObject("Research Engine Preset Runtime Bridge");
            host.AddComponent<ResearchEnginePresetRuntimeBridge>();
        }

        private void Awake()
        {
            ResolveBuilder();
        }

        private void Update()
        {
            ResolveBuilder();
            if (rocketBuilder == null)
            {
                return;
            }

            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            if (!session.HasPendingDesignEntry)
            {
                RestoreOriginalLibrary();
                return;
            }

            int checksum = CalculateResearchChecksum(session.Model);
            if (runtimeApplied && checksum == lastAppliedChecksum && rocketBuilder.PresetLibrary == runtimePresetLibrary)
            {
                return;
            }

            ApplyToBuilder(rocketBuilder, session.Model, GetBaseLibrary());
            lastAppliedChecksum = checksum;
            runtimeApplied = true;
        }

        public EnginePresetLibrarySO ApplyToBuilder(
            RocketBuilder targetBuilder,
            ResearchPrototypeModel model,
            EnginePresetLibrarySO sourceLibrary = null)
        {
            if (targetBuilder == null)
            {
                return null;
            }

            if (originalPresetLibrary == null && targetBuilder.PresetLibrary != runtimePresetLibrary)
            {
                originalPresetLibrary = targetBuilder.PresetLibrary;
            }

            runtimePresetLibrary = BuildRuntimeLibrary(model, sourceLibrary ?? GetBaseLibrary());
            targetBuilder.SetPresetLibrary(runtimePresetLibrary);
            return runtimePresetLibrary;
        }

        public static EnginePresetLibrarySO BuildRuntimeLibrary(ResearchPrototypeModel model, EnginePresetLibrarySO sourceLibrary)
        {
            var runtimeSlots = new List<EngineStatsSO>(ResearchPrototypeModel.MaxEnginePresetCount);
            for (int i = 0; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
            {
                EnginePresetId presetId = (EnginePresetId)i;
                EnginePresetState researchState = model.GetEnginePreset(presetId);
                EngineStatsSO sourcePreset = GetSourcePreset(sourceLibrary, i);
                runtimeSlots.Add(BuildRuntimePreset(i, sourcePreset, researchState));
            }

            return EnginePresetLibrarySO.CreateRuntime(runtimeSlots);
        }

        public static EngineStatsSO BuildRuntimePreset(int presetIndex, EngineStatsSO sourcePreset, EnginePresetState researchState)
        {
            float scaleBase = Mathf.Max(1f, ResearchPrototypeModel.InitialEngineStat);
            float fuelScale = researchState.FuelCapacity / scaleBase;
            float coolingScale = researchState.Cooling / scaleBase;
            float outputScale = researchState.MaxOutput / scaleBase;
            float ignitionScale = researchState.IgnitionReliability / scaleBase;

            float baseFuel = sourcePreset != null ? sourcePreset.FuelCapacity : DefaultFuelCapacity;
            float baseCooling = sourcePreset != null ? sourcePreset.Cooling : DefaultCooling;
            float baseOutput = sourcePreset != null ? sourcePreset.MaxOutput : DefaultMaxOutput;
            float baseIgnition = sourcePreset != null ? sourcePreset.IgnitionReliability : DefaultIgnitionReliability;

            return EngineStatsSO.CreateRuntimeCopy(
                presetIndex,
                sourcePreset,
                ResearchPrototypeModel.EngineInstallCost,
                baseFuel * fuelScale,
                baseCooling * coolingScale,
                baseOutput * outputScale,
                Mathf.Clamp(baseIgnition * ignitionScale, 0f, 100f));
        }

        private static EngineStatsSO GetSourcePreset(EnginePresetLibrarySO sourceLibrary, int presetIndex)
        {
            if (sourceLibrary == null || presetIndex < 0 || presetIndex >= sourceLibrary.Slots.Count)
            {
                return null;
            }

            return sourceLibrary.Slots[presetIndex];
        }

        private static int CalculateResearchChecksum(ResearchPrototypeModel model)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < model.EnginePresets.Length; i++)
                {
                    EnginePresetState preset = model.EnginePresets[i];
                    hash = hash * 31 + (int)preset.PresetId;
                    hash = hash * 31 + preset.Completion;
                    hash = hash * 31 + preset.FuelCapacity;
                    hash = hash * 31 + preset.Cooling;
                    hash = hash * 31 + preset.MaxOutput;
                    hash = hash * 31 + preset.IgnitionReliability;
                }

                return hash;
            }
        }

        private void ResolveBuilder()
        {
            if (rocketBuilder == null)
            {
                rocketBuilder = FindFirstObjectByType<RocketBuilder>();
            }

            if (basePresetLibrary == null && rocketBuilder != null)
            {
                basePresetLibrary = rocketBuilder.PresetLibrary;
            }
        }

        private EnginePresetLibrarySO GetBaseLibrary()
        {
            if (basePresetLibrary != null)
            {
                return basePresetLibrary;
            }

            return originalPresetLibrary;
        }

        private void RestoreOriginalLibrary()
        {
            if (!runtimeApplied || rocketBuilder == null)
            {
                return;
            }

            rocketBuilder.SetPresetLibrary(originalPresetLibrary);
            runtimeApplied = false;
            runtimePresetLibrary = null;
            lastAppliedChecksum = int.MinValue;
        }
    }
}
