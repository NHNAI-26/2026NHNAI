using System;
using System.Collections.Generic;

namespace Border.Research
{
    public enum ResearchStageId
    {
        Engine,
        Rocket,
        Orbit,
        Moon
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
        StageLocked,
        NotEnoughFunds,
        ProgressTooLow,
        DeadlineReached,
        NoPendingDesignEntry,
        EngineLevelMaxed
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

    [Serializable]
    public sealed class EnginePresetState
    {
        public EnginePresetId PresetId;
        public int Level;
        public int FuelCapacity;
        public int Cooling;
        public int MaxOutput;
        public int IgnitionReliability;
        public int AttemptCount;
        public ResearchGrade BestGrade;
        public bool HasBestGrade;

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
        {
            Id = id;
            DisplayName = displayName;
            NormalResearchCost = ResearchPrototypeModel.EngineNormalResearchCost;
            FocusedResearchCost = ResearchPrototypeModel.EngineFocusedResearchCost;
            InstallCost = ResearchPrototypeModel.EngineInstallCost;
            InitialFuelCapacity = ResearchPrototypeModel.InitialEngineStat;
            InitialCooling = ResearchPrototypeModel.InitialEngineStat;
            InitialMaxOutput = ResearchPrototypeModel.InitialEngineStat;
            InitialIgnitionReliability = ResearchPrototypeModel.InitialEngineStat;
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

    public readonly struct ResearchStageConfig
    {
        public ResearchStageConfig(ResearchStageId id, string displayName, int launchCost, string requirementText)
        {
            Id = id;
            DisplayName = displayName;
            LaunchCost = launchCost;
            TestCost = launchCost;
            RequirementText = requirementText;
            NormalResearchCost = 0;
            FocusedResearchCost = 0;
            MinimumTestProgress = 0;
            UnlockProgressRequirement = 0;
        }

        public ResearchStageId Id { get; }
        public string DisplayName { get; }
        public int LaunchCost { get; }
        public int TestCost { get; }
        public string RequirementText { get; }
        public int NormalResearchCost { get; }
        public int FocusedResearchCost { get; }
        public int MinimumTestProgress { get; }
        public int UnlockProgressRequirement { get; }
    }

    public readonly struct ResearchDesignEntryData
    {
        public ResearchDesignEntryData(
            ResearchStageId stageId,
            EnginePresetId selectedEnginePresetId,
            int year,
            int quarter,
            int mapSeed,
            string targetPathId,
            int selectedEngineLevel,
            int selectedEngineScore,
            int installedEngineScore,
            int[] installedEngineCounts,
            int reservedInstallCost,
            int launchCost,
            int designFit,
            TestVisibility visibility,
            int previousCertificationBonus,
            int experienceBonus)
        {
            StageId = stageId;
            SelectedEnginePresetId = selectedEnginePresetId;
            Year = year;
            Quarter = quarter;
            MapSeed = mapSeed;
            TargetPathId = targetPathId;
            SelectedEngineLevel = selectedEngineLevel;
            SelectedEngineScore = selectedEngineScore;
            InstalledEngineScore = installedEngineScore;
            InstalledEngineCounts = CopyEngineCounts(installedEngineCounts);
            ReservedInstallCost = reservedInstallCost;
            LaunchCost = launchCost;
            DesignFit = designFit;
            Visibility = visibility;
            PreviousCertificationBonus = previousCertificationBonus;
            ExperienceBonus = experienceBonus;
        }

        public ResearchStageId StageId { get; }
        public EnginePresetId SelectedEnginePresetId { get; }
        public int Year { get; }
        public int Quarter { get; }
        public int MapSeed { get; }
        public string TargetPathId { get; }
        public int SelectedEngineLevel { get; }
        public int SelectedEngineScore { get; }
        public int InstalledEngineScore { get; }
        public int[] InstalledEngineCounts { get; }
        public int ReservedInstallCost { get; }
        public int LaunchCost { get; }
        public int DesignFit { get; }
        public TestVisibility Visibility { get; }
        public int PreviousCertificationBonus { get; }
        public int ExperienceBonus { get; }
        public int CurrentProgress => SelectedEngineLevel;
        public double PrerequisiteAverage => PreviousCertificationBonus;

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
            ResearchStageId stageId,
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
            bool moonMissionWon,
            bool deadlineMissed)
        {
            StageId = stageId;
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
            MoonMissionWon = moonMissionWon;
            DeadlineMissed = deadlineMissed;
        }

        public ResearchStageId StageId { get; }
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
        public bool MoonMissionWon { get; }
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
        public const int NormalResearchLevelGain = 1;
        public const int FocusedResearchLevelGain = 2;
        public const int NormalResearchGain = NormalResearchLevelGain;
        public const int FocusedResearchGain = FocusedResearchLevelGain;
        public const int MaxEnginePresetLevel = 5;
        public const int MaxEnginePresetCount = 10;
        public const int InitialEngineStat = 40;
        public const int EngineNormalResearchCost = 350;
        public const int EngineFocusedResearchCost = 650;
        public const int EngineInstallCost = 350;
        public const int MinDesignFit = 0;
        public const int MaxDesignFit = 100;

        private static readonly EnginePresetConfig[] EnginePresetConfigs =
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

        private static readonly ResearchStageConfig[] StageConfigs =
        {
            new(ResearchStageId.Engine, "엔진 테스트", 600, "시험할 엔진 레벨 1 이상"),
            new(ResearchStageId.Rocket, "로켓 테스트", 900, "엔진 테스트 C 이상"),
            new(ResearchStageId.Orbit, "궤도 테스트", 1200, "로켓 테스트 C 이상"),
            new(ResearchStageId.Moon, "달 착륙", 1800, "궤도 테스트 C 이상"),
        };

        public ResearchPrototypeModel(int seed = 20260904)
        {
            Seed = seed;
            Stages = new ResearchStageState[StageConfigs.Length];
            EnginePresets = new EnginePresetState[EnginePresetConfigs.Length];
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
        public EnginePresetState[] EnginePresets { get; }

        public void Reset()
        {
            Year = StartYear;
            Quarter = StartQuarter;
            RemainingTurns = MaxTurns;
            Funds = InitialFunds;
            QuarterlyFunding = InitialQuarterlyFunding;
            LastMessage = "2018 Q1. 엔진 프리셋 연구 판단을 시작합니다.";

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

            for (int i = 0; i < EnginePresets.Length; i++)
            {
                EnginePresetConfig config = EnginePresetConfigs[i];
                EnginePresets[i] = new EnginePresetState
                {
                    PresetId = config.Id,
                    Level = 0,
                    FuelCapacity = config.InitialFuelCapacity,
                    Cooling = config.InitialCooling,
                    MaxOutput = config.InitialMaxOutput,
                    IgnitionReliability = config.InitialIgnitionReliability,
                    AttemptCount = 0,
                    BestGrade = ResearchGrade.F,
                    HasBestGrade = false
                };
            }
        }

        public static IReadOnlyList<ResearchStageConfig> GetStageConfigs()
        {
            return StageConfigs;
        }

        public static IReadOnlyList<EnginePresetConfig> GetEnginePresetConfigs()
        {
            return EnginePresetConfigs;
        }

        public static ResearchStageConfig GetStageConfig(ResearchStageId stageId)
        {
            return StageConfigs[(int)stageId];
        }

        public static EnginePresetConfig GetEnginePresetConfig(EnginePresetId presetId)
        {
            return EnginePresetConfigs[(int)presetId];
        }

        public ResearchStageState GetStage(ResearchStageId stageId)
        {
            return Stages[(int)stageId];
        }

        public EnginePresetState GetEnginePreset(EnginePresetId presetId)
        {
            return EnginePresets[(int)presetId];
        }

#if UNITY_EDITOR
        public void PrepareDebugDesignEntryState(ResearchStageId stageId, EnginePresetId presetId = EnginePresetId.Engine01)
        {
            EnginePresetState preset = GetEnginePreset(presetId);
            preset.Level = Math.Max(preset.Level, 3);
            preset.FuelCapacity = Math.Max(preset.FuelCapacity, 65);
            preset.Cooling = Math.Max(preset.Cooling, 65);
            preset.MaxOutput = Math.Max(preset.MaxOutput, 65);
            preset.IgnitionReliability = Math.Max(preset.IgnitionReliability, 65);

            for (int i = 0; i <= (int)stageId; i++)
            {
                Stages[i].Unlocked = true;
            }

            ResearchStageConfig config = GetStageConfig(stageId);
            Funds = Math.Max(Funds, config.LaunchCost + EngineInstallCost * 2);
        }

#endif
        public ResearchActionResult ExecuteResearch(ResearchStageId stageId, bool focused)
        {
            if (stageId != ResearchStageId.Engine)
            {
                LastMessage = "로켓, 궤도, 달 착륙은 연구 대상이 아닙니다. 설계와 발사로만 진행합니다.";
                return ResearchActionResult.StageLocked;
            }

            return ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.FuelCapacity, focused, 80);
        }

        public ResearchActionResult ExecuteEngineResearch(EnginePresetId presetId, EngineStatId statId, bool focused, int score)
        {
            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 연구할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            EnginePresetState preset = GetEnginePreset(presetId);
            if (preset.Level >= MaxEnginePresetLevel)
            {
                LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName}은 이미 최대 레벨입니다.";
                return ResearchActionResult.EngineLevelMaxed;
            }

            EnginePresetConfig config = GetEnginePresetConfig(presetId);
            int cost = focused ? config.FocusedResearchCost : config.NormalResearchCost;
            if (Funds < cost)
            {
                LastMessage = $"연구비 부족. 필요 {cost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            int oldStat = preset.GetStat(statId);
            int oldLevel = preset.Level;
            int statGain = GetResearchStatGain(focused, score);
            int levelGain = focused ? FocusedResearchLevelGain : NormalResearchLevelGain;

            Funds -= cost;
            preset.SetStat(statId, oldStat + statGain);
            preset.Level = Math.Min(MaxEnginePresetLevel, preset.Level + levelGain);
            AdvanceQuarter();

            LastMessage = $"{config.DisplayName} {GetStatDisplayName(statId)} {(focused ? "집중" : "일반")} 연구 완료. "
                + $"점수 {ClampInt(score, 0, 100)}, 스탯 {oldStat}->{preset.GetStat(statId)}, 레벨 {oldLevel}->{preset.Level}.";
            return ResearchActionResult.Success;
        }

        public ResearchActionResult WaitQuarter()
        {
            AdvanceQuarter();
            LastMessage = "한 분기 대기. 정기 연구비를 받았습니다.";
            return DeadlineReached ? ResearchActionResult.DeadlineReached : ResearchActionResult.Success;
        }

        public ResearchActionResult TryEnterDesign(ResearchStageId stageId, out ResearchDesignEntryData data)
        {
            return TryEnterDesign(stageId, EnginePresetId.Engine01, out data);
        }

        public ResearchActionResult TryEnterDesign(ResearchStageId stageId, EnginePresetId presetId, out ResearchDesignEntryData data)
        {
            data = default;
            ResearchStageState stage = GetStage(stageId);
            ResearchStageConfig config = GetStageConfig(stageId);

            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 설계에 진입할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            if (!stage.Unlocked)
            {
                LastMessage = $"{config.DisplayName} 단계는 아직 잠겨 있습니다.";
                return ResearchActionResult.StageLocked;
            }

            if (stageId == ResearchStageId.Engine && GetEnginePreset(presetId).Level < 1)
            {
                LastMessage = $"{GetEnginePresetConfig(presetId).DisplayName} 시험 조건 부족. 엔진 레벨 1 필요.";
                return ResearchActionResult.ProgressTooLow;
            }

            if (Funds < config.LaunchCost)
            {
                LastMessage = $"발사비 부족. 필요 {config.LaunchCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            data = CreateDesignEntry(stageId, presetId, CreateDefaultInstalledEngineCounts(stageId, presetId), 50, GetDefaultVisibility(stageId));
            LastMessage = $"{config.DisplayName} 설계 진입 준비 완료. 비용과 시간은 아직 소비하지 않습니다.";
            return ResearchActionResult.Success;
        }

        public ResearchDesignEntryData CreateDesignEntry(
            ResearchStageId stageId,
            EnginePresetId presetId,
            int[] installedEngineCounts,
            int designFit,
            TestVisibility visibility)
        {
            ResearchStageConfig stageConfig = GetStageConfig(stageId);
            EnginePresetState selectedEngine = GetEnginePreset(presetId);
            int clampedFit = ClampInt(designFit, MinDesignFit, MaxDesignFit);
            TestVisibility normalizedVisibility = stageId == ResearchStageId.Moon ? TestVisibility.FinalMission : visibility;
            int mapSeed = CreateDesignMapSeed(stageId, presetId);
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);

            if (stageId == ResearchStageId.Engine)
            {
                Array.Clear(counts, 0, counts.Length);
            }

            int reservedInstallCost = stageId == ResearchStageId.Engine ? 0 : CalculateReservedInstallCost(counts);
            int installedScore = stageId == ResearchStageId.Engine
                ? CalculateEnginePerformanceScore(presetId)
                : CalculateInstalledEngineScore(counts);

            return new ResearchDesignEntryData(
                stageId,
                presetId,
                Year,
                Quarter,
                mapSeed,
                CreateTargetPathId(stageId, mapSeed),
                selectedEngine.Level,
                CalculateEnginePerformanceScore(presetId),
                installedScore,
                counts,
                reservedInstallCost,
                stageConfig.LaunchCost,
                clampedFit,
                normalizedVisibility,
                GetPreviousCertificationBonus(stageId, presetId),
                GetExperienceBonus(stageId));
        }

        public ResearchActionResult CommitLaunch(ResearchDesignEntryData designEntry, out ResearchLaunchResultData result)
        {
            result = default;
            ResearchStageConfig config = GetStageConfig(designEntry.StageId);
            ResearchStageState stage = GetStage(designEntry.StageId);

            if (DeadlineReached)
            {
                LastMessage = "마감 도달. 더 이상 발사할 수 없습니다.";
                return ResearchActionResult.DeadlineReached;
            }

            if (!stage.Unlocked)
            {
                LastMessage = $"{config.DisplayName} 단계는 아직 잠겨 있습니다.";
                return ResearchActionResult.StageLocked;
            }

            if (designEntry.StageId == ResearchStageId.Engine && GetEnginePreset(designEntry.SelectedEnginePresetId).Level < 1)
            {
                LastMessage = $"{GetEnginePresetConfig(designEntry.SelectedEnginePresetId).DisplayName} 시험 조건 부족. 엔진 레벨 1 필요.";
                return ResearchActionResult.ProgressTooLow;
            }

            int totalCost = designEntry.LaunchCost + designEntry.ReservedInstallCost;
            if (Funds < totalCost)
            {
                LastMessage = $"발사비 부족. 필요 {totalCost}, 보유 {Funds}.";
                return ResearchActionResult.NotEnoughFunds;
            }

            int successChance = CalculateSuccessChance(designEntry);
            int partialChance = Math.Min(15, 95 - successChance);
            int failureChance = 100 - successChance - partialChance;
            int roll = CreateLaunchRoll(designEntry, stage.AttemptCount);
            ResearchGrade grade = DetermineGrade(successChance, roll);
            GetVisibilityAdjustedReward(grade, designEntry.Visibility, out int immediateFunding, out int quarterlyFundingDelta);

            Funds -= totalCost;
            stage.AttemptCount++;
            ApplyBestGrade(stage, grade);
            if (designEntry.StageId == ResearchStageId.Engine)
            {
                EnginePresetState preset = GetEnginePreset(designEntry.SelectedEnginePresetId);
                preset.AttemptCount++;
                ApplyBestGrade(preset, grade);
            }

            Funds += immediateFunding;
            QuarterlyFunding = ClampInt(QuarterlyFunding + quarterlyFundingDelta, MinQuarterlyFunding, MaxQuarterlyFunding);
            AdvanceQuarter();
            CheckUnlocks();

            bool moonMissionWon = designEntry.StageId == ResearchStageId.Moon && grade <= ResearchGrade.B;
            bool deadlineMissed = DeadlineReached && !moonMissionWon;
            result = new ResearchLaunchResultData(
                designEntry.StageId,
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
                moonMissionWon,
                deadlineMissed);

            LastMessage = $"{config.DisplayName} 발사 결과 {grade}. 총 비용 {totalCost}, 지원금 +{immediateFunding}, 분기 연구비 {quarterlyFundingDelta:+#;-#;0}.";
            if (moonMissionWon)
            {
                LastMessage += " 달 착륙 성공.";
            }
            else if (deadlineMissed)
            {
                LastMessage += " 2026 Q4 종료. 목표 달성 실패.";
            }

            return ResearchActionResult.Success;
        }

        public int CalculateSuccessChance(ResearchStageId stageId)
        {
            EnginePresetId presetId = GetDefaultCertifiedEnginePreset();
            ResearchDesignEntryData designEntry = CreateDesignEntry(stageId, presetId, CreateDefaultInstalledEngineCounts(stageId, presetId), 50, GetDefaultVisibility(stageId));
            return CalculateSuccessChance(designEntry);
        }

        public int CalculateSuccessChance(ResearchDesignEntryData designEntry)
        {
            int designFitModifier = CalculateDesignFitModifier(designEntry.DesignFit);
            int visibilityModifier = GetVisibilitySuccessModifier(designEntry.Visibility);
            double raw;

            if (designEntry.StageId == ResearchStageId.Engine)
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
                    + designEntry.InstalledEngineScore * GetStageEngineWeight(designEntry.StageId)
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
            double score = preset.Level * 6d + quality * 0.6d - penalty;
            return ClampInt((int)Math.Round(score, MidpointRounding.AwayFromZero), 0, 100);
        }

        public int CalculateInstalledEngineScore(int[] installedEngineCounts)
        {
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);
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

        public bool CanUnlockNext(ResearchStageId stageId)
        {
            switch (stageId)
            {
                case ResearchStageId.Engine:
                    return HasAnyCertifiedEngine();
                case ResearchStageId.Rocket:
                case ResearchStageId.Orbit:
                    ResearchStageState stage = GetStage(stageId);
                    return stage.HasBestGrade && stage.BestGrade <= ResearchGrade.C;
                default:
                    return false;
            }
        }

        public string GetUnlockConditionText(ResearchStageId stageId)
        {
            switch (stageId)
            {
                case ResearchStageId.Engine:
                    return "기본 해금";
                case ResearchStageId.Rocket:
                    return "필요: 엔진 테스트 C 이상";
                case ResearchStageId.Orbit:
                    return "필요: 로켓 테스트 C 이상";
                case ResearchStageId.Moon:
                    return "필요: 궤도 테스트 C 이상";
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageId), stageId, null);
            }
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

