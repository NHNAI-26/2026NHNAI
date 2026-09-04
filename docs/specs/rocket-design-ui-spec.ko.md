# 로켓 설계 화면 조작·UI 개선 계획 (한국어 미러)

> 정본은 `docs/specs/rocket-design-ui-spec.md`(영문). 이 파일은 동일한 ID·상태·내용을 담은 한국어 미러다.

## Document State

| 항목 | 값 |
| --- | --- |
| Interview state | `explicitly-finished` |
| 작업 언어 | 한국어(인터뷰) / 영문(정본 문서) |
| Current revision | 4 (최종) |
| Last updated | 2026-09-04 (KST) |
| 프로젝트 루트 | `C:\myGame\2026NHNAI` |
| 정본 경로 | `docs/specs/rocket-design-ui-spec.md` |
| 한국어 미러 | `docs/specs/rocket-design-ui-spec.ko.md` |
| 명시적 종료 | `yes` — "로 하고 구현 시작해줘" (rev 4) |
| 구현 권한 | 같은 메시지에서 **별도로** 부여됨 |
| 종료 시점 미해결 | **Q-004 / Q-005 / Q-009 및 OI-003, OI-005, OI-006, OI-007, OI-008, OI-010** |
| 다음 행동 | 명시된 가정 AR-012 / AR-013 / AR-014 아래 1단계 구현 |

관련 문서: `docs/specs/rocket-prototype-revision-spec.md`(선행 프로토타입, `explicitly-finished`),
`docs/specs/engine-preset-stats-spec.md`(엔진 스탯 SO, **다른 세션에서 진행 중**),
`docs/rocket-simulation.md`, `docs/artemis-2026-gdd/07_로켓_설계.md`,
`docs/artemis-2026-gdd/11_UI_UX_화면설계.md`, `docs/artemis-2026-gdd/18_확정사항_및_변경금지선.md`.

> **종료 시점 미해결.** Q-004(이동 버튼 vs 기존 좌클릭 드래그), Q-005(대상 씬), Q-009(엔진 제거 경로)는
> **묻기 전에** 인터뷰가 끝났다. 사용자 대신 결정하지 **않았다**. 구현은 명시적으로 표시된 가정
> AR-012 / AR-013 / AR-014 아래 진행하며, 이들은 사용자가 확인하기 전까지 사용자 결정이 아니라
> 에이전트 가정으로 남는다.

## Current Snapshot

- **Outcome:** 설계 화면 좌측 프리셋 패널에서 엔진에 마우스를 올려 스탯을 확인하고, 드래그해 꺼낸다.
  로켓 표면에 놓으면 즉시 부착, 바닥에 놓으면 바닥 배치. 붙은 엔진을 클릭하면 이동·회전 버튼이 뜨고,
  회전한 자세대로 추력이 나간다.
- **주 사용자:** 개발자 본인(조작 검증), ARTEMIS: 2026 플레이어(설계 단계).
- **In scope:** 프리팹 기반 좌측 프리셋 패널 UI, 호버 스탯 표시(2단계, 다른 세션 SO 기반), 드래그 부착/바닥 배치,
  선택 시 이동·회전 버튼, **부품 자세를 따르는 추력 모델 전환**, 기존 우클릭 궤도 회전 유지,
  UI·3D 입력 분리, GDD 07 §5 및 `docs/rocket-simulation.md` 개정.
- **Out of scope:** 엔진 스탯 SO 데이터 모델 자체(다른 세션), 발사 확률 계산, 엔진 ON/OFF 타임라인,
  맵·목표 경로 생성, 설계 적합도 표시.
- **단계 구분:** **1단계** = 패널 골격 + 드래그 배치·부착 + 회전/추력 모델 + 입력 분리 + 문서·테스트 개정.
  **2단계** = 다른 세션 SO 확정 후 호버 스탯 결선(UD-012).

## Outcome and Context

### 목표

현재 설계 조작은 "씬에 미리 놓인 엔진을 좌클릭 드래그해 로켓 표면에 붙인다"가 전부다. 이를 **좌측 프리셋
패널에서 꺼내 쓰는** 형태로 바꾼다. 항목 호버로 스탯을 미리 보고, 드래그해 로켓 표면에 놓으면 **그 자리에
즉시 부착**, 바닥에 놓으면 **바닥 배치**, 그 외에는 취소·폐기한다(UD-007, UD-010). 붙인 엔진은 클릭해
선택하면 이동·회전 버튼이 뜨고, **회전한 자세대로 추력이 나간다**(UD-008). 축·각도 제한은 없다(UD-011).
우클릭 드래그 화면 회전은 현행 유지.

### 배경

- 씬에 미리 배치된 `RocketPart`만 쓸 수 있다. `RocketBuilder.BeginDrag`가 커서 아래 기존 부품을 집는
  구조라 새로 꺼내는 경로가 없다(SF-002).
