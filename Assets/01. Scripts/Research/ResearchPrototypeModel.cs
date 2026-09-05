using System;
using System.Collections.Generic;

namespace Border.Research
{
    public enum LaunchMissionId
    {
        StaticFire,
        LowAltitude,
        HighAltitude,
        TargetZone,
        ZoneHold,
        LowPowerZoneHold
    }

    public enum EnginePresetId
    {
        Engine01,
        Engine02,
        Engine03,
        Engine04,
        Engine05,
        Engine06,
        Engine07,
        Engine08,
        Engine09,
        Engine10
    }

    public enum EngineStatId
    {
        FuelCapacity,
        Cooling,
        MaxOutput,
        IgnitionReliability
    }

    public enum TestVisibility
    {
        Public,
        Private,
        FinalMission
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
        MissionLocked,
        NotEnoughFunds,
        RequirementNotMet,
        DeadlineReached,
        NoPendingDesignEntry,
        EngineCompletionMaxed,
        EnginePresetLocked,
        EnginePresetLimitReached
    }

    public enum EngineRiskKind
    {
        OutputShortage,
        FuelExhaustion,
        CoolingShortage,
        IgnitionInstability
    }

    [Serializable]
    public sealed class LaunchMissionState
    {
        public LaunchMissionId Id;
        public int AttemptCount;
        public ResearchGrade BestGrade;
        public bool HasBestGrade;
        public bool Unlocked;
    }

    [Serializable]
    public sealed class EnginePresetState
    {
        public EnginePresetId PresetId;
        public int Completion;
        public int FuelCapacity;
        public int Cooling;
        public int MaxOutput;
        public int IgnitionReliability;
        public int AttemptCount;
        public ResearchGrade BestGrade;
        public bool HasBestGrade;
        public bool Unlocked;