        private void CheckUnlocks()
        {
            if (HasAnyCertifiedEngine())
            {
                GetStage(ResearchStageId.Rocket).Unlocked = true;
            }

            UnlockIfReady(ResearchStageId.Rocket, ResearchStageId.Orbit);
            UnlockIfReady(ResearchStageId.Orbit, ResearchStageId.Moon);
        }

        private void UnlockIfReady(ResearchStageId current, ResearchStageId next)
        {
            ResearchStageState currentStage = GetStage(current);
            if (currentStage.HasBestGrade && currentStage.BestGrade <= ResearchGrade.C)
            {
                GetStage(next).Unlocked = true;
            }
        }

        private bool HasAnyCertifiedEngine()
        {
            for (int i = 0; i < EnginePresets.Length; i++)
            {
                EnginePresetState preset = EnginePresets[i];
                if (preset.HasBestGrade && preset.BestGrade <= ResearchGrade.C)
                {
                    return true;
                }
            }

            return false;
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

        private int GetPreviousCertificationBonus(ResearchStageId stageId, EnginePresetId presetId)
        {
            switch (stageId)
            {
                case ResearchStageId.Engine:
                    return 0;
                case ResearchStageId.Rocket:
                    EnginePresetState preset = GetEnginePreset(presetId);
                    return GetGradeBonus(preset.BestGrade, preset.HasBestGrade);
                case ResearchStageId.Orbit:
                    return GetGradeBonus(GetStage(ResearchStageId.Rocket));
                case ResearchStageId.Moon:
                    return GetGradeBonus(GetStage(ResearchStageId.Orbit));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageId), stageId, null);
            }
        }

