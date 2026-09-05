# 엔진 프리셋 스탯(ScriptableObject) 기획 문서

> 한국어 미러. 권위 원본: `engine-preset-stats-spec.md`.

## Document State

| Field | Value |
| --- | --- |
| Interview state | `explicitly-finished` |
| Working language | 한국어(인터뷰) / 영어(권위 원본) |
| Current revision | 6 |
| Last updated | 2026-09-04 (KST) |
| Project or workspace root | `C:\myGame\2026NHNAI` |
| Base path | `docs/specs/engine-preset-stats-spec.md` |
| Korean mirror path | `docs/specs/engine-preset-stats-spec.ko.md` |
| Explicit finish received | `yes` — "로 하고 구현해줘" (rev 5) |
| Next authorized action | 에디터에서 컴파일하고 `Simulation.EditModeTests` 를 돌린다(둘 다 미확인). 문서 재작성(R-016)과 공식 재설계(OI-012)는 여전히 미승인. |
| Implementation state | 반영됨, 미검증 — **Implementation Record** 참고 |

## Current Snapshot

- **Outcome:** 엔진 프리셋을 **물리 단위**로 ScriptableObject에 저작하고, 로켓 설계·발사가 변환 계층 없이 그 값을 그대로 쓴다. 냉각은 실제 열 축적 모델로 작동하며 GDD의 `0~100` 스탯 체계는 폐기한다.
- **Primary audience:** 기획자(저작·밸런싱), 설계·발사 구현자, 플레이어.
- **In scope:** 물리 단위 SO 데이터 모델, 10슬롯 제한, 런타임 개발 슬롯 필터링, 열 모델, 설계 단계 값 소비, 에디터 테스트 채우기 툴.
- **Out of scope:** 미니게임 구현, 런타임 프리셋 스탯 편집 UI, 런타임 슬롯 저장, GDD 06 공식 재설계.
- **Material unresolved items:** OI-005, OI-006, OI-007, OI-010, OI-011, OI-012, OI-013, OI-016, OI-017.
- **Active question IDs:** 없음 — 인터뷰 종료.

## Outcome and Context

### Desired Outcome

프리셋마다 가격, 연료 탱크 용량, 냉각 능력, 최대 출력, 점화 신뢰도를 가지며 **물리 단위로 직접 저장**한다(UD-007). GDD 06의 `0~100` 정규화 체계는 폐기하고 공식을 재작성한다(UD-010). 냉각은 열 방출량이며, 초당 `발열 − 냉각`만큼 온도가 쌓이고 공통 임계 온도를 넘으면 폭발한다(UD-012).

### Problem and Background

`RocketPart`가 `thrust = 1200f`, `fuel = 100f`, `burnRate = 20f`를 하드코딩한다(SF-005). "엔진마다 다르다"가 GDD에는 있으나 데이터에는 없다. GDD 06/07은 4스탯을 `0~100`으로 확정했고 품질·페널티·확률·파생 성능 공식이 모두 `최대 출력 − 냉각 능력` 같은 동일 스케일 비교에 의존한다(SF-001, SF-009, SF-017). 사용자는 정규화 대신 체계 폐기를 선택했다(UD-010). 열 모델은 GDD 08 §9의 `Overheat` 사고에 실제 시뮬레이션 근거를 준다(SF-018).

### Planning Boundary

이 문서는 단위계, 열 모델, SO 구조·제약, 설계 단계 값 소비, 테스트 툴, GDD 재작성 범위를 결정한다. 커밋, GDD 편집, 06 공식 재설계는 승인하지 않는다.

## Users and Stakeholders

| Stakeholder | 필요·관심 | Source IDs | Status |
| --- | --- | --- | --- |
| 기획자 | 인스펙터에서 물리 단위로 스탯 조정 | UD-004, UD-007, SF-008 | active |
| 테스트 담당 | 테스트 씬에서 프리셋 값을 빠르게 채움 | UD-009 | active |
| 설계·발사 구현자 | SO 값을 부품 물리와 열 모델에 반영 | UD-005, UD-012, SF-005 | active |
| 플레이어 | 엔진 선택과 개수가 결과를 바꾼다고 이해 | UD-014, UD-015, SF-002 | active |
| GDD 관리자 | 06 재작성과 07/08의 정합성 | UD-010, RK-008 | active |

## Scope and Non-Goals

### In Scope

| Scope item | Source IDs | Status |
| --- | --- | --- |
| 5개 필드를 가진 엔진 프리셋 SO 타입 | UD-003, UD-004 | active |
| 값을 물리 단위로 직접 저장 | UD-007 | active |
| `0~100` 스탯 체계 폐기 | UD-010 | active |
| 열 모델 (`dT/dt = 발열 − 냉각`, 공통 임계 온도, 하한 0) | UD-011, UD-012, UD-017 | active |
| 발열은 프리셋 최대치가 아니라 실제 사용 출력 기준 | UD-016 | active |
| 점화 신뢰도를 % 로 저장 | UD-013 | active |
| 프리셋 슬롯 최대 10 | UD-002, UD-008 | active |
| 새 게임은 개발된 프리셋 1개만 노출하고, 이후 프리셋은 새 엔진 개발로 열린다 | 최신 GDD 수정 | active |
| 로켓당 엔진 개수 무제한 | UD-014 | active |
| 한 로켓에 서로 다른 프리셋 혼합 | UD-015 | active |
| 테스트 프리셋 값을 채우는 에디터 툴 | UD-009 | active |
| 설계 단계는 SO 값을 읽기만 함 | UD-005, SF-002 | active |

### Out of Scope / Non-Goals

