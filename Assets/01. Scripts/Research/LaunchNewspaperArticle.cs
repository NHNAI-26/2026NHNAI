using System;

namespace Border.Research
{
    public enum LaunchResultMedium
    {
        Newspaper,
        Mail
    }

    public readonly struct LaunchNewspaperArticle
    {
        private LaunchNewspaperArticle(string heading, string edition, string body, string effects, LaunchResultMedium medium)
        {
            Heading = heading;
            Edition = edition;
            Body = body;
            Effects = effects;
            Medium = medium;
        }

        public string Heading { get; }
        public string Edition { get; }
        public string Body { get; }
        public string Effects { get; }
        public LaunchResultMedium Medium { get; }

        public static LaunchNewspaperArticle Create(ResearchLaunchResultData result, string missionName)
        {
            string resolvedMissionName = string.IsNullOrWhiteSpace(missionName)
                ? ResearchPrototypeModel.GetMissionConfig(result.MissionId).DisplayName
                : missionName;

            bool succeeded = result.Grade <= ResearchGrade.B;
            LaunchResultMedium medium = ResolveMedium(result);
            string heading = CreateHeading(result, succeeded);
            string edition = CreateEdition(result, medium);
            string body = CreateBody(result, resolvedMissionName, succeeded, medium);
            string effects = CreateEffects(result);

            return new LaunchNewspaperArticle(heading, edition, body, effects, medium);
        }

        public static LaunchResultMedium ResolveMedium(ResearchLaunchResultData result)
        {
            if (result.MissionId == LaunchMissionId.LowPowerZoneHold || result.FinalMissionWon)
            {
                return LaunchResultMedium.Newspaper;
            }

            if (result.OutcomeEvent != null && TryResolveEventMedium(result.OutcomeEvent.Id, out LaunchResultMedium eventMedium))
            {
                return eventMedium;
            }

            return result.Visibility == TestVisibility.Private ? LaunchResultMedium.Mail : LaunchResultMedium.Newspaper;
        }

        private static bool TryResolveEventMedium(LaunchOutcomeEventId id, out LaunchResultMedium medium)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.FinalProof:
                case LaunchOutcomeEventId.FinalFailure:
                case LaunchOutcomeEventId.Whistleblower:
                case LaunchOutcomeEventId.SponsorBoost:
                case LaunchOutcomeEventId.PublicPressure:
                case LaunchOutcomeEventId.MediaBacklash:
                case LaunchOutcomeEventId.PadDamage:
                    medium = LaunchResultMedium.Newspaper;
                    return true;
                case LaunchOutcomeEventId.CleanTelemetry:
                case LaunchOutcomeEventId.NearMissInspection:
                case LaunchOutcomeEventId.RecoveredPayload:
                case LaunchOutcomeEventId.QuietLessons:
                case LaunchOutcomeEventId.QuietBreakthrough:
                case LaunchOutcomeEventId.UsefulFailureData:
                    medium = LaunchResultMedium.Mail;
                    return true;
                default:
                    medium = LaunchResultMedium.Newspaper;
                    return false;
            }
        }

        private static string CreateHeading(ResearchLaunchResultData result, bool succeeded)
        {
            LaunchOutcomeEventResult outcomeEvent = result.OutcomeEvent;
            if (outcomeEvent == null)
            {
                return succeeded ? "발사 성공 확인" : "발사 실패 확인";
            }

            switch (outcomeEvent.Id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "\"저건 됩니다\" 후원 기관, 뒤늦게 줄 섰다";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "연구진 \"이번엔 그래프가 우리 편\"";
                case LaunchOutcomeEventId.PublicPressure:
                    return "성공 축하합니다. 다음 건 언제죠?";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "추락 현장서 뜻밖의 개선점 발견";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "이륙 실패, 장비 회수에는 성공";
                case LaunchOutcomeEventId.PadDamage:
                    return "발사는 짧았고, 견적서는 길었다";
                case LaunchOutcomeEventId.QuietLessons:
                    return "아무도 몰랐지만 연구팀은 알았다";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "전 국민 앞에서 멈춘 로켓, 예산도 멈췄다";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "공식 발표 없이 성능표만 좋아졌다";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "깨진 기록에서 멀쩡한 답 나왔다";
                case LaunchOutcomeEventId.Whistleblower:
                    return "관계자, \"비리 관계 있다\" 밝혀";
                case LaunchOutcomeEventId.FinalProof:
                    return "적게 태우고, 끝내 증명했다";
                case LaunchOutcomeEventId.FinalFailure:
                    return "끝내 낮은 불꽃은 달에 닿지 못했다";
                default:
                    return succeeded ? "발사 성공 확인" : "발사 실패 확인";
            }
        }

        private static string CreateEdition(ResearchLaunchResultData result, LaunchResultMedium medium)
        {
            string date = $"{result.Year}년 {result.Quarter}분기";
            if (result.FinalMissionWon || result.OutcomeEvent?.Id == LaunchOutcomeEventId.FinalFailure)
            {
                return $"{date} 특별호";
            }

            if (medium == LaunchResultMedium.Mail)
            {
                return $"{date} 내부 메일";
            }

            string publication = result.Visibility == TestVisibility.Private ? "연구소 내부 회보" : "정규판";
            return $"{date} {publication}";
        }

        private static string CreateBody(ResearchLaunchResultData result, string missionName, bool succeeded, LaunchResultMedium medium)
        {
            return medium == LaunchResultMedium.Mail
                ? CreateMailBody(result, missionName, succeeded)
                : CreateNewspaperBody(result, missionName, succeeded);
        }

        private static string CreateNewspaperBody(ResearchLaunchResultData result, string missionName, bool succeeded)
        {
            string article = succeeded
                ? $"{missionName} 시험이 성공했다."
                : result.OutcomeEvent?.Id == LaunchOutcomeEventId.FinalFailure && result.DeadlineMissed
                    ? $"{result.Year}년 {result.Quarter}분기 마감까지 {missionName}을 통과하지 못했다."
                : CreateFailedBodyLead(result, missionName);
            if (result.OutcomeEvent == null || string.IsNullOrWhiteSpace(result.OutcomeEvent.Description))
            {
                return article + " 추가 사건은 기록되지 않았다.";
            }

            return article + " " + result.OutcomeEvent.Description;
        }

        private static string CreateMailBody(ResearchLaunchResultData result, string missionName, bool succeeded)
        {
            string outcome = succeeded
                ? $"{missionName} 시험 결과를 확인했습니다. 판정은 성공입니다."
                : $"{missionName} 시험 결과를 확인했습니다. {CreateFailedBodyLead(result, missionName)}";
            string eventNote = result.OutcomeEvent == null || string.IsNullOrWhiteSpace(result.OutcomeEvent.Description)
                ? "추가 전달 사항은 없습니다."
                : result.OutcomeEvent.Description;

            return $"책임자님,\n\n{outcome} {eventNote}\n\n정산과 후속 조치는 아래 항목으로 전달드립니다.";
        }

        private static string CreateFailedBodyLead(ResearchLaunchResultData result, string missionName)
        {
            if (result.Grade == ResearchGrade.C)
            {
                return $"{missionName} 시험은 부분 성공으로 기록됐다.";
            }

            return $"{missionName} 시험은 {CreateTerminationReasonText(result.TerminationReason)} 종료됐다.";
        }

        private static string CreateEffects(ResearchLaunchResultData result)
        {
            string label = result.MissionId == LaunchMissionId.LowPowerZoneHold ? "기본 보상" : "테스트 정산";
            string baseEffects = $"{label}: 즉시 지원금 {FormatSigned(result.ImmediateFunding)} / 분기 연구비 {FormatSigned(result.QuarterlyFundingDelta)}";
            if (result.OutcomeEvent == null || string.IsNullOrWhiteSpace(result.OutcomeEvent.EffectsText))
            {
                return baseEffects;
            }

            return baseEffects + "\n" + result.OutcomeEvent.EffectsText;
        }

        private static string CreateTerminationReasonText(LaunchTerminationReason reason)
        {
            switch (reason)
            {
                case LaunchTerminationReason.Succeeded:
                    return "판정 확인 후";
                case LaunchTerminationReason.NoLiftoff:
                    return "이륙 실패로";
                case LaunchTerminationReason.GroundCrash:
                    return "지면 충돌로";
                case LaunchTerminationReason.Splashdown:
                    return "해상 추락으로";
                case LaunchTerminationReason.SelfDestruct:
                    return "자폭으로";
                case LaunchTerminationReason.Overheat:
                    return "엔진 과열로";
                case LaunchTerminationReason.Unknown:
                    return "원인 미상의 문제로";
                default:
                    return "원인 미상의 문제로";
            }
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }
}
