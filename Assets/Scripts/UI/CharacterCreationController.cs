using System.Text;
using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// New-run setup: character (Dong/Inday), name, difficulty mode and, once unlocked, Infinite Mode.
    /// Every choice is passed to <see cref="GameManager.StartNewRun"/> and stored in the run.
    /// </summary>
    public class CharacterCreationController : MonoBehaviour
    {
        [Header("Character choice")]
        [SerializeField] private OptionToggle maleOption;
        [SerializeField] private OptionToggle femaleOption;
        [SerializeField] private Image malePortrait;
        [SerializeField] private Image femalePortrait;

        [Header("Preview")]
        [SerializeField] private Image previewImage;
        [SerializeField] private Text previewNameText;
        [SerializeField] private Text descriptionText;

        [Header("Lore")]
        [SerializeField] private Button loreButton;
        [SerializeField] private Text loreButtonText;
        [SerializeField] private GameObject lorePanel;
        [SerializeField] private Text loreTitleText;
        [SerializeField] private Text loreBodyText;
        [SerializeField] private Button loreCloseButton;

        [Header("Name")]
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text nameErrorText;

        [Header("Difficulty")]
        [SerializeField] private OptionToggle normalOption;
        [SerializeField] private OptionToggle hardOption;
        [SerializeField] private Text difficultyDescriptionText;
        [SerializeField] private Text startingMoneyText;

        [Header("Infinite Mode (unlocked by winning a run)")]
        [SerializeField] private OptionToggle infiniteOption;

        [Header("Navigation")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private Gender _gender = Gender.Male;
        private Difficulty _difficulty = Difficulty.Normal;
        private bool _infinite;
        private bool _infiniteUnlocked;

        private GameConfig Config => GameManager.Instance.Config;
        private CharacterDatabase Characters => GameManager.Instance.Characters;

        private void Start()
        {
            malePortrait.sprite = Characters.Get(Gender.Male).portrait;
            femalePortrait.sprite = Characters.Get(Gender.Female).portrait;
            nameInput.characterLimit = Config.maxNameLength;
            normalOption.Label.text = Config.normal.FullName;
            hardOption.Label.text = Config.hard.FullName;
            startingMoneyText.text = Loc.Get("cc.money", FormatPesos(GameConfig.StartingMoney));
            _infiniteUnlocked = SaveSystem.InfiniteModeUnlocked;
            if (infiniteOption != null)
            {
                infiniteOption.Button.interactable = _infiniteUnlocked;
                infiniteOption.Button.onClick.AddListener(ToggleInfinite);
            }

            maleOption.Button.onClick.AddListener(() => SelectGender(Gender.Male));
            femaleOption.Button.onClick.AddListener(() => SelectGender(Gender.Female));
            normalOption.Button.onClick.AddListener(() => SelectDifficulty(Difficulty.Normal));
            hardOption.Button.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));
            nameInput.onValueChanged.AddListener(_ => RefreshNameState());
            loreButton.onClick.AddListener(ShowLore);
            loreCloseButton.onClick.AddListener(() => lorePanel.SetActive(false));
            startButton.onClick.AddListener(OnStart);
            backButton.onClick.AddListener(() => GameManager.Instance.LoadScene(SceneNames.MainMenu));

            lorePanel.SetActive(false);
            SelectGender(Gender.Male);
        }

        // ---------- Selection ----------

        private void SelectGender(Gender gender)
        {
            var previousDefault = Characters.Get(_gender).defaultName;
            _gender = gender;

            // Keep a custom name; swap only an empty field or the other character's default name.
            var current = nameInput.text.Trim();
            if (current.Length == 0 || current == previousDefault || current == Characters.Get(Other(gender)).defaultName)
                nameInput.text = Characters.Get(gender).defaultName;

            RefreshAll();
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            _difficulty = difficulty;
            RefreshAll();
        }

        private void ToggleInfinite()
        {
            if (!_infiniteUnlocked) return;
            _infinite = !_infinite;
            RefreshAll();
        }

        // ---------- View ----------

        private void RefreshAll()
        {
            var profile = Characters.Get(_gender);
            maleOption.SetSelected(_gender == Gender.Male);
            femaleOption.SetSelected(_gender == Gender.Female);
            normalOption.SetSelected(_difficulty == Difficulty.Normal);
            hardOption.SetSelected(_difficulty == Difficulty.Hard);

            previewImage.sprite = profile.fullBody;
            previewImage.preserveAspect = true;
            descriptionText.text = Loc.Get(profile.descriptionKey);
            loreButtonText.text = Loc.Get("cc.lore.button", profile.defaultName);
            difficultyDescriptionText.text = Config.GetDifficulty(_difficulty).description;
            if (infiniteOption != null)
            {
                infiniteOption.SetSelected(_infinite);
                infiniteOption.Label.text = !_infiniteUnlocked
                    ? Loc.Get("cc.infinite.locked", Config.victoryDays)
                    : Loc.Get(_infinite ? "cc.infinite.on" : "cc.infinite.off");
            }

            RefreshNameState();
        }

        private void RefreshNameState()
        {
            var error = NameRules.Validate(nameInput.text, Config.minNameLength, Config.maxNameLength, out var clean);
            nameErrorText.text = error ?? string.Empty;
            previewNameText.text = string.IsNullOrEmpty(clean) ? "—" : clean;
            startButton.interactable = error == null;
        }

        private void ShowLore()
        {
            var profile = Characters.Get(_gender);
            loreTitleText.text = Loc.Get("cc.lore.title", profile.defaultName);
            var sb = new StringBuilder();
            foreach (var key in profile.loreKeys)
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(Loc.Get(key, profile.defaultName));
            }
            var fam = GameManager.Instance.Family;
            if (fam != null && fam.children.Count > 0)
            {
                sb.Append("\n\n<b><color=#D9A441>THE CHILDREN</color></b>");
                foreach (var c in fam.children)
                    sb.Append($"\n<b>{c.displayName}</b>, {c.age} — {c.personality}");
            }
            loreBodyText.text = sb.ToString();
            lorePanel.SetActive(true);
        }

        private void OnStart()
        {
            if (NameRules.Validate(nameInput.text, Config.minNameLength, Config.maxNameLength, out var clean) != null) return;
            startButton.interactable = false;
            GameManager.Instance.StartNewRun(clean, _gender, _difficulty, _infinite && _infiniteUnlocked);
        }

        private static Gender Other(Gender g) => g == Gender.Male ? Gender.Female : Gender.Male;

        public static string FormatPesos(int amount) => "₱" + amount.ToString("N0");
    }
}
