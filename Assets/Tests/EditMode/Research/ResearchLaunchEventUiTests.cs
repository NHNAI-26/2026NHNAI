using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.UI;
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
                { LaunchOutcomeEventId.SponsorBoost, "\"저건 됩니다\" 후원 기관, 뒤늦게 줄 섰다" },
                { LaunchOutcomeEventId.CleanTelemetry, "연구진 \"이번엔 그래프가 우리 편\"" },
                { LaunchOutcomeEventId.PublicPressure, "성공 축하합니다. 다음 건 언제죠?" },
                { LaunchOutcomeEventId.NearMissInspection, "추락 현장서 뜻밖의 개선점 발견" },
                { LaunchOutcomeEventId.RecoveredPayload, "이륙 실패, 장비 회수에는 성공" },
                { LaunchOutcomeEventId.PadDamage, "발사는 짧았고, 견적서는 길었다" },
                { LaunchOutcomeEventId.QuietLessons, "아무도 몰랐지만 연구팀은 알았다" },
                { LaunchOutcomeEventId.MediaBacklash, "전 국민 앞에서 멈춘 로켓, 예산도 멈췄다" },
                { LaunchOutcomeEventId.QuietBreakthrough, "공식 발표 없이 성능표만 좋아졌다" },
                { LaunchOutcomeEventId.UsefulFailureData, "깨진 기록에서 멀쩡한 답 나왔다" },
                { LaunchOutcomeEventId.Whistleblower, "관계자, \"비리 관계 있다\" 밝혀" },
                { LaunchOutcomeEventId.FinalProof, "적게 태우고, 끝내 증명했다" },
                { LaunchOutcomeEventId.FinalFailure, "끝내 낮은 불꽃은 달에 닿지 못했다" },
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
                    visibility: TestVisibility.Public,
                    finalMissionWon: true), "저전력 구역 체류");
            LaunchNewspaperArticle finalFailureArticle = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.FinalFailure,
                    "최종 검증 실패",
                    "저전력 검증은 최종 통과 기준에 못 미쳤습니다.",
                    "최종 미션 실패"),
                    missionId: LaunchMissionId.LowPowerZoneHold,
                    visibility: TestVisibility.Public,
                    finalMissionWon: false,
                    deadlineMissed: true,
                    grade: ResearchGrade.F), "저전력 구역 체류");

            Assert.That(privateArticle.Edition, Is.EqualTo("2024년 2분기 내부 메일"));
            Assert.That(finalArticle.Edition, Is.EqualTo("2024년 2분기 특별호"));
            Assert.That(finalFailureArticle.Edition, Is.EqualTo("2024년 2분기 특별호"));
        }

        [Test]
        public void LaunchNewspaperArticle_ResolvesDefaultAndEventMediums()
        {
            LaunchNewspaperArticle publicArticle = LaunchNewspaperArticle.Create(
                CreateResult(null, visibility: TestVisibility.Public), "저고도 안정화");
            LaunchNewspaperArticle privateArticle = LaunchNewspaperArticle.Create(
                CreateResult(null, visibility: TestVisibility.Private), "저고도 안정화");
            LaunchNewspaperArticle privateWhistleblower = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.Whistleblower,
                    "내부 고발자",
                    "관계자가 비공개 실패 기록과 예산 처리에 \"비리 관계 있다\"고 주장했습니다.",
                    "분기 연구비 -100"),
                    visibility: TestVisibility.Private), "저고도 안정화");
            LaunchNewspaperArticle publicCleanTelemetry = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.CleanTelemetry,
                    "그래프가 우리 편",
                    "비행 기록이 보기 드물게 깨끗했습니다.",
                    "연구비 +100"),
                    visibility: TestVisibility.Public), "저고도 안정화");
            LaunchNewspaperArticle finalArticle = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.FinalProof,
                    "최종 검증 인정",
                    "저전력 검증을 통과했습니다.",
                    "효율 검증 통과 · 최종 미션 성공"),
                    missionId: LaunchMissionId.LowPowerZoneHold,
                    visibility: TestVisibility.Public,
                    finalMissionWon: true), "저전력 구역 체류");

            Assert.That(publicArticle.Medium, Is.EqualTo(LaunchResultMedium.Newspaper));
            Assert.That(privateArticle.Medium, Is.EqualTo(LaunchResultMedium.Mail));
            Assert.That(privateWhistleblower.Medium, Is.EqualTo(LaunchResultMedium.Newspaper));
            Assert.That(publicCleanTelemetry.Medium, Is.EqualTo(LaunchResultMedium.Newspaper));
            Assert.That(finalArticle.Medium, Is.EqualTo(LaunchResultMedium.Newspaper));
        }

        [TestCaseSource(nameof(AllLaunchOutcomeEvents))]
        public void ResultReport_PopulatesNewspaperArticleAndEffects(LaunchOutcomeEventResult outcomeEvent)
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData result = CreateResult(outcomeEvent);
            string missionName = ResearchFlowSession.GetOrCreate().Model
                .GetConfiguredMissionConfig(result.MissionId).DisplayName;
            LaunchNewspaperArticle article = LaunchNewspaperArticle.Create(result, missionName);

            report.Initialize(ResearchFlowSession.GetOrCreate(), result, null);

            string headline = FindText(report.gameObject, "Headline").text;
            string body = FindText(report.gameObject, "Body").text;
            string effects = FindText(report.gameObject, "Effects").text;
            Assert.That(headline, Is.Not.Empty);
            if (article.Medium == LaunchResultMedium.Mail)
            {
                Assert.That(body, Does.Contain("책임자님,"));
                Assert.That(body, Does.Contain($"{missionName} 시험 결과를 확인했습니다."));
                Assert.That(body, Does.Contain("정산과 후속 조치는 아래 항목으로 전달드립니다."));
                Assert.That(body, Does.Not.Contain($"{missionName} 시험이 성공했다."));
            }
            else
            {
                Assert.That(body, Does.Contain($"{missionName} 시험이 성공했다."));
                Assert.That(body, Does.Not.Contain("책임자님,"));
            }

            if (article.Medium == LaunchResultMedium.Mail)
            {
                Assert.That(body, Does.Contain(outcomeEvent.Description));
            }
            else
            {
                Assert.That(body, Does.Not.Contain("책임자님,"));
                Assert.That(body, Does.Not.Contain("했습니다."));
            }
            Assert.That(body, Does.Not.Contain("미션:"));
            Assert.That(body, Does.Not.Contain("판정:"));
            Assert.That(body, Does.Not.Contain("종료 사유:"));
            Assert.That(effects, Does.Contain("테스트 정산: 즉시 지원금 +600 / 분기 연구비 +75"));
            Assert.That(effects, Does.Contain(outcomeEvent.EffectsText));
            Assert.That(effects, Does.Not.Contain("성공 80%"));
            Assert.That(effects, Does.Not.Contain("부분 10%"));
            Assert.That(effects, Does.Not.Contain("실패 10%"));
            Assert.That(effects, Does.Not.Contain("굴림"));
            Assert.That(effects, Does.Not.Contain("안내:"));
        }

        [Test]
        public void LaunchNewspaperArticle_WritesMailBodyAsPersonalMessage()
        {
            LaunchNewspaperArticle article = LaunchNewspaperArticle.Create(
                CreateResult(new LaunchOutcomeEventResult(
                    LaunchOutcomeEventId.QuietBreakthrough,
                    "닫힌 문 안의 정답",
                    "공식 발표는 없었지만 성능표는 좋아졌습니다.",
                    "연구비 +75"),
                    visibility: TestVisibility.Private), "저고도 안정화");

            Assert.That(article.Medium, Is.EqualTo(LaunchResultMedium.Mail));
            Assert.That(article.Heading, Is.EqualTo("[시험 결과] 저고도 안정화 - 비공개 성능 개선 확인"));
            Assert.That(article.Edition, Is.EqualTo("2024년 2분기 내부 메일"));
            Assert.That(article.Body, Does.StartWith("책임자님,"));
            Assert.That(article.Body, Does.Contain("저고도 안정화 시험 결과를 확인했습니다."));
            Assert.That(article.Body, Does.Contain("정산과 후속 조치는 아래 항목으로 전달드립니다."));
            Assert.That(article.Body, Does.Not.Contain("시험이 성공했다."));
            Assert.That(article.Body, Does.Not.Contain("추가 사건은 기록되지 않았다."));
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
        public void ResultReport_ContainsSeparateNewspaperAndMailPrefabs()
        {
            ResearchResultReportController report = CreateReport();
            NewspaperReveal[] reveals = report.GetComponentsInChildren<NewspaperReveal>(true);

            Assert.That(reveals, Has.Length.EqualTo(2));
            Assert.That(FindReveal(report, LaunchResultMedium.Newspaper).name, Is.EqualTo("NewspaperReveal"));
            Assert.That(FindReveal(report, LaunchResultMedium.Mail).name, Is.EqualTo("MailReveal"));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/MailReveal.prefab"), Is.Not.Null);
        }

        [Test]
        public void ResultReport_AssignedMailSpriteUsesEmailAsset()
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData result = CreateResult(null, visibility: TestVisibility.Private);

            report.Initialize(ResearchFlowSession.GetOrCreate(), result, null);

            var mail = FindReveal(report, LaunchResultMedium.Mail);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Sprite emailSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/05. Arts/UI/Email/Email.png");
            Sprite mailSprite = (Sprite)typeof(NewspaperReveal).GetField("presentationSprite", flags).GetValue(mail);
            Image image = (Image)typeof(NewspaperReveal).GetField("newspaperImage", flags).GetValue(mail);

            Assert.That(emailSprite, Is.Not.Null);
            Assert.That(mailSprite, Is.SameAs(emailSprite));
            Assert.That(image.sprite, Is.SameAs(emailSprite));
        }

        [Test]
        public void ResultReport_MailUsesEmailPaneLayoutAndColorPhoto()
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData result = CreateResult(null, visibility: TestVisibility.Private);

            report.Initialize(ResearchFlowSession.GetOrCreate(), result, null);

            TMP_Text headline = FindText(report.gameObject, "Headline");
            TMP_Text body = FindText(report.gameObject, "Body");
            TMP_Text effects = FindText(report.gameObject, "Effects");
            TMP_Text edition = FindText(report.gameObject, "Edition");
            RawImage photo = FindSelectedComponent<RawImage>(report.gameObject, "Photo");
            var mail = FindReveal(report, LaunchResultMedium.Mail);
            RectTransform effectsBackground = GetPrivateField<RectTransform>(mail, "effectsBackground");

            Assert.That(headline.rectTransform.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(headline.rectTransform.anchorMax.y, Is.LessThan(0.82f));
            Assert.That(headline.fontSizeMax, Is.LessThanOrEqualTo(16f));
            Assert.That(edition.rectTransform.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(body.rectTransform.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(body.rectTransform.anchorMax.y, Is.LessThan(0.66f));
            Assert.That(photo.rectTransform.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(photo.rectTransform.anchorMax.y, Is.LessThan(0.44f));
            Assert.That(effectsBackground.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(effectsBackground.anchorMax.y, Is.LessThan(0.28f));
            Assert.That(effects.rectTransform.anchorMin.x, Is.GreaterThan(0.3f));
            Assert.That(effects.rectTransform.anchorMax.y, Is.LessThan(0.26f));
            Assert.That(GetRawGraphicMaterial(photo), Is.Null);
        }

        [Test]
        public void ResultReport_NewspaperRestoresSavedLayoutAndPrintedPhoto()
        {
            ResearchResultReportController report = CreateReport();
            ResearchLaunchResultData publicResult = CreateResult(null, visibility: TestVisibility.Public);

            report.Initialize(ResearchFlowSession.GetOrCreate(), publicResult, null);

            TMP_Text headline = FindText(report.gameObject, "Headline");
            RawImage photo = FindSelectedComponent<RawImage>(report.gameObject, "Photo");

            Assert.That(headline.rectTransform.anchorMin.x, Is.EqualTo(0.24772727f).Within(0.0001f));
            Assert.That(headline.rectTransform.anchorMax.y, Is.EqualTo(0.9446773f).Within(0.0001f));
            Assert.That(headline.fontSizeMax, Is.EqualTo(28f).Within(0.0001f));
            Assert.That(photo.rectTransform.anchorMin.x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(GetRawGraphicMaterial(photo), Is.Not.Null);
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

            operation.InitializeForTests();
            ResearchPrototypeModel model = ResearchFlowSession.GetOrCreate().Model;
            SetPrivateField(model, "pendingFreeResearchEngine", EnginePresetId.Engine01);
            SetPrivateField(model, "pendingPublicPressure", true);
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
                .Invoke(null, new object[] { true, "다음 설계 진입 비용 -50" });

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
            bool finalMissionWon = false,
            bool deadlineMissed = false,
            ResearchGrade grade = ResearchGrade.B)
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
                grade,
                600,
                75,
                finalMissionWon,
                deadlineMissed,
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
                    return "돈 냄새를 맡은 사람들";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "그래프가 우리 편";
                case LaunchOutcomeEventId.PublicPressure:
                    return "박수 뒤의 독촉장";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "추락 현장의 힌트";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "못 뜬 덕분에 산 장비";
                case LaunchOutcomeEventId.PadDamage:
                    return "수리비 착륙";
                case LaunchOutcomeEventId.QuietLessons:
                    return "조용히 망하고 조용히 배움";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "공개 처형식";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "닫힌 문 안의 정답";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "망한 김에 본 것";
                case LaunchOutcomeEventId.Whistleblower:
                    return "내부 고발자";
                case LaunchOutcomeEventId.FinalProof:
                    return "최종 검증 인정";
                case LaunchOutcomeEventId.FinalFailure:
                    return "최종 검증 실패";
                default:
                    return id.ToString();
            }
        }

        private static string GetEventDescription(LaunchOutcomeEventId id)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "공개 발사 뒤 후원 기관들이 뒤늦게 줄을 섰습니다. 기술 설명보다 예산 회의가 먼저 잡혔습니다.";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "비행 기록이 보기 드물게 깨끗했습니다. 연구진은 실패 원인 대신 개선 목록을 적었습니다.";
                case LaunchOutcomeEventId.PublicPressure:
                    return "성공 축하가 끝나기도 전에 다음 발사 일정을 묻는 연락이 왔습니다.";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "추락 지점에서 설계 결함 하나가 또렷하게 드러났습니다. 사고는 났지만 점검 방향은 잡혔습니다.";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "로켓은 뜨지 못했지만 장비는 멀쩡했습니다. 실패 현장에서 다음 시도 비용을 건졌습니다.";
                case LaunchOutcomeEventId.PadDamage:
                    return "발사는 짧았고 견적서는 길었습니다. 시설팀은 로켓보다 발사대를 먼저 봤습니다.";
                case LaunchOutcomeEventId.QuietLessons:
                    return "밖은 몰랐고 연구실은 알았습니다. 체면은 지켰고 데이터는 남았습니다.";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "실패 장면이 너무 잘 보였습니다. 후원 기관은 박수 대신 예산 검토표를 꺼냈습니다.";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "공식 발표는 없었지만 성능표는 좋아졌습니다. 연구팀은 조용히 다음 설계를 고쳤습니다.";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "실패 순간에 평소엔 보이지 않던 흔들림이 잡혔습니다. 망했지만 빈손은 아니었습니다.";
                case LaunchOutcomeEventId.Whistleblower:
                    return "관계자가 비공개 실패 기록과 예산 처리에 \"비리 관계 있다\"고 주장했습니다. 후원 기관이 다음 분기 지원을 깎았습니다.";
                case LaunchOutcomeEventId.FinalProof:
                    return "저전력 검증을 통과했습니다. 아르테미스 발사 체계가 최종 인정됐습니다.";
                case LaunchOutcomeEventId.FinalFailure:
                    return "저전력 검증은 최종 통과 기준에 못 미쳤습니다. 남은 기록은 다음 판단 자료로 넘겨졌습니다.";
                default:
                    return id.ToString();
            }
        }

        private static string GetEventEffectsText(LaunchOutcomeEventId id)
        {
            switch (id)
            {
                case LaunchOutcomeEventId.SponsorBoost:
                    return "연구비 +500\n분기 연구비 +100";
                case LaunchOutcomeEventId.CleanTelemetry:
                    return "연구비 +100\n1번 엔진 완성도 +5\n1번 엔진 연료 탱크 용량 +4\n1번 엔진 다음 일반 연구 시간 면제 (비용 정상 지불)";
                case LaunchOutcomeEventId.PublicPressure:
                    return "연구비 +350\n다음 행동이 대기면 분기 예산 -50 / 설계 진입이면 비용 -50";
                case LaunchOutcomeEventId.NearMissInspection:
                    return "다음 설계 진입 비용 -50\n1번 엔진 완성도 +3";
                case LaunchOutcomeEventId.RecoveredPayload:
                    return "저고도 안정화 다음 설치비 -20% (최대 300)";
                case LaunchOutcomeEventId.PadDamage:
                    return "연구비 -200\n다음 설계 진입 비용 +50\n1번 엔진 완성도 -3";
                case LaunchOutcomeEventId.QuietLessons:
                    return "1번 엔진 완성도 +4\n1번 엔진 냉각 능력 +3";
                case LaunchOutcomeEventId.MediaBacklash:
                    return "분기 연구비 -150\n다음 발사: 공개 성공 이벤트 연구비 -25% / 비공개 선택 시 소멸";
                case LaunchOutcomeEventId.QuietBreakthrough:
                    return "연구비 +75\n1번 엔진 완성도 +8\n1번 엔진 연료 탱크 용량 +2";
                case LaunchOutcomeEventId.UsefulFailureData:
                    return "1번 엔진 완성도 +2\n1번 엔진 연료 탱크 용량 +1";
                case LaunchOutcomeEventId.Whistleblower:
                    return "분기 연구비 -100";
                case LaunchOutcomeEventId.FinalProof:
                    return "효율 검증 통과 · 최종 미션 성공\n1번 엔진 완성도 +10\n1번 엔진 연료 탱크 용량 +5";
                case LaunchOutcomeEventId.FinalFailure:
                    return "최종 미션 실패";
                default:
                    return id.ToString();
            }
        }

        private static TMP_Text FindText(GameObject root, string name)
        {
            return FindSelectedComponent<TMP_Text>(root, name);
        }

        private static T FindSelectedComponent<T>(GameObject root, string name) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).Single(item =>
            {
                NewspaperReveal reveal = item.GetComponentInParent<NewspaperReveal>(true);
                return item.name == name && reveal != null && reveal.gameObject.activeSelf;
            });
        }

        private static NewspaperReveal FindReveal(ResearchResultReportController report, LaunchResultMedium medium)
        {
            FieldInfo field = typeof(NewspaperReveal).GetField("medium", BindingFlags.Instance | BindingFlags.NonPublic);
            return report.GetComponentsInChildren<NewspaperReveal>(true)
                .Single(item => (LaunchResultMedium)field.GetValue(item) == medium);
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static Material GetRawGraphicMaterial(Graphic graphic)
        {
            FieldInfo field = typeof(Graphic).GetField("m_Material", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Material)field.GetValue(graphic);
        }
    }
}