        private int GetExperienceBonus(ResearchStageId stageId)
        {
            return Math.Min(GetStage(stageId).AttemptCount * 3, 9);
        }

        private static int GetGradeBonus(ResearchStageState stage)
        {
            return GetGradeBonus(stage.BestGrade, stage.HasBestGrade);
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

        private double GetStageEngineWeight(ResearchStageId stageId)
        {
            switch (stageId)
            {
                case ResearchStageId.Rocket:
                    return 0.55d;
                case ResearchStageId.Orbit:
                    return 0.45d;
                case ResearchStageId.Moon:
                    return 0.40d;
                default:
                    return 0d;
            }
        }

        private int CalculateReservedInstallCost(int[] installedEngineCounts)
        {
            int[] counts = CopyAndNormalizeEngineCounts(installedEngineCounts);
            int totalCount = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                totalCount += counts[i];
            }

            return totalCount * EngineInstallCost;
        }

        private int[] CreateDefaultInstalledEngineCounts(ResearchStageId stageId, EnginePresetId presetId)
        {
            var counts = new int[MaxEnginePresetCount];
            if (stageId != ResearchStageId.Engine)
            {
                counts[(int)presetId] = 1;
            }

            return counts;
        }

        private EnginePresetId GetDefaultCertifiedEnginePreset()
        {
            for (int i = 0; i < EnginePresets.Length; i++)
            {
                if (EnginePresets[i].HasBestGrade && EnginePresets[i].BestGrade <= ResearchGrade.C)
                {
                    return EnginePresets[i].PresetId;
                }
            }

            return EnginePresetId.Engine01;
        }

