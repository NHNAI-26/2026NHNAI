using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Border.Audio.Tests
{
    public sealed class SoundManagerTests
    {
        private readonly List<AudioClip> clips = new();
        private GameObject root;
        private SoundDatabaseSO database;
        private SoundManager manager;
        private BgmPlayer bgmPlayer;
        private SfxPool sfxPool;
        private AudioClip bgmA;
        private AudioClip bgmB;
        private AudioClip sfx2D;
        private AudioClip sfxSpatial;
        private AudioClip sfxAttached;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SoundManager.Instance != null)
            {
                Object.Destroy(SoundManager.Instance.gameObject);
                yield return null;
            }

            root = new GameObject("Sound Manager Test Root");
            root.AddComponent<AudioListener>();
            database = ScriptableObject.CreateInstance<SoundDatabaseSO>();

            GameObject bgmObject = CreateChild("BGM Player");
            bgmPlayer = bgmObject.AddComponent<BgmPlayer>();
            AudioSource sourceA = CreateChild("BGM A", bgmObject.transform).AddComponent<AudioSource>();
            AudioSource sourceB = CreateChild("BGM B", bgmObject.transform).AddComponent<AudioSource>();
            SetField(bgmPlayer, "sourceA", sourceA);
            SetField(bgmPlayer, "sourceB", sourceB);

            GameObject poolObject = CreateChild("SFX Pool");
            sfxPool = poolObject.AddComponent<SfxPool>();
            var volumeController = CreateChild("Volume Controller").AddComponent<AudioMixerVolumeController>();

            manager = root.AddComponent<SoundManager>();
            SetField(manager, "database", database);
            SetField(manager, "bgmPlayer", bgmPlayer);
            SetField(manager, "sfxPool", sfxPool);
            SetField(manager, "volumeController", volumeController);

            bgmA = CreateClip("BgmA");
            bgmB = CreateClip("BgmB");
            sfx2D = CreateClip("Sfx2D");
            sfxSpatial = CreateClip("SfxSpatial");
            sfxAttached = CreateClip("SfxAttached");
            SetEntries(
                new List<BgmEntry>
                {
                    new("BgmA", bgmA, .8f, 1f, true),
                    new("BgmB", bgmB, .6f, 1.1f, true)
                },
                new List<SfxEntry>
                {
                    new("2D", sfx2D, .7f, 1f, true),
                    new("Spatial", sfxSpatial, .8f, 1f, true, true, 2f, 60f),
                    new("Attached", sfxAttached, .9f, 1f, true, true, 1f, 30f)
                });
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            if (database != null) Object.Destroy(database);
            foreach (AudioClip clip in clips)
            {
                if (clip != null) Object.Destroy(clip);
            }

            clips.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bgm_ImmediateRepeatCrossfadeRapidReplacementAndStop()
        {
            Assert.That(manager.PlayBgm("BgmA", 0f), Is.True);
            AudioSource first = bgmPlayer.CurrentSource;
            Assert.That(first, Is.Not.Null);
            Assert.That(first.clip, Is.SameAs(bgmA));
            Assert.That(first.volume, Is.EqualTo(.8f).Within(.001f));
            Assert.That(first.pitch, Is.EqualTo(1f).Within(.001f));
            Assert.That(first.loop, Is.True);
            Assert.That(first.spatialBlend, Is.Zero);
            Assert.That(first.spatialize, Is.False);

            Assert.That(manager.PlayBgm("BgmA", .1f), Is.True);
            Assert.That(bgmPlayer.CurrentSource, Is.SameAs(first));
            LogAssert.Expect(LogType.Warning, "[SoundManager] Unknown or invalid BGM ID 'Missing'.");
            Assert.That(manager.PlayBgm("Missing", 0f), Is.False);
            Assert.That(bgmPlayer.CurrentSource, Is.SameAs(first));

            Assert.That(manager.PlayBgm("BgmB", .03f), Is.True);
            Assert.That(PlayingBgmSources(), Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(bgmPlayer.CurrentSource.clip, Is.SameAs(bgmB));
            Assert.That(bgmPlayer.CurrentSource.volume, Is.EqualTo(.6f).Within(.02f));

            AudioSource originalB = bgmPlayer.CurrentSource;
            Assert.That(manager.PlayBgm("BgmA", .1f), Is.True);
            AudioSource incomingA = bgmPlayer.CurrentSource;
            yield return new WaitForSecondsRealtime(.01f);
            int originalSample = originalB.timeSamples;
            Assert.That(manager.PlayBgm("BgmB", .02f), Is.True);
            Assert.That(bgmPlayer.CurrentSource, Is.SameAs(originalB));
            Assert.That(originalB.timeSamples, Is.GreaterThanOrEqualTo(originalSample));
            Assert.That(incomingA.clip, Is.SameAs(bgmA));
            Assert.That(incomingA.isPlaying, Is.True);
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(bgmPlayer.CurrentSource.clip, Is.SameAs(bgmB));
            Assert.That(PlayingBgmSources(), Is.EqualTo(1));

            manager.StopBgm(.02f);
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(PlayingBgmSources(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator Sfx_Configures2DFixedAndAttachedPlayback()
        {
            SoundHandle flat = manager.PlaySfx("2D");
            Assert.That(flat.IsValid, Is.True);
            AudioSource flatSource = FindVoice(sfx2D);
            Assert.That(flatSource.spatialBlend, Is.Zero);
            Assert.That(flatSource.spatialize, Is.False);
            Assert.That(flatSource.volume, Is.EqualTo(.7f).Within(.001f));

            LogAssert.Expect(LogType.Warning, "[SoundManager] Spatial SFX 'Spatial' requires a position or Transform.");
            Assert.That(manager.PlaySfx("Spatial").IsValid, Is.False);

            ExpectSpatializerWarningIfNeeded();
            Vector3 fixedPosition = new(4f, 5f, 6f);
            SoundHandle fixedHandle = manager.PlaySfxAt("Spatial", fixedPosition);
            Assert.That(fixedHandle.IsValid, Is.True);
            AudioSource fixedSource = FindVoice(sfxSpatial);
            Assert.That(fixedSource.transform.position, Is.EqualTo(fixedPosition));
            Assert.That(fixedSource.spatialBlend, Is.EqualTo(1f));
            Assert.That(fixedSource.spatialize, Is.True);
            Assert.That(fixedSource.minDistance, Is.EqualTo(2f));
            Assert.That(fixedSource.maxDistance, Is.EqualTo(60f));

            GameObject targetObject = CreateChild("Follow Target");
            targetObject.transform.position = new Vector3(1f, 2f, 3f);
            SoundHandle attached = manager.PlaySfxAttached("Attached", targetObject.transform);
            Assert.That(attached.IsValid, Is.True);
            AudioSource attachedSource = FindVoice(sfxAttached);
            targetObject.transform.position = new Vector3(8f, 2f, -1f);
            yield return null;
            Assert.That(attachedSource.transform.position, Is.EqualTo(targetObject.transform.position));

            Object.Destroy(targetObject);
            yield return null;
            yield return null;
            Assert.That(attached.IsValid, Is.False);
            flat.Stop();
            fixedHandle.Stop();
        }

        [Test]
        public void Handle_PreventsStaleControlAfterVoiceReuseAndAppliesMutators()
        {
            SoundHandle stale = manager.PlaySfx("2D");
            AudioSource firstSource = FindVoice(sfx2D);
            stale.Stop();
            Assert.That(stale.IsValid, Is.False);

            SoundHandle current = manager.PlaySfx("2D");
            AudioSource reusedSource = FindVoice(sfx2D);
            Assert.That(reusedSource, Is.SameAs(firstSource));
            current.SetVolume(.25f);
            current.SetPitch(.75f);
            current.SetLoop(false);
            stale.SetVolume(.95f);
            stale.SetPitch(2f);
            stale.SetLoop(true);

            Assert.That(reusedSource.volume, Is.EqualTo(.25f).Within(.001f));
            Assert.That(reusedSource.pitch, Is.EqualTo(.75f).Within(.001f));
            Assert.That(reusedSource.loop, Is.False);
            Assert.That(current.IsValid, Is.True);
            Assert.That(current.IsPlaying, Is.True);
            current.Stop();
            Assert.That(current.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator Handle_FadeStopCannotBeCancelledBySetVolume()
        {
            SoundHandle handle = manager.PlaySfx("2D");
            handle.Stop(.03f);
            handle.SetVolume(.95f);

            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(sfxPool.CountActive, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Handle_SetVolumeCancelsFadeInAndAppliesClampedVolume()
        {
            SoundHandle handle = manager.PlaySfx("2D", .2f);
            AudioSource source = FindVoice(sfx2D);
            handle.SetVolume(2f);

            Assert.That(source.volume, Is.EqualTo(1f).Within(.001f));
            Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(source.volume, Is.EqualTo(1f).Within(.001f));
            Assert.That(handle.IsValid, Is.True);
            handle.Stop();
        }

        [Test]
        public void Pool_GrowsBeyondPrewarmThenRetainsAtMostThirtyTwoAndReuses()
        {
            var handles = new List<SoundHandle>();
            for (int index = 0; index < 40; index++)
            {
                handles.Add(manager.PlaySfx("2D"));
            }

            Assert.That(sfxPool.CountActive, Is.EqualTo(40));
            Assert.That(sfxPool.CountAll, Is.GreaterThanOrEqualTo(40));
            foreach (SoundHandle handle in handles) handle.Stop();
            Assert.That(sfxPool.CountActive, Is.Zero);
            Assert.That(sfxPool.CountInactive, Is.LessThanOrEqualTo(32));

            int retained = sfxPool.CountAll;
            SoundHandle reused = manager.PlaySfx("2D");
            Assert.That(sfxPool.CountAll, Is.EqualTo(retained));
            reused.Stop();
        }

        [Test]
        public void VolumeConversion_ClampsAndMapsExpectedDecibels()
        {
            Assert.That(AudioMixerVolumeController.LinearToDecibels(-1f), Is.EqualTo(-80f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(0f), Is.EqualTo(-80f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(.5f), Is.EqualTo(-6.0206f).Within(.001f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(1f), Is.EqualTo(0f).Within(.001f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(2f), Is.EqualTo(0f).Within(.001f));
        }

        private void SetEntries(List<BgmEntry> bgm, List<SfxEntry> sfx)
        {
            SetField(database, "bgmEntries", bgm);
            SetField(database, "sfxEntries", sfx);
            database.RebuildLookup();
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 88200, 1, 44100, false);
            clips.Add(clip);
            return clip;
        }

        private GameObject CreateChild(string name, Transform parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent != null ? parent : root.transform, false);
            return child;
        }

        private AudioSource FindVoice(AudioClip clip)
        {
            foreach (AudioSource source in sfxPool.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.clip == clip) return source;
            }

            Assert.Fail($"No pooled voice is using clip '{clip.name}'.");
            return null;
        }

        private int PlayingBgmSources()
        {
            int count = 0;
            foreach (AudioSource source in bgmPlayer.GetComponentsInChildren<AudioSource>())
            {
                if (source.isPlaying) count++;
            }

            return count;
        }

        private void ExpectSpatializerWarningIfNeeded()
        {
            if (!string.Equals(AudioSettings.GetSpatializerPluginName(), "Steam Audio Spatializer", StringComparison.Ordinal))
            {
                LogAssert.Expect(LogType.Warning,
                    "[SoundManager] Steam Audio Spatializer is not selected; spatial playback will use Unity's available path.");
            }
        }

        private static void SetField(object instance, string name, object value)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = instance.GetType().GetField(name, Flags);
            Assert.That(field, Is.Not.Null, $"Missing serialized field '{name}'.");
            field.SetValue(instance, value);
        }
    }
}
