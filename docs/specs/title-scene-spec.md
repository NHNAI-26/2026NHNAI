# 타이틀 씬 계획

> 하나의 살아있는 기획 문서다. 채팅이 아니라 이 문서가 현재 기획 상태다.

## Document State

| 항목 | 값 |
|---|---|
| 인터뷰 상태 | `active` — 모든 재료 결정 완료. **오프닝 연출까지 구현 완료.** 명시적 finish 신호 대기 |
| 작업 언어 | 한국어 (명시적 finish 시 영문 정본 + `.ko.md` 미러 생성) |
| Revision | 10 |
| 최종 갱신 | 2026-09-06 |
| 프로젝트 루트 | `C:\myGame\2026NHNAI` |
| 기준 경로 (최종 English) | `docs/specs/title-scene-spec.md` |
| 한국어 미러 경로 (최종) | `docs/specs/title-scene-spec.ko.md` |
| 명시적 finish 수신 | `no` |
| 다음 승인된 행동 | 사용자의 명시적 finish 신호 대기. rev 7 오프닝 연출은 별도 승인을 받아 구현했다. **커밋은 아직 승인되지 않음.** |

용어: **GDD** = `docs/artemis-2026-gdd/` 의 게임 기획서 18개 문서(Game Design Document). 커밋 `7a64175`로
저장소에 들어온 기존 문서다.

관련 문서: `docs/artemis-2026-gdd/11_UI_UX_화면설계.md`, `docs/artemis-2026-gdd/13_기술구조_Unity.md`,
`docs/artemis-2026-gdd/18_확정사항_및_변경금지선.md`, `docs/artemis-2026-gdd/02_전체_플레이_흐름.md`.

> **rev 3 대폭 축소**: 사용자가 UD-006으로 **세이브/로드를 전면 제외**하고 항상 새 게임으로만 시작하도록
> 결정했다. CONTINUE 관련 요구사항(R-004~R-006, R-010)은 `cancelled`, 저장 방식·저장 대상·유효성 판정
> 관련 미해결 항목과 리스크도 모두 `cancelled`다.
>
> 부수 효과로 GDD 18 §10 `저장·불러오기` 금지선(SF-006)과의 충돌이 **소멸**했다. GDD와 이 계획이 이제
> 같은 방향이다.
>
> **rev 4**: UD-009로 GDD 문서는 수정하지 않기로 했다(이 기획서가 변경분의 진실). OI-005 해소.
>
> **rev 5**: UD-010으로 버튼 컴포넌트를 순수 `Button` + `TMP_Text`로 확정. OI-008·RK-007 해소.
> **미해결 항목이 남아 있지 않다.** 인터뷰는 사용자의 명시적 finish 신호까지 `active`로 유지된다.
>
> **rev 6 — 구현 완료**: 사용자가 "구현해줘"로 별도 승인했다. §17에 산출물과 검증 결과를 기록했다.
> R-001 ~ R-003, R-007 ~ R-009, R-011, R-012 검증 통과. 커밋은 아직 하지 않았다.
>
> **rev 10**: UD-018 로 타이틀 텍스트 묶음이 화면에 비스듬히 기울어 보인다. 캔버스를
> Screen Space - Camera 로 바꾸고 새 자식 `TitleGroup` 만 Y축으로 돌렸다. UD-019 로 설정창은 제외 —
> 회전은 자식에게 상속되지 않으므로 캔버스 루트 직속인 설정창은 정면으로 남는다.
>
> **rev 9**: UD-016 으로 글자 단위 떨림이 **호버 중에만** 돈다. 컴포넌트가 라벨에서 버튼으로 옮겨졌고
> (라벨은 레이캐스트 대상이 아니라 호버를 못 받는다) 제목 텍스트의 떨림은 사라졌다 — 호버할 수 없는 대상이라서다.
> UD-017 로 엔진 파티클의 타이틀 전용 과장을 걷어내고 `RocketEngine.prefab` 저작값으로 되돌렸다.
>
> **rev 8**: UD-013 으로 배경을 `SimulationTest` 와 동일하게 맞췄다(`SkyEnvironment` + 수면 + 안개·앰비언트).
> UD-014 로 텍스트 떨림이 트랜스폼 단위에서 **글자 단위**로 바뀌었다. 카메라 포즈는 사용자가 직접 잡는다 —
> 에이전트가 건드리지 않는다.
>
> **rev 7 — 오프닝 연출**: UD-011로 타이틀이 "텍스트만"에서 벗어났다. 좌측에 3D 로켓이 서고, 카메라가
> 미세하게 흔들려 발사 중처럼 보이며, 우측 텍스트가 떨린다. 종료 버튼이 들어왔다(AR-002 뒤집힘).
> UD-012로 설정 버튼은 남는다 — 해상도·볼륨에 닿는 유일한 경로이기 때문이다.

---

## 1. 목표 (Outcome)

게임 실행 직후 플레이어가 보는 타이틀 화면을 만든다. rev 7부터 텍스트 전용이 아니라 **발사 직전이 읽히는
오프닝 화면**이다: 좌측에 로켓, 우측에 메뉴.

- 화면 좌측: 3D 로켓이 비스듬한 앞각도(코가 기울어 보이는 3/4 뷰)로 서서 엔진 이펙트를 뿜는다
- 화면 우측: 게임 제목, `게임 시작`, `설정`, `게임 종료`
- 카메라가 계속 미세하게 흔들려 로켓이 위로 밀려 올라가는 인상을 준다
- 텍스트는 은은하게 떨린다

저장·불러오기는 만들지 않는다. 게임 시작 버튼은 `ResearchFlowSession.PrepareNewGame()`을 호출한 뒤 메인 씬을 연다. 타이틀로 돌아올 때 남아 있던 세션도 초기화하며, 세션이 없다면 타이틀에서 만들지 않는다. 실행할 때마다 항상 새 게임이다(UD-006).

배경 아트, 옵션 메뉴, 종료 버튼, 크레딧, 연출, CONTINUE는 범위 밖이다.

## 2. 현재 상태 스냅샷 (검증된 사실)

