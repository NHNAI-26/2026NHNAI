using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Border.Research.Tests
{
    public sealed class ResearchPrototypeModelTests
    {
        [SetUp]
        public void SetUp()
        {
            ResearchFlowSession.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            ResearchFlowSession.ResetForTests();
        }

        [Test]
        public void Reset_CreatesTenEnginePresetSlotsButOnlyFirstIsUnlocked()
        {
            var model = new ResearchPrototypeModel();

            Assert.That(ResearchPrototypeModel.GetEnginePresetConfigs().Count, Is.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
            Assert.That(model.EnginePresets, Has.Length.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
            Assert.That(model.ActiveEnginePresetCount, Is.EqualTo(1));

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                Assert.That(config.NormalResearchCost, Is.EqualTo(ResearchPrototypeModel.EngineNormalResearchCost));
                Assert.That(config.FocusedResearchCost, Is.EqualTo(ResearchPrototypeModel.EngineFocusedResearchCost));
                Assert.That(config.InstallCost, Is.EqualTo(ResearchPrototypeModel.EngineInstallCost));
            }

            for (int i = 0; i < model.EnginePresets.Length; i++)
            {
                EnginePresetState preset = model.EnginePresets[i];
                Assert.That(preset.Completion, Is.EqualTo(0));
                Assert.That(preset.FuelCapacity, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.Cooling, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.MaxOutput, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.IgnitionReliability, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.Unlocked, Is.EqualTo(i == 0));
            }
        }

        [Test]
        public void Reset_CreatesFiveMissionSlotsWithOnlyLowAltitudeUnlocked()
        {
            var model = new ResearchPrototypeModel();

            Assert.That(ResearchPrototypeModel.GetMissionConfigs().Count, Is.EqualTo(5));
            Assert.That(model.Missions, Has.Length.EqualTo(5));
            Assert.That(model.GetMission(LaunchMissionId.LowAltitude).Unlocked, Is.True);
            Assert.That(model.GetCurrentMission(), Is.EqualTo(LaunchMissionId.LowAltitude));
            Assert.That(model.GetMission(LaunchMissionId.HighAltitude).Unlocked, Is.False);
            Assert.That(model.GetMission(LaunchMissionId.TargetZone).Unlocked, Is.False);
            Assert.That(model.GetMission(LaunchMissionId.ZoneHold).Unlocked, Is.False);
            Assert.That(model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked, Is.False);
            Assert.That(ResearchPrototypeModel.GetNextMission(LaunchMissionId.ZoneHold), Is.EqualTo(LaunchMissionId.LowPowerZoneHold));
        }

        [Test]
        public void CreateNewEnginePreset_UnlocksNextSlotFor150WithoutTime()
        {
            var model = new ResearchPrototypeModel();
            int funds = model.Funds;
            int remainingTurns = model.RemainingTurns;

            ResearchActionResult result = model.CreateNewEnginePreset(out EnginePresetId presetId);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(presetId, Is.EqualTo(EnginePresetId.Engine02));
            Assert.That(model.ActiveEnginePresetCount, Is.EqualTo(2));
            Assert.That(model.IsEnginePresetUnlocked(EnginePresetId.Engine02), Is.True);
            Assert.That(model.Funds, Is.EqualTo(funds - ResearchPrototypeModel.NewEnginePresetCost));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
        }

        [Test]
        public void BalanceConfig_OverridesResearchGainVisibilityAndLaunchReward()
        {
            var config = new ResearchBalanceConfig(
                ResearchPrototypeModel.InitialFunds,
                ResearchPrototypeModel.InitialQuarterlyFunding,
                ResearchPrototypeModel.MinQuarterlyFunding,
                ResearchPrototypeModel.MaxQuarterlyFunding,
                ResearchPrototypeModel.ResearchCompletionGain,
                ResearchPrototypeModel.EngineNormalResearchCost,
                ResearchPrototypeModel.EngineFocusedResearchCost,
                ResearchPrototypeModel.NewEnginePresetCost,
                ResearchPrototypeModel.EngineInstallCost,
                ResearchPrototypeModel.CreateDefaultMissionConfigs(),
                new[] { new ResearchScoreRewardBand(0, 7) },
                new[] { new ResearchScoreRewardBand(0, 17) },
                new[] { new ResearchGradeReward(ResearchGrade.B, 123, 45) },
                publicSuccessModifier: -22,
                privateSuccessModifier: 11);
            var model = new ResearchPrototypeModel(balanceConfig: config);
            int publicChance = model.CalculateSuccessChance(model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new[] { 1 }, 50, TestVisibility.Public));
            int privateChance = model.CalculateSuccessChance(model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new[] { 1 }, 50, TestVisibility.Private));

            Assert.That(model.CalculateResearchStatGain(false, 100), Is.EqualTo(7));
            Assert.That(model.CalculateResearchStatGain(true, 100), Is.EqualTo(17));
            Assert.That(privateChance - publicChance, Is.EqualTo(33));
            Assert.That(config.GetLaunchReward(ResearchGrade.B).ImmediateFunding, Is.EqualTo(123));
            Assert.That(config.GetLaunchReward(ResearchGrade.B).QuarterlyFundingDelta, Is.EqualTo(45));
        }

        [Test]
        public void CreateNewEnginePreset_StopsAtTenPresets()
        {
            var model = new ResearchPrototypeModel();

            for (int i = 1; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
            {
                Assert.That(model.CreateNewEnginePreset(out _), Is.EqualTo(ResearchActionResult.Success));
            }

            ResearchActionResult result = model.CreateNewEnginePreset(out EnginePresetId presetId);

            Assert.That(result, Is.EqualTo(ResearchActionResult.EnginePresetLimitReached));
            Assert.That(presetId, Is.EqualTo(EnginePresetId.Engine10));
            Assert.That(model.ActiveEnginePresetCount, Is.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
        }

        [Test]
        public void ExecuteEngineResearch_Normal_IncreasesSelectedPresetOnly()
        {
            var model = new ResearchPrototypeModel();
            UnlockPreset(model, EnginePresetId.Engine04);
            UnlockPreset(model, EnginePresetId.Engine05);
            EnginePresetState selected = model.GetEnginePreset(EnginePresetId.Engine04);
            EnginePresetState untouched = model.GetEnginePreset(EnginePresetId.Engine05);
            int untouchedCompletion = untouched.Completion;
            int untouchedStat = untouched.Cooling;
            int fundsBeforeResearch = model.Funds;

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine04, EngineStatId.Cooling, false, 65);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(selected.Completion, Is.EqualTo(ResearchPrototypeModel.ResearchCompletionGain));
            Assert.That(selected.Cooling, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 13));
            Assert.That(untouched.Completion, Is.EqualTo(untouchedCompletion));
            Assert.That(untouched.Cooling, Is.EqualTo(untouchedStat));
            Assert.That(model.Funds, Is.EqualTo(fundsBeforeResearch - ResearchPrototypeModel.EngineNormalResearchCost + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        [Test]
        public void ExecuteEngineResearch_Focused_UsesHighScoreRewardAndSameCompletionGain()
        {
            var model = new ResearchPrototypeModel();
            UnlockPreset(model, EnginePresetId.Engine02);

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.MaxOutput, true, 85);

            EnginePresetState selected = model.GetEnginePreset(EnginePresetId.Engine02);
            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(selected.Completion, Is.EqualTo(ResearchPrototypeModel.ResearchCompletionGain));
            Assert.That(selected.MaxOutput, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 26));
        }

        [Test]
        public void ExecuteEngineResearch_WhenCompletionMaxed_ReturnsEngineCompletionMaxed()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Completion = ResearchPrototypeModel.MaxEngineCompletion;
            int funds = model.Funds;

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 100);

            Assert.That(result, Is.EqualTo(ResearchActionResult.EngineCompletionMaxed));
            Assert.That(model.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void ExecuteEngineResearch_LockedPreset_ReturnsEnginePresetLocked()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.Cooling, false, 80);

            Assert.That(result, Is.EqualTo(ResearchActionResult.EnginePresetLocked));
            Assert.That(model.GetEnginePreset(EnginePresetId.Engine02).Completion, Is.EqualTo(0));
        }

        [Test]
        public void WaitQuarter_KeepsFundingAndQuarterBehavior()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.WaitQuarter();

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Year, Is.EqualTo(2018));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        [Test]
        public void TryEnterDesign_EngineCompletionZero_EntersDesignAndConsumesLaunchCost()
        {
            var model = new ResearchPrototypeModel();
            int funds = model.Funds;

            ResearchActionResult result = model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(data.SelectedEngineCompletion, Is.EqualTo(0));
            Assert.That(data.LaunchCostPaid, Is.True);
            Assert.That(model.Funds, Is.EqualTo(funds - data.LaunchCost));
        }

        [Test]
        public void TryEnterDesign_WhenReady_ConsumesLaunchCostOnly()
        {
            var model = new ResearchPrototypeModel();
            UnlockPreset(model, EnginePresetId.Engine03);
            model.ExecuteEngineResearch(EnginePresetId.Engine03, EngineStatId.FuelCapacity, false, 80);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine03);
            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int remainingTurns = model.RemainingTurns;
            int attemptCount = engine.AttemptCount;
            int completion = engine.Completion;

            ResearchActionResult result = model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine03, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(funds - data.LaunchCost));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
            Assert.That(engine.AttemptCount, Is.EqualTo(attemptCount));
            Assert.That(engine.Completion, Is.EqualTo(completion));
            Assert.That(data.MissionId, Is.EqualTo(LaunchMissionId.LowAltitude));
            Assert.That(data.SelectedEnginePresetId, Is.EqualTo(EnginePresetId.Engine03));
            Assert.That(data.LaunchCost, Is.EqualTo(800));
            Assert.That(data.ReservedInstallCost, Is.EqualTo(model.GetEngineInstallCost(EnginePresetId.Engine03)));
            Assert.That(data.LaunchCostPaid, Is.True);
            Assert.That(data.TargetPathId, Is.Not.Empty);
        }

        [Test]
        public void TryEnterDesign_WhenMissionLocked_ReturnsMissionLocked()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.TryEnterDesign(LaunchMissionId.HighAltitude, EnginePresetId.Engine01, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.MissionLocked));
        }

        [Test]
        public void TryEnterDesign_WhenLaunchCostMissing_ReturnsNotEnoughFunds()
        {
            var model = new ResearchPrototypeModel();
            SetFunds(model, ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost - 1);

            ResearchActionResult result = model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.NotEnoughFunds));
        }

        [Test]
        public void Visibility_ChangesSuccessChanceByTwentyPoints()
        {
            var model = new ResearchPrototypeModel();
            ResearchDesignEntryData publicEntry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new[] { 1 }, 50, TestVisibility.Public);
            ResearchDesignEntryData privateEntry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new[] { 1 }, 50, TestVisibility.Private);

            Assert.That(model.CalculateSuccessChance(privateEntry) - model.CalculateSuccessChance(publicEntry), Is.EqualTo(20));
        }

        [Test]
        public void CommitLaunch_WhenLaunchCostPaid_ConsumesInstallCostOnlyOnLaunch()
        {
            var model = new ResearchPrototypeModel();
            int[] installed = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            installed[(int)EnginePresetId.Engine01] = 2;
            model.GetMission(LaunchMissionId.LowAltitude).Unlocked = true;
            ResearchDesignEntryData entry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, installed, 70, TestVisibility.Private, true);
            int funds = model.Funds;
            int quarterlyFunding = model.QuarterlyFunding;
            int remainingTurns = model.RemainingTurns;

            ResearchActionResult result = model.CommitLaunch(entry, out ResearchLaunchResultData launchResult);

            int expectedQuarterlyFunding = Mathf.Clamp(
                quarterlyFunding + launchResult.QuarterlyFundingDelta,
                ResearchPrototypeModel.MinQuarterlyFunding,
                ResearchPrototypeModel.MaxQuarterlyFunding);
            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(launchResult.TotalCost, Is.EqualTo(ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost + ResearchPrototypeModel.EngineInstallCost * 2));
            Assert.That(launchResult.SuccessChance + launchResult.PartialChance + launchResult.FailureChance, Is.EqualTo(100));
            Assert.That(model.Funds, Is.EqualTo(funds - launchResult.ReservedInstallCost + launchResult.ImmediateFunding + expectedQuarterlyFunding));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(expectedQuarterlyFunding));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns - 1));
            Assert.That(model.GetMission(LaunchMissionId.LowAltitude).AttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void LowAltitude_SuccessUnlocksHighAltitude()
        {
            var model = new ResearchPrototypeModel();
            model.TryEnterDesign(LaunchMissionId.LowAltitude, out ResearchDesignEntryData entry);
            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.GetCurrentMission(), Is.EqualTo(LaunchMissionId.LowAltitude));
            Assert.That(model.CompleteLaunch(true, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.Grade, Is.EqualTo(ResearchGrade.B));
            Assert.That(model.GetCurrentMission(), Is.EqualTo(LaunchMissionId.HighAltitude));
        }

        [Test]
        public void LowAltitudeDesignEntry_PreservesInstalledEngineCost()
        {
            var model = new ResearchPrototypeModel();
            int[] installed = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            installed[(int)EnginePresetId.Engine01] = 4;
            ResearchDesignEntryData entry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, installed, 60, TestVisibility.Private);
            Assert.That(entry.ReservedInstallCost, Is.EqualTo(ResearchPrototypeModel.EngineInstallCost * 4));
            Assert.That(entry.InstalledEngineCounts[(int)EnginePresetId.Engine01], Is.EqualTo(4));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PhysicalLaunch_ChargesOnceAndWaitsForOutcome(bool succeeded)
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out ResearchDesignEntryData entry);
            int funds = session.Model.Funds;
            int turns = session.Model.RemainingTurns;
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasActiveLaunch, Is.True);
            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.False);
            Assert.That(session.Model.Funds, Is.EqualTo(funds - entry.ReservedInstallCost));
            Assert.That(session.Model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(session.Model.GetMission(LaunchMissionId.HighAltitude).Unlocked, Is.False);
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.LaunchInProgress));
            Assert.That(session.Model.WaitQuarter(), Is.EqualTo(ResearchActionResult.LaunchInProgress));
            Assert.That(session.Model.TotalLaunches, Is.EqualTo(1));
            Assert.That(session.CompleteActiveLaunch(succeeded, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.Grade, Is.EqualTo(succeeded ? ResearchGrade.B : ResearchGrade.F));
            Assert.That(session.HasActiveLaunch, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.True);
            Assert.That(session.Model.RemainingTurns, Is.EqualTo(turns - 1));
            Assert.That(session.Model.FailedLaunches, Is.EqualTo(succeeded ? 0 : 1));
            Assert.That(session.Model.GetCurrentMission(), Is.EqualTo(succeeded ? LaunchMissionId.HighAltitude : LaunchMissionId.LowAltitude));
            int settledFunds = session.Model.Funds;
            Assert.That(session.CompleteActiveLaunch(succeeded, out _), Is.EqualTo(ResearchActionResult.NoPendingDesignEntry));
            Assert.That(session.Model.Funds, Is.EqualTo(settledFunds));
            Assert.That(session.Model.RemainingTurns, Is.EqualTo(turns - 1));
        }

        [Test]
        public void PhysicalLaunch_FiveSuccessesCompleteCampaignInOrder()
        {
            var model = new ResearchPrototypeModel();
            for (int id = 1; id <= 5; id++)
            {
                Assert.That(model.GetCurrentMission(), Is.EqualTo((LaunchMissionId)id));
                SetFunds(model, 10000);
                Assert.That(model.TryEnterDesign((LaunchMissionId)id, out ResearchDesignEntryData entry), Is.EqualTo(ResearchActionResult.Success));
                Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
                Assert.That(model.HasGameEnded, Is.False);
                Assert.That(model.CompleteLaunch(true, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
                Assert.That(result.FinalMissionWon, Is.EqualTo(id == 5));
            }
            Assert.That(model.GameWon, Is.True);
            Assert.That(model.TotalLaunches, Is.EqualTo(5));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 5));
        }

        [Test]
        public void BalanceConfig_LegacyMissionZeroIsIgnoredAndFinalMissionIsPreserved()
        {
            ResearchBalanceConfig defaults = ResearchBalanceConfig.CreateDefault();
            var legacy = new LaunchMissionConfig[6];
            legacy[0] = new LaunchMissionConfig(LaunchMissionId.StaticFire, "removed", 1, "removed", 0);
            for (int i = 0; i < defaults.MissionConfigs.Length; i++) legacy[i + 1] = defaults.MissionConfigs[i];
            legacy[5] = new LaunchMissionConfig(LaunchMissionId.LowPowerZoneHold, "final", 2345, "legacy", 0.4);
            var config = new ResearchBalanceConfig(1000, 100, 0, 1000, 10, 100, 100, 100, 100, legacy);
            Assert.That(config.MissionConfigs, Has.Length.EqualTo(5));
            Assert.That(config.MissionConfigs[0].Id, Is.EqualTo(LaunchMissionId.LowAltitude));
            Assert.That(config.MissionConfigs[4].LaunchCost, Is.EqualTo(2345));
            Assert.That(config.MissionConfigs[0].RequirementText, Is.EqualTo("기본 해금"));
        }

        [Test]
        public void LowPowerZoneHold_BGradeOrBetterWinsGame()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                var model = new ResearchPrototypeModel(seed);
                EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
                engine.Completion = ResearchPrototypeModel.MaxEngineCompletion;
                engine.FuelCapacity = 100;
                engine.Cooling = 100;
                engine.MaxOutput = 100;
                engine.IgnitionReliability = 100;
                model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
                int[] installed = new int[ResearchPrototypeModel.MaxEnginePresetCount];
                installed[(int)EnginePresetId.Engine01] = 1;
                ResearchDesignEntryData entry = model.CreateDesignEntry(LaunchMissionId.LowPowerZoneHold, EnginePresetId.Engine01, installed, 100, TestVisibility.FinalMission);

                model.CommitLaunch(entry, out ResearchLaunchResultData result);

                if (result.Grade > ResearchGrade.B)
                {
                    continue;
                }

                Assert.That(result.FinalMissionWon, Is.True);
                Assert.That(model.HasGameEnded, Is.True);
                Assert.That(model.GameWon, Is.True);
                return;
            }

            Assert.Fail("No deterministic seed produced a B-or-better final mission result.");
        }

        [Test]
        public void MiniGameScoring_ClampsAllScoresToValidRange()
        {
            Assert.That(ResearchMiniGameController.CalculateFuelCapacityScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(10, 10, -1f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateMaxOutputScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateIgnitionReliabilityScore(20, 4, -1f), Is.InRange(0, 100));
        }

        [Test]
        public void MiniGameScoring_FuelCapacity_RewardAccuracy()
        {
            int accurate = ResearchMiniGameController.CalculateFuelCapacityScore(0.01f, 0.02f, 0.01f);
            int inaccurate = ResearchMiniGameController.CalculateFuelCapacityScore(0.35f, 0.4f, 0.3f);

            Assert.That(accurate, Is.GreaterThan(inaccurate));
            Assert.That(accurate, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_Cooling_RewardsCorrectFastInput()
        {
            int good = ResearchMiniGameController.CalculateCoolingScore(4, 0, 0.25f);
            int poor = ResearchMiniGameController.CalculateCoolingScore(1, 3, 1.5f);

            Assert.That(good, Is.GreaterThan(poor));
            Assert.That(good, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_Cooling_SlowCorrectInputCannotReceiveTopReward()
        {
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(4, 0, 0.35f), Is.EqualTo(100));
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(4, 0, 0.9f), Is.EqualTo(60));
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(0, 0, 0f), Is.Zero);
        }

        [Test]
        public void MiniGameScoring_MaxOutput_RewardsSafeZoneCenter()
        {
            int centered = ResearchMiniGameController.CalculateMaxOutputScoreFromFills(0.35f, 0.6f, 0.85f);
            int outside = ResearchMiniGameController.CalculateMaxOutputScoreFromFills(0.05f, 0.95f, 0.2f);

            Assert.That(centered, Is.GreaterThan(outside));
            Assert.That(centered, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_IgnitionReliability_RewardsSequenceAccuracy()
        {
            int correct = ResearchMiniGameController.CalculateIgnitionReliabilityScore(9, 9, 0.35f);
            int wrong = ResearchMiniGameController.CalculateIgnitionReliabilityScore(3, 9, 1.4f);

            Assert.That(correct, Is.GreaterThan(wrong));
            Assert.That(correct, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameTargets_UseSeededRandomExceptMaxOutput()
        {
            var fuelHostA = new GameObject("Fuel Mini Game Test Host A");
            var fuelHostB = new GameObject("Fuel Mini Game Test Host B");
            var fuelHostC = new GameObject("Fuel Mini Game Test Host C");
            var coolingHost = new GameObject("Cooling Mini Game Test Host");
            var ignitionHost = new GameObject("Ignition Mini Game Test Host");

            try
            {
                ResearchMiniGameController fuelA = fuelHostA.AddComponent<ResearchMiniGameController>();
                ResearchMiniGameController fuelB = fuelHostB.AddComponent<ResearchMiniGameController>();
                ResearchMiniGameController fuelC = fuelHostC.AddComponent<ResearchMiniGameController>();
                ResearchMiniGameController cooling = coolingHost.AddComponent<ResearchMiniGameController>();
                ResearchMiniGameController ignition = ignitionHost.AddComponent<ResearchMiniGameController>();

                fuelA.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
                fuelB.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
                fuelC.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 78, _ => { });
                cooling.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, _ => { });
                ignition.InitializeForTests(EnginePresetId.Engine01, EngineStatId.IgnitionReliability, false, 80, _ => { });

                Assert.That(fuelA.GetFuelTargetForTests(), Is.InRange(0.38f, 0.84f));
                Assert.That(fuelA.GetFuelTargetForTests(), Is.EqualTo(fuelB.GetFuelTargetForTests()));
                Assert.That(fuelA.GetFuelTargetForTests(), Is.Not.EqualTo(fuelC.GetFuelTargetForTests()));
                Assert.That(cooling.GetActiveValveIndexForTests(), Is.InRange(0, 3));
                Assert.That(ignition.GetIgnitionSequenceForTests(), Has.Length.EqualTo(2));
                Assert.That(ResearchMiniGameController.CalculateMaxOutputScoreFromFills(0.35f, 0.6f, 0.85f), Is.GreaterThanOrEqualTo(80));
            }
            finally
            {
                Object.DestroyImmediate(fuelHostA);
                Object.DestroyImmediate(fuelHostB);
                Object.DestroyImmediate(fuelHostC);
                Object.DestroyImmediate(coolingHost);
                Object.DestroyImmediate(ignitionHost);
            }
        }

        [Test]
        public void MiniGameController_ForceComplete_ShowsResultBeforeCallback()
        {
            var host = new GameObject("Mini Game Test Host");
            ResearchMiniGameResult completedResult = default;
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine03, EngineStatId.Cooling, true, result =>
                {
                    completedResult = result;
                    completed = true;
                });

                controller.ForceCompleteForTests(125);

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingResult, Is.True);

                controller.ForceDismissForTests();

                Assert.That(completed, Is.True);
                Assert.That(completedResult.PresetId, Is.EqualTo(EnginePresetId.Engine03));
                Assert.That(completedResult.StatId, Is.EqualTo(EngineStatId.Cooling));
                Assert.That(completedResult.Focused, Is.True);
                Assert.That(completedResult.Score, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameScoring_FuelJudgement_UsesAccuracyBands()
        {
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.02f), Is.EqualTo("Perfect!"));
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.03f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.08f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.16f), Is.EqualTo("Good"));
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.17f), Is.EqualTo("Miss"));
        }

        [Test]
        public void MiniGameScoring_OutputJudgement_UsesAccuracyBands()
        {
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.02f), Is.EqualTo("Perfect!"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.03f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.08f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.16f), Is.EqualTo("Good"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.17f), Is.EqualTo("Miss"));
        }

        [Test]
        public void MiniGameController_FuelAttemptShowsJudgementBeforeNextStep()
        {
            var host = new GameObject("Mini Game Test Host");
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => completed = true);

                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.True);
                Assert.That(controller.GetStateTextForTests(), Does.Contain("판정 1/3"));

                controller.ForceAdvanceFuelJudgementForTests();

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.False);
                Assert.That(controller.IsShowingResult, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_FuelIgnoresExtraPointerUpDuringFinalJudgement()
        {
            var host = new GameObject("Mini Game Test Host");
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => completed = true);

                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());
                controller.ForceAdvanceFuelJudgementForTests();
                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());
                controller.ForceAdvanceFuelJudgementForTests();
                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());

                Assert.That(controller.IsShowingFuelJudgementForTests, Is.True);
                Assert.DoesNotThrow(() => controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests()));

                controller.ForceAdvanceFuelJudgementForTests();

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingResult, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_OutputShowsJudgementBeforeNextStep()
        {
            var host = new GameObject("Mini Game Test Host");
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => completed = true);

                controller.RecordOutputStageForTests(0.35f);

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                Assert.That(controller.GetStateTextForTests(), Does.Contain("판정 1/3"));

                controller.ForceAdvanceOutputJudgementForTests();

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.False);
                Assert.That(controller.IsShowingResult, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_DismissesZeroScoreAsValidCompletion()
        {
            var host = new GameObject("Mini Game Test Host");
            ResearchMiniGameResult completedResult = default;
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, result =>
                {
                    completedResult = result;
                    completed = true;
                });

                controller.ForceCompleteForTests(0);
                controller.ForceDismissForTests();

                Assert.That(completed, Is.True);
                Assert.That(completedResult.Score, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameStateText_AppendsExampleLineWhenRequested()
        {
            string firstFrame = ResearchMiniGameController.FormatStateText("밸브 1/4", true);
            string nextFrame = ResearchMiniGameController.FormatStateText("밸브 1/4", true);

            Assert.That(firstFrame, Is.EqualTo(nextFrame));
            Assert.That(firstFrame, Does.Contain("밸브 1/4"));
            Assert.That(firstFrame, Does.Contain("예시"));
        }

        [Test]
        public void MiniGameTimer_OnlyCoolingShowsNineSecondLimitAndStartsImmediately()
        {
            var fuelHost = new GameObject("Fuel Mini Game Test Host");
            var coolingHost = new GameObject("Cooling Mini Game Test Host");

            try
            {
                ResearchMiniGameController fuel = fuelHost.AddComponent<ResearchMiniGameController>();
                ResearchMiniGameController cooling = coolingHost.AddComponent<ResearchMiniGameController>();

                fuel.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
                cooling.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, _ => { });

                Assert.That(fuel.GetTimerTextForTests(), Is.Empty);
                Assert.That(cooling.GetTimerTextForTests(), Is.EqualTo("남은 시간 9초"));
                Assert.That(cooling.GetStateTextForTests(), Does.Contain("밸브 1/4"));
                Assert.That(cooling.GetStateTextForTests(), Does.Contain("예시"));
            }
            finally
            {
                Object.DestroyImmediate(fuelHost);
                Object.DestroyImmediate(coolingHost);
            }
        }

        [Test]
        public void FlowSession_StoresUpdatesAndClearsPendingDesignEntry()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            UnlockPreset(session.Model, EnginePresetId.Engine02);

            ResearchActionResult result = session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine02, out ResearchDesignEntryData data);
            ResearchDesignEntryData updated = session.Model.CreateDesignEntry(data.MissionId, data.SelectedEnginePresetId, data.InstalledEngineCounts, 80, TestVisibility.Public, data.LaunchCostPaid);
            session.UpdatePendingDesignEntry(updated);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.True);
            Assert.That(session.PendingDesignEntry.DesignFit, Is.EqualTo(80));

            session.ClearPendingDesignEntry();
            Assert.That(session.HasPendingDesignEntry, Is.False);
        }

        [Test]
        public void FlowSession_CommitPendingDesignLaunch_ClearsPendingAndStoresLaunchResult()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _);

            ResearchActionResult result = session.CommitPendingDesignLaunch(out ResearchLaunchResultData launchResult);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.True);
            Assert.That(session.LastLaunchResult.Roll, Is.EqualTo(launchResult.Roll));
        }

        [Test]
        public void OperationUI_InitialRender_ShowsFirstEngineWithoutExtraMissionDetails()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                Assert.That(FindButton(host.transform, "EngineCard_Engine01").interactable, Is.True);
                Assert.That(FindButton(host.transform, "EngineCard_Engine01").gameObject.activeSelf, Is.True);
                Assert.That(FindButton(host.transform, "EngineCard_Engine10").gameObject.activeSelf, Is.False);
                Assert.That(FindButton(host.transform, "CreateEnginePresetButton").interactable, Is.True);
                foreach (string name in new[] { "SelectedMissionText", "SelectedRequirementText", "RiskText", "StatInsightText" })
                    Assert.That(FindTransform(host.transform, name), Is.Null, name);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_CreateEnginePresetButton_RevealsNextPreset()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                FindButton(host.transform, "CreateEnginePresetButton").onClick.Invoke();

                Assert.That(controller.Model.ActiveEnginePresetCount, Is.EqualTo(2));
                Assert.That(FindButton(host.transform, "EngineCard_Engine02").gameObject.activeSelf, Is.True);
                Assert.That(controller.SelectedEnginePreset, Is.EqualTo(EnginePresetId.Engine02));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_UsesPreplacedEngineCardsBeforeConfiguredFallback()
        {
            var host = new GameObject("Research UI Test Host");
            Button engineTemplate = CreateCardTemplate("Engine Card Template", false);
            Button launchTemplate = CreateCardTemplate("Mission Card Template", true);

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.ConfigureCardPrefabsForTests(engineTemplate, launchTemplate);
                controller.InitializeForTests();

                Assert.That(FindButton(host.transform, "EngineCard_Engine01"), Is.Not.Null);
                Assert.That(FindTransform(FindButton(host.transform, "EngineCard_Engine01").transform, "PrefabMarker"), Is.Null);
                Assert.That(FindTransform(host.transform, "MissionCard_StaticFire"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(engineTemplate.gameObject);
                Object.DestroyImmediate(launchTemplate.gameObject);
            }
        }

        [Test]
        public void OperationUI_ResearchButtonStartsMiniGameAndCompletionUpdatesSelectedEngine()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                FindButton(host.transform, "StartDevelopmentButton").onClick.Invoke();

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Completion, Is.EqualTo(0));
                Assert.That(controller.GetActiveMiniGameControllerForTests(), Is.Not.Null);

                controller.GetActiveMiniGameControllerForTests().ForceCompleteForTests(65);

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Completion, Is.EqualTo(0));

                controller.GetActiveMiniGameControllerForTests().ForceDismissForTests();

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Completion, Is.EqualTo(ResearchPrototypeModel.ResearchCompletionGain));
                Assert.That(GetText(FindText(host.transform, "SelectedEngineText")), Does.Contain("완성도"));
                Assert.That(GetText(FindText(host.transform, "SelectedEngineText")), Does.Not.Contain("Lv."));
                Assert.That(GetText(FindText(host.transform, "SelectedEngineText")), Does.Not.Contain("시험 최고"));
                Assert.That(GetText(FindText(FindButton(host.transform, "EngineCard_Engine01").transform, "Detail")), Does.Not.Contain("최고"));
                Assert.That(FindTransform(host.transform, "SelectedRequirementText"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_EnterDesignStartsTransitionBeforeOpeningDesignScreen()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            int funds = session.Model.Funds;
            int remainingTurns = session.Model.RemainingTurns;
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                GameObject dialog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchTestVisibilityDialog.prefab"), host.transform);
                var binding = new SerializedObject(controller);
                binding.FindProperty("visibilityDialog").objectReferenceValue = dialog.GetComponent<ResearchTestVisibilityDialog>();
                binding.ApplyModifiedPropertiesWithoutUndo();

                FindButton(host.transform, "EnterDesignButton").onClick.Invoke();
                Assert.That(session.HasPendingDesignEntry, Is.False);
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                FindButton(dialog.transform, "ConfirmButton").onClick.Invoke();

                Assert.That(controller.Model, Is.SameAs(session.Model));
                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.ResearchScreenName));
                Assert.That(controller.IsTransitioningToDesignForTests(), Is.True);
                Assert.That(FindButton(host.transform, "EnterDesignButton").interactable, Is.False);
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Null);
                Assert.That(session.HasPendingDesignEntry, Is.True);
                Assert.That(session.Model.Funds, Is.EqualTo(funds - session.PendingDesignEntry.LaunchCost));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns));

                controller.CompleteDesignTransitionForTests();

                Assert.That(controller.IsTransitioningToDesignForTests(), Is.False);
                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.DesignScreenName));
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void OperationUI_DebugEnterDesignBypassesResearchGateAndStartsTransition()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                controller.EnterDesignDebugForEditor();

                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.ResearchScreenName));
                Assert.That(controller.IsTransitioningToDesignForTests(), Is.True);
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Null);
                Assert.That(session.HasPendingDesignEntry, Is.True);
                Assert.That(session.Model.GetEnginePreset(EnginePresetId.Engine01).Completion, Is.GreaterThanOrEqualTo(30));

                controller.CompleteDesignTransitionForTests();

                Assert.That(controller.IsTransitioningToDesignForTests(), Is.False);
                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.DesignScreenName));
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