- 어떤 엔진이 어떤 성능인지 화면에서 알 수 없다. `RocketPart`는 인스펙터에만 값을 노출한다(SF-011).
- 회전이 불가능하다. `Rocket.Attach`가 부품 회전을 로켓 회전으로 덮어쓰고(SF-003) EditMode 테스트가 그
  규칙을 잠근다(SF-013). UD-008이 이를 걷어낸다.

### 계획 경계

이 문서는 **조작 모델, UI 구성, 그에 따른 추력 방향 모델 변경**을 결정한다. 엔진 스탯 데이터 구조
(SO 필드·프리셋 개수·가격)는 `docs/specs/engine-preset-stats-spec.md` 소관이며 **다른 세션에서 작업 중**
이다(UD-009, SF-012).

## Users and Stakeholders

| 이해관계자 | 필요·책임·관심 | 근거 ID | 상태 |
| --- | --- | --- | --- |
| 개발자 본인 | 꺼내 붙이는 조작을 확인하고 튜닝 | UD-002, UD-007 | active |
| 플레이어 | 스탯을 보고 엔진을 고르고 배치·자세를 조정 | UD-005, UD-008 | active |
| GDD 문서 관리자 | GDD 07 §5의 두 조항(부품 카탈로그 금지, 부품 자세 고정)이 모두 개정 대상 | SF-004, RK-001, RK-002 | active |
| 엔진 스탯 SO 세션 | 2단계가 그 세션의 SO 타입·필드·에셋에 의존 | UD-009, UD-012, RK-004 | active |

## Scope and Non-Goals

### In Scope

| 항목 | 근거 ID | 상태 | 단계 | 비고 |
| --- | --- | --- | --- | --- |
| 좌측 엔진 프리셋 패널 UI | UD-002, UD-003 | active | 1 | UGUI, 화면 루트와 반복 항목 프리팹 기반 |
| 프리셋 드래그 → 로켓 부착 / 바닥 배치 | UD-007, UD-010 | active | 1 | 드롭 지점이 분기를 정함 |
| 부착 엔진 클릭 선택 → 이동·회전 버튼 | UD-004 | active | 1 | 이동 버튼 동작은 OI-003 |
| 제한 없는 부품 자세 회전 + 추력 추종 | UD-008, UD-011 | active | 1 | 잠금 규칙·테스트·GDD 조항 개정 포함 |
| 우클릭 드래그 궤도 회전 현행 유지 | UD-001, SF-001 | active | 1 | 변경 없음 |
| UI 포인터와 3D 레이캐스트 입력 분리 | AR-002, SF-009 | active | 1 | 미구현 시 패널 클릭이 3D로 샘 |
| GDD 07 §5 및 `docs/rocket-simulation.md` 개정 | UD-002, UD-008, SF-004 | active | 1 | 조항 2개가 구현과 모순 |
| 프리셋 항목 호버 시 스탯 표시 | UD-005, UD-009, UD-012 | active | 2 | 다른 세션 SO 확정 후 결선 |

### Out of Scope

| 제외 항목 | 근거 ID | 상태 | 이유 |
| --- | --- | --- | --- |
| 엔진 스탯 SO 필드·프리셋 개수·가격 정의 | UD-009, UD-012, SF-012 | active | 다른 세션 소관, 여기서는 소비만 |
| 힘 크기 슬라이더, 엔진 ON/OFF 타임라인 | SF-005 | active | GDD 07 §4 항목이나 이번 요청 밖 |
| 맵·목표 경로·설계 적합도·확률 표시 | SF-016 | active | GDD 11 §7 항목이나 이번 요청 밖 |
| 그리드·대칭 스냅, 겹침 검사 | SF-004 | active | GDD 07 §5 의도적 미구현. 회전 각도 스냅은 rev 4 에서 이 목록을 떠났다(UD-011) |
| 단 분리, 짐벌, 항력 | SF-004 | active | GDD 07 §5 의도적 미구현 |

## Core Experience / Operating Flow

### 주 흐름 (최종)

1. 설계 화면에 들어가면 3D 로켓과 **좌측 엔진 프리셋 패널**이 보인다.
2. (2단계) 항목 호버 시 그 엔진 스탯이 표시된다(다른 세션 SO에서 읽음, UD-009).
3. 항목을 **좌클릭 드래그**해 맵으로 끌면 엔진 인스턴스가 커서를 따라온다.
4. 놓은 지점이 **로켓 표면이면 즉시 부착**, **바닥이면 바닥 배치**, **그 외에는 취소·폐기**(UD-010).
5. 바닥에 놓인 엔진은 기존 좌클릭 드래그로 집어 부착할 수 있다(SF-002 경로 그대로).
6. 붙은 엔진을 **클릭**하면 선택되고 **이동·회전 버튼**이 나타난다.
7. 회전하면 **그 자세대로 추력이 나간다**(UD-008). 축·각도 제한 없음(UD-011).
8. 조작 내내 **우클릭 드래그**로 카메라를 돌린다(SF-001).

