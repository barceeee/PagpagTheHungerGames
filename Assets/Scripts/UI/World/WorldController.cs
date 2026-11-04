using System.Collections.Generic;
using System.Text;
using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Locations;
using Pagpag.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// World scene orchestration: map view, location screen, sidebar (location list + travel panel),
    /// HUD, intro story and game over. Reacts to <see cref="GameEvents"/>.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class WorldController : MonoBehaviour
    {
        [Header("Shared UI sprites (used by panels that build themselves at runtime)")]
        [SerializeField] private Sprite roundedSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite ringSprite;

        [Header("Family")]
        [SerializeField] private Button familyButton;

        [Header("Location drawer")]
        [SerializeField] private SideDrawer drawer;

        [Header("Views")]
        [SerializeField] private HudController hud;
        [SerializeField] private GameObject mapView;
        [SerializeField] private Image mapImage;
        [SerializeField] private LocationView locationView;
        [SerializeField] private IntroOverlay intro;
        [SerializeField] private GameOverOverlay gameOver;
        [SerializeField] private Button menuButton;

        [Header("Map and list (one entry per location, wired by the builder)")]
        [SerializeField] private List<MapHotspot> hotspots = new List<MapHotspot>();
        [SerializeField] private List<LocationListItem> listItems = new List<LocationListItem>();

        [Header("Sidebar info panel")]
        [SerializeField] private MapCropView infoThumbnail;
        [SerializeField] private Text infoName;
        [SerializeField] private Text infoStatus;
        [SerializeField] private Text infoDescription;
        [SerializeField] private Text infoDetail;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryLabel;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Text secondaryLabel;

        public static readonly Color HereColor = new Color(0.5f, 0.85f, 0.4f);
        public static readonly Color OpenColor = new Color(0.9f, 0.72f, 0.3f);
        public static readonly Color ClosedColor = new Color(0.6f, 0.62f, 0.66f);
        public static readonly Color LockedColor = new Color(0.8f, 0.35f, 0.32f);

        private GameManager _gm;
        private RunData _run;
        private LocationData _selected;
        private string _thumbId;
        private string _lastLocationId;

        private void Awake() => UIKit.Init(roundedSprite, circleSprite, ringSprite);

        private void Start()
        {
            _gm = GameManager.Instance;
            _run = _gm.CurrentRun ?? _gm.EnsureRunForTesting();
            if (_gm.Locations.Get(_run.currentLocationId) == null) _run.currentLocationId = _gm.Config.startLocationId;
            WorldService.RefreshUnlocks(_run, _gm.Locations);
            _selected = _gm.CurrentLocation;

            foreach (var h in hotspots)
            {
                var id = h.LocationId;
                h.Button.onClick.AddListener(() => OnLocationClicked(id));
            }
            foreach (var item in listItems)
            {
                var id = item.LocationId;
                item.Button.onClick.AddListener(() => OnLocationClicked(id));
            }
            primaryButton.onClick.AddListener(OnPrimary);
            secondaryButton.onClick.AddListener(OnSecondary);
            locationView.ReturnButton.onClick.AddListener(() => _gm.SetInsideLocation(false));
            menuButton.onClick.AddListener(ConfirmReturnToMenu);
            if (familyButton != null) familyButton.onClick.AddListener(() => GameEvents.OpenFamilyPanel());

            gameOver.gameObject.SetActive(false);
            intro.gameObject.SetActive(false);
            Refresh();

            if (_run.isDead || _run.victoryPending) gameOver.Show(_run);
            else if (!_run.introSeen) ShowIntro();
            else GameEvents.PostMessage($"{GameTime.PeriodName(GameTime.PeriodOf(_run.minuteOfDay))} in Tondo. You are at {_gm.CurrentLocation.displayName}.");
        }

        private void OnEnable()
        {
            GameEvents.RunChanged += OnRunChanged;
            GameEvents.RunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.RunChanged -= OnRunChanged;
            GameEvents.RunEnded -= OnRunEnded;
        }

        private void OnRunChanged(RunData _) => Refresh();

        private void OnRunEnded(RunData run) => gameOver.Show(run);

        // ---------- Intro ----------

        private void ShowIntro()
        {
            var profile = _gm.CurrentCharacter;
            var pages = new List<(string, string)>();
            for (var i = 0; i < profile.loreKeys.Length; i++)
                pages.Add((Loc.Get("intro.lore.title", profile.defaultName), Loc.Get(profile.loreKeys[i], _run.playerName)));
            pages.Add((Loc.Get("intro.day1.title"),
                Loc.Get("world.intro", _run.gender, _run.playerName, CharacterCreationController.FormatPesos(_run.money))));
            pages.Add((Loc.Get("intro.family.title"), FamilyPage()));
            pages.Add((Loc.Get("intro.howto.title"), Loc.Get("intro.howto", _gm.CurrentDifficulty.FullName)));
            intro.Show(profile.fullBody, pages, () =>
            {
                _gm.MarkIntroSeen();
                GameEvents.PostMessage("Day 1. Find food and find work. Click a place on the map to travel there.");
            });
        }

        private string FamilyPage()
        {
            var sb = new StringBuilder();
            sb.Append(Loc.Get("intro.family", _gm.SpouseCharacter != null ? _gm.SpouseCharacter.defaultName : ""));
            foreach (var c in _gm.Family.children)
                sb.Append($"\n\n<b>{c.displayName}</b>, {c.age} — <i>{c.role}.</i> {c.personality}");
            return sb.ToString();
        }

        // ---------- Interaction ----------

        private void ConfirmReturnToMenu()
        {
            ConfirmDialog.Show(Loc.Get("world.menu.confirm.title"), Loc.Get("world.menu.confirm.body"),
                Loc.Get("world.menu.confirm.yes"), Loc.Get("world.menu.confirm.no"), () => _gm.ReturnToMainMenu());
        }

        private void OnLocationClicked(string id)
        {
            var loc = _gm.Locations.Get(id);
            if (loc == null) return;
            // The travel panel lives in the drawer: choosing a place shows it.
            if (drawer != null) drawer.Open();
            // Clicking the place you're already at on the map takes you inside.
            if (_selected == loc && loc.id == _run.currentLocationId && !_run.insideLocation)
            {
                _gm.SetInsideLocation(true);
                return;
            }
            _selected = loc;
            Refresh();
        }

        private void OnPrimary()
        {
            if (_selected == null) return;
            if (_selected.id == _run.currentLocationId)
            {
                var entering = !_run.insideLocation;
                _gm.SetInsideLocation(entering);
                if (entering && drawer != null) drawer.Close();
                return;
            }
            if (_gm.Travel(_selected, TravelMode.Walk)) Arrived();
            Refresh();
        }

        private void OnSecondary()
        {
            if (_selected == null) return;
            if (_gm.Travel(_selected, TravelMode.Jeepney)) Arrived();
            Refresh();
        }

        private void Arrived()
        {
            _selected = _gm.CurrentLocation;
            if (drawer != null) drawer.Close(); // get out of the way of the location screen
        }

        // ---------- View ----------

        public static Color PeriodTint(int minuteOfDay) => GameTime.PeriodOf(minuteOfDay) switch
        {
            DayPeriod.Morning => new Color(1f, 1f, 1f),
            DayPeriod.Afternoon => new Color(1f, 0.95f, 0.86f),
            DayPeriod.Evening => new Color(0.78f, 0.7f, 0.86f),
            _ => new Color(0.48f, 0.52f, 0.7f)
        };

        private string StatusOf(LocationData loc, out Color color, out bool locked)
        {
            locked = !WorldService.IsUnlocked(_run, loc);
            if (locked) { color = LockedColor; return "Locked"; }
            if (loc.id == _run.currentLocationId) { color = HereColor; return "You are here"; }
            if (WorldService.IsOpen(loc, _run.minuteOfDay))
            {
                color = OpenColor;
                return loc.AlwaysOpen ? "Open" : $"Open · closes {GameTime.Hour(loc.closeHour)}";
            }
            color = ClosedColor;
            return $"Closed · opens {GameTime.Hour(loc.openHour)}";
        }

        private void Refresh()
        {
            var current = _gm.CurrentLocation;
            if (_lastLocationId != current.id)
            {
                _lastLocationId = current.id;
                _selected = current; // after travelling, the panel follows the player
            }
            var diff = _gm.CurrentDifficulty;
            var tint = PeriodTint(_run.minuteOfDay);
            hud.Refresh(_run, _gm.CurrentCharacter, diff, current, _gm.Config);
            if (_selected == null) _selected = current;

            foreach (var h in hotspots)
            {
                var loc = _gm.Locations.Get(h.LocationId);
                var status = StatusOf(loc, out var color, out var locked);
                h.SetState(status, color, locked, loc == _selected, loc == current);
            }
            foreach (var item in listItems)
            {
                var loc = _gm.Locations.Get(item.LocationId);
                var status = StatusOf(loc, out var color, out var locked);
                item.SetState(loc.displayName, locked ? "" : status, color, locked, loc == _selected);
            }

            mapImage.color = tint;
            if (_run.insideLocation && !_run.isDead)
            {
                mapView.SetActive(false);
                locationView.Show(current, _run, _gm.CurrentCharacter, _gm.Locations, _gm.Config, diff, tint);
            }
            else
            {
                locationView.Hide();
                mapView.SetActive(true);
            }
            RefreshInfoPanel(current, diff, tint);
        }

        private void RefreshInfoPanel(LocationData current, DifficultySettings diff, Color tint)
        {
            var loc = _selected;
            if (_thumbId != loc.id)
            {
                _thumbId = loc.id;
                infoThumbnail.Show(_gm.Locations.mapArt.texture, _gm.Locations.mapSize, loc.sceneCenter, loc.sceneZoom * 0.8f);
            }
            infoThumbnail.Image.color = tint;
            infoName.text = loc.displayName;
            var status = StatusOf(loc, out var color, out var locked);
            infoStatus.text = locked ? "Locked" : $"{status}  ·  {WorldService.HoursText(loc)}";
            infoStatus.color = color;
            infoDescription.text = loc.description;
            secondaryButton.gameObject.SetActive(false);
            primaryButton.interactable = !_run.isDead;

            if (locked)
            {
                infoDetail.text = "<b>How to unlock:</b> " + loc.unlock.hint;
                primaryLabel.text = "Locked";
                primaryButton.interactable = false;
                return;
            }

            if (loc == current)
            {
                infoDetail.text = "You are here.";
                primaryLabel.text = _run.insideLocation ? "Return to Map" : $"Enter {loc.displayName}";
                return;
            }

            var walk = WorldService.GetTravel(current, loc, TravelMode.Walk, _gm.Locations, _gm.Config, diff);
            var jeep = WorldService.GetTravel(current, loc, TravelMode.Jeepney, _gm.Locations, _gm.Config, diff);
            infoDetail.text = $"From {current.displayName}: walk {GameTime.Duration(walk.minutes)} (free)" +
                              (jeep.available ? $", or jeepney {GameTime.Duration(jeep.minutes)} for ₱{jeep.fare}." : ".");
            if (!WorldService.IsOpen(loc, _run.minuteOfDay))
                infoDetail.text += $"\n<color=#FF8A7A>Closed until {GameTime.Hour(loc.openHour)}.</color>";

            primaryLabel.text = $"Walk  ·  {GameTime.Duration(walk.minutes)}";
            if (jeep.available)
            {
                secondaryButton.gameObject.SetActive(true);
                secondaryLabel.text = $"Jeepney  ·  ₱{jeep.fare}  ·  {GameTime.Duration(jeep.minutes)}";
                secondaryButton.interactable = _run.money >= jeep.fare && !_run.isDead;
            }
        }
    }
}
