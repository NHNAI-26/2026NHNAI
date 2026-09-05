using System;
using System.Collections.Generic;

namespace Border.Research
{
    public enum LaunchOutcomeEventId
    {
        None, SponsorBoost, CleanTelemetry, PublicPressure, NearMissInspection,
        RecoveredPayload, PadDamage, QuietLessons, MediaBacklash, QuietBreakthrough,
        UsefulFailureData, Whistleblower, FinalProof, FinalFailure
    }

    public sealed class LaunchOutcomeEventResult
    {
        public LaunchOutcomeEventResult(LaunchOutcomeEventId id, string name, string description, string effectsText)
        {
            Id = id;
            Name = name;
            Description = description;
            EffectsText = effectsText;
        }

        public LaunchOutcomeEventId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string EffectsText { get; }
    }

    public sealed partial class ResearchPrototypeModel
    {
        private readonly Func<int, int> eventRandomOverride;
        private Random eventRandom;
        private LaunchOutcomeEventId previousLaunchEvent;
        private bool pendingPublicPressure;
        private bool pendingInspectionDiscount;
        private bool pendingPadSurcharge;
        private bool pendingPublicRewardPenalty;
        private bool activePublicRewardPenalty;
        private LaunchMissionId? pendingDiscountMission;
        private EnginePresetId? pendingFreeResearchEngine;

        public bool HasFreeNormalResearch(EnginePresetId presetId) => pendingFreeResearchEngine == presetId;
        public int NextWaitFunding => ClampInt(QuarterlyFunding - (pendingPublicPressure ? 50 : 0),
            balanceConfig.MinQuarterlyFunding, balanceConfig.MaxQuarterlyFunding);

        public string PendingLaunchEffectsText
        {
            get
            {
                var lines = new List<string>();
                if (pendingPublicPressure) lines.Add("다음 행동: 대기 시 분기 예산 -50 / 설계 진입 시 비용 -50");
                if (pendingInspectionDiscount) lines.Add("다음 설계 진입 비용 -50");
                if (pendingPadSurcharge) lines.Add("다음 설계 진입 비용 +50");
                if (pendingDiscountMission.HasValue)
                    lines.Add($"{GetConfiguredMissionConfig(pendingDiscountMission.Value).DisplayName} 다음 설치비 -20% (최대 300)");
                if (pendingPublicRewardPenalty) lines.Add("다음 발사: 공개 성공 이벤트 연구비 -25% / 비공개 선택 시 소멸");
                if (activePublicRewardPenalty) lines.Add("이번 공개 발사 성공 이벤트 연구비 -25% 적용");
                if (pendingFreeResearchEngine.HasValue)
                    lines.Add($"{GetEnginePresetName(pendingFreeResearchEngine.Value)} 다음 일반 연구: 시간 소모 없음 (비용 정상 지불)");
                return string.Join("\n", lines);
            }
        }

        private void ResetLaunchEvents()
        {
            eventRandom = new Random(unchecked(Seed ^ 0x4c41554e));
            previousLaunchEvent = LaunchOutcomeEventId.None;
            pendingPublicPressure = pendingInspectionDiscount = pendingPadSurcharge = false;
            pendingPublicRewardPenalty = activePublicRewardPenalty = false;
            pendingDiscountMission = null;
            pendingFreeResearchEngine = null;
        }

        public int GetDesignEntryCost(LaunchMissionId missionId)
        {
            return Math.Max(0, GetConfiguredMissionConfig(missionId).LaunchCost
                - (pendingPublicPressure ? 50 : 0) - (pendingInspectionDiscount ? 50 : 0)
                + (pendingPadSurcharge ? 50 : 0));
        }

        public int GetLaunchPaymentCost(ResearchDesignEntryData entry)
        {
            return (entry.LaunchCostPaid ? 0 : GetDesignEntryCost(entry.MissionId))
                + GetDiscountedInstallCost(entry.MissionId, entry.InstalledEngineCounts);
        }