| Excluded item | Source IDs | 이유 |
| --- | --- | --- |
| 연구 미니게임 4종 | UD-005 | 사용자가 "우선 SO만"으로 범위 지정 |
| 런타임 프리셋 스탯 편집 UI | UD-005 | 데이터 + 읽기 + 테스트 툴까지 |
| 런타임 슬롯 저장 | UD-008, OI-010 | 슬롯 모델은 확정, 매체는 보류 |
| 다섯 번째 경영 자원 | SF-013, UD-006 | GDD 18 §4 금지, 가격은 표시 전용 |
| 단 분리, 부품 카탈로그, 짐벌 | SF-002 | GDD 07 §5 "의도적으로 만들지 않는 것" |
| 연료 소모에 따른 질량 감소 | SF-002 | GDD 07 §5가 배제 |
| GDD 06 공식 재설계 | OI-012 | 별도 기획 작업 (RK-008) |

## Core Experience / Operating Flow

### Primary Flow

1. 기획자가 `EngineStatsSO` 에셋을 만들고 가격과 4스탯을 물리 단위로 입력한다.
2. 에디터 툴이 테스트 프리셋 값을 한 번에 채운다(UD-009).
3. 프리셋을 최대 10개 슬롯 목록에 등록한다. 단, 새 게임은 슬롯 0(`Engine01`)만 노출한다.
4. 플레이어가 로켓 설계 단계에 진입한다.
5. 설계 단계는 개발된 슬롯만 읽어 선택 가능한 엔진을 보여준다. `새로운 엔진 개발`은 다음 슬롯을 순서대로 노출하며 최대 10개까지 가능하다.
6. 플레이어가 엔진을 **원하는 개수만큼**(UD-014), **프리셋을 섞어서**(UD-015) 부착한다.
7. `RocketPart`가 SO의 물리 값을 추력·연료·연소율·발열·냉각에 그대로 쓴다.
8. 발사 시 엔진마다 점화 신뢰도 %로 점화를 판정하고, 점화된 엔진이 추력을 걸며 온도를 누적한다.
9. 온도가 공통 임계값을 넘으면 `Overheat` 결과로 전환한다(SF-018).

### 열 모델 (UD-012, UD-016, UD-017)

```text
발열률   = k * 실제사용출력      # 실제 적용 출력에 선형, k는 코드 상수 (OI-013)
dT/dt   = 발열률 - 냉각
온도     = max(0, 온도 + dT/dt * dt)
온도 >= 공통 임계 온도  ->  과열
```

- 임계 온도는 모든 엔진이 공유하는 단일 상수다(UD-012).
- 온도는 **엔진별로** 누적하고 임계값만 공유한다(AR-011).
- 냉각이 발열을 넘으면 — 엔진 OFF로 발열이 0인 경우 포함 — 초과분만큼 온도가 내려가고 하한은 0이다(UD-017). ON/OFF 타이밍이 실제 과열 관리 수단이 된다.
- 발열이 실제 적용 출력을 따르므로 힘 슬라이더를 낮추면 추력을 내주고 열 여유를 얻는다(UD-016).

### Alternate, Error, and Edge Flows

| Condition | Expected behavior | Related IDs | Status |
| --- | --- | --- | --- |
| 프리셋 슬롯 비어 있음 | 엔진 배치 불가, 명시적 경고 로그 | R-005 | active |
| 슬롯 10개 초과 등록 | 저작 시점에 거부 또는 클램프 | R-002 | active |
| `RocketPart`에 SO 미연결 | 경고 후 배치 거부 | R-017, OI-007 | active |
| 서로 다른 프리셋 혼합 | 허용. 연료와 온도가 엔진별로 갈림 | R-006, UD-015 | active |
| 냉각 ≥ 발열 | 초과분만큼 하강, 하한 0 | R-012, UD-017 | active |
| 점화 판정 실패 | 해당 엔진 미점화, GDD 08 §9 `IgnitionFailure` | R-014, SF-018 | active |
| 엔진 하나 과열 | 발사 전체가 `Overheat`로 종료 | R-013, AR-012, OI-016 | active (가정) |
| 음수 스탯 값 | 에디터에서 0으로 클램프 | R-003 | active |

### State, Data, and Lifecycle Notes

- SO는 **저작 데이터**다. 런타임에 SO 필드를 쓰면 Play Mode 종료 후에도 값이 남아 밸런스 데이터를 오염시킨다(AR-005). 프리셋이 저장 슬롯이기도 하므로(UD-008) 이번 범위는 SO를 테스트·저작 데이터로 쓰고 런타임 저장을 미룬다(AR-006, RK-007, OI-010).
- 온도는 런타임 전용 상태이며 SO에 기록하지 않는다.
- GDD 07 §7 `DesignData`에 이미 `engineStats` 필드가 있다(SF-011).

## Requirements

