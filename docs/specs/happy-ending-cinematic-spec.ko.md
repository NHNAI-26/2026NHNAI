# 해피엔딩 시네마틱 계획

> 이 문서 하나가 계획의 현재 상태다. 대화가 아니라 이 문서가 기준이다.

## Document State

| 항목 | 값 |
| --- | --- |
| Interview state | `explicitly-finished` |
| Working language | 한국어(인터뷰), 영어(정본) |
| Current revision | 6 |
| Last updated | 2026-09-06 (KST) |
| Project or workspace root | `C:\myGame\2026NHNAI` |
| Base path | `docs/specs/happy-ending-cinematic-spec.md` (영어 정본) |
| Korean mirror path | `docs/specs/happy-ending-cinematic-spec.ko.md` (이 파일) |
| Explicit finish received | `yes` ("계획확정하고 구현을 해줘", revision 4) |
| Next authorized action | Play Mode 가 비면 EditMode 스위트 실행과 게임 뷰 확인. 룩 작업과 Timeline 전환(R-016)은 별도 승인 필요 |

## Current Snapshot

- **Outcome:** 최종 미션 성공(`GameWon == true`)으로 게임이 끝났을 때, 지금의 텍스트 패널 대신 프롤로그와 짝을 이루는 해피엔딩 시네마틱을 재생한다.
- **Primary users or audience:** 게임을 끝까지 클리어한 플레이어. 부차적으로 데모/발표에서 엔딩을 보여줘야 하는 개발자.
- **In scope:** 해피엔딩 연출 7비트(날짜 카드 → 전화 대사 → 야간 발사 → 달 전환 → 달 항행 → 신문 → 페이드 아웃), 재생 트리거 지점, 로켓 외형 확보 방식, 구현 기술 선택.
- **Out of scope:** 새드엔딩 연출, 엔딩 크레딧, 세이브/로드, 승패 판정 규칙 변경.
- **Current decision focus:** 없음. 구조 결정은 모두 닫혔다.
- **Material unresolved items:** OI-007
- **Active question IDs:** 없음
- **확정된 축(rev 2~4):** 최종 성공 시 낙하산 6초와 결과 신문을 건너뛰고 곧바로 엔딩 시네마틱으로 진입한다(UD-004). 로켓은 씬 언로드 직전 보존한 실제 발사 로켓을 쓴다(UD-005). 3D 구간만 Timeline, 나머지는 프롤로그식 코루틴이다(UD-006). 페이드 후 `00_Title` 로 복귀한다(UD-007). 전화 대사는 3~4줄, 담담한 톤(UD-008). 총 길이 40~60초(UD-009).

## Outcome and Context

### Desired Outcome

승리 순간이 "MISSION COMPLETE" 라는 글자 한 줄이 아니라, 프롤로그가 던진 "2026년까지 유인 우주선을 달에 착륙시키십시오"라는 약속을 시각적으로 되갚는 장면으로 끝나야 한다. 플레이어가 8년간 굴린 프로젝트가 실제로 달로 향하는 모습을 보고 끝내는 것이 목적이다.

### Problem and Background

승패 분기 자체는 이미 코드에 있고 테스트로 잠겨 있다(SF-001, SF-002, SF-009). 없는 것은 연출이다. 현재 해피엔딩과 새드엔딩은 같은 프리팹, 같은 본문, 같은 버튼을 쓰고 제목 문자열만 다르다. 사용자는 해피엔딩 쪽부터 채우기로 했다(UD-001, UD-002).

프롤로그는 이미 "검은 화면 + 페이드 텍스트 + 통신음" 문법을 확립해 두었다(SF-004). 엔딩의 앞 두 비트는 의도적으로 그 문법을 그대로 재사용해 프롤로그와 수미상관을 만든다.

### Planning Boundary

이 계획은 해피엔딩 연출의 범위, 비트 구성, 트리거 지점, 데이터 소유, 기술 선택을 결정한다. 결정하지 않는 것: 실제 대사 문안, 사운드 에셋 제작, 새드엔딩. 구현은 이 계획을 확정한 같은 메시지에서 별도로 승인되었다. 커밋·씬 편집·패키지 설치·배포는 승인되지 않았다.

## Users and Stakeholders

| User or stakeholder | Need, responsibility, or concern | Evidence / source IDs | Status |
| --- | --- | --- | --- |
| 클리어한 플레이어 | 승리에 대한 감정적 보상. 스킵 가능해야 재플레이가 괴롭지 않다 | UD-001, SF-004 | active |
| 개발자(Hong) | 씬 충돌 없이, 기존 엔딩 테스트를 깨지 않고 붙일 수 있어야 함 | SF-009, SF-011 | active |
| 시연/발표 담당 | 엔딩을 강제 재생해 볼 수단이 필요 | AR-005 | proposed |

## Scope and Non-Goals

### In Scope

| Scope item | Source IDs | Status | Notes |
| --- | --- | --- | --- |
| 해피엔딩 7비트 연출 | UD-002 | active | 아래 Primary Flow |
| 재생 트리거 지점 변경 | UD-004, SF-003 | active | 최종 승리 발사에서 낙하산·결과 신문을 건너뛰고 엔딩으로 직행 |
| 최종 성공 로켓 외형 보존 | UD-005, SF-008 | active | 씬 언로드 직전 `Rocket` 루트 복제 후 엔딩에서 사용·파괴 |
| 하이브리드 구현 | UD-006, SF-005 | active | B3~B5 Timeline + Cinemachine, 나머지 코루틴 |
| 스킵/입력 처리 | SF-004 | active | 프롤로그와 동일한 클릭 스킵 |

### Out of Scope / Non-Goals

| Excluded item | Source IDs | Status | Why excluded or deferred |
| --- | --- | --- | --- |
| 새드엔딩 연출 | UD-002 | lifted | 이후 별도로 구현. `docs/sad-ending-cinematic.md` 참고 |
| 승패 판정 규칙 변경 | UD-001 | active | 현재 분기(B 이상 승리 / 2026 Q4 소진 패배)를 그대로 둔다 |
| 엔딩 크레딧, 스탭롤 | 없음 | active | 요청에 없음. 필요해지면 별도 계획 |
| 세이브/로드, 엔딩 갤러리 | 없음 | active | 요청에 없음 |

## Core Experience / Operating Flow

### Primary Flow

시작 조건: `ResearchPrototypeModel.HasGameEnded == true && GameWon == true` (SF-001).

진입 지점(UD-004): 최종 미션 성공 판정 직후. 이 경로에서는 낙하산 6초 연출(`MissionSuccessPresentation`)과 결과 신문(`ShowResultReport`)을 재생하지 않고, 시뮬레이션 씬을 내린 뒤 곧바로 B1 로 들어간다. 최종 미션이 아닌 발사의 성공은 기존 낙하산·신문 경로를 그대로 쓴다.

