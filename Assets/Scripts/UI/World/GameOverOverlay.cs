using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Localization;
using Pagpag.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// End screen of a run: a loss (death, pregnancy, reaching the goal after a family death), a win, or leaving the slums.
    /// When the run is over it is already deleted and the only way out is the main menu. After a win the player
    /// chooses to keep playing in Infinite Mode, finish the run, or (with the money quota) leave the slums.
    /// </summary>
    public class GameOverOverlay : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text noteText;
        [SerializeField] private Text causeText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button menuButton;
        [Tooltip("Shown only after a win: keep playing this run in Infinite Mode.")]
        [SerializeField] private Button continueButton;
        [Tooltip("Shown only after a win, below the other two: leave the slums (needs the money quota).")]
        [SerializeField] private Button leaveButton;

        private static readonly Color LossTitle = new Color(0.78f, 0.16f, 0.16f);
        private static readonly Color WinTitle = new Color(0.85f, 0.64f, 0.25f);
        private static readonly Color LossNote = new Color(0.9f, 0.42f, 0.4f);
        private static readonly Color WinNote = new Color(0.55f, 0.85f, 0.45f);

        /// <summary>Bottom rows of the dialog's buttons (y from the dialog's bottom edge).</summary>
        public const float LowerRowY = 36f, UpperRowY = 126f;

        public bool IsVictory { get; private set; }

        public void Show(RunData run)
        {
            var gm = GameManager.Instance;
            var goal = gm.Config.victoryDays;
            IsVictory = run.victoryPending;
            var leftSlums = run.endKind == EndKind.LeftTheSlums;
            SetTitle(IsVictory ? "YOU SURVIVED" : leftSlums ? "YOU LEFT THE SLUMS" : "GAME OVER", IsVictory || leftSlums);

            if (leftSlums)
            {
                Note("The best ending: a new life outside Tondo.", true);
                causeText.text = $"With ₱{run.money:N0} saved, {run.playerName} packs up the family and leaves the slums for good. " +
                                 $"{FamilyService.ChildrenAlive(run)} of 5 children leave with you.";
            }
            else if (IsVictory)
            {
                Note($"{goal} days in Tondo, and the whole family is still together.", true);
                causeText.text = $"{run.playerName}, {run.Spouse?.name} and all {FamilyService.ChildrenAlive(run)} children made it through {goal} days.\n" +
                                 "Infinite Mode is now unlocked. Keep going with this family, or start a new life in Infinite Mode from the main menu." +
                                 $"\nSave ₱{gm.Config.leaveSlumsQuota:N0} and the family can leave the slums for good.";
            }
            else if (run.endKind == EndKind.Pregnancy)
            {
                Note("A sixth child is on the way.", false);
                causeText.text = (run.gender == Gender.Female
                        ? "You are pregnant."
                        : "Inday is pregnant.") +
                    $"\nWith {FamilyService.ChildrenAlive(run)} children already going hungry and ₱{run.money:N0} to your name, the family cannot survive another mouth to feed.";
            }
            else if (run.endKind == EndKind.SurvivedWithLosses)
            {
                Note($"You lasted {goal} days, but not everyone did.", false);
                causeText.text = $"{run.causeOfDeath}.\nOnly a run that keeps the whole family alive for {goal} days is a win.";
            }
            else
            {
                Note("Hardcore mode: this run has ended permanently.", false);
                causeText.text = $"Cause of death: {run.causeOfDeath}\n{run.playerName} died on Day {run.day} at {GameTime.Clock(run.minuteOfDay)}.";
            }

            statsText.text =
                $"Days survived: {VictoryRules.DaysSurvived(run, gm.Config)}" + (run.infiniteMode && run.won ? "  (won, then Infinite Mode)" : run.infiniteMode ? "  (Infinite Mode)" : "") + "\n" +
                $"Children alive: {FamilyService.ChildrenAlive(run)} of 5" + (run.childDeaths > 0 ? $"  (lost {run.childDeaths})" : "") + "\n" +
                $"Money earned: ₱{run.totalEarned:N0}   ·   Money left: ₱{run.money:N0}\n" +
                $"Money spent: ₱{run.totalSpent:N0}  (food ₱{run.foodSpent:N0} · transport ₱{run.transportSpent:N0} · other ₱{run.otherSpent:N0})\n" +
                $"Your shifts: {run.shiftsWorked}   ·   Children's shifts: {run.childShiftsWorked}   ·   Minigames played: {run.minigamesPlayed}\n" +
                $"Places unlocked: {run.unlockedLocations.Count}";

            menuButton.onClick.RemoveAllListeners();
            SetButtonLabel(menuButton, Loc.Get(IsVictory ? "world.victory.finish" : "world.gameover.menu"));
            // After a win: Finish and Keep Playing side by side, Leave the Slums centred below them.
            // Otherwise the menu button is centred alone on the bottom row.
            var menuRt = (RectTransform)menuButton.transform;
            menuRt.anchoredPosition = IsVictory && continueButton != null ? new Vector2(-230f, UpperRowY) : new Vector2(0f, LowerRowY);
            if (IsVictory) menuButton.onClick.AddListener(() => GameManager.Instance.FinishWonRun());
            else menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinue);
                continueButton.gameObject.SetActive(IsVictory);
            }
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(() => GameManager.Instance.LeaveTheSlums());
                leaveButton.gameObject.SetActive(IsVictory);
                var met = VictoryRules.MetQuota(run, gm.Config);
                leaveButton.interactable = IsVictory && met;
                SetButtonLabel(leaveButton, met
                    ? Loc.Get("world.victory.leave")
                    : Loc.Get("world.victory.leave.locked", $"{run.money:N0}", $"{gm.Config.leaveSlumsQuota:N0}"));
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Sets a button's label from code. The builder gives buttons a <see cref="LocalizedText"/> that would put its
        /// key's string back every time the overlay is enabled, so the key is cleared first.
        /// </summary>
        private static void SetButtonLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            var localized = label.GetComponent<LocalizedText>();
            if (localized != null) localized.Key = null;
            label.text = text;
        }

        private void OnContinue()
        {
            gameObject.SetActive(false);
            GameManager.Instance.ContinueInInfiniteMode();
        }

        private void SetTitle(string text, bool win)
        {
            if (titleText == null) return;
            titleText.text = text;
            titleText.color = win ? WinTitle : LossTitle;
        }

        private void Note(string text, bool win)
        {
            if (noteText == null) return;
            noteText.text = text;
            noteText.color = win ? WinNote : LossNote;
        }
    }
}
