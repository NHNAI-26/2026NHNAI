using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 엔딩에서 로켓을 <b>정해진 경로</b>로 날린다. 로켓 자신은 평소처럼 살아 있다 — 점화 램프,
    /// 홀드 중 몸통 흔들림, 리프트 연기, 발사 사운드가 전부 인게임과 같은 경로로 나온다.
    /// 이 컴포넌트가 하는 일은 둘뿐이다: 물리를 못 움직이게 묶고, 위치를 대신 써 준다.
    ///
    /// <see cref="Rocket"/> 을 꺼 버리면 안 되는 이유는 <see cref="RocketPart"/> 의 점화 상태가
    /// <c>Rocket.Launch()</c> 안의 <c>Prepare</c> 에서만 세팅되기 때문이다. 그걸 건너뛰면
    /// 화염이 아예 붙지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HappyEndingFlight : MonoBehaviour
    {
        // 씬(SimulationTest)의 Rocket 인스펙터 값과 같아야 이륙 첫 구간이 인게임과 겹친다.
        // assistedLiftHeight / assistedLiftSeconds 가 private serialized 라 읽어 올 수 없어 박아 둔다.
        private const float AssistHeight = 3f;
        private const float AssistSeconds = 2.5f;

        /// <summary>보조 상승 + 물리 전환이 끝나는 시점. 이 전에 로켓을 눕히면 안 된다.</summary>
        private const float LiftPhaseSeconds = 3.5f;

        [SerializeField, Min(0f)] private float climbAcceleration = 26f;
        [SerializeField, Min(0f)] private float leanDegrees = 12f;
        [SerializeField, Min(0f)] private float leanSeconds = 3.5f;

        private Rocket rocket;
        private Rigidbody body;
        private bool pinOnly;
        private Vector3 origin;
        private Quaternion baseRotation;
        private Vector3 leanAxis;
        private float elapsed;
        private bool flying;

        /// <summary>기울일 방향을 카메라 기준으로 잡는다 — 화면에서 옆으로 눕는 것이 보여야 한다.</summary>
        public static HappyEndingFlight Attach(Rocket target, Vector3 leanTowardWorld)
        {
            if (target == null) return null;

            var flight = target.gameObject.AddComponent<HappyEndingFlight>();
            flight.rocket = target;
            flight.body = target.GetComponent<Rigidbody>();
            flight.origin = target.transform.position;
            flight.baseRotation = target.transform.rotation;

            Vector3 flat = Vector3.ProjectOnPlane(leanTowardWorld, Vector3.up);
            flight.leanAxis = flat.sqrMagnitude > 0.0001f
                ? Vector3.Cross(Vector3.up, flat.normalized)
                : Vector3.right;

            return flight;
        }

        /// <summary>
        /// <c>Rocket.ReleaseLift()</c> 가 이륙 2.5초 뒤 스스로 kinematic 을 푼다. private 이라 막을
        /// 방법이 없으므로 매 스텝 되돌린다. 실행 순서가 보장되지 않아 FixedUpdate 와 LateUpdate 양쪽에서 한다.
        /// </summary>
        private void FixedUpdate() => Pin();

        /// <summary>
        /// 위치는 부르는 쪽이 쥐고, 이쪽은 물리만 계속 묶는다. 달 컷처럼 경로를 밖에서 그릴 때 쓴다 —
        /// 이 핀을 놓으면 <c>ReleaseLift()</c> 가 kinematic 을 풀어 로켓이 스스로 날아간다.
        /// </summary>
        public void PinPhysicsOnly() => pinOnly = true;

        private void LateUpdate()
        {
            Pin();

            if (pinOnly || rocket == null) return;
            // 홀드(클램프) 동안은 로켓이 제자리에 있어야 한다 — 그 구간의 그림은 원본이 만든다.
            if (!rocket.Launched || rocket.Holding) return;

            if (!flying)
            {
                flying = true;
                origin = transform.position;
                baseRotation = transform.rotation;
            }

            elapsed += Time.deltaTime;
            transform.position = origin + Vector3.up * Rise(elapsed);

            // 리프트 구간이 끝난 뒤에만 기울인다. 그 안에서 눕히면 Rocket 이 "위를 향한 엔진이 없다"고
            // 판단해 즉시 리프트를 풀어 버리고, 리프트 연기도 그 자리에서 끊긴다.
            if (leanDegrees > 0f && elapsed > LiftPhaseSeconds)
            {
                float t = Mathf.Clamp01((elapsed - LiftPhaseSeconds) / Mathf.Max(0.01f, leanSeconds));
                float angle = Mathf.SmoothStep(0f, leanDegrees, t);
                transform.rotation = Quaternion.AngleAxis(angle, leanAxis) * baseRotation;
            }
        }

        /// <summary>
        /// 이륙 후 상승 높이. 앞 <see cref="AssistSeconds"/> 초는 인게임 보조 상승과 같은 식이라
        /// 그림이 겹치고, 그 뒤는 속도가 이어지도록 등가속으로 붙인다.
        /// </summary>
        private float Rise(float t)
        {
            if (t <= AssistSeconds)
            {
                float p = t / AssistSeconds;
                return AssistHeight * p * p;
            }

            float handoffSpeed = 2f * AssistHeight / AssistSeconds; // 보조 상승 종료 속도
            float d = t - AssistSeconds;
            return AssistHeight + handoffSpeed * d + 0.5f * climbAcceleration * d * d;
        }

        private void Pin()
        {
            if (body == null) return;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }
}