| ID | 사실 | 출처 |
|---|---|---|
| SF-001 | 빌드 세팅에 등록된 씬은 `Assets/00. Scenes/SampleScene.unity` **단 하나**다. `SimulationTest.unity`는 존재하지만 미등록·미추적(untracked) 상태다. | `ProjectSettings/EditorBuildSettings.asset` `m_Scenes`, `git status` |
| SF-002 | `Assets/01. Scripts/` 전체에 `SceneManager` / `LoadScene` 사용처가 **0건**이다. 씬 전환 인프라가 아직 없다. | `grep -rn "SceneManager\|LoadScene" "Assets/01. Scripts/"` |
| SF-003 | 저장 시스템(`SaveLoadSystem`, `FileManager`)이 프로젝트에 이미 존재한다. 이번 범위에서는 **사용하지 않는다**(UD-006). | `Assets/01. Scripts/SaveLoad/SaveLoadSystem.cs` |
| SF-004 | `Save` 클래스는 필드가 하나도 없는 빈 클래스다. `SetNewGameData()`는 빈 문자열을 쓴 뒤 저장하므로 새 게임 시작 시점부터 세이브 파일이 항상 존재하게 된다. — UD-006으로 이번 범위와 무관해짐 | `Assets/01. Scripts/SaveLoad/Save.cs:6`, `SaveLoadSystem.cs:44` |
| SF-005 | GDD 13 §11: "게임잼 버전은 중간 저장을 제공하지 않는다… 영구 저장 시스템은 만들지 않는다." — UD-006과 **일치** | `13_기술구조_Unity.md:407-417` |
| SF-006 | GDD 18 §10 "제작 중 추가 금지" 목록에 `저장·불러오기` 포함. GDD 01 범위 제외 목록에도 `중간 저장과 불러오기`. — UD-006과 **일치** | `18_확정사항_및_변경금지선.md:124`, `01_게임_비전_및_범위.md:111` |
| SF-007 | GDD 11 §1: 필수 화면은 6개이며 **타이틀 화면은 목록에 없다**. GDD 13 §1 권장 씬 목록(`00_Boot`, `01_Main`, `02_Sim_Engine` …)에도 타이틀 씬이 없다. | `11_UI_UX_화면설계.md:5-18`, `13_기술구조_Unity.md:5-20` |
| SF-008 | GDD 02 §2: 프롤로그는 20~30초이며 "첫 실행에만 자동 재생하되, 클릭으로 건너뛸 수 있다". | `02_전체_플레이_흐름.md:31-45` |
| SF-009 | 재사용 가능한 버튼 컴포넌트 `Border.UI.UIGenericButton`이 있다. `Clicked`(UnityAction) 노출, `SetButton(localizationKey)` 제공. | `Assets/01. Scripts/UI/UIGenericButton.cs` |
| SF-010 | **함정**: `UIGenericButton.Awake()`는 자식 TMP_Text에 `UILocalizeText`를 자동 부착한다. `UILocalizeText`는 `OnEnable`에서 키가 비어 있으면 본문을 빈 문자열로 덮어쓴다. 즉 UIGenericButton을 쓰면서 로컬라이즈 키를 비워두면 **버튼 라벨이 빈칸이 된다**. (`LocalizationManager`가 없으면 `Lookup`은 키를 그대로 반환하므로, 키를 `"NEW GAME"`으로 두면 그대로 표시된다.) | `UIGenericButton.cs:71-91`, `UILocalizeText.cs:96-101,110-113` |
| SF-011 | 로컬라이즈 테이블 에셋은 아직 없다. `Assets/02. ScriptableObjects/`에는 `Audio/SoundDatabase.asset`만 있다. | `ls -R "Assets/02. ScriptableObjects/"` |
| SF-012 | 프리팹은 `Assets/03. Prefabs/Systems/SoundManager.prefab` 하나뿐이다. CLAUDE.md는 "씬보다 프리팹 우선"을 규칙으로 둔다. | `ls -R "Assets/03. Prefabs/"`, `CLAUDE.md` |
| SF-013 | GDD 13 §2에 `GameState` / `StageState`가 `[Serializable]`로 정의되어 있다. — UD-006으로 이번 범위와 무관해짐 | `13_기술구조_Unity.md:55-100` |
| SF-014 | `Assets/01. Scripts/SaveLoad`는 CLAUDE.md 기준 **업스트림 패키지와 byte-identical한 벤더링 사본**이다. 삭제·수정하면 로컬 포크가 된다. | `CLAUDE.md` "Assets/01. Scripts is a vendored copy" |

## 3. 사용자·이해관계자

| 주체 | 필요 / 관심사 | 출처 ID | 상태 |
|---|---|---|---|
| 플레이어 | 실행 즉시 무엇을 눌러야 하는지 안다. | UD-001 | active |
| 개발자(본인) | 게임잼 일정 안에서 최소 비용으로 넣는다. | UD-006 | active |
| GDD 문서 | 저장·불러오기를 금지한다. UD-006과 일치하므로 충돌 없음. | SF-005, SF-006 | active |

## 4. 범위

### 포함

| 항목 | 출처 ID | 상태 | 비고 |
|---|---|---|---|
| 타이틀 화면의 제목, `게임 시작`, `설정` 버튼 | UD-001, UD-006, 2026-09-05 사용자 수정 | active | 두 버튼은 같은 크기이며 TitleScreen 프리팹에 저장 |
| 타이틀을 **새 씬**으로 만들고 UI는 **프리팹**으로 구성 | UD-005 | active | 씬은 신규 생성이라 병합 충돌 없음 |
| 타이틀 씬을 빌드 세팅 인덱스 0으로 등록 | UD-005 | active | 현재 인덱스 0은 SampleScene (SF-001) |
| `01_Main` 씬 신설 및 빌드 세팅 등록 | UD-004 | active | GDD 13 §1 권장 이름. 내용은 비어 있어도 됨 |
| `NEW GAME` 동작: 연구 상태 초기화 후 `01_Main`으로 씬 전환 | UD-001, UD-004, UD-006 | active | 지속 세션의 이전 진행을 지우고 연결된 밸런스 SO를 다시 읽음 |
| 좌측 3D 로켓 + 엔진 이펙트 | UD-011 | active | 기존 아트 프리팹 정적 조립. `RocketBuilder`·설계 데이터는 쓰지 않는다 |
| 배경·하늘을 `SimulationTest` 와 동일하게 | UD-013 | active | `SkyEnvironment` 프리팹 + 수면(`MAT_water`) + 안개·앰비언트·태양 값 이식 |
| 제목·메뉴를 원근으로 기울여 배치 | UD-018 | active | Screen Space - Camera + `TitleGroup` 자식 회전. 설정창은 제외(UD-019) |
| 카메라 미세 흔들림, 호버 시 글자 단위 텍스트 떨림 | UD-011, UD-014, UD-016 | active | 같은 `TitleJitter` 를 쓰되 자식에 `TMP_Text` 가 있으면 호버 중에만 글자별 메시 정점을, 없으면 트랜스폼을 계속 흔든다 |
| `게임 종료` 버튼 | UD-011 | active | `PauseMenuController.QuitGame` 과 같은 관용구 |

### 제외 (비목표)

| 제외 항목 | 출처 ID | 상태 | 이유 |
|---|---|---|---|
| **CONTINUE 버튼, 세이브, 로드** | UD-006 | active | "세이브 로드는 없고 그냥 항상 새게임으로". GDD 18 §10과도 일치 |
| 기존 `SaveLoadSystem` 호출 | UD-006, SF-014 | active | 코드는 그대로 두되 이번 화면에서 쓰지 않는다 (AR-008) |
| 배경 아트, 로고 이미지 | UD-001 | active | 배경은 여전히 단색이다. 화면을 채우는 것은 로켓 하나 |
| 인트로 연출 | UD-001 | **cancelled** (UD-011, rev 7) | rev 7에서 오프닝 연출이 범위에 들어왔다 |
| 프롤로그 재생 | UD-004 | active | Q-002에서 프롤로그 선택지를 고르지 않음. 별도 작업으로 구현됨 — `docs/specs/prologue-spec.md` |
| 별도 설정 전용 씬 | SF-007 | active | 설정은 타이틀에 연결한 SettingsMenu 프리팹으로 제공. 해상도와 전체/BGM/SFX 볼륨 조절, 닫기 버튼 포함 |
| 크레딧 | AR-002 | active | 요청에 없음 |
| ~~종료(QUIT) 버튼~~ | AR-002 | **cancelled** (UD-011, rev 7) | `게임 종료` 버튼이 범위에 들어왔다 |
| 로컬라이즈 테이블 구축 | SF-011 | active | 지금은 영문 리터럴로 충분 |
| `01_Main`의 실제 운영 화면 UI 구현 | UD-004 | active | 이번엔 전환 대상으로서의 빈 씬만 |

