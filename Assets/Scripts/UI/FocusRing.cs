using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Visible keyboard focus: draws a white outline around a control while it is the selected UI element.
    /// </summary>
    public class FocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public static readonly Color RingColor = Color.white;

        private GameObject _ring;

        private void Awake() => Ensure();

        private Outline _fallback;

        private void Ensure()
        {
            if (_ring != null || _fallback != null) return;
            if (UIKit.Rounded == null)
            {
                // No UI sprites loaded in this scene: outline the control's own graphic instead.
                var graphic = GetComponent<Graphic>();
                if (graphic == null) return;
                _fallback = gameObject.AddComponent<Outline>();
                _fallback.effectColor = RingColor;
                _fallback.effectDistance = new Vector2(4, -4);
                _fallback.enabled = false;
                return;
            }
            var rt = UIKit.Fill("FocusRing", transform, -6f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIKit.Rounded;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.pixelsPerUnitMultiplier = 4f;
            img.color = RingColor;
            img.raycastTarget = false;
            _ring = rt.gameObject;
            _ring.SetActive(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
        }

        public void OnSelect(BaseEventData eventData) => Show(true);

        public void OnDeselect(BaseEventData eventData) => Show(false);

        private void OnDisable() => Show(false);

        private void Show(bool on)
        {
            Ensure();
            if (_ring != null) _ring.SetActive(on);
            if (_fallback != null) _fallback.enabled = on;
        }
    }
}