1. **B1 — 날짜 카드.** 검은 전체 화면. `2026.04` 텍스트가 페이드 인, 유지, 페이드 아웃. 프롤로그의 `2017.12` 카드와 같은 문법(SF-004).
2. **B2 — 전화 대사.** 같은 검은 화면 위에서 "이제 고생했다" 계열의 대사 여러 줄이 순차로 페이드 인/아웃. 통신음 SFX. 줄 수·문안 미정(OI-005).
3. **B3 — 야간 발사.** 3D로 전환. 밤 시간대 발사대에서 로켓이 실제로 발사되는 장면. 이 로켓은 최종 성공 시의 플레이어 로켓 외형이어야 한다(UD-002, OI-002).
4. **B4 — 달 전환.** 어느 정도 상승을 보여준 뒤 카메라를 컷 전환. 화면에 달이 잡힌다.
5. **B5 — 달 항행.** 화면 우하단에서 로켓 머리가 서서히 프레임 안으로 들어오고, 달을 향해 날아가는 우주선의 뒷모습이 서서히 드러난다.
6. **B6 — 신문.** 기존 신문 연출과 동일한 형태로 성공 기사를 보여준다(SF-006).
7. **B7 — 종료.** 페이드 아웃 후 `00_Title` 씬으로 복귀한다(UD-007). 해피엔딩에서는 `ResearchEndingController` 의 기록 패널을 거치지 않는다. 데드라인 패배는 기존 엔딩 화면·재시작 경로를 그대로 유지하므로, 두 엔딩의 종결 방식이 서로 달라진다.

### 비트별 길이 배분 (UD-009, 총 40~60초)

| 비트 | 구간 | 목표 길이 | 담당 |
| --- | --- | --- | --- |
| B1 | `2026.04` 날짜 카드 | 약 5초 | 코루틴 + SO |
| B2 | 전화 대사 3~4줄 | 약 15초 (줄당 4초 내외) | 코루틴 + SO |
| B3~B5 | 야간 발사 → 달 전환 → 달 항행 | 20~30초 | Timeline + Cinemachine |
| B6~B7 | 신문 + 페이드 → 타이틀 | 약 10초 | 기존 신문 UI + 코루틴 |

### Alternate, Error, or Edge Flows

| Condition | Expected behavior | Related requirement or decision IDs | Status |
| --- | --- | --- | --- |
| 플레이어가 화면을 클릭 | 프롤로그와 동일하게 남은 비트를 버리고 마지막 페이드로 직행 | R-008, SF-004 | active |
| 연출 프리팹/참조 누락 | 연출을 포기하고 기존 `ResearchEndingController` 경로로 진행. 절대 검은 화면에 갇히지 않는다 | R-009, SF-004 | active |
| 최종 성공 로켓 외형을 확보하지 못함 | 대체 로켓으로 재생하고 연출을 중단하지 않는다 | R-010, UD-005 | active |
| 데드라인 패배(`GameWon == false`) | 이 연출은 재생하지 않는다. 기존 경로 유지 | R-011, UD-002 | active |
| 연출 도중 컴포넌트 비활성화 | 카메라·로켓·오디오를 원상 복구한다. `MissionSuccessPresentation` 의 기존 규칙과 동일 | R-012, SF-003 | active |

### State, Data, or Lifecycle Notes

- 현재 최종 성공 경로는 `SimulationStageHost.CompleteLaunch(true)` → 낙하산 6초 연출 → 시뮬레이션 씬 언로드 → 결과 신문 → 확인 → `ShowEndingScreen()` 이다(SF-003).
- 시뮬레이션 씬은 Additive 로드 후 언로드된다(SF-008). 로켓 파트 배치는 어디에도 직렬화되지 않으므로, 언로드 시점에 조치하지 않으면 최종 로켓의 외형은 소실된다. 이것이 Q-002 의 핵심이다.
- `ResearchFlowSession.LaunchPhoto` 는 발사 사진 `Texture2D` 를 들고 있다(SF-007). 정지 이미지이므로 B3~B5 의 3D 연출을 대체하지 못한다. 신문 사진 용도로만 유효하다.
- `SoundManager` 는 `DontDestroyOnLoad` 싱글턴이라 연출 중 BGM/SFX 전환에 그대로 쓸 수 있다.

## Requirements

| ID | Requirement | Type | Source IDs | Priority | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- |
| R-001 | `GameWon == true` 로 게임이 끝났을 때만 해피엔딩 시네마틱을 재생한다 | functional | UD-002, SF-001 | must | active | EditMode 테스트: 승리 시 재생, 데드라인 패배 시 미재생 |
| R-002 | B1 은 `2026.04` 날짜 카드를 검은 화면에서 페이드 인/아웃한다 | functional | UD-002 | must | active | 게임 뷰 확인 |
| R-003 | B2 는 전화 대사 컷을 데이터로 정의하고 순차 페이드 인/아웃한다. 컷 추가·삭제가 코드 수정 없이 가능해야 한다 | functional | UD-002, SF-004 | must | active | 인스펙터에서 컷 수 변경 후 재생 |
| R-004 | B3 은 야간 조명 상태의 발사대에서 로켓 발사를 보여준다 | functional | UD-002 | must | active | 게임 뷰 확인 |
| R-005 | B3~B5 에 등장하는 로켓은 최종 성공 시 플레이어 로켓의 외형이다 | functional | UD-002 | must | active | 서로 다른 엔진 구성 2회 클리어 시 화면상 로켓이 다름 |
| R-006 | B4~B5 는 달이 보이는 프레임으로 컷 전환하고, 우하단에서 로켓 뒷모습이 서서히 진입한다 | functional | UD-002 | must | active | 게임 뷰 확인 |
| R-007 | B6 은 기존 신문 연출과 동일한 형식으로 성공 기사를 보여준다 | functional | UD-002, SF-006 | must | active | 기존 신문 UI 재사용 확인 |
| R-008 | 연출 전체를 클릭으로 스킵할 수 있다 | quality | SF-004 | must | active | 클릭 시 마지막 페이드로 직행 |
| R-009 | 필수 참조가 누락되어도 게임이 잠기지 않고 기존 엔딩 경로로 진행한다 | operational | SF-004 | must | active | 참조를 비운 상태로 재생 |
| R-010 | 로켓 외형 확보에 실패해도 연출은 대체 외형으로 계속된다 | operational | UD-005 | should | active | 외형 소스 제거 후 재생 |
| R-011 | 기존 `ResearchCompletionFlowTests` 가 계속 통과한다 | quality | SF-009 | must | active | EditMode 테스트 통과 |
| R-012 | 연출 종료·중단 시 카메라, 로켓 부모, 오디오, 시간 상태를 원상 복구한다 | operational | SF-003 | must | active | 연출 중 비활성화 테스트 |
| R-013 | 연출 재생을 개발용으로 강제 트리거할 수단이 있다 | operational | AR-005 | should | proposed | 에디터 메뉴 또는 디버그 버튼 |
| R-014 | 최종 미션 승리 발사에서는 낙하산 6초 연출과 결과 신문을 재생하지 않는다. 최종 미션이 아닌 발사는 기존 경로를 유지한다 | functional | UD-004, SF-003 | must | active | 최종 승리 1회와 일반 성공 1회를 각각 재생해 분기 확인 |
| R-015 | 시뮬레이션 씬 언로드 전에 최종 발사 로켓의 시각 계층을 보존하고, 엔딩 종료 시 반드시 파괴한다 | functional | UD-005, SF-008 | must | active | 엔딩 종료 후 씬에 잔존 오브젝트가 없음 |
| R-016 | Timeline 은 B3~B5 3D 구간에만 쓰고, B1·B2·B6·B7 은 UI 코루틴 경로로 둔다 | operational | UD-006, SF-005 | must | active | 코드 리뷰: Timeline 트랙에 UI/텍스트 트랙 없음 |
| R-017 | B7 페이드 완료 후 `00_Title` 씬을 로드한다. 데드라인 패배 경로는 기존 엔딩 화면·재시작을 유지한다 | functional | UD-007, SF-002, SF-012 | must | active | 승리 시 타이틀 도달, 패배 시 기존 화면 유지를 각각 확인 |
| R-018 | 타이틀 복귀 시 `ResearchFlowSession` 의 진행 상태가 다음 새 게임에 새지 않는다 | operational | UD-007, SF-012 | must | active | 클리어 후 새 게임 시작 시 연차·자금·미션이 초기값 |
| R-019 | B2 는 3~4줄, 총 15초 내외로 재생된다. 총 길이는 40~60초를 넘지 않는다 | quality | UD-008, UD-009 | should | active | SO 의 합산 길이 검사(프롤로그 `TotalSeconds` 방식) |

