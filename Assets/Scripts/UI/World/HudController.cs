using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Locations;
using Pagpag.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>Persistent top bar: who, when, where, money and survival meters (100 = full/best).</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text nameText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text dayText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text locationText;
        [SerializeField] private Text moneyText;
        [SerializeField] private StatBar healthBar;
        [SerializeField] private StatBar hungerBar;
        [SerializeField] private StatBar fatigueBar;

        private bool _depressed;

        public void Refresh(RunData run, CharacterProfile profile, DifficultySettings diff, LocationData location, GameConfig cfg)
        {
            portrait.sprite = profile.portrait;
            nameText.text = run.playerName;
            _depressed = run.IsDepressed;
            var sick = Survival.IllnessRules.Describe(run.playerIllness, run.AbsMinute, GameManager.Instance != null ? GameManager.Instance.Family : null);
            if (sick != null)
            {
                difficultyText.text = "SICK · " + sick;
                difficultyText.color = new Color(1f, 0.55f, 0.45f);
            }
            else if (_depressed)
            {
                var left = run.DepressionMinutesLeft;
                difficultyText.text = $"DEPRESSED · {left / GameTime.MinutesPerDay}d {left % GameTime.MinutesPerDay / 60}h";
                difficultyText.color = new Color(0.78f, 0.62f, 1f);
            }
            else
            {
                difficultyText.text = diff.FullName + (run.developerRun ? "  ·  DEV" : "");
                difficultyText.color = new Color(0.85f, 0.64f, 0.25f);
            }
            var goal = run.infiniteMode ? " (Infinite)" : $"/{cfg.victoryDays}";
            dayText.text = $"Day {run.day}{goal}  ·  {GameTime.PeriodName(GameTime.PeriodOf(run.minuteOfDay))}";
            timeText.text = GameTime.Clock(run.minuteOfDay);
            locationText.text = location != null ? location.displayName : "—";
            moneyText.text = CharacterCreationController.FormatPesos(run.money);
            moneyText.color = run.money < 100 ? new Color(1f, 0.55f, 0.45f) : new Color(0.55f, 0.85f, 0.45f);

            healthBar.SetValue(run.health / 100f, Mathf.CeilToInt(run.health).ToString(), run.health <= cfg.healthWarning);
            hungerBar.SetValue(run.hunger / 100f, Mathf.CeilToInt(run.hunger).ToString(), run.hunger <= cfg.hungerWarning);
            fatigueBar.SetValue(run.energy / 100f, Mathf.CeilToInt(run.energy).ToString(), run.energy <= cfg.energyWarning);
        }

        private void Update()
        {
            // Grief tints the player's portrait and name until the week is over.
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            portrait.color = _depressed ? Color.Lerp(new Color(0.55f, 0.5f, 0.75f), new Color(0.75f, 0.7f, 0.95f), pulse) : Color.white;
            nameText.color = _depressed ? new Color(0.78f, 0.62f, 1f) : new Color(0.93f, 0.92f, 0.88f);
        }
    }
}
