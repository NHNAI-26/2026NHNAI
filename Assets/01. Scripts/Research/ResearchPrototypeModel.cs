using System;
using System.Collections.Generic;
using Border.Core;

namespace Border.Research
{
    public enum ResearchStageId
    {
        Engine,
        Rocket,
        Orbit,
        Moon
    }

    public enum ResearchEnvironmentId
    {
        Stable,
        Ideal,
        HighWind,
        Thunderstorm,
        MeteorShower,
        SolarStorm
    }

    public enum ResearchGrade
    {
        S,
        A,
        B,
        C,
        F
    }

    public enum ResearchActionResult
    {
        Success,
        StageLocked,
        NotEnoughFunds,
        ProgressTooLow,
        DeadlineReached
    }

    [Serializable]
    public sealed class ResearchStageState
    {
        public ResearchStageId Id;
        public int Progress;
        public int AttemptCount;
        public ResearchGrade BestGrade;
        public bool HasBestGrade;
        public bool Unlocked;
    }

    public readonly struct ResearchStageConfig
    {
        public ResearchStageConfig(
            ResearchStageId id,
            string displayName,
            int normalResearchCost,
            int focusedResearchCost,
            int testCost,
            int minimumTestProgress,
            int unlockProgressRequirement)
        {
            Id = id;
            DisplayName = displayName;
            NormalResearchCost = normalResearchCost;
            FocusedResearchCost = focusedResearchCost;
            TestCost = testCost;
            MinimumTestProgress = minimumTestProgress;
            UnlockProgressRequirement = unlockProgressRequirement;
        }

        public ResearchStageId Id { get; }
        public string DisplayName { get; }
        public int NormalResearchCost { get; }
        public int FocusedResearchCost { get; }
        public int TestCost { get; }
        public int MinimumTestProgress { get; }
        public int UnlockProgressRequirement { get; }
    }

    public readonly struct ResearchForecastSlot
    {
        public ResearchForecastSlot(int year, int quarter, ResearchEnvironmentId environmentId, int stageModifier)
        {
            Year = year;
            Quarter = quarter;
            EnvironmentId = environmentId;
            StageModifier = stageModifier;
        }

        public int Year { get; }
        public int Quarter { get; }
        public ResearchEnvironmentId EnvironmentId { get; }
        public int StageModifier { get; }
    }

    public readonly struct ResearchDesignEntryData
    {
        public ResearchDesignEntryData(
            ResearchStageId stageId,
            int year,
            int quarter,
            ResearchEnvironmentId environmentId,
            int mapSeed,
            string targetPathId,
            int currentProgress,
            double prerequisiteAverage,
            int experienceBonus)
        {
            StageId = stageId;
            Year = year;
            Quarter = quarter;
            EnvironmentId = environmentId;
            MapSeed = mapSeed;
            TargetPathId = targetPathId;
            CurrentProgress = currentProgress;
            PrerequisiteAverage = prerequisiteAverage;
            ExperienceBonus = experienceBonus;
        }

        public ResearchStageId StageId { get; }
        public int Year { get; }
        public int Quarter { get; }
        public ResearchEnvironmentId EnvironmentId { get; }
        public int MapSeed { get; }
        public string TargetPathId { get; }
        public int CurrentProgress { get; }
        public double PrerequisiteAverage { get; }
        public int ExperienceBonus { get; }
    }

    public sealed class ResearchPrototypeModel
    {
        public const int StartYear = 2018;
        public const int StartQuarter = 1;
        public const int EndYear = 2026;
        public const int EndQuarter = 4;
        public const int MaxTurns = 36;
        public const int InitialFunds = 2200;
        public const int InitialQuarterlyFunding = 600;
        public const int MinQuarterlyFunding = 300;
        public const int MaxQuarterlyFunding = 1000;
        public const int NormalResearchGain = 6;
        public const int FocusedResearchGain = 10;

        private static readonly ResearchStageConfig[] StageConfigs =
        {
            new(ResearchStageId.Engine, "Engine", 350, 650, 600, 20, 50),
            new(ResearchStageId.Rocket, "Rocket", 450, 800, 900, 20, 55),
            new(ResearchStageId.Orbit, "Orbit", 550, 950, 1200, 20, 60),
            new(ResearchStageId.Moon, "Moon", 650, 1100, 1800, 50, 0),
        };