## Constraints

| Category | Constraint | Source IDs | Consequence | Status |
| --- | --- | --- | --- | --- |
| compatibility | 씬(`.unity`)보다 프리팹·SO·코드를 우선한다. `01_Main.unity` 는 현재 이미 수정 상태 | SF-011 | 연출은 프리팹으로 만들고 씬에는 참조 하나만 붙이는 형태가 유리 | active |
| quality | 엔딩 흐름은 EditMode 테스트로 잠겨 있다 | SF-009 | 트리거 지점을 바꾸면 테스트 수정이 동반된다 | active |
| technical | 시뮬레이션 씬은 Additive 로드/언로드되며 로켓 배치는 비직렬화 | SF-008 | 로켓 외형은 언로드 전에 확보해야 한다 | active |
| technical | `com.unity.timeline` 1.8.10, `com.unity.cinemachine` 3.1.7 가 이미 설치됨 | SF-005 | Timeline 채택 시 신규 의존성 추가는 없다 | active |
| technical | `SkyEnvironment` 는 고도 기반이며 시각(time-of-day) 파라미터가 없다 | SF-010 | 야간 발사대는 별도 조명/스카이박스 상태로 만들어야 한다 | active |
| policy | Play Mode·게임 뷰 확인은 사용자 몫. 에이전트는 컴파일까지만 | 사용자 메모리 | 검증 계획에 사용자 확인 단계를 명시해야 함 | active |

## Success Evidence

| Related requirement IDs | Evidence or acceptance condition | Verification method | Owner or reviewer | Status |
| --- | --- | --- | --- | --- |
| R-001, R-011 | 승리/패배 분기별 재생 여부와 기존 엔딩 테스트 통과 | EditMode 테스트 | 개발자 | proposed |
| R-002~R-007 | 7비트가 의도한 순서·타이밍으로 재생됨 | 게임 뷰 육안 확인 | 사용자 | proposed |
| R-005 | 엔진 구성이 다른 2회 클리어에서 화면 속 로켓이 서로 다름 | 게임 뷰 육안 확인 | 사용자 | proposed |
| R-008, R-009, R-010, R-012 | 스킵·참조 누락·외형 실패·중단에서 게임이 잠기지 않음 | EditMode 또는 PlayMode 테스트 | 개발자 | proposed |
| R-014 | 최종 승리 발사는 낙하산·결과 신문 없이 엔딩으로 직행하고, 0~4번 미션 성공은 기존 경로를 유지 | EditMode 테스트 | 개발자 | proposed |
| R-015 | 엔딩 종료 후 보존 로켓 오브젝트가 씬에 남지 않음 | PlayMode 테스트 또는 Hierarchy 확인 | 개발자 | proposed |
| R-016 | Timeline 자산이 3D 구간만 담당함 | 코드/에셋 리뷰 | 개발자 | proposed |
| R-017, R-018 | 승리 시 타이틀 도달, 패배 시 기존 화면 유지. 클리어 후 새 게임이 초기 상태로 시작 | PlayMode 확인 + EditMode 테스트 | 개발자 / 사용자 | proposed |
| R-019 | SO 합산 길이가 40~60초 범위 안 | EditMode 테스트(프롤로그 `TotalSeconds` 검사 방식) | 개발자 | proposed |

## Decision and Evidence Ledger

