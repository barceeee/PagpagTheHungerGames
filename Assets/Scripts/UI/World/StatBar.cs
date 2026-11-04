using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>HUD meter (Health, Hunger, Fatigue). Pulses when critical.</summary>
    public class StatBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;
        [SerializeField] private Text valueText;

        private Color _baseColor;
        private bool _critical;

        private void Awake() => _baseColor = fillImage.color;

        public void SetValue(float value01, string valueLabel, bool critical)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            valueText.text = valueLabel;
            _critical = critical;
            label.color = critical ? new Color(1f, 0.45f, 0.4f) : new Color(0.93f, 0.92f, 0.88f);
            if (!critical) fillImage.color = _baseColor;
        }

        private void Update()
        {
            if (!_critical) return;
            var t = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            fillImage.color = Color.Lerp(new Color(0.35f, 0.05f, 0.05f), new Color(1f, 0.25f, 0.2f), t);
        }
    }
}
