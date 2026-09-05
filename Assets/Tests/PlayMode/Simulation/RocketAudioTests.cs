using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class RocketAudioTests
    {
        private GameObject soundRoot;
        private GameObject host;
        private SoundDatabaseSO database;
        private EngineStatsSO stats;
        private readonly List<AudioClip> clips = new();
        private Rocket rocket;
        private RocketPart first;
        private RocketPart second;
        private BgmPlayer bgm;
        private GameObject skyHost;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SoundManager.Instance != null)
            {
                Object.Destroy(SoundManager.Instance.gameObject);
                yield return null;
            }
            soundRoot = new GameObject("rocket audio test manager");
            soundRoot.AddComponent<AudioListener>();
            var pool = soundRoot.AddComponent<SfxPool>();
            database = ScriptableObject.CreateInstance<SoundDatabaseSO>();
            var entries = new List<SfxEntry>();
            foreach (string id in new[] { "SparkStart", "RocketLaunch", "RocketLoop", "EngineStop" })
            {
                var clip = AudioClip.Create(id, 441000, 1, 44100, false);
                clips.Add(clip);
                entries.Add(new SfxEntry(id, clip, loop: id == "RocketLoop", useSpatialAudio: id == "RocketLoop"));
            }
            SetField(database, "sfxEntries", entries);
            var music = new List<BgmEntry>();
            foreach (string id in new[] { "EngineBGM", "LaunchPanelLoop", "Launch", "ToSpace" })
            {
                var clip = AudioClip.Create(id, 441000, 1, 44100, false);
                clips.Add(clip);
                music.Add(new BgmEntry(id, clip, loop: true));
            }
            SetField(database, "bgmEntries", music);
            database.RebuildLookup();
            bgm = soundRoot.AddComponent<BgmPlayer>();
            SetField(bgm, "sourceA", soundRoot.AddComponent<AudioSource>());
            SetField(bgm, "sourceB", soundRoot.AddComponent<AudioSource>());
            var manager = soundRoot.AddComponent<SoundManager>();
            SetField(manager, "database", database);
            SetField(manager, "sfxPool", pool);
            SetField(manager, "bgmPlayer", bgm);

            host = new GameObject("rocket audio test");
            host.transform.position = new Vector3(5000f, 20f, 5000f);
            rocket = host.AddComponent<Rocket>();
            SetField(rocket, "holdSeconds", 0.1f);
            stats = EngineStatsSO.CreateRuntimeCopy(-1, null, 0, 100f, 1000f, 1200f, 100f);
            first = CreateEngine("first");
            second = CreateEngine("second");
            var audio = host.AddComponent<RocketAudio>();
            SetField(audio, "engineStopInterval", 0.1f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) Object.Destroy(host);
            if (skyHost != null) Object.Destroy(skyHost);
            yield return null;
            if (soundRoot != null) Object.Destroy(soundRoot);
            if (database != null) Object.Destroy(database);
            if (stats != null) Object.Destroy(stats);
            foreach (AudioClip clip in clips) Object.Destroy(clip);
            clips.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaunchSequence_StopsOnlyExhaustedEngine_AlertsExactlyThreeTimes()
        {
            rocket.Launch();
            Assert.AreEqual(1, Sources("SparkStart").Length);
            Assert.AreEqual(0, Sources("RocketLoop").Length);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Sources("SparkStart").Length);
            Assert.AreEqual(1, Sources("RocketLaunch").Length);
            Assert.AreEqual(2, Sources("RocketLoop").Length);
            Assert.IsTrue(Sources("RocketLoop").All(s => s.spatialize && s.spatialBlend == 1f && s.loop));
            first.Tick(10000f);
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(1, Sources("RocketLoop").Length);
            Assert.Less(Vector3.Distance(Sources("RocketLoop")[0].transform.position, second.transform.position), 0.5f);
            Assert.AreEqual(3, Sources("EngineStop").Length);
            first.Shutdown();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(3, Sources("EngineStop").Length, "Repeated shutdown must not repeat the alert sequence.");
            second.Shutdown();
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(0, Sources("RocketLoop").Length);
            Assert.AreEqual(6, Sources("EngineStop").Length);
        }

        [UnityTest]
        public IEnumerator Reset_CancelsPendingAlerts_AndAllowsFreshLaunch()
        {
            rocket.Launch();
            yield return new WaitForSeconds(0.2f);
            first.Shutdown();
            yield return new WaitForSeconds(0.04f);
            Assert.AreEqual(1, Sources("EngineStop").Length);
            rocket.ResetFlight(Vector3.up * 20f, Quaternion.identity);
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(0, Sources("EngineStop").Length);
            Assert.AreEqual(0, Sources("RocketLoop").Length);
            rocket.Launch();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(2, Sources("RocketLoop").Length);
            host.SetActive(false);
            Assert.AreEqual(0, Sources("RocketLoop").Length);
        }

        private AudioSource[] Sources(string id) => soundRoot.GetComponentsInChildren<AudioSource>()
            .Where(s => s.clip != null && s.clip.name == id).ToArray();

        [UnityTest]
        public IEnumerator Music_TransitionsAtSpaceBoundary_StaysDuringDescent_ResetsForNextLaunch()
        {
            skyHost = new GameObject("music altitude test");
            skyHost.SetActive(false);
            var sky = skyHost.AddComponent<SkyEnvironment>();
            sky.enabled = false;
            SetField(sky, "target", host.transform);
            SetField(sky, "_zeroY", 20f);
            SetField(sky, "worldMetersPerUnit", 1f);
            SetField(sky, "spaceKm", 0.05f);
            SetField(sky, "_bound", true);
            skyHost.SetActive(true);
            yield return null;
            Assert.AreEqual("LaunchPanelLoop", bgm.CurrentId);

            rocket.Launch();
            host.GetComponent<Rigidbody>().isKinematic = true;
            Assert.AreEqual("Launch", bgm.CurrentId);
            host.transform.position = Vector3.up * 69f;
            yield return null;
            Assert.AreEqual("Launch", bgm.CurrentId);
            host.transform.position = Vector3.up * 70f;
            yield return null;
            Assert.AreEqual("ToSpace", bgm.CurrentId);
            var spaceSource = bgm.CurrentSource;
            host.transform.position = Vector3.up * 40f;
            yield return null;
            Assert.AreSame(spaceSource, bgm.CurrentSource);
            Assert.AreEqual("ToSpace", bgm.CurrentId);

            rocket.ResetFlight(Vector3.up * 20f, Quaternion.identity);
            yield return null;
            Assert.AreEqual("LaunchPanelLoop", bgm.CurrentId);
            rocket.Launch();
            Assert.AreEqual("Launch", bgm.CurrentId);
            SetField(sky, "_bound", false);
        }

        private RocketPart CreateEngine(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(host.transform, false);
            child.AddComponent<BoxCollider>();
            var engine = child.AddComponent<RocketPart>();
            engine.ApplyPreset(stats);
            return engine;
        }

        private static void SetField(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