| ID | Kind | Statement | Evidence / rationale | Status | Consequence / linked IDs |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | 현재 분기 동작(최종 미션 실패해도 게임이 끝나지 않고, 새드엔딩은 2026 Q4 소진 시점에만 발생)을 그대로 인정한다 | "1번은 맞는 말이야" | active | 승패 규칙 변경 없음. Out of Scope |
| UD-002 | user decision | 해피엔딩 연출을 먼저 만든다. 비트 순서는 날짜 카드 `2026.04` → 전화 대사 → 야간 발사대 발사(최종 성공 로켓 외형) → 카메라 전환/달 → 우하단에서 로켓 뒷모습 진입 → 신문 성공 기사 → 페이드 종료 | 사용자 요청 원문 | active | Primary Flow, R-002~R-007 |
| UD-003 | user decision | Timeline 으로 구현해도 좋다고 열어 둠. 확정이 아니라 후보 제시 | "타임라인으로 구현해도 좋을 거 같고" | active | Q-003, UD-006 |
| UD-004 | user decision | 최종 성공에서는 낙하산 6초 연출을 생략하고 곧바로 엔딩 시네마틱으로 간다. 신문은 엔딩 마지막 비트에서 1회만 나온다 | Q-001 답변 "낙하산 생략, 바로 엔딩" (rev 2) | active | R-014, OI-001 resolved, RK-005 |
| UD-005 | user decision | 로켓 외형은 시뮬레이션 씬 언로드 직전 실제 발사 `Rocket` 루트를 보존해 그대로 쓴다 (AR-003 수락) | Q-002 답변 "발사 로켓 보존" (rev 2) | active | R-005, R-015, OI-002 resolved, RK-001 |
| UD-006 | user decision | 하이브리드로 만든다. B3~B5 3D 구간만 Timeline + Cinemachine, B1·B2·B6·B7 은 프롤로그식 코루틴 + SO (AR-002 수락) | Q-003 답변 "하이브리드" (rev 2) | active | R-016, OI-003 resolved, RK-003 |
| UD-007 | user decision | B7 페이드 아웃 후 `00_Title` 로 복귀한다. 해피엔딩에서는 기존 기록 패널을 보여주지 않는다 | Q-004 답변 "타이틀로 복귀" (rev 3) | active | R-017, R-018, OI-004 resolved, RK-007 |
| UD-008 | user decision | B2 전화 대사는 3~4줄, 담담한 톤. 프롤로그 통신 톤을 뒤집는 방향 | Q-005 답변 "3~4줄, 담담하게" (rev 3) | active | R-019, OI-005 resolved |
| UD-009 | user decision | 연출 총 길이 예산은 40~60초 | Q-006 답변 "40~60초" (rev 3) | active | R-019, OI-006 resolved, 비트별 길이 배분 |
| UD-010 | user decision | 계획을 확정하고 구현까지 진행한다 | "계획확정하고 구현을 해줘" (rev 4) | active | Interview state `explicitly-finished`, 구현 승인 |
| SF-012 | sourced fact | 타이틀에서 본편으로 가는 경로만 존재한다. `TitleMenu.NewGame()` 이 `ResearchFlowSession.PrepareNewGame()`(내부적으로 `ResetResearch()`) 후 `SceneManager.LoadScene("01_Main")` 을 호출한다. 본편에서 타이틀로 돌아가는 코드는 없다. `ResearchFlowSession` 은 `DontDestroyOnLoad` 라 씬을 넘어도 살아남는다 | `Assets/01. Scripts/Title/TitleMenu.cs:38-44`, `Assets/01. Scripts/Research/ResearchFlowSession.cs:253-257,283` | active | R-017, R-018, RK-007 |
| SF-001 | sourced fact | 승리는 `LowPowerZoneHold` 최고 등급 B 이상, 패배는 `RemainingTurns <= 0`(2026 Q4). `EvaluateGameEnd` 가 `HasGameEnded`/`GameWon` 을 확정 | `Assets/01. Scripts/Research/ResearchPrototypeModel.cs:1524-1534` | active | R-001 |
| SF-002 | sourced fact | 엔딩 화면은 `ResearchEndingController` 하나이며 제목 문자열만 `MISSION COMPLETE`/`MISSION FAILED` 로 갈린다. 본문·버튼은 공통 | `Assets/01. Scripts/Research/ResearchEndingController.cs:27-28` | active | OI-004 |
| SF-003 | sourced fact | 최종 성공 경로는 낙하산 6초 연출 → 결과 신문 → 확인 시 `ShowEndingScreen()` 이다 | `Assets/01. Scripts/Research/ResearchOperationUIController.cs:956-988`, `docs/mission-success-cinematic.md` | active | Q-001, R-012 |
| SF-004 | sourced fact | 프롤로그는 `PrologueController`(코루틴) + `PrologueSequenceSO`(컷 리스트, 페이드/유지/타이핑/SFX id/RevealSeconds) 구조이며 클릭 스킵과 참조 누락 시 자기 파괴 안전장치를 갖는다 | `Assets/01. Scripts/Prologue/PrologueController.cs`, `PrologueSequenceSO.cs` | active | R-003, R-008, R-009 |
| SF-005 | sourced fact | `com.unity.timeline` 1.8.10 과 `com.unity.cinemachine` 3.1.7 이 이미 프로젝트에 설치되어 있다 | `Packages/manifest.json:12,21` | active | Q-003 |
| SF-006 | sourced fact | 신문 표현은 `ResearchResultReportController` + `LaunchNewspaperArticle` 이며, 최종 미션이거나 최종 승리면 매체가 항상 `Newspaper` 로 고정된다 | `Assets/01. Scripts/Research/LaunchNewspaperArticle.cs:44-57` | active | R-007, Q-001 |
| SF-007 | sourced fact | 발사 사진은 `ResearchFlowSession.LaunchPhoto`(`Texture2D`)로 보관된다 | `Assets/01. Scripts/Research/ResearchFlowSession.cs:31`, `Assets/01. Scripts/Simulation/LaunchPhotoCapture.cs` | active | 신문 사진 재사용 가능. 3D 연출은 대체 불가 |
| SF-008 | sourced fact | 시뮬레이션 씬은 Additive 로 로드·언로드되며, 로켓 파트 배치를 저장하는 코드는 없다 | `Assets/01. Scripts/Simulation/SimulationStageHost.cs:121,165`; 배치 직렬화 검색 결과 없음 | active | RK-001, Q-002 |
| SF-009 | sourced fact | 엔딩 흐름은 `ResearchCompletionFlowTests` 로 잠겨 있다(조기 승리 차단, 최종 실패 보고서 선행, 재시작 화면 재사용 등) | `Assets/Tests/EditMode/Research/ResearchCompletionFlowTests.cs` | active | R-011, RK-005 |
| SF-010 | sourced fact | `SkyEnvironment` 는 고도 기반 스카이/태양/안개 제어이며 야간 프리셋이나 시각 파라미터가 없다 | `Assets/01. Scripts/Simulation/SkyEnvironment.cs` | active | RK-004 |
| SF-011 | sourced fact | 프로젝트 규칙상 씬 편집은 최후 수단이며, `01_Main.unity` 는 현재 이미 변경 상태다 | `CLAUDE.md`, 세션 시작 git status | active | 프리팹 우선 설계 |
| AR-001 | agent recommendation | B1·B2 는 프롤로그 자산을 재사용한다. `PrologueSequenceSO` 를 그대로 쓰거나 동일 구조의 엔딩용 SO 를 만들어 대사·타이밍을 데이터로 둔다 | 프롤로그가 이미 같은 문법을 구현(SF-004). 새 코드가 거의 필요 없음 | proposed | R-003. 수락 시 새 UD |
| AR-002 | agent recommendation | B3~B5 는 Timeline 1개 + Cinemachine 카메라 2~3개로 만들고, B1·B2·B6·B7 은 기존 UI 코루틴 경로로 둔다 | 3D 카메라 컷·타이밍은 Timeline 이 압도적으로 편하고, 텍스트 페이드는 이미 되는 것을 다시 만들 이유가 없음 | accepted | UD-006 으로 승격 |
| AR-003 | agent recommendation | 로켓 외형은 언로드 직전 `Rocket` 루트를 복제해 `DontDestroyOnLoad` 로 보존하고, 엔딩 연출에서 재사용 후 파괴한다 | 배치가 직렬화되지 않으므로(SF-008) 재조립보다 보존이 확실하고 코드가 짧다 | accepted | UD-005 로 승격 |
| AR-004 | agent recommendation | 야간은 시뮬레이션 씬 재사용이 아니라 엔딩 전용 프리팹(발사대 + 밤 조명 + 스카이박스 + 달)으로 만든다 | `SkyEnvironment` 에 시각 개념이 없고(SF-010), 씬 편집을 피해야 함(SF-011) | proposed | R-004, RK-004 |
| AR-005 | agent recommendation | 엔딩을 강제 재생하는 디버그 진입점을 함께 만든다 | 6개 미션을 다 클리어해야만 볼 수 있는 연출은 반복 확인 비용이 지나치게 크다 | proposed | R-013 |
| OI-001 | unresolved item | 기존 결과 신문과 엔딩 신문의 관계. 신문을 한 번만 보여줄지, 두 번 보여줄지, 낙하산 연출을 유지할지 | 현재 최종 성공 시 이미 신문이 한 번 나온다(SF-003) | resolved | UD-004 로 해결. R-014 생성 |
| OI-002 | unresolved item | B3~B5 로켓 외형의 소스 | 시뮬레이션 씬 언로드로 외형이 소실됨(SF-008) | resolved | UD-005 로 해결. R-015 생성 |
| OI-003 | unresolved item | 구현 기술(Timeline 전면 / 코루틴 전면 / 하이브리드) | 사용자가 Timeline 을 후보로만 제시(UD-003) | resolved | UD-006 로 해결. R-016 생성 |
| OI-007 | unresolved item | 낙하산 연출을 최종 미션에서 못 보게 되는 것에 대한 보완 여부 | 이미 만들어 테스트까지 마친 연출이 최종 미션에서 사라진다(SF-003, UD-004) | open | 연출 자산 활용도. 보완 필요 없다면 그대로 종결 |
| OI-004 | unresolved item | B7 페이드 아웃 이후 도착 지점(기존 엔딩 화면 유지 / 타이틀 복귀 / 재시작 버튼만) | 현재는 재시작 버튼이 있는 엔딩 화면이 최종 화면(SF-002) | resolved | UD-007 로 해결. R-017, R-018 생성 |
| OI-005 | unresolved item | B2 전화 대사의 줄 수, 문안, 화자, 사운드 | 사용자가 "몇 번"이라고만 함 | resolved | UD-008 로 줄 수·톤 확정. 실제 문안은 구현 시 작성 |
| OI-006 | unresolved item | 연출 총 길이 예산 | 프롤로그는 20~30초 예산이 있으나 엔딩 기준은 없음 | resolved | UD-009 로 해결. 비트별 배분표 생성 |

