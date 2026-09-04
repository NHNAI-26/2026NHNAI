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