| ID | Requirement | Type | Source IDs | Priority | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- |
| R-001 | 프리셋은 가격, 연료 용량, 냉각, 최대 출력, 점화 신뢰도를 가진다 | functional | UD-003 | must | active | 인스펙터에 5개 필드 |
| R-002 | 프리셋 슬롯은 최대 10개 | functional | UD-002, UD-008 | must | active | 11번째 거부/클램프 |
| R-003 | 스탯 값은 유효 하한(0 이상)으로 클램프 | quality | UD-007 | must | active | 음수 입력 클램프 |
| R-004 | 프리셋 데이터는 ScriptableObject로 저작 | technical | UD-004 | must | active | `Simulation/...` 메뉴로 생성 |
| R-005 | 설계 단계는 SO 값을 읽기만 함 | functional | UD-005, SF-002 | must | active | 스탯 상승 경로 없음 |
| R-006 | 엔진 여러 개는 추력을 합산 | functional | SF-002 | should | active | GDD 07 §6.3 일치 |
| R-007 | 프리셋 물리 값이 변환 없이 추력·연료·연소율에 쓰임 | functional | UD-005, UD-007, SF-005 | must | active | 프리셋 변경 시 궤적 변화 |
| R-008 | 가격은 표시·밸런스 전용, 자원 차감 없음 | functional | UD-006 | must | active | 소비 코드 경로 없음 |
| R-009 | 테스트 씬용 프리셋 SO 값 채우기 에디터 툴 | operational | UD-009 | must | active | 한 번에 발사 가능 상태 |
| R-010 | 미니게임 보상도 같은 물리 단위계 | functional | UD-007, UD-010 | should | blocked | OI-012 종속 |
| R-011 | 기존 1200 N / 100 / 20과 동등한 기준 프리셋 존재 | quality | SF-005, RK-003 | should | active | 기준 궤적이 프로토타입과 동일 |
| R-012 | 냉각은 열 방출량이며 초당 `발열 − 냉각`을 누적, 하한 0 | functional | UD-011, UD-012, UD-017 | must | active | 냉각 낮은 프리셋이 먼저 임계 도달, OFF 시 냉각 |
| R-013 | 공통 임계 온도 초과 시 과열 실패 | functional | UD-012 | must | active | 임계에서 과열 발생 |
| R-014 | 점화 신뢰도는 % 확률로 점화 판정에 사용 | functional | UD-013 | must | active | 0%는 항상 실패, 100%는 항상 성공 |
| R-015 | 부착 엔진 개수 상한 없음 | functional | UD-014 | must | active | 다수 부착 거부 없음 |
| R-016 | GDD 06의 `0~100` 공식을 물리 단위로 재작성 | operational | UD-010 | must | blocked | OI-012 종속, 이번 미승인 |
| R-017 | 발열은 프리셋 최대치가 아니라 실제 적용 출력 기준 | functional | UD-016 | must | active | 출력을 낮추면 온도 상승 둔화 |

## Constraints

| Category | Constraint | Source IDs | Consequence | Status |
| --- | --- | --- | --- | --- |
| policy | GDD 07 §3 설계 단계 자원 구매 금지 | SF-003 | UD-006으로 준수 | active |
| policy | GDD 18 §4 다섯 번째 경영 자원 금지 | SF-013 | UD-006으로 준수 | active |
| policy | GDD 08 §9 한 발사에 주요 사고 하나 | SF-018 | 점화 실패와 과열이 동시에 나면 안 됨 | active |
| policy | 4스탯 표시 이름 유지, 값 체계만 교체 | SF-001 | 이름 보존 | active |
| technical | 엔진 코드는 `Simulation` 어셈블리, SO 메뉴는 `Simulation/...` (UD-019) | SF-008 | 새 SO도 동일 | active |
| technical | 에디터 전용 코드는 `Border.Editor`(`Assets/06. Packages/Editor`) | SF-016 | 툴 배치 위치 | active |
| technical | `Rocket`은 추력을 뉴턴으로 직접 적용 | SF-005, SF-006 | 물리 저장이므로 변환 불필요 | active |
| technical | 엔진 목록은 발사 시점 고정(`ponytail:` 주석) | SF-014 | 비행 중 교체 없음 | active |
| technical | 연료 소모에 따른 질량 감소 없음 | SF-002 | 연료 질량은 고정 무게 — `Rocket.tankMassPerFuel`로 구현, 발사 시 `Rigidbody.mass`에 합산 | active |
| process | `docs/artemis-2026-gdd/07_로켓_설계.md` 워킹트리 수정 중 | SF-015 | 사용자 변경분 보존 | active |

## Success Evidence

| Related IDs | 수용 조건 | 방법 | Owner | Status |
| --- | --- | --- | --- | --- |
| R-001, R-003, R-004 | 5개 필드 입력·클램프 확인 | 에디터 검사 | 기획자 | proposed |
| R-002 | 11번째 슬롯 거부 | 검사 | 구현자 | proposed |
| R-007, R-011 | 기준 프리셋은 프로토타입과 동일, 다른 프리셋은 도달 고도 상이 | Play Mode | 구현자 | proposed |
| R-012, R-013, R-017 | 냉각 부족 프리셋 과열, 출력 낮추거나 OFF 하면 냉각 | Play Mode + `Log.D` | 구현자 | proposed |
| R-014 | 0%는 미점화, 100%는 항상 점화 | Play Mode | 구현자 | proposed |
| R-006, R-015 | 혼합 다수 엔진의 추력 합산과 연료·온도 분화 | Play Mode | 구현자 | proposed |
| R-005, R-008 | SO 쓰기 경로와 가격 차감 경로 부재 | 코드 리뷰 / grep | 리뷰어 | proposed |
| R-009 | 툴 한 번으로 테스트 씬 발사 가능 | 검사 | 테스트 담당 | proposed |
| R-016 | GDD 06에 `0~100` 잔재 없음 | 문서 리뷰 | 문서 관리자 | blocked |

## Decision and Evidence Ledger