## Question Register

| ID | Decision needed | Why it matters | Related IDs | State | Asked / updated revision | Resolution |
| --- | --- | --- | --- | --- | --- | --- |
| Q-001 | 기존 결과 신문·낙하산 연출과 엔딩 신문의 관계 | 트리거 지점과 신문 중복 여부가 달라지고, 기존 엔딩 테스트 수정 범위가 달라진다 | OI-001, SF-003, SF-009, R-007 | answered | 1 / 2 | UD-004 |
| Q-002 | B3~B5 로켓 외형을 어디서 가져올지 | "최종 성공 시 로켓의 모습" 요구(R-005)의 성립 여부와 구현 난이도를 결정한다 | OI-002, SF-008, AR-003 | answered | 1 / 2 | UD-005 |
| Q-003 | 구현 기술 선택 | 유지보수 비용, 타이밍 조정 방식, 코드량이 갈린다 | OI-003, UD-003, SF-005, AR-002 | answered | 1 / 2 | UD-006 |
| Q-004 | B7 페이드 아웃 이후 도착 지점 | 게임의 마지막 화면이 무엇인지, 재시작 수단을 어디에 두는지가 결정된다 | OI-004, SF-002 | answered | 2 / 3 | UD-007 |
| Q-005 | B2 전화 대사의 줄 수와 톤 | 연출 길이와 감정 곡선이 결정되고, SO 데이터 분량이 정해진다 | OI-005, R-003 | answered | 2 / 3 | UD-008 |
| Q-006 | 연출 총 길이 예산 | 각 비트 타이밍과 Timeline 구간 길이의 상한이 정해진다 | OI-006, R-016 | answered | 2 / 3 | UD-009 |

## Corrections and Revision History

| Revision | Trigger | Change | Corrected / superseded IDs | Downstream sections and IDs reconciled |
| --- | --- | --- | --- | --- |
| 1 | 최초 요청 + 코드/문서 조사 | 최초 계획 가설 작성 | 없음 | Snapshot, Scope, Flow, R-001~R-013, RK-001~RK-006, Ledger, Q-001~Q-003 |
| 2 | Q-001~Q-003 답변 | 낙하산 생략·즉시 엔딩, 발사 로켓 보존, 하이브리드 구현 확정 | AR-002·AR-003 accepted, OI-001~OI-003 resolved, Q-001~Q-003 answered | Snapshot, Scope, Primary Flow 진입 지점, R-014~R-016, RK-001·RK-003·RK-005, OI-007 신규, Q-004~Q-006 신규 |
| 3 | Q-004~Q-006 답변 | 타이틀 복귀, 대사 3~4줄, 40~60초 예산 확정. 타이틀 복귀 경로가 신규 구현임을 SF-012 로 확인 | OI-004~OI-006 resolved, Q-004~Q-006 answered | Snapshot, Primary Flow B7 + 길이 배분표, R-017~R-019, SF-012, RK-006·RK-007·RK-008, Coverage, Checkpoint |
| 4 | 명시적 finish + 구현 승인 | 인터뷰 종료. 영어 정본 작성, 한국어 미러 동기화 | UD-010 신규. Open Items 표에서 뒤늦게 남아 있던 OI-004~OI-006 상태를 resolved 로 정정 | Document State, Snapshot, Ledger, Open Items, Coverage, Finalization |
| 5 | 1차 구현 | 구현 결과와 계획의 차이를 기록 | R-003·R-004·R-006 partial, R-013 not-implemented, R-016 deviated, RK-005 overstated 로 정정 | 아래 "구현 기록" |

## 구현 기록 (revision 5)

코드는 들어갔다. 계획과 다른 지점만 적는다. 같은 것은 위 표가 이미 말한다.

### 들어간 것

| 파일 | 내용 |
| --- | --- |
| `Assets/01. Scripts/Simulation/HappyEndingSequence.cs` | 신규. 일곱 비트 전부와 무대 생성·해체, 로켓 보존, 클릭 스킵, 타이틀 복귀 |
| `Assets/01. Scripts/Simulation/SimulationStageHost.cs` | `CompleteLaunch` 에 `result.FinalMissionWon` 분기와 `HappyEndingRoutine` 추가 |
| `Assets/01. Scripts/Research/ResearchOperationUIController.cs` | `SetEndingOverride` 추가. `ShowEndingScreen` 한 곳에서만 가로챈다 |
| `Assets/Tests/EditMode/Research/ResearchCompletionFlowTests.cs` | `EndingOverride_TakesOverInsteadOfShowingEndingScreen` 추가 |
| `Assets/01. Scripts/Simulation/HappyEndingDebugTester.cs` | 신규. 에디터 전용 강제 재생. **F8** 과 `Tools > Border > Debug > Play Happy Ending` |
| `docs/mission-success-cinematic.md` | 최종 미션은 낙하산 연출을 타지 않는다는 예외 명시 |