### 대체·오류·경계 흐름

| 조건 | 기대 동작 | 관련 ID | 상태 |
| --- | --- | --- | --- |
| 패널 위 좌·우클릭 | 3D 부품 드래그·카메라 회전 미발동 | R-006, SF-009 | active |
| 프리셋 드롭이 로켓 표면 위 | 그 지점에 즉시 부착 | UD-010, R-013 | active |
| 프리셋 드롭이 바닥 위 | 바닥 배치, 부착 안 함 | UD-007, R-004 | active |
| 프리셋 드롭이 그 외 | 취소, 인스턴스 폐기 | UD-010, R-013 | active |
| 선택 상태에서 빈 공간 클릭 | 선택 해제, 버튼 숨김 | R-005 | active |
| 발사 후(`rocket.Launched`) 패널 조작 | 부품 드래그는 이미 차단. 패널 처리 미정 | SF-002, OI-006 | open |
| 뒤집거나 눕힌 엔진으로 발사 | 추력이 부품을 따라 로켓이 회전·추락 — 의도된 결과 | UD-008, UD-011, RK-007 | active |
| 바닥에 꺼내 놓고 안 쓴 엔진 | 제거·회수 경로 미정 | OI-007 | open |
| 2단계 결선 전 호버 | 스탯 값 없음, 영역만 존재 | UD-012, AR-011 | active |

### 상태·데이터·수명 주기

- **추력 모델이 바뀐다.** `Rocket.FixedUpdate`가 쓰던 `transform.up`(로켓 up, SF-003)이 UD-008에 따라
  `engine.transform.up`이 되고 `Rocket.Attach`의 회전 덮어쓰기가 사라진다.
  `RocketSimulationTests.Attach_KeepsWorldPoint_AndAlignsToRocket`(SF-013)은 갱신 대상이다.
- 이는 `docs/rocket-simulation.md`가 **의도적으로 배제했다고 기록한 결과**를 받아들이는 것이다
  ("측면 엔진이 로켓을 돌려버려 게임이 다른 것이 된다", SF-020). UD-008로 방향을 택했고 UD-011로 제한도
  두지 않았으므로, 뒤집힌 엔진이 로켓을 땅에 박는 배치까지 허용된다(RK-007).
- 드롭 분기는 기존 `EndDrag`의 `_overRocket` 판정을 확장한다(SF-021, AR-008).
- 인스턴스 소스로 `Assets/03. Prefabs/Simulation/RocketEngine.prefab`이 이미 있다(SF-006).
- `Border.Simulation`은 `autoReferenced: true`라 asmdef 수정 없이 UGUI/TMP를 쓸 수 있다(SF-008).

## Requirements

| ID | 요구사항 | 유형 | 근거 ID | 우선도 | 단계 | 상태 | 성공 근거 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R-001 | 우클릭 궤도 회전·휠 줌 현행 유지 | functional | UD-001, SF-001 | must | 1 | active | 기존 조작 회귀 없음 |
| R-002 | 화면 좌측에 엔진 프리셋 목록 패널 표시 | functional | UD-002 | must | 1 | active | 좌측에 항목이 보임 |
| R-003 | 프리셋 항목 호버 시 스탯 표시 | functional | UD-005 | must | 2 | active | 항목마다 값이 다름 |
| R-004 | 바닥 드롭 시 엔진 인스턴스가 바닥에 배치 | functional | UD-007 | must | 1 | active | 미리 두지 않은 엔진이 바닥에 생김 |
| R-005 | 부착 엔진 클릭 시 선택 + 이동·회전 버튼 표시 | functional | UD-004 | must | 1 | active | 클릭 시 표시, 해제 시 숨김 |
| R-006 | UI 위 포인터가 3D 드래그·카메라 회전을 발동시키지 않음 | quality | AR-002, SF-009 | must | 1 | active | 패널 드래그로 카메라 안 돎 |
| R-007 | 회전 버튼은 축·각도 제한 없이 자세를 회전하고 추력이 자세를 따름 | functional | UD-008, UD-011 | must | 1 | active | 눕힌 엔진이 로켓을 회전시킴 |
| R-008 | 이동 버튼과 기존 좌클릭 드래그의 관계 정의 | functional | UD-004, OI-003 | should | 1 | blocked | Q-004 미질문, AR-012로 구현 |
| R-009 | `docs/rocket-simulation.md`와 GDD 07 §5를 같은 커밋에서 개정 | process | UD-002, UD-008, SF-004 | must | 1 | active | 문서와 구현 불일치 없음 |
| R-010 | `Rocket.FixedUpdate` 추력을 `engine.transform.up` 기준으로 | technical | UD-008, SF-003 | must | 1 | active | 자세가 궤적에 반영 |
| R-011 | `Rocket.Attach` 회전 덮어쓰기 제거 + 테스트 갱신 | technical | UD-008, SF-013 | must | 1 | active | 테스트가 새 규칙을 잠금 |
| R-012 | 호버 스탯은 다른 세션 확정 SO에서 읽음 | technical | UD-009, UD-012 | must | 2 | deferred | SO 확정이 revisit trigger |
| R-013 | 드롭 지점에 따라 부착 / 바닥 배치 / 취소 분기 | functional | UD-010 | must | 1 | active | 세 경우가 각각 다르게 동작 |

