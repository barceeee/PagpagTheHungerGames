using System.Collections.Generic;
using Pagpag.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.Minigames
{
    /// <summary>Parcel delivery: read the address, click the matching shop; the street keeps changing.</summary>
    public class ParcelRouteGame : Minigame
    {
        public override string Title => "Parcel Runs";
        public override string Instructions =>
            "Read the address on each parcel and click the shop it goes to.\nThe street layout changes every few deliveries, and every parcel is on a clock.";
        public override float TimeLimit => 35f;

        private static readonly string[] Shops =
            { "Botika ni Aling Cora", "Mang Ben's Barbershop", "Sunrise Bakery", "Tindahan ni Pacing", "JM Hardware", "Lola's Carinderia" };
        private static readonly Vector2[] Slots =
            { new Vector2(-390, 70), new Vector2(0, 70), new Vector2(390, 70), new Vector2(-390, -120), new Vector2(0, -120), new Vector2(390, -120) };

        private readonly int[] _shopAtSlot = { 0, 1, 2, 3, 4, 5 };
        private readonly Text[] _slotLabels = new Text[6];
        private int _target, _correct, _wrong, _late, _sinceShuffle;
        private float _parcelTime;
        private Text _parcel;
        private RectTransform _timer;

        public int SlotOfTarget => System.Array.IndexOf(_shopAtSlot, _target);
        public override float Score => Clamp01(_correct / 16f - _wrong * 0.04f);

        protected override void Build()
        {
            var card = UIKit.Rect("Parcel", Area, new Vector2(0, 220), new Vector2(620, 70));
            UIKit.Img(card, new Color(0.62f, 0.47f, 0.3f), true);
            _parcel = UIKit.Txt(UIKit.Fill("T", card, 8), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            _timer = UIKit.Bar("Timer", Area, new Vector2(0, 175), new Vector2(620, 10), UIKit.Gold, out _);
            for (var i = 0; i < 6; i++)
            {
                var slot = i;
                var b = UIKit.Btn("Shop" + i, Area, Slots[i], new Vector2(340, 150), "", () => Deliver(slot), UIKit.PanelLight, UIKit.Cream, 26);
                _slotLabels[i] = UIKit.LabelOf(b);
            }
            MakeFeedback(new Vector2(0, -235));
        }

        protected override void OnBegin()
        {
            Shuffle();
            NextParcel();
        }

        private void Shuffle()
        {
            for (var i = 5; i > 0; i--)
            {
                var j = RandInt(0, i + 1);
                (_shopAtSlot[i], _shopAtSlot[j]) = (_shopAtSlot[j], _shopAtSlot[i]);
            }
            for (var i = 0; i < 6; i++) _slotLabels[i].text = Shops[_shopAtSlot[i]];
            _sinceShuffle = 0;
        }

        private void NextParcel()
        {
            _target = RandInt(0, Shops.Length);
            _parcel.text = "Deliver to:  " + Shops[_target];
            _parcelTime = 0;
        }

        public void Deliver(int slot)
        {
            if (!Running) return;
            if (_shopAtSlot[slot] == _target)
            {
                _correct++;
                Say("Delivered!", UIKit.Good);
            }
            else
            {
                _wrong++;
                Say($"Wrong door — that's {Shops[_shopAtSlot[slot]]}.", UIKit.Danger);
            }
            if (++_sinceShuffle >= Mathf.RoundToInt(3 * Ease) + 1)
            {
                Shuffle();
                Say("New street!", UIKit.Gold);
            }
            NextParcel();
        }

        protected override void Tick(float dt)
        {
            _parcelTime += dt;
            var limit = 4.5f * Ease;
            UIKit.SetBar(_timer, 1f - _parcelTime / limit);
            if (_parcelTime >= limit)
            {
                _late++;
                Say("Too slow — the customer left.", UIKit.Danger);
                NextParcel();
            }
            Status = $"Delivered {_correct}  ·  Wrong {_wrong}  ·  Late {_late}";
        }
    }

    /// <summary>Community cleanup: sweep away piles of dirt before the court gets filthy.</summary>
    public class SweepCourtGame : Minigame
    {
        public override string Title => "Sweep the Court";
        public override string Instructions =>
            "Litter keeps piling up on the covered court. Click each pile until it's gone (each takes a few sweeps).\nDon't let the court get filthy.";
        public override float TimeLimit => 30f;

        private class Pile { public RectTransform rt; public int left; }
        private readonly List<Pile> _piles = new List<Pile>();
        private int _cleared;
        private float _spawn;
        private int ClicksNeeded => Ease >= 1.15f ? 2 : 3;

        public int PileCount => _piles.Count;
        public override float Score => Clamp01(_cleared / 24f - (Running ? 0 : _piles.Count * 0.02f));

        protected override void Build()
        {
            var court = UIKit.Fill("Court", Area, 10);
            UIKit.Img(court, new Color(0.25f, 0.3f, 0.32f), true);
            var line = UIKit.Rect("CentreLine", court, Vector2.zero, new Vector2(6, 520));
            UIKit.Img(line, new Color(1, 1, 1, 0.25f));
            MakeFeedback(new Vector2(0, 240));
        }

        protected override void Tick(float dt)
        {
            _spawn -= dt;
            if (_spawn <= 0 && _piles.Count < 8)
            {
                _spawn = 0.65f * Ease;
                var pile = new Pile { left = ClicksNeeded };
                pile.rt = UIKit.Rect("Pile", Area, new Vector2(Rand(-520, 520), Rand(-200, 190)), new Vector2(100, 100));
                UIKit.CircleImg(pile.rt, new Color(0.45f, 0.33f, 0.2f), true);
                UIKit.Txt(UIKit.Fill("T", pile.rt), "dirt", 16, UIKit.Cream);
                var b = pile.rt.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => Sweep(pile));
                _piles.Add(pile);
            }
            Status = $"Cleared {_cleared}  ·  Piles on the court {_piles.Count}";
        }

        private void Sweep(Pile pile)
        {
            if (!Running || !_piles.Contains(pile)) return;
            pile.left--;
            if (pile.left <= 0)
            {
                _cleared++;
                _piles.Remove(pile);
                Remove(pile.rt.gameObject);
                Say("Clean!", UIKit.Good);
            }
            else
            {
                var s = 40f + 60f * pile.left / ClicksNeeded;
                pile.rt.sizeDelta = new Vector2(s, s);
            }
        }

        /// <summary>For tests: one sweep on the oldest pile.</summary>
        public void SweepOldest()
        {
            if (_piles.Count > 0) Sweep(_piles[0]);
        }
    }

    /// <summary>Hand out flyers: click passers-by, avoid the ones in a hurry.</summary>
    public class FlyersGame : Minigame
    {
        public override string Title => "Hand Out Flyers";
        public override string Instructions =>
            "Click on people walking by to hand them a flyer.\nPeople marked in red are in a hurry: leave them alone or they'll complain to your boss.";
        public override float TimeLimit => 30f;

        private class Walker { public RectTransform rt; public float speed; public bool hurried; }
        private static readonly float[] Rows = { 160, 80, 0, -80, -160 };
        private readonly List<Walker> _walkers = new List<Walker>();
        private int _handed, _rejected;
        private float _spawn;

        public override float Score => Clamp01(_handed / 16f - _rejected * 0.06f);

        protected override void Build()
        {
            var road = UIKit.Fill("Sidewalk", Area, 10);
            UIKit.Img(road, new Color(0.3f, 0.3f, 0.32f), true);
            MakeFeedback(new Vector2(0, 245));
        }

        protected override void Tick(float dt)
        {
            _spawn -= dt;
            if (_spawn <= 0)
            {
                _spawn = 0.7f;
                var fromLeft = Rng.NextDouble() < 0.5;
                var w = new Walker { hurried = Rng.NextDouble() < 0.25, speed = Rand(150, 260) / Ease * (fromLeft ? 1 : -1) };
                w.rt = UIKit.Rect("Walker", Area, new Vector2(fromLeft ? -640 : 640, Pick(Rows)), new Vector2(70, 100));
                var img = UIKit.Img(w.rt, w.hurried ? UIKit.Danger : new Color(0.4f, 0.55f, 0.75f), true, true);
                UIKit.Txt(UIKit.Fill("T", w.rt), w.hurried ? "In a\nhurry" : "", 14, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
                var b = w.rt.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                b.onClick.AddListener(() => Offer(w));
                _walkers.Add(w);
            }
            for (var i = _walkers.Count - 1; i >= 0; i--)
            {
                var w = _walkers[i];
                var p = w.rt.anchoredPosition;
                p.x += w.speed * dt;
                w.rt.anchoredPosition = p;
                if (Mathf.Abs(p.x) > 680)
                {
                    Remove(w.rt.gameObject);
                    _walkers.RemoveAt(i);
                }
            }
            Status = $"Flyers handed out {_handed}  ·  Complaints {_rejected}";
        }

        private void Offer(Walker w)
        {
            if (!Running || !_walkers.Contains(w)) return;
            if (w.hurried) { _rejected++; Say("\"Not now!\" — a complaint.", UIKit.Danger); }
            else { _handed++; Say("Flyer taken.", UIKit.Good); }
            _walkers.Remove(w);
            Remove(w.rt.gameObject);
        }

        /// <summary>For tests: offer a flyer to the first walker of the given kind.</summary>
        public bool OfferFirst(bool hurried)
        {
            var w = _walkers.Find(x => x.hurried == hurried);
            if (w == null) return false;
            Offer(w);
            return true;
        }
    }

    /// <summary>Laundry: alternate scrubbing, then hold to wring.</summary>
    public class LaundryGame : Minigame
    {
        public override string Title => "Scrub and Wring";
        public override string Instructions =>
            "Scrub out each stain by alternating ◄ and ► (A/D, arrow keys or the buttons). Pressing the same side twice slips.\nThen hold WRING (or Space) until the piece is dry. 6 pieces.";
        public override float TimeLimit => 40f;
        public const int Pieces = 6;

        private static readonly string[] Names = { "School uniform", "Bedsheet", "Work jeans", "Towel", "Barong", "Baby clothes", "Curtain" };
        private int _done, _lastDir;
        private float _stain = 100f, _wring;
        private bool _wringing;
        private Text _pieceName;
        private Image _stainImg;
        private RectTransform _stainBar, _wringBar;
        private HoldButton _wringButton;

        public bool Wringing => _wringing;
        public override float Score => Clamp01((_done + (_wringing ? 0.5f + _wring * 0.5f : (100f - _stain) / 200f)) / Pieces);

        protected override void Build()
        {
            var cloth = UIKit.Rect("Cloth", Area, new Vector2(0, 90), new Vector2(380, 210));
            UIKit.Img(cloth, new Color(0.85f, 0.85f, 0.8f), true);
            _stainImg = UIKit.CircleImg(UIKit.Rect("Stain", cloth, Vector2.zero, new Vector2(160, 120)), new Color(0.4f, 0.3f, 0.15f, 1f));
            _pieceName = UIKit.Label("Name", Area, new Vector2(0, 225), new Vector2(600, 40), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Label("StainLabel", Area, new Vector2(-330, -40), new Vector2(200, 24), "Stain", 18, UIKit.Muted);
            _stainBar = UIKit.Bar("StainBar", Area, new Vector2(-330, -65), new Vector2(260, 14), new Color(0.6f, 0.45f, 0.25f), out _);
            UIKit.Label("WringLabel", Area, new Vector2(330, -40), new Vector2(200, 24), "Wring", 18, UIKit.Muted);
            _wringBar = UIKit.Bar("WringBar", Area, new Vector2(330, -65), new Vector2(260, 14), new Color(0.35f, 0.6f, 0.9f), out _);
            UIKit.Btn("ScrubLeft", Area, new Vector2(-330, -165), new Vector2(240, 90), "◄ Scrub", () => Scrub(-1), null, null, 28);
            UIKit.Btn("ScrubRight", Area, new Vector2(330, -165), new Vector2(240, 90), "Scrub ►", () => Scrub(1), null, null, 28);
            _wringButton = UIKit.Btn("Wring", Area, new Vector2(0, -165), new Vector2(240, 90), "WRING", null, UIKit.Gold, UIKit.Dark, 28)
                .gameObject.AddComponent<HoldButton>();
            MakeFeedback(new Vector2(0, -245));
        }

        protected override void OnBegin() => NextPiece();

        private void NextPiece()
        {
            _stain = 100f;
            _wring = 0;
            _wringing = false;
            _lastDir = 0;
            _pieceName.text = Names[RandInt(0, Names.Length)];
        }

        public void Scrub(int dir)
        {
            if (!Running || _wringing) return;
            if (dir == _lastDir)
            {
                Say("Slip! Alternate sides.", UIKit.Danger);
                return;
            }
            _lastDir = dir;
            _stain = Mathf.Max(0, _stain - 7f * Ease);
            if (_stain <= 0)
            {
                _wringing = true;
                Say("Clean! Now wring it out.", UIKit.Good);
            }
        }

        public void Wring(float dt, bool held)
        {
            if (!Running || !_wringing) return;
            _wring = held ? _wring + 0.55f * Ease * dt : Mathf.Max(0, _wring - 0.3f * dt);
            if (_wring >= 1f)
            {
                _done++;
                Say($"Piece {_done} done!", UIKit.Good);
                if (_done >= Pieces) { End(); return; }
                NextPiece();
            }
        }

        protected override void Tick(float dt)
        {
            if (PressedAny(Key.A, Key.LeftArrow)) Scrub(-1);
            if (PressedAny(Key.D, Key.RightArrow)) Scrub(1);
            Wring(dt, _wringButton.Held || Held(Key.Space));
            var c = _stainImg.color;
            c.a = _stain / 100f;
            _stainImg.color = c;
            UIKit.SetBar(_stainBar, _stain / 100f);
            UIKit.SetBar(_wringBar, _wring);
            Status = $"Pieces done {_done}/{Pieces}";
        }
    }

    /// <summary>Selling vegetables: hit the customer's exact weight on the scale.</summary>
    public class WeighVegGame : Minigame
    {
        public override string Title => "Weigh and Sell";
        public override string Instructions =>
            "Each customer wants an exact weight. Add vegetables to the scale, then press SELL when it matches.\nCustomers won't wait forever.";
        public override float TimeLimit => 55f;
        public const int Customers = 8;

        private static readonly string[] Veg = { "kangkong", "tomatoes", "onions", "pechay", "string beans", "eggplant", "squash" };
        private int _customer, _targetTenths, _currentTenths;
        private float _sum, _patience;
        private Text _order, _scale;
        private RectTransform _patienceBar;

        public int TargetTenths => _targetTenths;
        public int CurrentTenths => _currentTenths;
        public override float Score => _sum / Customers;

        protected override void Build()
        {
            _order = UIKit.Label("Order", Area, new Vector2(0, 215), new Vector2(900, 50), "", 30, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            _patienceBar = UIKit.Bar("Patience", Area, new Vector2(0, 175), new Vector2(500, 12), UIKit.Danger, out _);
            var scale = UIKit.Rect("Scale", Area, new Vector2(0, 60), new Vector2(340, 150));
            UIKit.Img(scale, UIKit.PanelLight, true);
            _scale = UIKit.Txt(UIKit.Fill("T", scale), "", 56, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            var adds = new[] { 5, 2, 1, -1 };
            for (var i = 0; i < adds.Length; i++)
            {
                var a = adds[i];
                UIKit.Btn("Add" + a, Area, new Vector2(-450 + i * 180, -110), new Vector2(160, 80),
                    a > 0 ? $"+{a / 10f:0.0} kg" : $"−{-a / 10f:0.0} kg", () => Add(a), null, null, 26);
            }
            UIKit.Btn("Clear", Area, new Vector2(270, -110), new Vector2(160, 80), "Clear", () => { _currentTenths = 0; }, null, null, 24);
            UIKit.Btn("Sell", Area, new Vector2(450, -110), new Vector2(160, 80), "SELL", Sell, UIKit.Gold, UIKit.Dark, 28);
            MakeFeedback(new Vector2(0, -215));
        }

        protected override void OnBegin() => NextCustomer();

        private void NextCustomer()
        {
            if (_customer >= Customers) { End(); return; }
            _customer++;
            _targetTenths = RandInt(4, 21);
            _currentTenths = 0;
            _patience = 0;
            _order.text = $"Customer {_customer}: \"{_targetTenths / 10f:0.0} kg of {Pick(Veg)}, please.\"";
        }

        public void Add(int tenths)
        {
            if (!Running) return;
            _currentTenths = Mathf.Clamp(_currentTenths + tenths, 0, 40);
        }

        public void Sell()
        {
            if (!Running) return;
            var off = Mathf.Abs(_currentTenths - _targetTenths);
            var acc = off == 0 ? 1f : off == 1 ? 0.6f : off == 2 ? 0.3f : 0f;
            _sum += acc;
            Say(off == 0 ? "Exact! Happy customer." : off <= 2 ? "Close enough..." : "Way off! The customer complains.",
                off == 0 ? UIKit.Good : off <= 2 ? UIKit.Gold : UIKit.Danger);
            NextCustomer();
        }

        protected override void Tick(float dt)
        {
            _patience += dt;
            var limit = 9f * Ease;
            UIKit.SetBar(_patienceBar, 1f - _patience / limit);
            if (_patience >= limit)
            {
                Say("The customer walked away.", UIKit.Danger);
                NextCustomer();
            }
            if (_scale != null) _scale.text = $"{_currentTenths / 10f:0.0} kg";
            Status = $"Customer {_customer}/{Customers}  ·  Sales score {Mathf.RoundToInt(_sum / Mathf.Max(1, _customer - 1) * 100)}%";
        }
    }

    /// <summary>Bundling vegetables: memory pairs.</summary>
    public class MemoryMatchGame : Minigame
    {
        public override string Title => "Bundle Match";
        public override string Instructions =>
            "Match pairs of vegetables to tie them into bundles. Flip two cards at a time.\nYou get a short look at all the cards first.";
        public override float TimeLimit => 45f;
        public const int Pairs = 6;

        private static readonly string[] Veg = { "Kangkong", "Pechay", "Sitaw", "Okra", "Talong", "Ampalaya" };
        private readonly List<int> _cards = new List<int>();
        private readonly List<Text> _labels = new List<Text>();
        private readonly List<Image> _backs = new List<Image>();
        private readonly bool[] _matched = new bool[12];
        private int _first = -1, _second = -1, _pairs;
        private float _preview, _hideTimer;

        public int CardValue(int i) => _cards[i];
        public bool Previewing => _preview > 0;
        public bool Busy => _second >= 0;
        public override float Score => (float)_pairs / Pairs;

        protected override void Build()
        {
            for (var p = 0; p < Pairs; p++) { _cards.Add(p); _cards.Add(p); }
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = RandInt(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
            for (var i = 0; i < 12; i++)
            {
                var idx = i;
                var b = UIKit.Btn("Card" + i, Area, new Vector2(-390 + (i % 4) * 260, 150 - (i / 4) * 150), new Vector2(230, 130), "",
                    () => Flip(idx), UIKit.PanelLight, UIKit.Cream, 26);
                _labels.Add(UIKit.LabelOf(b));
                _backs.Add((Image)b.targetGraphic);
            }
            MakeFeedback(new Vector2(0, -250));
        }

        protected override void OnBegin()
        {
            _preview = 1.6f * Ease;
            for (var i = 0; i < 12; i++) _labels[i].text = Veg[_cards[i]];
            Say("Memorize!", UIKit.Gold);
        }

        public void Flip(int i)
        {
            if (!Running || Previewing || Busy || _matched[i] || i == _first) return;
            _labels[i].text = Veg[_cards[i]];
            _backs[i].color = UIKit.Gold;
            _labels[i].color = UIKit.Dark;
            if (_first < 0) { _first = i; return; }
            _second = i;
            if (_cards[_first] == _cards[_second])
            {
                _matched[_first] = _matched[_second] = true;
                _backs[_first].color = _backs[_second].color = UIKit.Good;
                _pairs++;
                Say($"{Veg[_cards[i]]} bundled!", UIKit.Good);
                _first = _second = -1;
                if (_pairs >= Pairs) End();
            }
            else
            {
                _hideTimer = 0.6f;
                Say("Not a pair.", UIKit.Danger);
            }
        }

        protected override void Tick(float dt)
        {
            if (_preview > 0)
            {
                _preview -= dt;
                if (_preview <= 0)
                    for (var i = 0; i < 12; i++) _labels[i].text = "?";
            }
            if (_second >= 0)
            {
                _hideTimer -= dt;
                if (_hideTimer <= 0)
                {
                    foreach (var c in new[] { _first, _second })
                    {
                        _labels[c].text = "?";
                        _labels[c].color = UIKit.Cream;
                        _backs[c].color = UIKit.PanelLight;
                    }
                    _first = _second = -1;
                }
            }
            Status = $"Bundles {_pairs}/{Pairs}";
        }
    }
}
