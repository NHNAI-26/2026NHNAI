# 연구 단계 전체 개발 단계별 계획

> 연구 단계 변경 기획을 반영한 구현 계획 문서.  
> 이 문서는 연구 담당 범위를 정리하고, 설계·발사 담당과 만나는 경계를 고정한다.

## Document State

| Field | Value |
| --- | --- |
| Interview state | `active` |
| Working language | Korean |
| Current revision | 1 |
| Last updated | 2026-09-04T19:21:11+09:00 |
| Project or workspace root | `C:\Users\angel\OneDrive\문서\GitHub\2026NHNAI` |
| Base path | `docs/specs/research-stage-development-plan.md` |
| Korean mirror path | not required; this active document is already Korean |
| Explicit finish received | `no` |
| Next authorized action | research-stage implementation requires separate user authorization |

## 1. 목표 요약

연구 단계는 플레이어가 매 분기 프로젝트 운영 판단을 하는 메인 화면이다. 여기서 플레이어는 날짜, 연구비, 분기 연구비, 네 단계 진행도, 현재 포함 4분기 환경 예보를 보고 아래 행동 중 하나를 고른다.

1. 활성 단계 일반 연구
2. 활성 단계 집중 연구
3. 활성 단계 설계 화면 진입
4. 이전 단계 재선택 후 연구 또는 설계 진입
5. 1분기 대기

바뀐 기획의 핵심은 `시뮬레이션 버튼`이 곧바로 결과를 굴리는 버튼이 아니라 `로켓 설계 화면 진입`이라는 점이다. 설계 화면에 들어가는 것만으로는 비용, 분기, 시험 횟수를 소비하지 않는다. 비용 차감, 결과 고정, 분기 소비는 설계 화면의 최종 `발사` 버튼 이후에만 발생한다.

## 2. 근거와 변경 요약

| ID | Kind | Statement | Evidence / rationale | Status | Consequence |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | 사용자는 연구 단계 담당이며, 부품을 달고 날리는 것은 시뮬레이션/설계 쪽이고 그 외 운영 구간이 연구 단계라고 정정했다. | User message in this task thread | active | 연구 단계 계획은 운영 UI와 설계 진입 경계 중심으로 작성한다. |
| UD-002 | user decision | 연구 단계 기획 문서가 바뀌었으니 이를 참조해 연구 단계 전체 개발 계획 문서를 저장하라고 요청했다. | User message, 2026-09-04 | active | 이 문서를 `docs/specs/research-stage-development-plan.md`에 생성한다. |
| SF-001 | sourced fact | 전체 흐름은 연구/대기 또는 시뮬레이션 진입으로 갈라지고, 시뮬레이션 진입은 로켓 설계 단계로 이동한다. | `docs/artemis-2026-gdd/02_전체_플레이_흐름.md` section 1, 3 | active | 연구 단계는 설계 씬 진입 요청까지만 만들고 발사 결과 계산은 설계 확정 뒤로 미룬다. |
| SF-002 | sourced fact | 설계 단계 진입과 설계 수정은 비용과 분기를 소비하지 않으며, 최종 발사 버튼만 되돌릴 수 없는 행동이다. | `docs/artemis-2026-gdd/02_전체_플레이_흐름.md` section 3; `03_시간_연구비_경제.md` section 4 | active | 연구 화면의 진입 버튼은 비용을 차감하지 않는다. |
| SF-003 | sourced fact | 연구는 일반 +6, 집중 +10, 1분기 소비, 실패 없음, 진행도 100 상한이다. | `docs/artemis-2026-gdd/04_연구_진행도_단계해금.md` section 2; `12_밸런스_데이터표.md` section 1 | active | 연구 로직의 첫 구현 대상이다. |
| SF-004 | sourced fact | 설계·발사 가능 조건은 Engine/Rocket/Orbit 진행도 20 이상, Moon 진행도 50 이상이다. | `docs/artemis-2026-gdd/04_연구_진행도_단계해금.md` section 3; `12_밸런스_데이터표.md` section 2 | active | 연구 화면의 시뮬레이션 진입 버튼 잠금 조건으로 쓴다. |
| SF-005 | sourced fact | 다음 단계 해금은 선행 단계 진행도와 선행 발사 최고 등급 C 이상을 모두 요구한다. | `docs/artemis-2026-gdd/04_연구_진행도_단계해금.md` section 4; `12_밸런스_데이터표.md` section 3 | active | 결과 적용 후 연구 화면 복귀 시 해금 갱신이 필요하다. |
| SF-006 | sourced fact | 연구 화면은 프로젝트 운영 화면이며, 상단 정보 바, 단계 카드, 환경 예보, 행동 버튼을 한 화면에 보여줘야 한다. | `docs/artemis-2026-gdd/11_UI_UX_화면설계.md` sections 2-6 | active | UI 구현 순서와 완료 기준을 정한다. |
| SF-007 | sourced fact | 기술 구조는 `GameFlowController`, `TimeManager`, `EconomyManager`, `ResearchManager`, `ForecastManager`, `DesignSceneController`, `ResultApplier` 분리를 권장한다. | `docs/artemis-2026-gdd/13_기술구조_Unity.md` section 3 | active | 임시 프로토타입에서 정식 구조로 옮긴다. |
| SF-008 | sourced fact | 현재 구현된 프로토타입은 `ResearchPrototypeModel`과 `ResearchPrototypeController`가 있으며, `CreateTestPreview`와 `ApplyPendingTestResult`가 즉시 더미 결과를 다룬다. | `Assets/01. Scripts/Research/ResearchPrototypeModel.cs`; `Assets/01. Scripts/Research/ResearchPrototypeController.cs` | active | 새 기획에 맞게 더미 결과 직접 적용을 제거하거나 개발 전용으로 격리해야 한다. |
| AR-001 | agent recommendation | 정식 개발은 먼저 순수 상태/로직을 분리하고, 그 다음 UI, 마지막에 설계 씬 인계로 확장한다. | Unity 기획 구조와 현재 프로토타입 상태상 작은 diff로 이동 가능 | proposed | 단계별 구현 실패 위험을 줄인다. |
| OI-001 | unresolved item | 정식 UI를 IMGUI로 계속 갈지, UGUI 프리팹으로 갈지 아직 명시 결정이 없다. | 현재는 IMGUI 프로토타입, 문서는 운영 화면 레이아웃만 지정 | open | 48시간 버전은 IMGUI/UGUI 중 하나를 고정해야 한다. 현재 추천은 UGUI 전환이다. |

