using System.Collections.Generic;
using Border.Core;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 저장된 엔진 프리셋 슬롯 목록. 최대 개수를 강제하는 단일 지점이라 슬롯 상한이 여기서만 검사된다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnginePresetLibrary", menuName = "Simulation/Engine Preset Library")]
    public sealed class EnginePresetLibrarySO : ScriptableObject
    {
        public const int MaxSlots = 10;

        [SerializeField] private List<EngineStatsSO> slots = new();

        public IReadOnlyList<EngineStatsSO> Slots => slots;

        public static EnginePresetLibrarySO CreateRuntime(IReadOnlyList<EngineStatsSO> runtimeSlots)
        {
            EnginePresetLibrarySO library = CreateInstance<EnginePresetLibrarySO>();
            library.hideFlags = HideFlags.DontSave;
            library.name = "EnginePresetLibrary_Runtime";
            library.slots = new List<EngineStatsSO>();

            if (runtimeSlots == null)
            {
                return library;
            }

            int count = Mathf.Min(runtimeSlots.Count, MaxSlots);
            for (int i = 0; i < count; i++)
            {
                library.slots.Add(runtimeSlots[i]);
            }

            return library;
        }

        private void OnValidate()
        {
            if (slots.Count <= MaxSlots) return;

            Log.W($"Engine preset slots capped at {MaxSlots}; dropped {slots.Count - MaxSlots}", this);
            slots.RemoveRange(MaxSlots, slots.Count - MaxSlots);
        }
    }
}