| ID | Kind | Statement | Evidence / rationale | Status | Linked IDs |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | 엔진마다 스탯이 다르고 프리셋으로 저장한다 | 최초 요청 (rev 1) | active | R-001, R-004 |
| UD-002 | user decision | 프리셋 최대 10개 | 최초 요청 | active | R-002 |
| UD-003 | user decision | 필드: 가격, 연료 탱크 용량, 냉각 능력, 최대 출력, 점화 신뢰도 | 최초 요청 | active | R-001, R-008 |
| UD-004 | user decision | 프리셋 스탯은 ScriptableObject로 저작 | 최초 요청 | active | R-004, AR-001 |
| UD-005 | user decision | 이번 범위: SO 먼저, 설계 단계는 값만 읽어 실행 | 최초 요청 | active | R-005, R-007 |
| UD-006 | user decision | 가격은 표시·밸런스 전용, 차감 없음 | Q-001 (rev 2) | active | R-008 |
| UD-007 | user decision | SO는 물리 단위 직접 저장, 미니게임 보상도 동일 단위계 | Q-002 (rev 2) | active | R-007, R-010 |
| UD-008 | user decision | 프리셋은 10슬롯 저장 모델 | Q-003 (rev 2) | active | R-002, OI-010 |
| UD-009 | user decision | 테스트 전용 SO 값 채우기 툴 제공 | Q-003 (rev 2) | active | R-009, OI-011 |
| UD-010 | user decision | `0~100` 체계 폐기, GDD 06 공식 재작성 | Q-005 (rev 3) | active | R-016, OI-012, RK-008 |
| UD-011 | user decision | 냉각 능력은 엔진의 열 방출량 | Q-006 (rev 3) | active | R-012 |
| UD-012 | user decision | 발열은 출력에 선형, 초당 `발열 − 냉각` 누적, 모든 엔진 공통 임계 온도 초과 시 폭발 | Q-006 (rev 3) | active | R-012, R-013, OI-013, OI-016 |
| UD-013 | user decision | 점화 신뢰도는 % | Q-006 (rev 3) | active | R-014 |
| UD-014 | user decision | 엔진은 원하는 만큼 부착 가능 | Q-004 (rev 3) | active | R-015 |
| UD-015 | user decision | 한 로켓에 서로 다른 프리셋 혼합 허용 | Q-007 (rev 4) | active | R-006, OI-004 resolved |
| UD-016 | user decision | 발열은 실제 적용 출력 기준 | Q-008 (rev 4) | active | R-017, OI-014 resolved, RK-010 resolved |
| UD-017 | user decision | 냉각이 발열보다 크면 초과분만큼 하강, 하한 0. 엔진 OFF면 발열 0 | Q-009 (rev 4) | active | R-012, OI-015 resolved |
| UD-018 | user decision | 이 결정대로 진행하고 구현한다 | "로 하고 구현해줘" (rev 5) | active | 최종화 + 구현 승인 |
| SF-001 | sourced fact | 4스탯 `FuelCapacity`/`Cooling`/`MaxOutput`/`IgnitionReliability`, 초기 20, 범위 0~100 | `06_엔진_연구.md` §2.2 | active | 값 체계는 UD-010이 대체 |
| SF-002 | sourced fact | 설계 단계는 읽기만, 추력 합산·연소 시간 최단 기준, 연료 질량 감소 없음 | `07_로켓_설계.md` §5, §6, §6.3 | active | R-005, R-006 |
| SF-003 | sourced fact | 설계 단계 "새 자원 구매" 금지 | `07_로켓_설계.md` §3 | active | UD-006 |
| SF-004 | sourced fact | GDD는 세션당 엔진 1개(`EngineState`) 모델, 프리셋 개념 없음 | `06_엔진_연구.md` §22 | active | RK-001, OI-005 |
| SF-005 | sourced fact | `RocketPart`가 `thrust=1200f`, `fuel=100f`, `burnRate=20f` 하드코딩 | `Assets/01. Scripts/Simulation/RocketPart.cs:9-12` | active | R-007, R-011 |
| SF-006 | sourced fact | 추력은 `AddForceAtPosition(transform.up * engine.Thrust, ...)` | `Assets/01. Scripts/Simulation/Rocket.cs:63` | active | UD-007과 정합 |
| SF-007 | sourced fact | `RocketBuilder`는 씬의 기존 `RocketPart`를 드래그할 뿐 데이터로 스폰하지 않음 | `Assets/01. Scripts/Simulation/RocketBuilder.cs:88-95` | active | R-015, R-017 |
| SF-008 | sourced fact | 기존 SO 관례 `[CreateAssetMenu(menuName = "Border/...")]`, `Border` 어셈블리 | `Assets/01. Scripts/Audio/SoundDatabaseSO.cs:90` | active | 신규 코드에 대해서는 UD-019 가 대체. 기존 에셋 메뉴 경로는 유지 |
| SF-009 | sourced fact | GDD 06 §23 파생 성능 공식이 모두 0~100 전제 | `06_엔진_연구.md` §23 | active | OI-012 |
| SF-010 | sourced fact | `docs/specs`는 base `.md` + `.ko.md` 쌍 관례 사용 | `docs/specs/rocket-prototype-revision-spec{,.ko}.md` | active | 최종화 경로 |
| SF-011 | sourced fact | `DesignData`에 `engineStats` 필드 존재 | `07_로켓_설계.md` §7 | active | 설계 데이터 형태 |
| SF-012 | sourced fact | `ResearchPrototypeModel`은 단계별 `Progress`만 추적, 4스탯 미구현 | `Assets/01. Scripts/Research/ResearchPrototypeModel.cs:30-38` | active | 프리셋이 유일 스탯 소스일 수 있음 |
| SF-013 | sourced fact | GDD 18 §4 다섯 번째 경영 자원 금지 | `18_확정사항_및_변경금지선.md` §4 | active | UD-006 |
| SF-014 | sourced fact | 엔진 목록은 `Launch()` 시점 고정(`ponytail:` 주석) | `Assets/01. Scripts/Simulation/Rocket.cs:52-53` | active | 비행 중 교체 없음 |
| SF-015 | sourced fact | `07_로켓_설계.md`에 미커밋 1줄 수정 | `git diff` (rev 1) | active | RK-004 |
| SF-016 | sourced fact | 에디터 전용 코드는 `Border.Editor`, `Border`를 참조 | `CLAUDE.md`; `Border.Editor.asmdef` | active | R-009 |
| SF-017 | sourced fact | GDD 06 §5는 0~100 점수에 스탯 `+10~+26` 부여 | `06_엔진_연구.md` §5 | active | R-010, OI-012 |
| SF-018 | sourced fact | GDD 08 §8~9: 냉각은 온도 지표 상승률과 과열 사고, `Overheat`는 "온도 경고 후 추력 진동 또는 폭발", 한 발사에 주요 사고 하나 | `08_로켓_발사.md` §8, §9 | active | R-013, RK-009 |
| SF-019 | sourced fact | `Assets/00. Scenes/SimulationTest.unity`가 기존 시뮬레이션 테스트 씬 | `find Assets -name "*.unity"` (rev 5) | active | R-009 대상 |
| UD-019 | user decision | 앞으로 만드는 코드에 `Border.` 접두사를 붙이지 않는다. Simulation 어셈블리를 `Border.Simulation` → `Simulation` 으로 개명(asmdef 이름, rootNamespace, 네임스페이스 선언, `CreateAssetMenu` 경로) | "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" (rev 6) | active | SF-020, SF-025, RK-012 |
| SF-020 | sourced fact | 런타임 코드는 하나가 아니라 세 어셈블리에 있다: `Border`(`Assets/01. Scripts`), `Border.Input`, 그리고 UD-019 이후 `Simulation`(`Assets/01. Scripts/Simulation`, `Border` 참조). `Border` 라는 이름은 vendored 업스트림 패키지 `com.borderjung.unity-modules` 저자 핸들에서 왔다 | `Assets/01. Scripts/Simulation/Simulation.asmdef`; `Packages/manifest.json:3` | active | 엔진 프리셋 타입은 `Simulation.*` |
| SF-021 | sourced fact | Unity 어셈블리 참조는 전이되지 않는다: `Border.Editor` 와 `Simulation.EditModeTests` 에 각각 참조를 명시적으로 추가해야 했다 | `Border.Editor.asmdef`; `Simulation.EditModeTests.asmdef` (rev 6) | active | 없으면 에디터 툴과 테스트가 컴파일되지 않는다 |
| SF-022 | sourced fact | `Rocket.Attach` 가 부품 회전을 로켓 기준으로 맞추지 않도록 바뀌고 주석은 추력이 부품 up 을 따른다고 말하지만, `Rocket.FixedUpdate` 는 여전히 `transform.up`(로켓 기준)을 쓰고 `RocketBuilder.Drag` 도 로켓 회전으로 스냅한다 | `Rocket.cs:29-37`, `Rocket.cs:84`; `RocketBuilder.cs:140` (rev 6, 이 세션 밖에서 변경됨) | active | RK-011, OI-017 |
| SF-023 | sourced fact | 컴파일과 테스트를 검증할 수 없었다: Unity 프로세스 3개가 프로젝트 락을 잡고 있어 batchmode 가 불가능하고 MCP for Unity 브리지도 끊겨 있다 | `Get-Process Unity`; `Temp/UnityLockfile`; 세션 MCP 상태 (rev 6) | active | 모든 성공 근거 항목이 `proposed` 로 남는다 |
| SF-024 | sourced fact | `RocketPart.ApplyPreset(EngineStatsSO)` 가 이 세션 밖에서 추가되었다 — 씬 인스턴스 전용 setter 이며 프리셋 에셋은 건드리지 않는다 | `RocketPart.cs:25-29` (rev 6) | active | R-005, AR-005 와 정합 |
| SF-025 | sourced fact | `Log` 와 `DeterministicRng` 는 여전히 `Border` 어셈블리(`Assets/01. Scripts/Core`)에 있어 `Simulation` 코드가 `using Border.Core;` 를 유지한다 | `Assets/01. Scripts/Core/Log.cs`, `DeterministicRng.cs` | active | UD-019 는 공용 코드까지 가지 않는다 |
| SF-026 | sourced fact | `CLAUDE.md` 는 모든 C# 이 `Border.*` 네임스페이스라고 적고 `Border`, `Border.Input`, `Border.Editor` 와 테스트 어셈블리 3개만 나열한다 — `Simulation` 과 `Border.Research.EditModeTests` 가 빠져 있다 | `CLAUDE.md`, Assembly layout | active | RK-012 |
| AR-001 | agent recommendation | 프리셋 1개 = `EngineStatsSO` 에셋 1개, 라이브러리 SO가 슬롯 목록을 갖고 `OnValidate`에서 10으로 클램프 | 개수 강제는 단일 지점 필요 | proposed | R-002, OI-010 |
| AR-002 | agent recommendation | (폐기) 0~100 저장 후 코드 변환 | UD-007이 대체 | superseded | — |
| AR-003 | agent recommendation | `RocketPart`에 `EngineStatsSO` 참조를 넣고 기존 필드를 파생시킴 | 최소 diff | proposed | R-007, SF-005 |
| AR-004 | agent assumption | 가격은 표시·밸런스 전용 | — | resolved | UD-006이 채택 |
| AR-005 | agent recommendation | 런타임에 SO 필드를 쓰지 않음 | 에디터 쓰기는 Play Mode 후에도 남음 | proposed | R-005, RK-007 |
| AR-006 | agent recommendation | 이번 범위는 SO를 테스트·저작 데이터로, 런타임 10슬롯 저장은 `SaveLoad`로 분리 | UD-004와 UD-008의 긴장을 단계 분리로 해소 | proposed | OI-010, RK-007 |
| AR-007 | agent recommendation | (폐기) 냉각 = 유지 가능 추력 N | UD-011이 대체 | superseded | — |
| AR-008 | agent recommendation | 연소율은 6번째 필드 대신 최대 출력에서 파생 | 필드 5개 고정, 추력이 크면 연료를 빨리 쓰는 관계가 자연스러움 | proposed | R-007, OI-006 |
| AR-009 | agent recommendation | (폐기) 물리 저장 + 0~100 정규화 파생 | UD-010이 대체 | superseded | — |
| AR-010 | agent recommendation | 발열률과 냉각을 같은 단위(도/초)로 두고 출력→발열 계수 하나만 코드 상수로 | `발열 − 냉각`은 단위가 같아야 성립 | proposed | R-012, OI-013 |
| AR-011 | agent assumption | 온도는 엔진별 누적, 임계값만 공유 | "해당 온도는 모든 엔진 다 똑같음"은 임계값 공유로 읽히고, 엔진별 출력·냉각이 다르므로 엔진별 온도라야 의미가 있음. UD-015(혼합 허용)가 이를 강화 | proposed | R-013, OI-016 |
| AR-012 | agent recommendation | 과열은 엔진 하나 파괴가 아니라 발사 전체 실패 | GDD 08 §9는 주요 사고 하나만 허용 | proposed | R-013, OI-016 |
| OI-001 | unresolved item | 엔진 가격의 의미와 소비 시점 | — | resolved | UD-006 |
| OI-002 | unresolved item | SO 저장 단위 | — | resolved | UD-007 |
| OI-003 | unresolved item | 프리셋 출처 | — | resolved | UD-008, UD-009 |
| OI-004 | unresolved item | 프리셋 혼합 가능 여부 | — | resolved | UD-015 |
| OI-005 | unresolved item | GDD 06/07/08 갱신 범위 | 문서와 코드가 어긋난 채 남으면 이후 작업이 오도됨 | open | UD-010, RK-008 |
| OI-006 | unresolved item | 연소율 계수와 기준 프리셋 수치 | 측정된 밸런스 근거 없음 | open | R-007, R-011, AR-008 |
| OI-007 | unresolved item | SO 미연결 시 동작: 폴백 vs 거부 | 조용한 0추력은 디버깅 곤란 | open | R-017; 구현은 "경고 후 거부" 채택 |
| OI-008 | unresolved item | 0~100 공식 처리 | — | resolved | UD-010 |
| OI-009 | unresolved item | 냉각·점화 신뢰도의 물리 단위 | — | resolved | UD-011, UD-013 |
| OI-010 | unresolved item | 런타임 10슬롯 저장 매체: SO 에셋 vs `SaveLoad` JSON | 런타임 SO 쓰기는 에디터 데이터 오염 | open | UD-008, RK-007, AR-006 |
| OI-011 | unresolved item | 테스트 툴 형태 | 호스팅 어셈블리가 갈림 | open | R-009; 구현은 `Border.Editor` 메뉴 아이템 채택 |
| OI-012 | unresolved item | 재작성될 GDD 06 공식의 실제 내용 | 단위가 다르면 평균·감산 불가 | open | UD-010, R-010, R-016, RK-008 |
| OI-013 | unresolved item | 출력→발열 계수와 공통 임계 온도 | 밸런스 근거 없음 | open | R-012, R-013, AR-010 |
| OI-014 | unresolved item | 발열 기준 출력 | — | resolved | UD-016 |
| OI-015 | unresolved item | 냉각 회복과 OFF 구간 처리 | — | resolved | UD-017 |
| OI-016 | unresolved item | 과열이 엔진 하나인가 발사 전체인가 | GDD 08 §9는 주요 사고 하나 | open | R-013, AR-011, AR-012 |
| OI-017 | unresolved item | 추력이 로켓의 up 을 따르는가, 부품 각자의 up 을 따르는가 | GDD 07 §5 는 로켓 up 으로 고정하고 열 모델도 그 축의 적용 출력을 전제한다. 코드가 절반만 바뀐 상태라(SF-022) 배치와 힘 방향이 어긋날 수 있다 | open | R-007, SF-022, RK-011 |