## 5. 핵심 흐름

### 주 흐름

1. 게임 실행. 타이틀 씬(빌드 인덱스 0)이 로드된다.
2. 제목과 `게임 시작`, `설정`이 표시된다. 설정 버튼은 미리 배치된 설정창 프리팹을 연다. 해상도는 모니터 목록과 1280x720, 1600x900, 1920x1080, 2560x1440 중 모니터 크기 이하의 선택지를 합쳐 제공하며 같은 크기는 중복하지 않는다.
3. `NEW GAME` 클릭 → `01_Main` 로드.

### 대안·예외 흐름

| 조건 | 기대 동작 | 관련 ID | 상태 |
|---|---|---|---|
| 버튼 연타 | 씬 전환 1회만 실행. GDD 13 §10이 "버튼 연타"를 방지 대상으로 명시. | R-007 | active |

### 상태·데이터 메모

저장 상태가 없다(UD-006). 타이틀 화면은 어떤 영속 데이터도 읽거나 쓰지 않는다. 씬 간 상태 보존이
나중에 필요해지면 GDD 13 §11이 권장하는 `DontDestroyOnLoad GameSession` 방식이 후보지만, 이번 범위 밖이다.

## 6. 요구사항

| ID | 요구사항 | 유형 | 출처 ID | 우선도 | 상태 | 성공 근거 |
|---|---|---|---|---|---|---|
| R-001 | 게임 실행 시 타이틀 화면이 첫 화면으로 표시된다 | functional | UD-001, UD-005 | must | active | Play Mode 진입 시 타이틀 표시, Game View 스크린샷 |
| R-002 | 화면에 게임 제목과 `NEW GAME` 텍스트가 표시된다 | functional | UD-001 | must | active | Game View 스크린샷. 라벨이 빈칸이 아님 (SF-010 함정 확인) |
| R-003 | `NEW GAME` 클릭 시 `01_Main`으로 전환된다 | functional | UD-001, UD-004 | must | active | 클릭 후 `01_Main` 로드 확인 |
| R-004 | ~~이어할 저장 데이터가 있을 때만 `CONTINUE`가 표시된다~~ | functional | UD-002 | — | **cancelled** (UD-006, rev 3) | — |
| R-005 | ~~`CONTINUE` 클릭 시 저장 상태로 진입한다~~ | functional | UD-002 | — | **cancelled** (UD-006, rev 3) | — |
| R-006 | ~~파손 세이브 시 `CONTINUE` 미노출~~ | quality | SF-004 | — | **cancelled** (UD-006, rev 3) | — |
| R-007 | 버튼 연타로 씬 전환이 중복 실행되지 않는다 | quality | SF-002 | should | active | 연타 테스트, Console 확인 |
| R-008 | 타이틀 화면 UI는 프리팹으로 구성해 씬 diff를 최소화한다 | operational | UD-005, SF-012 | should | active | 씬 파일 diff가 프리팹 인스턴스 1개 수준 |
| R-009 | `01_Main` 씬이 존재하고 빌드 세팅에 등록되어 있다 | functional | UD-004 | must | active | `EditorBuildSettings.asset`에 항목 추가 확인 |
| R-010 | ~~저장 데이터는 재실행해도 유지된다~~ | functional | UD-003 | — | **cancelled** (UD-006, rev 3) | — |
| R-011 | 타이틀 화면은 어떤 세이브 파일도 읽거나 쓰지 않는다 | quality | UD-006 | must | active | 코드 리뷰: `SaveLoadSystem` / `FileManager` 참조 0건 |
| R-012 | `NEW GAME` 버튼은 Unity `Button` + `TMP_Text`로 구성하며 `UIGenericButton` / `UILocalizeText`를 부착하지 않는다 | operational | UD-010 | must | active | 프리팹 컴포넌트 확인. SF-010 빈 라벨 재현 안 됨 |
| R-013 | 타이틀 좌측에 로켓이 비스듬한 앞각도로 보이고 엔진 이펙트가 재생된다 | functional | UD-011 | must | active | Game View 스크린샷 |
| R-014 | 카메라가 미세하게 흔들려 발사 중 인상을 준다 | functional | UD-011 | should | active | Play Mode 육안 확인 |
| R-015 | 메뉴 버튼에 마우스를 올리면 그 라벨의 **글자 하나하나**가 은은하게 떨린다. 올리지 않은 버튼과 제목은 떨지 않는다 | functional | UD-011, UD-014, UD-016 | should | active | Play Mode 육안 확인 |
| R-018 | 엔진 파티클은 `RocketEngine.prefab` 저작값을 그대로 쓴다. 타이틀 쪽 차이는 `playOnAwake` 뿐이다 | operational | UD-017 | must | active | 프리팹 값 비교 |
| R-019 | 제목과 메뉴 세 줄이 카메라 시점에서 비스듬한 평면으로 보인다 | functional | UD-018 | must | active | Game View 스크린샷 |
| R-020 | 설정창은 기울지 않고 화면 정면에 뜬다 | functional | UD-019 | must | active | 설정창을 연 Game View 스크린샷 |
| R-017 | 타이틀 배경이 `SimulationTest` 와 같은 하늘·수면·안개로 보인다 | functional | UD-013 | must | active | Play Mode 육안 확인 (`SkyEnvironment` 는 런타임 전용이라 에디트 모드에서는 기본 스카이박스가 보인다) |
| R-016 | `게임 종료` 클릭 시 애플리케이션이 종료된다(에디터에서는 플레이 모드 종료) | functional | UD-011 | must | active | Play Mode 육안 확인 |

## 7. 제약

| 분류 | 제약 | 출처 ID | 영향 | 상태 |
|---|---|---|---|---|
| policy | GDD 18 §10 / 13 §11 / 01이 저장·불러오기를 금지 | SF-005, SF-006 | UD-006과 일치. 충돌 없음 | active |
| policy | GDD 11 §1의 필수 화면 6개에 타이틀 없음 | SF-007 | UD-009로 GDD는 수정하지 않음. 이 기획서가 변경분의 진실 | **resolved** |
| technical | 씬 전환 코드가 프로젝트에 전무 | SF-002 | 최소한의 씬 로더를 새로 만들어야 함 | active |
| technical | `UIGenericButton` 사용 시 로컬라이즈 키를 반드시 채워야 라벨이 보임 | SF-010 | UD-010으로 해당 컴포넌트를 쓰지 않아 회피 | **resolved** |
| technical | `SaveLoad`는 업스트림 벤더링 사본 | SF-014 | 삭제·수정하면 로컬 포크. 손대지 않는다 | active |
| schedule | 게임잼(48시간) 일정 | `15_48시간_제작계획.md` | 최소 구현 우선 | active |

## 8. 성공 근거

| 관련 요구사항 | 근거 / 수용 조건 | 검증 방법 | 담당 | 상태 |
|---|---|---|---|---|
| R-001, R-002 | Play Mode 진입 시 타이틀이 표시되고 라벨이 읽힌다 | Game View 스크린샷 (CLAUDE.md: UI 변경은 시각 검증 필수) | 개발자 | proposed |
| R-003, R-009 | `NEW GAME` 클릭 후 `01_Main`으로 전환된다 | Play Mode 수동 확인 | 개발자 | proposed |
| R-007 | 연타 시 Console 오류·중복 로드 없음 | Play Mode 수동 확인 | 개발자 | proposed |
| R-008 | 씬 파일 diff가 프리팹 인스턴스 수준 | `git diff` 확인 | 개발자 | proposed |
| R-011 | 타이틀 관련 스크립트에 저장 API 참조 없음 | 코드 리뷰 / grep | 개발자 | proposed |

