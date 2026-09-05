using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class IgnitionPolishTests
    {
        private GameObject host;
        private ResearchMiniGameController controller;
        private Button[] buttons;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Ignition Polish Test");
            controller = host.AddComponent<ResearchMiniGameController>();
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.IgnitionReliability, false, 80, _ => { });
            buttons = host.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("Igniter_")).OrderBy(b => b.name).ToArray();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        private int Inputs => (int)typeof(ResearchMiniGameController).GetField("ignitionTotalInputs", Private).GetValue(controller);
        private void Tick(float time) => controller.AdvanceTimeForTests(time);
        private void Ready()
        {
            Tick(0.5f);
            Tick(controller.GetIgnitionSequenceForTests().Length * 0.45f + 0.001f);
            Tick(0.5f);
        }

        [Test]
        public void StartAndBlackout_BlockClicksUntilExactlyHalfSecond()
        {
            var off = buttons.Select(b => b.image.color).ToArray();
            buttons[0].onClick.Invoke();
            Tick(0.499f);
            Assert.That(buttons.All(b => !b.interactable), Is.True);
            Assert.That(Inputs, Is.Zero);
            Tick(0.0011f);
            Assert.That(controller.GetStateTextForTests(), Does.StartWith("순서 보기"));
            Tick(0.901f);
            for (int i = 0; i < 4; i++) Assert.That(buttons[i].image.color, Is.EqualTo(off[i]));
            Tick(0.499f);
            buttons[0].onClick.Invoke();
            Assert.That(Inputs, Is.Zero);
            Tick(0.0011f);
            Assert.That(buttons.All(b => b.interactable), Is.True);
            for (int i = 0; i < 4; i++) Assert.That(buttons[i].image.color, Is.Not.EqualTo(off[i]));
        }

        [Test]
        public void Correct_LocksDuringPunch_ThenAdvancesAfterCompletedSequence()
        {
            Ready();
            int[] sequence = controller.GetIgnitionSequenceForTests();
            var rest = buttons[sequence[0]].transform.localScale;
            buttons[sequence[0]].onClick.Invoke();
            Assert.That(buttons[sequence[0]].transform.localScale, Is.EqualTo(rest * 0.94f));
            buttons[sequence[1]].onClick.Invoke();
            Assert.That(Inputs, Is.EqualTo(1));
            Tick(0.126f);
            buttons[sequence[1]].onClick.Invoke();
            Tick(0.126f);
            Assert.That(controller.GetStateTextForTests(), Does.StartWith("순서 보기 2/3"));
            Assert.That(buttons.All(b => !b.interactable), Is.True);
        }

        [Test]
        public void Wrong_BlinksAllButtons_BlocksExtraInput_ThenContinues()
        {
            Ready();
            int wrong = (controller.GetIgnitionSequenceForTests()[0] + 1) % 4;
            var lit = buttons.Select(b => b.image.color).ToArray();
            buttons[wrong].onClick.Invoke();
            Assert.That(controller.GetStateTextForTests(), Does.StartWith("틀렸어요"));
            Tick(0.101f);
            for (int i = 0; i < 4; i++) Assert.That(buttons[i].image.color, Is.Not.EqualTo(lit[i]));
            buttons[wrong].onClick.Invoke();
            Assert.That(Inputs, Is.EqualTo(1));
            Tick(0.1f);
            for (int i = 0; i < 4; i++) Assert.That(buttons[i].image.color, Is.EqualTo(lit[i]));
            Tick(0.4f);
            Assert.That(controller.GetStateTextForTests(), Does.StartWith("순서 보기 2/3"));
        }

        [Test]
        public void HideAndReuse_RestoreScaleAndReuseParticleLayers()
        {
            Ready();
            int correct = controller.GetIgnitionSequenceForTests()[0];
            var rest = buttons[correct].transform.localScale;
            buttons[correct].onClick.Invoke();
            controller.HideForReuse();
            Assert.That(buttons[correct].transform.localScale, Is.EqualTo(rest));
            host.SetActive(true);
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.IgnitionReliability, false, 80, _ => { });
            Assert.That(host.GetComponentsInChildren<ParticleSystem>(true), Has.Length.EqualTo(8));
            Assert.That(host.GetComponentsInChildren<IgnitionClickParticles>(true).All(p => p.GetComponent<CanvasRenderer>() != null), Is.True);
            Assert.That(host.GetComponentsInChildren<ParticleSystem>(true).All(p => !p.IsAlive()), Is.True);
            Assert.That(buttons.All(b => !b.interactable), Is.True);
            Assert.That(controller.GetStateTextForTests(), Does.StartWith("준비"));
        }
    }
}