## Implementation Record (rev 6)

UD-018 에 따라 반영했다. 아래는 전부 작성되었을 뿐 **컴파일도 테스트도 되지 않았다** — 에디터가 프로젝트
락을 잡고 있고 MCP 브리지가 끊겨 있어 어떤 검증도 돌리지 못했다(SF-023).

| Requirement | 위치 | 비고 |
| --- | --- | --- |
| R-001, R-003, R-004 | `Assets/01. Scripts/Simulation/EngineStatsSO.cs` | 5개 필드. 공유 상수 `CriticalTemperature = 300`, `HeatPerNewton = 0.05`, `FuelPerNewton = 20/1200`. 연소율과 발열은 출력에서 파생(AR-008)해 필드는 5개로 유지 |
| R-002 | `Assets/01. Scripts/Simulation/EnginePresetLibrarySO.cs` | `MaxSlots = 10`, `OnValidate` 에서 잘라냄(AR-001) |
| R-005, R-007, R-012, R-014, R-017 | `Assets/01. Scripts/Simulation/RocketPart.cs` | 하드코딩 필드를 SO 참조로 교체. `throttle` 이 실제 적용 출력을 만들고, `Prepare(rng)` 가 점화를 굴리고, `Tick(dt)` 가 연소와 온도(하한 0)를 갱신 |
| R-006, R-013, R-015 | `Assets/01. Scripts/Simulation/Rocket.cs` | 발사 시 시드 고정 점화 판정, 과열이면 즉시 발사 종료(AR-012, GDD 08 §9) |
| OI-007 | `Assets/01. Scripts/Simulation/RocketBuilder.cs` | SO 없는 부품은 집히지 않고 경고만 남긴다 |
| R-009 | `Assets/06. Packages/Editor/Simulation/EnginePresetTestFiller.cs` | `Tools/Engine Preset/Fill Test Presets` 가 프리셋 10개와 라이브러리를 쓴다. 기준 프리셋을 씬 엔진에 꽂는 메뉴는 씬을 건드리므로 분리 |
| R-011 | 같은 파일의 `Baseline` 프리셋 | 1200 N / 100 kg / 냉각 60 이 기존 하드코딩 동작을 재현한다. 발열 = 냉각이라 과열되지 않는다 |
| — | `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs` | 기존 테스트를 새 API로 옮기고 스로틀, 온도 상승→과열, OFF 시 냉각→하한, 점화 0%/100%, 슬롯 상한 추가 |