## 9. 결정·근거 원장 (Ledger)

| ID | 종류 | 내용 | 근거 / 출처 | 상태 | 결과 / 연결 ID |
|---|---|---|---|---|---|
| UD-001 | 사용자 결정 | "타이틀 씬을 만들고 싶어. 일단 텍스트로 NEW GAME" | 사용자 메시지 (rev 1) | active | R-001~R-003 |
| UD-002 | 사용자 결정 | "이어할 세이브파일이 잇으면 CONTINUE 정도만 잇으면 될거 같아" | 사용자 메시지 (rev 1) | **corrected** (UD-006, rev 3) | R-004~R-006 cancelled |
| UD-003 | 사용자 결정 | Q-001 답변 "세이브 할 수 있게 해줘" — 디스크 영구 저장 허용 | 사용자 메시지 (rev 2) | **corrected** (UD-006, rev 3) | R-010 cancelled |
| UD-004 | 사용자 결정 | Q-002 답변 "01_Main 신설" — NEW GAME은 새 `01_Main` 씬으로 전환 | 사용자 메시지 (rev 2) | active | R-003, R-009 |
| UD-005 | 사용자 결정 | Q-003 답변 "새 씬 + 프리팹 UI" — 타이틀은 새 씬, UI는 프리팹, 빌드 인덱스 0 | 사용자 메시지 (rev 2) | active | R-001, R-008 |
| UD-006 | 사용자 결정 | "세이브 로드는 없고 그냥 항상 새게임으로 할 수 있게 해줘" — **저장·불러오기 전면 제외.** UD-002·UD-003·UD-007·UD-008을 대체한다 | 사용자 메시지 (rev 3) | active | R-004~R-006/R-010 cancelled, R-011 신설, OI-002/003/004/007 cancelled, RK-002/003/006 cancelled |
| UD-007 | 사용자 결정 | Q-005 답변 "GDD 13 §2 GameState 통째로" — 저장 대상 지정 | 사용자 메시지 (rev 3, UD-006 직전) | **superseded** (UD-006) | 저장 자체가 제외됨 |
| UD-008 | 사용자 결정 | Q-006 답변 "저장 데이터 버전 필드" — 세이브 유효성 판정 방식 | 사용자 메시지 (rev 3, UD-006 직전) | **superseded** (UD-006) | 저장 자체가 제외됨 |
| UD-009 | 사용자 결정 | Q-007 답변 "이 기획서만으로 충분" — **GDD는 초기 기획 스냅샷으로 두고 수정하지 않는다.** 이후 변경은 `docs/specs/` 기획서가 진실 | 사용자 메시지 (rev 4) | active | OI-005 resolved, GDD 11/13 미수정 |
| UD-018 | 사용자 결정 | "타이틀 캔버스를 기울여서 보이게 배치하고 싶은데 카메라에 보이게 배치를 잘 못하겠어" (스케치 첨부) | 사용자 메시지 (rev 10) | active | R-019 |
| UD-019 | 사용자 결정 | "설정창 빼고 해줘" — 기울임 대상에서 설정창 제외 | 사용자 메시지 (rev 10) | active | R-020 |
| UD-016 | 사용자 결정 | "글자 하나하나 떨리는거는 호버 했을 때 되도록 해줘" | 사용자 메시지 (rev 9) | active | R-015 |
| UD-017 | 사용자 결정 | "로켓 이펙트 engine_01이나, rocket프리팹에 적용되어있는 거 그대로 써줘" — 타이틀 전용 파티클 과장 폐기 | 사용자 메시지 (rev 9) | active | R-018 |
| UD-013 | 사용자 결정 | "배경을 simulater test 에 있는 배경 그대로 가져와줘… 배경이랑 하늘 같은거 그대로" | 사용자 메시지 (rev 8) | active | R-017 |
| UD-014 | 사용자 결정 | "텍스트 떨림을 텍스트 글자 하나하나가 떨리는 느낌으로" | 사용자 메시지 (rev 8) | active | R-015 |
| UD-015 | 사용자 결정 | "카메라 바뀐거 돌리지마 내가 일부러 잡은거야" — 타이틀 카메라 포즈는 사용자가 직접 잡는다. 에이전트는 되돌리지 않는다 | 사용자 메시지 (rev 8) | active | 카메라 포즈 고정 |
| UD-011 | 사용자 결정 | "오프닝 연출을 만들려고 해… 좌측에 로켓, 우측에 게임 시작·게임 종료. 로켓이 위쪽으로 발사되고 있는 것처럼 카메라를 좀 흔들어줘. 글자가 은은하게 부들부들 떨리는 효과" — 로켓은 기존 프리팹 정적 배치 | 사용자 메시지 (rev 7) | active | R-013~R-016, AR-002 뒤집힘 |
| UD-012 | 사용자 결정 | "아 그러면 설정도 넣어줘 게임시작 게임종료 설정" — 설정 버튼 유지. 제거 시 `MenuPrefabTests` 2건이 깨지고 해상도·볼륨 설정이 게임에서 닿을 수 없게 된다는 점을 확인한 뒤의 결정 | 사용자 메시지 (rev 7) | active | 메뉴 3개 확정 |
| UD-010 | 사용자 결정 | Q-008 답변 "순수 Button + TMP_Text" — `NEW GAME` 버튼은 Unity 기본 `Button`과 `TMP_Text`로 만든다. `UIGenericButton` / `UILocalizeText`는 쓰지 않는다 | 사용자 메시지 (rev 5) | active | AR-005 accepted, OI-008 resolved, RK-007 resolved, R-002, R-012 |
| SF-001 | 검증된 사실 | 빌드 세팅 씬은 SampleScene 하나 | `EditorBuildSettings.asset` | active | R-009 |
| SF-002 | 검증된 사실 | 씬 전환 코드 0건 | grep 결과 | active | 제약, R-007 |
| SF-003 | 검증된 사실 | SaveLoadSystem/FileManager 존재 | `SaveLoadSystem.cs` | active | AR-008, R-011 |
| SF-004 | 검증된 사실 | `Save`는 빈 클래스, `SetNewGameData()`가 빈 파일 생성 | `Save.cs:6`, `SaveLoadSystem.cs:44` | active (범위 무관) | — |
| SF-005 | 검증된 사실 | GDD 13 §11 "영구 저장 시스템은 만들지 않는다" | `13_기술구조_Unity.md:417` | active | UD-006과 일치 |
| SF-006 | 검증된 사실 | GDD 18 §10 금지 목록에 `저장·불러오기` | `18_확정사항_및_변경금지선.md:124` | active | UD-006과 일치 |
| SF-007 | 검증된 사실 | GDD 11/13에 타이틀 화면·씬 없음 | `11_UI_UX_화면설계.md:5-18` | active | OI-005, Q-007 |
| SF-008 | 검증된 사실 | 프롤로그는 첫 실행에만 자동 재생, 스킵 가능 | `02_전체_플레이_흐름.md:45` | active | 범위 제외 |
| SF-009 | 검증된 사실 | `UIGenericButton` 재사용 가능 | `UIGenericButton.cs` | active | AR-005 |
| SF-010 | 검증된 사실 | UIGenericButton + 빈 로컬라이즈 키 = 빈 라벨 | `UILocalizeText.cs:96-101` | active | R-002 |
| SF-011 | 검증된 사실 | 로컬라이즈 테이블 에셋 없음 | 디렉터리 목록 | active | 범위 제외 |
| SF-012 | 검증된 사실 | 프리팹 우선 규칙, 기존 프리팹 1개 | `CLAUDE.md`, 디렉터리 목록 | active | R-008 |
| SF-013 | 검증된 사실 | GDD 13 §2 `GameState` / `StageState` 구조 | `13_기술구조_Unity.md:55-100` | active (범위 무관) | — |
| SF-014 | 검증된 사실 | `SaveLoad`는 업스트림 벤더링 사본. 손대면 로컬 포크 | `CLAUDE.md` | active | AR-008 |
| AR-001 | 권고 | 타이틀 씬 신설 + 빌드 인덱스 0 | SF-001, SF-012 | **accepted** (UD-005) | R-001 |
| AR-002 | 권고 | QUIT 버튼은 넣지 않는다 | UD-001 | **rejected** (UD-011, rev 7) | `게임 종료` 버튼 구현됨 |
| AR-003 | 권고 | 세이브 슬롯 1개 고정 | UD-002 | **cancelled** (UD-006) | — |
| AR-004 | 권고 | 세션 복원만으로 CONTINUE 구성 | SF-005, SF-006 | **cancelled** (UD-006) | — |
| AR-005 | 권고 | `UIGenericButton` 대신 순수 `Button` + `TMP_Text`를 쓴다 | SF-010, SF-011 | **accepted** (UD-010) | R-002, R-012 |
| AR-006 | 권고 | `GameState` 통째 저장 | SF-013 | **cancelled** (UD-006) | — |
| AR-007 | 권고 | 버전 필드로 세이브 유효성 판정 | SF-004 | **cancelled** (UD-006) | — |
| AR-008 | 권고 | 기존 `SaveLoad` 코드는 **삭제하지 않고 그대로 둔다**. 업스트림 벤더링 사본이라(SF-014) 지우면 로컬 포크가 되고, 안 쓰면 그만이다 | SF-014, UD-006 | proposed | R-011 |
| OI-001 | 미해결 | CONTINUE vs GDD 금지선 충돌 | — | **resolved** (UD-006으로 소멸, rev 3) | — |
| OI-002 | 미해결 | 세이브 유효성 판정 기준 | — | **cancelled** (UD-006) | — |
| OI-003 | 미해결 | NEW GAME 덮어쓰기 확인 | — | **cancelled** (UD-006) | — |
| OI-004 | 미해결 | 저장 대상 필드 | — | **cancelled** (UD-006) | — |
| OI-005 | 미해결 | 타이틀 화면이 GDD 11 필수 화면 목록·GDD 13 씬 목록에 없다 | SF-007 | **resolved** (UD-009, rev 4) | GDD 미수정. 이 기획서가 변경분 기록 |
| OI-006 | 미해결 | NEW GAME 대상 씬 | — | **resolved** (UD-004, rev 2) | R-003, R-009 |
| OI-007 | 미해결 | GDD 저장 금지 서술 정리 | — | **cancelled** (UD-006으로 불일치 소멸) | — |

