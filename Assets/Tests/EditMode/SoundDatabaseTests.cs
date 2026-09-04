using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Border.Audio.Tests
{
    public sealed class SoundDatabaseTests
    {
        private readonly List<Object> disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object disposable in disposables)
            {
                Object.DestroyImmediate(disposable);
            }
        }

        [Test]
        public void Lookup_IsCategoryLocalAndCaseSensitive()
        {
            SoundDatabaseSO database = CreateDatabase();
            AudioClip bgmClip = CreateClip("Bgm");
            AudioClip sfxClip = CreateClip("Sfx");
            SetEntries(database,
                new List<BgmEntry> { new("Shared", bgmClip) },
                new List<SfxEntry> { new("Shared", sfxClip) });

            Assert.That(database.TryGetBgm("Shared", out BgmEntry bgm), Is.True);
            Assert.That(database.TryGetSfx("Shared", out SfxEntry sfx), Is.True);
            Assert.That(bgm.Clip, Is.SameAs(bgmClip));
            Assert.That(sfx.Clip, Is.SameAs(sfxClip));
            Assert.That(database.TryGetBgm("shared", out _), Is.False);
        }

        [Test]
        public void Lookup_SkipsInvalidRowsAndKeepsFirstValidDuplicate()
        {
            SoundDatabaseSO database = CreateDatabase();
            AudioClip first = CreateClip("First");
            AudioClip second = CreateClip("Second");
            SetEntries(database,
                new List<BgmEntry> { null, new(" ", first), new("Missing", null), new("Dup", first), new("Dup", second) },
                new List<SfxEntry> { new("Spatial", first, minDistance: 5f, maxDistance: 1f), new("Spatial", second) });

            Assert.That(database.TryGetBgm("Dup", out BgmEntry bgm), Is.True);
            Assert.That(bgm.Clip, Is.SameAs(first));
            Assert.That(database.TryGetBgm("Missing", out _), Is.False);
            Assert.That(database.TryGetSfx("Spatial", out SfxEntry sfx), Is.True);
            Assert.That(sfx.Clip, Is.SameAs(second));
        }

        [Test]
        public void Entries_PreserveDefaultsAndSpatialValues()
        {
            var bgm = new BgmEntry();
            var defaults = new SfxEntry();
            var custom = new SfxEntry("World", CreateClip("World"), .4f, 1.25f, true, true, 2f, 80f);

            Assert.That(bgm.Volume, Is.EqualTo(1f));
            Assert.That(bgm.Pitch, Is.EqualTo(1f));
            Assert.That(bgm.Loop, Is.False);
            Assert.That(defaults.UseSpatialAudio, Is.False);
            Assert.That(defaults.MinDistance, Is.EqualTo(1f));
            Assert.That(defaults.MaxDistance, Is.EqualTo(50f));
            Assert.That(custom.UseSpatialAudio, Is.True);
            Assert.That(custom.MinDistance, Is.EqualTo(2f));
            Assert.That(custom.MaxDistance, Is.EqualTo(80f));
            Assert.That(custom.Loop, Is.True);
            Assert.That(custom.Volume, Is.EqualTo(.4f));
            Assert.That(custom.Pitch, Is.EqualTo(1.25f));
        }

        private SoundDatabaseSO CreateDatabase()
        {
            var database = ScriptableObject.CreateInstance<SoundDatabaseSO>();
            disposables.Add(database);
            return database;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 32, 1, 44100, false);
            disposables.Add(clip);
            return clip;
        }

        internal static void SetEntries(SoundDatabaseSO database, List<BgmEntry> bgm, List<SfxEntry> sfx)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SoundDatabaseSO).GetField("bgmEntries", Flags)?.SetValue(database, bgm);
            typeof(SoundDatabaseSO).GetField("sfxEntries", Flags)?.SetValue(database, sfx);
            database.RebuildLookup();
        }
    }
}
