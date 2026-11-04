using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        [Header("Labels")]
        [SerializeField] private Text continueDetailText;

        [Header("New-game confirmation (shown when a run already exists)")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Text confirmBodyText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private RunData _existingRun;

        private void Start()
        {
            newGameButton.onClick.AddListener(OnNewGame);
            continueButton.onClick.AddListener(OnContinue);
            quitButton.onClick.AddListener(OnQuit);
            confirmYesButton.onClick.AddListener(GoToCharacterCreation);
            confirmNoButton.onClick.AddListener(() => confirmPanel.SetActive(false));

            confirmPanel.SetActive(false);
            Refresh();
        }

        private void Refresh()
        {
            SaveSystem.TryLoad(out _existingRun);
            continueButton.interactable = _existingRun != null;
            continueDetailText.text = _existingRun != null
                ? Loc.Get("menu.continue.detail", _existingRun.day, _existingRun.playerName)
                : Loc.Get("menu.continue.none");
        }

        private void OnNewGame()
        {
            if (_existingRun == null)
            {
                GoToCharacterCreation();
                return;
            }
            confirmBodyText.text = Loc.Get("menu.confirm.body", _existingRun.playerName, _existingRun.day);
            confirmPanel.SetActive(true);
        }

        private void GoToCharacterCreation() => GameManager.Instance.LoadScene(SceneNames.CharacterCreation);

        private void OnContinue()
        {
            if (!GameManager.Instance.ContinueRun()) Refresh();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