        private static readonly ResearchEnvironmentId[] EnvironmentIds =
        {
            ResearchEnvironmentId.Stable,
            ResearchEnvironmentId.Ideal,
            ResearchEnvironmentId.HighWind,
            ResearchEnvironmentId.Thunderstorm,
            ResearchEnvironmentId.MeteorShower,
            ResearchEnvironmentId.SolarStorm,
        };

        private static readonly int[] EnvironmentWeights = { 35, 15, 15, 10, 15, 10 };

        private readonly DeterministicRng rng = new();
        private readonly ResearchEnvironmentId[] environmentSchedule = new ResearchEnvironmentId[MaxTurns];

        public ResearchPrototypeModel(int seed = 20260904)
        {
            Seed = seed;
            Stages = new ResearchStageState[StageConfigs.Length];
            Reset();
        }

        public int Seed { get; private set; }
        public int Year { get; private set; }
        public int Quarter { get; private set; }
        public int RemainingTurns { get; private set; }
        public int Funds { get; private set; }
        public int QuarterlyFunding { get; private set; }
        public int CurrentTurnIndex => MaxTurns - RemainingTurns;
        public bool DeadlineReached => RemainingTurns <= 0;
        public string LastMessage { get; private set; }
        public ResearchStageState[] Stages { get; }

        public void Reset()
        {
            rng.Reseed(Seed);
            Year = StartYear;
            Quarter = StartQuarter;
            RemainingTurns = MaxTurns;
            Funds = InitialFunds;
            QuarterlyFunding = InitialQuarterlyFunding;
            LastMessage = "2018 Q1. 연구 판단을 시작합니다.";

            for (int i = 0; i < Stages.Length; i++)
            {
                ResearchStageConfig config = StageConfigs[i];
                Stages[i] = new ResearchStageState
                {
                    Id = config.Id,
                    Progress = 0,
                    AttemptCount = 0,
                    BestGrade = ResearchGrade.F,
                    HasBestGrade = false,
                    Unlocked = config.Id == ResearchStageId.Engine
                };
            }

            GenerateEnvironmentSchedule();
        }

        public static IReadOnlyList<ResearchStageConfig> GetStageConfigs()
        {
            return StageConfigs;
        }

        public static ResearchStageConfig GetStageConfig(ResearchStageId stageId)
        {
            return StageConfigs[(int)stageId];
        }

        public ResearchStageState GetStage(ResearchStageId stageId)
        {
            return Stages[(int)stageId];
        }