## 3. 개발 단계별 계획

### Phase 0. 현재 프로토타입 정리와 기준 잠금

목표: 기존 연구 프로토타입을 버리지 않고 새 기획 기준으로 어디까지 쓸지 결정한다.

작업:

- `ResearchPrototypeModel`의 기존 수치가 `12_밸런스_데이터표.md`와 맞는지 확인한다.
- `CreateTestPreview`가 발사 비용을 바로 차감하는 현재 구조를 새 기획과 충돌 항목으로 표시한다.
- `ApplyPendingTestResult`는 정식 흐름에서는 제거 대상이다. 임시 테스트용이면 `DEBUG` 또는 Editor-only 경로로 격리한다.
- 연구 화면의 "시뮬레이션 인계" 표현을 "설계 진입"으로 바꾼다.

완료 기준:

- 연구 단계에서 직접 결과를 굴리지 않는다는 구현 경계가 코드와 문서에 반영된다.
- 현재 프로토타입으로 남길 것과 정식화할 것이 분리된다.

### Phase 1. 연구 도메인 모델 정식화

목표: UI와 씬에 묶이지 않는 연구/경제/시간 상태를 만든다.

작업:

- `GameState`에 날짜, 남은 분기, 현재 연구비, 분기 연구비, 단계별 진행도, 발사 횟수, 최고 등급, 해금 상태를 둔다.
- `StageState`는 `id`, `progress`, `attemptCount`, `bestGrade`, `unlocked`만 가진다.
- `StageConfig`와 `GameBalanceConfig`에 연구 비용, 집중 연구 비용, 발사 비용, 최소 진행도, 해금 조건, 연구 증가량을 둔다.
- 연구 행동 함수는 `validate_stage_unlocked -> validate_funds -> subtract_cost -> add_progress -> clamp_progress -> check_unlocks -> end_quarter -> pay_quarterly_funding -> advance_forecast -> check_deadline` 순서를 따른다.
- 대기 행동 함수는 비용 없이 `end_quarter -> pay_quarterly_funding -> advance_forecast -> check_deadline`만 수행한다.

완료 기준:

- 일반 연구는 항상 +6, 집중 연구는 항상 +10.
- 연구 실패 없음.
- 진행도는 100을 넘지 않음.
- 연구와 대기는 정확히 1분기만 소비.
- 자금 부족이면 상태를 바꾸지 않고 실패 결과를 반환.

### Phase 2. 환경 예보와 단계 선택 구현

목표: 연구 화면 판단 재료를 완성한다.

작업:

