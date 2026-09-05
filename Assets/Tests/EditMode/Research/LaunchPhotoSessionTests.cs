using NUnit.Framework;
using UnityEngine;

namespace Border.Research.Tests
{
    public sealed class LaunchPhotoSessionTests
    {
        [SetUp]
        public void SetUp() => ResearchFlowSession.ResetForTests();

        [TearDown]
        public void TearDown() => ResearchFlowSession.ResetForTests();

        [Test]
        public void LaunchPhoto_AcceptsOnlyCurrentGeneration()
        {
            ResearchFlowSession session = BeginLaunch();
            int generation = session.LaunchPhotoGeneration;
            Texture2D stale = CreatePhoto("stale");
            Texture2D current = CreatePhoto("current");

            session.SetLaunchPhoto(stale, generation - 1);
            Assert.That(session.LaunchPhoto, Is.Null);
            Assert.That(stale == null, Is.True);

            session.SetLaunchPhoto(current, generation);
            Assert.That(session.LaunchPhoto, Is.SameAs(current));
            Assert.That(current == null, Is.False);
        }

        [Test]
        public void LaunchPhoto_ClearsOnAcknowledgementAndReset()
        {
            ResearchFlowSession session = BeginLaunch();
            Texture2D acknowledged = CreatePhoto("acknowledged");
            session.SetLaunchPhoto(acknowledged, session.LaunchPhotoGeneration);
            Assert.That(session.CompleteActiveLaunch(false, out _), Is.EqualTo(ResearchActionResult.Success));

            session.AcknowledgeLaunchResult();
            Assert.That(session.LaunchPhoto, Is.Null);

            BeginLaunch(session);
            Texture2D reset = CreatePhoto("reset");
            session.SetLaunchPhoto(reset, session.LaunchPhotoGeneration);
            session.ResetResearch();
            Assert.That(session.LaunchPhoto, Is.Null);
        }

        [Test]
        public void NewLaunch_ClearsPreviousPhotoAndRejectsPreviousGeneration()
        {
            ResearchFlowSession session = BeginLaunch();
            int firstGeneration = session.LaunchPhotoGeneration;
            Texture2D first = CreatePhoto("first");
            session.SetLaunchPhoto(first, firstGeneration);
            Assert.That(session.CompleteActiveLaunch(true, out _), Is.EqualTo(ResearchActionResult.Success));
            session.AcknowledgeLaunchResult();

            BeginLaunch(session);
            Assert.That(session.LaunchPhotoGeneration, Is.GreaterThan(firstGeneration));
            Assert.That(session.LaunchPhoto, Is.Null);
            Assert.That(first == null, Is.True);

            Texture2D stale = CreatePhoto("stale after new launch");
            session.SetLaunchPhoto(stale, firstGeneration);
            Assert.That(session.LaunchPhoto, Is.Null);
            Assert.That(stale == null, Is.True);
        }

        private static ResearchFlowSession BeginLaunch()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            BeginLaunch(session);
            return session;
        }

        private static void BeginLaunch(ResearchFlowSession session)
        {
            // 새 게임은 프리셋 0개로 시작한다 — 설계에 들어가려면 먼저 하나 만들어야 한다.
            session.Model.CreateNewEnginePreset(out _);
            Assert.That(session.TryEnterDesign(LaunchMissionId.LowAltitude, out _), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasActiveLaunch, Is.True);
        }

        private static Texture2D CreatePhoto(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = name };
            texture.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            texture.Apply(false, false);
            return texture;
        }
    }
}
