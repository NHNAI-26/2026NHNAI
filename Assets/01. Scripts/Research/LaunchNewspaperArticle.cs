using System;

namespace Border.Research
{
    public readonly struct LaunchNewspaperArticle
    {
        private LaunchNewspaperArticle(string heading, string edition, string body, string effects)
        {
            Heading = heading;
            Edition = edition;
            Body = body;
            Effects = effects;
        }

        public string Heading { get; }
        public string Edition { get; }
        public string Body { get; }
        public string Effects { get; }

        public static LaunchNewspaperArticle Create(ResearchLaunchResultData result, string missionName)
        {
            string resolvedMissionName = string.IsNullOrWhiteSpace(missionName)
                ? ResearchPrototypeModel.GetMissionConfig(result.MissionId).DisplayName
                : missionName;

            bool succeeded = result.Grade <= ResearchGrade.B;
            string heading = CreateHeading(result, succeeded);
            string edition = CreateEdition(result);
            string body = CreateBody(result, resolvedMissionName, succeeded);
            string effects = CreateEffects(result);

            return new LaunchNewspaperArticle(heading, edition, body, effects);
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
                    return "로켓이 뜨자, 예산도 떴다";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "비행은 끝났고, 연구는 앞당겨졌다";
                case LaunchOutcomeEventId.PublicPressure:
                    return "성공 확인, 다음 발사는 언제?";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "땅에 남긴 흔적, 설계에 남길 교훈";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "못 뜬 로켓, 버리지 않은 장비";
                case LaunchOutcomeEventId.PadDamage:
                    return "발사는 끝났고, 수리비가 남았다";
                case LaunchOutcomeEventId.QuietLessons:
                    return "실패는 비공개, 교훈은 연구실로";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "로켓은 멈추고, 여론은 들끓었다";
                case LaunchOutcomeEventId.FinalProof:
                    return "적게 태우고, 끝내 증명했다";
                default:
                    return succeeded ? "발사 성공 확인" : "발사 실패 확인";
            }
        }

        private static string CreateEdition(ResearchLaunchResultData result)
        {
            string date = $"{result.Year}년 {result.Quarter}분기";
            if (result.FinalMissionWon)
            {
                return $"{date} 특별호";
            }

            string publication = result.Visibility == TestVisibility.Private ? "연구소 내부 회보" : "정규판";
            return $"{date} {publication}";
        }

        private static string CreateBody(ResearchLaunchResultData result, string missionName, bool succeeded)
        {
            string article = succeeded
                ? $"{missionName} 시험이 성공했다."
                : CreateFailedBodyLead(result, missionName);
            if (result.OutcomeEvent == null || string.IsNullOrWhiteSpace(result.OutcomeEvent.Description))
            {
                return article + " 추가 사건은 기록되지 않았다.";
            }

            return article + " " + result.OutcomeEvent.Description;
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
            string baseEffects = $"기본 보상: 즉시 지원금 {FormatSigned(result.ImmediateFunding)} / 분기 연구비 {FormatSigned(result.QuarterlyFundingDelta)}";
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
