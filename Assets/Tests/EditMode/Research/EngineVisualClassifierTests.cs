using NUnit.Framework;

namespace Border.Research.Tests
{
    public sealed class EngineVisualClassifierTests
    {
        [Test]
        public void Classify_WhenStatsAreEven_ReturnsBalanced()
        {
            var engine = CreateEngine(40, 40, 40, 40);

            EngineVisualArchetype archetype = EngineVisualClassifier.Classify(engine);

            Assert.That(archetype, Is.EqualTo(EngineVisualArchetype.Balanced));
        }

        [Test]
        public void Classify_WhenOneStatLeadsByThreshold_ReturnsMatchingArchetype()
        {
            var engine = CreateEngine(40, 48, 39, 38);

            EngineVisualArchetype archetype = EngineVisualClassifier.Classify(engine);

            Assert.That(archetype, Is.EqualTo(EngineVisualArchetype.Cooling));
        }

        [Test]
        public void Classify_WhenLeadIsBelowThreshold_ReturnsBalanced()
        {
            var engine = CreateEngine(47, 40, 39, 38);

            EngineVisualArchetype archetype = EngineVisualClassifier.Classify(engine);

            Assert.That(archetype, Is.EqualTo(EngineVisualArchetype.Balanced));
        }

        [Test]
        public void Classify_WhenTopStatsAreTied_ReturnsBalanced()
        {
            var engine = CreateEngine(55, 55, 40, 38);

            EngineVisualArchetype archetype = EngineVisualClassifier.Classify(engine);

            Assert.That(archetype, Is.EqualTo(EngineVisualArchetype.Balanced));
        }

        [Test]
        public void Classify_WhenEngineIsNull_ReturnsBalanced()
        {
            EngineVisualArchetype archetype = EngineVisualClassifier.Classify(null);

            Assert.That(archetype, Is.EqualTo(EngineVisualArchetype.Balanced));
        }

        private static EnginePresetState CreateEngine(int fuelCapacity, int cooling, int maxOutput, int ignitionReliability)
        {
            return new EnginePresetState
            {
                FuelCapacity = fuelCapacity,
                Cooling = cooling,
                MaxOutput = maxOutput,
                IgnitionReliability = ignitionReliability
            };
        }
    }
}