## Constraints

| 범주 | 제약 | 근거 ID | 결과 | 상태 |
| --- | --- | --- | --- | --- |
| policy | GDD 07 §5 의도적 미구현에 "부품 카탈로그" | SF-004 | 프리셋 패널은 조항 개정 필요 | active |
| policy | GDD 07 §5 "부품 자세는 로켓 기준" | SF-004, SF-003 | UD-008/UD-011이 뒤집음 — 개정 필수 | active |
| policy | GDD 07 §3 허용 입력에 "힘 방향 조정" | SF-005 | 자세 회전을 힘 방향 조정으로 기술하면 정합 | active |
| policy | GDD 18 "플레이어는 직접 조종하지 않는다" | SF-017 | 설계 단계 조작만 확장 가능 | active |
| technical | `Attach_KeepsWorldPoint_AndAlignsToRocket`가 자세 규칙 잠금 | SF-013 | R-011로 갱신 | active |
| technical | `activeInputHandler: 1` — 레거시 `UnityEngine.Input` 불가 | SF-018 | Input System 또는 EventSystem 경유 | active |
| technical | `RocketBuilder`가 `Physics.Raycast` 직접 호출, UI 차단 없음 | SF-009 | 입력 분리 필수 | active |
| dependency | SO 타입·필드·경로는 다른 세션에서 확정. 2단계는 그 전에 시작 안 함 | UD-009, UD-012 | 1단계는 SO 없이 완결 | active |
| process | 정식 UI는 프리팹 기반. 코드 생성 UGUI는 임시 디버그 화면에만 허용 | 최신 GDD 11/18 | 메인/설계 UI는 프리팹 인스턴스에 데이터 바인딩 | active |
| process | `07_로켓_설계.md`에 미커밋 수정분 존재 | SF-015 | 사용자 변경분 보존 | active |

## Success Evidence

| 관련 R | 근거·수용 조건 | 검증 방법 | 담당 | 상태 |
| --- | --- | --- | --- | --- |
| R-002 | 좌측에 프리셋 항목이 보인다 | Play Mode + 스크린샷 | 개발자 | proposed |
| R-004, R-013 | 로켓 드롭 → 부착, 바닥 드롭 → 배치, 허공 드롭 → 폐기 | Play Mode 관찰 | 개발자 | proposed |
| R-005 | 클릭 → 버튼 표시, 빈 공간 클릭 → 숨김 | Play Mode + 스크린샷 | 개발자 | proposed |
| R-006 | 패널 위 우클릭 드래그로 카메라가 안 돈다 | Play Mode 관찰 | 개발자 | proposed |
| R-007, R-010 | 엔진 하나를 눕히고 발사하면 로켓이 그 방향으로 돈다 | Play Mode 발사 | 개발자 | proposed |
| R-011 | 갱신된 EditMode 테스트 통과 | EditMode 테스트 | 개발자 | proposed |
| R-001 | 기존 궤도 회전·줌·표면 부착 회귀 없음 | Play Mode + EditMode | 개발자 | proposed |
| R-003, R-012 | 호버 시 SO 값이 항목마다 다르게 뜬다 | Play Mode + 스크린샷 | 개발자 | deferred (2단계) |

## Decision and Evidence Ledger

