# 13. 기술 구조 - Unity 기준

## 씬

- 타이틀 씬
- 프로젝트 운영 씬
- 연구 미니게임 씬 또는 오버레이
- 로켓 설계 씬
- 발사 시뮬레이션 씬
- 결과 오버레이

씬 전환 중에도 캠페인 상태는 하나의 세션 객체가 소유한다.

## 런타임 데이터

```csharp
public struct MissionLaunchState
{
    public LaunchMissionId MissionId;
    public bool Unlocked;
    public bool Completed;
    public int AttemptCount;
}

public struct LaunchResultData
{
    public LaunchMissionId MissionId;
    public bool Succeeded;
    public LaunchTerminationReason TerminationReason;
    public LaunchTelemetry Telemetry;
    public TestVisibility Visibility;
}
```

엔진 상태는 완성도와 네 실제 스탯을 가진다. 설계 데이터는 설치 프리셋 ID, 위치, 회전과 가동 구간을 가진다. 실행 전 데이터에는 성공 여부를 넣지 않는다.

## 책임 분리

- 캠페인 모델: 시간, 연구비, 엔진과 미션 상태
- 설계 컨트롤러: 조립 입력과 비용 검증
- 시뮬레이션 호스트: 설계 데이터를 물리 객체로 변환
- 로켓·엔진 컴포넌트: 힘, 연료, 열, 점화 처리
- 미션 평가기: 실제 상태에서 목표와 종료 조건 평가
- 결과 적용기: 해금, 이벤트와 캠페인 복귀를 한 번 처리
- 결과 UI: 확정된 데이터만 표시

## 발사 처리

```text
validate -> pay -> load -> construct -> simulate
-> measure -> evaluate -> capture -> apply -> report
```

시뮬레이션 호스트는 평가기가 준 성공 여부를 그대로 전달한다. 캠페인 모델은 이를 다른 공식으로 다시 계산하지 않는다.

## 미션 해금

결과 적용기는 성공한 미션을 완료 상태로 바꾸고 바로 다음 미션을 연다. 이미 완료된 미션을 다시 성공해도 해금 처리를 반복하지 않는다.

## 공개성

공개성 설정은 시뮬레이션 입력에서 표시·이벤트 메타데이터로만 전달한다. 힘, 연료, 열, 점화와 평가 조건에는 접근하지 않는다.

## 결과 중복 방지

각 발사에는 실행 ID를 부여한다. 캠페인 모델은 처리한 실행 ID를 다시 적용하지 않는다. 장면 해제, 결과 UI 재표시와 중복 콜백이 자원이나 진행 상태를 바꾸지 않아야 한다.

## 데이터 에셋

- 엔진 기본값과 비용
- 미션 목표와 제한 시간
- 결과 이벤트 후보와 효과
- 카메라·VFX·사운드 설정

실제 물리 수치의 원천을 중복하지 않는다. ScriptableObject와 프리팹 중 한 곳을 권위 원천으로 정한다.

## 오류 처리

- 필수 설계 데이터가 없으면 발사를 시작하지 않는다.
- 알 수 없는 종료 사유는 일반 실패로 표시하되 원본 값을 로그에 남긴다.
- 결과 UI가 없어도 캠페인 상태 적용과 장면 복귀는 완료한다.
- 사진 촬영 실패는 결과와 정산을 바꾸지 않는다.