        private static TestVisibility GetDefaultVisibility(ResearchStageId stageId)
        {
            return stageId == ResearchStageId.Moon ? TestVisibility.FinalMission : TestVisibility.Private;
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

        private static int GetResearchStatGain(bool focused, int score)
        {
            int clampedScore = ClampInt(score, 0, 100);
            if (clampedScore < 50)
            {
                return focused ? 16 : 10;
            }

            if (clampedScore < 80)
            {
                return focused ? 21 : 13;
            }

            return focused ? 26 : 16;
        }

        private static void ApplyBestGrade(ResearchStageState stage, ResearchGrade grade)
        {
            if (!stage.HasBestGrade || grade < stage.BestGrade)
            {
                stage.BestGrade = grade;
                stage.HasBestGrade = true;
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

        private static void GetVisibilityAdjustedReward(ResearchGrade grade, TestVisibility visibility, out int immediateFunding, out int quarterlyFundingDelta)
        {
            GetGradeReward(grade, out int baseImmediateFunding, out int baseQuarterlyFundingDelta);
            if (grade == ResearchGrade.F)
            {
                immediateFunding = 0;
                switch (visibility)
                {
                    case TestVisibility.Public:
                        quarterlyFundingDelta = -150;
                        break;
                    case TestVisibility.Private:
                        quarterlyFundingDelta = -50;
                        break;
                    default:
                        quarterlyFundingDelta = -100;
                        break;
                }

                return;
            }

            double multiplier = GetRewardMultiplier(visibility);
            immediateFunding = (int)Math.Round(baseImmediateFunding * multiplier, MidpointRounding.AwayFromZero);
            quarterlyFundingDelta = (int)Math.Round(baseQuarterlyFundingDelta * multiplier, MidpointRounding.AwayFromZero);
        }

        private static double GetRewardMultiplier(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return 1.5d;
                case TestVisibility.Private:
                    return 0.5d;
                case TestVisibility.FinalMission:
                    return 1d;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        private int CreateDesignMapSeed(ResearchStageId stageId, EnginePresetId presetId)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Seed;
                hash = hash * 31 + Year;
                hash = hash * 31 + Quarter;
                hash = hash * 31 + (int)stageId;
                hash = hash * 31 + (int)presetId;
                return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
            }
        }

        private static string CreateTargetPathId(ResearchStageId stageId, int mapSeed)
        {
            int pathIndex = mapSeed % 3 + 1;
            return $"{stageId}_Path_{pathIndex}";
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
                hash = hash * 31 + (int)designEntry.StageId;
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

        private static void GetGradeReward(ResearchGrade grade, out int immediateFunding, out int quarterlyFundingDelta)
        {
            switch (grade)
            {
                case ResearchGrade.S:
                    immediateFunding = 900;
                    quarterlyFundingDelta = 150;
                    break;
                case ResearchGrade.A:
                    immediateFunding = 600;
                    quarterlyFundingDelta = 100;
                    break;
                case ResearchGrade.B:
                    immediateFunding = 400;
                    quarterlyFundingDelta = 50;
                    break;
                case ResearchGrade.C:
                    immediateFunding = 150;
                    quarterlyFundingDelta = 0;
                    break;
                default:
                    immediateFunding = 0;
                    quarterlyFundingDelta = -100;
                    break;
            }
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
    }
}
