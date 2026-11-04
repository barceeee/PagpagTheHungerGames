using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Survival;
using Pagpag.TimeSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Family list behind a hamburger button, at the top-left under the player's profile (HUD portrait).
    /// Expanded, it shows everything the old family strip showed — each member's portrait, name, status,
    /// hunger/energy/health bars, critical highlight, death — plus the pantry; a member opens the Family panel.
    /// Collapsed, a red dot on the button still warns when anyone is in danger, and the grief badge stays
    /// visible beside it. Closes from the button, Escape, or a click anywhere outside.
    /// </summary>
    public class FamilyMenu : MonoBehaviour
    {
        private const float ButtonWidth = 176f, ButtonHeight = 64f, ListWidth = 380f, RowHeight = 84f, Gap = 6f;
        private static readonly Color CriticalRow = new Color(0.45f, 0.12f, 0.12f);
        private static readonly Color DeadRow = new Color(0.12f, 0.12f, 0.13f);
        private static readonly Color AlertDot = new Color(0.95f, 0.25f, 0.22f);

        private class Row
        {
            public string id;
            public Button button;
            public Image bg, portrait;
            public Text name, status;
            public RectTransform hunger, energy, health;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Button _toggle;
        private Text _toggleLabel;
        private Image _alert;
        private RectTransform _dropdown, _viewport, _content, _catcher;
        private Text _pantryText;
        private RectTransform _griefBadge;
        private Image _griefBg;
        private Text _griefText;
        private bool _built;

        public bool IsOpen { get; private set; }

        private void OnEnable() => GameEvents.RunChanged += OnRunChanged;
        private void OnDisable() => GameEvents.RunChanged -= OnRunChanged;
        private void OnRunChanged(RunData run) => Refresh();
        private void Start() => Refresh();

        // ---------- Build ----------

        private void Build(RunData run)
        {
            _built = true;
            var root = (RectTransform)transform;

            // Click-catcher behind the menu (a sibling, so clicks on the list never reach it).
            _catcher = UIKit.Rect("FamilyMenuCatcher", root.parent, Vector2.zero, Vector2.zero);
            _catcher.anchorMin = Vector2.zero;
            _catcher.anchorMax = Vector2.one;
            _catcher.offsetMin = _catcher.offsetMax = Vector2.zero;
            _catcher.SetSiblingIndex(root.GetSiblingIndex());
            var catcherImg = UIKit.Img(_catcher, new Color(0, 0, 0, 0.001f), false, true);
            var catcherButton = _catcher.gameObject.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.targetGraphic = catcherImg;
            catcherButton.navigation = new Navigation { mode = Navigation.Mode.None };
            catcherButton.onClick.AddListener(() => SetOpen(false));
            _catcher.gameObject.SetActive(false);

            // Hamburger button
            var tl = new Vector2(0, 1);
            _toggle = UIKit.Btn("FamilyMenuButton", root, Vector2.zero, new Vector2(ButtonWidth, ButtonHeight), "", () => SetOpen(!IsOpen),
                UIKit.ButtonColor, UIKit.Cream, 24, tl);
            _toggle.gameObject.AddComponent<FocusRing>();
            var label = (RectTransform)_toggle.transform.Find("Label");
            label.offsetMin = new Vector2(62, 6);
            _toggleLabel = UIKit.LabelOf(_toggle);
            _toggleLabel.alignment = TextAnchor.MiddleLeft;
            _toggleLabel.text = "Family";
            for (var i = 0; i < 3; i++)
            {
                var bar = UIKit.Rect("Bar" + i, _toggle.transform, new Vector2(20, -21 - i * 10), new Vector2(30, 5), tl, tl);
                UIKit.Img(bar, UIKit.Cream, true);
            }
            var dot = UIKit.Rect("Alert", _toggle.transform, new Vector2(-8, -8), new Vector2(18, 18), Vector2.one, Vector2.one);
            _alert = UIKit.CircleImg(dot, AlertDot);

            // Grief badge stays visible next to the button
            _griefBadge = UIKit.Rect("Depression", root, new Vector2(ButtonWidth + 10, 0), new Vector2(250, ButtonHeight), tl, tl);
            _griefBg = UIKit.Img(_griefBadge, UIKit.Grief, true);
            _griefText = UIKit.Txt(UIKit.Fill("T", _griefBadge, 4), "", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // Dropdown list
            _dropdown = UIKit.Rect("FamilyList", root, new Vector2(0, -(ButtonHeight + 8)), new Vector2(ListWidth, 100), tl, tl);
            UIKit.Img(_dropdown, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.45f), true, true);
            var fill = UIKit.Fill("Fill", _dropdown, 2);
            UIKit.Img(fill, UIKit.Panel, true, true);
            _viewport = UIKit.Fill("Viewport", fill, 8);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _content = UIKit.Rect("Content", _viewport, Vector2.zero, new Vector2(ListWidth - 20, 0), tl, tl);
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.offsetMin = new Vector2(0, _content.offsetMin.y);
            _content.offsetMax = new Vector2(0, 0);
            var scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = _viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var y = 0f;
            var pantry = UIKit.Rect("Pantry", _content, new Vector2(0, y), new Vector2(ListWidth - 20, 44), tl, tl);
            _pantryText = UIKit.Txt(UIKit.Fill("T", pantry, 4), "", 18, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            y -= 44 + Gap;

            Selectable previous = null;
            foreach (var m in run.family)
            {
                var row = new Row { id = m.id };
                var rt = UIKit.Rect("Member_" + m.id, _content, new Vector2(0, y), new Vector2(ListWidth - 20, RowHeight), tl, tl);
                row.bg = UIKit.Img(rt, UIKit.PanelLight, true, true);
                row.button = rt.gameObject.AddComponent<Button>();
                row.button.targetGraphic = row.bg;
                var colors = row.button.colors;
                colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                colors.colorMultiplier = 1.25f;
                row.button.colors = colors;
                rt.gameObject.AddComponent<FocusRing>();
                var id = m.id;
                row.button.onClick.AddListener(() => OpenMember(id));

                row.portrait = UIKit.Img(UIKit.Rect("Portrait", rt, new Vector2(10, 0), new Vector2(64, 64), new Vector2(0, 0.5f), new Vector2(0, 0.5f)), Color.white);
                row.name = UIKit.Label("Name", rt, new Vector2(86, -8), new Vector2(260, 26), m.name, 20, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, tl, true);
                row.name.rectTransform.pivot = tl;
                row.status = UIKit.Label("Status", rt, new Vector2(86, -34), new Vector2(262, 20), "", 15, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, tl, true);
                row.status.rectTransform.pivot = tl;
                row.hunger = MiniBar(rt, 60, UIKit.HungerColor);
                row.energy = MiniBar(rt, 68, UIKit.EnergyColor);
                row.health = MiniBar(rt, 76, UIKit.HealthColor);

                // Up/down arrows move through the list.
                if (previous != null)
                {
                    var nav = previous.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnDown = row.button;
                    previous.navigation = nav;
                    row.button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous };
                }
                else row.button.navigation = new Navigation { mode = Navigation.Mode.Explicit };
                previous = row.button;

                _rows.Add(row);
                y -= RowHeight + Gap;
            }
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, -y);
            _dropdown.gameObject.SetActive(false);
        }

        private static RectTransform MiniBar(RectTransform row, float yFromTop, Color color)
        {
            var track = UIKit.Rect("Bar", row, new Vector2(86, -yFromTop), new Vector2(262, 5), new Vector2(0, 1), new Vector2(0, 1));
            UIKit.Img(track, new Color(0.2f, 0.2f, 0.24f));
            var fill = UIKit.Fill("Fill", track);
            UIKit.Img(fill, color);
            return fill;
        }

        // ---------- Open / close ----------

        public void SetOpen(bool open)
        {
            if (!_built) return;
            IsOpen = open;
            _dropdown.gameObject.SetActive(open);
            _catcher.gameObject.SetActive(open);
            if (open)
            {
                transform.SetAsLastSibling();
                _catcher.SetSiblingIndex(transform.GetSiblingIndex());
                FitList();
                Refresh();
                if (EventSystem.current != null && _rows.Count > 0) EventSystem.current.SetSelectedGameObject(_rows[0].button.gameObject);
            }
            else if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                     EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_dropdown))
            {
                EventSystem.current.SetSelectedGameObject(_toggle.gameObject);
            }
        }

        private void OpenMember(string id)
        {
            SetOpen(false);
            GameEvents.OpenFamilyPanel(id);
        }

        /// <summary>Limits the list's height to the space below it; the rest scrolls.</summary>
        private void FitList()
        {
            var parent = transform.parent as RectTransform;
            var root = (RectTransform)transform;
            var available = parent != null ? parent.rect.height + root.anchoredPosition.y - (ButtonHeight + 8) - 16 : 600f;
            var wanted = _content.sizeDelta.y + 20;
            _dropdown.sizeDelta = new Vector2(ListWidth, Mathf.Clamp(wanted, 120f, Mathf.Max(120f, available)));
        }

        private void Update()
        {
            if (IsOpen)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame && !ConfirmDialog.IsOpen) SetOpen(false);
                else FitList();
            }
            if (_griefBadge != null && _griefBadge.gameObject.activeSelf)
                _griefBg.color = Color.Lerp(UIKit.Grief, new Color(0.25f, 0.15f, 0.4f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            var gm = GameManager.Instance;
            var run = gm != null ? gm.CurrentRun : null;
            if (run == null || gm.Family == null) return;
            if (!_built) Build(run);
            var cfg = gm.Config;
            var anyCritical = false;

            foreach (var row in _rows)
            {
                var m = run.Member(row.id);
                if (m == null) continue;
                row.portrait.sprite = m.isSpouse ? gm.SpouseCharacter?.portrait : gm.Family.Get(m.id)?.portrait;
                UIKit.SetBar(row.hunger, m.hunger / 100f);
                UIKit.SetBar(row.energy, m.energy / 100f);
                UIKit.SetBar(row.health, m.health / 100f);
                if (!m.alive)
                {
                    row.bg.color = DeadRow;
                    row.portrait.color = new Color(0.35f, 0.35f, 0.35f);
                    row.name.text = "† " + m.name;
                    row.status.text = $"Died Day {m.diedDay}";
                    row.status.color = UIKit.Muted;
                    continue;
                }
                row.portrait.color = Color.white;
                row.name.text = m.name;
                var critical = m.hunger <= cfg.hungerWarning || m.energy <= cfg.energyWarning || m.health <= cfg.healthWarning;
                anyCritical |= critical;
                row.bg.color = critical ? CriticalRow : UIKit.PanelLight;
                string status;
                if (SurvivalSystem.IsBusy(run, m)) status = $"Working · back {GameTime.Clock(m.busyUntilAbs % GameTime.MinutesPerDay)}";
                else if (m.health <= cfg.healthWarning) status = "Health critical!";
                else if (m.hunger <= cfg.hungerWarning) status = "Very hungry!";
                else if (m.energy <= cfg.energyWarning) status = "Exhausted!";
                else if (SurvivalSystem.IsAsleep(run, m, cfg)) status = "Asleep";
                else status = "At home";
                row.status.text = status;
                row.status.color = critical ? new Color(1f, 0.6f, 0.55f) : UIKit.Muted;
            }

            _alert.gameObject.SetActive(anyCritical);
            _pantryText.text = $"Pantry: {run.PantryTotal} servings" + (run.pantryPagpag > 0 ? $" ({run.pantryPagpag} pagpag)" : "");
            _pantryText.color = run.PantryTotal == 0 ? UIKit.Danger : UIKit.Cream;

            _griefBadge.gameObject.SetActive(run.IsDepressed);
            if (run.IsDepressed)
            {
                var left = run.DepressionMinutesLeft;
                _griefText.text = $"DEPRESSED\nGrieving {run.depressionCause}\n{left / GameTime.MinutesPerDay}d {left % GameTime.MinutesPerDay / 60}h left";
            }
        }
    }
}