## 10. 질문 등록부

| ID | 필요한 결정 | 왜 중요한가 | 관련 ID | 상태 | 갱신 revision | 해소 |
|---|---|---|---|---|---|---|
| Q-001 | CONTINUE 성립 방식 | R-004~R-006 차단 | OI-001 | answered → **superseded** | 3 | UD-003 → UD-006이 대체 |
| Q-002 | NEW GAME 전환 대상 | 전환 대상이 실재하지 않았음 | OI-006 | **answered** | 2 | UD-004 (`01_Main` 신설) |
| Q-003 | 타이틀 화면 구성 형태 | 작업량·씬 diff 크기 | AR-001, R-008 | **answered** | 2 | UD-005 (새 씬 + 프리팹 UI) |
| Q-004 | GDD 저장 금지 서술 + 타이틀 누락 정리 | 문서-구현 불일치 | OI-005, OI-007 | **cancelled** | 3 | 저장 부분은 UD-006으로 소멸. 타이틀 부분은 Q-007로 재등록 |
| Q-005 | `Save`에 무엇을 넣을 것인가 | CONTINUE 복원 내용 | OI-004 | answered → **superseded** | 3 | UD-007 → UD-006이 대체 |
| Q-006 | 세이브 유효성 판정 기준 | CONTINUE 오노출 | OI-002 | answered → **superseded** | 3 | UD-008 → UD-006이 대체 |
| Q-007 | 타이틀 화면을 GDD 11 §1 화면 목록과 13 §1 씬 목록에 반영할 것인가 | GDD는 "필수 화면 6개"라고 못박고 타이틀을 넣지 않았다 | OI-005, SF-007 | **answered** | 4 | UD-009 (GDD 미수정, 이 기획서가 진실) |
| Q-008 | `NEW GAME` 버튼을 어떤 컴포넌트로 만들 것인가 | 잘못 고르면 버튼이 런타임에 빈칸으로 보인다(SF-010) | AR-005, RK-007, R-002, SF-009, SF-010 | **answered** | 5 | UD-010 (순수 `Button` + `TMP_Text`) |

## 11. 정정·개정 이력