        private int GetDiscountedInstallCost(LaunchMissionId missionId, int[] counts)
        {
            int[] normalized = CopyAndNormalizeEngineCounts(counts);
            ClearLockedEngineCounts(normalized);
            int cost = CalculateReservedInstallCost(normalized);
            return pendingDiscountMission == missionId ? cost - Math.Min(300, cost / 5) : cost;
        }

        private void ConsumeEntryEffects()
        {
            pendingPublicPressure = pendingInspectionDiscount = pendingPadSurcharge = false;
        }

        public static IReadOnlyList<LaunchOutcomeEventId> GetEligibleLaunchEvents(
            LaunchMissionId mission, bool succeeded, TestVisibility visibility, int launchYear, LaunchTerminationReason reason)
        {
            var candidates = new List<LaunchOutcomeEventId>();
            if (mission == LaunchMissionId.LowPowerZoneHold)
            {
                candidates.Add(succeeded ? LaunchOutcomeEventId.FinalProof : LaunchOutcomeEventId.FinalFailure);
                return candidates;
            }
            if (succeeded)
            {
                if (visibility == TestVisibility.Public) candidates.Add(LaunchOutcomeEventId.SponsorBoost);
                candidates.Add(LaunchOutcomeEventId.CleanTelemetry);
                if (visibility == TestVisibility.Private) candidates.Add(LaunchOutcomeEventId.QuietBreakthrough);
                if (visibility == TestVisibility.Public && launchYear < 2025) candidates.Add(LaunchOutcomeEventId.PublicPressure);
            }
            else
            {
                if (reason == LaunchTerminationReason.GroundCrash) candidates.Add(LaunchOutcomeEventId.NearMissInspection);
                if (visibility == TestVisibility.Private && reason == LaunchTerminationReason.NoLiftoff)
                    candidates.Add(LaunchOutcomeEventId.RecoveredPayload);
                if (visibility == TestVisibility.Public && reason == LaunchTerminationReason.GroundCrash)
                    candidates.Add(LaunchOutcomeEventId.PadDamage);
                if (visibility == TestVisibility.Private) candidates.Add(LaunchOutcomeEventId.QuietLessons);
                if (visibility == TestVisibility.Public) candidates.Add(LaunchOutcomeEventId.MediaBacklash);
                candidates.Add(LaunchOutcomeEventId.UsefulFailureData);
                if (visibility == TestVisibility.Private) candidates.Add(LaunchOutcomeEventId.Whistleblower);
            }
            return candidates;
        }

        private int NextEventIndex(int count)
        {
            int index = eventRandomOverride == null ? eventRandom.Next(count) : eventRandomOverride(count);
            if (index < 0 || index >= count) throw new InvalidOperationException("Event random index is outside the candidate list.");
            return index;
        }