| ID | 종류 | 내용 | 근거 | 상태 | 연결 |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | 화면 회전은 우클릭 드래그 유지 | 최초 요청 (rev 1) | active | R-001 |
| UD-002 | user decision | 화면 좌측 프리셋에서 엔진을 가져온다 | 최초 요청 | active | R-002, RK-001 |
| UD-003 | user decision | 프리셋은 "우선 UI만 구현" | 최초 요청 | active | UD-007이 경계 확정 |
| UD-004 | user decision | 엔진 클릭 시 이동·회전 버튼 | 최초 요청 | active | R-005, R-007, R-008 |
| UD-005 | user decision | 프리셋 호버 시 스탯 표시 | 최초 요청 | active | R-003, UD-009 |
| UD-006 | user decision | 프리셋에서 드래그로 맵에 가져와 부착 | 최초 요청 | corrected (rev 2) | UD-007/UD-010으로 대체 |
| UD-007 | user decision | 범위는 호버 + 드래그 배치까지. 바닥 드롭은 바닥에 배치 | Q-001 응답 (rev 2) | active | R-004, R-013 |
| UD-008 | user decision | 회전 버튼은 부품 자세를 회전시키고 **추력도 따라간다** | Q-002 응답 (rev 2) | active | R-007, R-010, R-011, RK-007 |
| UD-009 | user decision | 호버 스탯은 **다른 세션 제작 중 SO** 사용 | Q-003 응답 (rev 2) | active | R-012, RK-004, OI-010 |
| UD-010 | user decision | 로켓 표면 드롭 = **즉시 부착**, 바닥 = 배치, 그 외 = 취소·폐기 | Q-006 응답 (rev 3) | active | R-013, OI-009·OI-011 해소 |
| UD-011 | user decision | 회전은 **축·각도 제한 없이 자유**. rev 4 에서 45° 배수 조준 스냅(허용치 7°, 걸린 동안만 가이드선)을 추가 | Q-007 응답 (rev 3), 사용자 개정 (rev 4) | active | R-007, OI-012 해소, RK-007 강화 |
| UD-012 | user decision | SO **확정 후 연결**. 1단계는 SO 없이 완결 | Q-008 응답 (rev 3) | active | R-012 deferred, RK-004·008 완화 |
| UD-013 | user decision | 인터뷰 종료 및 구현 착수 | "로 하고 구현 시작해줘" (rev 4) | active | 최종화, 구현 권한 |
| SF-001 | sourced fact | 우클릭 궤도 회전 + 휠 줌이 이미 구현됨 | `RocketBuilder.cs:66-80` | active | R-001 |
| SF-002 | sourced fact | 좌클릭 드래그는 **기존** `RocketPart`만 집는다. 생성 경로 없음 | `RocketBuilder.cs:82-127` | active | R-004, AR-003 |
| SF-003 | sourced fact | `Rocket.Attach`가 회전을 덮어쓰고 추력은 `transform.up * Thrust` | `Rocket.cs:28-33`, `Rocket.cs:63` | active | R-010, R-011, RK-002 |
| SF-004 | sourced fact | GDD 07 §5에 **부품 카탈로그** 미구현 + **"부품 자세는 로켓 기준"** | `07_로켓_설계.md` §5 | active | RK-001, RK-002, R-009 |
| SF-005 | sourced fact | GDD 07 §3·GDD 11 §7 모두 "힘 방향 조정"을 허용 입력으로 명시 | 두 문서 | active | R-009 |
| SF-006 | sourced fact | `RocketEngine.prefab` 존재 (`thrust=1200`, `fuel=100`, `burnRate=20`) | 프리팹 4964-4967행 | active | AR-003 |
| SF-007 | sourced fact | 이전 연구 프로토타입은 UGUI를 코드로 생성하고 `[RuntimeInitializeOnLoadMethod]`로 스폰했다. 최신 GDD는 정식 UI에서 이 방식을 대체한다 | `ResearchOperationUIController.cs:43-90`; GDD 11/18 | active | AR-001 superseded |
| SF-008 | sourced fact | `Border.Simulation`은 `autoReferenced: true`라 UGUI/TMP 사용 가능 | `Border.Simulation.asmdef` | active | AR-001, R-012 |
| SF-009 | sourced fact | `RocketBuilder`가 `Physics.Raycast` 직접 호출, UI 차단 코드 전무 | `RocketBuilder.cs:87,102` + 전체 grep | active | R-006, RK-003 |
| SF-010 | sourced fact | 엔진 스탯은 GDD 4종, 0~100 | `07_로켓_설계.md` §6 | active | R-003, R-012 |
| SF-011 | sourced fact | `RocketPart`에 4스탯 구현 없음 | `RocketPart.cs:9-12` | active | OI-010 |
| SF-012 | sourced fact | `engine-preset-stats-spec.md`가 rev 1 `active`, Q-001~003 미응답 | 해당 문서 | active | R-012, RK-004, OI-010 |
| SF-013 | sourced fact | `Attach_KeepsWorldPoint_AndAlignsToRocket`가 자세 규칙을 잠금 | `RocketSimulationTests.cs:37-59` | active | R-011 |
| SF-014 | sourced fact | `SimulationTest.unity`는 git 추적 중 (선행 spec 기술은 낡음) | `git ls-files` | active | 씬 편집이 커밋에 남음 |
| SF-015 | sourced fact | `07_로켓_설계.md` §6.1에 미커밋 1줄 수정 | `git diff` | active | R-009, RK-005 |
| SF-016 | sourced fact | GDD 11 §7 설계 화면 필수 정보 목록 | `11_UI_UX_화면설계.md` §7 | active | 범위 밖이나 화면 구성에 영향 |
| SF-017 | sourced fact | GDD 18 변경 금지선 | `18_확정사항_및_변경금지선.md` | active | 설계 단계만 확장 가능 |
| SF-018 | sourced fact | `activeInputHandler: 1` | `docs/rocket-simulation.md` | active | 입력 경로 제한 |
| SF-019 | sourced fact | 선행 spec R-018/R-019가 Q-011 대기로 `blocked` | 해당 문서 §6 | active | RK-006 |
| SF-020 | sourced fact | `rocket-simulation.md`가 부품 추종 추력을 **의도적 배제 결정**으로 기록 | 해당 문서 | active | RK-007, R-009 |
| SF-021 | sourced fact | `EndDrag`가 이미 `_overRocket`으로 부착/복귀를 분기 | `RocketBuilder.cs:129-138` | active | R-013, AR-008 |
| AR-001 | agent recommendation | 패널은 프리팹으로 만들고 런타임에는 개발된 프리셋 목록만 바인딩한다 | 최신 사용자 수정이 정식 화면의 전체 코드 생성을 거부 | active | R-002 |
| AR-002 | agent recommendation | `EventSystem.current.IsPointerOverGameObject()`로 UI 위 입력 차단 | 현재 차단 전무 | proposed | R-006, RK-003 |
| AR-003 | agent recommendation | 프리셋 드래그는 프리팹 인스턴스화 후 기존 드래그 상태 재사용 | 배치 로직 중복 방지 | proposed | R-004, R-013 |
| AR-004 | agent recommendation | 회전을 추력 방향 조정으로만 정의 | 최소 변경안이었음 | superseded (rev 2) | UD-008이 자세 회전 선택 |
| AR-005 | agent assumption | 더미 스탯 표시 후 교체 | UI 선행 진행용 | superseded (rev 2) | UD-009/UD-012가 대기 선택 |
| AR-006 | agent recommendation | 이동 버튼은 선택 후 표면 드래그 모드가 최소 추가 | 별도 기즈모는 규칙 중복 | proposed | R-008, OI-003 |
| AR-007 | agent recommendation | 추력을 `engine.transform.up`으로, `Attach`의 회전 덮어쓰기만 제거 | 규칙 하나만 푸는 최소 diff | proposed | R-010, R-011 |
| AR-008 | agent recommendation | 드롭 3분기는 기존 `_overRocket` 분기 확장으로 구현 | 새 상태 기계 불필요 | proposed | R-013 |
| AR-009 | agent recommendation | 다른 세션 SO와의 결합면 최소화 | 타입 중복 정의 시 컴파일 충돌 | proposed | R-012, OI-010 |
| AR-010 | agent assumption | 제한 없으면 뒤집힌 엔진으로 로켓을 땅에 박을 수 있다 | 추력이 자세를 따름 | confirmed (rev 3) | UD-011 — 의도된 결과 |
| AR-011 | agent recommendation | 1단계는 SO 없이 완결, 호버 영역은 자리만 | UD-012가 대기를 선택 | proposed | R-003, R-012 |
| AR-012 | agent assumption | **이동 버튼**은 선택 부품의 표면 드래그 모드로 진입(기존 경로 재사용, 기즈모 없음) | Q-004 미질문. 구현 진행을 위해 AR-006을 적용 | proposed | R-008, OI-003 — 사용자 확인 필요 |
| AR-013 | agent assumption | **대상 씬**은 `SimulationTest.unity`. UI는 코드에서 스폰하므로 씬 YAML은 건드리지 않음 | Q-005 미질문. 존재하는 유일한 설계 씬 | proposed | OI-006, RK-006 — 사용자 확인 필요 |
| AR-014 | agent assumption | **제거 경로**는 선택 후 Delete/Backspace | Q-009 미질문. 없으면 무한히 쌓임(RK-009) | proposed | OI-007, RK-009 — 사용자 확인 필요 |
| OI-001 | unresolved item | "우선 UI만"의 경계 | 요청 내 상충 | resolved (rev 2) | UD-007 |
| OI-002 | unresolved item | 회전 버튼의 대상 | 잠금 규칙에 직결 | resolved (rev 2) | UD-008 |
| OI-003 | unresolved item | 이동 버튼 vs 좌클릭 드래그 | 조작 모호성 | open | R-008, AR-012, Q-004 |
| OI-004 | unresolved item | 호버 스탯 출처 | 표시할 값이 없음 | resolved (rev 2) | UD-009 |
| OI-005 | unresolved item | 프리셋 항목 수·기준 | 레이아웃 미정 | open | R-002, OI-010 |
| OI-006 | unresolved item | 대상 씬 | 씬 편집 범위 | open | RK-006, AR-013, Q-005 |
| OI-007 | unresolved item | 엔진 제거·회수 | 무한 배치, 되돌릴 방법 없음 | open | R-013, AR-014, Q-009 |
| OI-008 | unresolved item | GDD 07 §5 개정 범위(조항 2개) | 문서·구현 모순 | open | R-009, RK-001, RK-002 |
| OI-009 | unresolved item | 로켓 위 드롭 | 정의 없었음 | resolved (rev 3) | UD-010 |
| OI-010 | unresolved item | 다른 세션 SO 계약 | 이름 불일치 시 컴파일 실패 | deferred | UD-012 — SO 확정 시 |
| OI-011 | unresolved item | 허공 드롭 | 회수 불가 인스턴스 | resolved (rev 3) | UD-010 |
| OI-012 | unresolved item | 회전 자유도·제한 | 극단 배치가 곧 물리 | resolved (rev 3) | UD-011 |