- `ForecastManager`는 전체 36분기 환경 스케줄을 만들고 현재 포함 4분기만 UI에 제공한다.
- 환경 생성은 문서의 가중치와 제약을 따른다: 위험 환경 최대 2연속, 모든 4분기 창 안에 안정 또는 최적 최소 1회.
- 단계 선택은 해금된 모든 단계와 이전 단계를 포함한다.
- 선택 단계가 바뀌면 환경 예보 카드의 보정 수치를 해당 단계 기준으로 다시 계산한다.
- 잠긴 단계도 카드로 보이게 하고 필요한 해금 조건을 표시한다.

완료 기준:

- 현재 포함 4분기 예보가 항상 보인다.
- 미래 예보 클릭으로 예약 발사나 즉시 이동은 발생하지 않는다.
- 잠긴 단계의 조건이 항상 보인다.
- 이전 단계는 다음 단계가 열려도 계속 선택 가능하다.

### Phase 3. 프로젝트 운영 화면 UI 구현

목표: 문서의 프로젝트 운영 화면을 실제 플레이 가능한 연구 화면으로 만든다.

작업:

- 상단 정보 바: 프로젝트명, 현재 연도/분기, 남은 분기, 현재 연구비, 분기 연구비.
- 좌측 단계 카드: 단계명, 진행도 게이지와 숫자, 잠금/활성, 최고 등급, 발사 횟수, 해금 조건, 선택 상태.
- 우측 예보 카드: 현재 포함 4분기, 환경명, 선택 단계 보정, 위험도 라벨.
- 행동 영역: 일반 연구, 집중 연구, 설계 진입, 1분기 대기.
- 버튼은 비용, 효과, 시간 정보를 같이 표시한다.
- 연구 완료 후 별도 결과 화면 없이 1~2초 안에 비용 감소, 게이지 증가, 날짜 이동, 정기 연구비 지급, 예보 이동을 보여준다.
- 연구비 부족 또는 진행도 부족이면 버튼을 비활성화하고 필요한 수치와 보유 수치를 표시한다.

완료 기준:

- 16:9 화면에서 스크롤 없이 핵심 정보가 보인다.
- 색만으로 상태를 전달하지 않고 텍스트도 같이 표시한다.
- 버튼을 눌렀는데 아무 반응 없는 상태가 없다.
- 튜토리얼 팝업은 최대 3개 규칙을 지킨다.

### Phase 4. 설계 화면 진입 경계 구현

목표: 연구 담당 범위에서 설계 담당으로 넘기는 계약을 만든다.

작업:

- 연구 화면의 설계 진입 버튼은 아래만 검사한다:
  - 단계 해금 여부
  - 최소 진행도
  - 현재 연구비가 발사 비용 이상인지
  - 현재 환경 존재 여부
- 조건 통과 시 비용과 분기를 차감하지 않고 `DesignData` 생성을 요청한다.
- `DesignData`에는 `stageId`, `year`, `quarter`, `environmentId`, `mapSeed`, `targetPathId`, 초기 부품 배치, 힘, 방향, `designFit` 기본값을 둔다.
- 같은 분기와 같은 단계에서는 다시 설계 화면에 들어가도 같은 `mapSeed`와 목표 경로가 나오게 한다.
- 설계 화면에서 연구 단계로 돌아오면 임시 설계 데이터는 버리고 비용/시간/시험 횟수는 그대로 둔다.

완료 기준:

- 설계 진입만으로 돈, 시간, 발사 횟수가 변하지 않는다.
- 진행도 부족 시 "현재 진행도 / 최소 진행도"가 보인다.
- 연구 화면 복귀 시 기존 연구 상태가 보존된다.

### Phase 5. 발사 결과 복귀와 해금 반영

목표: 자동 발사/결과 보고서 이후 연구 화면 상태가 정확히 갱신되게 한다.

작업:

- `ResultApplier.ApplyOnce`를 통해 결과를 한 번만 반영한다.
- 결과 등급별 진행도, 즉시 지원금, 분기 연구비 변화는 `12_밸런스_데이터표.md`를 따른다.
- 발사 결과로 진행도가 해금 임계치를 넘으면 같은 결과 처리 뒤 즉시 다음 단계가 열린다.
- Moon 결과가 B 이상이면 승리 화면으로 가고, C/F면 시간이 남을 때 연구 화면으로 돌아온다.
- 2026 Q4 마지막 행동 결과가 승리가 아니면 패배 화면으로 간다.

완료 기준:

- F는 다음 단계 검증으로 인정하지 않음.
- C는 다음 단계 검증으로 인정하지만 Moon 승리로는 인정하지 않음.
- 실패해도 진행도 감소 없음.
- 결과 중복 적용 없음.