### 계획과 다른 점

| ID | 상태 | 실제 |
| --- | --- | --- |
| R-016 | deviated | Timeline 을 쓰지 않는다. 무대를 런타임에 세우므로 Timeline 이 바인딩할 대상이 없다. B3~B5 는 코루틴 코드다. Timeline 으로 바꾸려면 발사대·달·카메라를 먼저 프리팹으로 만들어야 하고, 그건 에디터 작업이다 |
| R-004, R-006 | partial | 발사대·지면·달은 프리미티브 자리표시자다. 밤은 조명 세기와 색으로만 만든다. 룩 교체는 에디터 몫 |
| R-003 | partial | 대사는 `HappyEndingSequence` 의 직렬화 필드다. 런타임 생성 컴포넌트라 인스펙터로 열리지 않으므로, 지금은 문안 수정에 코드 편집이 필요하다 |
| R-013 | done | `HappyEndingDebugTester` 가 F8 과 메뉴로 강제 재생한다. 확인 대기 중인 결과가 없으면 최종 미션 성공 결과를 지어내 신문까지 띄운다 |

## 1차 피드백 수정 (revision 6)

F8 첫 재생에서 나온 문제 다섯을 고쳤다. 전부 `HappyEndingSequence.cs` 와 `HappyEndingDebugTester.cs` 안에서 끝났고 씬·프리팹·에셋은 건드리지 않았다.

| 증상 | 원인 | 수정 |
| --- | --- | --- |
| 대사에 타이핑 효과 없음 | `ShowLine` 이 알파 페이드만 했다 | `TypeText` 추가. `maxVisibleCharacters` 를 올리는 프롤로그 기법. B2 대사만 타이핑, B1 날짜 카드는 페이드 유지. 새 글자가 나타나면 공백을 제외하고 `keyboard01..04`를 프레임당 한 번 재생한다. 타이핑 완료·클릭으로 즉시 완성·비활성화 시 해당 타건음을 정리한다 |
| 야간 발사가 너무 어두움 | ambient `(0.02,0.03,0.06)`, moonlight 0.35, Spot intensity 40 | ambient `(0.08,0.09,0.12)`, moonlight 0.8, Spot `intensity 6 / range 60 / spotAngle 70`. URP 는 Physical Light Units 를 쓰지 않아 Spot intensity 는 임의 단위이고 1~10 이 정상 범위다 |
| 우주가 새까맘 | **조명이 전부 `pad` 자식이라 `pad.SetActive(false)` 와 함께 광원이 0개가 됐다** | 조명을 `Pad Rig` / `Space Rig` 로 분리해 `stage` 직속에 두고 컷에서 교체. 우주 리그는 Directional 키 + fill 이라 로켓 위치와 무관하게 먹는다 |
| 3D 구간에 UI 가 뜸 | 연구 화면이 Screen Space Overlay 라 카메라를 꺼도 그려진다 | `BuildStage` 에서 연구 화면 루트를 끄고 신문 비트에서 되살린다 |
| 배기 이펙트 없음 | 로켓 파티클은 전부 `playOnAwake: 0` 이고 평소엔 `RocketPart` 가 켠다 | 새 이펙트를 만들지 않는다. 보존한 로켓의 기존 `Flame`/`Smoke_Single` 을 그대로 켜고, 달 컷에서 순간이동하므로 `Clear` 후 다시 `Play` 해 잔상을 지운다 |
| 신문이 안 나옴 | 디버그용 "미확인 결과 없으면 건너뛰기" 가드 | 가드 제거. 신문 호출에 명시적 콜백을 넘기고, 디버그 테스터가 최종 미션 성공 결과를 지어낸다 |

곁들여 고친 것 둘:

- **별밭** — 우주 리그에 코드로 만든 고정 별 파티클 하나. 새 에셋 없음.
- **클론 머티리얼** — `PreserveRocket` 이 복제본 렌더러의 머티리얼을 사본으로 갈아 끼운다. 원본 `RocketPart` 가 파괴되며 공유 인스턴스를 해제해도 복제본이 깨지지 않는다.

## 3차 피드백 수정 (revision 8)

발사 장면이 인게임과 다르게 보인다는 지적. 원인은 하나였다 — **`Rocket` 컴포넌트를 재우고 있었다.**

인게임 발사의 그림은 전부 `Rocket` 이 만든다. 화염 세기는 `FixedUpdate` 의 `engine.Tick(dt, ramp)`,
홀드 중 배기는 `TickHold` 의 `HoldExhaust(HoldProgress)`, 몸통 흔들림은 `SetWobble`, 리프트 연기는
`RocketLiftSmoke` 가 보는 `Launched && LiftAssistActive`, 사운드는 `RocketAudio` 가 구독하는
`LaunchStarted`/`LiftoffStarted` 다. 결정적으로 **엔진 점화(`RocketPart.Prepare`)는 `Rocket.Launch()`
안에서만 일어난다** — 그걸 건너뛰면 `SetFlame` 이 아예 불을 안 붙인다. 밖에서 파티클을 `Play()` 해
봐야 세기 없는 불만 나온다.

| 항목 | 수정 |
| --- | --- |
| 비행 이펙트 | `Suspend(rocket)` 제거. `rocket.Launch()` 를 정상적으로 부른다. 파티클을 밖에서 켜던 `PlayParticles`/`ResetParticles` 삭제 — 이제 `RocketPart` 가 제대로 켠다 |
| 정해진 경로 | 새 컴포넌트 `HappyEndingFlight`. 매 `FixedUpdate`·`LateUpdate` 에서 `isKinematic = true` 를 재강제하고(이륙 2.5초 뒤 `ReleaseLift()` 가 스스로 푸는 것을 되돌린다) 위치를 경로로 덮어쓴다. 앞 2.5초는 인게임 보조 상승과 같은 식이라 그림이 겹치고, 그 뒤 등가속으로 이어 붙인다 |
| 기울기 | 이륙 후 3.5초가 지난 뒤에만 최대 12도. 그 전에 눕히면 `hasUpwardEngine` 이 false 가 되어 리프트 연기가 끊기고 kinematic 이 풀린다 |
| 사운드 | `RocketAudio` 를 재우지 않는다. SparkStart → RocketLaunch/RocketLoop는 인게임과 같다. `rocket.Launch()` 직후 BGM을 `ToSpace`로 교체하고, 달 항행과 신문까지 유지한 뒤 타이틀 복귀 전에 페이드 아웃한다 |
| 달 텍스처 | `Assets/05. Arts/Texture/Noise/Craters/Craters_03-512x512.png` 를 `Assets/05. Arts/Texture/Resources/` 로 이동해 `Resources.Load` 로 읽는다. 아무도 참조하지 않던 파일이다. Unlit → **Lit** 으로 바꿔 키 라이트가 명암 경계를 만든다 |
| 밤하늘 | `SkyBlend.shader` 에 `_MidColor` + `_MidBlend` 추가. **`_MidBlend` 기본값 0 이라 켜지 않으면 기존 2색 lerp 와 완전히 동일** — 인게임 하늘은 안 변한다. 엔딩만 1 로 켜서 지평선 핑크 (0.72,0.26,0.45) → 중간 보라 (0.42,0.20,0.52) → 천정 남색 (0.07,0.08,0.26). `_AtmosphereThickness 3`, `_SpaceBlend` 0.35 → 0.12 (파란 성운이 핑크를 먹어서) |