## Question Register

| ID | 결정 필요 사항 | 중요한 이유 | 관련 ID | 상태 | 질문 revision | 결론 |
| --- | --- | --- | --- | --- | --- | --- |
| Q-001 | 이번 범위 경계 | "우선 UI만"과 "드래그로 부착"이 상충 | OI-001, UD-007 | answered | 1 → 2 | UD-007 |
| Q-002 | 회전 버튼의 대상 | 잠금 규칙·테스트·GDD 조항 | OI-002, UD-008 | answered | 1 → 2 | UD-008 |
| Q-003 | 호버 스탯 출처 | 스탯 SO 미확정 | OI-004, UD-009 | answered | 1 → 2 | UD-009 |
| Q-006 | 로켓 위·허공 드롭 | UD-007이 바닥만 정의 | OI-009, OI-011 | answered | 2 → 3 | UD-010 |
| Q-007 | 회전 자유도·제한 | 극단 배치가 곧 물리 결과 | OI-012 | answered | 2 → 3 | UD-011 |
| Q-008 | 다른 세션 SO 연결 계약·순서 | 타입 불일치·동시 수정 위험 | OI-010 | answered | 2 → 3 | UD-012 |
| Q-004 | 이동 버튼 vs 좌클릭 드래그 | 조작 경로 이중화 | OI-003, AR-012 | **open — 묻지 못함** | 1 → 4 | AR-012 가정으로 구현 |
| Q-005 | 대상 씬 | 씬 편집 범위, 본편 편입(RK-006) | OI-006, AR-013 | **open — 묻지 못함** | 1 → 4 | AR-013 가정으로 구현 |
| Q-009 | 꺼낸 엔진의 회수·제거 경로 | 무한 배치 | OI-007, AR-014 | **open — 묻지 못함** | 3 → 4 | AR-014 가정으로 구현 |

