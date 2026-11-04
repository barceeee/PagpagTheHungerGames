using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// The location list as a drawer that slides in over the right edge of the world instead of taking
    /// permanent space. Closed by default. Opens from its toggle button (or when a place is clicked on the map),
    /// closes from its ✕ button or Escape. Width is capped to the screen so it fits on narrow devices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SideDrawer : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [Tooltip("Full width of the drawer on large screens.")]
        [SerializeField] private float width = 420f;
        [Tooltip("Never wider than this share of the screen.")]
        [SerializeField] private float maxScreenShare = 0.92f;
        [SerializeField] private float slideSpeed = 7f;

        private RectTransform _rt;
        private CanvasGroup _group;
        private float _t; // 0 = closed, 1 = open

        public bool IsOpen { get; private set; }
        public float OpenAmount => _t;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            if (toggleButton != null) toggleButton.onClick.AddListener(() => Toggle(true));
            if (closeButton != null) closeButton.onClick.AddListener(() => Close(true));
            Snap();
        }

        public void Open(bool focusInside = false)
        {
            if (IsOpen) return;
            IsOpen = true;
            if (focusInside && closeButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close(bool focusToggle = false)
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (focusToggle && toggleButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(toggleButton.gameObject);
        }

        public void Toggle(bool viaControl)
        {
            if (IsOpen) Close(viaControl);
            else Open(viaControl);
        }

        /// <summary>Jump straight to the current open/closed position (no animation).</summary>
        public void Snap()
        {
            _t = IsOpen ? 1f : 0f;
            Layout();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame && !ConfirmDialog.IsOpen) Close(true);
            _t = Mathf.MoveTowards(_t, IsOpen ? 1f : 0f, slideSpeed * Time.unscaledDeltaTime);
            Layout();
        }

        private void Layout()
        {
            if (_rt == null) return;
            var parent = _rt.parent as RectTransform;
            var w = width;
            if (parent != null && parent.rect.width > 1f) w = Mathf.Min(width, parent.rect.width * maxScreenShare);
            _rt.sizeDelta = new Vector2(w, _rt.sizeDelta.y);
            var eased = 1f - Mathf.Pow(1f - _t, 3f);
            _rt.anchoredPosition = new Vector2(Mathf.Lerp(w + 8f, 0f, eased), _rt.anchoredPosition.y);
            var visible = _t > 0.001f;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = IsOpen;
            _group.blocksRaycasts = visible;
            if (toggleButton != null) toggleButton.gameObject.SetActive(!IsOpen && _t < 0.5f);
        }
    }
}