아직 열려 있는 항목에 채택한 엔지니어링 기본값 — 전부 기획 확인 대기로 open 유지:
OI-006(`FuelPerNewton` 을 1200 N → 20 kg/s 에서 역산), OI-007(경고 후 거부),
OI-011(`Border.Editor` 메뉴 아이템), OI-013(`HeatPerNewton = 0.05`, 임계 300 °C — 기준 프리셋이
정상 상태를 유지하도록 설정), OI-016(AR-012, 발사 전체 실패).

## Question Register

| ID | Decision needed | Related IDs | State | Revision | Resolution |
| --- | --- | --- | --- | --- | --- |
| Q-001 | 엔진 가격의 의미와 소비 시점 | OI-001, R-008 | answered | 1 | UD-006 |
| Q-002 | SO 저장 단위 | OI-002, R-007 | answered | 1 | UD-007 |
| Q-003 | 프리셋 10개의 출처 | OI-003, RK-001 | answered | 1 | UD-008, UD-009 |
| Q-004 | 프리셋 선택 단위 | OI-004, R-006 | answered (부분) | 2 | UD-014(개수만), 혼합은 Q-007로 이월 |
| Q-005 | 단위 전환 후 0~100 공식 처리 | OI-008, RK-006 | answered | 2 | UD-010 |
| Q-006 | 냉각·점화 신뢰도의 물리 단위 | OI-009, R-001 | answered | 2 | UD-011, UD-012, UD-013 |
| Q-007 | 서로 다른 프리셋 혼합 | OI-004, R-006 | answered | 3 | UD-015 (허용) |
| Q-008 | 발열 기준 출력 | OI-014, R-012 | answered | 3 | UD-016 (실제 적용 출력) |
| Q-009 | 냉각 > 발열일 때 온도 | OI-015, R-012 | answered | 3 | UD-017 (초과분 하강, 하한 0) |