| Revision | 트리거 | 변경 | 정정/대체된 ID | 반영된 하위 섹션 |
|---|---|---|---|---|
| 1 | 최초 요청 + 코드·GDD 조사 | 최초 기획 가설. SF-001~SF-012 수집. UD-001/UD-002 기록. RK-001 충돌로 R-004~R-006 blocked | 없음 | 전 섹션 |
| 2 | Q-001~Q-003 답변 | UD-003(디스크 저장 허용), UD-004(`01_Main`), UD-005(새 씬 + 프리팹). RK-001·OI-001·OI-006 해소. R-009/R-010 신설. SF-013 조사. OI-007·RK-006 신설 | SF-005/006 superseded, AR-001 accepted, AR-004 superseded | 전 섹션 |
| 3 | 사용자 정정: "세이브 로드는 없고 그냥 항상 새게임으로 할 수 있게 해줘" | UD-006 신규(저장·불러오기 전면 제외). UD-002·UD-003·UD-007·UD-008 대체. R-004~R-006·R-010 cancelled, R-011 신설. OI-002/003/004/007·RK-002/003/006·AR-003/004/006/007 cancelled. SF-005/SF-006을 active로 복귀(이제 계획과 일치). SF-014 추가 조사. Q-004 cancelled, Q-007 신규 | UD-002, UD-003 → corrected / UD-007, UD-008 → superseded / SF-005, SF-006 → active 복귀 | 목표, 스냅샷, 이해관계자, 범위, 흐름, 요구사항, 제약, 성공 근거, 원장, 질문, 리스크, 미해결, 커버리지, 체크포인트 |
| 4 | Q-007 답변 "이 기획서만으로 충분" | UD-009 신규(GDD 미수정, `docs/specs/` 기획서가 변경분의 진실). OI-005 resolved. GDD 11 §1 제약 resolved. Q-008 신규(버튼 컴포넌트 선택, RK-007 대응) | OI-005 → resolved / Q-007 → answered | 배너, Document State, 제약, 원장, 질문, 미해결, 리스크, 커버리지, 체크포인트 |
| 10 | 사용자 요청: 캔버스 기울이기(설정창 제외) | UD-018·UD-019 신규. R-019·R-020 신설. §17 rev 10 산출물 추가 | 없음 | 배너, Document State, 범위, 요구사항, 원장, 개정 이력, 산출물 |
| 9 | 사용자 요청: 호버 시 떨림 + 파티클 원복 | UD-016·UD-017 신규. R-018 신설, R-015 개정. §17 rev 9 산출물 추가 | R-015 개정 | 배너, Document State, 범위, 요구사항, 원장, 개정 이력, 산출물 |
| 8 | 사용자 요청: 배경 이식 + 글자 단위 떨림 | UD-013·UD-014·UD-015 신규. R-017 신설, R-015 개정. §17 rev 8 산출물 추가 | R-015 개정 | 배너, Document State, 범위, 요구사항, 원장, 개정 이력, 산출물 |
| 7 | 사용자 요청: 오프닝 연출 | UD-011·UD-012 신규. R-013~R-016 신설. AR-002 rejected. "인트로 연출" 제외 항목 cancelled. §17에 rev 7 산출물 추가 | AR-002 → rejected | 배너, Document State, 목표, 범위, 요구사항, 원장, 개정 이력, 산출물 |
| 5 | Q-008 답변 "순수 Button + TMP_Text" | UD-010 신규. R-012 신설. AR-005 accepted. OI-008·RK-007 resolved. `UIGenericButton` 제약 resolved. 활성 미해결 0건 | AR-005 → accepted / OI-008, RK-007 → resolved / Q-008 → answered | 배너, Document State, 요구사항, 제약, 원장, 질문, 미해결, 리스크, 커버리지, 체크포인트 |

## 12. 리스크·충돌·의존성

| ID | 종류 | 내용 | 가능성/영향 | 대응 | 관련 ID | 상태 |
|---|---|---|---|---|---|---|
| RK-001 | conflict | CONTINUE가 GDD 저장 금지선과 충돌 | — | UD-006으로 CONTINUE 자체가 제외되어 소멸 | OI-001, UD-006 | **resolved** (rev 3) |
| RK-002 | risk | `Save`가 빈 클래스라 복원할 상태가 없다 | — | 저장 제외로 무관 | — | **cancelled** (rev 3) |
| RK-003 | risk | 빈 세이브 파일 오판으로 CONTINUE 항상 켜짐 | — | 저장 제외로 무관 | — | **cancelled** (rev 3) |
| RK-004 | dependency | 씬 전환 코드가 전무하므로 최소 로더를 새로 만들어야 한다 | 확실 / 낮음 | `SceneManager.LoadScene` 직접 호출로 충분 | SF-002 | open |
| RK-005 | risk | 작업 트리에 미추적 파일 다수(`SimulationTest.unity`, `Simulation/` 등). 병행 작업과 충돌 가능 | 중간 / 중간 | 현재 세션 변경분만 스테이징 (CLAUDE.md 규칙) | `git status` | open |
| RK-006 | conflict | GDD 문서와 구현이 저장 문제에서 반대 | — | UD-006으로 소멸 | — | **cancelled** (rev 3) |
| RK-007 | risk | `UIGenericButton`을 쓰면서 로컬라이즈 키를 비워두면 `NEW GAME` 라벨이 런타임에 빈칸이 된다 | — | UD-010으로 해당 컴포넌트를 쓰지 않기로 해 소멸 | SF-010, R-012, OI-008 | **resolved** (rev 5) |

## 13. 미해결·보류 항목

| ID | 항목 | 상태 | 영향 | 현재 권고 | 담당 | 재검토 트리거 |
|---|---|---|---|---|---|---|
| OI-005 | 타이틀 화면을 GDD 11 §1 / 13 §1에 반영할지 | **resolved** (rev 4) | 해소됨 — UD-009로 GDD는 수정하지 않고 이 기획서가 변경분을 기록한다 | — | 사용자 | — |
| OI-008 | `NEW GAME` 버튼 컴포넌트 선택 | **resolved** (rev 5) | 해소됨 — UD-010으로 순수 `Button` + `TMP_Text` 확정 | — | 사용자 | — |

**활성 미해결 항목 없음.**

## 14. 커버리지·일관성 점검

| 영역 | 상태 | 근거 ID | 남은 공백 |
|---|---|---|---|
| 목표 | covered | UD-001, UD-004~UD-006 | 없음 |
| 사용자·이해관계자 | covered | UD-001, UD-006 | 없음 |
| 범위 | covered | UD-001, UD-004~UD-006 | 없음 |
| 비목표 | covered | UD-006, UD-009, AR-002 | QUIT 버튼(AR-002), SaveLoad 코드 유지(AR-008)는 권고 상태로 유지 |
| 핵심 흐름 | covered | R-001~R-003, R-007, R-012 | 없음 |
| 제약 | covered | SF-002, SF-005~SF-014 | 없음 |
| 성공 근거 | covered | R-001~R-011 | 없음 |
| 리스크·의존성 | covered | RK-004, RK-005, RK-007 | 없음 |
| 미해결 결정 | covered | OI-001~OI-008 전부 resolved 또는 cancelled | 없음 |
| 핸드오프·권한 | covered | — | 구현 승인 없음 |

## 15. 인터뷰 체크포인트

- **반영된 최신 사용자 메시지**: Q-008 답변 "순수 Button + TMP_Text" (rev 5)
- **반영된 최신 근거**: 신규 없음
- **원장 전이**: UD-010 신규 / AR-005 accepted / R-012 신규 / OI-008 resolved / RK-007 resolved / Q-008 answered / `UIGenericButton` 제약 resolved
- **반영 섹션**: 배너, Document State, 요구사항, 제약, 원장, 질문 등록부, 미해결, 리스크, 커버리지
- **상충 활성 항목 점검**: 통과 — active 결정 간 상충 없음
- **추적성 점검**: 통과 — active 요구사항 R-001, R-002, R-003, R-007, R-008, R-009, R-011, R-012 모두 UD 또는 SF에 연결됨
- **활성 미해결 항목**: 없음
- **현재 초점**: 없음 — 모든 재료 결정 완료
- **다음 질문 ID**: 없음
- **재개 지점**: 사용자의 명시적 finish 신호. finish 시 §16을 채우고 영문 정본 + `.ko.md` 미러 생성
- **미확정 권고**: AR-002(QUIT 버튼 없음), AR-008(`SaveLoad` 코드 삭제하지 않음) — 사용자가 다르게 말하지 않는 한 이 권고대로 유지

## 17. 구현 결과 (rev 6)

사용자의 별도 승인("구현해줘")으로 실행했다. 계획 대비 변경 없이 그대로 구현했다.

### 산출물

