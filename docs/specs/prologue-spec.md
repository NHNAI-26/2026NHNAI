# 프롤로그 명세

GDD `docs/artemis-2026-gdd/02_전체_플레이_흐름.md` §2 가 확정한 프롤로그의 구현 명세다.
GDD 자체는 수정하지 않는다(`docs/specs/title-scene-spec.md` UD-009) — 변경분은 이 문서가 들고 간다.
`title-scene-spec.md` 가 `별도 작업`으로 미뤄둔 항목이 이 작업이다.

## 무엇을 만들었나

`01_Main` 이 로드되면 검은 전체화면 오버레이가 운영 화면을 덮고 5개 컷을 차례로 재생한 뒤,
스스로 사라지며 운영 화면을 드러낸다. 총 23.0초. 아무 곳이나 클릭하면 즉시 건너뛴다.

| 파일 | 역할 |
|---|---|
| `Assets/01. Scripts/Prologue/PrologueSequenceSO.cs` | 컷 데이터. `PrologueBeat` 리스트 + BGM id + reveal 시간 |
| `Assets/01. Scripts/Prologue/PrologueController.cs` | 재생 코루틴, 스킵, 사운드 호출 |
| `Assets/02. ScriptableObjects/Prologue/PrologueSequence.asset` | 저작된 5컷 |
| `Assets/03. Prefabs/UI/PrologueOverlay.prefab` | Canvas(sortingOrder 200) + Backdrop + Line |
| `Assets/00. Scenes/01_Main.unity` | 위 프리팹 인스턴스 1개 |
| `Assets/Tests/EditMode/Prologue/PrologueSequenceTests.cs` | 5컷·12~20초 예산 잠금 |

## 결정과 이유

### 운영 화면을 끄지 않고 sortingOrder 로 덮는다

`SimulationStageHost` 는 `ResearchOperationUIController` 의 루트를 껐다 켠다. 그건 3D 씬을 운영 화면
*밑에* additive 로 깔기 때문이고, 프롤로그는 반대로 *위에* 불투명하게 덮는다.

오버레이 캔버스는 sortingOrder 200 이다. 운영 화면 캔버스는 sortingOrder 를 설정하지 않아 0,
`SimulationStageHost` 의 토글 캔버스가 100 이다. 불투명한 `Backdrop` Image + `GraphicRaycaster` 가
그리기와 입력을 동시에 막는다 — EventSystem 이 캔버스 sorting order 로 레이캐스트를 정렬하므로
뒤쪽 UI 는 클릭되지 않는다. 껐다 켜는 코드가 없으니 복원 실패로 운영 화면이 사라질 경로도 없다.

부수 효과로 **실행 순서 문제가 사라진다.** `ResearchOperationUIController.SpawnInMainScene` 같은
`RuntimeInitializeOnLoadMethod` 들 사이의 순서는 Unity 가 보장하지 않지만, 프롤로그는 씬 인스턴스라
그 경쟁에 아예 들어가지 않고(`AfterSceneLoad` 는 모든 씬 `Awake()` 이후), 보이는 것은 순서가 아니라
sortingOrder 가 정한다. 두 객체 모두 렌더링 전 같은 프레임에 만들어지므로 운영 화면이 새어 보이는
프레임은 없다.

### 컷은 고정 필드가 아니라 리스트다

컷 추가·삭제·순서 변경·문구 수정을 코드 수정 없이 인스펙터에서 끝내기 위함이다.
`kind` enum 은 두지 않았다 — `line` 이 비면 "텍스트 없는 검은 화면"이라는 뜻이고, 그게 1번 컷(통신음)과
5번 컷(전환 직전 여백)이다. 한 문자열이 표현하지 못하는 컷(이미지, 흔들림)이 생기면 그때 enum 을 넣는다.

### 타이밍은 오디오 클립 길이와 무관하다

모든 길이는 SO 데이터에서만 나온다. `AudioClip.length` 를 읽거나 `handle.IsPlaying` 을 기다리는 코드는
없다. 그래서 클립이 하나도 없는 지금도 연출이 정상 속도로 흘러간다(무음). 이 성질은 유지해야 한다.

