using UnityEngine;
using UnityEngine.EventSystems;

namespace Simulation
{
    public sealed class DesignTesterPresetEntry : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private EngineStatsSO preset;
        private RocketDesignUI owner;
        public void SetPreset(EngineStatsSO value) => preset = value;
        public void Bind(RocketDesignUI value) => owner = value;
        public void OnPointerEnter(PointerEventData data) => owner.ShowStats(preset, (RectTransform)transform);
        public void OnPointerExit(PointerEventData data) => owner.HideStats();
        public void OnBeginDrag(PointerEventData data) => owner.BeginPresetDrag(preset, data.position);
        public void OnDrag(PointerEventData data) { }
    }
}