| 경로 | 내용 |
|---|---|
| `Assets/01. Scripts/Title/TitleMenu.cs` | `Border.Title.TitleMenu`. `newGameButton` 자동 보정 후 `onClick` 구독, `loading` 플래그로 중복 로드 차단, `SceneManager.LoadScene(mainSceneName)` |
| `Assets/00. Scenes/00_Title.unity` | 타이틀 씬. `Main Camera`(Orthographic, SolidColor, 배경 `0.05/0.06/0.09`), `EventSystem`(`InputSystemUIInputModule`), `TitleScreen` 프리팹 인스턴스 |
| `Assets/00. Scenes/01_Main.unity` | 전환 대상 빈 씬 |
| `Assets/03. Prefabs/UI/TitleScreen.prefab` | Canvas(Overlay) + CanvasScaler(1920×1080) + GraphicRaycaster + `TitleMenu`, 자식 `TitleText`("ARTEMIS: 2026", 96pt) / `NewGameButton` → `Label`("NEW GAME", 48pt) |
| `ProjectSettings/EditorBuildSettings.asset` | `00_Title`(0), `01_Main`(1), `SampleScene`(2). 기존 항목 미변경, 6줄 추가만 |

구현 중 확인한 사실:

- `activeInputHandler: 1` (Input System 전용)이라 EventSystem에는 `StandaloneInputModule`이 아니라 `InputSystemUIInputModule`을 붙였다.
- 버튼 클릭은 인스펙터 persistent UnityEvent가 아니라 `Awake`의 `onClick.AddListener`로 배선했다. MCP로 persistent UnityEvent를 심는 경로가 불안정하고, 프로젝트의 `UIGenericButton`도 같은 자동 보정 관용구를 쓴다. `OnDestroy`에서 해제한다.
- `TitleMenu`는 `Assets/01. Scripts/UI/`가 아니라 `Assets/01. Scripts/Title/`에 뒀다. `UI/`는 업스트림 벤더링 사본이라(SF-014) 파일을 추가하면 로컬 포크가 된다.

### 검증 결과

| 요구사항 | 결과 | 근거 |
|---|---|---|
| R-001 | 통과 | 빌드 세팅 `00_Title` buildIndex 0 |
| R-002 | 통과 | Scene View 캡처에 `ARTEMIS: 2026` / `NEW GAME` 렌더 확인. 런타임 조회로 `font=LiberationSans SDF`, 알파 1, 빈 라벨 아님 |
| R-003 | 통과 | Play Mode에서 `onClick.Invoke()` 후 `active_scene = Assets/00. Scenes/01_Main.unity` |
| R-007 | 통과 | 클릭 2회 시 `loading` False → True → True, 두 번째 호출은 조기 반환 |
| R-008 | 통과 | 씬 diff가 프리팹 인스턴스 수준 |
| R-009 | 통과 | `EditorBuildSettings.asset`에 `01_Main` buildIndex 1 |
| R-011 | 통과 | `TitleMenu.cs`에 `SaveLoadSystem` / `FileManager` 참조 0건 |
| R-012 | 통과 | 자동 바인딩된 버튼이 씬의 `NewGameButton`과 동일 인스턴스. `UIGenericButton` / `UILocalizeText` 미부착 |

Console 오류 0건.

**검증하지 않은 것**: 실제 마우스 클릭 입력 경로(`InputSystemUIInputModule` 경유)는 테스트하지 않았다.
`onClick`을 코드로 직접 호출해 검증했으므로 레이캐스트·입력 모듈 구간은 미검증이다. 플레이어 빌드도
만들지 않았다. EditMode/PlayMode 테스트 스위트는 이 기능에 대해 추가하지 않았다.

### rev 7 산출물 (오프닝 연출)

| 경로 | 내용 |
|---|---|
| `Assets/01. Scripts/Title/TitleJitter.cs` | Perlin 노이즈로 `localPosition`/`localEulerAngles` 에 오프셋을 얹는 컴포넌트 하나. 카메라(진폭 크게, 회전 포함)와 텍스트(위치만, 픽셀 단위)가 **같은 스크립트를 공유**한다 |
| `Assets/01. Scripts/Title/TitleMenu.cs` | `quitButton` 필드 + `QuitGame()`. `PauseMenuController.QuitGame` 과 같은 `#if UNITY_EDITOR` 관용구, `loading` 가드로 연타 차단 |
| `Assets/03. Prefabs/3D/TitleRocket.prefab` | `RocketBody`(스케일 400, X −90) + `RocketEngine` 3기(스케일 0.6, 반경 0.17 클러스터). 높이 4.5 유닛, 바닥 y −0.5 |
| `Assets/03. Prefabs/UI/TitleScreen.prefab` | 우측 컬럼 재배치, `QuitButton` 신설, 배경 `Image` 알파 0, `Button.targetGraphic` 을 라벨로 교체, 텍스트 4개에 `TitleJitter` |
| `Assets/00. Scenes/00_Title.unity` | 카메라 perspective 전환 + 포즈, `TitleJitter`, `Directional Light` 신설, `TitleRocket` 인스턴스 |
| `Assets/Tests/EditMode/Research/MenuPrefabTests.cs` | 종료 버튼 어서션 3줄 추가 |

설계 판단:

- **`RocketBuilder` 를 쓰지 않는다.** 그것은 팩토리가 아니라 드래그 입력·Cinemachine·`Rocket` 리지드바디를
  끌고 오는 씬 컨트롤러다. 타이틀 로켓은 아트 프리팹을 손으로 조립한 정적 프리팹이면 충분하다.
  `RocketPart` 는 `Awake`/`Update` 가 없어 붙어 있어도 완전히 무동작이므로 떼지 않았다.
- **엔진 이펙트는 `playOnAwake` 오버라이드가 전부다.** `RocketEngine.prefab` 의 `Flame`/`Smoke` 는
  `playOnAwake: 0` 이라 평소엔 `RocketPart` 가 켜 준다. 타이틀에서는 그 한 값만 뒤집었다.
- **호버 피드백에 코드를 쓰지 않았다.** `Button.targetGraphic` 을 배경 `Image` 대신 자식 라벨로 바꾸면
  기존 ColorTint 가 그대로 텍스트를 물들인다.
- **버튼 크기 460×110 과 라벨 "설정" 은 고정이다.** `MenuPrefabTests` 가 세 버튼의 `sizeDelta` 일치를
  잠그고, `MenuUiPrefabBuilder.InstallTitleSettings` 가 SettingsButton 을 NewGameButton −130 위치로
  재생성한다. 배치를 바꿀 때 이 두 곳을 같이 본다.

**검증하지 않은 것**: BGM. 타이틀의 효과음과 `AudioListener` 처리는 아래 효과음 구현 항목에 반영했다.

### 오프닝 효과음 (2026-09-06)

- `TitleMenu`는 기존 게임의 공간 효과음 `RocketLoop`를 4개 동시에 재생한다. `rocketSoundTarget`이 지정되지 않으면 같은 씬의 루트 `TitleRocket`을 찾아 연결한다.
- 타이틀 진입 시 리스너가 없으면 메인 카메라에 `AudioListener`를 추가한다. 이때 추가한 리스너만 타이틀이 파괴될 때 제거한다.
- 게임 시작·설정·게임 종료 버튼은 동작을 처리하기 전에 `click`을 한 번 재생한다. 해당 버튼에는 런타임에 `UIManualClickSound`를 붙여 공통 버튼 훅과의 중복 재생을 막는다. 설정창 내부 버튼은 기존 공통 훅을 사용한다.
- 종료는 클릭음 재생이 끝난 뒤 실행한다. 새 게임의 클릭음은 씬 전환에도 유지되는 `SoundManager`에서 계속 재생된다.
- 타이틀 비활성화·씬 전환 시 로켓 루프 4개를 정지하고, 다시 활성화하면 4개만 재생한다. 다른 효과음은 정지하지 않는다.
- 회귀 테스트: `Assets/Tests/PlayMode/TitleAudioTests.cs`에 루프 개수, 공간 설정, 클릭 중복 방지, 비활성화·재활성화·파괴 시 정리를 확인하는 테스트를 추가했다.
- 검증: Unity의 기존 컴파일 설정으로 `Border` 어셈블리를 임시 경로에 별도 컴파일했다. 사용자 Play Mode를 유지하기 위해 Unity 내 재생 테스트와 실제 청취 검증은 실행하지 않았다.