## Corrections and Revision History

| Revision | Trigger | Change | Superseded IDs | Sections reconciled |
| --- | --- | --- | --- | --- |
| 1 | 최초 요청 + GDD 06/07/18, `Assets/01. Scripts/{Simulation,Research}` 조사 | 초기 가설 | none | 전 섹션 |
| 2 | Q-001/Q-002/Q-003 | 가격 표시 전용, 물리 단위, 10슬롯 + 테스트 툴 | AR-002 superseded; AR-004, OI-001~003 resolved | Snapshot, scope, flow, R-003/008~011, SF-016/017, AR-006~009, OI-008~011, RK-006/007, Q-004~006 |
| 3 | Q-004/Q-005/Q-006 + `08_로켓_발사.md` §8~9 | 0~100 폐기, 열 모델, 점화 %, 개수 무제한 | AR-007/AR-009 superseded; OI-008/009 resolved; RK-006 resolved | Snapshot, outcome, scope, 열 모델, flow, R-012~016, SF-018, AR-010~012, OI-012~016, RK-008~010, Q-007~009 |
| 4 | Q-007/Q-008/Q-009 | 혼합 허용, 실제 출력 기준 발열, 하한 0 하강 | OI-004/014/015 resolved; RK-010 resolved | Snapshot, 열 모델, flow, R-012/R-017, UD-015~017 |
| 5 | "로 하고 구현해줘" | 명시적 종료와 구현 승인, 영문 base + 한국어 미러로 최종화 | none | Document State, snapshot, ledger (UD-018, SF-019), finalization |
| 6 | 구현 반영 + "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" | 무엇을 만들었고 무엇이 미검증인지 기록, UD-019 에 따라 `Border.Simulation` → `Simulation` 개명, 절반만 적용된 `Attach` 회전 변경과 낡은 `CLAUDE.md` 어셈블리 표 기록 | none | Document State, Implementation Record(신설), ledger (UD-019, SF-020~026), OI-017, RK-011/012, checkpoint |

## Risks, Conflicts, and Dependencies

| ID | Kind | Item | Likelihood / impact | Response | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | conflict | GDD 06/07은 개발형 엔진 1개 모델, 10슬롯과 충돌 | 높음 / 높음 | UD-008 확정, 문서 범위는 OI-005 | SF-004, OI-005 | open |
| RK-002 | conflict | 설계 단계 가격 차감은 GDD 07 §3 위반 | — | UD-006으로 해소 | SF-003 | resolved |
| RK-003 | risk | 임의 물리 값은 프로토타입 발사 감각을 무너뜨림 | 중간 / 중간 | 기준 프리셋(R-011) 먼저 | SF-005, OI-006 | open |
| RK-004 | dependency | `07_로켓_설계.md` 미커밋 수정 | 낮음 / 낮음 | 사용자 변경분 보존 | SF-015 | open |
| RK-005 | risk | 프리셋이 유일 스탯 소스면 미니게임 보상이 갈 곳을 잃음 | 중간 / 중간 | UD-007/UD-010이 물리 단위로, 수치는 OI-012 | SF-012, SF-017 | open |
| RK-006 | conflict | 단위 전환이 0~100 공식 전체를 무효화 | — | UD-010으로 결정, 내용은 OI-012 | SF-009 | resolved |
| RK-007 | conflict | UD-008(런타임 슬롯)과 UD-004(SO 저작) 충돌 | 중간 / 높음 | AR-006 단계 분리 | UD-004, UD-008, OI-010 | open |
| RK-008 | risk | GDD 06 재작성이 §2.2, §5, §8, §9, §11, §12, §13, §15, §16, §20, §21, §23에 걸치고 밸런스 재측정 필요 | 높음 / 높음 | OI-012를 별도 기획 작업으로 분리 | UD-010, R-016 | open |
| RK-009 | dependency | 열 모델은 GDD 08 §8~9 `Overheat` 연출과 이어져야 함 | 중간 / 중간 | SF-018로 정합 확인 | SF-018, R-013 | open |
| RK-010 | risk | 최대 출력 기준 발열이면 힘 슬라이더가 무의미 | — | UD-016으로 해소 | OI-014 | resolved |
| RK-011 | conflict | `Rocket.Attach` 는 부품 자세를 그대로 두도록 바뀌고 주석은 추력이 부품 up 을 따른다고 하지만, `FixedUpdate` 는 로켓 up 을 쓰고 `RocketBuilder.Drag` 는 로켓 회전으로 스냅한다. 배치와 힘 방향이 어긋날 수 있고 GDD 07 §5 는 로켓 up 으로 고정한다 | 중간 / 높음 | OI-017 을 확정한 뒤 주석·`Attach`·`Drag`·`FixedUpdate` 를 일치시킨다 | SF-022, OI-017 | open |
| RK-012 | risk | `CLAUDE.md` 가 아직 모든 것이 `Border.*` 라고 적고 `Simulation` 어셈블리를 빠뜨려 UD-019 와 모순되고 이후 작업을 오도한다 | 중간 / 중간 | Assembly layout 표 갱신 — 이번 범위에서는 미승인 | UD-019, SF-026 | open |