### Phase 6. 테스트와 플레이 확인

목표: 연구 담당 범위의 회귀를 잠근다.

테스트 케이스:

- 일반 연구 +6, 집중 연구 +10.
- 진행도 100 상한.
- 연구비 부족 시 연구 상태 변화 없음.
- 대기 시 비용 없음, 분기 종료, 정기 연구비 지급.
- 2026 Q4 마지막 행동 후 승리 아니면 패배.
- Engine 50 + Engine 발사 C 이상에서 Rocket 해금.
- Rocket 55 + Rocket 발사 C 이상에서 Orbit 해금.
- Orbit 60 + Orbit 발사 C 이상에서 Moon 해금.
- 진행도만 충분하고 최고 등급이 없거나 F뿐이면 해금 안 됨.
- 설계 진입은 비용/분기/발사 횟수 미소비.
- 설계 복귀는 연구 상태 보존.
- 선택 단계 변경 시 예보 보정 수치 갱신.

검증 명령:

```powershell
dotnet build .\NHNAI2026.slnx -nologo -v minimal
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults .\Logs\editmode-results.xml -quit
```

Unity Editor가 이미 프로젝트를 열고 있으면 batchmode는 잠길 수 있다. 이 경우 Editor 안에서 PlayMode 수동 확인 또는 연결 가능한 Unity CLI/Pipeline으로 대체한다.

## 4. 요구사항 표

| ID | Requirement | Type | Source IDs | Priority | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- |
| R-001 | 연구 화면은 날짜, 남은 분기, 현재 연구비, 분기 연구비를 항상 표시한다. | functional | SF-006 | must | active | 운영 화면 육안 확인 및 UI 테스트 |
| R-002 | 활성 단계는 일반 연구와 집중 연구를 수행할 수 있다. | functional | SF-003 | must | active | EditMode test: progress/funds/turn mutation |
| R-003 | 연구 행동은 실패하지 않고 1분기를 소비한다. | functional | SF-003 | must | active | EditMode test: deterministic research |
| R-004 | 자금 부족 시 해당 행동은 비활성화되고 필요/보유 연구비가 표시된다. | functional | SF-006 | must | active | UI inspection and model test |
| R-005 | 시뮬레이션 진입 버튼은 설계 화면 진입 버튼으로 동작하며 진입만으로 비용과 분기를 소비하지 않는다. | functional | SF-001, SF-002 | must | active | Model and scene-transition test |
| R-006 | 설계 진입 조건은 단계 해금, 최소 진행도, 발사 비용 보유 여부를 검사한다. | functional | SF-004, SF-002 | must | active | Model test for each failure branch |
| R-007 | 현재 포함 4분기 예보와 선택 단계별 환경 보정을 표시한다. | functional | SF-006, SF-007 | must | active | Forecast model test and UI inspection |
| R-008 | 다음 단계가 해금되어도 이전 단계 연구와 설계 진입은 계속 가능하다. | functional | SF-001, SF-005 | must | active | Full flow test |
| R-009 | 결과 복귀 후 진행도, 발사 횟수, 최고 등급, 연구비, 분기 연구비, 해금 상태가 한 번만 반영된다. | functional | SF-005, SF-007 | must | active | ResultApplier idempotency test |
| R-010 | Moon C는 계속 진행이고 Moon B 이상만 승리다. | functional | SF-005 | must | active | Moon result tests |
| R-011 | 연구 화면은 16:9 화면에서 핵심 정보를 스크롤 없이 보여준다. | quality | SF-006 | should | active | 1280x720 and 1920x1080 screenshot review |
| R-012 | 기존 `CreateTestPreview`/`ApplyPendingTestResult` 직접 결과 적용 흐름은 정식 연구 흐름에서 제거하거나 개발 전용으로 격리한다. | technical | SF-008, SF-001, SF-002 | must | active | Code review confirms no production path |

## 5. 구현 순서와 파일 방향

권장 순서:

1. `Assets/01. Scripts/Research` 아래 프로토타입 모델을 정식 `GameState`, `StageState`, `ResearchManager`, `ForecastManager` 구조로 분리한다.
2. 기존 IMGUI `ResearchPrototypeController`는 잠깐 유지하되, 버튼 문구와 동작을 새 기획에 맞춘다.
3. 설계 담당 코드가 준비되기 전까지는 `EnterDesign(stage)`가 `DesignData`를 만들고 로그/더미 화면으로 검증 가능하게 한다.
4. 정식 UI는 `11_UI_UX_화면설계.md` 레이아웃 기준으로 UGUI 프리팹 또는 기존 UI 패턴에 맞춰 옮긴다.
5. 결과 적용은 연구 모델 안이 아니라 `ResultApplier` 한 곳으로 이동한다.
6. 테스트를 붙인 뒤 프로토타입 더미 결과 적용 경로를 제거한다.