미싱 id 는 안전하다: `SoundManager.TryGetSfx`/`TryGetBgm` 이 `Log.W` 후 `SoundHandle.Invalid` 를 반환할
뿐 예외를 던지지 않고, `SoundDatabaseSO` 는 클립이 null 인 행을 아예 목록에서 뺀다. `Log.W` 는
`[Conditional("UNITY_EDITOR")]` 라 빌드에서는 사라진다.

### 텍스트 등장은 타이핑, 알파 페이드가 아니다

글자를 하나씩 찍는다. 알파를 올리는 대신 `TMP_Text.maxVisibleCharacters` 만 증가시킨다 —
텍스트 전체가 처음부터 레이아웃돼 있으므로 줄이 늘어나도 문장이 위아래로 튀지 않는다.
알파 페이드로 같은 효과를 내려면 글자마다 별도 메시 컬러를 만져야 해서 훨씬 비싸다.

컷마다 `typeSecondsPerChar` 로 속도를 정하고, **0 이면 타이핑 없이 기존 `fadeInSeconds` 페이드로
돌아간다.** 빈 줄(1·5번 컷)은 찍을 글자가 없어 자동으로 타이핑 대상에서 빠진다.
사라질 때는 여전히 `fadeOutSeconds` 알파 페이드다 — 글자를 거꾸로 지우는 연출은 쓰지 않는다.

예산 계산에서 타이핑 컷은 `fadeInSeconds` 대신 `글자 수 × typeSecondsPerChar` 를 등장 구간으로
잡는다(`PrologueBeat.TotalSeconds`). 문구를 길게 고치면 총 길이가 같이 늘어나므로 예산 테스트가
그것도 함께 잡아낸다.

### 페이드는 DOTween 이 아니라 손으로 짠 `Mathf.Lerp`

DOTween 은 설치돼 있고 asmdef 수정 없이 쓸 수 있지만, 코루틴 안에서 쓰려면 언스케일드 시간
(`SetUpdate(true)`), 스킵 시 파괴된 `CanvasGroup` 에 계속 쓰는 것을 막는 `SetLink`/`Kill`,
`WaitForCompletion()` 이 붙어 8줄짜리 lerp 루프보다 길고 실패 지점도 많다. 선형 페이드에는 이득이 없고
프로젝트에 UI 페이드 선례도 없다. 이징이나 체이닝이 필요해지면 그때 바꾼다.

시간은 전부 언스케일드(`Time.unscaledDeltaTime`, `WaitForSecondsRealtime`)다. 지금 `Time.timeScale` 을
건드리는 코드는 없지만, 나중에 일시정지가 생겨도 프롤로그가 검은 화면에서 멈추지 않는다.

### 스킵은 Button 이 아니라 `IPointerClickHandler`

`Backdrop` 클릭이 부모인 `PrologueController` 까지 버블링된다 — uGUI 는 핸들러를 찾을 때까지 부모를
거슬러 올라간다. `Button` + `UnityEvent` 배선이 사라져 프리팹에서 끊길 여지가 없다.

레거시 `UnityEngine.Input` 은 쓸 수 없다. `ProjectSettings.asset` 의 `activeInputHandler: 1`
(Input System 전용)이라 `Input.GetMouseButtonDown` 은 예외를 던진다. EventSystem 은
`ResearchOperationUIController.EnsureEventSystem()` 이 Awake 중에 만들어 둔다.

### `FadeChannelSO` 는 쓰지 않았다

타입은 있지만 프로젝트 전체에 에셋·리스너·호출부가 0개다. 같은 프리팹 위의 두 컴포넌트 사이에 이벤트
채널을 끼우는 것은 생산자 1·소비자 1짜리 간접층이다.

### "첫 실행에만" 가드는 넣지 않았다

`01_Main` 은 프로세스당 최대 1회 로드된다 — `SceneManager.LoadScene` 호출부가 `TitleMenu.cs` 하나뿐이고
`00_Title` 로 돌아오는 경로가 없다. `static bool` 가드는 죽은 코드가 된다. 타이틀로 돌아가는 흐름이
생기면 그때 추가한다.