`SkyEnvironmentTests` 가 못 박는 것은 셰이더 이름과 기존 다섯 프로퍼티뿐이라 프로퍼티 추가는 안전하다.

알려진 천장(`ponytail:` 주석): 엔진이 실제로 연료를 태우고 발열한다. 발사 비트가 길어지면 연료 소진으로
불이 꺼지거나 과열로 로켓이 스스로 폭발한다. `launchSeconds` 를 10초 안쪽으로 유지해야 한다.

## 2차 피드백 수정 (revision 7)

1차 수정을 재생해 본 뒤 나온 요청 셋을 반영했다. **UD-005(로켓 복제 보존)는 폐기됐다** — 시뮬레이션 씬을 살려 두면 복제할 이유가 없다.

| 요청·증상 | 원인 | 수정 |
| --- | --- | --- |
| `Can't remove Rocket because LaunchMissionController depends on it` | `Destroy` 는 프레임 끝까지 지연되므로 `Rigidbody` 를 지울 때 `Rocket` 이 아직 살아 있고 `[RequireComponent]` 사슬에 걸린다 | 복제 자체를 없앴다. 씬의 로켓을 그대로 쓰고 `Rocket` 컴포넌트만 재운다(`enabled = false` + `isKinematic`). `RocketPart` 는 건드리지 않는다 — 끄면 `OnDisable` 이 화염을 꺼 버린다 |
| 진짜 발사대에서 발사할 것 | 프리미티브 원기둥 무대를 멀리 세우고 있었다 | `SimulationTest` 씬을 3D 구간 동안 살려 두고 그 발사대에서 올린다. `MissionSuccessPresentation` 이 낙하산 연출에서 쓰는 `Suspend` 방식 그대로 카메라·캔버스·`RocketBuilder`·`RocketDesignUI`·`SkyEnvironment` 를 재운 뒤 우리 카메라를 얹는다 |
| 하늘만 밤으로 | `SkyEnvironment` 가 매 프레임 하늘·태양·안개를 덮어쓴다 | 재우면 `OnDisable` 이 없어 마지막 값이 굳는다. 그 뒤 `RenderSettings.skybox` 에 `_Exposure 0.25`, `_SkyTint`, `_HorizonColor`, `_SpaceBlend 0.35` 를 직접 넣는다. 값은 기존 `ResearchLabNightSky.mat` 기준. **별은 `_SpaceCube` 성운 큐브맵에서 공짜로 나온다** — 1차의 코드 생성 별밭은 삭제 |
| 우주선이 카메라에 훨씬 가까이 | 거리 배수가 `1.6 → 9` 였다 | `0.8 → 3.5` 로 좁혔다. 달 컷에서 `_SpaceBlend` 를 1 로 올려 배경을 성운으로 채운다 |
| 텍스트만 스킵 | 클릭 하나가 `skipRequested` 를 세워 연출 전체를 날렸다 | `advanceRequested` 로 좁혔다. 타이핑 중 클릭은 그 줄을 즉시 드러내고, 다 나온 뒤 클릭은 다음 줄로. 3D 구간과 신문은 클릭을 보지 않는다 |

부수 효과: 지면이 대낮으로 남던 문제도 같이 잡힌다. 씬 앰비언트가 Skybox 모드인데 기본 스카이박스에서 구워진 값에 고정돼 있고 아무도 갱신하지 않아서, `ambientMode = Flat` + 어두운 `ambientLight` 를 직접 넣어야 한다.

디버그 F8 은 `SimulationTest` 씬이 없으면 직접 additive 로 올린다. 연구 화면에서 눌러도 진짜 발사대가 나온다.

미확정: 상승을 코드로 올릴지 물리로 날릴지는 코드 상승으로 진행했다. 근거는 씬 기본 로켓에 엔진이 없어 추력이 0 이고(디버그 경로에서 로켓이 꿈쩍하지 않는다), 실제 최종 미션 로켓은 목표 구역을 노리게 기울어져 있다는 것. 사용자 확인 대기.
| RK-005 | corrected | 과대평가였다. `ResearchCompletionFlowTests` 는 `SimulationStageHost` 를 지나가지 않고, 문제의 테스트는 최종 승리가 아니라 데드라인 패배 경로다. 기존 테스트 수정 없음 |
| RK-007 | mitigated | 타이틀 복귀는 `SceneManager.LoadScene("00_Title")` 한 줄. 세션 초기화는 기존 `TitleMenu.NewGame` 에 맡기고 중복 호출하지 않는다 |

### 검증 상태

컴파일 통과(에디터가 Play Mode 로 들어갔으므로 컴파일 에러가 없다). 테스트 추가분과
`ResearchCompletionFlowTests` 전체는 **아직 돌리지 않았다** — 작업 시점에 사용자가 Play Mode 를
점유 중이었다. 게임 뷰 확인은 사용자 몫이며 아직 수행되지 않았다.

## Risks, Conflicts, and Dependencies

