using Pagpag.Core;
using Pagpag.Jobs;
using Pagpag.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.Minigames
{
    /// <summary>
    /// Full-screen overlay that runs a job's minigame: briefing → play → result → collect pay.
    /// Listens for <see cref="GameEvents.MinigameRequested"/>. Builds its own UI.
    /// </summary>
    public class MinigameHost : MonoBehaviour
    {
        public static MinigameHost Instance { get; private set; }

        private RectTransform _root, _area;
        private GameObject _briefing, _result, _playBar;
        private Text _title, _worker, _pay, _instructions, _timeText, _perfText, _statusText, _resultTitle, _resultBody;
        private RectTransform _timeFill;
        private Button _quitButton;

        private Minigame _game;
        private MinigameRequest _request;
        private float _finalScore;

        public Minigame Current => _game;
        public bool IsOpen => _request != null;

        private void Awake()
        {
            Instance = this;
            BuildUI();
            _root.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.MinigameRequested += Open;
        private void OnDisable() => GameEvents.MinigameRequested -= Open;

        private void BuildUI()
        {
            _root = UIKit.Fill("Overlay", transform);
            UIKit.Img(_root, new Color(0, 0, 0, 0.88f), false, true);
            var win = UIKit.Rect("Window", _root, Vector2.zero, new Vector2(1320, 900));
            var fit = win.gameObject.AddComponent<FitInParent>();
            fit.designSize = new Vector2(1320, 900);
            fit.reserved = new Vector2(32, 32);
            UIKit.Img(win, UIKit.Panel, true, true);

            _title = UIKit.Label("Title", win, new Vector2(0, 410), new Vector2(1200, 50), "", 36, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, null, true);
            _worker = UIKit.Label("Worker", win, new Vector2(0, 368), new Vector2(1200, 30), "", 21, UIKit.Cream);
            _pay = UIKit.Label("Pay", win, new Vector2(0, 338), new Vector2(1200, 28), "", 20, UIKit.Good);

            _area = UIKit.Rect("PlayArea", win, new Vector2(0, 30), new Vector2(1240, 560));
            UIKit.Img(_area, new Color(0.04f, 0.05f, 0.07f), true);

            _playBar = UIKit.Rect("PlayBar", win, new Vector2(0, -320), new Vector2(1240, 100)).gameObject;
            _timeFill = UIKit.Bar("Time", _playBar.transform, new Vector2(-250, 20), new Vector2(640, 18), UIKit.Gold, out _);
            _timeText = UIKit.Label("TimeText", _playBar.transform, new Vector2(150, 20), new Vector2(160, 30), "", 22, UIKit.Cream, TextAnchor.MiddleLeft);
            _perfText = UIKit.Label("Performance", _playBar.transform, new Vector2(450, 20), new Vector2(340, 30), "", 24, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            _statusText = UIKit.Label("Status", _playBar.transform, new Vector2(-100, -22), new Vector2(1000, 28), "", 20, UIKit.Muted, TextAnchor.MiddleLeft);
            _quitButton = UIKit.Btn("WalkAway", win, new Vector2(520, -400), new Vector2(220, 56), "Walk away", WalkAway, new Color(0.4f, 0.18f, 0.18f), UIKit.Cream, 20);

            // Briefing
            var brief = UIKit.Rect("Briefing", win, new Vector2(0, 30), new Vector2(1240, 560));
            UIKit.Img(brief, new Color(0.04f, 0.05f, 0.07f, 1f), true, true);
            _briefing = brief.gameObject;
            UIKit.Label("HowTo", brief, new Vector2(0, 200), new Vector2(1000, 40), "How to play", 30, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _instructions = UIKit.Label("Instructions", brief, new Vector2(0, 60), new Vector2(1000, 220), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Normal, null, true);
            UIKit.Label("PayRule", brief, new Vector2(0, -90), new Vector2(1000, 60),
                "Pay depends on how well you do: 80% performance earns the full wage, a perfect game up to 125%, and below 15% nothing.",
                20, UIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic, null, true);
            UIKit.Btn("Start", brief, new Vector2(0, -190), new Vector2(320, 80), "Start (Enter)", BeginGame, UIKit.Gold, UIKit.Dark, 30);

            // Result
            var res = UIKit.Rect("Result", win, new Vector2(0, 30), new Vector2(1240, 560));
            UIKit.Img(res, new Color(0.04f, 0.05f, 0.07f, 1f), true, true);
            _result = res.gameObject;
            _resultTitle = UIKit.Label("ResultTitle", res, new Vector2(0, 120), new Vector2(1000, 70), "", 52, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _resultBody = UIKit.Label("ResultBody", res, new Vector2(0, 10), new Vector2(1000, 120), "", 30, UIKit.Cream, TextAnchor.MiddleCenter);
            UIKit.Btn("Collect", res, new Vector2(0, -150), new Vector2(320, 80), "Collect (Enter)", Collect, UIKit.Gold, UIKit.Dark, 30);
        }

        public void Open(MinigameRequest request)
        {
            Close();
            _request = request;
            var ctx = request.context;
            _root.gameObject.SetActive(true);
            transform.SetAsLastSibling();

            var holder = UIKit.Fill("Game", _area);
            _game = MinigameFactory.Create(ctx.minigameId, holder.gameObject);
            if (_game == null)
            {
                Debug.LogError($"[Minigame] Unknown minigame '{ctx.minigameId}' for {ctx.jobName}");
                GameEvents.PostMessage("This job has no minigame configured.", true);
                var r = _request;
                Close();
                r.onComplete?.Invoke(new MinigameResult { completed = false, score = 0 });
                return;
            }
            _game.Setup(holder, ctx);

            _title.text = $"{ctx.jobName}  —  {_game.Title}";
            _worker.text = (ctx.isChild ? "Worker: " : "") + $"{ctx.workerName}  ·  {ctx.locationName}" +
                           (string.IsNullOrEmpty(ctx.workerNote) ? "" : $"  ·  {ctx.workerNote}");
            _pay.text = $"Full pay ₱{ctx.fullPay} at 80%  ·  up to ₱{ctx.maxPay} for a perfect game";
            _instructions.text = _game.Instructions;
            _briefing.SetActive(true);
            _result.SetActive(false);
            _playBar.SetActive(true);
            _quitButton.gameObject.SetActive(true);
            UpdateBar();
        }

        public void BeginGame()
        {
            if (_game == null || _game.Running || _game.Finished) return;
            _briefing.SetActive(false);
            _game.Begin(OnGameFinished);
        }

        private void OnGameFinished(float score)
        {
            _finalScore = score;
            var ctx = _request.context;
            var pay = JobRules.Payout(ctx.wage, score, ctx.payMultiplier);
            _resultTitle.text = $"{JobRules.Grade(score)} — {Mathf.RoundToInt(score * 100)}%";
            _resultTitle.color = pay > 0 ? UIKit.Gold : UIKit.Danger;
            _resultBody.text = pay > 0
                ? $"{ctx.workerName} earned ₱{pay}."
                : "That wasn't good enough. No pay for this job.";
            _result.SetActive(true);
            _quitButton.gameObject.SetActive(false);
        }

        public void Collect()
        {
            if (_request == null || _game == null || !_game.Finished) return;
            var r = _request;
            var score = _finalScore;
            Close();
            r.onComplete?.Invoke(new MinigameResult { completed = true, score = score });
        }

        public void WalkAway()
        {
            if (_request == null) return;
            var r = _request;
            _game?.Abort();
            Close();
            r.onComplete?.Invoke(new MinigameResult { completed = false, score = 0 });
        }

        private void Close()
        {
            if (_game != null) Destroy(_game.gameObject);
            _game = null;
            _request = null;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_game == null) return;
            var enter = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
            if (enter && _briefing.activeSelf) BeginGame();
            else if (enter && _result.activeSelf) Collect();
            if (_game != null) UpdateBar();
        }

        private void UpdateBar()
        {
            var limit = Mathf.Max(1f, _game.TimeLimit);
            var left = _game.Running ? _game.TimeLeft : _game.Finished ? 0 : limit;
            UIKit.SetBar(_timeFill, left / limit);
            _timeText.text = $"{Mathf.CeilToInt(Mathf.Max(0, left))}s";
            _perfText.text = $"Performance {Mathf.RoundToInt(Mathf.Clamp01(_game.Score) * 100)}%";
            _statusText.text = _game.Status;
        }
    }
}
