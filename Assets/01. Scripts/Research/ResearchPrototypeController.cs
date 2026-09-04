using UnityEngine;
using UnityEngine.SceneManagement;

namespace Border.Research
{
    public sealed class ResearchPrototypeController : MonoBehaviour
    {
        private const string TargetSceneName = "ResearchTestScene";

        private ResearchPrototypeModel model;
        private ResearchStageId selectedStage;
        private ResearchDesignEntryData? pendingDesignEntry;
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInResearchTestScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != TargetSceneName || FindFirstObjectByType<ResearchPrototypeController>() != null)
            {
                return;
            }

            var host = new GameObject("Research Prototype Controller");
            host.AddComponent<ResearchPrototypeController>();
        }

        private void Awake()
        {
            model = new ResearchPrototypeModel();
        }

        private void OnGUI()
        {
            EnsureStyles();

            int panelWidth = Mathf.Min(720, Screen.width - 32);
            int panelHeight = Mathf.Min(680, Screen.height - 32);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;
            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, boxStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            GUILayout.Space(12);
            DrawStageTabs();
            GUILayout.Space(10);
            DrawSelectedStage();
            GUILayout.Space(10);
            DrawDesignEntry();
            GUILayout.Space(10);
            DrawLog();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label("ARTEMIS: 2026 연구 단계 프로토타입", titleStyle);
            GUILayout.Label("연구비와 마감 사이에서 연구, 대기, 설계 진입을 고른다.", bodyStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"날짜: {model.Year} Q{model.Quarter}", headerStyle, GUILayout.Width(140));
            GUILayout.Label($"남은 분기: {model.RemainingTurns}", headerStyle, GUILayout.Width(130));
            GUILayout.Label($"연구비: {model.Funds}", headerStyle, GUILayout.Width(130));
            GUILayout.Label($"분기 연구비: {model.QuarterlyFunding}", headerStyle, GUILayout.Width(160));
            if (GUILayout.Button("초기화", buttonStyle, GUILayout.Width(90)))
            {
                model.Reset();
                pendingDesignEntry = null;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStageTabs()
        {
            GUILayout.BeginHorizontal();
            foreach (ResearchStageConfig config in ResearchPrototypeModel.GetStageConfigs())
            {
                ResearchStageState stage = model.GetStage(config.Id);
                GUI.enabled = stage.Unlocked;
                string label = stage.Unlocked ? config.DisplayName : $"{config.DisplayName}\n잠김";
                if (GUILayout.Toggle(selectedStage == config.Id, label, buttonStyle, GUILayout.Height(42)))
                {
                    selectedStage = config.Id;
                }

                GUI.enabled = true;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.Repaint && !stage.Unlocked)
                {
                    GUI.Label(new Rect(lastRect.x, lastRect.yMax + 2, lastRect.width, 38), GetUnlockConditionText(config.Id), smallStyle);
                }
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(40);
        }

        private void DrawSelectedStage()
        {
            ResearchStageConfig config = ResearchPrototypeModel.GetStageConfig(selectedStage);
            ResearchStageState stage = model.GetStage(selectedStage);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label(config.DisplayName, headerStyle);
            GUILayout.Label($"진행도 {stage.Progress}/100", bodyStyle);
            Rect progressRect = GUILayoutUtility.GetRect(1, 18, GUILayout.ExpandWidth(true));
            GUI.Box(progressRect, GUIContent.none);
            Rect fillRect = progressRect;
            fillRect.width *= Mathf.Clamp01(stage.Progress / 100f);
            Color previous = GUI.color;
            GUI.color = stage.Unlocked ? new Color(0.3f, 0.8f, 0.65f) : new Color(0.35f, 0.35f, 0.35f);
            GUI.Box(fillRect, GUIContent.none);
            GUI.color = previous;

            string bestGrade = stage.HasBestGrade ? stage.BestGrade.ToString() : "-";
            GUILayout.Label($"시험 조건: 진행도 {config.MinimumTestProgress}+ | 최고 등급: {bestGrade} | 시도: {stage.AttemptCount}", smallStyle);
            GUILayout.Label($"연구 기준 성공률: {model.CalculateSuccessChance(selectedStage)}%", smallStyle);
            GUILayout.Label(GetDesignEntryRequirementText(config, stage), smallStyle);
            GUILayout.Space(8);

            GUI.enabled = stage.Unlocked && !model.DeadlineReached;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"일반 연구  -{config.NormalResearchCost} / +{ResearchPrototypeModel.NormalResearchGain}", buttonStyle, GUILayout.Height(36)))
            {
                model.ExecuteResearch(selectedStage, false);
                pendingDesignEntry = null;
            }

            if (GUILayout.Button($"집중 연구  -{config.FocusedResearchCost} / +{ResearchPrototypeModel.FocusedResearchGain}", buttonStyle, GUILayout.Height(36)))
            {
                model.ExecuteResearch(selectedStage, true);
                pendingDesignEntry = null;
            }

            bool canEnterDesign = stage.Unlocked
                && stage.Progress >= config.MinimumTestProgress
                && model.Funds >= config.TestCost
                && !model.DeadlineReached;

            GUI.enabled = canEnterDesign;
            if (GUILayout.Button($"설계 진입  비용 {config.TestCost} 필요", buttonStyle, GUILayout.Height(36)))
            {
                if (model.TryEnterDesign(selectedStage, out ResearchDesignEntryData data) == ResearchActionResult.Success)
                {
                    pendingDesignEntry = data;
                }
                else
                {
                    pendingDesignEntry = null;
                }
            }

            GUILayout.EndHorizontal();
            GUI.enabled = !model.DeadlineReached;
            if (GUILayout.Button($"대기: 한 분기 넘기기 / 연구비 +{model.QuarterlyFunding}", buttonStyle, GUILayout.Height(32)))
            {
                model.WaitQuarter();
                pendingDesignEntry = null;
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private string GetUnlockConditionText(ResearchStageId stageId)
        {
            if (stageId == ResearchStageId.Engine)
            {
                return "기본 해금";
            }

            var previousId = (ResearchStageId)((int)stageId - 1);
            ResearchStageConfig previousConfig = ResearchPrototypeModel.GetStageConfig(previousId);
            ResearchStageState previousStage = model.GetStage(previousId);
            string bestGrade = previousStage.HasBestGrade ? previousStage.BestGrade.ToString() : "없음";
            return $"{previousConfig.DisplayName} {previousStage.Progress}/{previousConfig.UnlockProgressRequirement}, 최고 C 이상: {bestGrade}";
        }

        private string GetDesignEntryRequirementText(ResearchStageConfig config, ResearchStageState stage)
        {
            if (!stage.Unlocked)
            {
                return $"설계 진입 불가: {GetUnlockConditionText(config.Id)}";
            }

            if (stage.Progress < config.MinimumTestProgress && model.Funds < config.TestCost)
            {
                return $"설계 진입 불가: 진행도 {stage.Progress}/{config.MinimumTestProgress}, 연구비 {model.Funds}/{config.TestCost}";
            }

            if (stage.Progress < config.MinimumTestProgress)
            {
                return $"설계 진입 불가: 진행도 {stage.Progress}/{config.MinimumTestProgress}";
            }

            if (model.Funds < config.TestCost)
            {
                return $"설계 진입 불가: 연구비 {model.Funds}/{config.TestCost}";
            }

            return "설계 진입 가능: 비용과 분기는 발사 전까지 소비하지 않음";
        }

        private void DrawDesignEntry()
        {
            if (!pendingDesignEntry.HasValue)
            {
                return;
            }

            ResearchDesignEntryData data = pendingDesignEntry.Value;
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("설계 진입 데이터", headerStyle);
            GUILayout.Label($"단계: {data.StageId} | 날짜: {data.Year} Q{data.Quarter}", bodyStyle);
            GUILayout.Label($"맵 시드: {data.MapSeed} | 목표 경로: {data.TargetPathId}", bodyStyle);
            GUILayout.Label($"진행도: {data.CurrentProgress}/100 | 이전 단계 평균: {data.PrerequisiteAverage:0.0} | 경험 보정: +{data.ExperienceBonus}%p", bodyStyle);
            GUILayout.Label($"연구 기준 성공률: {model.CalculateSuccessChance(data.StageId)}%", bodyStyle);
            GUILayout.Label("비용, 분기, 발사 횟수, 결과는 아직 변하지 않습니다.", smallStyle);
            GUILayout.EndVertical();
        }

        private void DrawLog()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("상태", headerStyle);
            GUILayout.Label(model.LastMessage, bodyStyle);
            if (model.DeadlineReached)
            {
                GUILayout.Label("2026 Q4 종료. 연구 단계 프로토타입 종료.", bodyStyle);
            }

            GUILayout.EndVertical();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                wordWrap = true
            };
        }
    }
}
