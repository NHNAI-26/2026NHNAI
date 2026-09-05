using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Simulation.Tests
{
    public sealed class SkyEnvironmentTests
    {
        private readonly List<Object> _spawned = new();
        private Material _skyboxBefore;
        private bool _fogBefore;

        [SetUp]
        public void SetUp()
        {
            _skyboxBefore = RenderSettings.skybox;
            _fogBefore = RenderSettings.fog;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);

            _spawned.Clear();

            // Bind 이 손댄 전역 렌더 설정은 OnDestroy 가 되돌리지만, 테스트가 실패해 중간에 끊겨도
            // 에디터의 현재 씬이 안개투성이로 남지 않게 여기서 한 번 더 못을 박는다.
            RenderSettings.skybox = _skyboxBefore;
            RenderSettings.fog = _fogBefore;
        }

        [Test]
        public void SkyboxShader_CompilesAndKeepsDrivenProperties()
        {
            Shader shader = Shader.Find("Sky/AtmosphereNebulaBlend");
            Assert.IsNotNull(shader, "셰이더가 사라지면 하늘이 분홍색이 된다.");
            Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(shader), "스카이박스 셰이더가 컴파일에 실패했다.");

            Material material = new(shader);
            _spawned.Add(material);

            // Material.SetColor/SetFloat 는 없는 이름에도 예외를 던지지 않는다. 오타 하나면 고도별
            // 하늘이 통째로 얼어붙는데 로그도 안 남으므로, Update 가 쓰는 이름을 여기서 못 박는다.
            foreach (string property in new[]
                     { "_SkyTint", "_HorizonColor", "_Exposure", "_AtmosphereThickness", "_SpaceBlend" })
                Assert.IsTrue(material.HasProperty(property), property);
        }

        [Test]
        public void AltitudeKm_ScalesWorldUnitsByMetersPerUnit()
        {
            SkyEnvironment sky = Create(out Transform target, out _, worldMetersPerUnit: 250f);
            float padY = target.position.y;

            Assert.AreEqual(0f, sky.AltitudeKm, 1e-3f, "발사대 높이가 고도 0 이어야 한다.");

            // 프로토타입 비행의 정점(발사대 +400 유닛) = 1 유닛당 250 m 로 읽으면 100 km.
            target.position = new Vector3(0f, padY + 400f, 0f);
            Assert.AreEqual(100f, sky.AltitudeKm, 1e-2f);

            // 실제 스케일 비행이 붙으면 이 값만 1 로 바꾸면 되는 것이 요점이다.
            SetField(sky, "worldMetersPerUnit", 1f);
            Assert.AreEqual(0.4f, sky.AltitudeKm, 1e-3f);
        }

        [Test]
        public void Bind_UsesSkyboxCopy_AndRestoresOnDestroy()
        {
            Material source = new(Shader.Find("Skybox/Procedural"));
            _spawned.Add(source);

            Material before = RenderSettings.skybox;
            SkyEnvironment sky = Create(out _, out _, worldMetersPerUnit: 250f, skybox: source);

            Assert.AreNotSame(source, RenderSettings.skybox,
                "공유 에셋을 그대로 넘기면 런타임 변경이 .mat 에 눌어붙는다. 복사본이어야 한다.");
            Assert.IsTrue(RenderSettings.fog, "고도 안개를 쓰려면 Bind 가 안개를 켜야 한다.");

            sky.Unbind(); // 런타임에서는 OnDestroy 가 이걸 부른다. EditMode 에서는 OnDestroy 가 돌지 않는다.

            Assert.AreSame(before, RenderSettings.skybox, "정리 후에는 원래 하늘로 돌아와야 한다.");
            Assert.AreEqual(_fogBefore, RenderSettings.fog, "안개 설정도 원래대로 돌아와야 한다.");
        }

        [Test]
        public void CurvatureRadius_PutsHorizonAtRequestedDistance()
        {
            const float horizon = 450f;

            // 역산의 정의 그대로: 그 반지름 위에서 지평선 거리만큼 떨어진 점의 낙차가 곧 눈높이다.
            // 그보다 먼 수면은 눈높이 아래로 내려가 화면에서 사라진다.
            foreach (float height in new[] { 10f, 100f, 289f })
            {
                float radius = SkyEnvironment.CurvatureRadius(horizon, height);
                Assert.AreEqual(height, SkyEnvironment.SphereDrop(horizon, radius), height * 1e-3f,
                    $"높이 {height} 에서 지평선이 {horizon} 에 서지 않는다.");
            }

            Assert.Greater(SkyEnvironment.CurvatureRadius(horizon, 10f),
                SkyEnvironment.CurvatureRadius(horizon, 289f),
                "올라갈수록 반지름이 줄어야 곡률이 보인다.");

            Assert.IsTrue(float.IsInfinity(SkyEnvironment.CurvatureRadius(horizon, 0f)),
                "수면 높이에서는 굽히지 않는다 — 무한대 = 평면.");
            Assert.IsTrue(float.IsInfinity(SkyEnvironment.CurvatureRadius(horizon, -5f)),
                "수면 아래로 내려가도 뒤집힌 곡률이 나오면 안 된다.");
        }

        [Test]
        public void SphereDrop_IsZeroAtCenter_AndStopsAtEquator()
        {
            Assert.AreEqual(0f, SkyEnvironment.SphereDrop(0f, 1000f), 1e-4f,
                "꼭대기는 평면 높이 그대로다 — 발사대가 물에 잠기면 안 된다.");
            Assert.AreEqual(0f, SkyEnvironment.SphereDrop(500f, float.PositiveInfinity), 1e-4f,
                "무한 반지름은 평면이다.");

            // 수면 반폭 1050 은 고고도 반지름을 넘어선다. 근사였다면 여기서 발산한다.
            Assert.AreEqual(848f, SkyEnvironment.SphereDrop(1050f, 848f), 1e-3f,
                "반지름을 넘어선 정점은 적도에서 멈춰야 한다.");
            Assert.AreEqual(848f, SkyEnvironment.SphereDrop(5000f, 848f), 1e-3f,
                "훨씬 밖에서도 적도 아래로는 내려가지 않는다.");

            Assert.Less(SkyEnvironment.SphereDrop(300f, 848f),
                SkyEnvironment.SphereDrop(600f, 848f), "멀수록 더 내려간다.");
        }

        [Test]
        public void StarLag_IsZeroBelowSpace_AndParksTheShellAbove()
        {
            Assert.AreEqual(0f, SkyEnvironment.StarLagUnits(69f, 70f, 250f, 1f), 1e-4f,
                "우주 아래에서는 별이 카메라에 붙어 있어야 한다 — 경계에서 껍질이 튀면 안 된다.");

            // 정점 434 유닛 = 108.5 km, spaceKm 70 = 280 유닛. 계수 1 이면 껍질이 우주 진입점에 그대로 선다.
            Assert.AreEqual(154f, SkyEnvironment.StarLagUnits(108.5f, 70f, 250f, 1f), 1e-2f);
            Assert.AreEqual(77f, SkyEnvironment.StarLagUnits(108.5f, 70f, 250f, 0.5f), 1e-2f,
                "계수는 시차를 선형으로 줄인다.");

            // 껍질 반지름 400(프리팹 ShapeModule). 넘어서면 카메라가 별 바깥으로 나가 천정이 빈다.
            Assert.Less(SkyEnvironment.StarLagUnits(108.5f, 70f, 250f, 1f), 400f);
        }

        private SkyEnvironment Create(out Transform target, out GameObject root,
            float worldMetersPerUnit, Material skybox = null)
        {
            root = new GameObject("SkyEnvironment");
            _spawned.Add(root);

            GameObject rocket = new("Rocket");
            rocket.transform.position = new Vector3(0f, 2f, 0f); // 씬의 발사대 높이와 같은 의미
            _spawned.Add(rocket);
            target = rocket.transform;

            GameObject cameraObject = new("Camera");
            Camera cam = cameraObject.AddComponent<Camera>();
            _spawned.Add(cameraObject);

            SkyEnvironment sky = root.AddComponent<SkyEnvironment>();
            SetField(sky, "target", target);
            SetField(sky, "cam", cam);
            SetField(sky, "worldMetersPerUnit", worldMetersPerUnit);
            SetField(sky, "skyboxSource", skybox);
            sky.Bind(); // EditMode 에서는 Awake 가 돌지 않는다
            return sky;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
