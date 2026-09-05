using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Border.Research.Tests
{
    public sealed class ResearchLaunchEventsTests
    {
        [TestCase(LaunchMissionId.LowAltitude, true, TestVisibility.Public, 2024, LaunchTerminationReason.Succeeded,
            new[] { LaunchOutcomeEventId.SponsorBoost, LaunchOutcomeEventId.CleanTelemetry, LaunchOutcomeEventId.PublicPressure })]
        [TestCase(LaunchMissionId.LowAltitude, true, TestVisibility.Public, 2025, LaunchTerminationReason.Succeeded,
            new[] { LaunchOutcomeEventId.SponsorBoost, LaunchOutcomeEventId.CleanTelemetry })]
        [TestCase(LaunchMissionId.LowAltitude, true, TestVisibility.Private, 2024, LaunchTerminationReason.Succeeded,
            new[] { LaunchOutcomeEventId.CleanTelemetry })]
        [TestCase(LaunchMissionId.LowAltitude, false, TestVisibility.Private, 2024, LaunchTerminationReason.NoLiftoff,
            new[] { LaunchOutcomeEventId.RecoveredPayload, LaunchOutcomeEventId.QuietLessons })]
        [TestCase(LaunchMissionId.LowAltitude, false, TestVisibility.Private, 2024, LaunchTerminationReason.GroundCrash,
            new[] { LaunchOutcomeEventId.NearMissInspection, LaunchOutcomeEventId.QuietLessons })]
        [TestCase(LaunchMissionId.LowAltitude, false, TestVisibility.Public, 2024, LaunchTerminationReason.GroundCrash,
            new[] { LaunchOutcomeEventId.NearMissInspection, LaunchOutcomeEventId.PadDamage, LaunchOutcomeEventId.MediaBacklash })]
        [TestCase(LaunchMissionId.LowAltitude, false, TestVisibility.Public, 2024, LaunchTerminationReason.NoLiftoff,
            new[] { LaunchOutcomeEventId.MediaBacklash })]
        [TestCase(LaunchMissionId.LowAltitude, false, TestVisibility.Private, 2024, LaunchTerminationReason.SelfDestruct,
            new[] { LaunchOutcomeEventId.QuietLessons })]
        [TestCase(LaunchMissionId.LowPowerZoneHold, true, TestVisibility.FinalMission, 2024, LaunchTerminationReason.Succeeded,
            new[] { LaunchOutcomeEventId.FinalProof })]
        [TestCase(LaunchMissionId.LowPowerZoneHold, false, TestVisibility.FinalMission, 2024, LaunchTerminationReason.GroundCrash,
            new LaunchOutcomeEventId[0])]
        public void GetEligibleLaunchEvents_ReturnsApprovedCandidatesInSourceOrder(
            LaunchMissionId mission,
            bool succeeded,
            TestVisibility visibility,
            int launchYear,
            LaunchTerminationReason reason,
            LaunchOutcomeEventId[] expected)
        {
            IReadOnlyList<LaunchOutcomeEventId> result = ResearchPrototypeModel.GetEligibleLaunchEvents(
                mission,
                succeeded,
                visibility,
                launchYear,
                reason);

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void CompleteLaunch_UsesEventRandomOverrideAndAvoidsRepeatingSameEventWhenOtherCandidatesExist()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);

            ResearchLaunchResultData first = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);
            ResearchLaunchResultData second = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);
            model.Reset();
            ResearchLaunchResultData afterReset = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);

            Assert.That(first.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.SponsorBoost));
            Assert.That(second.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(afterReset.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.SponsorBoost));
        }

        [Test]
        public void CompleteLaunch_DoubleCompletionReturnsNoPendingAndDoesNotApplyEffectsAgain()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            BeginLaunch(model, TestVisibility.Public);

            Assert.That(model.CompleteLaunch(true, LaunchTerminationReason.Succeeded, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            int funds = model.Funds;
            int quarterlyFunding = model.QuarterlyFunding;
            int launches = model.TotalLaunches;
            int attempts = model.GetMission(LaunchMissionId.LowAltitude).AttemptCount;
            int failedLaunches = model.FailedLaunches;
            bool activeLaunch = model.HasActiveLaunch;
            ResearchGrade bestGrade = model.GetMission(LaunchMissionId.LowAltitude).BestGrade;

            Assert.That(model.CompleteLaunch(true, LaunchTerminationReason.Succeeded, out ResearchLaunchResultData second), Is.EqualTo(ResearchActionResult.NoPendingDesignEntry));
            Assert.That(second.OutcomeEvent, Is.Null);
            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.SponsorBoost));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(quarterlyFunding));
            Assert.That(model.TotalLaunches, Is.EqualTo(launches));
            Assert.That(model.GetMission(LaunchMissionId.LowAltitude).AttemptCount, Is.EqualTo(attempts));
            Assert.That(model.FailedLaunches, Is.EqualTo(failedLaunches));
            Assert.That(model.HasActiveLaunch, Is.EqualTo(activeLaunch));
            Assert.That(model.GetMission(LaunchMissionId.LowAltitude).BestGrade, Is.EqualTo(bestGrade));
        }

        [Test]
        public void SeededRandom_ResetReproducesEventSequence()
        {
            var model = new ResearchPrototypeModel(12345, CreateBalance());

            LaunchOutcomeEventId first = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded).OutcomeEvent.Id;
            LaunchOutcomeEventId second = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded).OutcomeEvent.Id;
            model.Reset();
            LaunchOutcomeEventId resetFirst = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded).OutcomeEvent.Id;
            LaunchOutcomeEventId resetSecond = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded).OutcomeEvent.Id;

            Assert.That(resetFirst, Is.EqualTo(first));
            Assert.That(resetSecond, Is.EqualTo(second));
        }

        [Test]
        public void SponsorBoost_AppliesEventFundingAndQuarterlyClampWithoutBaseSettlement()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(initialQuarterlyFunding: 990, maxQuarterlyFunding: 1000), eventRandom: AlwaysFirst);

            ResearchLaunchResultData result = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.SponsorBoost));
            Assert.That(result.ImmediateFunding, Is.Zero);
            Assert.That(result.QuarterlyFundingDelta, Is.Zero);
            Assert.That(model.QuarterlyFunding, Is.EqualTo(1000));
            Assert.That(model.HighestQuarterlyFunding, Is.EqualTo(1000));
            Assert.That(model.Funds, Is.EqualTo(11_100));
        }

        [Test]
        public void CleanTelemetry_AppliesCompletionCapAndLowestStatTieToInstalledEngine()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.Completion = 98;
            engine.FuelCapacity = 98;
            engine.Cooling = 98;
            engine.MaxOutput = 100;
            engine.IgnitionReliability = 100;

            ResearchLaunchResultData result = Launch(model, TestVisibility.Private, true, LaunchTerminationReason.Succeeded);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(result.OutcomeEvent.EffectsText, Does.Contain("완성도 +2"));
            Assert.That(result.OutcomeEvent.EffectsText, Does.Contain("연료 탱크 용량 +2"));
            Assert.That(engine.Completion, Is.EqualTo(ResearchPrototypeModel.MaxEngineCompletion));
            Assert.That(engine.FuelCapacity, Is.EqualTo(100));
            Assert.That(engine.Cooling, Is.EqualTo(98));
        }

        [Test]
        public void CleanTelemetry_WithNoInstalledEnginesReportsNoEngineReward()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            int completion = engine.Completion;
            int fuel = engine.FuelCapacity;
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];

            ResearchLaunchResultData result = Launch(model, TestVisibility.Private, true, LaunchTerminationReason.Succeeded, counts);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(result.OutcomeEvent.EffectsText, Does.Contain("설치된 엔진 없음"));
            Assert.That(engine.Completion, Is.EqualTo(completion));
            Assert.That(engine.FuelCapacity, Is.EqualTo(fuel));
        }

        [Test]
        public void EngineEventTargetsLargestInstalledCountAndLowerIdOnTie()
        {
            var largestModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            UnlockPreset(largestModel, EnginePresetId.Engine02);
            var largestCounts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            largestCounts[(int)EnginePresetId.Engine01] = 1;
            largestCounts[(int)EnginePresetId.Engine02] = 3;
            EnginePresetState largestFirst = largestModel.GetEnginePreset(EnginePresetId.Engine01);
            EnginePresetState largestSecond = largestModel.GetEnginePreset(EnginePresetId.Engine02);

            ResearchLaunchResultData largestResult = Launch(largestModel, TestVisibility.Private, true, LaunchTerminationReason.Succeeded, largestCounts);

            Assert.That(largestResult.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(largestFirst.Completion, Is.Zero);
            Assert.That(largestSecond.Completion, Is.EqualTo(5));

            var tieModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            UnlockPreset(tieModel, EnginePresetId.Engine02);
            var tieCounts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            tieCounts[(int)EnginePresetId.Engine01] = 2;
            tieCounts[(int)EnginePresetId.Engine02] = 2;
            EnginePresetState tieFirst = tieModel.GetEnginePreset(EnginePresetId.Engine01);
            EnginePresetState tieSecond = tieModel.GetEnginePreset(EnginePresetId.Engine02);

            ResearchLaunchResultData tieResult = Launch(tieModel, TestVisibility.Private, true, LaunchTerminationReason.Succeeded, tieCounts);

            Assert.That(tieResult.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(tieFirst.Completion, Is.EqualTo(5));
            Assert.That(tieSecond.Completion, Is.Zero);
        }

        [Test]
        public void QuietLessons_AppliesCompletionAndLowestStatReward()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.FuelCapacity = 70;
            engine.Cooling = 35;
            engine.MaxOutput = 80;
            engine.IgnitionReliability = 90;

            ResearchLaunchResultData result = Launch(model, TestVisibility.Private, false, LaunchTerminationReason.SelfDestruct);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.QuietLessons));
            Assert.That(engine.Completion, Is.EqualTo(4));
            Assert.That(engine.Cooling, Is.EqualTo(38));
            Assert.That(result.OutcomeEvent.EffectsText, Does.Contain("냉각 능력 +3"));
        }

        [Test]
        public void PadDamage_AppliesFundsFloorCompletionFloorAndRandomInstalledTarget()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(initialFunds: 1_660, minQuarterlyFunding: 0), eventRandom: Sequence(1, 1));
            UnlockPreset(model, EnginePresetId.Engine02);
            EnginePresetState first = model.GetEnginePreset(EnginePresetId.Engine01);
            EnginePresetState second = model.GetEnginePreset(EnginePresetId.Engine02);
            first.Completion = 2;
            second.Completion = 2;
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[(int)EnginePresetId.Engine01] = 1;
            counts[(int)EnginePresetId.Engine02] = 1;

            ResearchLaunchResultData result = Launch(model, TestVisibility.Public, false, LaunchTerminationReason.GroundCrash, counts);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.PadDamage));
            Assert.That(model.Funds, Is.GreaterThanOrEqualTo(0));
            Assert.That(first.Completion, Is.EqualTo(2));
            Assert.That(second.Completion, Is.Zero);
            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost + 50));
        }

        [Test]
        public void PadDamage_CannotDriveFundsOrEngineBelowFloor()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(initialFunds: 400, initialQuarterlyFunding: 0, minQuarterlyFunding: 0), eventRandom: Sequence(1, 0));
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.Completion = 1;

            ResearchLaunchResultData result = Launch(model, TestVisibility.Public, false, LaunchTerminationReason.GroundCrash);

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.PadDamage));
            Assert.That(model.Funds, Is.Zero);
            Assert.That(engine.Completion, Is.Zero);
        }

        [Test]
        public void RecoveredPayload_DiscountsSameMissionInstallQuoteAndPaymentOnly()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            ResearchLaunchResultData failed = Launch(model, TestVisibility.Private, false, LaunchTerminationReason.NoLiftoff);
            model.GetMission(LaunchMissionId.HighAltitude).Unlocked = true;
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[(int)EnginePresetId.Engine01] = 5;

            ResearchDesignEntryData retry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, counts, 50, TestVisibility.Private);
            ResearchDesignEntryData otherMission = model.CreateDesignEntry(LaunchMissionId.HighAltitude, EnginePresetId.Engine01, counts, 50, TestVisibility.Private);
            int funds = model.Funds;

            Assert.That(failed.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.RecoveredPayload));
            Assert.That(retry.ReservedInstallCost, Is.EqualTo(1_450));
            Assert.That(model.GetLaunchPaymentCost(retry), Is.EqualTo(retry.LaunchCost + 1_450));
            Assert.That(otherMission.ReservedInstallCost, Is.EqualTo(ResearchPrototypeModel.EngineInstallCost * 5));
            Assert.That(model.BeginLaunch(retry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(funds - retry.LaunchCost - 1_450));
        }

        [Test]
        public void RecoveredPayload_DifferentMissionLaunchRetainsRetryDiscount()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            Launch(model, TestVisibility.Private, false, LaunchTerminationReason.NoLiftoff);
            model.GetMission(LaunchMissionId.HighAltitude).Unlocked = true;
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[(int)EnginePresetId.Engine01] = 5;
            ResearchDesignEntryData otherMission = model.CreateDesignEntry(LaunchMissionId.HighAltitude, EnginePresetId.Engine01, counts, 50, TestVisibility.Private);
            ResearchDesignEntryData retry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, counts, 50, TestVisibility.Private);

            Assert.That(model.BeginLaunch(otherMission), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.GetLaunchPaymentCost(retry), Is.EqualTo(retry.LaunchCost + 1_450));
        }

        [Test]
        public void BeginLaunch_WhenFundsAreShortDoesNotConsumeDiscountOrFunds()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(initialFunds: 1_000), eventRandom: AlwaysFirst);
            Launch(model, TestVisibility.Private, false, LaunchTerminationReason.NoLiftoff);
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[(int)EnginePresetId.Engine01] = 5;
            ResearchDesignEntryData retry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, counts, 50, TestVisibility.Private);
            int funds = model.Funds;
            int payment = model.GetLaunchPaymentCost(retry);

            Assert.That(funds, Is.LessThan(payment));
            Assert.That(model.BeginLaunch(retry), Is.EqualTo(ResearchActionResult.NotEnoughFunds));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.GetLaunchPaymentCost(retry), Is.EqualTo(payment));
        }

        [Test]
        public void FreeResearchEvent_MakesOnlyMatchingNormalResearchCostPaidButTimeFreeWithNoIncome()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            UnlockPreset(model, EnginePresetId.Engine02);
            ResearchLaunchResultData result = Launch(model, TestVisibility.Private, true, LaunchTerminationReason.Succeeded);
            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int turns = model.RemainingTurns;

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(funds - ResearchPrototypeModel.EngineNormalResearchCost));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));

            int fundsBeforeOtherNormal = model.Funds;
            Assert.That(model.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(fundsBeforeOtherNormal - ResearchPrototypeModel.EngineNormalResearchCost + model.QuarterlyFunding));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns - 1));
        }

        [Test]
        public void FreeResearchEvent_WhenCompletionMaxedFailsAndKeepsPendingFreeResearch()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            ResearchLaunchResultData result = Launch(model, TestVisibility.Private, true, LaunchTerminationReason.Succeeded);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.Completion = ResearchPrototypeModel.MaxEngineCompletion;
            int funds = model.Funds;
            int turns = model.RemainingTurns;

            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.CleanTelemetry));
            Assert.That(model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.EngineCompletionMaxed));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 일반 연구"));
        }

        [Test]
        public void FinalProof_AppliesRewardAndWinWhileFinalFailureHasNoEvent()
        {
            var successModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            PrepareFinalMission(successModel);

            ResearchLaunchResultData success = Launch(successModel, LaunchMissionId.LowPowerZoneHold, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);

            Assert.That(success.Visibility, Is.EqualTo(TestVisibility.FinalMission));
            Assert.That(success.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.FinalProof));
            Assert.That(success.FinalMissionWon, Is.True);
            Assert.That(successModel.GameWon, Is.True);
            Assert.That(successModel.HasGameEnded, Is.True);
            Assert.That(success.ImmediateFunding, Is.EqualTo(400));
            Assert.That(success.QuarterlyFundingDelta, Is.EqualTo(50));

            var failureModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            PrepareFinalMission(failureModel);
            BeginLaunch(failureModel, LaunchMissionId.LowPowerZoneHold, TestVisibility.FinalMission);
            Assert.That(failureModel.CompleteLaunch(false, LaunchTerminationReason.GroundCrash, out ResearchLaunchResultData failure), Is.EqualTo(ResearchActionResult.Success));

            Assert.That(failure.OutcomeEvent, Is.Null);
            Assert.That(failure.FinalMissionWon, Is.False);
            Assert.That(failureModel.GameWon, Is.False);
        }

        [Test]
        public void PublicPressure_NextSuccessfulWaitResearchNewEngineOrDesignConsumesPendingEffect()
        {
            AssertPublicPressureWait();
            AssertPublicPressureResearch();
            AssertPublicPressureNewEngine();
            AssertPublicPressureDesignEntry();
        }

        [Test]
        public void PublicPressure_FailedResearchKeepsEffectAndFocusedResearchConsumesWithTime()
        {
            var failedResearchModel = CreatePressureModel();
            Assert.That(failedResearchModel.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.EnginePresetLocked));
            Assert.That(failedResearchModel.PendingLaunchEffectsText, Does.Contain("다음 행동"));
            Assert.That(failedResearchModel.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(0));

            var focusedModel = CreatePressureModel();
            int funds = focusedModel.Funds;
            int quarterlyFunding = focusedModel.QuarterlyFunding;
            int turns = focusedModel.RemainingTurns;
            Assert.That(focusedModel.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, true, 100), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(focusedModel.Funds, Is.EqualTo(funds - ResearchPrototypeModel.EngineFocusedResearchCost + quarterlyFunding));
            Assert.That(focusedModel.QuarterlyFunding, Is.EqualTo(quarterlyFunding));
            Assert.That(focusedModel.RemainingTurns, Is.EqualTo(turns - 1));
            Assert.That(focusedModel.PendingLaunchEffectsText, Does.Not.Contain("다음 행동"));
        }

        [Test]
        public void MediaBacklash_PublicPenaltyIsActivatedAtBeginLaunchAndPrivateBeginClearsIt()
        {
            var publicModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            ResearchLaunchResultData backlash = Launch(publicModel, TestVisibility.Public, false, LaunchTerminationReason.NoLiftoff);
            Assert.That(backlash.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.MediaBacklash));
            Assert.That(publicModel.PendingLaunchEffectsText, Does.Contain("공개 성공 이벤트 연구비 -25%"));

            BeginLaunch(publicModel, TestVisibility.Public);
            Assert.That(publicModel.PendingLaunchEffectsText, Does.Contain("이번 공개 발사 성공 이벤트 연구비 -25% 적용"));
            Assert.That(publicModel.CompleteLaunch(true, LaunchTerminationReason.Succeeded, out ResearchLaunchResultData penalized), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(penalized.ImmediateFunding, Is.Zero);
            Assert.That(penalized.QuarterlyFundingDelta, Is.Zero);
            Assert.That(penalized.OutcomeEvent.EffectsText, Does.Contain("연구비 +375"));
            Assert.That(penalized.OutcomeEvent.EffectsText, Does.Contain("분기 연구비 +75"));
            Assert.That(publicModel.PendingLaunchEffectsText, Does.Not.Contain("공개 성공 이벤트 연구비 -25%"));

            var privateModel = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: AlwaysFirst);
            Launch(privateModel, TestVisibility.Public, false, LaunchTerminationReason.NoLiftoff);
            BeginLaunch(privateModel, TestVisibility.Private);
            Assert.That(privateModel.PendingLaunchEffectsText, Does.Not.Contain("공개 성공 이벤트 연구비 -25%"));
            Assert.That(privateModel.PendingLaunchEffectsText, Does.Not.Contain("이번 공개 발사 성공 이벤트 연구비 -25% 적용"));
            Assert.That(privateModel.CompleteLaunch(true, LaunchTerminationReason.Succeeded, out ResearchLaunchResultData privateSuccess), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(privateSuccess.ImmediateFunding, Is.Zero);
            Assert.That(privateSuccess.QuarterlyFundingDelta, Is.Zero);
            Assert.That(privateSuccess.OutcomeEvent.EffectsText, Does.Contain("연구비 +100"));
        }

        [Test]
        public void PendingEntryEffects_CombineByTypeAndClearTogetherAtDesignEntry()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: Sequence(2, 0, 0, 0));
            Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);
            LaunchPaidWithEvent(model, LaunchMissionId.LowAltitude, TestVisibility.Private, false, LaunchTerminationReason.GroundCrash);
            LaunchPaidWithEvent(model, LaunchMissionId.LowAltitude, TestVisibility.Public, false, LaunchTerminationReason.GroundCrash);
            int baseCost = ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost;

            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(0));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("설계 진입 시 비용 -50"));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 설계 진입 비용 -50"));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 설계 진입 비용 +50"));

            Assert.That(model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, TestVisibility.Private, out ResearchDesignEntryData entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(entry.LaunchCost, Is.EqualTo(0));
            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(baseCost));
            Assert.That(model.PendingLaunchEffectsText, Does.Not.Contain("설계 진입"));
        }

        [Test]
        public void PendingEntryEffectSameTypeRefreshesWithoutStackingWhileDifferentTypesCoexist()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: Sequence(1, 0, 1, 0, 0));
            LaunchPaidWithEvent(model, LaunchMissionId.LowAltitude, TestVisibility.Public, false, LaunchTerminationReason.GroundCrash);
            LaunchPaid(model, LaunchMissionId.LowPowerZoneHold, TestVisibility.FinalMission, false, LaunchTerminationReason.GroundCrash);
            LaunchPaidWithEvent(model, LaunchMissionId.LowAltitude, TestVisibility.Public, false, LaunchTerminationReason.GroundCrash);
            int baseCost = ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost;

            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(baseCost + 50));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 설계 진입 비용 +50"));

            LaunchPaid(model, LaunchMissionId.LowPowerZoneHold, TestVisibility.FinalMission, false, LaunchTerminationReason.GroundCrash);
            LaunchPaidWithEvent(model, LaunchMissionId.LowAltitude, TestVisibility.Private, false, LaunchTerminationReason.GroundCrash);
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 설계 진입 비용 -50"));
            Assert.That(model.PendingLaunchEffectsText, Does.Contain("다음 설계 진입 비용 +50"));
            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(baseCost));
        }

        private static void AssertPublicPressureWait()
        {
            var model = CreatePressureModel();
            int quarterlyFunding = model.QuarterlyFunding;
            int turns = model.RemainingTurns;
            Assert.That(model.WaitQuarter(), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(quarterlyFunding - 50));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns - 1));
            Assert.That(model.PendingLaunchEffectsText, Does.Not.Contain("다음 행동"));
        }

        private static void AssertPublicPressureResearch()
        {
            var model = CreatePressureModel();
            int funds = model.Funds;
            int quarterlyFunding = model.QuarterlyFunding;
            int turns = model.RemainingTurns;
            Assert.That(model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(quarterlyFunding));
            Assert.That(model.Funds, Is.EqualTo(funds - ResearchPrototypeModel.EngineNormalResearchCost + quarterlyFunding));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns - 1));
            Assert.That(model.PendingLaunchEffectsText, Does.Not.Contain("다음 행동"));
        }

        private static void AssertPublicPressureNewEngine()
        {
            var model = CreatePressureModel();
            int funds = model.Funds;
            int turns = model.RemainingTurns;
            Assert.That(model.CreateNewEnginePreset(out EnginePresetId presetId), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(presetId, Is.EqualTo(EnginePresetId.Engine02));
            Assert.That(model.Funds, Is.EqualTo(funds - ResearchPrototypeModel.NewEnginePresetCost));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(model.PendingLaunchEffectsText, Does.Not.Contain("다음 행동"));
        }

        private static void AssertPublicPressureDesignEntry()
        {
            var model = CreatePressureModel();
            int baseCost = ResearchPrototypeModel.GetMissionConfig(LaunchMissionId.LowAltitude).LaunchCost;
            int funds = model.Funds;
            Assert.That(model.GetDesignEntryCost(LaunchMissionId.LowAltitude), Is.EqualTo(0));
            Assert.That(model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, TestVisibility.Private, out ResearchDesignEntryData entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(entry.LaunchCost, Is.EqualTo(0));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.PendingLaunchEffectsText, Does.Not.Contain("다음 행동"));
        }

        private static ResearchPrototypeModel CreatePressureModel()
        {
            var model = new ResearchPrototypeModel(balanceConfig: CreateBalance(), eventRandom: Sequence(2));
            ResearchLaunchResultData result = Launch(model, TestVisibility.Public, true, LaunchTerminationReason.Succeeded);
            Assert.That(result.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.PublicPressure));
            return model;
        }

        private static ResearchLaunchResultData Launch(
            ResearchPrototypeModel model,
            TestVisibility visibility,
            bool succeeded,
            LaunchTerminationReason reason,
            int[] installedEngineCounts = null)
        {
            return Launch(model, LaunchMissionId.LowAltitude, visibility, succeeded, reason, installedEngineCounts);
        }

        private static ResearchLaunchResultData Launch(
            ResearchPrototypeModel model,
            LaunchMissionId missionId,
            TestVisibility visibility,
            bool succeeded,
            LaunchTerminationReason reason,
            int[] installedEngineCounts = null)
        {
            BeginLaunch(model, missionId, visibility, installedEngineCounts);
            Assert.That(model.CompleteLaunch(succeeded, reason, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.OutcomeEvent, Is.Not.Null);
            Assert.That(result.TerminationReason, Is.EqualTo(reason));
            return result;
        }

        private static ResearchLaunchResultData LaunchPaid(
            ResearchPrototypeModel model,
            LaunchMissionId missionId,
            TestVisibility visibility,
            bool succeeded,
            LaunchTerminationReason reason)
        {
            PrepareFinalMission(model);
            ResearchDesignEntryData entry = model.CreateDesignEntry(missionId, EnginePresetId.Engine01, SingleEngine(), 50, visibility, true);
            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.CompleteLaunch(succeeded, reason, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.OutcomeEvent, Is.Null);
            return result;
        }

        private static ResearchLaunchResultData LaunchPaidWithEvent(
            ResearchPrototypeModel model,
            LaunchMissionId missionId,
            TestVisibility visibility,
            bool succeeded,
            LaunchTerminationReason reason)
        {
            ResearchDesignEntryData entry = model.CreateDesignEntry(missionId, EnginePresetId.Engine01, SingleEngine(), 50, visibility, true);
            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.CompleteLaunch(succeeded, reason, out ResearchLaunchResultData result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.OutcomeEvent, Is.Not.Null);
            return result;
        }

        private static void BeginLaunch(ResearchPrototypeModel model, TestVisibility visibility, int[] installedEngineCounts = null)
        {
            BeginLaunch(model, LaunchMissionId.LowAltitude, visibility, installedEngineCounts);
        }

        private static void BeginLaunch(
            ResearchPrototypeModel model,
            LaunchMissionId missionId,
            TestVisibility visibility,
            int[] installedEngineCounts = null)
        {
            int[] counts = installedEngineCounts ?? SingleEngine();
            ResearchDesignEntryData entry = model.CreateDesignEntry(missionId, EnginePresetId.Engine01, counts, 50, visibility);
            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
        }

        private static int[] SingleEngine()
        {
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[(int)EnginePresetId.Engine01] = 1;
            return counts;
        }

        private static ResearchBalanceConfig CreateBalance(
            int initialFunds = 10_000,
            int initialQuarterlyFunding = ResearchPrototypeModel.InitialQuarterlyFunding,
            int minQuarterlyFunding = ResearchPrototypeModel.MinQuarterlyFunding,
            int maxQuarterlyFunding = ResearchPrototypeModel.MaxQuarterlyFunding)
        {
            return new ResearchBalanceConfig(
                initialFunds,
                initialQuarterlyFunding,
                minQuarterlyFunding,
                maxQuarterlyFunding,
                ResearchPrototypeModel.ResearchCompletionGain,
                ResearchPrototypeModel.EngineNormalResearchCost,
                ResearchPrototypeModel.EngineFocusedResearchCost,
                ResearchPrototypeModel.NewEnginePresetCost,
                ResearchPrototypeModel.EngineInstallCost,
                ResearchPrototypeModel.CreateDefaultMissionConfigs());
        }

        private static void UnlockPreset(ResearchPrototypeModel model, EnginePresetId presetId)
        {
            while (!model.IsEnginePresetUnlocked(presetId))
            {
                Assert.That(model.CreateNewEnginePreset(out _), Is.EqualTo(ResearchActionResult.Success));
            }
        }

        private static void PrepareFinalMission(ResearchPrototypeModel model)
        {
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.Completion = ResearchPrototypeModel.MaxEngineCompletion;
            engine.FuelCapacity = ResearchPrototypeModel.MaxEngineCompletion;
            engine.Cooling = ResearchPrototypeModel.MaxEngineCompletion;
            engine.MaxOutput = ResearchPrototypeModel.MaxEngineCompletion;
            engine.IgnitionReliability = ResearchPrototypeModel.MaxEngineCompletion;
        }

        private static int AlwaysFirst(int count)
        {
            Assert.That(count, Is.Positive);
            return 0;
        }

        private static Func<int, int> Sequence(params int[] indexes)
        {
            int position = 0;
            return count =>
            {
                Assert.That(count, Is.Positive);
                int index = position < indexes.Length ? indexes[position] : 0;
                position++;
                Assert.That(index, Is.InRange(0, count - 1));
                return index;
            };
        }
    }
}
