using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Modal confirmation, laid out like the main menu's new-game confirmation.
    /// Confirm runs the action; Cancel, a click on the dimmed backdrop, or Escape closes it.
    /// <see cref="ShowConversation"/> is the same modal as a short exchange with someone (the speaker as title,
    /// the player's line and the reply as the body) before an everyday action; focus then starts on Confirm.
    /// Keyboard: focus starts on Cancel (the safe choice), ←/→ or Tab move between the buttons,
    /// Enter/Space activates, and focus stays inside the dialog until it closes, then returns where it was.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        private static readonly Color ConfirmColor = new Color(0.55f, 0.16f, 0.16f);
        private static readonly Vector2 ConfirmSize = new Vector2(860, 400);
        private static readonly Vector2 ConversationSize = new Vector2(1000, 480);

        private RectTransform _root;
        private Text _title, _body, _confirmLabel, _cancelLabel;
        private Button _confirm, _cancel;
        private RectTransform _frame;
        private FitInParent _fit;
        private Image _confirmBg;
        private Action _onConfirm;
        private GameObject _previous;
        private bool _open;
        private bool _focusConfirm;

        private void Awake()
        {
            Instance = this;
            _root = UIKit.Fill("Overlay", transform);
            Build();
            _root.gameObject.SetActive(false);
        }

        private void Build()
        {
            // The backdrop is a sibling of the dialog (not its parent), so clicks on the dialog never reach it.
            var backdropRt = UIKit.Fill("Backdrop", _root);
            var backdrop = UIKit.Img(backdropRt, new Color(0, 0, 0, 0.78f), false, true);
            var backdropButton = backdropRt.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };
            backdropButton.onClick.AddListener(Cancel);

            var frame = _frame = UIKit.Rect("Dialog", _root, Vector2.zero, ConfirmSize);
            var fit = _fit = frame.gameObject.AddComponent<FitInParent>();
            fit.designSize = ConfirmSize;
            fit.reserved = new Vector2(48, 48);
            UIKit.Img(frame, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.45f), true, true);
            var fill = UIKit.Fill("Fill", frame, 2);
            UIKit.Img(fill, UIKit.Panel, true, true);

            _title = UIKit.Label("Title", fill, new Vector2(0, -30), new Vector2(780, 60), "", 38, UIKit.Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.5f, 1f), true);
            _body = UIKit.Label("Body", fill, new Vector2(0, 20), new Vector2(760, 150), "", 28, UIKit.Cream,
                TextAnchor.MiddleCenter, FontStyle.Normal, null, true);
            _confirm = UIKit.Btn("ConfirmButton", fill, new Vector2(-170, 40), new Vector2(300, 76), "", Confirm,
                ConfirmColor, UIKit.Cream, 30, new Vector2(0.5f, 0f));
            _cancel = UIKit.Btn("CancelButton", fill, new Vector2(170, 40), new Vector2(300, 76), "", Cancel,
                UIKit.ButtonColor, UIKit.Cream, 30, new Vector2(0.5f, 0f));
            _confirmLabel = UIKit.LabelOf(_confirm);
            _confirmBg = (Image)_confirm.targetGraphic;
            _cancelLabel = UIKit.LabelOf(_cancel);
            _confirm.gameObject.AddComponent<FocusRing>();
            _cancel.gameObject.AddComponent<FocusRing>();
            _confirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _cancel, selectOnLeft = _cancel };
            _cancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _confirm, selectOnRight = _confirm };
        }

        /// <summary>Shows the dialog. <paramref name="onConfirm"/> runs only if the player confirms.</summary>
        public static void Show(string title, string body, string confirmLabel, string cancelLabel, Action onConfirm)
        {
            if (Instance == null)
            {
                onConfirm?.Invoke();
                return;
            }
            Instance.Open(title, body, confirmLabel, cancelLabel, onConfirm);
        }

        /// <summary>
        /// A short exchange before an action: <paramref name="speaker"/> as the title, then the player's line and the reply.
        /// <paramref name="onConfirm"/> runs only if the player goes ahead. Focus starts on the go-ahead button unless
        /// <paramref name="danger"/> (an action that can't be undone), which keeps the red button and the safe focus.
        /// </summary>
        public static void ShowConversation(string speaker, string playerLine, string reply, string confirmLabel, string cancelLabel,
            Action onConfirm, bool danger = false)
        {
            if (Instance == null)
            {
                onConfirm?.Invoke();
                return;
            }
            var body = string.IsNullOrEmpty(playerLine)
                ? $"<b>{speaker}:</b> “{reply}”"
                : $"<color=#B8B8B8><b>You:</b> “{playerLine}”</color>\n\n<b>{speaker}:</b> “{reply}”";
            Instance.Open(speaker, body, confirmLabel, cancelLabel, onConfirm, conversation: true, danger: danger);
        }

        private void Open(string title, string body, string confirmLabel, string cancelLabel, Action onConfirm,
            bool conversation = false, bool danger = true)
        {
            var size = conversation ? ConversationSize : ConfirmSize;
            _frame.sizeDelta = size;
            _fit.designSize = size;
            _body.rectTransform.sizeDelta = conversation ? new Vector2(900, 250) : new Vector2(760, 150);
            _body.rectTransform.anchoredPosition = conversation ? new Vector2(0, 30) : new Vector2(0, 20);
            _body.alignment = conversation ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            _body.fontSize = conversation ? 25 : 28;
            _confirmBg.color = danger ? ConfirmColor : UIKit.Gold;
            _confirmLabel.color = danger ? UIKit.Cream : UIKit.Dark;
            _focusConfirm = conversation && !danger;
            _title.text = title;
            _body.text = body;
            _confirmLabel.text = confirmLabel;
            _cancelLabel.text = cancelLabel;
            _onConfirm = onConfirm;
            _previous = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            transform.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            _open = true;
            Select(_focusConfirm ? _confirm : _cancel);
        }

        public void Confirm()
        {
            if (!_open) return;
            var action = _onConfirm;
            Close(false);
            action?.Invoke();
        }

        public void Cancel()
        {
            if (!_open) return;
            Close(true);
        }

        private void Close(bool restoreFocus)
        {
            _open = false;
            _onConfirm = null;
            _root.gameObject.SetActive(false);
            if (restoreFocus && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_previous != null && _previous.activeInHierarchy ? _previous : null);
        }

        private static void Select(Selectable s)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        private void Update()
        {
            if (!_open) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cancel();
                return;
            }
            var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                Select(current == _cancel.gameObject ? _confirm : _cancel);
                return;
            }
            // Keep focus inside the dialog.
            if (current != _cancel.gameObject && current != _confirm.gameObject) Select(_focusConfirm ? _confirm : _cancel);
        }
    }
}
