using System;
using System.Collections.Generic;

namespace Border.Research
{
    public enum LaunchOutcomeEventId
    {
        None, SponsorBoost, CleanTelemetry, PublicPressure, NearMissInspection,
        RecoveredPayload, PadDamage, QuietLessons, MediaBacklash, FinalProof
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
                if (pendingPublicPressure) lines.Add("다음 행동: 대기 시 분기 예산 -50 / 설계 진입 시 비용 -100");
                if (pendingInspectionDiscount) lines.Add("다음 설계 진입 비용 -150");
                if (pendingPadSurcharge) lines.Add("다음 설계 진입 비용 +150");
                if (pendingDiscountMission.HasValue)
                    lines.Add($"{GetConfiguredMissionConfig(pendingDiscountMission.Value).DisplayName} 다음 설치비 -20% (최대 300)");
                if (pendingPublicRewardPenalty) lines.Add("다음 발사: 공개 보상 배율 -0.25 / 비공개 선택 시 소멸");
                if (activePublicRewardPenalty) lines.Add("이번 공개 발사 보상 배율 -0.25 적용");
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
                - (pendingPublicPressure ? 100 : 0) - (pendingInspectionDiscount ? 150 : 0)
                + (pendingPadSurcharge ? 150 : 0));
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
                if (succeeded) candidates.Add(LaunchOutcomeEventId.FinalProof);
                return candidates;
            }
            if (succeeded)
            {
                if (visibility == TestVisibility.Public) candidates.Add(LaunchOutcomeEventId.SponsorBoost);
                candidates.Add(LaunchOutcomeEventId.CleanTelemetry);
                if (visibility == TestVisibility.Public && launchYear < 2025) candidates.Add(LaunchOutcomeEventId.PublicPressure);
            }
            else
            {
                if (reason == LaunchTerminationReason.GroundCrash) candidates.Add(LaunchOutcomeEventId.NearMissInspection);
                if (visibility == TestVisibility.Private && reason == LaunchTerminationReason.NoLiftoff)
                    candidates.Add(LaunchOutcomeEventId.RecoveredPayload);
                if (reason == LaunchTerminationReason.GroundCrash) candidates.Add(LaunchOutcomeEventId.PadDamage);
                if (visibility == TestVisibility.Private) candidates.Add(LaunchOutcomeEventId.QuietLessons);
                if (visibility == TestVisibility.Public) candidates.Add(LaunchOutcomeEventId.MediaBacklash);
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
                    name = "후원 기관 추가 지원";
                    description = "공개 발사 성공을 본 후원 기관이 다음 시험 예산을 추가 지원했습니다.";
                    ApplyEventFunds(300, 50, effects);
                    break;
                case LaunchOutcomeEventId.CleanTelemetry:
                    name = "깨끗한 비행 데이터";
                    description = "성공한 비행의 기록으로 다음 엔진 개선 연구를 준비했습니다.";
                    ApplyEventEngine(target, 5, 4, effects);
                    if (target != null)
                    {
                        pendingFreeResearchEngine = target.PresetId;
                        effects.Add($"{GetEnginePresetName(target.PresetId)} 다음 일반 연구 시간 면제 (비용 정상 지불)");
                    }
                    break;
                case LaunchOutcomeEventId.PublicPressure:
                    name = "공개 성공 뒤 일정 압박";
                    description = "추가 지원과 함께 다음 시험 일정을 앞당겨 달라는 요청이 왔습니다.";
                    ApplyEventFunds(250, 0, effects);
                    pendingPublicPressure = true;
                    effects.Add("다음 행동이 대기면 분기 예산 -50 / 설계 진입이면 비용 -100");
                    break;
                case LaunchOutcomeEventId.NearMissInspection:
                    name = "근접 사고 점검";
                    description = "지면 추락 기록을 분석하고 다음 설계 점검 비용을 지원합니다.";
                    pendingInspectionDiscount = true;
                    effects.Add("다음 설계 진입 비용 -150");
                    ApplyEventEngine(target, 3, 0, effects);
                    break;
                case LaunchOutcomeEventId.RecoveredPayload:
                    name = "시험 장비 회수";
                    description = "이륙하지 못한 시험 장비를 회수했습니다. 같은 미션에 다시 사용할 수 있습니다.";
                    pendingDiscountMission = entry.MissionId;
                    effects.Add($"{GetConfiguredMissionConfig(entry.MissionId).DisplayName} 다음 설치비 -20% (최대 300)");
                    break;
                case LaunchOutcomeEventId.PadDamage:
                    name = "발사대 손상";
                    description = "지면 추락으로 시설과 엔진에 정비가 필요합니다.";
                    ApplyEventFunds(-200, 0, effects);
                    pendingPadSurcharge = true;
                    effects.Add("다음 설계 진입 비용 +150");
                    ApplyEventEngine(target, -3, 0, effects);
                    break;
                case LaunchOutcomeEventId.QuietLessons:
                    name = "조용한 실패 분석";
                    description = "비공개 시험의 실패 기록을 엔진 개선에 반영했습니다.";
                    ApplyEventEngine(target, 4, 3, effects);
                    break;
                case LaunchOutcomeEventId.MediaBacklash:
                    name = "공개 실패 역풍";
                    description = "공개 실패 소식에 후원 기관이 지원 규모를 줄였습니다.";
                    ApplyEventFunds(0, -100, effects);
                    pendingPublicRewardPenalty = true;
                    effects.Add("다음 발사: 공개 보상 배율 -0.25 / 비공개 선택 시 소멸");
                    break;
                default:
                    name = "최종 검증 인정";
                    description = "저전력 검증을 통과했습니다. 아르테미스 발사 체계가 최종 인정됐습니다.";
                    effects.Add("효율 검증 통과 · 최종 미션 성공");
                    ApplyEventEngine(target, 10, 5, effects);
                    break;
            }
            return new LaunchOutcomeEventResult(id, name, description, string.Join("\n", effects));
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
