using System.Collections.Generic;
using System.Linq;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// The family overlay: every member's stats, personality and backstory; feeding; and choosing jobs
    /// for the children (each job opens its minigame, played on the child's behalf).
    /// </summary>
    public class FamilyPanel : MonoBehaviour
    {
        private class Row
        {
            public string id;
            public Image bg, portrait;
            public Text name, status;
            public RectTransform hunger, energy, health;
        }

        private readonly List<Row> _rows = new List<Row>();
        private RectTransform _root, _detail, _jobs, _jobsContent;
        private Text _header, _dName, _dRole, _dTraits, _dSkills, _dPersonality, _dBackstory, _dStatus, _jobsTitle;
        private Text _hungerVal, _energyVal, _healthVal;
        private RectTransform _hungerBar, _energyBar, _healthBar;
        private Image _dPortrait;
        private Button _feedButton, _jobsButton, _feedAllButton, _doseButton, _cureButton;
        private Text _feedLabel, _doseLabel, _cureLabel;
        private string _selected;
        private bool _built;

        private GameManager Gm => GameManager.Instance;
        private RunData Run => Gm.CurrentRun;
        public bool IsOpen => _root != null && _root.gameObject.activeSelf;

        private void Awake()
        {
            _root = UIKit.Fill("Overlay", transform);
        }

        private void OnEnable()
        {
            GameEvents.FamilyPanelRequested += Open;
            GameEvents.RunChanged += OnRunChanged;
        }

        private void OnDisable()
        {
            GameEvents.FamilyPanelRequested -= Open;
            GameEvents.RunChanged -= OnRunChanged;
        }

        private void Start() => _root.gameObject.SetActive(false);

        private void OnRunChanged(RunData _)
        {
            if (IsOpen) Refresh();
        }

        public void Open(string memberId)
        {
            if (Run == null || Gm.EventInProgress) return;
            if (!_built) Build();
            _selected = memberId ?? _selected ?? Run.family.FirstOrDefault()?.id;
            _root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowJobs(false);
            Refresh();
        }

        public void Close() => _root.gameObject.SetActive(false);

        // =====================================================================
        // Build
        // =====================================================================

        private void Build()
        {
            _built = true;
            UIKit.Img(_root, new Color(0, 0, 0, 0.82f), false, true);
            var win = UIKit.Rect("Window", _root, Vector2.zero, new Vector2(1600, 940));
            var fit = win.gameObject.AddComponent<FitInParent>();
            fit.designSize = new Vector2(1600, 940);
            fit.reserved = new Vector2(32, 32);
            UIKit.Img(win, UIKit.Panel, true, true);

            UIKit.Label("Title", win, new Vector2(-560, 435), new Vector2(400, 50), "Your Family", 40, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _header = UIKit.Label("Header", win, new Vector2(60, 435), new Vector2(760, 40), "", 20, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Normal, null, true);
            _feedAllButton = UIKit.Btn("FeedAll", win, new Vector2(560, 435), new Vector2(260, 52), "Serve a family meal", FeedAll, UIKit.Gold, UIKit.Dark, 20);
            UIKit.Btn("Close", win, new Vector2(745, 435), new Vector2(80, 52), "✕", Close, new Color(0.4f, 0.18f, 0.18f), UIKit.Cream, 26);

            var run = Run;
            var y = 350f;
            foreach (var m in run.family)
            {
                var row = new Row { id = m.id };
                var card = UIKit.Rect("Row_" + m.id, win, new Vector2(-560, y), new Vector2(440, 108));
                row.bg = UIKit.Img(card, UIKit.PanelLight, true, true);
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = row.bg;
                var id = m.id;
                btn.onClick.AddListener(() => { _selected = id; ShowJobs(false); Refresh(); });
                row.portrait = UIKit.Img(UIKit.Rect("Portrait", card, new Vector2(12, 0), new Vector2(84, 84), new Vector2(0, 0.5f), new Vector2(0, 0.5f)), Color.white);
                row.name = UIKit.Label("Name", card, new Vector2(108, -10), new Vector2(320, 28), "", 22, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1));
                row.name.rectTransform.pivot = new Vector2(0, 1);
                row.status = UIKit.Label("Status", card, new Vector2(108, -38), new Vector2(320, 22), "", 15, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, new Vector2(0, 1), true);
                row.status.rectTransform.pivot = new Vector2(0, 1);
                row.hunger = RowBar(card, 70, UIKit.HungerColor);
                row.energy = RowBar(card, 80, UIKit.EnergyColor);
                row.health = RowBar(card, 90, UIKit.HealthColor);
                _rows.Add(row);
                y -= 118f;
            }

            // Detail pane
            _detail = UIKit.Rect("Detail", win, new Vector2(230, -20), new Vector2(1080, 840));
            UIKit.Img(_detail, new Color(0.09f, 0.11f, 0.15f), true);
            _dPortrait = UIKit.Img(UIKit.Rect("Portrait", _detail, new Vector2(30, -30), new Vector2(170, 170), new Vector2(0, 1), new Vector2(0, 1)), Color.white);
            _dName = UIKit.Label("Name", _detail, new Vector2(225, -30), new Vector2(500, 50), "", 40, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1));
            _dName.rectTransform.pivot = new Vector2(0, 1);
            _dRole = UIKit.Label("Role", _detail, new Vector2(225, -82), new Vector2(500, 30), "", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Normal, new Vector2(0, 1));
            _dRole.rectTransform.pivot = new Vector2(0, 1);
            _dTraits = UIKit.Label("Traits", _detail, new Vector2(225, -118), new Vector2(520, 60), "", 18, UIKit.Cream, TextAnchor.UpperLeft, FontStyle.Italic, new Vector2(0, 1), true);
            _dTraits.rectTransform.pivot = new Vector2(0, 1);
            _dSkills = UIKit.Label("Skills", _detail, new Vector2(-30, -30), new Vector2(300, 150), "", 20, UIKit.Cream, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(1, 1), true);
            _dSkills.rectTransform.pivot = new Vector2(1, 1);

            _hungerBar = BigBar(_detail, "Hunger", -230, UIKit.HungerColor, out _hungerVal);
            _energyBar = BigBar(_detail, "Energy", -272, UIKit.EnergyColor, out _energyVal);
            _healthBar = BigBar(_detail, "Health", -314, UIKit.HealthColor, out _healthVal);
            _dStatus = UIKit.Label("Status", _detail, new Vector2(30, -356), new Vector2(1020, 30), "", 21, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1));
            _dStatus.rectTransform.pivot = new Vector2(0, 1);
            _dPersonality = UIKit.Label("Personality", _detail, new Vector2(30, -398), new Vector2(1020, 60), "", 22, UIKit.Cream, TextAnchor.UpperLeft, FontStyle.Italic, new Vector2(0, 1), true);
            _dPersonality.rectTransform.pivot = new Vector2(0, 1);
            _dBackstory = UIKit.Label("Backstory", _detail, new Vector2(30, -466), new Vector2(1020, 200), "", 20, new Color(0.85f, 0.83f, 0.78f), TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(0, 1), true);
            _dBackstory.rectTransform.pivot = new Vector2(0, 1);
            _dBackstory.lineSpacing = 1.1f;

            _feedButton = UIKit.Btn("Feed", _detail, new Vector2(-250, 50), new Vector2(380, 64), "Feed (1 serving)", FeedSelected, null, null, 22, new Vector2(0.5f, 0));
            _feedLabel = UIKit.LabelOf(_feedButton);
            _jobsButton = UIKit.Btn("Jobs", _detail, new Vector2(250, 50), new Vector2(380, 64), "Send to work…", () => ShowJobs(true), UIKit.Gold, UIKit.Dark, 22, new Vector2(0.5f, 0));
            // Medicine from the home cabinet (bought at the pharmacy), one row above Feed / Jobs.
            _doseButton = UIKit.Btn("GiveDose", _detail, new Vector2(-250, 128), new Vector2(380, 64), "Give medicine", () => GiveMedicine(false), null, null, 20, new Vector2(0.5f, 0));
            _doseLabel = UIKit.LabelOf(_doseButton);
            _cureButton = UIKit.Btn("GiveCure", _detail, new Vector2(250, 128), new Vector2(380, 64), "Instant cure", () => GiveMedicine(true), null, null, 20, new Vector2(0.5f, 0));
            _cureLabel = UIKit.LabelOf(_cureButton);

            // Jobs pane (scrollable)
            _jobs = UIKit.Rect("Jobs", win, new Vector2(230, -20), new Vector2(1080, 840));
            UIKit.Img(_jobs, new Color(0.09f, 0.11f, 0.15f), true, true);
            _jobsTitle = UIKit.Label("JobsTitle", _jobs, new Vector2(30, -20), new Vector2(800, 44), "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1));
            _jobsTitle.rectTransform.pivot = new Vector2(0, 1);
            UIKit.Btn("Back", _jobs, new Vector2(-30, -20), new Vector2(160, 48), "◄ Back", () => ShowJobs(false), null, null, 20, new Vector2(1, 1))
                .GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            UIKit.Label("JobsHint", _jobs, new Vector2(30, -66), new Vector2(1020, 50),
                "Children earn part of an adult's wage. You play the job's minigame for them; the better suited the child, the easier it is. Work drains their hunger and energy.",
                17, UIKit.Muted, TextAnchor.UpperLeft, FontStyle.Italic, new Vector2(0, 1), true).rectTransform.pivot = new Vector2(0, 1);

            var viewport = UIKit.Rect("Viewport", _jobs, new Vector2(0, -50), new Vector2(1040, 680));
            viewport.gameObject.AddComponent<RectMask2D>();
            UIKit.Img(viewport, new Color(0, 0, 0, 0.01f), false, true);
            _jobsContent = UIKit.Rect("Content", viewport, Vector2.zero, new Vector2(1040, 0), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            var layout = _jobsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            _jobsContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _jobsContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            _jobs.gameObject.SetActive(false);
        }

        private static RectTransform RowBar(RectTransform card, float yFromTop, Color color)
        {
            var track = UIKit.Rect("Bar", card, new Vector2(108, -yFromTop), new Vector2(300, 6), new Vector2(0, 1), new Vector2(0, 1));
            UIKit.Img(track, new Color(0.2f, 0.2f, 0.24f));
            var fill = UIKit.Fill("Fill", track);
            UIKit.Img(fill, color);
            return fill;
        }

        private static RectTransform BigBar(RectTransform parent, string label, float y, Color color, out Text value)
        {
            UIKit.Label(label + "Label", parent, new Vector2(30, y), new Vector2(120, 30), label, 20, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1))
                .rectTransform.pivot = new Vector2(0, 1);
            var track = UIKit.Rect(label + "Track", parent, new Vector2(160, y - 7), new Vector2(760, 18), new Vector2(0, 1), new Vector2(0, 1));
            UIKit.Img(track, new Color(0.17f, 0.18f, 0.22f), true);
            var fill = UIKit.Fill("Fill", track);
            UIKit.Img(fill, color, true);
            value = UIKit.Label(label + "Value", parent, new Vector2(935, y), new Vector2(110, 30), "", 20, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1));
            value.rectTransform.pivot = new Vector2(0, 1);
            return fill;
        }

        // =====================================================================
        // Refresh
        // =====================================================================

        private void Refresh()
        {
            var run = Run;
            if (run == null) return;
            var fam = Gm.Family;
            var cfg = Gm.Config;
            var atHome = FamilyService.PlayerAtHome(run);

            _header.text = $"Pantry: {run.PantryTotal} servings{(run.pantryPagpag > 0 ? $" ({run.pantryPagpag} pagpag)" : "")}  ·  " +
                           $"Medicine: {run.medicineDoses} dose(s), {run.instantCures} cure(s)  ·  " +
                           $"Children alive: {FamilyService.ChildrenAlive(run)}/5  ·  " +
                           (atHome ? "You are home." : "Go home to feed the family yourself. The spouse serves meals at 7 AM, noon and 6 PM.");
            _feedAllButton.interactable = atHome && run.PantryTotal > 0 && !run.isDead;

            foreach (var row in _rows)
            {
                var m = run.Member(row.id);
                var c = fam.Get(m.id);
                row.portrait.sprite = m.isSpouse ? Gm.SpouseCharacter?.portrait : c?.portrait;
                row.portrait.color = m.alive ? Color.white : new Color(0.35f, 0.35f, 0.35f);
                row.name.text = (m.alive ? "" : "† ") + m.name + (c != null ? $", {c.age}" : "");
                row.status.text = FamilyService.StatusText(run, m, cfg);
                UIKit.SetBar(row.hunger, m.hunger / 100f);
                UIKit.SetBar(row.energy, m.energy / 100f);
                UIKit.SetBar(row.health, m.health / 100f);
                var critical = m.alive && (m.hunger <= cfg.hungerWarning || m.energy <= cfg.energyWarning || m.health <= cfg.healthWarning ||
                                           (IllnessRules.IsIll(m.illness) && m.illness.severity >= IllnessRules.SeriousAt));
                row.bg.color = row.id == _selected ? new Color(0.2f, 0.3f, 0.45f) : critical ? new Color(0.42f, 0.12f, 0.12f) : UIKit.PanelLight;
            }

            var sel = run.Member(_selected) ?? run.family[0];
            var child = fam.Get(sel.id);
            _dPortrait.sprite = sel.isSpouse ? Gm.SpouseCharacter?.portrait : child?.portrait;
            _dPortrait.color = sel.alive ? Color.white : new Color(0.35f, 0.35f, 0.35f);
            _dName.text = (sel.alive ? "" : "† ") + sel.name;
            _dRole.text = FamilyService.Role(run, sel, fam) + (child != null ? $"  ·  Age {child.age}" : "");
            _dStatus.text = FamilyService.StatusText(run, sel, cfg);
            SetBig(_hungerBar, _hungerVal, sel.hunger);
            SetBig(_energyBar, _energyVal, sel.energy);
            SetBig(_healthBar, _healthVal, sel.health);

            if (sel.isSpouse)
            {
                var spouseProfile = Gm.SpouseCharacter;
                _dTraits.text = "Stays home · Minds the children · Serves meals from the pantry";
                _dSkills.text = "";
                _dPersonality.text = run.gender == Gender.Male
                    ? "Inday keeps the budget notebook and the household running while you work."
                    : "Dong watches the kids, fixes what he can, and tells terrible jokes to keep everyone going.";
                _dBackstory.text = spouseProfile != null ? Localization.Loc.Get(spouseProfile.descriptionKey) +
                    "\n\nAt 7 AM, noon and 6 PM your spouse feeds whoever is home and hungry from the pantry, youngest first. Keep the pantry stocked — and keep your spouse fed too." : "";
                _jobsButton.gameObject.SetActive(false);
            }
            else
            {
                _dTraits.text = string.Join("  ·  ", child.traitTags);
                _dSkills.text = $"Strength   {FamilyService.Stars(child.strength)}\nDexterity  {FamilyService.Stars(child.dexterity)}\n" +
                                $"Smarts     {FamilyService.Stars(child.smarts)}\nCharm      {FamilyService.Stars(child.charm)}\n" +
                                $"<size=15>Appetite ×{child.appetite:0.0}  ·  Tires ×{child.tireRate:0.0}</size>";
                _dPersonality.text = child.personality;
                _dBackstory.text = child.backstory;
                _jobsButton.gameObject.SetActive(true);
                _jobsButton.interactable = sel.alive && !run.isDead;
            }

            var canFeed = sel.alive && atHome && FamilyService.AtHome(run, sel) && run.PantryTotal > 0 && !run.isDead;
            _feedButton.interactable = canFeed;
            _feedLabel.text = !sel.alive ? "—" : !atHome ? "Go home to feed" : run.PantryTotal == 0 ? "Pantry empty" :
                SurvivalSystem.IsBusy(run, sel) ? "Away working" : "Feed (1 serving)";

            var sick = sel.alive && IllnessRules.IsIll(sel.illness);
            var canTreat = sick && atHome && FamilyService.AtHome(run, sel) && !run.isDead;
            _doseButton.interactable = canTreat && run.medicineDoses > 0;
            _cureButton.interactable = canTreat && run.instantCures > 0;
            _doseLabel.text = !sick ? "Not sick" : !canTreat ? "Go home to give medicine" : $"Give medicine ({run.medicineDoses} at home)";
            _cureLabel.text = !sick ? "—" : $"Instant cure ({run.instantCures} at home)";

            if (_jobs.gameObject.activeSelf) BuildJobs(sel, child);
        }

        private static void SetBig(RectTransform bar, Text value, float v)
        {
            UIKit.SetBar(bar, v / 100f);
            value.text = $"{Mathf.RoundToInt(v)}/100";
            value.color = v <= 25 ? UIKit.Danger : UIKit.Cream;
        }

        private void ShowJobs(bool show)
        {
            _jobs.gameObject.SetActive(show);
            _detail.gameObject.SetActive(!show);
            if (show) Refresh();
        }

        private void BuildJobs(MemberState m, ChildProfile c)
        {
            UIKit.Clear(_jobsContent);
            if (c == null) return;
            var run = Run;
            var fam = Gm.Family;
            var cfg = Gm.Config;
            var diff = Gm.CurrentDifficulty;
            _jobsTitle.text = $"Jobs for {c.displayName} (age {c.age})";

            var jobs = new List<(LocationData loc, LocationAction a, string reason)>();
            foreach (var loc in Gm.Locations.locations)
                foreach (var a in loc.actions.Where(a => a.ChildrenCanDo))
                    jobs.Add((loc, a, FamilyService.CanAssign(run, c, m, loc, a, cfg, fam)));

            foreach (var (loc, a, reason) in jobs.OrderBy(j => j.reason == null ? 0 : 1).ThenByDescending(j => c.SkillValue(j.a.skill)))
            {
                var row = UIKit.Rect("Job_" + a.id, _jobsContent, Vector2.zero, new Vector2(1040, 74));
                UIKit.Img(row, reason == null ? UIKit.PanelLight : new Color(0.1f, 0.12f, 0.15f), true);
                var skill = c.SkillValue(a.skill);
                SurvivalSystem.Body(m, fam, out var appetite, out var tire, out _);
                var energyCost = Mathf.RoundToInt(-a.energy * tire * diff.fatigueRateMultiplier);
                var hungerCost = Mathf.RoundToInt(-a.hunger * appetite * diff.hungerRateMultiplier);
                UIKit.Label("Name", row, new Vector2(16, -8), new Vector2(760, 28),
                    $"{a.name}  <color=#B8B8B8>— {loc.displayName}</color>", 20, UIKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0, 1), true)
                    .rectTransform.pivot = new Vector2(0, 1);
                var info = $"{a.skill} {FamilyService.Stars(skill)} {FamilyService.FitLabel(c, a.skill)}  ·  ~₱{FamilyService.ChildFullPay(c, a, fam)}  ·  " +
                           $"{Mathf.RoundToInt(a.durationMinutes / 60f)} hr  ·  Hunger −{hungerCost}  ·  Energy −{energyCost}";
                if (reason != null) info += $"  ·  <color=#FF8A7A>{reason}</color>";
                UIKit.Label("Info", row, new Vector2(16, -40), new Vector2(820, 26), info, 15, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, new Vector2(0, 1), true)
                    .rectTransform.pivot = new Vector2(0, 1);
                var loc1 = loc;
                var a1 = a;
                var start = UIKit.Btn("Start", row, new Vector2(-14, 0), new Vector2(170, 52), "Start job", () =>
                {
                    Close();
                    Gm.StartChildJob(c.id, loc1, a1);
                }, UIKit.Gold, UIKit.Dark, 20, new Vector2(1, 0.5f));
                start.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
                start.interactable = reason == null;
                if (reason != null)
                {
                    ((Image)start.targetGraphic).color = new Color(0.25f, 0.27f, 0.3f);
                    UIKit.LabelOf(start).text = "Not now";
                    UIKit.LabelOf(start).color = UIKit.Muted;
                }
            }
        }

        // =====================================================================
        // Actions
        // =====================================================================

        private void FeedSelected()
        {
            Gm.FeedMember(_selected);
            Refresh();
        }

        private void GiveMedicine(bool instant)
        {
            Gm.GiveMedicine(_selected, instant);
            Refresh();
        }

        private void FeedAll()
        {
            var home = Gm.Locations.Get("home");
            var meal = home?.actions.FirstOrDefault(a => a.kind == ActionKind.FamilyMeal);
            if (meal != null) Gm.PerformAction(home, meal);
            Refresh();
        }
    }
}
