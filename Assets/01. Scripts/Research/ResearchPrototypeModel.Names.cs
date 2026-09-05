using System.Globalization;
using System.Text;

namespace Border.Research
{
    public sealed partial class ResearchPrototypeModel
    {
        public string GetEnginePresetName(EnginePresetId id)
        {
            string custom = GetEnginePreset(id).CustomName;
            return string.IsNullOrWhiteSpace(custom) ? GetEnginePresetConfig(id).DisplayName : custom;
        }

        public bool RenameEnginePreset(EnginePresetId id, string name)
        {
            if (!IsEnginePresetUnlocked(id)) return false;
            string normalized = NormalizeEnginePresetName(name);
            if (normalized.Length == 0) return false;
            GetEnginePreset(id).CustomName = normalized;
            return true;
        }

        public static string NormalizeEnginePresetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var output = new StringBuilder();
            var elements = StringInfo.GetTextElementEnumerator(name.Trim().Normalize(NormalizationForm.FormC));
            int remaining = 24;
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (char.IsControl(element[0]) || element == "<" || element == ">") continue;
                int cost = element.Length == 1 && element[0] <= 0x7f ? 2 : 3;
                if (cost > remaining) break;
                output.Append(element);
                remaining -= cost;
            }
            return output.ToString().Trim();
        }
    }
}