## Corrections and Revision History

| Revision | 계기 | 변경 | 정정·대체된 ID | 반영 범위 |
| --- | --- | --- | --- | --- |
| 1 | 최초 요청 + 코드·GDD·선행 spec 조사 | 초기 가설 수립 | none | 전 섹션 |
| 2 | Q-001~003 응답 | 범위 확정, 추력 모델 전환, 호버 데이터를 SO에 연결 | UD-006 `corrected`, AR-004·005 `superseded`, OI-001·002·004 `resolved` | Snapshot, Scope, Core flow, R-004/R-007/R-010~013, RK-002·007·008, Q-006~008 |
| 3 | Q-006~008 응답 | 드롭 3분기, 회전 제한 없음, SO 연결 2단계 분리 | OI-009·011·012 `resolved`, OI-010 `deferred`, AR-010 `confirmed` | Snapshot(단계), Scope(단계 열), Core flow 4, Requirements, Ledger(UD-010~012, SF-021, AR-011), RK-004·007·008, Q-009 |
| 4 | "로 하고 구현 시작해줘" — 명시적 종료 + 구현 승인 | 인터뷰 종료, 정본 영문화 + 한국어 미러 생성, Q-004/005/009는 `open` 유지하고 가정으로 구현 | none — 미해결 항목을 임의 결정하지 않음 | Document State, 미해결 배너, AR-012~014, Question Register, Finalization |

## Risks, Conflicts, and Dependencies

| ID | 종류 | 내용 | 가능성/영향 | 대응 | 관련 ID | 상태 |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | conflict | GDD 07 §5가 부품 카탈로그를 금지하는데 좌측 패널이 곧 그것이다 | 높음 / 중간 | R-009로 개정, 범위는 OI-008 | SF-004, UD-002 | open |
| RK-002 | conflict | 회전 버튼이 자세 고정 규칙·잠금 테스트·GDD 조항과 충돌 | 높음 / 높음 | **UD-008/UD-011로 결정** — 규칙을 바꾼다. R-010/R-011/R-009로 실행 | UD-008 | resolved (rev 2) |
| RK-003 | risk | UGUI 오버레이가 3D 레이캐스트·카메라 회전으로 샌다 | 높음 / 중간 | AR-002를 R-006으로 필수화 | SF-009, R-006 | open |
| RK-004 | dependency | 호버 스탯이 다른 세션 SO에 의존 | 높음 / 중간 | **UD-012로 완화** — 1단계 독립, 2단계 결선 | UD-012, OI-010 | mitigated |
| RK-005 | dependency | `07_로켓_설계.md` 미커밋 수정 상태 | 낮음 / 낮음 | 사용자 변경분 보존 | SF-015 | open |
| RK-006 | dependency | 본편 편입 결정(선행 spec Q-011)이 열려 있어 호스트 씬 미확정 | 중간 / 중간 | AR-013 가정으로 진행 | SF-019, OI-006 | open |
| RK-007 | risk | 추력이 자세를 따르고 **제한도 없어서** 조립 게임성이 바뀌고 뒤집힌 엔진이 로켓을 추락시킨다 | 확정 / 높음 | 사용자 선택(UD-008, UD-011). 문서를 그에 맞게 개정 | SF-020 | accepted |
| RK-008 | risk | 두 세션이 같은 영역을 동시 수정 | 중간 / 중간 | **UD-012로 완화** — 순차 진행, 착수 전 `git status` 확인 | UD-012 | mitigated |
| RK-009 | risk | 프리셋에서 무한히 꺼낼 수 있는데 회수 경로가 없어 잔해가 쌓인다 | 중간 / 낮음 | AR-014 가정(Delete 키)으로 구현 | OI-007 | open |

