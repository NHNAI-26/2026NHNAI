using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public void Reset_CreatesTenEnginePresetSlotsWithNoneUnlocked()
        {
            var model = new ResearchPrototypeModel();

            Assert.That(ResearchPrototypeModel.GetEnginePresetConfigs().Count, Is.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
            Assert.That(model.EnginePresets, Has.Length.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
            Assert.That(model.ActiveEnginePresetCount, Is.Zero);

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
                Assert.That(preset.Unlocked, Is.False);
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
        public void CreateNewEnginePreset_UnlocksFirstSlotFor150WithoutTime()
        {
            var model = new ResearchPrototypeModel();
            int funds = model.Funds;
            int remainingTurns = model.RemainingTurns;

            ResearchActionResult result = model.CreateNewEnginePreset(out EnginePresetId presetId);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(presetId, Is.EqualTo(EnginePresetId.Engine01));
            Assert.That(model.ActiveEnginePresetCount, Is.EqualTo(1));
            Assert.That(model.IsEnginePresetUnlocked(EnginePresetId.Engine01), Is.True);
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

            for (int i = 0; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
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
            Assert.That(selected.Cooling, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 25));
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
            Assert.That(selected.MaxOutput, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 50));
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
        public void TryEnterDesign_RequiresCreatedEngineThenAllowsCompletionZero()
        {
            var model = new ResearchPrototypeModel();
            int funds = model.Funds;

            Assert.That(model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out _),
                Is.EqualTo(ResearchActionResult.EnginePresetLocked));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.CreateNewEnginePreset(out EnginePresetId presetId), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(presetId, Is.EqualTo(EnginePresetId.Engine01));

            ResearchActionResult result = model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(data.SelectedEngineCompletion, Is.EqualTo(0));
            Assert.That(data.LaunchCostPaid, Is.True);
            Assert.That(model.Funds, Is.EqualTo(funds - ResearchPrototypeModel.NewEnginePresetCost - data.LaunchCost));
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
            Assert.That(data.LaunchCost, Is.EqualTo(50));
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
                ResearchDesignEntryData entry = model.CreateDesignEntry(LaunchMissionId.LowPowerZoneHold, EnginePresetId.Engine01, installed, 100, TestVisibility.Public);

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
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(-2f, 10f), Is.Zero);
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(-1f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateMaxOutputScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateIgnitionReliabilityScore(20, 4, -1f), Is.InRange(0, 100));
        }

        [TestCase(0f, 0f, 0)]
        [TestCase(0.5f, 0f, 0)]
        [TestCase(1f, 0f, 41)]
        [TestCase(1f, 1f, 0)]
        [TestCase(1f, 2f, 0)]
        [TestCase(1f, 3f, 0)]
        public void MiniGameScoring_FuelCapacity_RewardsFullChargeAndPenalizesOverflow(float fill, float overflow, int expected)
        {
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(fill, overflow), Is.EqualTo(expected));
        }

        [Test]
        public void MiniGameFuel_PeaksAtRightmostWhiteLineOfSkyBluePassBand()
        {
            float start = ResearchMiniGameController.FuelPassStart;
            float end = ResearchMiniGameController.FuelPassEnd;
            Assert.That(ResearchMiniGameController.GetFuelNeedleAngle(0f), Is.EqualTo(69f));
            Assert.That(ResearchMiniGameController.GetFuelNeedleAngle(1f), Is.EqualTo(-68f));
            Assert.That(ResearchMiniGameController.GetFuelNeedleAngle(start), Is.EqualTo(-39.4f).Within(0.001f));
            Assert.That(ResearchMiniGameController.GetFuelNeedleAngle(end), Is.EqualTo(-62.7f).Within(0.001f));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(start, 0f), Is.EqualTo(80));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore((start + end) * 0.5f, 0f), Is.EqualTo(90));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(end, 0f), Is.EqualTo(100));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(start - 0.001f, 0f), Is.LessThan(50));
            Assert.That(ResearchMiniGameController.CalculateFuelAttemptScore(end + 0.001f, 0f), Is.LessThan(50));
        }

        [TestCase(-1f, 100)]
        [TestCase(0f, 100)]
        [TestCase(0.4f, 60)]
        [TestCase(0.753f, 25)]
        [TestCase(1f, 0)]
        [TestCase(2f, 0)]
        public void MiniGameScoring_Cooling_RewardsLowFinalHeat(float heat, int expected)
        {
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(heat), Is.EqualTo(expected));
        }

        [TestCase(0f, 100)]
        [TestCase(0.02f, 100)]
        [TestCase(0.0201f, 80)]
        [TestCase(0.08f, 80)]
        [TestCase(0.0801f, 0)]
        [TestCase(-0.02f, 100)]
        [TestCase(-0.08f, 80)]
        public void MiniGameScoring_MaxOutput_QuantizesBothSidesOfTarget(float error, int expected)
        {
            Assert.That(ResearchMiniGameController.CalculateMaxOutputScore(error), Is.EqualTo(expected));
            Assert.That(ResearchMiniGameController.CalculateOutputAttemptScore(error, 0f), Is.EqualTo(expected));
        }

        [Test]
        public void MiniGameScoring_MaxOutput_RewardsSafeZoneCenter()
        {
            Assert.That(ResearchMiniGameController.CalculateOutputAttemptScore(0.73f, 0.73f), Is.EqualTo(100));
            Assert.That(ResearchMiniGameController.CalculateOutputAttemptScore(0.2f, 0.73f), Is.Zero);
            Assert.That(ResearchMiniGameController.CalculateMaxOutputScore(0f, 0.05f, 0.3f), Is.EqualTo(60));
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
        public void MiniGameTargets_RandomizeFuelDurationCoolingRotationAndOutputPosition()
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

                Assert.That(fuelA.GetFuelTargetForTests(), Is.EqualTo(ResearchMiniGameController.FuelPassEnd));
                Assert.That(fuelA.GetFuelDurationForTests(), Is.InRange(1.8f, 4.2f));
                Assert.That(fuelA.GetFuelDurationForTests(), Is.EqualTo(fuelB.GetFuelDurationForTests()));
                Assert.That(fuelA.GetFuelDurationForTests(), Is.Not.EqualTo(fuelC.GetFuelDurationForTests()));
                Assert.That(cooling.GetCoolingTargetForTests(), Is.InRange(3600f, 4320f));
                Assert.That(ignition.GetIgnitionSequenceForTests(), Has.Length.EqualTo(2));

                fuelA.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => { });
                fuelB.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => { });
                fuelC.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 78, _ => { });
                Assert.That(fuelA.GetOutputTargetForTests(), Is.InRange(0.08f, 0.92f));
                Assert.That(fuelA.GetOutputTargetForTests(), Is.EqualTo(fuelB.GetOutputTargetForTests()));
                Assert.That(fuelA.GetOutputTargetForTests(), Is.Not.EqualTo(fuelC.GetOutputTargetForTests()));
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
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.2f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetFuelJudgementText(0.21f), Is.EqualTo("Miss"));
        }

        [Test]
        public void MiniGameScoring_OutputJudgement_UsesAccuracyBands()
        {
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.02f), Is.EqualTo("Perfect"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.03f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.08f), Is.EqualTo("Great"));
            Assert.That(ResearchMiniGameController.GetOutputJudgementText(0.0801f), Is.EqualTo("Miss"));
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
                Assert.That(controller.GetStateTextForTests(), Does.Contain("판정 1/1"));

                controller.ForceAdvanceFuelJudgementForTests();

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.False);
                Assert.That(controller.IsShowingResult, Is.True);
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

                Assert.That(controller.IsShowingFuelJudgementForTests, Is.True);
                Assert.DoesNotThrow(() => controller.ReleaseFuelForTests());

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

                controller.RecordOutputStageForTests(controller.GetOutputTargetForTests());

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
        public void MiniGameStateText_DoesNotAccumulateExampleInstructions()
        {
            string firstFrame = ResearchMiniGameController.FormatStateText("순서 보기", true);
            string nextFrame = ResearchMiniGameController.FormatStateText(firstFrame, true);

            Assert.That(firstFrame, Is.EqualTo(nextFrame));
            Assert.That(firstFrame, Is.EqualTo("순서 보기"));
        }

        [Test]
        public void MiniGameController_FuelHoldCompletesOneAttemptAndRejectsLateRelease()
        {
            var host = new GameObject("Fuel Hold Test");
            ResearchMiniGameResult result = default;
            int callbacks = 0;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, value =>
                {
                    result = value;
                    callbacks++;
                });

                controller.ReleaseFuelForTests();
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.False);
                controller.BeginFuelFillForTests();
                controller.AdvanceTimeForTests(controller.GetFuelDurationForTests() * controller.GetFuelTargetForTests());
                controller.ReleaseFuelForTests();
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.True);
                controller.BeginFuelFillForTests();
                controller.ReleaseFuelForTests();
                controller.ForceAdvanceFuelJudgementForTests();

                // Late inputs must not create another attempt or change the final score.
                controller.ReleaseFuelForTests();
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.False);
                Assert.That(controller.IsShowingResult, Is.True);
                controller.BeginFuelFillForTests();
                controller.ReleaseFuelForTests();
                controller.ForceAdvanceFuelJudgementForTests();
                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(callbacks, Is.Zero);
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.EqualTo(100));
                Assert.That(callbacks, Is.EqualTo(1));
                controller.ForceDismissForTests();
                Assert.That(callbacks, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_FuelOverfillAutomaticallyCompletesSingleAttempt()
        {
            var host = new GameObject("Fuel Overflow Test");
            ResearchMiniGameResult result = default;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, value => result = value);
                controller.BeginFuelFillForTests();
                controller.AdvanceTimeForTests(controller.GetFuelDurationForTests() + 2.01f);
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.True);
                controller.ReleaseFuelForTests();
                controller.ForceAdvanceFuelJudgementForTests();
                controller.ReleaseFuelForTests();
                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(controller.IsShowingFuelJudgementForTests, Is.False);
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_CoolingHandlesAngleWrapReverseDeadzoneAndRedrag()
        {
            var host = new GameObject("Cooling Drag Test");
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, _ => { });
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.4f).Within(0.0001f));
                controller.RotateValveForTests(ValvePoint(-170f), true);
                controller.RotateValveForTests(ValvePoint(170f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(20f).Within(0.01f));
                Assert.That(controller.GetCoolingHeatForTests(),
                    Is.EqualTo(0.4f - 20f / controller.GetCoolingTargetForTests() * 1.2f).Within(0.0001f));
                controller.RotateValveForTests(ValvePoint(-150f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.Zero);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.4f).Within(0.0001f));
                controller.RotateValveForTests(ValvePoint(120f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(90f).Within(0.01f));
                controller.RotateValveForTests(Vector2.zero);
                controller.RotateValveForTests(ValvePoint(-60f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(90f).Within(0.01f));
                controller.ReleaseValveForTests();
                controller.RotateValveForTests(ValvePoint(-150f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(90f).Within(0.01f));
                controller.RotateValveForTests(ValvePoint(60f), true);
                controller.RotateValveForTests(ValvePoint(-30f));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(180f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [TestCase(1f)]
        [TestCase(3f)]
        public void MiniGameController_CoolingWaitsFullDurationAndCapsLastTickHeating(float finalTick)
        {
            var host = new GameObject("Cooling Goal Test");
            ResearchMiniGameResult result = default;
            int callbacks = 0;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, value =>
                {
                    result = value;
                    callbacks++;
                });
                controller.AdvanceTimeForTests(4f);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(controller.IsShowingResult, Is.False);
                controller.RotateValveForTests(ValvePoint(0f), true);
                for (int step = 1; step <= 52; step++)
                    controller.RotateValveForTests(ValvePoint(-90f * step));
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(4680f).Within(0.01f));
                Assert.That(controller.GetCoolingHeatForTests(), Is.Zero);
                Assert.That(controller.IsShowingResult, Is.False);
                controller.AdvanceTimeForTests(4f);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.4f).Within(0.0001f));
                for (int step = 53; step <= 105; step++)
                    controller.RotateValveForTests(ValvePoint(-90f * step));
                Assert.That(controller.GetCoolingHeatForTests(), Is.Zero);
                Assert.That(controller.IsShowingResult, Is.False);
                controller.AdvanceTimeForTests(finalTick);
                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(callbacks, Is.Zero);
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.EqualTo(90));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameCooling_DragImmediatelyCoolsOnlyPipeAndIdleReheatsIt()
        {
            var host = new GameObject("Cooling Pipe Heat Test");
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, _ => { });
                controller.AdvanceTimeForTests(4.5f);
                controller.RotateValveForTests(ValvePoint(0f), true);
                for (int step = 1; step <= 8; step++) controller.RotateValveForTests(ValvePoint(-90f * step));
                Assert.That(controller.IsCompleted, Is.False);
                Assert.That(controller.GetCoolingDegreesForTests(), Is.EqualTo(720f).Within(0.01f));
                float heat = 0.85f - 720f / controller.GetCoolingTargetForTests() * 1.2f;
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(heat).Within(0.0001f));
                int checkedImages = 0;
                foreach (Image image in host.GetComponentsInChildren<Image>(true))
                {
                    if (image.name == "CoolingPipe")
                    {
                        Assert.That(image.material.GetFloat("_Heat"), Is.EqualTo(heat).Within(0.0001f));
                        checkedImages++;
                    }
                    if (image.name == "CoolingValve")
                    {
                        Assert.That(image.material.GetFloat("_Heat"), Is.Zero);
                        checkedImages++;
                    }
                }
                Assert.That(checkedImages, Is.EqualTo(2));
                controller.AdvanceTimeForTests(1f);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(heat + 0.1f).Within(0.0001f));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MiniGameController_CoolingOverheatAppliesZeroScoreRewardOnce(bool automaticDismiss)
        {
            var host = new GameObject("Cooling Overheat Test");
            var model = new ResearchPrototypeModel();
            int callbacks = 0;
            int turns = model.RemainingTurns;
            int initialCooling = model.GetEnginePreset(EnginePresetId.Engine01).Cooling;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, result =>
                {
                    callbacks++;
                    Assert.That(result.Score, Is.Zero);
                    Assert.That(model.ExecuteEngineResearch(result.PresetId, result.StatId, result.Focused, result.Score),
                        Is.EqualTo(ResearchActionResult.Success));
                });
                controller.AdvanceTimeForTests(6.1f);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(1f));
                Assert.That(controller.IsShowingResult, Is.True);
                controller.RotateValveForTests(ValvePoint(0f), true);
                controller.RotateValveForTests(ValvePoint(-90f));
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(1f));
                Assert.That(callbacks, Is.Zero);
                if (automaticDismiss)
                    controller.AdvanceTimeForTests(2.01f);
                else
                    controller.ForceDismissForTests();
                controller.ForceDismissForTests();
                controller.AdvanceTimeForTests(3f);
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(model.RemainingTurns, Is.EqualTo(turns - 1));
                Assert.That(model.GetEnginePreset(EnginePresetId.Engine01).Cooling, Is.EqualTo(initialCooling + 10));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_CoolingReverseRotationOverheatsImmediately()
        {
            var host = new GameObject("Cooling Reverse Overheat Test");
            ResearchMiniGameResult result = default;
            int callbacks = 0;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 79, value =>
                {
                    result = value;
                    callbacks++;
                });
                controller.RotateValveForTests(ValvePoint(0f), true);
                for (int step = 1; step <= 48; step++)
                    controller.RotateValveForTests(ValvePoint(-90f * step));
                Assert.That(controller.GetCoolingHeatForTests(), Is.Zero);
                controller.AdvanceTimeForTests(5f);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(0.5f).Within(0.0001f));
                for (int step = 1; step <= 48 && !controller.IsShowingResult; step++)
                    controller.RotateValveForTests(ValvePoint(90f * step));
                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(controller.GetCoolingHeatForTests(), Is.EqualTo(1f));
                Assert.That(callbacks, Is.Zero);
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.Zero);
                Assert.That(callbacks, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void MiniGameController_OutputPingPongsAndUsesOneInputPerRandomizedStage()
        {
            var host = new GameObject("Output Motion Test");
            ResearchMiniGameResult result = default;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, value => result = value);
                float[] traverseTimes = { 0.9f, 0.75f, 0.6f };
                float previousTarget = -1f;
                for (int stage = 0; stage < 3; stage++)
                {
                    float target = controller.GetOutputTargetForTests();
                    Assert.That(target, Is.InRange(0.08f, 0.92f));
                    Assert.That(target, Is.Not.EqualTo(previousTarget));
                    previousTarget = target;
                    controller.AdvanceTimeForTests(traverseTimes[stage]);
                    Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(1f).Within(0.001f));
                    controller.AdvanceTimeForTests(traverseTimes[stage] * 0.5f);
                    Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(0.5f).Within(0.001f));
                    Assert.That(controller.GetOutputTargetForTests(), Is.EqualTo(target));
                    float cursor = stage == 0 ? target : stage == 1 ? target + 0.05f : (target < 0.5f ? 1f : 0f);
                    controller.RecordOutputStageForTests(cursor);
                    controller.RecordOutputStageForTests(target);
                    Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                    controller.ForceAdvanceOutputJudgementForTests();
                    Assert.That(controller.IsShowingResult, Is.EqualTo(stage == 2));
                }
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.EqualTo(60));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MiniGameController_OutputPrimaryPointerDownStopsImmediatelyAndReleaseCannotRecord(bool releaseAfterStageAdvance)
        {
            var host = new GameObject("Output Primary Pointer Test");
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => { });
                Button button = FindButton(host.transform, "PrimaryActionButton");
                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                controller.AdvanceTimeForTests(0.27f);
                float renderedCursor = controller.GetOutputCursorForTests();
                string remainingTime = GetText(FindText(host.transform, "Timer"));

                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(renderedCursor));
                controller.AdvanceTimeForTests(0.5f);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(renderedCursor));
                Assert.That(GetText(FindText(host.transform, "Timer")), Is.EqualTo(remainingTime));

                if (!releaseAfterStageAdvance)
                {
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(renderedCursor));
                }
                controller.AdvanceTimeForTests(1.49f);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                controller.AdvanceTimeForTests(0.02f);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.False);
                Assert.That(controller.IsShowingResult, Is.False);

                if (releaseAfterStageAdvance)
                {
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    Assert.That(controller.IsShowingOutputJudgementForTests, Is.False);
                }
                controller.AdvanceTimeForTests(0.3f);
                Assert.That(controller.GetOutputCursorForTests(), Is.GreaterThan(0f));
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MiniGameController_OutputFeedbackSeparatesMissAndSuccessAndRestoresOnHide(bool success)
        {
            var host = new GameObject("Output Feedback Test");
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => { });
                var group = (RectTransform)FindTransform(host.transform, "OutputGame");
                Button button = FindButton(host.transform, "PrimaryActionButton");
                Vector3 groupScale = group.localScale;
                Vector3 buttonScale = button.transform.localScale;
                var panel = (RectTransform)FindTransform(host.transform, "OutputTrack");
                Vector3 panelScale = panel.localScale;
                Vector2 position = panel.anchoredPosition;
                var cursor = FindTransform(host.transform, "OutputCursor");
                var safeZone = FindTransform(host.transform, "SafeZone");
                float cursorWorldScale = cursor.lossyScale.x;
                float targetWorldScale = safeZone.lossyScale.x;
                Transform[] fixedElements = { group, button.transform, FindTransform(host.transform, "OutputBackground"), FindTransform(host.transform, "OutputJudgementText") };
                Matrix4x4[] fixedMatrices = System.Array.ConvertAll(fixedElements, element => element.localToWorldMatrix);
                float buttonWorldScale = button.transform.lossyScale.x;
                float target = controller.GetOutputTargetForTests();
                controller.RecordOutputStageForTests(success ? target : (target < 0.5f ? 1f : 0f));
                float stoppedCursor = controller.GetOutputCursorForTests();
                Assert.That(panel.localScale.x, Is.LessThan(panelScale.x));
                Assert.That(button.transform.localScale, Is.EqualTo(buttonScale));
                Assert.That(group.localScale, Is.EqualTo(groupScale));
                Assert.That(cursor.lossyScale.x / cursorWorldScale,
                    Is.EqualTo(safeZone.lossyScale.x / targetWorldScale).Within(0.0001f));

                FieldInfo feedbackField = typeof(ResearchMiniGameController)
                    .GetField("outputFeedbackTween", BindingFlags.Instance | BindingFlags.NonPublic);
                object tween = feedbackField.GetValue(controller);
                System.Type extensions = tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions");
                extensions.GetMethod("Goto").Invoke(null, new object[] { tween, success ? 0.07f : 0.18f, false });
                if (success)
                {
                    Assert.That(panel.localScale.x, Is.GreaterThan(panelScale.x));
                    Assert.That(panel.anchoredPosition, Is.EqualTo(position));
                    Vector3 impactScale = panel.localScale;
                    Assert.That(impactScale.x / panelScale.x, Is.GreaterThan(1.13f));
                    extensions.GetMethod("Goto").Invoke(null, new object[] { tween, 0.085f, false });
                    Assert.That(panel.localScale.x, Is.LessThan(impactScale.x), "The stronger peak must immediately flow into recovery without a hold.");
                    Assert.That(panel.localScale.x, Is.GreaterThan(panelScale.x));
                    extensions.GetMethod("Goto").Invoke(null, new object[] { tween, 0.13f, false });
                    Assert.That(panel.localScale, Is.EqualTo(panelScale), "Success must return directly without a trailing rebound.");
                }
                else
                {
                    Assert.That(panel.anchoredPosition.x, Is.Not.EqualTo(position.x));
                    Assert.That(panel.localScale, Is.EqualTo(panelScale));
                }
                Assert.That(cursor.lossyScale.x / cursorWorldScale,
                    Is.EqualTo(safeZone.lossyScale.x / targetWorldScale).Within(0.0001f));
                Assert.That(button.transform.lossyScale.x, Is.EqualTo(buttonWorldScale));
                for (int i = 0; i < fixedElements.Length; i++)
                    Assert.That(fixedElements[i].localToWorldMatrix, Is.EqualTo(fixedMatrices[i]), fixedElements[i].name + " must stay still.");
                Assert.That(controller.GetOutputCursorForTests(), Is.EqualTo(stoppedCursor));
                controller.HideForReuse();
                Assert.That(group.localScale, Is.EqualTo(groupScale));
                Assert.That(panel.localScale, Is.EqualTo(panelScale));
                Assert.That(panel.anchoredPosition, Is.EqualTo(position));
                Assert.That(button.transform.localScale, Is.EqualTo(buttonScale));
                Assert.That(feedbackField.GetValue(controller), Is.Null);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void MiniGameController_OutputTimeoutRecordsZeroForEveryStage()
        {
            var host = new GameObject("Output Timeout Test");
            ResearchMiniGameResult result = default;
            int callbacks = 0;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, value =>
                {
                    result = value;
                    callbacks++;
                });
                for (int stage = 0; stage < 3; stage++)
                {
                    controller.AdvanceTimeForTests(4.99f);
                    Assert.That(controller.IsShowingOutputJudgementForTests, Is.False);
                    controller.AdvanceTimeForTests(0.02f);
                    Assert.That(controller.IsShowingOutputJudgementForTests, Is.True);
                    controller.RecordOutputStageForTests(controller.GetOutputTargetForTests());
                    controller.ForceAdvanceOutputJudgementForTests();
                    Assert.That(controller.IsShowingResult, Is.EqualTo(stage == 2));
                }
                Assert.That(callbacks, Is.Zero);
                controller.ForceDismissForTests();
                Assert.That(result.Score, Is.Zero);
                Assert.That(callbacks, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_ResultButtonAcceptsPointerClickAfterOutputAndFuelReuseOnlyOnce()
        {
            var host = new GameObject("Mini Game Result Pointer Test");
            int outputCallbacks = 0;
            int fuelCallbacks = 0;
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => outputCallbacks++);
                Button button = FindButton(host.transform, "PrimaryActionButton");
                for (int stage = 0; stage < 3; stage++)
                {
                    controller.RecordOutputStageForTests(controller.GetOutputTargetForTests());
                    controller.ForceAdvanceOutputJudgementForTests();
                }

                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                Assert.That(button.interactable, Is.True);
                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(outputCallbacks, Is.EqualTo(1));
                Assert.That(controller.IsShowingResult, Is.False);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                controller.AdvanceTimeForTests(3f);
                Assert.That(outputCallbacks, Is.EqualTo(1));

                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 78, _ => fuelCallbacks++);
                Assert.That(FindButton(host.transform, "PrimaryActionButton"), Is.SameAs(button));
                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());
                controller.ForceAdvanceFuelJudgementForTests();

                Assert.That(controller.IsShowingResult, Is.True);
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                Assert.That(button.interactable, Is.True);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(fuelCallbacks, Is.EqualTo(1));
                Assert.That(controller.IsShowingResult, Is.False);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                controller.AdvanceTimeForTests(3f);
                Assert.That(fuelCallbacks, Is.EqualTo(1));
                Assert.That(outputCallbacks, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_FuelPerfectJudgementUsesBlueGreenTextInsteadOfMissRed()
        {
            var host = new GameObject("Fuel Perfect Color Test");
            try
            {
                var controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
                controller.RecordFuelAttemptForTests(controller.GetFuelTargetForTests());
                TMP_Text judgement = (TMP_Text)FindText(host.transform, "FuelJudgementText");

                Assert.That(judgement.gameObject.activeInHierarchy, Is.True);
                Assert.That(judgement.text, Is.EqualTo("Perfect!"));
                Assert.That(judgement.color.g, Is.GreaterThan(judgement.color.r));
                Assert.That(judgement.color.b, Is.GreaterThan(judgement.color.r));
                Assert.That(judgement.color.g, Is.GreaterThan(0.8f));
                Assert.That(judgement.color.b, Is.GreaterThan(0.8f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static Vector2 ValvePoint(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 100f;
        }

        [Test]
        public void MiniGameTimer_CoolingStartsImmediatelyWithoutExample()
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
                Assert.That(cooling.GetCoolingDegreesForTests(), Is.Zero);
                Assert.That(cooling.GetStateTextForTests(), Does.Not.Contain("예시"));
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
        public void OperationUI_InitialRender_HidesAllEnginesAndDisablesDesign()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                Assert.That(controller.Model.ActiveEnginePresetCount, Is.Zero);
                Assert.That(FindButton(host.transform, "EngineCard_Engine01").gameObject.activeSelf, Is.False);
                Assert.That(FindButton(host.transform, "EngineCard_Engine10").gameObject.activeSelf, Is.False);
                Assert.That(FindButton(host.transform, "CreateEnginePresetButton").interactable, Is.True);
                Assert.That(FindButton(host.transform, "EnterDesignButton").interactable, Is.False);
                foreach (string name in new[] { "SelectedMissionText", "SelectedRequirementText", "RiskText", "StatInsightText" })
                    Assert.That(FindTransform(host.transform, name), Is.Null, name);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_CreateEnginePresetButton_RevealsFirstPresetAndEnablesDesign()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                FindButton(host.transform, "CreateEnginePresetButton").onClick.Invoke();

                Assert.That(controller.Model.ActiveEnginePresetCount, Is.EqualTo(1));
                Assert.That(FindButton(host.transform, "EngineCard_Engine01").gameObject.activeSelf, Is.True);
                Assert.That(controller.SelectedEnginePreset, Is.EqualTo(EnginePresetId.Engine01));
                Assert.That(FindButton(host.transform, "EnterDesignButton").interactable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_FundsFeedback_InitialAndUnchangedRefreshStayNeutral()
        {
            var host = new GameObject("Research UI Funds Feedback Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                var funds = (TMP_Text)FindText(host.transform, "Funds");
                Color baseColor = funds.color;
                Vector3 baseScale = funds.rectTransform.localScale;

                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.False);
                controller.RefreshForTests();

                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.False);
                AssertColor(funds.color, baseColor);
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_FundsFeedback_IncreaseTurnsGreenAndGrowsThenRestores()
        {
            var host = new GameObject("Research UI Funds Increase Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                var funds = (TMP_Text)FindText(host.transform, "Funds");
                Color baseColor = funds.color;
                Vector3 baseScale = funds.rectTransform.localScale;

                FindButton(host.transform, "WaitQuarterButton").onClick.Invoke();
                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.True);
                controller.GotoFundsFeedbackForTests(0.25f);

                AssertColor(funds.color, new Color32(91, 214, 123, 255));
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale * 1.15f), Is.LessThan(0.001f));

                controller.CompleteFundsFeedbackForTests();
                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.False);
                AssertColor(funds.color, baseColor);
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_FundsFeedback_DecreaseTurnsRedWithoutScalingThenRestores()
        {
            var host = new GameObject("Research UI Funds Decrease Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                var funds = (TMP_Text)FindText(host.transform, "Funds");
                Color baseColor = funds.color;
                Vector3 baseScale = funds.rectTransform.localScale;

                FindButton(host.transform, "CreateEnginePresetButton").onClick.Invoke();
                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.True);
                controller.GotoFundsFeedbackForTests(0.25f);

                AssertColor(funds.color, new Color32(239, 91, 91, 255));
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));

                controller.CompleteFundsFeedbackForTests();
                Assert.That(controller.IsFundsFeedbackActiveForTests(), Is.False);
                AssertColor(funds.color, baseColor);
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_FundsFeedback_NewChangeInterruptsAndRestartsFromBaseVisual()
        {
            var host = new GameObject("Research UI Funds Interrupt Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                var funds = (TMP_Text)FindText(host.transform, "Funds");
                Vector3 baseScale = funds.rectTransform.localScale;

                FindButton(host.transform, "WaitQuarterButton").onClick.Invoke();
                controller.GotoFundsFeedbackForTests(0.25f);
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale * 1.15f), Is.LessThan(0.001f));

                FindButton(host.transform, "CreateEnginePresetButton").onClick.Invoke();
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
                controller.GotoFundsFeedbackForTests(0.25f);

                AssertColor(funds.color, new Color32(239, 91, 91, 255));
                Assert.That(Vector3.Distance(funds.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
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

                controller.ReturnFromDesignScreenForTests();
                FindButton(host.transform, "PartDevelopmentButton").onClick.Invoke();

                Assert.That(FindButton(host.transform, "CancelDevelopmentButton").interactable, Is.True);
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
        public void ResultReportController_NewspaperResponseInvokesCallback()
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

                typeof(ResearchResultReportController).GetMethod("Respond", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);

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

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