#endif
        [Test]
        public void DesignScreenController_WithoutPendingData_RequestsResearchReturn()
        {
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.InitializeForTests();

                Assert.That(controller.RequestedResearchReturn, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DesignScreenController_ReturnToResearch_ClearsOnlyPendingData()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _);
            int funds = session.Model.Funds;
            int remainingTurns = session.Model.RemainingTurns;
            bool returned = false;
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.Initialize(session, () => returned = true);

                controller.ReturnToResearch();

                Assert.That(session.HasPendingDesignEntry, Is.False);
                Assert.That(controller.RequestedResearchReturn, Is.True);
                Assert.That(returned, Is.True);
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DesignScreenController_LaunchCommitsResultAndRequestsResearchReturn()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _);
            int remainingTurns = session.Model.RemainingTurns;
            bool returned = false;
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.Initialize(session, () => returned = true);

                Assert.That(FindButton(host.transform, "LaunchButton").interactable, Is.True);
                ResearchActionResult result = controller.LaunchForTests(out ResearchLaunchResultData launchResult);

                Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
                Assert.That(returned, Is.True);
                Assert.That(controller.RequestedResearchReturn, Is.True);
                Assert.That(session.HasPendingDesignEntry, Is.False);
                Assert.That(session.HasLastLaunchResult, Is.True);
                Assert.That(session.LastLaunchResult.Roll, Is.EqualTo(launchResult.Roll));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns - 1));
                Assert.That(session.Model.GetMission(LaunchMissionId.LowAltitude).AttemptCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ResultReportController_CloseButtonInvokesCallback()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _);
            session.CommitPendingDesignLaunch(out ResearchLaunchResultData launchResult);
            GameObject host = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchResultReport.prefab"));
            bool closed = false;

            try
            {
                ResearchResultReportController controller = host.GetComponent<ResearchResultReportController>();
                controller.Initialize(session, launchResult, () => closed = true);

                FindButton(host.transform, "CloseButton").onClick.Invoke();

                Assert.That(closed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EndingController_RestartButtonInvokesCallback()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            GameObject host = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchEnding.prefab"));
            bool restarted = false;

            try
            {
                ResearchEndingController controller = host.GetComponent<ResearchEndingController>();
                controller.Initialize(session, () => restarted = true);

                FindButton(host.transform, "RestartButton").onClick.Invoke();

                Assert.That(restarted, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void SetFunds(ResearchPrototypeModel model, int funds)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(ResearchPrototypeModel).GetField("<Funds>k__BackingField", Flags);
            Assert.That(field, Is.Not.Null);
            field.SetValue(model, funds);
        }

        private static void UnlockPreset(ResearchPrototypeModel model, EnginePresetId presetId)
        {
            while (!model.IsEnginePresetUnlocked(presetId))
            {
                ResearchActionResult result = model.CreateNewEnginePreset(out _);
                Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            }
        }

        private static Button FindButton(Transform root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail($"Button not found: {name}");
            return null;
        }

        private static Button CreateCardTemplate(string name, bool includeRequirement)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.AddComponent<Image>();
            Button button = root.AddComponent<Button>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            CreateTemplateText("Title", content.transform);
            if (includeRequirement)
            {
                CreateTemplateText("Requirement", content.transform);
            }

            CreateTemplateText("Detail", content.transform);

            var marker = new GameObject("PrefabMarker", typeof(RectTransform));
            marker.transform.SetParent(root.transform, false);
            return button;
        }

        private static void CreateTemplateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<TextMeshProUGUI>();
        }

        private static Transform FindTransform(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Component FindText(Transform root, string name)
        {
            foreach (Component text in root.GetComponentsInChildren<Component>(true))
            {
                if (text.name == name && text.GetType().FullName == "TMPro.TextMeshProUGUI")
                {
                    return text;
                }
            }

            Assert.Fail($"Text not found: {name}");
            return null;
        }

        private static string GetText(Component text)
        {
            PropertyInfo property = text.GetType().GetProperty("text");
            Assert.That(property, Is.Not.Null);
            return (string)property.GetValue(text);
        }
    }
}