## 컷 데이터

`PrologueSequence.asset`. 총 15.5초 = 컷 14.0초 + reveal 1.5초.

| # | line | type/char | fadeIn | hold | fadeOut | fontSize | sfxId |
|---|---|---|---|---|---|---|---|
| 1 | *(빈 문자열)* | 0 | 0 | 2.5 | 0 | 48 | `Prologue_SecureComms` |
| 2 | `2017.12` | 0.10 | — | 1.2 | 0.5 | 56 | |
| 3 | `2026년까지 유인 우주선을 달에 착륙시키십시오.` | 0.06 | — | 2.2 | 0.7 | 40 | |
| 4 | `ARTEMIS: 2026` | 0.09 | — | 2.0 | 0.8 | 84 | |
| 5 | *(빈 문자열)* | 0 | 0 | 0.6 | 0 | 48 | |

`type/char` 가 0 보다 큰 컷은 `fadeInSeconds` 를 쓰지 않는다(표의 `—`).

`bgmId = Prologue_LowTension`, `bgmFadeInSeconds = 2`, `revealSeconds = 1.5`.

### GDD 의 20~30초 예산에서 벗어났다

GDD 02 §2 는 20~30초를 적었지만, 실제로 돌려 보니 글자가 다 찍힌 뒤 머무는 시간이 길어 늘어졌다.
사용자 지시로 `holdSeconds` 와 `fadeOutSeconds` 를 줄여 **총 15.5초**로 맞췄다. 타이핑 속도
(`typeSecondsPerChar`)는 그대로다 — 문제는 등장 속도가 아니라 등장 후 정지 시간이었다.

`PrologueSequenceTests` 의 예산 상수도 12~20초로 함께 바꿨다. 테스트의 목적은 GDD 숫자를 지키는 것이
아니라 인스펙터에서 값을 만지다 길이가 폭주하는 것을 잡는 것이다.

이 예산은 `PrologueSequenceTests` 가 잠근다. 인스펙터에서 길이를 만지다 예산을 깨는
것이 현실적인 실패 모드라서 데이터를 테스트한다. 컷 진행 코루틴은 테스트하지 않는다 —
`Time.unscaledDeltaTime` 에는 시임이 없고, 가짜 시계를 위해 실제 구현이 하나뿐인 주입점을 프로덕션
코드에 만들 이유가 없다.

## 타이핑 효과음

DiaBlackJack의 `Assets/04. Audios/SFX/keyboard01.mp3`부터 `keyboard04.mp3`까지 네 효과음을
가져왔다. 원본 앞의 약 0.08~0.10초 무음과 뒤쪽 무음을 줄여 이 프로젝트의
`Assets/04. Audios/SFX/keyboard01.wav`~`keyboard04.wav`로 저장했다.
`SoundDatabase.asset`의 같은 이름 ID로 등록하며 볼륨 0.45, 비루프, 2D 재생이다.
짧은 효과음이므로 Decompress On Load와 Preload Audio Data를 사용한다.

`PrologueController`는 새 글자가 나타날 때 네 소리를 순서대로 재생한다. 공백만 드러난
프레임에는 새 소리를 내지 않고, 느린 프레임에 여러 글자가 드러나도 한 번만 재생한다.
각 키의 짧은 잔향은 겹쳐 재생할 수 있지만, 줄의 타이핑 완료·건너뛰기·비활성화 시에는
이 컨트롤러가 재생한 키 소리를 모두 멈춘다. 컷 사이 대기와 페이드에서는 새 소리를 내지 않는다.

기존 컷별 효과음과 프롤로그 BGM 설정은 유지한다. `SoundManager`는 연구 화면 초기화에서
준비하며, 연구실 음악은 프롤로그 텍스트와 오버레이 페이드가 모두 끝난 뒤 시작한다.

### 검증

`PrologueTypingAudioTests` Play Mode 테스트 3개를 통과했다. 한 프레임 중복 재생 방지,
공백 무음, 다른 효과음 유지, 스킵·줄 완료·비활성화 정지를 확인했다. Unity 컴파일도 통과했다.