## Open, Skipped, and Deferred Items

| ID | Item | State | Consequence | Recommendation | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-005 | GDD 06/07/08 갱신 범위 | open | 문서와 코드 분기 | OI-012 확정 후 일괄 갱신 | 사용자 | OI-012 해소 |
| OI-006 | 연소율 계수와 기준 수치 | open | 측정 근거 없음 | 1200 N / 100 / 20 재현을 기준으로 | 구현자 | 밸런스 작업 |
| OI-007 | SO 미연결 동작 | open | 조용한 0추력은 디버깅 곤란 | 경고 후 배치 거부 | 구현자 | — |
| OI-010 | 런타임 슬롯 저장 매체 | open | 런타임 SO 쓰기는 데이터 오염 | SO는 테스트용, 저장은 `SaveLoad`로 후속 | 사용자 | 런타임 슬롯 작업 |
| OI-011 | 테스트 툴 형태 | open | 호스팅 어셈블리 결정 | `Border.Editor` 메뉴 아이템 | 구현자 | — |
| OI-012 | 재작성될 GDD 06 공식 내용 | open | 단위가 다르면 평균·감산 불가 | 별도 기획 작업 | 사용자 | 이번 범위 이후 |
| OI-013 | 출력→발열 계수, 임계 온도 | open | 밸런스 근거 없음 | 기준 프리셋이 정상 연소를 마치도록 설정 | 구현자 | 밸런스 작업 |
| OI-016 | 과열 파급 범위 | open | GDD 08 §9는 주요 사고 하나 | 발사 전체 실패 (AR-012) | 사용자 | 발사 단계 작업 |

## Coverage and Consistency Check

| Area | State | Supporting IDs | Note |
| --- | --- | --- | --- |
| Outcome | covered | UD-001, UD-007, UD-010 | — |
| Users and stakeholders | covered | UD-004, UD-009 | — |
| Scope | covered | UD-005~018 | — |
| Non-goals | covered | SF-002, SF-013, OI-012 | — |
| Core flow | covered | UD-012, UD-014~017 | — |
| Constraints | covered | SF-003, SF-008, SF-013, SF-016, SF-018 | — |
| Success evidence | partial | R-001~R-017 | R-010, R-016은 OI-012 종속 |
| Risks and dependencies | covered | RK-001~010 | RK-008이 최대 미해소 위험 |
| Unresolved decisions | open | OI-005~007, OI-010~013, OI-016, OI-017 | 보존, 임의 해소하지 않음 |
| Handoff and authorization | covered | UD-018 | 구현 승인, 문서 재작성은 미승인 |

## Interview Checkpoint

- **Latest user message incorporated:** "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" — `Border.` 접두사 제거 (rev 6).
- **Latest sourced evidence incorporated:** SF-020~026 (어셈블리 구성과 개명, 어셈블리 참조 비전이성, 절반만 적용된 `Attach` 변경, 검증 불가, `ApplyPreset`, 낡은 `CLAUDE.md`).
- **Ledger transitions applied:** UD-019 추가, SF-020~026 추가, OI-017·RK-011·RK-012 개설.
- **Contradictory active items check:** passed. R-010, R-016은 OI-012로 `blocked` 유지. RK-011 은 임의 해소하지 않고 미해소 충돌로 기록.
- **Traceability check:** passed — R-001~R-017 전부 UD/SF/OI에 연결.
- **Verification status:** 컴파일·테스트 모두 미실행(SF-023). 성공 근거 항목 전부 `proposed`.
- **Resume point if planning reopens:** OI-017(추력 축, RK-011 을 막고 있음) → OI-012(공식 재작성) → OI-005(문서 범위) → OI-016(과열 범위) → OI-010(저장).

## Finalization and Handoff

- **Final interview state:** `explicitly-finished`
- **Authoritative English source:** `docs/specs/engine-preset-stats-spec.md`
- **Korean mirror:** `docs/specs/engine-preset-stats-spec.ko.md`
- **Synchronization check:** 두 파일의 안정 ID, 상태, 요구사항, 결정, 위험, 미해소 항목, 다음 승인 행동이 동일하다.
- **Remaining gaps:** OI-005, OI-006, OI-007, OI-010, OI-011, OI-012, OI-013, OI-016, OI-017; 위험 RK-001, RK-003, RK-004, RK-005, RK-007, RK-008, RK-009.
- **Assumptions still requiring confirmation:** AR-001, AR-003, AR-005, AR-006, AR-008, AR-010, AR-011, AR-012.
- **Next authorized action:** R-001~R-015, R-017 구현. 구현은 AR-001, AR-003, AR-008, AR-010, AR-011, AR-012와 OI-006/007/011/013의 권장안을 엔지니어링 기본값으로 채택하며, 해당 항목은 기획 확인을 위해 open으로 남는다.
- **Not authorized:** GDD 06/07/08 편집(R-016), 공식 재설계(OI-012), 런타임 슬롯 저장(OI-010), 커밋.

> 이 계획 승인만으로 커밋, 배포, 공개, 외부 시스템 변경이 승인되지는 않는다.
