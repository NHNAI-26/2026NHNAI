# 13. 기술 구조 — Unity 기준

> 상태: **권장 구현 구조 확정**  
> 목표: 48시간 안에 연구, 설계, 자동 발사를 분리하고, 발사 결과 중복 적용과 재굴림을 방지한다.

## 1. 씬 구성

권장 씬:

```text
00_Boot
01_Main
02_Design
03_Sim_Engine
04_Sim_Rocket
05_Sim_Orbit
06_Sim_Moon
```

`00_Boot`는 선택 사항이다. 시간이 부족하면 `01_Main`에서 바로 초기화한다.

설계 단계는 공통 씬 하나를 사용한다. 자동 발사는 단계별 별도 씬으로 두되, 공통 HUD와 실행 구조는 프리팹 또는 공통 컴포넌트로 공유한다.

## 2. 핵심 런타임 데이터

```csharp
public enum StageId
{
    Engine,
    Rocket,
    Orbit,
    Moon
}

public enum EnvironmentId
{
    Stable,
    Ideal,
    HighWind,
    Thunderstorm,
    MeteorShower,
    SolarStorm
}

public enum TestGrade
{
    S,
    A,
    B,
    C,
    F
}
```

### GameState

```csharp
[Serializable]
public sealed class GameState
{
    public int year = 2018;
    public int quarter = 1;
    public int remainingTurns = 36;

    public int funds = 2200;
    public int quarterlyFunding = 600;

    public StageState engine;
    public StageState rocket;
    public StageState orbit;
    public StageState moon;

    public EnvironmentId[] environmentSchedule;
    public int totalFundsSpent;
    public int totalLaunches;
    public int totalFailures;
    public bool gameEnded;
}
```

### StageState

```csharp
[Serializable]
public sealed class StageState
{
    public StageId id;
    public int progress;
    public int attemptCount;
    public TestGrade? bestGrade;
    public bool unlocked;
}
```

### DesignData

```csharp
[Serializable]
public sealed class DesignData
{
    public StageId stageId;
    public int year;
    public int quarter;
    public EnvironmentId environmentId;

    public int mapSeed;
    public string targetPathId;

    public RocketPartPlacement[] partPlacements;
    public float force;
    public float directionDegrees;
    public int designFit;
}
```

`DesignData`는 설계 씬에서 수정된다. 발사 전 연구 화면으로 돌아가면 버릴 수 있다. 같은 분기·단계·시드로 다시 설계 씬에 들어오면 같은 맵과 목표 경로를 생성해야 한다.

### SimRunData

```csharp
[Serializable]
public sealed class SimRunData
{
    public StageId stageId;
    public int year;
    public int quarter;
    public EnvironmentId environmentId;

    public int mapSeed;
    public string targetPathId;
    public int designFit;

    public int currentProgress;
    public float prerequisiteAverage;
    public int experienceBonus;

    public int successChance;
    public int partialChance;
    public int failChance;
    public int roll;

    public TestGrade grade;
    public string incidentId;
    public bool incidentRecovered;
    public int seed;

    public SimMetrics metrics;
    public DesignData designData;

    public bool resultApplied;
}
```

`SimRunData`는 설계 씬에서 최종 `발사` 버튼을 누른 직후 생성하고 자동 발사 씬 전환 전에 보존한다.

## 3. 권장 관리자

### GameFlowController

- 새 게임
- 분기 행동 처리
- 설계 씬 진입과 복귀
- 씬 전환
- 승리·패배 검사
- 결과 보고서 호출

### TimeManager

- 현재 분기 계산
- 남은 분기 계산
- 분기 종료
- 2026 Q4 마지막 행동 처리

### EconomyManager

- 비용 검사와 차감
- 즉시 지원금 반영
- 분기 연구비 증감과 범위 제한
- 분기 종료 지급

### ResearchManager

- 일반·집중 연구
- 진행도 상한 처리
- 단계 해금 검사
- 이전 단계 평균 계산

### ForecastManager

- 36분기 환경 생성
- 현재 포함 4분기 반환
- 환경 연속 제약 처리
- 단계별 환경 보정 반환

### ProbabilityResolver

- 성공·부분 성공·실패 확률 계산
- 설계 적합도 보정 반영
- 난수 생성
- 등급 결정
- 사고 선택
- 연출용 지표 생성

### SimulationDirector