## Open, Skipped, and Deferred Items

| ID | 항목 | 상태 | 결과 | 현재 입장 | 담당 | 재검토 계기 |
| --- | --- | --- | --- | --- | --- | --- |
| OI-003 | 이동 버튼 vs 좌클릭 드래그 | open | 조작 모호 | AR-012 가정 적용 중 | 사용자 | Q-004 |
| OI-005 | 프리셋 항목 수·기준 | open | 레이아웃 미정 | 1단계는 프리팹 1종 반복, 2단계에 SO 목록 | 사용자 | OI-010 |
| OI-006 | 대상 씬 | open | 씬 편집 범위 | AR-013 가정 적용 중 | 사용자 | Q-005 |
| OI-007 | 엔진 제거·회수 | open | 잔해 누적 | AR-014 가정 적용 중 | 사용자 | Q-009 |
| OI-008 | GDD 07 §5 개정 범위 | open | 문서·구현 모순 | 두 조항 동시 개정 | 사용자 | Q-005 이후 |
| OI-010 | 다른 세션 SO 계약 | deferred | 컴파일 실패 위험 | AR-009 결합면 최소화 | 사용자 | 그 세션 SO 확정 |

## Coverage and Consistency Check

| 영역 | 상태 | 근거 ID | 남은 공백 |
| --- | --- | --- | --- |
| Outcome | covered | UD-001~005, UD-007~013 | — |
| 이해관계자 | covered | UD-009, UD-012, SF-012 | — |
| 범위 | covered | UD-007, UD-010, UD-012 | 단계 확정 |
| 비목표 | covered | SF-004, SF-016, UD-011 | — |
| 핵심 흐름 | covered | UD-007, UD-008, UD-010, UD-011 | 이동 버튼 조작은 가정(OI-003) |
| 제약 | covered | SF-003~005, SF-009, SF-013, SF-018 | GDD 07 §5 개정 전제 |
| 성공 근거 | covered | R-001~R-013 | R-003/R-012는 2단계 |
| 위험·의존성 | covered | RK-001~009 | RK-007 `accepted`, RK-004·008 `mitigated` |
| 미해결 결정 | open | OI-003, OI-005~008, OI-010 | Q-004, Q-005, Q-009 미질문 |
| 인수인계·권한 | covered | UD-013 | 1단계 구현 승인됨 |

## Finalization and Handoff

- **정본:** `docs/specs/rocket-design-ui-spec.md` (영문).
- **한국어 미러:** `docs/specs/rocket-design-ui-spec.ko.md` (이 파일) — ID·상태·요구사항·결정·위험·미해결
  항목·인터뷰 상태·다음 행동이 정본과 정확히 일치한다.
- **인터뷰 상태:** `explicitly-finished`. 추가 기획 질문 없음.
- **해소하지 않고 보존한 것:** Q-004 / Q-005 / Q-009 및 OI-003, OI-005, OI-006, OI-007, OI-008, OI-010.
  어느 것도 사용자 대신 결정하지 않았다. AR-012·AR-013·AR-014는 구현 진행을 위한 에이전트 가정이며,
  사용자가 확인하기 전까지 가정으로 남는다.
- **수용된 결과:** RK-007 — 조립 게임성이 바뀌고, 뒤집힌 엔진이 로켓을 추락시킬 수 있다.
- **다음 행동:** 1단계 구현(R-001, R-002, R-004, R-005, R-006, R-007, R-009, R-010, R-011, R-013).
  2단계(R-003, R-012)는 다른 세션 SO 확정 대기.
- **기획 재개 시 시작점:** Q-004·Q-005·Q-009를 묻고, OI-008(GDD 07 §5 개정 범위)과 OI-005(프리셋 항목 수)를
  정리한다.

> 이 계획 승인만으로는 커밋, PR, 배포, 외부 시스템 변경 권한이 생기지 않는다.
> 1단계 구현은 같은 메시지에서 별도로 승인되었다(UD-013).