## 6. 위험과 대응

| ID | Kind | Risk, conflict, or dependency | Likelihood / impact | Mitigation | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | conflict | 기존 프로토타입은 연구 화면에서 바로 테스트 결과를 만들고 적용하지만, 새 기획은 설계 화면의 발사 버튼 이후 결과를 고정한다. | high / high | 직접 결과 적용 경로를 개발 전용으로 격리하고 정식 흐름은 `EnterDesign`으로 바꾼다. | SF-001, SF-002, SF-008, R-005, R-012 | active |
| RK-002 | dependency | 설계 화면 담당 구현이 없으면 연구 단계의 설계 진입 이후 실제 흐름이 막힌다. | medium / medium | 초기에는 `DesignData` 생성과 씬 전환 계약만 구현하고, 설계 담당과 인터페이스를 맞춘다. | R-005, R-006 | active |
| RK-003 | risk | UI를 IMGUI로 오래 끌면 정식 화면 품질과 16:9 배치 검증이 늦어진다. | medium / medium | 연구 로직 검증 후 빠르게 UGUI 또는 팀 표준 UI로 이전한다. | OI-001, R-011 | open |
| RK-004 | risk | 수치가 코드 여러 곳에 흩어지면 변경된 밸런스 문서 반영이 어려워진다. | medium / high | `GameBalanceConfig`/`StageConfig` 단일 출처로 묶는다. | SF-003, SF-004, R-002, R-006 | active |

## 7. Open Items

| ID | Item | State | Why it matters / consequence | Current recommendation | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-001 | 정식 연구 화면 UI 기술 선택: IMGUI 유지, UGUI 전환, UIToolkit 전환 중 결정 필요 | open | 화면 품질, 프리팹 작업량, 테스트 방식이 달라진다. | UGUI 전환 추천. 기존 `UnityEngine.UI`/`TextMeshPro` 참조가 있고 게임잼 UI에 충분하다. | User / UI owner | 연구 로직 테스트 통과 후 UI 정식화 시작 전 |

## 8. Coverage and Consistency Check

| Planning area | State | Supporting IDs | Remaining gap or note |
| --- | --- | --- | --- |
| Outcome | covered | UD-001, UD-002, SF-001 | 연구 담당 경계 반영 |
| Users and stakeholders | covered | UD-001 | 연구 담당 구현자와 설계 담당 경계 |
| Scope | covered | SF-001, SF-002, SF-006 | 설계 내부 조작은 별도 담당 |
| Non-goals | covered | SF-002, SF-008 | 연구 단계에서 발사 결과 직접 적용 금지 |
| Core flow | covered | SF-001, SF-002, SF-007 | 설계 진입과 복귀 포함 |
| Constraints | covered | SF-003, SF-004, SF-005, SF-006 | 수치는 밸런스 문서 우선 |
| Success evidence | covered | R-001 through R-012 | 테스트와 화면 확인 필요 |
| Risks and dependencies | covered | RK-001 through RK-004 | UI 기술 선택만 open |
| Unresolved decisions | partial | OI-001 | 정식 UI 기술 선택 남음 |
| Handoff and authorization | covered | UD-002 | 구현은 별도 승인 필요 |

## 9. Interview Checkpoint

- **Latest user message incorporated:** 연구 단계 기획 변경을 참조해 연구 단계 전체 개발 단계별 계획 문서를 저장하라는 요청, revision 1.
- **Latest sourced evidence incorporated:** SF-001 through SF-008 from changed GDD docs and current prototype code.
- **Ledger transitions applied:** Initial UD/SF/AR/OI/RK/R IDs created.
- **Affected sections reconciled:** Goal, phase plan, requirements, risks, open items.
- **Contradictory active items check:** passed.
- **Traceability check:** passed; all active requirements have source IDs.
- **Current focus:** research-stage implementation handoff.
- **Next question IDs:** none.
- **Resume point:** decide OI-001 before 정식 UI 구현, or begin implementation from Phase 0 if UGUI recommendation is accepted.

## 10. Finalization and Handoff

Planning interview is not explicitly finished. This document is still usable as the current implementation guide.

- **Suggested next executor:** `$feature-implementer` or direct implementation pass.
- **First implementation target:** Phase 0 and Phase 1.
- **Do not implement without separate authorization:** source-code changes, scene edits, tests that change tracked assets, commits, pull requests.
