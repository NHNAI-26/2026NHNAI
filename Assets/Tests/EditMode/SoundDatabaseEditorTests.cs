using System.Collections.Generic;
using Border.Audio.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Border.Audio.Tests
{
    public sealed class SoundDatabaseEditorTests
    {
        private SoundDatabaseSO database;
        private AudioClip clip;

        [SetUp]
        public void SetUp()
        {
            SoundDatabaseSOEditor.StopPreview();
            database = ScriptableObject.CreateInstance<SoundDatabaseSO>();
            clip = AudioClip.Create("OriginalClip", 32, 1, 44100, false);
            SoundDatabaseTests.SetEntries(database,
                new List<BgmEntry> { new("ManualId", clip) },
                new List<SfxEntry>());
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            SoundDatabaseSOEditor.StopPreview();
            Undo.ClearAll();
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void SetIdFromClipName_ChangesIdOnlyWhenExplicitlyCalled()
        {
            clip.name = "RenamedBeforeClick";
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("ManualId"));

            Assert.That(SoundDatabaseSOEditor.SetIdFromClipName(database, false, 0), Is.True);
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("RenamedBeforeClick"));

            clip.name = "RenamedAfterClick";
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("RenamedBeforeClick"));
        }

        [Test]
        public void SetIdFromClipName_IsUndoable()
        {
            Assert.That(SoundDatabaseSOEditor.SetIdFromClipName(database, false, 0), Is.True);
            Undo.FlushUndoRecordObjects();
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("OriginalClip"));

            Undo.PerformUndo();
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("ManualId"));
        }

        [Test]
        public void SetIdFromClipName_RejectsMissingClipAndOutOfRangeIndex()
        {
            SoundDatabaseTests.SetEntries(database,
                new List<BgmEntry> { new("KeepMe", null) },
                new List<SfxEntry>());

            Assert.That(SoundDatabaseSOEditor.SetIdFromClipName(database, false, 0), Is.False);
            Assert.That(SoundDatabaseSOEditor.SetIdFromClipName(database, false, 1), Is.False);
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("KeepMe"));
        }

        [TestCase("BGM", 0, "BattleTheme", "BGM 0 - BattleTheme")]
        [TestCase("SFX", 12, "", "SFX 12 - ")]
        public void FormatRowLabel_IncludesCategoryIndexAndCurrentId(
            string category, int index, string id, string expected)
        {
            Assert.That(SoundDatabaseSOEditor.FormatRowLabel(category, index, id), Is.EqualTo(expected));
        }

        [Test]
        public void DuplicateEntry_DuplicatesBgmCategory()
        {
            Assert.That(SoundDatabaseSOEditor.DuplicateEntry(database, false, 0), Is.True);
            Assert.That(database.BgmEntries.Count, Is.EqualTo(2));
            Assert.That(database.BgmEntries[0].Id, Is.EqualTo("ManualId"));
            Assert.That(database.BgmEntries[1].Id, Is.EqualTo("ManualId"));
            Assert.That(database.SfxEntries, Is.Empty);
        }

        [Test]
        public void DuplicateEntry_InsertsExpandedAdjacentCompleteSfxCopy()
        {
            var source = new SfxEntry("Spatial", clip, 0.35f, 1.25f, true, true, 2.5f, 40f);
            var tail = new SfxEntry("Tail", clip);
            SoundDatabaseTests.SetEntries(database,
                new List<BgmEntry>(), new List<SfxEntry> { source, tail });
            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("sfxEntries");
            entries.GetArrayElementAtIndex(0).isExpanded = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(SoundDatabaseSOEditor.DuplicateEntry(database, true, 0), Is.True);
            serialized.Update();
            entries = serialized.FindProperty("sfxEntries");

            Assert.That(database.SfxEntries.Count, Is.EqualTo(3));
            Assert.That(database.SfxEntries[0].Id, Is.EqualTo("Spatial"));
            Assert.That(database.SfxEntries[1].Id, Is.EqualTo("Spatial"));
            Assert.That(database.SfxEntries[2].Id, Is.EqualTo("Tail"));
            SfxEntry copy = database.SfxEntries[1];
            Assert.That(copy.Clip, Is.SameAs(clip));
            Assert.That(copy.Volume, Is.EqualTo(0.35f));
            Assert.That(copy.Pitch, Is.EqualTo(1.25f));
            Assert.That(copy.Loop, Is.True);
            Assert.That(copy.UseSpatialAudio, Is.True);
            Assert.That(copy.MinDistance, Is.EqualTo(2.5f));
            Assert.That(copy.MaxDistance, Is.EqualTo(40f));
            Assert.That(entries.GetArrayElementAtIndex(0).isExpanded, Is.False);
            Assert.That(entries.GetArrayElementAtIndex(1).isExpanded, Is.True);
            Assert.That(database.TryGetSfx("Spatial", out SfxEntry resolved), Is.True);
            Assert.That(resolved, Is.SameAs(database.SfxEntries[0]));
        }

        [Test]
        public void PreviewClip_ConfiguresRuntimeEquivalentTwoDimensionalSourceWithoutDirtyingScene()
        {
            bool wasSceneDirty = SceneManager.GetActiveScene().isDirty;

            Assert.That(SoundDatabaseSOEditor.PreviewClip(clip, 1.5f, -4f, true), Is.True);
            AudioSource source = SoundDatabaseSOEditor.ActivePreviewSource;

            Assert.That(source, Is.Not.Null);
            Assert.That(source.clip, Is.SameAs(clip));
            Assert.That(source.volume, Is.EqualTo(1f));
            Assert.That(source.pitch, Is.EqualTo(-3f));
            Assert.That(source.loop, Is.True);
            Assert.That(source.spatialBlend, Is.Zero);
            Assert.That(source.spatialize, Is.False);
            Assert.That(source.gameObject.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(wasSceneDirty));
        }

        [Test]
        public void PreviewClip_ReplacesExistingPreviewAndStopRemovesIt()
        {
            Assert.That(SoundDatabaseSOEditor.PreviewClip(clip, 0.25f, 1.25f, false), Is.True);
            AudioSource first = SoundDatabaseSOEditor.ActivePreviewSource;

            Assert.That(SoundDatabaseSOEditor.PreviewClip(clip, 0.75f, 2f, true), Is.True);
            AudioSource replacement = SoundDatabaseSOEditor.ActivePreviewSource;

            Assert.That(first == null, Is.True);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement, Is.Not.SameAs(first));
            Assert.That(replacement.volume, Is.EqualTo(0.75f));
            Assert.That(replacement.pitch, Is.EqualTo(2f));
            Assert.That(replacement.loop, Is.True);

            SoundDatabaseSOEditor.StopPreview();
            Assert.That(SoundDatabaseSOEditor.ActivePreviewSource, Is.Null);
            Assert.That(replacement == null, Is.True);
        }

        [Test]
        public void PreviewClip_RejectsMissingClipAndLeavesNoPreview()
        {
            Assert.That(SoundDatabaseSOEditor.PreviewClip(clip, 1f, 1f, false), Is.True);

            Assert.That(SoundDatabaseSOEditor.PreviewClip(null, 1f, 1f, false), Is.False);

            Assert.That(SoundDatabaseSOEditor.ActivePreviewSource, Is.Null);
        }
    }
}
