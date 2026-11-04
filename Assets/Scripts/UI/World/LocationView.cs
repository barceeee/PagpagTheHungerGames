using System.Collections.Generic;
using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Locations;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>The screen shown while the player is inside a location.</summary>
    public class LocationView : MonoBehaviour
    {
        [SerializeField] private MapCropView background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text hoursText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image playerBody;
        [SerializeField] private RectTransform actionsContainer;
        [SerializeField] private ActionCard cardTemplate;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button returnButton;

        private readonly List<ActionCard> _cards = new List<ActionCard>();
        private string _shownLocationId;

        public Button ReturnButton => returnButton;

        public void Show(LocationData loc, RunData run, CharacterProfile profile, LocationDatabase db, GameConfig cfg,
            DifficultySettings diff, Color periodTint)
        {
            gameObject.SetActive(true);
            if (_shownLocationId != loc.id)
            {
                _shownLocationId = loc.id;
                background.Show(db.mapArt.texture, db.mapSize, loc.sceneCenter, loc.sceneZoom);
            }
            background.Image.color = periodTint;
            playerBody.sprite = profile.fullBody;
            playerBody.preserveAspect = true;

            titleText.text = loc.displayName;
            var open = WorldService.IsOpen(loc, run.minuteOfDay);
            hoursText.text = (open ? "<color=#8FD47A>Open now</color>" : "<color=#FF8A7A>Closed now</color>") +
                             "  ·  " + WorldService.HoursText(loc);
            descriptionText.text = loc.description;

            var visible = new List<LocationAction>();
            foreach (var a in loc.actions)
                if (WorldService.IsVisible(run, a)) visible.Add(a);

            while (_cards.Count < visible.Count)
            {
                var card = Instantiate(cardTemplate, actionsContainer);
                _cards.Add(card);
            }
            for (var i = 0; i < _cards.Count; i++)
            {
                var active = i < visible.Count;
                _cards[i].gameObject.SetActive(active);
                if (!active) continue;
                var action = visible[i];
                _cards[i].Bind(action, WorldService.Describe(action, cfg, diff), WorldService.CanDo(run, loc, action, cfg),
                    () => OnAction(loc, action));
            }
            emptyText.gameObject.SetActive(visible.Count == 0);
        }

        private static void OnAction(LocationData loc, LocationAction action)
        {
            var gm = GameManager.Instance;
            if (action.HasDialog)
            {
                // Every interaction with someone opens with a short exchange; going ahead does the action.
                var reply = action.npcLines[Random.Range(0, action.npcLines.Length)];
                ConfirmDialog.ShowConversation(Fill(action.speaker), Fill(action.playerLine), Fill(reply),
                    GoAheadLabel(action), Loc.Get("dialog.never.mind"), () => gm.PerformAction(loc, action),
                    danger: action.kind == ActionKind.LeaveSlums);
                return;
            }
            if (action.kind == ActionKind.LeaveSlums)
            {
                // Leaving ends the run for good, so it is confirmed first.
                ConfirmDialog.Show(Loc.Get("world.leave.confirm.title"), Loc.Get("world.leave.confirm.body"),
                    Loc.Get("world.leave.confirm.yes"), Loc.Get("world.leave.confirm.no"), () => gm.PerformAction(loc, action));
                return;
            }
            gm.PerformAction(loc, action);
        }

        /// <summary>{name} = the player, {spouse} = the spouse.</summary>
        private static string Fill(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            var run = GameManager.Instance.CurrentRun;
            return line.Replace("{name}", run?.playerName ?? "").Replace("{spouse}", run?.Spouse?.name ?? "");
        }

        private static string GoAheadLabel(LocationAction a) => a.kind switch
        {
            ActionKind.Work => Loc.Get("dialog.go.work"),
            ActionKind.Eat => Loc.Get("dialog.go.eat"),
            ActionKind.Provision => Loc.Get("dialog.go.buy"),
            ActionKind.BuyMedicine => Loc.Get("dialog.go.buy"),
            ActionKind.Service => Loc.Get("dialog.go.pay", a.cost),
            ActionKind.LeaveSlums => Loc.Get("world.leave.confirm.yes"),
            ActionKind.Intimacy => Loc.Get("dialog.go.stay"),
            _ => Loc.Get("dialog.go.talk")
        };

        public void Hide() => gameObject.SetActive(false);
    }
}
