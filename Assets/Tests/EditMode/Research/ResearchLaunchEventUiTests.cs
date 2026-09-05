using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class ResearchLaunchEventUiTests
    {
        private const string ReportPrefabPath = "Assets/03. Prefabs/UI/ResearchResultReport.prefab";

        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            ResearchFlowSession.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            ResearchFlowSession.ResetForTests();
        }

        [Test]
        public void LaunchNewspaperArticle_UsesApprovedOutcomeHeadlines()
        {
            var expected = new Dictionary<LaunchOutcomeEventId, string>
            {
                { LaunchOutcomeEventId.SponsorBoost, "로켓이 뜨자, 예산도 떴다" },
                { LaunchOutcomeEventId.CleanTelemetry, "비행은 끝났고, 연구는 앞당겨졌다" },
                { LaunchOutcomeEventId.PublicPressure, "성공 확인, 다음 발사는 언제?" },
                { LaunchOutcomeEventId.NearMissInspection, "땅에 남긴 흔적, 설계에 남길 교훈" },
                { LaunchOutcomeEventId.RecoveredPayload, "못 뜬 로켓, 버리지 않은 장비" },
                { LaunchOutcomeEventId.PadDamage, "발사는 끝났고, 수리비가 남았다" },
                { LaunchOutcomeEventId.QuietLessons, "실패는 비공개, 교훈은 연구실로" },
                { LaunchOutcomeEventId.MediaBacklash, "로켓은 멈추고, 여론은 들끓었다" },
                { LaunchOutcomeEventId.FinalProof, "적게 태우고, 끝내 증명했다" },
            };

            foreach (LaunchOutcomeEventResult outcomeEvent in AllLaunchOutcomeEvents())
            {
                LaunchNewspaperArticle article = LaunchNewspaperArticle.Create(CreateResult(outcomeEvent), "저고도 안정화");
                Assert.That(article.Heading, Is.EqualTo(expected[outcomeEvent.Id]));
            }
        }

        [Test]
        public void LaunchNewspaperArticle_UsesDatedPrivateAndFinalEditions()
        {
            LaunchNewspaperArticle privateArticle = LaunchNewspaperArticle.Create(
                CreateResult(null, visibility: TestVisibility.Private), "저고도 안정화");
            LaunchNewspaperArticle finalArticle = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.FinalProof,
                    "최종 검증 인정",
                    "저전력 검증을 통과했습니다. 아르테미스 발사 체계가 최종 인정됐습니다.",
                    "효율 검증 통과 · 최종 미션 성공"),
                    missionId: LaunchMissionId.LowPowerZoneHold,
                    visibility: TestVisibility.FinalMission,
                    finalMissionWon: true), "저전력 구역 체류");

            Assert.That(privateArticle.Edition, Is.EqualTo("2024년 2분기 연구소 내부 회보"));
            Assert.That(finalArticle.Edition, Is.EqualTo("2024년 2분기 특별호"));
        }

        [TestCaseSource(nameof(AllLaunchOutcomeEvents))]
        public void ResultReport_PopulatesNewspaperArticleAndEffects(LaunchOutcomeEventResult outcomeEvent)
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData result = CreateResult(outcomeEvent);
            string missionName = ResearchFlowSession.GetOrCreate().Model
                .GetConfiguredMissionConfig(result.MissionId).DisplayName;

            report.Initialize(ResearchFlowSession.GetOrCreate(), result, null);

            string headline = FindText(report.gameObject, "Headline").text;
            string body = FindText(report.gameObject, "Body").text;
            string effects = FindText(report.gameObject, "Effects").text;
            Assert.That(headline, Is.Not.Empty);
            Assert.That(body, Does.Contain($"{missionName} 시험이 성공했다."));
            Assert.That(body, Does.Contain(outcomeEvent.Description));
            Assert.That(body, Does.Not.Contain("미션:"));
            Assert.That(body, Does.Not.Contain("판정:"));
            Assert.That(body, Does.Not.Contain("종료 사유:"));
            Assert.That(effects, Does.Contain("기본 보상: 즉시 지원금 +600 / 분기 연구비 +75"));
            Assert.That(effects, Does.Contain(outcomeEvent.EffectsText));
            Assert.That(effects, Does.Not.Contain("성공 80%"));
            Assert.That(effects, Does.Not.Contain("부분 10%"));
            Assert.That(effects, Does.Not.Contain("실패 10%"));
            Assert.That(effects, Does.Not.Contain("굴림"));
            Assert.That(effects, Does.Not.Contain("안내:"));
        }

        [Test]
        public void ResultReport_NullOutcomeUsesFallbackHeadlineAndPhotoLabel()
        {
            ResearchResultReportController report = CreateReport();

            report.Initialize(ResearchFlowSession.GetOrCreate(), CreateResult(null), null);

            string headline = FindText(report.gameObject, "Headline").text;
            string body = FindText(report.gameObject, "Body").text;
            TMP_Text fallback = FindText(report.gameObject, "PhotoFallback");
            Assert.That(headline, Is.EqualTo("발사 성공 확인"));
            Assert.That(body, Does.Contain("추가 사건은 기록되지 않았다."));
            Assert.That(fallback.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ResultReport_NewspaperTextsDoNotTruncateOnSavedPrefab()
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData result = CreateResult(new LaunchOutcomeEventResult(
                LaunchOutcomeEventId.CleanTelemetry,
                "깨끗한 비행 데이터",
                "성공한 비행의 기록으로 다음 엔진 개선 연구를 준비했습니다.",
                "1번 엔진 완성도 +5\n1번 엔진 연료 탱크 용량 +4\n1번 엔진 다음 일반 연구 시간 면제 (비용 정상 지불)"));

            report.Initialize(ResearchFlowSession.GetOrCreate(), result, null);
            Canvas.ForceUpdateCanvases();

            AssertTextNotTruncated(report.gameObject, "Headline");
            AssertTextNotTruncated(report.gameObject, "Body");
            AssertTextNotTruncated(report.gameObject, "Effects");
        }

        [Test]
        public void OperationStatus_UsesRemainingEventEffectsLabel()
        {
            var host = new GameObject("Research Launch Event UI Operation Status Test");
            createdObjects.Add(host);
            ResearchOperationUIController operation = host.AddComponent<ResearchOperationUIController>();
            ResearchPrototypeModel model = ResearchFlowSession.GetOrCreate().Model;
            SetPrivateField(model, "pendingPublicPressure", true);

            string status = (string)typeof(ResearchOperationUIController)
                .GetMethod("FormatResearchStatusText", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(operation, new object[] { model });

            Assert.That(status, Does.Contain("남은 이벤트 효과:"));
            Assert.That(status, Does.Not.Contain("예상 이벤트 효과"));
        }

        [Test]
        public void OperationRefresh_UsesFreeNormalResearchAndNextWaitFundingLabels()
        {
            var host = new GameObject("Research Launch Event UI Operation Fixture Test");
            createdObjects.Add(host);
            ResearchOperationUIController operation = host.AddComponent<ResearchOperationUIController>();
            ResearchPrototypeModel model = ResearchFlowSession.GetOrCreate().Model;
            SetPrivateField(model, "pendingFreeResearchEngine", EnginePresetId.Engine01);
            SetPrivateField(model, "pendingPublicPressure", true);

            operation.InitializeForTests();
            operation.RefreshForTests();

            string normalResearch = FindButtonLabel(host, "NormalResearchButton").text;
            string wait = FindButtonLabel(host, "WaitQuarterButton").text;
            Assert.That(normalResearch, Does.Contain("시간 0분기"));
            Assert.That(wait, Does.Contain($"+{model.NextWaitFunding}"));
            Assert.That(wait, Does.Not.Contain($"+{model.QuarterlyFunding}"));
            Assert.That(FindText(host, "StatusText").fontSizeMax, Is.LessThanOrEqualTo(14f),
                "Pending effects must not enlarge the status text and crowd out action buttons.");
        }

        [Test]
        public void DesignStatus_UsesRemainingEventEffectsLabel()
        {
            string status = (string)typeof(ResearchDesignScreenController)
                .GetMethod("FormatDesignStatusText", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { true, "다음 설계 진입 비용 -150" });

            Assert.That(status, Does.Contain("남은 이벤트 효과:"));
            Assert.That(status, Does.Not.Contain("예상 이벤트 효과"));
        }

        private ResearchResultReportController CreateReport()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReportPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject host = UnityEngine.Object.Instantiate(prefab);
            createdObjects.Add(host);
            return host.GetComponent<ResearchResultReportController>();
        }

        private static ResearchLaunchResultData CreateResult(
            LaunchOutcomeEventResult outcomeEvent,
            LaunchMissionId missionId = LaunchMissionId.LowAltitude,
            TestVisibility visibility = TestVisibility.Public,
            bool finalMissionWon = false)
        {
            return new ResearchLaunchResultData(
                missionId,
                EnginePresetId.Engine01,
                2024,
                2,
                800,
                350,
                visibility,
                50,
                80,
                70,
                80,
                10,
                10,
                42,
                ResearchGrade.B,
                600,
                75,
                finalMissionWon,
                false,
                outcomeEvent,
                LaunchTerminationReason.Succeeded);
        }

        private static IEnumerable<LaunchOutcomeEventResult> AllLaunchOutcomeEvents()
        {
            foreach (LaunchOutcomeEventId id in Enum.GetValues(typeof(LaunchOutcomeEventId)).Cast<LaunchOutcomeEventId>())
            {
                if (id == LaunchOutcomeEventId.None)
                {
                    continue;
                }

                yield return new LaunchOutcomeEventResult(
                    id,
                    GetEventName(id),
                    GetEventDescription(id),
                    GetEventEffectsText(id));
            }
        }

        private static string GetEventName(LaunchOutcomeEventId id)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "후원 기관 추가 지원";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "깨끗한 비행 데이터";
                case LaunchOutcomeEventId.PublicPressure:
                    return "공개 성공 뒤 일정 압박";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "근접 사고 점검";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "시험 장비 회수";
                case LaunchOutcomeEventId.PadDamage:
                    return "발사대 손상";
                case LaunchOutcomeEventId.QuietLessons:
                    return "조용한 실패 분석";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "공개 실패 역풍";
                case LaunchOutcomeEventId.FinalProof:
                    return "최종 검증 인정";
                default:
                    return id.ToString();
            }
        }

        private static string GetEventDescription(LaunchOutcomeEventId id)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "공개 발사 성공을 본 후원 기관이 다음 시험 예산을 추가 지원했습니다.";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "성공한 비행의 기록으로 다음 엔진 개선 연구를 준비했습니다.";
                case LaunchOutcomeEventId.PublicPressure:
                    return "추가 지원과 함께 다음 시험 일정을 앞당겨 달라는 요청이 왔습니다.";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "지면 추락 기록을 분석하고 다음 설계 점검 비용을 지원합니다.";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "이륙하지 못한 시험 장비를 회수했습니다. 같은 미션에 다시 사용할 수 있습니다.";
                case LaunchOutcomeEventId.PadDamage:
                    return "지면 추락으로 시설과 엔진에 정비가 필요합니다.";
                case LaunchOutcomeEventId.QuietLessons:
                    return "비공개 시험의 실패 기록을 엔진 개선에 반영했습니다.";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "공개 실패 소식에 후원 기관이 지원 규모를 줄였습니다.";
                case LaunchOutcomeEventId.FinalProof:
                    return "저전력 검증을 통과했습니다. 아르테미스 발사 체계가 최종 인정됐습니다.";
                default:
                    return id.ToString();
            }
        }

        private static string GetEventEffectsText(LaunchOutcomeEventId id)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "연구비 +300\n분기 연구비 +50";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "1번 엔진 완성도 +5\n1번 엔진 연료 탱크 용량 +4\n1번 엔진 다음 일반 연구 시간 면제 (비용 정상 지불)";
                case LaunchOutcomeEventId.PublicPressure:
                    return "연구비 +250\n다음 행동이 대기면 분기 예산 -50 / 설계 진입이면 비용 -100";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "다음 설계 진입 비용 -150\n1번 엔진 완성도 +3";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "저고도 안정화 다음 설치비 -20% (최대 300)";
                case LaunchOutcomeEventId.PadDamage:
                    return "연구비 -200\n다음 설계 진입 비용 +150\n1번 엔진 완성도 -3";
                case LaunchOutcomeEventId.QuietLessons:
                    return "1번 엔진 완성도 +4\n1번 엔진 냉각 능력 +3";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "분기 연구비 -100\n다음 발사: 공개 보상 배율 -0.25 / 비공개 선택 시 소멸";
                case LaunchOutcomeEventId.FinalProof:
                    return "효율 검증 통과 · 최종 미션 성공\n1번 엔진 완성도 +10\n1번 엔진 연료 탱크 용량 +5";
                default:
                    return id.ToString();
            }
        }

        private static TMP_Text FindText(GameObject root, string name)
        {
            TMP_Text text = root.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name == name);
            return text;
        }

        private static TMP_Text FindButtonLabel(GameObject root, string buttonName)
        {
            Button button = root.GetComponentsInChildren<Button>(true).Single(item => item.name == buttonName);
            return button.GetComponentInChildren<TMP_Text>(true);
        }

        private static void AssertTextNotTruncated(GameObject root, string name)
        {
            TMP_Text text = FindText(root, name);
            text.ForceMeshUpdate();
            Assert.That(text.isTextTruncated, Is.False, $"{name} text must fit in the saved prefab.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
