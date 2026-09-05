using System;
using System.Collections.Generic;
using UnityEngine;

namespace Border.Research
{
    [CreateAssetMenu(fileName = "ResearchBalanceConfig", menuName = "Research/Balance Config")]
    public sealed class ResearchBalanceConfigSO : ScriptableObject
    {
        [SerializeField] private int initialFunds = ResearchPrototypeModel.InitialFunds;
        [SerializeField] private int initialQuarterlyFunding = ResearchPrototypeModel.InitialQuarterlyFunding;
        [SerializeField] private int minQuarterlyFunding = ResearchPrototypeModel.MinQuarterlyFunding;
        [SerializeField] private int maxQuarterlyFunding = ResearchPrototypeModel.MaxQuarterlyFunding;
        [SerializeField] private int researchCompletionGain = ResearchPrototypeModel.ResearchCompletionGain;
        [SerializeField] private int engineNormalResearchCost = ResearchPrototypeModel.EngineNormalResearchCost;
        [SerializeField] private int engineFocusedResearchCost = ResearchPrototypeModel.EngineFocusedResearchCost;
        [SerializeField] private int newEnginePresetCost = ResearchPrototypeModel.NewEnginePresetCost;
        [SerializeField] private int engineInstallCost = ResearchPrototypeModel.EngineInstallCost;
        [SerializeField] private ScoreRewardBandEntry[] normalResearchStatRewards =
        {
            new(0, 10),
            new(50, 13),
            new(80, 16),
        };
        [SerializeField] private ScoreRewardBandEntry[] focusedResearchStatRewards =
        {
            new(0, 16),
            new(50, 21),
            new(80, 26),
        };
        [SerializeField] private GradeRewardEntry[] launchRewards =
        {
            new(ResearchGrade.S, 900, 150),
            new(ResearchGrade.A, 600, 100),
            new(ResearchGrade.B, 400, 50),
            new(ResearchGrade.C, 150, 0),
            new(ResearchGrade.F, 0, -100),
        };
        [SerializeField] private int publicSuccessModifier = -10;
        [SerializeField] private int privateSuccessModifier = 10;
        [SerializeField] private int finalMissionSuccessModifier = 0;
        [SerializeField] private float publicRewardMultiplier = 1.5f;
        [SerializeField] private float privateRewardMultiplier = 0.5f;
        [SerializeField] private float finalMissionRewardMultiplier = 1f;
        [SerializeField] private int publicFailureQuarterlyFundingDelta = -150;
        [SerializeField] private int privateFailureQuarterlyFundingDelta = -50;
        [SerializeField] private int finalMissionFailureQuarterlyFundingDelta = -100;
        [SerializeField] private MissionBalanceEntry[] missions =
        {
            new(LaunchMissionId.StaticFire, "정적 연소 시험", 600, "기본 해금", 0f),
            new(LaunchMissionId.LowAltitude, "낮은 고도 도달", 800, "정적 연소 시험 C 이상", 0.55f),
            new(LaunchMissionId.HighAltitude, "높은 고도 도달", 900, "낮은 고도 도달 C 이상", 0.50f),
            new(LaunchMissionId.TargetZone, "목표 구역 도달", 1100, "높은 고도 도달 C 이상", 0.45f),
            new(LaunchMissionId.ZoneHold, "목표 구역 체류", 1300, "목표 구역 도달 C 이상", 0.42f),
            new(LaunchMissionId.LowPowerZoneHold, "저전력 검증", 1500, "목표 구역 체류 C 이상", 0.40f),
        };

        public ResearchBalanceConfig ToRuntimeConfig()
        {
            return new ResearchBalanceConfig(
                initialFunds,
                initialQuarterlyFunding,
                minQuarterlyFunding,
                maxQuarterlyFunding,
                researchCompletionGain,
                engineNormalResearchCost,
                engineFocusedResearchCost,
                newEnginePresetCost,
                engineInstallCost,
                CreateMissionConfigs(),
                CreateScoreRewardBands(normalResearchStatRewards),
                CreateScoreRewardBands(focusedResearchStatRewards),
                CreateGradeRewards(launchRewards),
                publicSuccessModifier,
                privateSuccessModifier,
                finalMissionSuccessModifier,
                publicRewardMultiplier,
                privateRewardMultiplier,
                finalMissionRewardMultiplier,
                publicFailureQuarterlyFundingDelta,
                privateFailureQuarterlyFundingDelta,
                finalMissionFailureQuarterlyFundingDelta);
        }

        private IReadOnlyList<LaunchMissionConfig> CreateMissionConfigs()
        {
            if (missions == null || missions.Length == 0)
            {
                return ResearchPrototypeModel.CreateDefaultMissionConfigs();
            }

            var configs = new List<LaunchMissionConfig>(missions.Length);
            for (int i = 0; i < missions.Length; i++)
            {
                configs.Add(missions[i].ToRuntimeConfig());
            }

            return configs;
        }

        private static IReadOnlyList<ResearchScoreRewardBand> CreateScoreRewardBands(ScoreRewardBandEntry[] source)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }

            var rewards = new List<ResearchScoreRewardBand>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                rewards.Add(source[i].ToRuntimeConfig());
            }

            return rewards;
        }

        private static IReadOnlyList<ResearchGradeReward> CreateGradeRewards(GradeRewardEntry[] source)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }

            var rewards = new List<ResearchGradeReward>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                rewards.Add(source[i].ToRuntimeConfig());
            }

            return rewards;
        }

        [Serializable]
        private sealed class ScoreRewardBandEntry
        {
            [SerializeField] private int minScore;
            [SerializeField] private int gain;

            public ScoreRewardBandEntry(int minScore, int gain)
            {
                this.minScore = minScore;
                this.gain = gain;
            }

            public ResearchScoreRewardBand ToRuntimeConfig()
            {
                return new ResearchScoreRewardBand(minScore, gain);
            }
        }

        [Serializable]
        private sealed class GradeRewardEntry
        {
            [SerializeField] private ResearchGrade grade;
            [SerializeField] private int immediateFunding;
            [SerializeField] private int quarterlyFundingDelta;

            public GradeRewardEntry(ResearchGrade grade, int immediateFunding, int quarterlyFundingDelta)
            {
                this.grade = grade;
                this.immediateFunding = immediateFunding;
                this.quarterlyFundingDelta = quarterlyFundingDelta;
            }

            public ResearchGradeReward ToRuntimeConfig()
            {
                return new ResearchGradeReward(grade, immediateFunding, quarterlyFundingDelta);
            }
        }

        [Serializable]
        private sealed class MissionBalanceEntry
        {
            [SerializeField] private LaunchMissionId id;
            [SerializeField] private string displayName;
            [SerializeField] private int launchCost;
            [SerializeField] private string requirementText;
            [SerializeField] private float engineWeight;

            public MissionBalanceEntry(LaunchMissionId id, string displayName, int launchCost, string requirementText, float engineWeight)
            {
                this.id = id;
                this.displayName = displayName;
                this.launchCost = launchCost;
                this.requirementText = requirementText;
                this.engineWeight = engineWeight;
            }

            public LaunchMissionConfig ToRuntimeConfig()
            {
                return new LaunchMissionConfig(id, displayName, launchCost, requirementText, engineWeight);
            }
        }
    }
}