        public ResearchActionResult ExecuteResearch(ResearchStageId stageId, bool focused)
        {
            ResearchStageState stage = GetStage(stageId);
            if (!stage.Unlocked)
            {
                LastMessage = $"{GetStageConfig(stageId).DisplayName} 단계는 아직 잠겨 있습니다.";
                return ResearchActionResult.StageLocked;
            }

            ResearchStageConfig config = GetStageConfig(stageId);
            int cost = focused ? config.FocusedResearchCost : config.NormalResearchCost;
            if (Funds < cost)
            {
                LastMessage = $"연구비 부족. 필요 {cost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            Funds -= cost;
            stage.Progress = Math.Min(100, stage.Progress + (focused ? FocusedResearchGain : NormalResearchGain));
            CheckUnlocks();
            AdvanceQuarter();
            LastMessage = $"{config.DisplayName} {(focused ? "집중" : "일반")} 연구 완료.";
            return ResearchActionResult.Success;
        }

        public ResearchActionResult WaitQuarter()
        {
            AdvanceQuarter();
            LastMessage = "한 분기 대기. 발사창을 기다렸습니다.";
            return DeadlineReached ? ResearchActionResult.DeadlineReached : ResearchActionResult.Success;
        }

        public ResearchActionResult TryEnterDesign(ResearchStageId stageId, out ResearchDesignEntryData data)
        {
            data = default;
            ResearchStageState stage = GetStage(stageId);
            ResearchStageConfig config = GetStageConfig(stageId);

            if (!stage.Unlocked)
            {
                LastMessage = $"{config.DisplayName} 단계는 아직 잠겨 있습니다.";
                return ResearchActionResult.StageLocked;
            }

            if (stage.Progress < config.MinimumTestProgress)
            {
                LastMessage = $"{config.DisplayName} 시험 조건 부족. 진행도 {config.MinimumTestProgress} 필요.";
                return ResearchActionResult.ProgressTooLow;
            }

            if (Funds < config.TestCost)
            {
                LastMessage = $"시험비 부족. 필요 {config.TestCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            ResearchEnvironmentId environmentId = GetCurrentEnvironment();
            int mapSeed = CreateDesignMapSeed(stageId);
            data = new ResearchDesignEntryData(
                stageId,
                Year,
                Quarter,
                environmentId,
                mapSeed,
                CreateTargetPathId(stageId, mapSeed),
                stage.Progress,
                GetPrerequisiteAverage(stageId),
                Math.Min(stage.AttemptCount * 3, 9));

            LastMessage = $"{config.DisplayName} 설계 진입 준비 완료. 비용과 시간은 아직 소비하지 않습니다.";
            return ResearchActionResult.Success;
        }

        public ResearchForecastSlot[] GetForecast(ResearchStageId stageId)
        {
            var forecast = new ResearchForecastSlot[4];
            for (int i = 0; i < forecast.Length; i++)
            {
                int turnIndex = CurrentTurnIndex + i;
                int year;
                int quarter;
                if (turnIndex >= MaxTurns)
                {
                    year = EndYear;
                    quarter = EndQuarter;
                    forecast[i] = new ResearchForecastSlot(year, quarter, ResearchEnvironmentId.Stable, 0);
                    continue;
                }

                GetDateForTurn(turnIndex, out year, out quarter);
                ResearchEnvironmentId environmentId = environmentSchedule[turnIndex];
                forecast[i] = new ResearchForecastSlot(year, quarter, environmentId, GetEnvironmentModifier(environmentId, stageId));
            }

            return forecast;
        }

        public int CalculateSuccessChance(ResearchStageId stageId)
        {
            ResearchStageState stage = GetStage(stageId);
            int experience = Math.Min(stage.AttemptCount * 3, 9);
            int environment = GetEnvironmentModifier(GetCurrentEnvironment(), stageId);
            double raw;
            if (stageId == ResearchStageId.Engine)
            {
                raw = 20 + stage.Progress * 0.8d + experience + environment;
            }
            else
            {
                raw = 20 + stage.Progress * 0.6d + GetPrerequisiteAverage(stageId) * 0.2d + experience + environment;
            }

            return Math.Max(10, Math.Min(90, (int)Math.Round(raw)));
        }

        public bool CanUnlockNext(ResearchStageId stageId)
        {
            if (stageId == ResearchStageId.Moon)
            {
                return false;
            }

            ResearchStageState stage = GetStage(stageId);
            ResearchStageConfig config = GetStageConfig(stageId);
            return stage.Progress >= config.UnlockProgressRequirement
                && stage.HasBestGrade
                && stage.BestGrade <= ResearchGrade.C;
        }

        public string GetEnvironmentDisplayName(ResearchEnvironmentId environmentId)
        {
            switch (environmentId)
            {
                case ResearchEnvironmentId.Stable:
                    return "안정";
                case ResearchEnvironmentId.Ideal:
                    return "최적 발사창";
                case ResearchEnvironmentId.HighWind:
                    return "강풍";
                case ResearchEnvironmentId.Thunderstorm:
                    return "뇌우";
                case ResearchEnvironmentId.MeteorShower:
                    return "유성우";
                case ResearchEnvironmentId.SolarStorm:
                    return "태양 폭풍";
                default:
                    return environmentId.ToString();
            }
        }

        private void CheckUnlocks()
        {
            UnlockIfReady(ResearchStageId.Engine, ResearchStageId.Rocket);
            UnlockIfReady(ResearchStageId.Rocket, ResearchStageId.Orbit);
            UnlockIfReady(ResearchStageId.Orbit, ResearchStageId.Moon);
        }

        private void UnlockIfReady(ResearchStageId current, ResearchStageId next)
        {
            if (CanUnlockNext(current))
            {
                GetStage(next).Unlocked = true;
            }
        }

        private void AdvanceQuarter()
        {
            if (RemainingTurns <= 0)
            {
                return;
            }

            RemainingTurns--;
            Funds += QuarterlyFunding;

            if (Quarter == 4)
            {
                Year++;
                Quarter = 1;
            }
            else
            {
                Quarter++;
            }
        }

        private ResearchEnvironmentId GetCurrentEnvironment()
        {
            int index = Math.Min(CurrentTurnIndex, MaxTurns - 1);
            return environmentSchedule[index];
        }

        private double GetPrerequisiteAverage(ResearchStageId stageId)
        {
            int count = (int)stageId;
            if (count <= 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += Stages[i].Progress;
            }

            return total / (double)count;
        }

        private void GenerateEnvironmentSchedule()
        {
            for (int i = 0; i < environmentSchedule.Length; i++)
            {
                ResearchEnvironmentId candidate = PickWeightedEnvironment();
                if (IsThirdSameRiskInARow(i, candidate) || LastThreeWereRisk(i))
                {
                    candidate = rng.Next(0, 2) == 0 ? ResearchEnvironmentId.Stable : ResearchEnvironmentId.Ideal;
                }

                environmentSchedule[i] = candidate;
            }
        }

        private ResearchEnvironmentId PickWeightedEnvironment()
        {
            int roll = rng.Next(0, 100);
            int cursor = 0;
            for (int i = 0; i < EnvironmentWeights.Length; i++)
            {
                cursor += EnvironmentWeights[i];
                if (roll < cursor)
                {
                    return EnvironmentIds[i];
                }
            }

            return ResearchEnvironmentId.Stable;
        }

        private bool IsThirdSameRiskInARow(int index, ResearchEnvironmentId candidate)
        {
            return index >= 2
                && IsRisk(candidate)
                && environmentSchedule[index - 1] == candidate
                && environmentSchedule[index - 2] == candidate;
        }

        private bool LastThreeWereRisk(int index)
        {
            return index >= 3
                && IsRisk(environmentSchedule[index - 1])
                && IsRisk(environmentSchedule[index - 2])
                && IsRisk(environmentSchedule[index - 3]);
        }

        private static bool IsRisk(ResearchEnvironmentId environmentId)
        {
            return environmentId == ResearchEnvironmentId.HighWind
                || environmentId == ResearchEnvironmentId.Thunderstorm
                || environmentId == ResearchEnvironmentId.MeteorShower
                || environmentId == ResearchEnvironmentId.SolarStorm;
        }

        private static int GetEnvironmentModifier(ResearchEnvironmentId environmentId, ResearchStageId stageId)
        {
            switch (environmentId)
            {
                case ResearchEnvironmentId.Ideal:
                    return 10;
                case ResearchEnvironmentId.HighWind:
                    return stageId == ResearchStageId.Engine ? -5 : stageId == ResearchStageId.Rocket ? -20 : 0;
                case ResearchEnvironmentId.Thunderstorm:
                    return stageId == ResearchStageId.Engine ? -15 : stageId == ResearchStageId.Rocket ? -25 : 0;
                case ResearchEnvironmentId.MeteorShower:
                    return stageId == ResearchStageId.Rocket ? -5 : stageId == ResearchStageId.Orbit ? -25 : stageId == ResearchStageId.Moon ? -20 : 0;
                case ResearchEnvironmentId.SolarStorm:
                    return stageId == ResearchStageId.Orbit ? -20 : stageId == ResearchStageId.Moon ? -25 : 0;
                default:
                    return 0;
            }
        }

        private int CreateDesignMapSeed(ResearchStageId stageId)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Seed;
                hash = hash * 31 + Year;
                hash = hash * 31 + Quarter;
                hash = hash * 31 + (int)stageId;
                return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
            }
        }

        private static string CreateTargetPathId(ResearchStageId stageId, int mapSeed)
        {
            int pathIndex = mapSeed % 3 + 1;
            return $"{stageId}_Path_{pathIndex}";
        }

        private static void GetDateForTurn(int turnIndex, out int year, out int quarter)
        {
            year = StartYear + turnIndex / 4;
            quarter = StartQuarter + turnIndex % 4;
        }
    }
}