- `SimRunData` 읽기
- 환경 효과 활성화
- 카운트다운
- 정상 시퀀스
- 사고 시퀀스
- 결말 시퀀스
- 결과 보고서 전환
- 배속과 건너뛰기

### DesignSceneController

- 현재 단계와 분기 기준 맵 생성
- 목표 경로 표시
- 부품 위치, 힘, 방향 입력 처리
- 설계 적합도 계산
- 예상 성공률 표시
- 연구 단계로 복귀
- 발사 확인과 `SimRunData` 생성 요청

### ResultApplier

- 결과를 정확히 한 번 반영
- 진행도 증가
- 발사 횟수 증가
- 최고 등급 갱신
- 즉시 지원금
- 분기 연구비 변화
- 총계 기록
- 단계 해금

## 4. 데이터 에셋

ScriptableObject 권장:

### GameBalanceConfig

- 시작 자금
- 분기 연구비
- 하한·상한
- 연구 진행도
- 등급 보상
- 확률 하한·상한

### StageConfig

```text
stageId
displayName
normalResearchCost
focusedResearchCost
testCost
minimumTestProgress
unlockProgressRequirement
designSceneName
simulationSceneName
```

### EnvironmentConfig

```text
environmentId
displayName
weight
engineModifier
rocketModifier
orbitModifier
moonModifier
icon
environmentVfxPrefab
```

### IncidentConfig

```text
incidentId
stageId
compatibleEnvironments
allowedGrades
recoveredForGrades
warningText
resultReasonText
```

### DesignConfig

```text
stageId
availablePartIds
attachmentPoints
forceMin
forceMax
targetPathPatterns
designFitToleranceByProgress
```

시간이 부족하면 ScriptableObject 대신 직렬화된 단일 설정 클래스도 허용한다. 단, 수치를 여러 스크립트에 하드코딩하지 않는다.

## 5. 행동 처리 의사코드

### 연구

```pseudo
function ExecuteResearch(stage, mode):
    cost = GetResearchCost(stage, mode)

    if funds < cost:
        return NotEnoughFunds

    funds -= cost
    totalFundsSpent += cost

    gain = mode == Normal ? 6 : 10
    stage.progress = min(stage.progress + gain, 100)

    CheckStageUnlocks()
    EndQuarter()
```

### 설계 진입

```pseudo
function EnterDesign(stage):
    if not stage.unlocked:
        return Locked

    if stage.progress < stage.minimumTestProgress:
        return ProgressTooLow

    if funds < stage.testCost:
        return NotEnoughFunds

    designData = CreateOrLoadDesignData(stage, currentYear, currentQuarter)
    LoadScene("02_Design")
```

설계 진입은 비용과 분기를 소비하지 않는다.

### 연구 단계로 복귀

```pseudo
function ReturnFromDesign():
    DiscardUnsavedDesignData()
    LoadScene("01_Main")
```

### 발사 시작

```pseudo
function ConfirmLaunch(stage, designData):
    if funds < stage.testCost:
        return NotEnoughFunds

    funds -= stage.testCost
    totalFundsSpent += stage.testCost

    currentEnvironment = forecast[currentTurn]
    simRunData = ProbabilityResolver.Resolve(stage, currentEnvironment, designData)

    Persist(simRunData)
    LoadScene(stage.simulationSceneName)
```

### 발사 종료

```pseudo
function FinishSimulation():
    ResultApplier.ApplyOnce(simRunData)

    if IsMoonVictory(simRunData):
        ShowVictoryEnding(simRunData)
        return

    ShowResultReport(
        simRunData,
        onClose: EndQuarterAndReturnToMain
    )
```

결과 보고서를 메인 씬에서 보여주거나 시뮬레이션 씬 위에서 보여주는 것은 선택 가능하다. 단, 결과 적용은 한 곳에서만 수행한다.

## 6. 확률 처리 의사코드

```pseudo
function GetSuccessChance(stage, designData):
    current = stage.progress
    experience = min(stage.attemptCount * 3, 9)
    environment = GetEnvironmentModifier(currentEnvironment, stage.id)
    design = round((designData.designFit - 50) * 0.4)

    if stage.id == Engine:
        raw = 20 + current * 0.8 + experience + environment + design
    else:
        prerequisiteAverage = GetPrerequisiteAverage(stage.id)
        raw = 20 + current * 0.6
                 + prerequisiteAverage * 0.2
                 + experience
                 + environment
                 + design

    return clamp(round(raw), 10, 90)
```

## 7. 설계 맵 생성

