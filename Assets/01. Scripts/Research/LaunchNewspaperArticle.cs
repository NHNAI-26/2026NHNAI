using System;

namespace Border.Research
{
    public enum LaunchResultMedium
    {
        Newspaper,
        Mail
    }

    public enum LaunchResultStamp
    {
        Success,
        Fail,
        Clear
    }

    public readonly struct LaunchNewspaperArticle
    {
        private LaunchNewspaperArticle(string heading, string edition, string body, string effects, LaunchResultMedium medium, LaunchResultStamp stamp)
        {
            Heading = heading;
            Edition = edition;
            Body = body;
            Effects = effects;
            Medium = medium;
            Stamp = stamp;
        }

        public string Heading { get; }
        public string Edition { get; }
        public string Body { get; }
        public string Effects { get; }
        public LaunchResultMedium Medium { get; }
        public LaunchResultStamp Stamp { get; }

        public static LaunchNewspaperArticle Create(ResearchLaunchResultData result, string missionName)
        {
            string resolvedMissionName = string.IsNullOrWhiteSpace(missionName)
                ? ResearchPrototypeModel.GetMissionConfig(result.MissionId).DisplayName
                : missionName;

            bool succeeded = result.Grade <= ResearchGrade.B;
            LaunchResultMedium medium = ResolveMedium(result);
            string heading = CreateHeading(result, resolvedMissionName, succeeded, medium);
            string edition = CreateEdition(result, medium);
            string body = CreateBody(result, resolvedMissionName, succeeded, medium);
            string effects = CreateEffects(result);

            LaunchResultStamp stamp = result.FinalMissionWon ? LaunchResultStamp.Clear
                : succeeded ? LaunchResultStamp.Success : LaunchResultStamp.Fail;
            return new LaunchNewspaperArticle(heading, edition, body, effects, medium, stamp);
        }

        public static LaunchResultMedium ResolveMedium(ResearchLaunchResultData result)
        {
            if (result.MissionId == LaunchMissionId.LowPowerZoneHold || result.FinalMissionWon)
            {
                return LaunchResultMedium.Newspaper;
            }

            if (result.Visibility == TestVisibility.Private
                && result.OutcomeEvent?.Id == LaunchOutcomeEventId.Whistleblower)
            {
                return LaunchResultMedium.Newspaper;
            }

            return result.Visibility == TestVisibility.Private ? LaunchResultMedium.Mail : LaunchResultMedium.Newspaper;
        }

        private static string CreateHeading(
            ResearchLaunchResultData result,
            string missionName,
            bool succeeded,
            LaunchResultMedium medium)
        {
            if (medium == LaunchResultMedium.Mail)
            {
                string subject = result.OutcomeEvent == null
                    ? succeeded ? "성공 판정" : result.Grade == ResearchGrade.C ? "부분 성공 판정" : "실패 판정"
                    : CreateMailSubject(result.OutcomeEvent.Id, result.OutcomeEvent.Name);
                return $"[시험 결과] {missionName} - {subject}";
            }

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
                    return "세금만 태운 8년, 책임자 구속";
                default:
                    return succeeded ? "발사 성공 확인" : "발사 실패 확인";
            }
        }

        private static string CreateMailSubject(LaunchOutcomeEventId id, string fallback)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "정상 비행 데이터 확보";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "추락 현장 점검 결과";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "탑재 장비 회수 결과";
                case LaunchOutcomeEventId.QuietLessons:
                    return "비공개 시험 분석 결과";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "비공개 성능 개선 확인";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "실패 비행 데이터 분석 결과";
                default:
                    return string.IsNullOrWhiteSpace(fallback) ? "시험 결과 보고" : fallback;
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

            string publication = result.OutcomeEvent?.Id == LaunchOutcomeEventId.Whistleblower ? "특종" : "정규판";
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
            if (result.OutcomeEvent == null)
            {
                return article + " 추가 사건은 기록되지 않았다.";
            }

            return article + " " + CreateNewspaperEventParagraph(result.OutcomeEvent.Id, result.OutcomeEvent.Description);
        }

        private static string CreateNewspaperEventParagraph(LaunchOutcomeEventId id, string fallback)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "공개 발사 뒤 후원 기관들이 잇달아 지원 의사를 밝혔다. 기술 설명보다 예산 회의가 먼저 잡혔다.";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "비행 기록은 보기 드물게 깨끗했다. 연구진은 실패 원인 대신 다음 개선 목록을 작성했다.";
                case LaunchOutcomeEventId.PublicPressure:
                    return "성공 축하가 끝나기도 전에 다음 발사 일정을 묻는 요구가 이어졌다.";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "추락 지점에서 설계 결함 하나가 또렷하게 드러났다. 사고는 났지만 점검 방향은 잡혔다.";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "로켓은 뜨지 못했지만 탑재 장비는 온전히 회수됐다. 실패 현장에서 다음 시도 비용을 건졌다.";
                case LaunchOutcomeEventId.PadDamage:
                    return "발사는 짧았고 견적서는 길었다. 시설팀은 로켓보다 발사대를 먼저 살폈다.";
                case LaunchOutcomeEventId.QuietLessons:
                    return "외부에 알려지지 않은 시험에서 의미 있는 데이터가 확보됐다.";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "실패 장면이 대중에 그대로 공개됐다. 후원 기관들은 박수 대신 예산 검토표를 꺼냈다.";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "공식 발표는 없었지만 성능 지표가 개선됐다. 연구팀은 다음 설계를 수정했다.";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "실패 순간 평소에는 보이지 않던 흔들림이 기록됐다. 실패했지만 빈손은 아니었다.";
                case LaunchOutcomeEventId.Whistleblower:
                    return "관계자가 비공개 실패 기록과 예산 처리에 비리가 있다고 주장했다. 후원 기관은 다음 분기 지원을 삭감했다.";
                case LaunchOutcomeEventId.FinalProof:
                    return "저전력 검증을 통과하며 아르테미스 발사 체계가 최종 인정을 받았다.";
                case LaunchOutcomeEventId.FinalFailure:
                    return "감사원은 이 계획을 예산 낭비로 결론지었고, 검찰은 책임자를 구속 기소했다. 남은 설비는 매각 절차에 들어갔다.";
                default:
                    return string.IsNullOrWhiteSpace(fallback) ? "추가 사건은 기록되지 않았다." : fallback;
            }
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