        private LaunchOutcomeEventResult ApplyLaunchEvent(ResearchDesignEntryData entry, bool succeeded, LaunchTerminationReason reason)
        {
            var candidates = new List<LaunchOutcomeEventId>(GetEligibleLaunchEvents(entry.MissionId, succeeded, entry.Visibility, entry.Year, reason));
            if (candidates.Count > 1) candidates.Remove(previousLaunchEvent);
            if (candidates.Count == 0)
            {
                previousLaunchEvent = LaunchOutcomeEventId.None;
                return null;
            }
            LaunchOutcomeEventId id = candidates[NextEventIndex(candidates.Count)];
            previousLaunchEvent = id;
            EnginePresetState target = FindEventEngine(entry.InstalledEngineCounts, id == LaunchOutcomeEventId.PadDamage);
            var effects = new List<string>();
            string name;
            string description;
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    name = "돈 냄새를 맡은 사람들";
                    description = "공개 발사 뒤 후원 기관들이 뒤늦게 줄을 섰습니다. 기술 설명보다 예산 회의가 먼저 잡혔습니다.";
                    ApplyPublicSuccessEventFunds(500, 100, effects);
                    break;
                case LaunchOutcomeEventId.CleanTelemetry:
                    name = "그래프가 우리 편";
                    description = "비행 기록이 보기 드물게 깨끗했습니다. 연구진은 실패 원인 대신 개선 목록을 적었습니다.";
                    ApplyEventFunds(100, 0, effects);
                    ApplyEventEngine(target, 5, 4, effects);
                    if (target != null)
                    {
                        pendingFreeResearchEngine = target.PresetId;
                        effects.Add($"{GetEnginePresetName(target.PresetId)} 다음 일반 연구 시간 면제 (비용 정상 지불)");
                    }
                    break;
                case LaunchOutcomeEventId.PublicPressure:
                    name = "박수 뒤의 독촉장";
                    description = "성공 축하가 끝나기도 전에 다음 발사 일정을 묻는 연락이 왔습니다.";
                    ApplyPublicSuccessEventFunds(350, 0, effects);
                    pendingPublicPressure = true;
                    effects.Add("다음 행동이 대기면 분기 예산 -50 / 설계 진입이면 비용 -50");
                    break;
                case LaunchOutcomeEventId.NearMissInspection:
                    name = "추락 현장의 힌트";
                    description = "추락 지점에서 설계 결함 하나가 또렷하게 드러났습니다. 사고는 났지만 점검 방향은 잡혔습니다.";
                    pendingInspectionDiscount = true;
                    effects.Add("다음 설계 진입 비용 -50");
                    ApplyEventEngine(target, 3, 0, effects);
                    break;
                case LaunchOutcomeEventId.RecoveredPayload:
                    name = "못 뜬 덕분에 산 장비";
                    description = "로켓은 뜨지 못했지만 장비는 멀쩡했습니다. 실패 현장에서 다음 시도 비용을 건졌습니다.";
                    pendingDiscountMission = entry.MissionId;
                    effects.Add($"{GetConfiguredMissionConfig(entry.MissionId).DisplayName} 다음 설치비 -20% (최대 300)");
                    break;
                case LaunchOutcomeEventId.PadDamage:
                    name = "수리비 착륙";
                    description = "발사는 짧았고 견적서는 길었습니다. 시설팀은 로켓보다 발사대를 먼저 봤습니다.";
                    ApplyEventFunds(-200, 0, effects);
                    pendingPadSurcharge = true;
                    effects.Add("다음 설계 진입 비용 +50");
                    ApplyEventEngine(target, -3, 0, effects);
                    break;
                case LaunchOutcomeEventId.QuietLessons:
                    name = "조용히 망하고 조용히 배움";
                    description = "밖은 몰랐고 연구실은 알았습니다. 체면은 지켰고 데이터는 남았습니다.";
                    ApplyEventEngine(target, 4, 3, effects);
                    break;
                case LaunchOutcomeEventId.MediaBacklash:
                    name = "공개 처형식";
                    description = "실패 장면이 너무 잘 보였습니다. 후원 기관은 박수 대신 예산 검토표를 꺼냈습니다.";
                    ApplyEventFunds(0, -150, effects);
                    pendingPublicRewardPenalty = true;
                    effects.Add("다음 발사: 공개 성공 이벤트 연구비 -25% / 비공개 선택 시 소멸");
                    break;
                case LaunchOutcomeEventId.QuietBreakthrough:
                    name = "닫힌 문 안의 정답";
                    description = "공식 발표는 없었지만 성능표는 좋아졌습니다. 연구팀은 조용히 다음 설계를 고쳤습니다.";
                    ApplyEventFunds(75, 0, effects);
                    ApplyEventEngine(target, 8, 2, effects);
                    break;
                case LaunchOutcomeEventId.UsefulFailureData:
                    name = "망한 김에 본 것";
                    description = "실패 순간에 평소엔 보이지 않던 흔들림이 잡혔습니다. 망했지만 빈손은 아니었습니다.";
                    ApplyEventEngine(target, 2, 1, effects);
                    break;
                case LaunchOutcomeEventId.Whistleblower:
                    name = "내부 고발자";
                    description = "관계자가 비공개 실패 기록과 예산 처리에 \"비리 관계 있다\"고 주장했습니다. 후원 기관이 다음 분기 지원을 깎았습니다.";
                    ApplyEventFunds(0, -100, effects);
                    break;
                case LaunchOutcomeEventId.FinalProof:
                    name = "최종 검증 인정";
                    description = "저전력 검증을 통과했습니다. 아르테미스 발사 체계가 최종 인정됐습니다.";
                    effects.Add("효율 검증 통과 · 최종 미션 성공");
                    ApplyEventEngine(target, 10, 5, effects);
                    break;
                case LaunchOutcomeEventId.FinalFailure:
                    return CreateFinalFailureOutcomeEvent();
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
            return new LaunchOutcomeEventResult(id, name, description, string.Join("\n", effects));
        }

