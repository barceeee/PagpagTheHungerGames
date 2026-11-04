using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Minigames;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// The intimacy event: a non-explicit fade to black, then a fertility wheel.
    /// The wheel has <see cref="WheelRequest.segments"/> equal segments and exactly one is the pregnancy
    /// segment, so what you see matches the odds (1 in 20 = 5%). The outcome is rolled before the spin
    /// and the wheel is animated to stop inside a segment of that outcome.
    /// </summary>
    public class WheelOverlay : MonoBehaviour
    {
        public static WheelOverlay Instance { get; private set; }

        private RectTransform _root, _wheel;
        private Image _fade;
        private Text _sceneText, _resultText, _legend;
        private GameObject _scenePage, _wheelPage;
        private Button _continueButton, _spinButton, _doneButton;
        private readonly List<GameObject> _segments = new List<GameObject>();

        private WheelRequest _request;
        private float _fadeT = -1f, _spinT = -1f, _startAngle, _targetAngle;
        private const float SpinSeconds = 4.2f;

        public bool IsOpen => _request != null;
        public bool Spinning => _spinT >= 0f;
        public float WheelAngle => _wheel != null ? _wheel.localEulerAngles.z : 0f;

        private void Awake()
        {
            Instance = this;
            _root = UIKit.Fill("Overlay", transform);
            Build();
            _root.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.WheelRequested += Open;
        private void OnDisable() => GameEvents.WheelRequested -= Open;

        private void Build()
        {
            _fade = UIKit.Img(_root, Color.black, false, true);

            var scene = FittedPage("ScenePage");
            _scenePage = scene.gameObject;
            _sceneText = UIKit.Label("Text", scene, new Vector2(0, 60), new Vector2(1100, 300), "", 32, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Italic, null, true);
            _sceneText.lineSpacing = 1.2f;
            _continueButton = UIKit.Btn("Continue", scene, new Vector2(0, -220), new Vector2(300, 70), "Continue", ShowWheel, UIKit.ButtonColor, UIKit.Cream, 26);

            var page = FittedPage("WheelPage");
            _wheelPage = page.gameObject;
            UIKit.Label("Title", page, new Vector2(0, 430), new Vector2(1200, 60), "Weeks later…", 44, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Label("Subtitle", page, new Vector2(0, 380), new Vector2(1200, 36), "Spin the wheel of fate.", 24, UIKit.Cream);

            _wheel = UIKit.Rect("Wheel", page, new Vector2(-180, 0), new Vector2(560, 560));
            var pointer = UIKit.Label("Pointer", page, new Vector2(-180, 300), new Vector2(110, 110), "▼", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            pointer.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = pointer.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);
            var hub = UIKit.Rect("Hub", page, new Vector2(-180, 0), new Vector2(110, 110));
            UIKit.CircleImg(hub, UIKit.Panel);
            UIKit.Txt(UIKit.Fill("T", hub), "5%", 30, UIKit.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);

            _legend = UIKit.Label("Legend", page, new Vector2(380, 80), new Vector2(520, 220), "", 24, UIKit.Cream, TextAnchor.UpperLeft, FontStyle.Normal, null, true);
            _resultText = UIKit.Label("Result", page, new Vector2(380, -120), new Vector2(520, 200), "", 28, UIKit.Cream, TextAnchor.UpperLeft, FontStyle.Bold, null, true);
            _spinButton = UIKit.Btn("Spin", page, new Vector2(380, -300), new Vector2(300, 80), "SPIN", Spin, UIKit.Gold, UIKit.Dark, 32);
            _doneButton = UIKit.Btn("Done", page, new Vector2(380, -300), new Vector2(300, 80), "Continue", Finish, UIKit.ButtonColor, UIKit.Cream, 28);
        }

        /// <summary>A 1920×1080 page that shrinks to fit smaller screens.</summary>
        private RectTransform FittedPage(string name)
        {
            var rt = UIKit.Rect(name, _root, Vector2.zero, new Vector2(1920, 1080));
            var fit = rt.gameObject.AddComponent<FitInParent>();
            fit.designSize = new Vector2(1920, 1080);
            return rt;
        }

        private void BuildSegments(int count)
        {
            foreach (var s in _segments) Destroy(s);
            _segments.Clear();
            var step = 360f / count;
            for (var i = 0; i < count; i++)
            {
                var seg = UIKit.Rect("Segment" + i, _wheel, Vector2.zero, new Vector2(540, 540));
                var img = seg.gameObject.AddComponent<Image>();
                img.sprite = UIKit.Circle;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount = 1f / count - 0.002f;
                img.raycastTarget = false;
                img.color = i == 0 ? new Color(0.85f, 0.15f, 0.2f) : i % 2 == 0 ? new Color(0.22f, 0.3f, 0.42f) : new Color(0.3f, 0.4f, 0.55f);
                seg.localRotation = Quaternion.Euler(0, 0, -i * step);
                if (i == 0)
                {
                    // Label along the middle of the pregnancy segment
                    var label = UIKit.Rect("Label", seg, Vector2.zero, new Vector2(40, 200));
                    label.localRotation = Quaternion.Euler(0, 0, -step / 2f);
                    var t = UIKit.Txt(UIKit.Rect("T", label, new Vector2(0, 160), new Vector2(40, 120)), "B\nA\nB\nY", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    t.lineSpacing = 0.8f;
                }
                _segments.Add(seg.gameObject);
            }
            var rim = UIKit.Rect("Rim", _wheel, Vector2.zero, new Vector2(560, 560));
            var rimImg = rim.gameObject.AddComponent<Image>();
            rimImg.sprite = UIKit.Ring;
            rimImg.color = UIKit.Gold;
            rimImg.raycastTarget = false;
            _segments.Add(rim.gameObject);
        }

        public void Open(WheelRequest request)
        {
            _request = request;
            transform.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            _scenePage.SetActive(true);
            _wheelPage.SetActive(false);
            _sceneText.text = request.sceneText;
            _fadeT = 0f;
            _spinT = -1f;
            var pct = Mathf.RoundToInt(request.pregnancyChance * 100);
            var who = request.playerIsInday ? "You become pregnant" : "Inday becomes pregnant";
            _legend.text = $"<color=#E05050>■ 1 of {request.segments} segments ({pct}%)</color>\n{who} — GAME OVER: the family cannot survive a sixth child.\n\n" +
                           $"<color=#8FA8C8>■ {request.segments - 1} of {request.segments} segments ({100 - pct}%)</color>\nNot this time.";
            _resultText.text = "";
            BuildSegments(request.segments);
            _wheelPage.transform.Find("Pointer").SetAsLastSibling();
            _wheel.localRotation = Quaternion.identity;
        }

        public void ShowWheel()
        {
            if (_request == null) return;
            _scenePage.SetActive(false);
            _wheelPage.SetActive(true);
            _spinButton.gameObject.SetActive(true);
            _doneButton.gameObject.SetActive(false);
        }

        /// <summary>Angle (degrees, clockwise from the top) of the middle of segment i.</summary>
        public static float SegmentCenter(int i, int count) => (i + 0.5f) * 360f / count;

        /// <summary>Which segment is under the pointer for a given wheel rotation (z, counter-clockwise).</summary>
        public static int SegmentAtPointer(float wheelZ, int count)
        {
            var a = Mathf.Repeat(wheelZ, 360f);
            return Mathf.FloorToInt(a / (360f / count)) % count;
        }

        public void Spin()
        {
            if (_request == null || Spinning) return;
            var count = _request.segments;
            var step = 360f / count;
            var segment = _request.pregnant ? 0 : Random.Range(1, count);
            var jitter = Random.Range(-step * 0.35f, step * 0.35f);
            _startAngle = _wheel.localEulerAngles.z;
            // Rotating the wheel by +z (counter-clockwise) brings the segment at clockwise angle z to the top.
            _targetAngle = 360f * 6 + SegmentCenter(segment, count) + jitter;
            _spinT = 0f;
            _spinButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_request == null) return;
            if (_fadeT >= 0f)
            {
                _fadeT += Time.unscaledDeltaTime;
                var a = Mathf.Clamp01(_fadeT / 1.2f);
                _fade.color = new Color(0, 0, 0, a);
                _sceneText.color = new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, Mathf.Clamp01((_fadeT - 0.8f) / 1f));
                if (_fadeT > 2f) _fadeT = -1f;
            }
            if (_spinT >= 0f) StepSpin(Time.unscaledDeltaTime);
        }

        /// <summary>Advance the spin animation (public for tests).</summary>
        public void StepSpin(float dt)
        {
            if (_spinT < 0f) return;
            _spinT += dt;
            var t = Mathf.Clamp01(_spinT / SpinSeconds);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            _wheel.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(_startAngle, _targetAngle, eased));
            if (t < 1f) return;
            _spinT = -1f;
            var landed = SegmentAtPointer(_targetAngle, _request.segments);
            var pregnant = landed == 0;
            _resultText.text = pregnant
                ? (_request.playerIsInday ? "<color=#E05050>The wheel stops on red.\nYou are pregnant.</color>" : "<color=#E05050>The wheel stops on red.\nInday is pregnant.</color>")
                : "<color=#8FD47A>Not this time.</color>\nThe two of you fall asleep holding hands.";
            _request.pregnant = pregnant;
            _doneButton.gameObject.SetActive(true);
        }

        public void Finish()
        {
            if (_request == null || Spinning) return;
            var r = _request;
            _request = null;
            _root.gameObject.SetActive(false);
            r.onComplete?.Invoke(r.pregnant);
        }
    }
}