| ID | Kind | Risk, conflict, or dependency | Likelihood / impact | Mitigation, decision, or owner | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | risk | 보존한 로켓이 파괴되지 않고 남거나, 물리·오디오 컴포넌트가 살아 있어 엔딩 중 오작동 | 중간 / 중간 | UD-005 채택으로 소실 위험은 해소. 보존 시 물리·스크립트를 떼고 시각 계층만 남긴다. R-015 로 파괴 보장 | UD-005, R-015 | open |
| RK-002 | risk | 씬 편집이 필요해져 `01_Main.unity` 충돌 발생 | 중간 / 중간 | 연출 전체를 프리팹으로 만들고 씬 참조는 최소 1개 | SF-011 | open |
| RK-003 | risk | Timeline 구간과 코루틴 구간의 경계에서 타이밍·스킵·페이드가 어긋남 | 중간 / 중간 | UD-006 하이브리드 확정. 경계는 B2 종료 시점과 B5 종료 시점 두 곳뿐이며, 양쪽 다 검은 화면에서 교차하도록 설계 | UD-006, R-016, R-008 | open |
| RK-004 | risk | 야간 조명 상태를 만들 기존 수단이 없어 룩 작업이 예상보다 커짐 | 중간 / 중간 | AR-004 전용 프리팹. 조명·스카이박스는 엔딩 전용 값으로 고정 | SF-010 | open |
| RK-005 | risk | UD-004 로 최종 승리 경로에서 결과 신문 호출이 빠지면서 `ResearchCompletionFlowTests` 가 깨짐. 특히 `Operation_FinalLaunchShowsResultThenFinalFailureReportBeforeEndingWithoutDuplicateRewards` 는 신문 표시를 전제로 한다 | 높음 / 높음 | 구현 착수 전에 해당 테스트가 검증하는 것(중복 보상 없음, 확인 후 엔딩 진입)을 새 경로 기준으로 다시 표현한다. 보상 정산은 신문 UI 가 아니라 `FinishLaunch` 에서 이미 끝나므로 판정 자체는 영향 없음 | SF-009, UD-004, R-011, R-014 | open |
| RK-006 | dependency | 대사 문안·통신음·엔딩 BGM 등 콘텐츠 에셋 | 중간 / 중간 | UD-008 로 분량 확정(3~4줄). 사운드 없어도 타이밍은 데이터대로 진행(SF-004 방식) | UD-008 | open |
| RK-007 | risk | 본편에서 타이틀로 돌아가는 경로가 없어 신규 구현이며, `ResearchFlowSession` 이 `DontDestroyOnLoad` 라 진행 상태가 다음 새 게임으로 샐 수 있음 | 중간 / 높음 | `TitleMenu.NewGame()` 이 이미 `PrepareNewGame()` 을 호출하므로 초기화는 그쪽에 맡기고 중복 호출하지 않는다. R-018 로 검증 | UD-007, SF-012, R-018 | open |
| RK-008 | conflict | 해피엔딩은 타이틀로, 새드엔딩은 기존 기록 패널로 끝나 두 엔딩의 종결 방식이 달라진다 | 중간 / 낮음 | 해소됨: 배드엔딩 시네마틱도 타이틀로 복귀하며, 런타임 실패 경로에서는 기록 패널로 가지 않는다. `docs/sad-ending-cinematic.md` 참고 | UD-002, UD-007, SF-002 | closed |

## Open, Skipped, and Deferred Items

| ID | Item | State | Why it matters / consequence | Current recommendation | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-001 | 신문 중복·낙하산 유지 여부 | resolved | 트리거 지점, 테스트 수정 범위 | — | 사용자 | UD-004 로 종결 |
| OI-002 | 로켓 외형 소스 | resolved | R-005 성립 여부 | — | 사용자 | UD-005 로 종결 |
| OI-003 | 구현 기술 | resolved | 코드량·유지보수 | — | 사용자 | UD-006 로 종결 |
| OI-004 | 페이드 이후 도착 지점 | resolved | 흐름 종결부 | — | 사용자 | UD-007 로 종결 |
| OI-005 | 전화 대사 내용 | resolved | 연출 길이와 톤 | — | 사용자 | UD-008 로 종결. 문안은 구현 시 작성 |
| OI-006 | 총 길이 예산 | resolved | 각 비트 타이밍 | — | 사용자 | UD-009 로 종결 |
| OI-007 | 낙하산 연출 보완 여부 | open | 이미 검증까지 끝낸 연출이 최종 미션에서 사라짐 | 보완하지 않는다. 0~4번 미션 성공에서는 그대로 쓰이므로 사장되지 않음 | 사용자 | 엔딩 첫 재생 확인 후 |

## Coverage and Consistency Check

| Planning area | State | Supporting IDs | Remaining gap or note |
| --- | --- | --- | --- |
| Outcome | covered | UD-002 | — |
| Users and stakeholders | covered | UD-002, AR-005 | — |
| Scope | covered | UD-002, UD-004~UD-006 | — |
| Non-goals | covered | UD-001, UD-002 | — |
| Core flow | covered | UD-002, UD-004, UD-007, UD-009 | 진입·종료 지점과 길이 배분 확정 |
| Constraints | covered | SF-005, SF-008~SF-012 | — |
| Success evidence | partial | R-001~R-019 | 육안 확인 항목의 기준 컷 미정 |
| Risks and dependencies | covered | RK-001~RK-008 | — |
| Unresolved decisions | covered | OI-007 | 6건 해결, 1건 잔존(연출 자산 활용도, 구현 무관) |
| Handoff and authorization | covered | UD-010, Document State | 구현 승인됨. 커밋·씬 편집은 미승인 |

## Interview Checkpoint

- **Latest user message incorporated:** 명시적 finish + 구현 승인 (revision 4)
- **Latest sourced evidence incorporated:** SF-012 (`TitleMenu.cs`, `ResearchFlowSession.cs`)
- **Ledger transitions applied:** UD-010 신규. Open Items 표의 OI-004~OI-006 을 resolved 로 정정
- **Affected sections reconciled:** Document State, Snapshot, Ledger, Open Items, Coverage, Finalization
- **Contradictory active items check:** passed
- **Traceability check:** passed (R-001~R-019 모두 UD/SF/AR/OI 연결)
- **Current focus:** 없음. 인터뷰 종료
- **Next question IDs:** 없음
- **Resume point:** 계획이 다시 열리면 OI-007 과 구현에서 드러난 RK-005 결과부터

## Finalization and Handoff

- **Final interview state:** `explicitly-finished`
- **Authoritative English source:** `docs/specs/happy-ending-cinematic-spec.md`
- **Korean mirror:** `docs/specs/happy-ending-cinematic-spec.ko.md`
- **Synchronization check:** 두 파일이 같은 ID, 상태, 요구사항, 결정, 위험, 미해결 항목, next authorized action 을 담는다
- **Remaining gaps and consequences:** OI-007(낙하산 연출 보완 여부) — 구현을 막지 않는다. RK-005(테스트 재작성)는 구현 중 처리
- **Assumptions still requiring confirmation:** AR-001, AR-004, AR-005 는 여전히 권고이며 사용자 결정이 아니다
- **Next authorized action:** R-001~R-019 구현. finish 와 같은 메시지에서 승인됨
- **Implementation handoff:** 진입점 `SimulationStageHost.CompleteLaunch`(R-014), 보존 `Rocket` 루트 복제(R-015), 연출 B1~B7(R-002~R-007), 종료 `SceneManager.LoadScene("00_Title")`(R-017, R-018), 테스트 재작성 `ResearchCompletionFlowTests`(R-011, RK-005)
- **Resume point if planning reopens:** OI-007, 그리고 구현에서 나온 RK-005 결과

> 이 계획을 확정·승인하는 것은 커밋, PR, 패키지 설치, 배포, 외부 시스템 변경을 승인하는 것이 아니다. 이 계획의 구현만 사용자가 별도로 승인했다.
