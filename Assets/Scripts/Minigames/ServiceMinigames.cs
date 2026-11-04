using System.Collections.Generic;
using System.Linq;
using Pagpag.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.Minigames
{
    /// <summary>Minding the store: give exact change.</summary>
    public class MakeChangeGame : Minigame
    {
        public override string Title => "Make Change";
        public override string Instructions =>
            "Mind the counter: each customer pays, and you hand back the exact change.\nClick coins and bills to add them, then GIVE CHANGE. Customers won't wait forever.";
        public override float TimeLimit => 60f;
        public const int Customers = 8;

        private static readonly (string item, int price)[] Items =
        {
            ("a coffee sachet", 8), ("two eggs", 18), ("pancit canton", 16), ("a can of sardines", 27), ("a kilo of rice", 52),
            ("cooking oil", 68), ("a loaf of bread", 35), ("a shampoo sachet", 7), ("laundry soap", 29), ("a softdrink", 23),
            ("a bag of sugar", 43), ("candles", 14)
        };
        private static readonly int[] Coins = { 1, 5, 10, 20, 50, 100 };
        private readonly List<int> _given = new List<int>();
        private int _customer, _change, _correct, _wrong;
        private float _patience;
        private Text _order, _total;
        private RectTransform _patienceBar;

        public int ChangeDue => _change;
        public int Given => _given.Sum();
        public override float Score => (float)_correct / Customers;

        protected override void Build()
        {
            _order = UIKit.Label("Order", Area, new Vector2(0, 210), new Vector2(1000, 60), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            _patienceBar = UIKit.Bar("Patience", Area, new Vector2(0, 165), new Vector2(500, 12), UIKit.Danger, out _);
            var box = UIKit.Rect("Given", Area, new Vector2(0, 80), new Vector2(420, 100));
            UIKit.Img(box, UIKit.PanelLight, true);
            _total = UIKit.Txt(UIKit.Fill("T", box), "", 34, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (var i = 0; i < Coins.Length; i++)
            {
                var v = Coins[i];
                UIKit.Btn("Coin" + v, Area, new Vector2(-475 + i * 190, -55), new Vector2(170, 90), $"₱{v}", () => AddCoin(v),
                    v >= 20 ? new Color(0.3f, 0.5f, 0.35f) : new Color(0.55f, 0.5f, 0.35f), UIKit.Cream, 30);
            }
            UIKit.Btn("Undo", Area, new Vector2(-260, -170), new Vector2(200, 70), "Undo", Undo, null, null, 24);
            UIKit.Btn("ClearChange", Area, new Vector2(-40, -170), new Vector2(200, 70), "Clear", () => _given.Clear(), null, null, 24);
            UIKit.Btn("Give", Area, new Vector2(240, -170), new Vector2(300, 70), "GIVE CHANGE", Give, UIKit.Gold, UIKit.Dark, 26);
            MakeFeedback(new Vector2(0, -240));
        }

        protected override void OnBegin() => NextCustomer();

        private void NextCustomer()
        {
            if (_customer >= Customers) { End(); return; }
            _customer++;
            var (item, price) = Items[RandInt(0, Items.Length)];
            var qty = RandInt(1, 3);
            var total = price * qty;
            var bills = new[] { 20, 50, 100, 200, 500 }.Where(b => b > total).ToArray();
            var paid = bills[Mathf.Min(RandInt(0, 2), bills.Length - 1)];
            _change = paid - total;
            _given.Clear();
            _patience = 0;
            _order.text = $"Customer {_customer}: {qty} × {item} = ₱{total}. Pays with ₱{paid}.";
        }

        public void AddCoin(int value)
        {
            if (Running) _given.Add(value);
        }

        public void Undo()
        {
            if (Running && _given.Count > 0) _given.RemoveAt(_given.Count - 1);
        }

        public void Give()
        {
            if (!Running) return;
            if (Given == _change) { _correct++; Say("Exact change. Salamat!", UIKit.Good); }
            else { _wrong++; Say($"Wrong! The change was ₱{_change}.", UIKit.Danger); }
            NextCustomer();
        }

        protected override void Tick(float dt)
        {
            _patience += dt;
            var limit = 14f * Ease;
            UIKit.SetBar(_patienceBar, 1f - _patience / limit);
            if (_patience >= limit)
            {
                _wrong++;
                Say("The customer got tired of waiting.", UIKit.Danger);
                NextCustomer();
            }
            if (_total != null) _total.text = $"Change: ₱{Given}";
            Status = $"Customer {_customer}/{Customers}  ·  Correct {_correct}  ·  Mistakes {_wrong}";
        }
    }

    /// <summary>Cooking for the construction crew: remember and repeat recipe steps.</summary>
    public class CookSequenceGame : Minigame
    {
        public override string Title => "Cook for the Crew";
        public override string Instructions =>
            "Memorize each recipe's steps, then click them in the same order. Recipes get longer.\nOne wrong step ruins the dish.";
        public override float TimeLimit => 60f;

        private static readonly string[] Steps = { "Wash rice", "Boil water", "Garlic", "Onion", "Pork", "Soy sauce", "Vinegar", "Simmer" };
        private static readonly (string name, int length)[] Dishes = { ("Rice", 3), ("Adobo", 4), ("Sinigang", 5), ("Menudo", 6) };
        private readonly List<int> _sequence = new List<int>();
        private int _dish = -1, _pos, _correctSteps;
        private float _showTimer;
        private Text _recipe;

        public IReadOnlyList<int> Sequence => _sequence;
        public int Position => _pos;
        public bool Showing => _showTimer > 0;
        public static int TotalSteps => Dishes.Sum(d => d.length);
        public override float Score => (float)_correctSteps / TotalSteps;

        protected override void Build()
        {
            var card = UIKit.Rect("Recipe", Area, new Vector2(0, 170), new Vector2(1100, 130));
            UIKit.Img(card, UIKit.PanelLight, true);
            _recipe = UIKit.Txt(UIKit.Fill("T", card, 12), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            for (var i = 0; i < Steps.Length; i++)
            {
                var idx = i;
                UIKit.Btn("Step" + i, Area, new Vector2(-450 + (i % 4) * 300, i < 4 ? -20 : -130), new Vector2(270, 90), Steps[i],
                    () => Press(idx), null, null, 26);
            }
            MakeFeedback(new Vector2(0, -230));
        }

        protected override void OnBegin() => NextDish();

        private void NextDish()
        {
            _dish++;
            if (_dish >= Dishes.Length) { End(); return; }
            _sequence.Clear();
            for (var i = 0; i < Dishes[_dish].length; i++) _sequence.Add(RandInt(0, Steps.Length));
            _pos = 0;
            _showTimer = 0.9f * Dishes[_dish].length * Ease;
            _recipe.text = $"{Dishes[_dish].name}:  " + string.Join("  →  ", _sequence.Select(s => Steps[s]));
        }

        public void Press(int step)
        {
            if (!Running || Showing || _dish >= Dishes.Length) return;
            if (step == _sequence[_pos])
            {
                _correctSteps++;
                _pos++;
                if (_pos >= _sequence.Count)
                {
                    Say($"{Dishes[_dish].name} served!", UIKit.Good);
                    NextDish();
                }
                else Say($"Step {_pos}/{_sequence.Count}", UIKit.Cream);
            }
            else
            {
                Say($"Wrong step — the {Dishes[_dish].name} is ruined.", UIKit.Danger);
                NextDish();
            }
        }

        protected override void Tick(float dt)
        {
            if (_showTimer > 0)
            {
                _showTimer -= dt;
                if (_showTimer <= 0 && _dish < Dishes.Length) _recipe.text = $"{Dishes[_dish].name}: now cook it from memory!";
            }
            Status = $"Dish {Mathf.Min(_dish + 1, Dishes.Length)}/{Dishes.Length}  ·  Steps right {_correctSteps}/{TotalSteps}";
        }
    }

    /// <summary>Cleaning fish: quick directional knife cues.</summary>
    public class GutFishGame : Minigame
    {
        public override string Title => "Clean the Fish";
        public override string Instructions =>
            "Follow the knife cues: press the arrow shown (arrow keys, WASD, or click the arrow) before the cue runs out. 24 cuts.";
        public override float TimeLimit => 45f;
        public const int Cuts = 24;

        private static readonly string[] Glyphs = { "▲", "▼", "◄", "►" };
        private static readonly string[] Words = { "Scale up", "Scale down", "Cut left", "Cut right" };
        private int _cue = -1, _count, _hits, _misses;
        private float _t;
        private Text _cueText;
        private RectTransform _timer;

        public int CurrentCue => _cue;
        public override float Score => (float)_hits / Cuts;

        protected override void Build()
        {
            var fish = UIKit.Rect("Fish", Area, new Vector2(0, 120), new Vector2(380, 150));
            UIKit.CircleImg(fish, new Color(0.55f, 0.65f, 0.7f));
            _cueText = UIKit.Txt(UIKit.Fill("Cue", fish), "", 80, UIKit.Dark, TextAnchor.MiddleCenter, FontStyle.Bold);
            _timer = UIKit.Bar("Timer", Area, new Vector2(0, 25), new Vector2(380, 12), UIKit.Gold, out _);
            var pos = new[] { new Vector2(0, -60), new Vector2(0, -175), new Vector2(-160, -175), new Vector2(160, -175) };
            for (var i = 0; i < 4; i++)
            {
                var d = i;
                UIKit.Btn("Arrow" + i, Area, pos[i], new Vector2(140, 100), Glyphs[i], () => Press(d), null, null, 40);
            }
            MakeFeedback(new Vector2(380, 120));
        }

        protected override void OnBegin() => NextCue();

        private void NextCue()
        {
            if (_count >= Cuts) { End(); return; }
            _count++;
            _cue = RandInt(0, 4);
            _t = 0;
            _cueText.text = Glyphs[_cue];
        }

        public void Press(int dir)
        {
            if (!Running || _cue < 0) return;
            if (dir == _cue) { _hits++; Say("Clean cut!", UIKit.Good); }
            else { _misses++; Say($"Wrong — {Words[_cue]}.", UIKit.Danger); }
            NextCue();
        }

        protected override void Tick(float dt)
        {
            if (PressedAny(Key.UpArrow, Key.W)) Press(0);
            else if (PressedAny(Key.DownArrow, Key.S)) Press(1);
            else if (PressedAny(Key.LeftArrow, Key.A)) Press(2);
            else if (PressedAny(Key.RightArrow, Key.D)) Press(3);
            if (!Running) return;
            _t += dt;
            var limit = 1.25f * Ease;
            UIKit.SetBar(_timer, 1f - _t / limit);
            if (_t >= limit)
            {
                _misses++;
                Say("Too slow!", UIKit.Danger);
                NextCue();
            }
            Status = $"Cut {_count}/{Cuts}  ·  Clean {_hits}  ·  Botched {_misses}";
        }
    }

    /// <summary>Port canteen: serve the dish each waiting worker wants before they walk off.</summary>
    public class ServeLineGame : Minigame
    {
        public override string Title => "Serve the Line";
        public override string Instructions =>
            "Dockworkers line up for lunch. Click the dish a worker wants (or press 1–4); it goes to whoever has waited longest for it.\nDon't let them walk off hungry.";
        public override float TimeLimit => 35f;

        private static readonly string[] Dishes = { "Rice", "Fried fish", "Adobo", "Sinigang" };
        private class Customer { public int want; public float waited; public float refill = -1; }
        private readonly Customer[] _slots = { new Customer(), new Customer(), new Customer() };
        private readonly Text[] _slotText = new Text[3];
        private readonly RectTransform[] _slotBars = new RectTransform[3];
        private int _served, _walked, _mistakes;

        public int WantAt(int slot) => _slots[slot].refill >= 0 ? -1 : _slots[slot].want;
        public override float Score => Clamp01(_served / 18f - _walked * 0.03f - _mistakes * 0.02f);

        protected override void Build()
        {
            for (var i = 0; i < 3; i++)
            {
                var slot = UIKit.Rect("Worker" + i, Area, new Vector2(-380 + i * 380, 130), new Vector2(330, 170));
                UIKit.Img(slot, UIKit.PanelLight, true);
                _slotText[i] = UIKit.Txt(UIKit.Fill("T", slot, 10), "", 28, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
                _slotBars[i] = UIKit.Bar("Patience", slot, new Vector2(0, -65), new Vector2(280, 10), UIKit.Danger, out _);
            }
            for (var i = 0; i < Dishes.Length; i++)
            {
                var d = i;
                UIKit.Btn("Dish" + i, Area, new Vector2(-450 + i * 300, -110), new Vector2(270, 100), $"{i + 1} · {Dishes[i]}", () => Serve(d),
                    UIKit.Gold, UIKit.Dark, 26);
            }
            MakeFeedback(new Vector2(0, -230));
        }

        protected override void OnBegin()
        {
            for (var i = 0; i < 3; i++) NewCustomer(i);
        }

        private void NewCustomer(int i)
        {
            _slots[i].want = RandInt(0, Dishes.Length);
            _slots[i].waited = 0;
            _slots[i].refill = -1;
        }

        public void Serve(int dish)
        {
            if (!Running) return;
            var best = -1;
            for (var i = 0; i < 3; i++)
                if (_slots[i].refill < 0 && _slots[i].want == dish && (best < 0 || _slots[i].waited > _slots[best].waited)) best = i;
            if (best < 0)
            {
                _mistakes++;
                Say($"Nobody asked for {Dishes[dish]}.", UIKit.Danger);
                return;
            }
            _served++;
            _slots[best].refill = 0.5f;
            Say("Served!", UIKit.Good);
        }

        protected override void Tick(float dt)
        {
            var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            for (var k = 0; k < 4; k++) if (Pressed(keys[k])) Serve(k);
            var limit = 7f * Ease;
            for (var i = 0; i < 3; i++)
            {
                var c = _slots[i];
                if (c.refill >= 0)
                {
                    c.refill -= dt;
                    _slotText[i].text = "...";
                    UIKit.SetBar(_slotBars[i], 0);
                    if (c.refill < 0) NewCustomer(i);
                    continue;
                }
                c.waited += dt;
                _slotText[i].text = $"Wants:\n{Dishes[c.want]}";
                UIKit.SetBar(_slotBars[i], 1f - c.waited / limit);
                if (c.waited >= limit)
                {
                    _walked++;
                    Say("A worker walked off hungry.", UIKit.Danger);
                    c.refill = 0.5f;
                }
            }
            Status = $"Served {_served}  ·  Walked off {_walked}  ·  Mix-ups {_mistakes}";
        }
    }

    /// <summary>Food kiosk: take each siopao out of its steamer at the right moment.</summary>
    public class SteamerGame : Minigame
    {
        public override string Title => "Siopao Steamers";
        public override string Instructions =>
            "Take each siopao out when its steam bar is in the green zone. Too early it's raw; too late it burns.\nClick a steamer or press 1–4.";
        public override float TimeLimit => 35f;

        private readonly float[] _p = new float[4];
        private readonly float[] _rate = new float[4];
        private readonly float[] _cool = new float[4];
        private readonly RectTransform[] _fills = new RectTransform[4];
        private readonly RectTransform[] _zones = new RectTransform[4];
        private int _perfect, _raw, _burnt;
        private const float ZoneStart = 0.72f;
        private const float BarH = 250f;
        private float ZoneEnd => Mathf.Min(0.98f, ZoneStart + 0.18f * Ease);

        public float Progress(int i) => _cool[i] > 0 ? -1 : _p[i];
        public bool InZone(int i) => _cool[i] <= 0 && _p[i] >= ZoneStart && _p[i] <= ZoneEnd;
        public override float Score => Clamp01(_perfect / 14f - (_raw + _burnt) * 0.03f);

        protected override void Build()
        {
            for (var i = 0; i < 4; i++)
            {
                var idx = i;
                var x = -390 + i * 260;
                var bar = UIKit.Rect("Steam" + i, Area, new Vector2(x, 60), new Vector2(90, BarH));
                UIKit.Img(bar, new Color(0.15f, 0.16f, 0.19f), true);
                _zones[i] = UIKit.Rect("Zone", bar, Vector2.zero, new Vector2(90, 10), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                UIKit.Img(_zones[i], new Color(0.3f, 0.7f, 0.3f, 0.6f));
                _fills[i] = UIKit.Rect("Fill", bar, Vector2.zero, new Vector2(60, 0), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                UIKit.Img(_fills[i], new Color(0.95f, 0.92f, 0.85f), true);
                UIKit.Btn("Take" + i, Area, new Vector2(x, -150), new Vector2(200, 80), $"{i + 1} · Take out", () => Take(idx), UIKit.Gold, UIKit.Dark, 22);
            }
            MakeFeedback(new Vector2(0, -240));
        }

        protected override void OnBegin()
        {
            for (var i = 0; i < 4; i++) ResetSteamer(i, Rand(0f, 1.2f));
        }

        private void ResetSteamer(int i, float cooldown)
        {
            _p[i] = 0;
            _rate[i] = Rand(0.16f, 0.28f);
            _cool[i] = cooldown;
        }

        public void Take(int i)
        {
            if (!Running || _cool[i] > 0) return;
            if (InZone(i)) { _perfect++; Say("Perfect siopao!", UIKit.Good); }
            else { _raw++; Say("Raw in the middle!", UIKit.Danger); }
            ResetSteamer(i, 0.6f);
        }

        protected override void Tick(float dt)
        {
            var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            for (var k = 0; k < 4; k++) if (Pressed(keys[k])) Take(k);
            for (var i = 0; i < 4; i++)
            {
                _zones[i].anchoredPosition = new Vector2(0, BarH * ZoneStart);
                _zones[i].sizeDelta = new Vector2(90, BarH * (ZoneEnd - ZoneStart));
                if (_cool[i] > 0)
                {
                    _cool[i] -= dt;
                    _fills[i].sizeDelta = new Vector2(60, 0);
                    continue;
                }
                _p[i] += _rate[i] * dt;
                if (_p[i] >= 1f)
                {
                    _burnt++;
                    Say("Burnt!", UIKit.Danger);
                    ResetSteamer(i, 0.6f);
                    continue;
                }
                _fills[i].sizeDelta = new Vector2(60, BarH * _p[i]);
            }
            Status = $"Perfect {_perfect}  ·  Raw {_raw}  ·  Burnt {_burnt}";
        }
    }

    /// <summary>Creates minigames by id. Every job's minigameId must be listed here.</summary>
    public static class MinigameFactory
    {
        public static readonly string[] All =
        {
            "sort_recyclables", "haul_balance", "catch_cases", "hollow_blocks", "mix_cement", "fish_relay", "crane_drop",
            "parcel_route", "sweep_court", "hand_flyers", "scrub_laundry", "weigh_veg", "bundle_match", "make_change",
            "cook_sequence", "gut_fish", "serve_line", "siopao_steam"
        };

        public static Minigame Create(string id, GameObject host) => id switch
        {
            "sort_recyclables" => host.AddComponent<SortRecyclablesGame>(),
            "haul_balance" => host.AddComponent<HaulBalanceGame>(),
            "catch_cases" => host.AddComponent<CatchCasesGame>(),
            "hollow_blocks" => host.AddComponent<HollowBlocksGame>(),
            "mix_cement" => host.AddComponent<MixCementGame>(),
            "fish_relay" => host.AddComponent<FishRelayGame>(),
            "crane_drop" => host.AddComponent<CraneDropGame>(),
            "parcel_route" => host.AddComponent<ParcelRouteGame>(),
            "sweep_court" => host.AddComponent<SweepCourtGame>(),
            "hand_flyers" => host.AddComponent<FlyersGame>(),
            "scrub_laundry" => host.AddComponent<LaundryGame>(),
            "weigh_veg" => host.AddComponent<WeighVegGame>(),
            "bundle_match" => host.AddComponent<MemoryMatchGame>(),
            "make_change" => host.AddComponent<MakeChangeGame>(),
            "cook_sequence" => host.AddComponent<CookSequenceGame>(),
            "gut_fish" => host.AddComponent<GutFishGame>(),
            "serve_line" => host.AddComponent<ServeLineGame>(),
            "siopao_steam" => host.AddComponent<SteamerGame>(),
            _ => null
        };
    }
}
