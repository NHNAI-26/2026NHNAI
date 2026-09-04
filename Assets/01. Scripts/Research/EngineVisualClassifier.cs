namespace Border.Research
{
    public static class EngineVisualClassifier
    {
        public const int SpecializationLeadThreshold = 8;

        private static readonly EngineStatId[] OrderedStats =
        {
            EngineStatId.FuelCapacity,
            EngineStatId.Cooling,
            EngineStatId.MaxOutput,
            EngineStatId.IgnitionReliability
        };

        public static EngineVisualArchetype Classify(EnginePresetState engine)
        {
            if (engine == null)
            {
                return EngineVisualArchetype.Balanced;
            }

            EngineStatId bestStat = OrderedStats[0];
            int bestValue = int.MinValue;
            int secondValue = int.MinValue;
            bool tiedBest = false;

            foreach (EngineStatId statId in OrderedStats)
            {
                int value = engine.GetStat(statId);
                if (value > bestValue)
                {
                    secondValue = bestValue;
                    bestValue = value;
                    bestStat = statId;
                    tiedBest = false;
                    continue;
                }

                if (value == bestValue)
                {
                    tiedBest = true;
                    secondValue = value;
                    continue;
                }

                if (value > secondValue)
                {
                    secondValue = value;
                }
            }

            if (tiedBest || bestValue - secondValue < SpecializationLeadThreshold)
            {
                return EngineVisualArchetype.Balanced;
            }

            return ToArchetype(bestStat);
        }

        private static EngineVisualArchetype ToArchetype(EngineStatId statId)
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    return EngineVisualArchetype.FuelCapacity;
                case EngineStatId.Cooling:
                    return EngineVisualArchetype.Cooling;
                case EngineStatId.MaxOutput:
                    return EngineVisualArchetype.MaxOutput;
                case EngineStatId.IgnitionReliability:
                    return EngineVisualArchetype.IgnitionReliability;
                default:
                    return EngineVisualArchetype.Balanced;
            }
        }
    }
}
