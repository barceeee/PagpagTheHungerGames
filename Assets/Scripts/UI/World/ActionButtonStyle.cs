using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// High-contrast states for the "What will you do?" action buttons. Replaces the multiplied colour tint
    /// (which can't change text colour) with explicit colours for each state:
    /// normal gold / dark text (~9:1), hover bright gold, pressed deep gold, disabled grey with light text (~5.5:1).
    /// Keyboard focus is shown by <see cref="FocusRing"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ActionButtonStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public static readonly Color Normal = new Color(0.85f, 0.64f, 0.25f);
        public static readonly Color Hover = new Color(1f, 0.8f, 0.38f);
        public static readonly Color Pressed = new Color(0.68f, 0.5f, 0.18f);
        public static readonly Color Disabled = new Color(0.23f, 0.25f, 0.28f);
        public static readonly Color Text = new Color(0.08f, 0.08f, 0.1f);
        public static readonly Color DisabledText = new Color(0.72f, 0.74f, 0.77f);

        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Outline border;

        private Button _button;
        private bool _hover, _down;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.transition = Selectable.Transition.None;
        }

        public void OnPointerEnter(PointerEventData e) => _hover = true;
        public void OnPointerExit(PointerEventData e) { _hover = false; _down = false; }
        public void OnPointerDown(PointerEventData e) => _down = true;
        public void OnPointerUp(PointerEventData e) => _down = false;

        private void OnDisable() => _hover = _down = false;

        private void LateUpdate()
        {
            var enabled = _button.IsInteractable();
            background.color = !enabled ? Disabled : _down ? Pressed : _hover ? Hover : Normal;
            label.color = enabled ? Text : DisabledText;
            if (border != null) border.enabled = enabled;
            var s = enabled && _hover ? 1.04f : 1f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