단순 구현 방법:

1. 세션 시드, 현재 분기, 단계 ID로 `mapSeed` 생성
2. 단계별 목표 경로 패턴 중 하나 선택
3. 현재 환경에 맞는 위험 구간 또는 보정 방향 표시
4. 출발 지점과 목표 지점 배치
5. 설계 입력으로 예상 경로와 `designFit` 계산

같은 분기와 같은 단계에서는 설계 씬을 다시 열어도 같은 맵과 목표 경로가 나와야 한다. 발사하지 않고 연구 단계로 돌아오면 비용과 시간은 그대로다.

## 8. 환경 스케줄 생성

단순 구현 방법:

1. 0번 분기부터 순서대로 가중치 추첨
2. 직전 2개와 같은 위험 환경이 뽑히면 다시 추첨
3. 직전 3분기가 모두 위험 환경이면 현재 분기는 `Stable` 또는 `Ideal` 중 하나로 강제
4. 36개를 생성해 결과 배열 저장
5. UI에는 현재 인덱스부터 최대 4개를 표시
6. 2026 Q4 이후 자리는 `PROJECT DEADLINE` 플레이스홀더로 채움

이 방식이면 모든 연속 4분기 창에 안정 또는 최적 환경이 최소 한 번 존재한다. 복잡한 재귀 생성기나 백트래킹은 필요 없다.

## 9. 3D 시퀀스 구현 방식

권장 우선순위:

1. 코루틴 또는 단순 상태 머신
2. Animation Clip
3. Unity Timeline
4. 필요 시 DOTween

팀이 이미 익숙한 방식 하나만 사용한다. Timeline, Animator, DOTween을 한 장면 안에서 무분별하게 혼합하지 않는다.

성공 경로는 Transform 애니메이션으로 만들고, 실패 시점부터 Rigidbody를 켜는 방식이 가장 빠르다.

## 10. 시뮬레이션 상태

```csharp
public enum SimulationPhase
{
    Briefing,
    Countdown,
    Nominal,
    Incident,
    Recovery,
    SuccessEnding,
    PartialEnding,
    FailureEnding,
    Complete
}
```

각 단계 컨트롤러는 공통 인터페이스를 구현한다.

```csharp
public interface IStageSimulation
{
    void Initialize(SimRunData data);
    void Play();
    void SkipToResult();
}
```

## 11. 결과 중복 방지

필수 안전장치:

```csharp
if (simRunData.resultApplied)
{
    return;
}

simRunData.resultApplied = true;
ApplyRewardsAndProgress();
```

아래 상황에서도 한 번만 적용되어야 한다.

- 건너뛰기
- 씬 재로드
- 버튼 연타
- 결과 화면 중복 호출
- 애니메이션 이벤트 중복

## 12. 저장 정책

게임잼 버전은 중간 저장을 제공하지 않는다. 그러나 씬 사이 상태 보존은 필요하다.

허용 방식:

- `DontDestroyOnLoad` 런타임 세션 객체
- 정적 세션 컨테이너
- 임시 JSON 직렬화

권장 방식은 `DontDestroyOnLoad GameSession` 하나다. 영구 저장 시스템은 만들지 않는다.

## 13. 입력

메인 UI:

- 마우스 클릭
- Enter 또는 Space로 확인 가능하면 P1
- Esc로 모달 닫기

설계:

- 마우스 드래그: 부품 위치 또는 방향 조정
- 슬라이더: 힘 조정
- Esc: 연구 단계로 돌아가기 확인

시뮬레이션:

- Space: 2배속 토글, P1
- Esc: 일시정지
- 결과 개입 입력 없음

## 14. 오류 처리

- `SimRunData` 없이 시뮬레이션 씬에 진입하면 메인으로 복귀
- `DesignData` 없이 설계 씬에 진입하면 메인으로 복귀
- 존재하지 않는 사고 ID면 등급 기본 시퀀스 사용
- 환경 VFX가 없어도 확률과 결과는 정상 처리
- 카메라가 누락되어도 기본 카메라 사용
- 결과 보고서 데이터가 누락되면 등급과 경제 변화만 표시

## 15. 성능 기준

- 1080p에서 안정적 실행
- 폭발 파편 수를 제한
- 유성우는 풀링 또는 소수 파티클로 구현
- 실시간 그림자 수를 최소화
- 한 장면에 하나의 주요 광원
- 시뮬레이션 씬 전환 중 짧은 로딩 화면 허용