        public static LaunchOutcomeEventResult CreateFinalFailureOutcomeEvent()
        {
            return new LaunchOutcomeEventResult(
                LaunchOutcomeEventId.FinalFailure,
                "최종 검증 실패",
                "저전력 검증은 최종 통과 기준에 못 미쳤습니다. 남은 기록은 다음 판단 자료로 넘겨졌습니다.",
                "최종 미션 실패");
        }

        private EnginePresetState FindEventEngine(int[] counts, bool random)
        {
            var installed = new List<int>();
            int target = -1;
            for (int i = 0; counts != null && i < Math.Min(counts.Length, EnginePresets.Length); i++)
            {
                if (counts[i] <= 0 || !EnginePresets[i].Unlocked) continue;
                installed.Add(i);
                if (target < 0 || counts[i] > counts[target]) target = i;
            }
            if (target < 0) return null;
            return EnginePresets[random ? installed[NextEventIndex(installed.Count)] : target];
        }

        private void ApplyEventFunds(int fundsDelta, int quarterlyDelta, List<string> effects)
        {
            int oldFunds = Funds;
            int oldQuarterly = QuarterlyFunding;
            Funds = Math.Max(0, Funds + fundsDelta);
            QuarterlyFunding = ClampInt(QuarterlyFunding + quarterlyDelta, balanceConfig.MinQuarterlyFunding, balanceConfig.MaxQuarterlyFunding);
            if (fundsDelta != 0) effects.Add($"연구비 {Funds - oldFunds:+#;-#;0}");
            if (quarterlyDelta != 0) effects.Add($"분기 연구비 {QuarterlyFunding - oldQuarterly:+#;-#;0}");
        }

        private void ApplyPublicSuccessEventFunds(int fundsDelta, int quarterlyDelta, List<string> effects)
        {
            if (activePublicRewardPenalty)
            {
                fundsDelta = (int)Math.Round(fundsDelta * 0.75d, MidpointRounding.AwayFromZero);
                quarterlyDelta = (int)Math.Round(quarterlyDelta * 0.75d, MidpointRounding.AwayFromZero);
            }

            ApplyEventFunds(fundsDelta, quarterlyDelta, effects);
        }

        private void ApplyEventEngine(EnginePresetState target, int completionDelta, int statDelta, List<string> effects)
        {
            if (target == null)
            {
                effects.Add("설치된 엔진 없음: 엔진 보상 미적용");
                return;
            }
            int oldCompletion = target.Completion;
            target.Completion = ClampInt(oldCompletion + completionDelta, 0, MaxEngineCompletion);
            string name = GetEnginePresetName(target.PresetId);
            effects.Add($"{name} 완성도 {target.Completion - oldCompletion:+#;-#;0}");
            if (statDelta == 0) return;
            EngineStatId stat = EngineStatId.FuelCapacity;
            for (int i = 1; i <= (int)EngineStatId.IgnitionReliability; i++)
                if (target.GetStat((EngineStatId)i) < target.GetStat(stat)) stat = (EngineStatId)i;
            int oldStat = target.GetStat(stat);
            target.SetStat(stat, oldStat + statDelta);
            effects.Add($"{name} {GetStatDisplayName(stat)} {target.GetStat(stat) - oldStat:+#;-#;0}");
        }
    }
}
