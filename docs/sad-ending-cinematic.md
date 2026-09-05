# 배드 엔딩 연출

2026 Q4 마감까지 최종 미션 `LowPowerZoneHold` 를 B 이상으로 통과하지 못했을 때 도는 마무리다.
성공 경로의 짝은 `docs/specs/happy-ending-cinematic-spec.md` 에 있다.

## 트리거

새 판정을 만들지 않았다. `ResearchPrototypeModel.EvaluateGameEnd` 가 이미 내리는
`HasGameEnded && !GameWon` 을 그대로 쓴다.

신문을 닫는 경로는 여럿이지만(결과 보고 콜백, `OnEnable` 의 `Refresh`, 설계 화면 복귀)
모두 `ResearchOperationUIController.ShowEndingScreen()` 한 곳으로 모인다. 그래서 가로채는 자리도
거기 하나다 — 해피엔딩이 `SetEndingOverride` 로 잡는 것과 같은 지점이다.

`Play()` 는 신문이 이미 나온 뒤 이어 붙는 실제 경로용이고, `Play(research, result)` 는 신문부터
띄우는 디버그용이다. 신문 닫힘은 `ShowLaunchResultOverlay` 의 `afterReports` 콜백으로 받는다 —
해피엔딩이 쓰는 `SetEndingOverride` 와 달리 게임이 끝나지 않은 디버그 재생에서도 걸린다.

`SadEndingSequence.Play()` 는 **플레이 모드가 아니면 `null` 을 돌려준다.** 이때 호출부는 기존
`ResearchEndingController` 통계 패널로 그대로 떨어진다. 시네마틱은 코루틴이 필요하고 EditMode
테스트는 코루틴을 돌리지 못하므로, 패널은 삭제하지 않고 폴백 겸 승리 경로용으로 남겼다.
런타임 실패 경로에서는 패널이 나오지 않는다 — 다시하기 버튼 대신 타이틀로 돌려보낸다.

## 비트

| 비트 | 내용 |
|---|---|
| B0 | 실패 신문. 실제 경로에서는 이미 나온 뒤라 건너뛴다 — 디버그 재생처럼 앞선 결과 보고가 없을 때만 시퀀스가 직접 띄운다 |
| B1 | 검은 오버레이를 alpha 0 에서 1 로 페이드(1.2초). 컷이 아니라 페이드인 이유는 직전 화면이 신문이기 때문 |
| B2 | 대사 5줄. 프롤로그와 같은 타이핑 연출 — `maxVisibleCharacters` 를 올리고 `keyboard01..04` 를 친다 |
| B3 | 여백(1.5초) 뒤 `00_Title` 로. 해피엔딩과 같은 퇴장 |

`Assets/01. Scripts/Research/SadEndingSequence.cs`. 대사·타이밍은 전부 직렬화 필드라 코드 수정 없이
인스펙터에서 바꾼다.

화면 어디를 눌러도 되지만 **클릭 한 번은 현재 대사 한 줄만 앞당긴다**(`IPointerClickHandler`).
타이핑 중이면 남은 글자를 즉시 드러내고 거기서 멈춘다 — 다음 줄로 넘어가려면 한 번 더 눌러야 한다.
연출 전체를 건너뛰는 클릭은 없다. 검은 화면으로 들어가는 페이드도 클릭으로 끊지 않는다.

무대는 없다. 검은 화면과 글자뿐이라 3D 구간도, 씬·프리팹·ScriptableObject 신규 에셋도 만들지
않았다 — 오버레이는 해피엔딩과 같이 런타임에 세운다. `01_Main` 을 더럽히지 않기 위한 선택이다.

해피엔딩이 `Simulation` 어셈블리에 사는 것과 달리 배드엔딩은 `Border` 쪽에 둔다. 어셈블리 의존은
`Simulation` → `Border` 한 방향뿐이라, `ResearchOperationUIController` 에서 부르려면 여기 있어야 한다.
3D 무대도 `Rocket` 참조도 없으니 넘어갈 이유도 없다.

## 신문은 새로 만들지 않았다

앞의 실패 신문은 이미 있던 `FinalFailure` 특별호다.
`ResearchFlowSession.QueueDeadlineFailureReportIfNeeded()` 가 띄우고 `NewspaperReveal` 이 그린다.
시네마틱은 그 신문이 닫힌 **뒤에** 시작한다 — 연출을 재구현하지 않는다.

문구만 배드엔딩 톤으로 교체했다(`LaunchNewspaperArticle`):
헤드라인 "세금만 태운 8년, 책임자 구속", 본문에 감사원 결론과 구속 기소, 설비 매각.
`LaunchOutcomeEventId.FinalFailure` 의 이벤트 `Description`(존댓말 사본)은 메일·이벤트 알림에서도
쓰이므로 건드리지 않았다.

## 디버그

`HappyEndingDebugTester` 에 얹었다. **F9** 또는 **Tools > Border > Debug > Play Sad Ending**.
36분기를 실제로 소진하지 않고 신문부터 재생한다. 확인 대기 중인 결과가 있으면 그것을, 없으면
`ResearchPrototypeModel.CreateDeadlineFailureResult()` 로 실제 마감 실패와 같은 기사를 만든다 —
최종 미션·공개 발사로 고정돼 있어 매체가 항상 신문이다.

## 알려진 부채

타이핑 루프가 이 파일에서 **세 번째 사본**이다(`PrologueController`, `NewspaperReveal` 에 이어).
앞의 둘은 PlayMode 테스트로 동작이 잠겨 있어 이번에 통합하지 않았다. 네 번째 호출자가 생기면
그때 공용 헬퍼로 추출한다. 코드에도 `ponytail:` 주석으로 표시해 두었다.