### rev 8 산출물 (배경 이식 · 글자 단위 떨림)

| 경로 | 변경 |
|---|---|
| `Assets/00. Scenes/00_Title.unity` | `SkyEnvironment` 프리팹 인스턴스 + `Ground`(수면, `MAT_water`, pos `(0, -6.71, 0)`, scale `42`) 추가. `RenderSettings`(안개 ExponentialSquared 0.005, 앰비언트 3색, 스카이박스)와 `Directional Light`(회전·색·강도·색온도 6570) 를 `SimulationTest` 값으로 이식. 카메라 `clearFlags` 를 Skybox 로 |
| `Assets/01. Scripts/Title/TitleJitter.cs` | `TMP_Text` 가 붙어 있으면 `ForceMeshUpdate` 후 글자별로 메시 정점 4개씩 오프셋한다. 없으면 종전대로 트랜스폼을 흔든다 — 카메라가 이쪽 |

설계 판단:

- **배경은 복제가 아니라 이식이다.** `SkyEnvironment` 는 프리팹 인스턴스로 새로 넣고 씬 참조 4개
  (`target`=TitleRocket, `cam`, `sun`, `groundRenderer`)만 다시 배선했다. `SimulationTest` 인스턴스의
  오버라이드도 그 4개뿐이라 룩은 프리팹 기본값 그대로다.
- **`SkyEnvironment` 는 `ExecuteAlways` 가 아니다.** 에디트 모드 씬 뷰·게임 뷰에서는 기본 스카이박스와
  평평한 수면이 보이고, 실제 하늘 그라디언트·굽은 지평선은 Play Mode 에서만 나온다. 이건 버그가 아니다.
- **글자 단위 떨림에 정점 캐시를 두지 않았다.** `ForceMeshUpdate` 가 매 프레임 정점을 원본으로 되돌리므로
  캐시가 불필요하다. 타이틀은 짧은 문자열 네 개뿐이라 비용도 문제되지 않는다.
- **카메라 포즈는 사용자 소유다(UD-015).** 프레이밍을 코드로 다시 잡지 않는다.

### rev 9 산출물 (호버 떨림 · 파티클 원복)

| 경로 | 변경 |
|---|---|
| `Assets/01. Scripts/Title/TitleJitter.cs` | `IPointerEnterHandler`/`IPointerExitHandler` 구현. 자식에 `TMP_Text` 가 있으면 **호버 중에만** 글자별로 떨고, 호버가 끝난 프레임에 `ForceMeshUpdate` 한 번으로 원본 메시를 되돌린다. `TMP_Text` 가 없으면(카메라) 종전대로 계속 흔든다 |
| `Assets/03. Prefabs/UI/TitleScreen.prefab` | `TitleJitter` 를 라벨 3개 + 제목에서 떼고 **버튼 3개**에 붙였다 |
| `Assets/03. Prefabs/3D/TitleRocket.prefab` | 엔진 파티클 오버라이드를 `RevertObjectOverride` 로 전부 되돌렸다. 남은 차이는 `Flame`·`Smoke_Single` 의 `playOnAwake` 뿐 |

설계 판단:

- **지터는 라벨이 아니라 버튼에 붙는다.** 라벨은 `raycastTarget` 이 꺼져 있어 포인터 이벤트를 못 받는다.
  버튼의 `Image` 가 레이캐스트 대상이므로 핸들러가 거기 있어야 하고, 컴포넌트는 자식에서 `TMP_Text` 를 찾는다.
- **제목 텍스트는 이제 떨지 않는다.** 호버할 수 없는 대상이라 호버 전용 규칙에서는 영구히 정지다.
  계속 떨게 하려면 제목에도 컴포넌트를 붙이고 호버 조건을 예외 처리해야 하는데, 요청과 어긋나 넣지 않았다.
- **켜는 파티클은 게임과 같은 둘뿐이다.** `RocketPart.SetFlame` 이 실제로 재생하는 것은 `flame` 과
  `smokeSingle` 이다. `smoke` 는 프리팹에서 GameObject 가 꺼져 있어 런타임에도 재생되지 않고,
  `smokeFail` 은 점화 실패 전용이다 — 타이틀도 같은 조합을 쓴다.

### rev 10 산출물 (캔버스 기울이기)

| 경로 | 변경 |
|---|---|
| `Assets/03. Prefabs/UI/TitleScreen.prefab` | 루트 `Canvas.renderMode` → **Screen Space - Camera**, `planeDistance 8`. 새 자식 `TitleGroup`(900×620, anchoredPosition `(380, -10)`, `localScale 1.1`, `localEulerAngles (0, 22, 0)`) 안에 제목 + 버튼 3개를 넣었다. 버튼 좌표는 그룹 기준 `(0, 240)` / `(0, 20)` / `(0, -110)` / `(0, -240)` |
| `Assets/00. Scenes/00_Title.unity` | `Canvas.worldCamera` = Main Camera. 사용자가 시도했던 World Space 오버라이드(스케일 0.1, 위치 `(-160, -63, -209)`)를 프리팹 값으로 되돌렸다 |
| `Assets/06. Packages/Editor/MenuUiPrefabBuilder.cs` | `root.transform.Find("NewGameButton"/"SettingsButton")` 가 계층 변경으로 깨지므로 재귀 탐색 `FindDeep` 으로 교체 |

설계 판단:

- **World Space 가 아니라 Screen Space - Camera 다.** World Space 로 바꾸면 자식 캔버스인 설정창까지
  같은 평면에 얹혀 함께 기울고, 캔버스를 카메라 시야 안에 손으로 맞춰야 한다. Screen Space - Camera 는
  캔버스가 화면을 그대로 덮으므로 배치 고민이 사라지고, **회전은 상속되지 않으므로** 기울이고 싶은
  자식(`TitleGroup`)만 돌리면 된다. 원근은 카메라가 원근 투영이라 그대로 걸린다.
- **각도의 부호는 스케치가 정한다.** 그림에서 오른쪽 변이 더 크므로 오른쪽이 카메라 쪽으로 나와야 한다 —
  Y `+22°`. 반대 부호면 오른쪽이 멀어져 그림과 어긋난다.
- **`planeDistance 8` 은 카메라 근평면(0.1)과 로켓(카메라에서 약 5~7 유닛) 사이 값이다.** 더 줄이면
  UI 가 로켓 앞으로 파고들고, 더 키우면 로켓이 UI 를 뚫는다.

## 16. 확정 및 핸드오프

명시적 finish 신호를 받은 뒤에만 작성한다. 현재 상태: **미확정**.

> 이 계획을 세우거나 승인하는 것은 구현, 커밋, PR, 패키지 설치, 배포, 씬·프리팹 생성, GDD 문서 개정,
> 외부 시스템 변경을 승인하지 않는다. 각각 별도의 명시적 승인이 필요하다.
