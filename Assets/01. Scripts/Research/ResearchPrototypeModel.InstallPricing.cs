using System;

namespace Border.Research
{
    public sealed partial class ResearchPrototypeModel
    {
        public const double MaxEngineInstallMarkup = 0.20d;

        public int GetEngineInstallCost(EnginePresetId presetId)
        {
            return CalculateEngineInstallCost(GetEnginePreset(presetId), balanceConfig.EngineInstallCost);
        }

        public static int CalculateEngineInstallCost(EnginePresetState preset, int baseCost = EngineInstallCost)
        {
            double average = (preset.FuelCapacity + preset.Cooling + preset.MaxOutput + preset.IgnitionReliability) / 4d;
            // Raw stats keep the price monotonic even when a specialized build's balance score drops.
            double progress = Math.Max(0d, Math.Min(1d, (average - InitialEngineStat) / (100d - InitialEngineStat)));
            return (int)Math.Round(Math.Max(0, baseCost) * (1d + MaxEngineInstallMarkup * progress),
                MidpointRounding.AwayFromZero);
        }
    }
}