        public int GetStat(EngineStatId statId)
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    return FuelCapacity;
                case EngineStatId.Cooling:
                    return Cooling;
                case EngineStatId.MaxOutput:
                    return MaxOutput;
                case EngineStatId.IgnitionReliability:
                    return IgnitionReliability;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statId), statId, null);
            }
        }

        public void SetStat(EngineStatId statId, int value)
        {
            int clamped = ResearchPrototypeModel.ClampInt(value, 0, 100);
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    FuelCapacity = clamped;
                    break;
                case EngineStatId.Cooling:
                    Cooling = clamped;
                    break;
                case EngineStatId.MaxOutput:
                    MaxOutput = clamped;
                    break;
                case EngineStatId.IgnitionReliability:
                    IgnitionReliability = clamped;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statId), statId, null);
            }
        }
    }

    public readonly struct EnginePresetConfig
    {
        public EnginePresetConfig(EnginePresetId id, string displayName)
            : this(
                id,
                displayName,
                ResearchPrototypeModel.EngineNormalResearchCost,
                ResearchPrototypeModel.EngineFocusedResearchCost,
                ResearchPrototypeModel.EngineInstallCost,
                ResearchPrototypeModel.InitialEngineStat,
                ResearchPrototypeModel.InitialEngineStat,
                ResearchPrototypeModel.InitialEngineStat,
                ResearchPrototypeModel.InitialEngineStat)
        {
        }

        public EnginePresetConfig(
            EnginePresetId id,
            string displayName,
            int normalResearchCost,
            int focusedResearchCost,
            int installCost,
            int initialFuelCapacity,
            int initialCooling,
            int initialMaxOutput,
            int initialIgnitionReliability)
        {
            Id = id;
            DisplayName = displayName;
            NormalResearchCost = normalResearchCost;
            FocusedResearchCost = focusedResearchCost;
            InstallCost = installCost;
            InitialFuelCapacity = initialFuelCapacity;
            InitialCooling = initialCooling;
            InitialMaxOutput = initialMaxOutput;
            InitialIgnitionReliability = initialIgnitionReliability;
        }

        public EnginePresetId Id { get; }
        public string DisplayName { get; }
        public int NormalResearchCost { get; }
        public int FocusedResearchCost { get; }
        public int InstallCost { get; }
        public int InitialFuelCapacity { get; }
        public int InitialCooling { get; }
        public int InitialMaxOutput { get; }
        public int InitialIgnitionReliability { get; }
    }

    public readonly struct LaunchMissionConfig
    {
        public LaunchMissionConfig(LaunchMissionId id, string displayName, int launchCost, string requirementText, double engineWeight)
        {
            Id = id;
            DisplayName = displayName;
            LaunchCost = launchCost;
            TestCost = launchCost;
            RequirementText = requirementText;
            EngineWeight = engineWeight;
        }

        public LaunchMissionId Id { get; }
        public string DisplayName { get; }
        public int LaunchCost { get; }
        public int TestCost { get; }
        public string RequirementText { get; }
        public double EngineWeight { get; }
    }

    public readonly struct EngineRiskInfo
    {
        public EngineRiskInfo(EngineRiskKind kind, int severity, string displayName, string description)
        {
            Kind = kind;
            Severity = severity;
            DisplayName = displayName;
            Description = description;
        }

        public EngineRiskKind Kind { get; }
        public int Severity { get; }
        public string DisplayName { get; }
        public string Description { get; }
    }

    public readonly struct ResearchScoreRewardBand
    {
        public ResearchScoreRewardBand(int minScore, int gain)
        {
            MinScore = minScore;
            Gain = gain;
        }

        public int MinScore { get; }
        public int Gain { get; }
    }

    public readonly struct ResearchGradeReward
    {
        public ResearchGradeReward(ResearchGrade grade, int immediateFunding, int quarterlyFundingDelta)
        {
            Grade = grade;
            ImmediateFunding = immediateFunding;
            QuarterlyFundingDelta = quarterlyFundingDelta;
        }

        public ResearchGrade Grade { get; }
        public int ImmediateFunding { get; }
        public int QuarterlyFundingDelta { get; }
    }

    public sealed class ResearchBalanceConfig
    {
        public ResearchBalanceConfig(
            int initialFunds,
            int initialQuarterlyFunding,
            int minQuarterlyFunding,
            int maxQuarterlyFunding,
            int researchCompletionGain,
            int engineNormalResearchCost,
            int engineFocusedResearchCost,
            int newEnginePresetCost,
            int engineInstallCost,
            IReadOnlyList<LaunchMissionConfig> missionConfigs,
            IReadOnlyList<ResearchScoreRewardBand> normalResearchStatRewards = null,
            IReadOnlyList<ResearchScoreRewardBand> focusedResearchStatRewards = null,
            IReadOnlyList<ResearchGradeReward> launchRewards = null,
            int publicSuccessModifier = -10,
            int privateSuccessModifier = 10,
            int finalMissionSuccessModifier = 0,
            double publicRewardMultiplier = 1.5d,
            double privateRewardMultiplier = 0.5d,
            double finalMissionRewardMultiplier = 1d,
            int publicFailureQuarterlyFundingDelta = -150,
            int privateFailureQuarterlyFundingDelta = -50,
            int finalMissionFailureQuarterlyFundingDelta = -100)
        {
            InitialFunds = initialFunds;
            InitialQuarterlyFunding = initialQuarterlyFunding;
            MinQuarterlyFunding = minQuarterlyFunding;
            MaxQuarterlyFunding = maxQuarterlyFunding;
            ResearchCompletionGain = researchCompletionGain;
            EngineNormalResearchCost = engineNormalResearchCost;
            EngineFocusedResearchCost = engineFocusedResearchCost;
            NewEnginePresetCost = newEnginePresetCost;
            EngineInstallCost = engineInstallCost;
            MissionConfigs = CopyMissionConfigs(missionConfigs);
            NormalResearchStatRewards = CopyScoreRewardBands(normalResearchStatRewards, CreateDefaultNormalResearchStatRewards());
            FocusedResearchStatRewards = CopyScoreRewardBands(focusedResearchStatRewards, CreateDefaultFocusedResearchStatRewards());
            LaunchRewards = CopyGradeRewards(launchRewards, CreateDefaultLaunchRewards());
            PublicSuccessModifier = publicSuccessModifier;
            PrivateSuccessModifier = privateSuccessModifier;
            FinalMissionSuccessModifier = finalMissionSuccessModifier;
            PublicRewardMultiplier = publicRewardMultiplier;
            PrivateRewardMultiplier = privateRewardMultiplier;
            FinalMissionRewardMultiplier = finalMissionRewardMultiplier;
            PublicFailureQuarterlyFundingDelta = publicFailureQuarterlyFundingDelta;
            PrivateFailureQuarterlyFundingDelta = privateFailureQuarterlyFundingDelta;
            FinalMissionFailureQuarterlyFundingDelta = finalMissionFailureQuarterlyFundingDelta;
        }

        public int InitialFunds { get; }
        public int InitialQuarterlyFunding { get; }
        public int MinQuarterlyFunding { get; }
        public int MaxQuarterlyFunding { get; }
        public int ResearchCompletionGain { get; }
        public int EngineNormalResearchCost { get; }
        public int EngineFocusedResearchCost { get; }
        public int NewEnginePresetCost { get; }
        public int EngineInstallCost { get; }
        public LaunchMissionConfig[] MissionConfigs { get; }
        public ResearchScoreRewardBand[] NormalResearchStatRewards { get; }
        public ResearchScoreRewardBand[] FocusedResearchStatRewards { get; }
        public ResearchGradeReward[] LaunchRewards { get; }
        public int PublicSuccessModifier { get; }
        public int PrivateSuccessModifier { get; }
        public int FinalMissionSuccessModifier { get; }
        public double PublicRewardMultiplier { get; }
        public double PrivateRewardMultiplier { get; }
        public double FinalMissionRewardMultiplier { get; }
        public int PublicFailureQuarterlyFundingDelta { get; }
        public int PrivateFailureQuarterlyFundingDelta { get; }
        public int FinalMissionFailureQuarterlyFundingDelta { get; }

        public static ResearchBalanceConfig CreateDefault()
        {
            return new ResearchBalanceConfig(
                ResearchPrototypeModel.InitialFunds,
                ResearchPrototypeModel.InitialQuarterlyFunding,
                ResearchPrototypeModel.MinQuarterlyFunding,
                ResearchPrototypeModel.MaxQuarterlyFunding,
                ResearchPrototypeModel.ResearchCompletionGain,
                ResearchPrototypeModel.EngineNormalResearchCost,
                ResearchPrototypeModel.EngineFocusedResearchCost,
                ResearchPrototypeModel.NewEnginePresetCost,
                ResearchPrototypeModel.EngineInstallCost,
                ResearchPrototypeModel.CreateDefaultMissionConfigs());
        }

        public int GetResearchStatGain(bool focused, int score)
        {
            ResearchScoreRewardBand[] bands = focused ? FocusedResearchStatRewards : NormalResearchStatRewards;
            int clampedScore = ResearchPrototypeModel.ClampInt(score, 0, 100);
            int gain = bands.Length > 0 ? bands[0].Gain : 0;
            for (int i = 0; i < bands.Length; i++)
            {
                if (clampedScore >= bands[i].MinScore)
                {
                    gain = bands[i].Gain;
                }
            }

            return gain;
        }

        public ResearchGradeReward GetLaunchReward(ResearchGrade grade)
        {
            for (int i = 0; i < LaunchRewards.Length; i++)
            {
                if (LaunchRewards[i].Grade == grade)
                {
                    return LaunchRewards[i];
                }
            }

            return new ResearchGradeReward(grade, 0, -100);
        }

        public int GetVisibilitySuccessModifier(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return PublicSuccessModifier;
                case TestVisibility.Private:
                    return PrivateSuccessModifier;
                case TestVisibility.FinalMission:
                    return FinalMissionSuccessModifier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        public double GetRewardMultiplier(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return PublicRewardMultiplier;
                case TestVisibility.Private:
                    return PrivateRewardMultiplier;
                case TestVisibility.FinalMission:
                    return FinalMissionRewardMultiplier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        public int GetFailureQuarterlyFundingDelta(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return PublicFailureQuarterlyFundingDelta;
                case TestVisibility.Private:
                    return PrivateFailureQuarterlyFundingDelta;
                case TestVisibility.FinalMission:
                    return FinalMissionFailureQuarterlyFundingDelta;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        private static LaunchMissionConfig[] CopyMissionConfigs(IReadOnlyList<LaunchMissionConfig> source)
        {
            LaunchMissionConfig[] defaults = ResearchPrototypeModel.CreateDefaultMissionConfigs();
            if (source == null || source.Count == 0)
            {
                return defaults;
            }

            var copy = new LaunchMissionConfig[defaults.Length];
            Array.Copy(defaults, copy, defaults.Length);
            int length = Math.Min(copy.Length, source.Count);
            for (int i = 0; i < length; i++)
            {
                copy[(int)source[i].Id] = source[i];
            }

            return copy;
        }

        private static ResearchScoreRewardBand[] CreateDefaultNormalResearchStatRewards()
        {
            return new[]
            {
                new ResearchScoreRewardBand(0, 10),
                new ResearchScoreRewardBand(50, 13),
                new ResearchScoreRewardBand(80, 16),
            };
        }

        private static ResearchScoreRewardBand[] CreateDefaultFocusedResearchStatRewards()
        {
            return new[]
            {
                new ResearchScoreRewardBand(0, 16),
                new ResearchScoreRewardBand(50, 21),
                new ResearchScoreRewardBand(80, 26),
            };
        }

        private static ResearchGradeReward[] CreateDefaultLaunchRewards()
        {
            return new[]
            {
                new ResearchGradeReward(ResearchGrade.S, 900, 150),
                new ResearchGradeReward(ResearchGrade.A, 600, 100),
                new ResearchGradeReward(ResearchGrade.B, 400, 50),
                new ResearchGradeReward(ResearchGrade.C, 150, 0),
                new ResearchGradeReward(ResearchGrade.F, 0, -100),
            };
        }

        private static ResearchScoreRewardBand[] CopyScoreRewardBands(IReadOnlyList<ResearchScoreRewardBand> source, ResearchScoreRewardBand[] defaults)
        {
            if (source == null || source.Count == 0)
            {
                return defaults;
            }

            var copy = new ResearchScoreRewardBand[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            Array.Sort(copy, (a, b) => a.MinScore.CompareTo(b.MinScore));
            return copy;
        }

        private static ResearchGradeReward[] CopyGradeRewards(IReadOnlyList<ResearchGradeReward> source, ResearchGradeReward[] defaults)
        {
            var copy = new ResearchGradeReward[defaults.Length];
            Array.Copy(defaults, copy, defaults.Length);
            if (source == null || source.Count == 0)
            {
                return copy;
            }

            for (int i = 0; i < source.Count; i++)
            {
                int index = Array.FindIndex(copy, reward => reward.Grade == source[i].Grade);
                if (index >= 0)
                {
                    copy[index] = source[i];
                }
            }

            return copy;
        }
    }

    public readonly struct ResearchDesignEntryData
    {
        public ResearchDesignEntryData(
            LaunchMissionId missionId,
            EnginePresetId selectedEnginePresetId,
            int year,
            int quarter,
            int mapSeed,
            string targetPathId,
            int selectedEngineCompletion,
            int selectedEngineScore,
            int installedEngineScore,
            int[] installedEngineCounts,
            int reservedInstallCost,
            int launchCost,
            int designFit,
            TestVisibility visibility,
            int previousCertificationBonus,
            int experienceBonus,
            bool launchCostPaid)
        {
            MissionId = missionId;
            SelectedEnginePresetId = selectedEnginePresetId;
            Year = year;
            Quarter = quarter;
            MapSeed = mapSeed;
            TargetPathId = targetPathId;
            SelectedEngineCompletion = selectedEngineCompletion;
            SelectedEngineScore = selectedEngineScore;
            InstalledEngineScore = installedEngineScore;
            InstalledEngineCounts = CopyEngineCounts(installedEngineCounts);
            ReservedInstallCost = reservedInstallCost;
            LaunchCost = launchCost;
            DesignFit = designFit;
            Visibility = visibility;
            PreviousCertificationBonus = previousCertificationBonus;
            ExperienceBonus = experienceBonus;
            LaunchCostPaid = launchCostPaid;
        }

        public LaunchMissionId MissionId { get; }
        public EnginePresetId SelectedEnginePresetId { get; }
        public int Year { get; }
        public int Quarter { get; }
        public int MapSeed { get; }
        public string TargetPathId { get; }
        public int SelectedEngineCompletion { get; }
        public int SelectedEngineScore { get; }
        public int InstalledEngineScore { get; }
        public int[] InstalledEngineCounts { get; }
        public int ReservedInstallCost { get; }
        public int LaunchCost { get; }
        public int DesignFit { get; }
        public TestVisibility Visibility { get; }
        public int PreviousCertificationBonus { get; }
        public int ExperienceBonus { get; }
        public bool LaunchCostPaid { get; }

        public int InstalledEngineCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < InstalledEngineCounts.Length; i++)
                {
                    total += InstalledEngineCounts[i];
                }

                return total;
            }
        }

        private static int[] CopyEngineCounts(int[] source)
        {
            var copy = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            if (source == null)
            {
                return copy;
            }

            int length = Math.Min(copy.Length, source.Length);
            for (int i = 0; i < length; i++)
            {
                copy[i] = Math.Max(0, source[i]);
            }

            return copy;
        }
    }

    public readonly struct ResearchLaunchResultData
    {
        public ResearchLaunchResultData(
            LaunchMissionId missionId,
            EnginePresetId selectedEnginePresetId,
            int year,
            int quarter,
            int launchCost,
            int reservedInstallCost,
            TestVisibility visibility,
            int designFit,
            int selectedEngineScore,
            int installedEngineScore,
            int successChance,
            int partialChance,
            int failureChance,
            int roll,
            ResearchGrade grade,
            int immediateFunding,
            int quarterlyFundingDelta,
            bool finalMissionWon,
            bool deadlineMissed)
        {
            MissionId = missionId;
            SelectedEnginePresetId = selectedEnginePresetId;
            Year = year;
            Quarter = quarter;
            LaunchCost = launchCost;
            ReservedInstallCost = reservedInstallCost;
            Visibility = visibility;
            DesignFit = designFit;
            SelectedEngineScore = selectedEngineScore;
            InstalledEngineScore = installedEngineScore;
            SuccessChance = successChance;
            PartialChance = partialChance;
            FailureChance = failureChance;
            Roll = roll;
            Grade = grade;
            ImmediateFunding = immediateFunding;
            QuarterlyFundingDelta = quarterlyFundingDelta;
            FinalMissionWon = finalMissionWon;
            DeadlineMissed = deadlineMissed;
        }

        public LaunchMissionId MissionId { get; }
        public EnginePresetId SelectedEnginePresetId { get; }
        public int Year { get; }
        public int Quarter { get; }
        public int LaunchCost { get; }
        public int ReservedInstallCost { get; }
        public int TotalCost => LaunchCost + ReservedInstallCost;
        public TestVisibility Visibility { get; }
        public int DesignFit { get; }
        public int SelectedEngineScore { get; }
        public int InstalledEngineScore { get; }
        public int SuccessChance { get; }
        public int PartialChance { get; }
        public int FailureChance { get; }
        public int Roll { get; }
        public ResearchGrade Grade { get; }
        public int ImmediateFunding { get; }
        public int QuarterlyFundingDelta { get; }
        public bool FinalMissionWon { get; }
        public bool DeadlineMissed { get; }
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
        public const int ResearchCompletionGain = 10;
        public const int NormalResearchGain = ResearchCompletionGain;
        public const int FocusedResearchGain = ResearchCompletionGain;
        public const int MaxEngineCompletion = 100;
        public const int MaxEnginePresetCount = 10;
        public const int InitialEngineStat = 40;
        public const int EngineNormalResearchCost = 350;
        public const int EngineFocusedResearchCost = 650;
        public const int NewEnginePresetCost = 150;
        public const int EngineInstallCost = 350;
        public const int MinDesignFit = 0;
        public const int MaxDesignFit = 100;

        private static readonly EnginePresetConfig[] DefaultEnginePresetConfigs =
        {
            new(EnginePresetId.Engine01, "엔진 01"),
            new(EnginePresetId.Engine02, "엔진 02"),
            new(EnginePresetId.Engine03, "엔진 03"),
            new(EnginePresetId.Engine04, "엔진 04"),
            new(EnginePresetId.Engine05, "엔진 05"),
            new(EnginePresetId.Engine06, "엔진 06"),
            new(EnginePresetId.Engine07, "엔진 07"),
            new(EnginePresetId.Engine08, "엔진 08"),
            new(EnginePresetId.Engine09, "엔진 09"),
            new(EnginePresetId.Engine10, "엔진 10"),
        };

        private readonly ResearchBalanceConfig balanceConfig;
        private readonly LaunchMissionConfig[] missionConfigs;

        public ResearchPrototypeModel(int seed = 20260904, ResearchBalanceConfig balanceConfig = null)
        {
            Seed = seed;
            this.balanceConfig = balanceConfig ?? ResearchBalanceConfig.CreateDefault();
            missionConfigs = this.balanceConfig.MissionConfigs;
            Missions = new LaunchMissionState[missionConfigs.Length];
            EnginePresets = new EnginePresetState[DefaultEnginePresetConfigs.Length];
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
        public bool HasGameEnded { get; private set; }
        public bool GameWon { get; private set; }
        public int TotalLaunches { get; private set; }
        public int FailedLaunches { get; private set; }
        public int HighestQuarterlyFunding { get; private set; }
        public int TotalSpentFunds { get; private set; }
        public int ConfiguredResearchCompletionGain => balanceConfig.ResearchCompletionGain;
        public int ConfiguredEngineNormalResearchCost => balanceConfig.EngineNormalResearchCost;
        public int ConfiguredEngineFocusedResearchCost => balanceConfig.EngineFocusedResearchCost;
        public int ConfiguredNewEnginePresetCost => balanceConfig.NewEnginePresetCost;
        public int ConfiguredEngineInstallCost => balanceConfig.EngineInstallCost;
        public string LastMessage { get; private set; }
        public LaunchMissionState[] Missions { get; }
        public EnginePresetState[] EnginePresets { get; }
        public int ActiveEnginePresetCount { get; private set; }

        public void Reset()
        {
            Year = StartYear;
            Quarter = StartQuarter;
            RemainingTurns = MaxTurns;
            Funds = balanceConfig.InitialFunds;
            QuarterlyFunding = balanceConfig.InitialQuarterlyFunding;
            HighestQuarterlyFunding = QuarterlyFunding;
            TotalLaunches = 0;
            FailedLaunches = 0;
            TotalSpentFunds = 0;
            HasGameEnded = false;
            GameWon = false;
            ActiveEnginePresetCount = 1;
            LastMessage = "2018 Q1. 첫 엔진 프리셋 연구 판단을 시작합니다.";

            for (int i = 0; i < Missions.Length; i++)
            {
                LaunchMissionConfig config = missionConfigs[i];
                Missions[i] = new LaunchMissionState
                {
                    Id = config.Id,
                    AttemptCount = 0,
                    BestGrade = ResearchGrade.F,
                    HasBestGrade = false,
                    Unlocked = config.Id == LaunchMissionId.StaticFire
                };
            }

            for (int i = 0; i < EnginePresets.Length; i++)
            {
                EnginePresetConfig config = DefaultEnginePresetConfigs[i];
                EnginePresets[i] = new EnginePresetState
                {
                    PresetId = config.Id,
                    Completion = 0,
                    FuelCapacity = config.InitialFuelCapacity,
                    Cooling = config.InitialCooling,
                    MaxOutput = config.InitialMaxOutput,
                    IgnitionReliability = config.InitialIgnitionReliability,
                    AttemptCount = 0,
                    BestGrade = ResearchGrade.F,
                    HasBestGrade = false,
                    Unlocked = i == 0
                };
            }
        }

        public static LaunchMissionConfig[] CreateDefaultMissionConfigs()
        {
            return new[]
            {
                new LaunchMissionConfig(LaunchMissionId.StaticFire, "정적 연소 시험", 600, "기본 해금", 0d),
                new LaunchMissionConfig(LaunchMissionId.LowAltitude, "낮은 고도 도달", 800, "정적 연소 시험 C 이상", 0.55d),
                new LaunchMissionConfig(LaunchMissionId.HighAltitude, "높은 고도 도달", 900, "낮은 고도 도달 C 이상", 0.50d),
                new LaunchMissionConfig(LaunchMissionId.TargetZone, "목표 구역 도달", 1100, "높은 고도 도달 C 이상", 0.45d),
                new LaunchMissionConfig(LaunchMissionId.ZoneHold, "목표 구역 체류", 1300, "목표 구역 도달 C 이상", 0.42d),
                new LaunchMissionConfig(LaunchMissionId.LowPowerZoneHold, "저전력 검증", 1500, "목표 구역 체류 C 이상", 0.40d),
            };
        }

        public static IReadOnlyList<LaunchMissionConfig> GetMissionConfigs()
        {
            return ResearchBalanceConfig.CreateDefault().MissionConfigs;
        }

        public static IReadOnlyList<EnginePresetConfig> GetEnginePresetConfigs()
        {
            return DefaultEnginePresetConfigs;
        }

        public static LaunchMissionConfig GetMissionConfig(LaunchMissionId missionId)
        {
            return CreateDefaultMissionConfigs()[(int)missionId];
        }

        public LaunchMissionConfig GetConfiguredMissionConfig(LaunchMissionId missionId)
        {
            return missionConfigs[(int)missionId];
        }

        public static EnginePresetConfig GetEnginePresetConfig(EnginePresetId presetId)
        {
            return DefaultEnginePresetConfigs[(int)presetId];
        }

        public LaunchMissionState GetMission(LaunchMissionId missionId)
        {
            return Missions[(int)missionId];
        }

        public LaunchMissionId GetCurrentMission()
        {
            for (int i = Missions.Length - 1; i >= 0; i--)
            {
                if (Missions[i].Unlocked)
                {
                    return Missions[i].Id;
                }
            }

            return LaunchMissionId.StaticFire;
        }

        public EnginePresetState GetEnginePreset(EnginePresetId presetId)
        {
            return EnginePresets[(int)presetId];
        }

        public bool IsEnginePresetUnlocked(EnginePresetId presetId)
        {
            int index = (int)presetId;
            return index >= 0
                && index < EnginePresets.Length
                && index < ActiveEnginePresetCount
                && EnginePresets[index].Unlocked;
        }

        public ResearchActionResult CreateNewEnginePreset(out EnginePresetId presetId)
        {
            presetId = default;
            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 새 엔진을 개발할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            if (ActiveEnginePresetCount >= MaxEnginePresetCount)
            {
                presetId = EnginePresetId.Engine10;
                LastMessage = "엔진 프리셋은 최대 10개입니다.";
                return ResearchActionResult.EnginePresetLimitReached;
            }

            if (Funds < balanceConfig.NewEnginePresetCost)
            {
                LastMessage = $"예산 부족. 필요 {balanceConfig.NewEnginePresetCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            Funds -= balanceConfig.NewEnginePresetCost;
            TotalSpentFunds += balanceConfig.NewEnginePresetCost;
            int index = ActiveEnginePresetCount;
            EnginePresets[index].Unlocked = true;
            ActiveEnginePresetCount++;
            presetId = EnginePresets[index].PresetId;
            LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName} 개발 슬롯이 열렸습니다. 비용 {balanceConfig.NewEnginePresetCost}, 시간 소모 없음.";
            return ResearchActionResult.Success;
        }

#if UNITY_EDITOR
        public void PrepareDebugDesignEntryState(LaunchMissionId missionId, EnginePresetId presetId = EnginePresetId.Engine01)
        {
            while (!IsEnginePresetUnlocked(presetId) && ActiveEnginePresetCount < MaxEnginePresetCount)
            {
                Funds = Math.Max(Funds, balanceConfig.NewEnginePresetCost);
                CreateNewEnginePreset(out _);
            }

            EnginePresetState preset = GetEnginePreset(presetId);
            preset.Completion = Math.Max(preset.Completion, 30);
            preset.FuelCapacity = Math.Max(preset.FuelCapacity, 65);
            preset.Cooling = Math.Max(preset.Cooling, 65);
            preset.MaxOutput = Math.Max(preset.MaxOutput, 65);
            preset.IgnitionReliability = Math.Max(preset.IgnitionReliability, 65);

            for (int i = 0; i <= (int)missionId; i++)
            {
                Missions[i].Unlocked = true;
            }

            LaunchMissionConfig config = GetConfiguredMissionConfig(missionId);
            Funds = Math.Max(Funds, config.LaunchCost + balanceConfig.EngineInstallCost * 2);
        }
#endif

        public ResearchActionResult ExecuteEngineResearch(EnginePresetId presetId, EngineStatId statId, bool focused, int score)
        {
            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 연구할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            EnginePresetState preset = GetEnginePreset(presetId);
            if (!IsEnginePresetUnlocked(presetId))
            {
                LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName}은 아직 개발되지 않았습니다.";
                return ResearchActionResult.EnginePresetLocked;
            }

            if (preset.Completion >= MaxEngineCompletion)
            {
                LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName}은 완성도 100입니다. 더 이상 연구할 수 없습니다.";
                return ResearchActionResult.EngineCompletionMaxed;
            }

            EnginePresetConfig config = GetEnginePresetConfig(presetId);
            int cost = focused ? balanceConfig.EngineFocusedResearchCost : balanceConfig.EngineNormalResearchCost;
            if (Funds < cost)
            {
                LastMessage = $"예산 부족. 필요 {cost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            int oldStat = preset.GetStat(statId);
            int oldCompletion = preset.Completion;
            int statGain = CalculateResearchStatGain(focused, score);

            Funds -= cost;
            TotalSpentFunds += cost;
            preset.SetStat(statId, oldStat + statGain);
            preset.Completion = Math.Min(MaxEngineCompletion, preset.Completion + balanceConfig.ResearchCompletionGain);
            AdvanceQuarter();

            LastMessage = $"{config.DisplayName} {GetStatDisplayName(statId)} {(focused ? "집중" : "일반")} 연구 완료. "
                + $"점수 {ClampInt(score, 0, 100)}, 스탯 {oldStat}->{preset.GetStat(statId)}, 완성도 {oldCompletion}->{preset.Completion}.";
            return ResearchActionResult.Success;
        }

        public ResearchActionResult WaitQuarter()
        {
            AdvanceQuarter();
            LastMessage = "한 분기 대기. 정기 예산을 받았습니다.";
            return DeadlineReached ? ResearchActionResult.DeadlineReached : ResearchActionResult.Success;
        }

        public ResearchActionResult TryEnterDesign(LaunchMissionId missionId, out ResearchDesignEntryData data)
        {
            return TryEnterDesign(missionId, EnginePresetId.Engine01, out data);
        }

        public ResearchActionResult TryEnterDesign(LaunchMissionId missionId, EnginePresetId presetId, out ResearchDesignEntryData data)
        {
            data = default;
            LaunchMissionState mission = GetMission(missionId);
            LaunchMissionConfig config = GetConfiguredMissionConfig(missionId);
            if (!IsEnginePresetUnlocked(presetId))
            {
                LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName}은 아직 개발되지 않았습니다.";
                return ResearchActionResult.EnginePresetLocked;
            }

            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 설계에 진입할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            if (!mission.Unlocked)
            {
                LastMessage = $"{config.DisplayName} 미션은 아직 잠겨 있습니다.";
                return ResearchActionResult.MissionLocked;
            }

            if (Funds < config.LaunchCost)
            {
                LastMessage = $"예산 부족. 필요 {config.LaunchCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            Funds -= config.LaunchCost;
            TotalSpentFunds += config.LaunchCost;
            data = CreateDesignEntry(missionId, presetId, CreateDefaultInstalledEngineCounts(missionId, presetId), 50, GetDefaultVisibility(missionId), true);
            LastMessage = $"{config.DisplayName} 설계 진입. 예산 {config.LaunchCost} 지불.";
            return ResearchActionResult.Success;
        }

        public ResearchDesignEntryData CreateDesignEntry(
            LaunchMissionId missionId,
            EnginePresetId presetId,
            int[] installedEngineCounts,
            int designFit,
            TestVisibility visibility,
            bool launchCostPaid = false)
        {
            LaunchMissionConfig missionConfig = GetConfiguredMissionConfig(missionId);
            EnginePresetState selectedEngine = GetEnginePreset(presetId);
            int clampedFit = ClampInt(designFit, MinDesignFit, MaxDesignFit);
            TestVisibility normalizedVisibility = missionId == LaunchMissionId.LowPowerZoneHold ? TestVisibility.FinalMission : visibility;
            int mapSeed = CreateDesignMapSeed(missionId, presetId);
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);
            ClearLockedEngineCounts(counts);

            if (missionId == LaunchMissionId.StaticFire)
            {
                Array.Clear(counts, 0, counts.Length);
            }

            int reservedInstallCost = missionId == LaunchMissionId.StaticFire ? 0 : CalculateReservedInstallCost(counts);
            int installedScore = missionId == LaunchMissionId.StaticFire
                ? CalculateEnginePerformanceScore(presetId)
                : CalculateInstalledEngineScore(counts);

            return new ResearchDesignEntryData(
                missionId,
                presetId,
                Year,
                Quarter,
                mapSeed,
                CreateTargetPathId(missionId, mapSeed),
                selectedEngine.Completion,
                CalculateEnginePerformanceScore(presetId),
                installedScore,
                counts,
                reservedInstallCost,
                missionConfig.LaunchCost,
                clampedFit,
                normalizedVisibility,
                GetPreviousCertificationBonus(missionId, presetId),
                GetExperienceBonus(missionId),
                launchCostPaid);
        }

        public ResearchActionResult CommitLaunch(ResearchDesignEntryData designEntry, out ResearchLaunchResultData result)
        {
            result = default;
            LaunchMissionConfig config = GetConfiguredMissionConfig(designEntry.MissionId);
            LaunchMissionState mission = GetMission(designEntry.MissionId);

            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 발사할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            if (!IsEnginePresetUnlocked(designEntry.SelectedEnginePresetId))
            {
                LastMessage = $"{GetEnginePresetConfig(designEntry.SelectedEnginePresetId).DisplayName}은 아직 개발되지 않았습니다.";
                return ResearchActionResult.EnginePresetLocked;
            }

            if (!mission.Unlocked)
            {
                LastMessage = $"{config.DisplayName} 미션은 아직 잠겨 있습니다.";
                return ResearchActionResult.MissionLocked;
            }

            int entryCost = designEntry.LaunchCostPaid ? 0 : designEntry.LaunchCost;
            int remainingCost = entryCost + designEntry.ReservedInstallCost;
            if (Funds < remainingCost)
            {
                LastMessage = $"예산 부족. 필요 {remainingCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            int successChance = CalculateSuccessChance(designEntry);
            int partialChance = Math.Min(15, 95 - successChance);
            int failureChance = 100 - successChance - partialChance;
            int roll = CreateLaunchRoll(designEntry, mission.AttemptCount);
            ResearchGrade grade = DetermineGrade(successChance, roll);
            GetVisibilityAdjustedReward(grade, designEntry.Visibility, out int immediateFunding, out int quarterlyFundingDelta);

            Funds -= remainingCost;
            TotalSpentFunds += remainingCost;
            TotalLaunches++;
            if (grade == ResearchGrade.F)
            {
                FailedLaunches++;
            }

            mission.AttemptCount++;
            ApplyBestGrade(mission, grade);
            if (designEntry.MissionId == LaunchMissionId.StaticFire)
            {
                EnginePresetState preset = GetEnginePreset(designEntry.SelectedEnginePresetId);
                preset.AttemptCount++;
                ApplyBestGrade(preset, grade);
            }

            Funds += immediateFunding;
            QuarterlyFunding = ClampInt(QuarterlyFunding + quarterlyFundingDelta, balanceConfig.MinQuarterlyFunding, balanceConfig.MaxQuarterlyFunding);
            HighestQuarterlyFunding = Math.Max(HighestQuarterlyFunding, QuarterlyFunding);
            AdvanceQuarter();
            CheckUnlocks();

            bool finalMissionWon = designEntry.MissionId == LaunchMissionId.LowPowerZoneHold && grade <= ResearchGrade.B;
            bool deadlineMissed = DeadlineReached && !finalMissionWon;
            HasGameEnded = finalMissionWon || deadlineMissed;
            GameWon = finalMissionWon;

            result = new ResearchLaunchResultData(
                designEntry.MissionId,
                designEntry.SelectedEnginePresetId,
                designEntry.Year,
                designEntry.Quarter,
                designEntry.LaunchCost,
                designEntry.ReservedInstallCost,
                designEntry.Visibility,
                designEntry.DesignFit,
                designEntry.SelectedEngineScore,
                designEntry.InstalledEngineScore,
                successChance,
                partialChance,
                failureChance,
                roll,
                grade,
                immediateFunding,
                quarterlyFundingDelta,
                finalMissionWon,
                deadlineMissed);

            LastMessage = $"{config.DisplayName} 결과 {grade}. 총 비용 {designEntry.LaunchCost + designEntry.ReservedInstallCost}, 지원금 +{immediateFunding}, 분기 예산 {quarterlyFundingDelta:+#;-#;0}.";
            if (finalMissionWon)
            {
                LastMessage += " 최종 미션 성공.";
            }
            else if (deadlineMissed)
            {
                LastMessage += " 2026 Q4 종료. 목표 달성 실패.";
            }

            return ResearchActionResult.Success;
        }

        public int CalculateSuccessChance(LaunchMissionId missionId)
        {
            EnginePresetId presetId = GetDefaultCertifiedEnginePreset();
            ResearchDesignEntryData designEntry = CreateDesignEntry(missionId, presetId, CreateDefaultInstalledEngineCounts(missionId, presetId), 50, GetDefaultVisibility(missionId));
            return CalculateSuccessChance(designEntry);
        }

        public int CalculateSuccessChance(ResearchDesignEntryData designEntry)
        {
            int designFitModifier = CalculateDesignFitModifier(designEntry.DesignFit);
            int visibilityModifier = balanceConfig.GetVisibilitySuccessModifier(designEntry.Visibility);
            double raw;

            if (designEntry.MissionId == LaunchMissionId.StaticFire)
            {
                raw = 20
                    + designEntry.SelectedEngineScore * 0.8d
                    + designEntry.ExperienceBonus
                    + designFitModifier
                    + visibilityModifier;
            }
            else
            {
                raw = 20
                    + designEntry.InstalledEngineScore * GetMissionEngineWeight(designEntry.MissionId)
                    + designEntry.PreviousCertificationBonus
                    + designEntry.ExperienceBonus
                    + designFitModifier
                    + visibilityModifier;
            }

            return ClampInt((int)Math.Round(raw, MidpointRounding.AwayFromZero), 10, 90);
        }

        public int CalculateEnginePerformanceScore(EnginePresetId presetId)
        {
            EnginePresetState preset = GetEnginePreset(presetId);
            double average = (preset.FuelCapacity + preset.Cooling + preset.MaxOutput + preset.IgnitionReliability) / 4d;
            int minimum = Math.Min(Math.Min(preset.FuelCapacity, preset.Cooling), Math.Min(preset.MaxOutput, preset.IgnitionReliability));
            double quality = average * 0.6d + minimum * 0.4d;
            double penalty = CalculateEngineImbalancePenalty(preset);
            double score = quality * 0.6d - penalty;
            return ClampInt((int)Math.Round(score, MidpointRounding.AwayFromZero), 0, 100);
        }

        public int CalculateResearchStatGain(bool focused, int score)
        {
            return balanceConfig.GetResearchStatGain(focused, score);
        }

        public int GetConfiguredVisibilitySuccessModifier(TestVisibility visibility)
        {
            return balanceConfig.GetVisibilitySuccessModifier(visibility);
        }

        public int CalculateInstalledEngineScore(int[] installedEngineCounts)
        {
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);
            ClearLockedEngineCounts(counts);
            int count = 0;
            double total = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                count += counts[i];
                total += CalculateEnginePerformanceScore((EnginePresetId)i) * counts[i];
            }

            if (count <= 0)
            {
                return 0;
            }

            double average = total / count;
            double countBonus = Math.Min(8, (count - 1) * 4);
            double overweightPenalty = Math.Max(0, count - 3) * 8;
            return ClampInt((int)Math.Round(average + countBonus - overweightPenalty, MidpointRounding.AwayFromZero), 0, 100);
        }

        public EngineRiskInfo[] GetTopEngineRisks(EnginePresetId presetId, int count = 2)
        {
            return GetTopEngineRisks(GetEnginePreset(presetId), count);
        }

        public static EngineRiskInfo[] GetTopEngineRisks(EnginePresetState engine, int count = 2)
        {
            var risks = new[]
            {
                new EngineRiskInfo(
                    EngineRiskKind.OutputShortage,
                    ClampInt(100 - engine.MaxOutput + Math.Max(0, engine.FuelCapacity - engine.MaxOutput) / 2, 0, 100),
                    "출력 부족",
                    "최대 출력이 낮으면 목표 고도와 구역 도달 여유가 줄어듭니다."),
                new EngineRiskInfo(
                    EngineRiskKind.FuelExhaustion,
                    ClampInt(100 - engine.FuelCapacity + Math.Max(0, engine.MaxOutput - engine.FuelCapacity) / 2, 0, 100),
                    "연료 소진",
                    "연료량이 부족하면 장시간 연소와 체류 미션에서 먼저 무너집니다."),
                new EngineRiskInfo(
                    EngineRiskKind.CoolingShortage,
                    ClampInt(100 - engine.Cooling + Math.Max(0, engine.MaxOutput - engine.Cooling), 0, 100),
                    "냉각 부족",
                    "냉각이 부족하면 고출력 연소 중 과열 위험이 커집니다."),
                new EngineRiskInfo(
                    EngineRiskKind.IgnitionInstability,
                    ClampInt(100 - engine.IgnitionReliability + Math.Max(0, engine.MaxOutput - engine.IgnitionReliability) / 2, 0, 100),
                    "점화 불안정",
                    "점화 신뢰도가 낮으면 발사 초반 실패와 재시동 실패 가능성이 커집니다."),
            };

            Array.Sort(risks, (a, b) => b.Severity.CompareTo(a.Severity));
            int length = ClampInt(count, 0, risks.Length);
            var result = new EngineRiskInfo[length];
            Array.Copy(risks, result, length);
            return result;
        }

        public bool CanUnlockNext(LaunchMissionId missionId)
        {
            LaunchMissionId next = GetNextMission(missionId);
            return next != missionId && GetMission(missionId).HasBestGrade && GetMission(missionId).BestGrade <= ResearchGrade.C;
        }

        public string GetUnlockConditionText(LaunchMissionId missionId)
        {
            return GetConfiguredMissionConfig(missionId).RequirementText;
        }

        public static string GetStatDisplayName(EngineStatId statId)
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    return "연료 탱크 용량";
                case EngineStatId.Cooling:
                    return "냉각 능력";
                case EngineStatId.MaxOutput:
                    return "최대 출력";
                case EngineStatId.IgnitionReliability:
                    return "점화 신뢰도";
                default:
                    throw new ArgumentOutOfRangeException(nameof(statId), statId, null);
            }
        }

        public static string GetStatInsightText(EngineStatId statId)
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    return "연료량은 출력 지속 시간과 체류 여유를 받쳐줍니다.";
                case EngineStatId.Cooling:
                    return "냉각은 고출력 연소의 과열 위험을 낮춥니다.";
                case EngineStatId.MaxOutput:
                    return "최대 출력은 고도와 목표 구역 도달 가능성을 직접 밀어 올립니다.";
                case EngineStatId.IgnitionReliability:
                    return "점화 신뢰도는 발사 초반 실패와 재시동 불안을 줄입니다.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(statId), statId, null);
            }
        }

        public static string GetVisibilityDisplayName(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return "공개 테스트";
                case TestVisibility.Private:
                    return "비공개 테스트";
                case TestVisibility.FinalMission:
                    return "FINAL MISSION";
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        public static int CalculateDesignFitModifier(int designFit)
        {
            return ClampInt((int)Math.Round((ClampInt(designFit, MinDesignFit, MaxDesignFit) - 50) * 0.4d, MidpointRounding.AwayFromZero), -20, 20);
        }

        public static int GetVisibilitySuccessModifier(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return -10;
                case TestVisibility.Private:
                    return 10;
                case TestVisibility.FinalMission:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        public static LaunchMissionId GetNextMission(LaunchMissionId missionId)
        {
            int next = (int)missionId + 1;
            return next < Enum.GetValues(typeof(LaunchMissionId)).Length ? (LaunchMissionId)next : missionId;
        }

        private void CheckUnlocks()
        {
            UnlockIfReady(LaunchMissionId.StaticFire, LaunchMissionId.LowAltitude);
            UnlockIfReady(LaunchMissionId.LowAltitude, LaunchMissionId.HighAltitude);
            UnlockIfReady(LaunchMissionId.HighAltitude, LaunchMissionId.TargetZone);
            UnlockIfReady(LaunchMissionId.TargetZone, LaunchMissionId.ZoneHold);
            UnlockIfReady(LaunchMissionId.ZoneHold, LaunchMissionId.LowPowerZoneHold);
        }

        private void UnlockIfReady(LaunchMissionId current, LaunchMissionId next)
        {
            LaunchMissionState currentMission = GetMission(current);
            if (currentMission.HasBestGrade && currentMission.BestGrade <= ResearchGrade.C)
            {
                GetMission(next).Unlocked = true;
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

        private int GetPreviousCertificationBonus(LaunchMissionId missionId, EnginePresetId presetId)
        {
            switch (missionId)
            {
                case LaunchMissionId.StaticFire:
                    return 0;
                case LaunchMissionId.LowAltitude:
                    EnginePresetState preset = GetEnginePreset(presetId);
                    return GetGradeBonus(preset.BestGrade, preset.HasBestGrade);
                case LaunchMissionId.HighAltitude:
                    return GetGradeBonus(GetMission(LaunchMissionId.LowAltitude));
                case LaunchMissionId.TargetZone:
                    return GetGradeBonus(GetMission(LaunchMissionId.HighAltitude));
                case LaunchMissionId.ZoneHold:
                    return GetGradeBonus(GetMission(LaunchMissionId.TargetZone));
                case LaunchMissionId.LowPowerZoneHold:
                    return GetGradeBonus(GetMission(LaunchMissionId.ZoneHold));
                default:
                    throw new ArgumentOutOfRangeException(nameof(missionId), missionId, null);
            }
        }

        private int GetExperienceBonus(LaunchMissionId missionId)
        {
            return Math.Min(GetMission(missionId).AttemptCount * 3, 9);
        }

        private static int GetGradeBonus(LaunchMissionState mission)
        {
            return GetGradeBonus(mission.BestGrade, mission.HasBestGrade);
        }

        private static int GetGradeBonus(ResearchGrade grade, bool hasGrade)
        {
            if (!hasGrade)
            {
                return 0;
            }

            switch (grade)
            {
                case ResearchGrade.S:
                    return 18;
                case ResearchGrade.A:
                    return 14;
                case ResearchGrade.B:
                    return 10;
                case ResearchGrade.C:
                    return 5;
                default:
                    return 0;
            }
        }

        private double GetMissionEngineWeight(LaunchMissionId missionId)
        {
            return GetConfiguredMissionConfig(missionId).EngineWeight;
        }

        private int CalculateReservedInstallCost(int[] installedEngineCounts)
        {
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);
            ClearLockedEngineCounts(counts);
            int totalCount = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                totalCount += counts[i];
            }

            return totalCount * balanceConfig.EngineInstallCost;
        }

        private int[] CreateDefaultInstalledEngineCounts(LaunchMissionId missionId, EnginePresetId presetId)
        {
            var counts = new int[MaxEnginePresetCount];
            if (missionId != LaunchMissionId.StaticFire)
            {
                counts[(int)presetId] = 1;
            }

            return counts;
        }

        private EnginePresetId GetDefaultCertifiedEnginePreset()
        {
            for (int i = 0; i < EnginePresets.Length; i++)
            {
                if (EnginePresets[i].Unlocked && EnginePresets[i].HasBestGrade && EnginePresets[i].BestGrade <= ResearchGrade.C)
                {
                    return EnginePresets[i].PresetId;
                }
            }

            return EnginePresetId.Engine01;
        }

        private static TestVisibility GetDefaultVisibility(LaunchMissionId missionId)
        {
            return missionId == LaunchMissionId.LowPowerZoneHold ? TestVisibility.FinalMission : TestVisibility.Private;
        }

        private double CalculateEngineImbalancePenalty(EnginePresetState preset)
        {
            int minimum = Math.Min(Math.Min(preset.FuelCapacity, preset.Cooling), Math.Min(preset.MaxOutput, preset.IgnitionReliability));
            double tankOverweight = Math.Max(0, preset.FuelCapacity - preset.MaxOutput - 15) * 0.4d;
            double fuelShortage = Math.Max(0, preset.MaxOutput - preset.FuelCapacity - 15) * 0.4d;
            double coolingShortage = Math.Max(0, preset.MaxOutput - preset.Cooling - 10) * 0.6d;
            double ignitionUnstable = Math.Max(0, preset.MaxOutput - preset.IgnitionReliability - 20) * 0.35d;
            double minimumStatPenalty = Math.Max(0, 35 - minimum) * 0.8d;
            return Math.Min(35, tankOverweight + fuelShortage + coolingShortage + ignitionUnstable + minimumStatPenalty);
        }

        private static void ApplyBestGrade(LaunchMissionState mission, ResearchGrade grade)
        {
            if (!mission.HasBestGrade || grade < mission.BestGrade)
            {
                mission.BestGrade = grade;
                mission.HasBestGrade = true;
            }
        }

        private static void ApplyBestGrade(EnginePresetState preset, ResearchGrade grade)
        {
            if (!preset.HasBestGrade || grade < preset.BestGrade)
            {
                preset.BestGrade = grade;
                preset.HasBestGrade = true;
            }
        }

        private void GetVisibilityAdjustedReward(ResearchGrade grade, TestVisibility visibility, out int immediateFunding, out int quarterlyFundingDelta)
        {
            ResearchGradeReward reward = balanceConfig.GetLaunchReward(grade);
            if (grade == ResearchGrade.F)
            {
                immediateFunding = 0;
                quarterlyFundingDelta = balanceConfig.GetFailureQuarterlyFundingDelta(visibility);
                return;
            }

            double multiplier = balanceConfig.GetRewardMultiplier(visibility);
            immediateFunding = (int)Math.Round(reward.ImmediateFunding * multiplier, MidpointRounding.AwayFromZero);
            quarterlyFundingDelta = (int)Math.Round(reward.QuarterlyFundingDelta * multiplier, MidpointRounding.AwayFromZero);
        }

        private int CreateDesignMapSeed(LaunchMissionId missionId, EnginePresetId presetId)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Seed;
                hash = hash * 31 + Year;
                hash = hash * 31 + Quarter;
                hash = hash * 31 + (int)missionId;
                hash = hash * 31 + (int)presetId;
                return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
            }
        }

        private static string CreateTargetPathId(LaunchMissionId missionId, int mapSeed)
        {
            int pathIndex = mapSeed % 3 + 1;
            return $"{missionId}_Path_{pathIndex}";
        }

        private int CreateLaunchRoll(ResearchDesignEntryData designEntry, int attemptCount)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + Seed;
                hash = hash * 31 + designEntry.MapSeed;
                hash = hash * 31 + designEntry.Year;
                hash = hash * 31 + designEntry.Quarter;
                hash = hash * 31 + (int)designEntry.MissionId;
                hash = hash * 31 + (int)designEntry.SelectedEnginePresetId;
                for (int i = 0; i < designEntry.InstalledEngineCounts.Length; i++)
                {
                    hash = hash * 31 + designEntry.InstalledEngineCounts[i];
                }

                hash = hash * 31 + designEntry.DesignFit;
                hash = hash * 31 + (int)designEntry.Visibility;
                hash = hash * 31 + attemptCount;
                return (hash & int.MaxValue) % 100 + 1;
            }
        }

        private static ResearchGrade DetermineGrade(int successChance, int roll)
        {
            if (roll <= successChance)
            {
                int margin = successChance - roll;
                if (margin >= 50)
                {
                    return ResearchGrade.S;
                }

                if (margin >= 20)
                {
                    return ResearchGrade.A;
                }

                return ResearchGrade.B;
            }

            return roll <= Math.Min(successChance + 15, 95)
                ? ResearchGrade.C
                : ResearchGrade.F;
        }

        private static int[] CopyAndNormalizeEngineCounts(int[] source)
        {
            var copy = new int[MaxEnginePresetCount];
            if (source == null)
            {
                return copy;
            }

            int length = Math.Min(copy.Length, source.Length);
            for (int i = 0; i < length; i++)
            {
                copy[i] = Math.Max(0, source[i]);
            }

            return copy;
        }

        private void ClearLockedEngineCounts(int[] counts)
        {
            if (counts == null)
            {
                return;
            }

            for (int i = 0; i < counts.Length; i++)
            {
                if (!IsEnginePresetUnlocked((EnginePresetId)i))
                {
                    counts[i] = 0;
                }
            }
        }
    }
}
