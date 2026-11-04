using System.Collections.Generic;
using Pagpag.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.Minigames
{
    // Heavy-labor jobs. Play area is about 1240 x 560, origin at its centre.

    /// <summary>Collect recyclables: sort each item into the right sack before it's gone.</summary>
    public class SortRecyclablesGame : Minigame
    {
        public override string Title => "Sort the Kalakal";
        public override string Instructions =>
            "Sort each item into the right sack before it's gone: Plastic, Metal or Paper — or throw out the trash.\nClick a sack or press 1–4.";
        public override float TimeLimit => 45f;
        public const int Total = 18;

        private static readonly string[] Bins = { "Plastic", "Metal", "Paper", "Trash" };
        private static readonly (string name, int bin)[] Items =
        {
            ("PET bottle", 0), ("Shampoo bottle", 0), ("Plastic cup", 0), ("Ice cream tub", 0),
            ("Tin can", 1), ("Softdrink can", 1), ("Copper wire", 1), ("Bottle cap", 1),
            ("Cardboard box", 2), ("Newspaper", 2), ("Egg tray", 2), ("Old notebook", 2),
            ("Rotten food", 3), ("Used diaper", 3), ("Broken glass", 3), ("Wet tissue", 3)
        };

        private int _index, _correct, _wrong;
        private float _itemTime, _itemLimit;
        private (string name, int bin) _current;
        private Text _itemText;
        private RectTransform _timer;

        public int CurrentBin => _current.bin;
        public override float Score => Clamp01((float)_correct / Total);

        protected override void Build()
        {
            var card = UIKit.Rect("Item", Area, new Vector2(0, 110), new Vector2(460, 150));
            UIKit.Img(card, UIKit.PanelLight, true);
            _itemText = UIKit.Txt(UIKit.Fill("Text", card, 10), "", 40, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            _timer = UIKit.Bar("Timer", Area, new Vector2(0, 20), new Vector2(460, 14), UIKit.Gold, out _);
            var colors = new[] { new Color(0.2f, 0.45f, 0.7f), new Color(0.45f, 0.48f, 0.52f), new Color(0.6f, 0.45f, 0.25f), new Color(0.35f, 0.3f, 0.3f) };
            for (var i = 0; i < 4; i++)
            {
                var bin = i;
                UIKit.Btn("Bin" + i, Area, new Vector2(-405 + i * 270, -140), new Vector2(250, 120), $"{i + 1}\n{Bins[i]}",
                    () => Choose(bin), colors[i], UIKit.Cream, 30);
            }
            MakeFeedback(new Vector2(0, -240));
        }

        protected override void OnBegin() => Next();

        private void Next()
        {
            if (_index >= Total) { End(); return; }
            _current = Items[RandInt(0, Items.Length)];
            _index++;
            _itemTime = 0;
            _itemLimit = 2.8f * Ease;
            _itemText.text = _current.name;
        }

        public void Choose(int bin)
        {
            if (!Running) return;
            if (bin == _current.bin) { _correct++; Say("Correct!", UIKit.Good); }
            else { _wrong++; Say($"Wrong — that goes in {Bins[_current.bin]}.", UIKit.Danger); }
            Next();
        }

        protected override void Tick(float dt)
        {
            if (PressedAny(Key.Digit1, Key.Numpad1)) Choose(0);
            else if (PressedAny(Key.Digit2, Key.Numpad2)) Choose(1);
            else if (PressedAny(Key.Digit3, Key.Numpad3)) Choose(2);
            else if (PressedAny(Key.Digit4, Key.Numpad4)) Choose(3);
            if (!Running) return;
            _itemTime += dt;
            UIKit.SetBar(_timer, 1f - _itemTime / _itemLimit);
            if (_itemTime >= _itemLimit)
            {
                _wrong++;
                Say("Too slow!", UIKit.Danger);
                Next();
            }
            Status = $"Item {_index}/{Total}  ·  Correct {_correct}  ·  Wrong {_wrong}";
        }
    }

    /// <summary>Haul crates: keep a wobbling stack balanced while walking it to the stalls.</summary>
    public class HaulBalanceGame : Minigame
    {
        public override string Title => "Balance the Crates";
        public override string Instructions =>
            "Carry the stack of crates to the stalls without dropping it.\nHold ◄ / ► (A/D or arrow keys) to lean against the wobble. Keep the marker in the green.";
        public override float TimeLimit => 32f;

        private float _tilt, _vel, _progress;
        private int _drops;
        private RectTransform _stack, _marker, _progressFill;
        private HoldButton _left, _right;

        public float Tilt => _tilt;
        public float Progress => _progress;
        public override float Score => Clamp01(_progress) * Mathf.Max(0f, 1f - 0.12f * _drops);

        protected override void Build()
        {
            UIKit.Label("PathLabel", Area, new Vector2(0, 245), new Vector2(900, 30), "Distance to the stalls", 20, UIKit.Muted);
            _progressFill = UIKit.Bar("Path", Area, new Vector2(0, 215), new Vector2(900, 24), UIKit.Good, out _);
            UIKit.SetBar(_progressFill, 0);

            _stack = UIKit.Rect("Stack", Area, new Vector2(0, -150), new Vector2(150, 270), null, new Vector2(0.5f, 0f));
            for (var i = 0; i < 3; i++)
            {
                var crate = UIKit.Rect("Crate" + i, _stack, new Vector2(0, 45 + i * 90), new Vector2(150 - i * 10, 82));
                crate.anchorMin = crate.anchorMax = new Vector2(0.5f, 0f);
                UIKit.Img(crate, new Color(0.6f - i * 0.05f, 0.42f, 0.22f), true);
                UIKit.Txt(UIKit.Fill("T", crate), i == 2 ? "TOMATOES" : i == 1 ? "CABBAGE" : "ONIONS", 16, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            var meter = UIKit.Rect("Meter", Area, new Vector2(0, -215), new Vector2(620, 20));
            UIKit.Img(meter, new Color(0.5f, 0.15f, 0.15f), true);
            var zone = UIKit.Rect("Safe", meter, Vector2.zero, new Vector2(620 * 0.35f, 20));
            UIKit.Img(zone, UIKit.Good, true);
            _marker = UIKit.Rect("Marker", meter, Vector2.zero, new Vector2(10, 40));
            UIKit.Img(_marker, Color.white);

            _left = UIKit.Btn("Left", Area, new Vector2(-450, -60), new Vector2(230, 120), "◄ Lean", null, null, null, 30).gameObject.AddComponent<HoldButton>();
            _right = UIKit.Btn("Right", Area, new Vector2(450, -60), new Vector2(230, 120), "Lean ►", null, null, null, 30).gameObject.AddComponent<HoldButton>();
            MakeFeedback(new Vector2(0, -252));
        }

        protected override void Tick(float dt)
        {
            var left = _left.Held || HeldAny(Key.A, Key.LeftArrow);
            var right = _right.Held || HeldAny(Key.D, Key.RightArrow);
            Simulate(dt, left, right);
        }

        /// <summary>One physics step with the given input (public for tests).</summary>
        public void Simulate(float dt, bool left, bool right)
        {
            _vel += (Rand(-1f, 1f) * 2.4f + _tilt * 1.5f) / Ease * dt;
            if (left) _vel -= 3.2f * dt;
            if (right) _vel += 3.2f * dt;
            _vel *= 1f - 0.9f * dt;
            _tilt += _vel * dt;

            var abs = Mathf.Abs(_tilt);
            if (abs < 0.35f) _progress += dt / 22f;
            else if (abs < 1f) _progress += dt / 60f;
            if (abs >= 1f)
            {
                _drops++;
                _tilt = 0;
                _vel = 0;
                _progress = Mathf.Max(0, _progress - 0.06f);
                Say("Crash! The crates hit the ground.", UIKit.Danger);
            }

            _stack.localRotation = Quaternion.Euler(0, 0, -_tilt * 35f);
            _marker.anchoredPosition = new Vector2(Mathf.Clamp(_tilt, -1, 1) * 300f, 0);
            UIKit.SetBar(_progressFill, _progress);
            Status = $"Delivered {Mathf.RoundToInt(Clamp01(_progress) * 100)}%  ·  Drops {_drops}";
            if (_progress >= 1f) End();
        }
    }

    /// <summary>Unload deliveries: catch falling cases, dodge rotten sacks.</summary>
    public class CatchCasesGame : Minigame
    {
        public override string Title => "Catch the Cases";
        public override string Instructions =>
            "Cases slide off the delivery truck. Move left and right (◄ ► buttons, A/D or arrow keys) to catch the softdrink cases.\nDodge the rotten sacks.";
        public override float TimeLimit => 30f;

        private static readonly float[] Lanes = { -320f, 0f, 320f };
        private readonly List<(RectTransform rt, int lane, bool good)> _falling = new List<(RectTransform, int, bool)>();
        private int _lane = 1, _caught, _missed, _bad, _goodSpawned;
        private float _spawnTimer;
        private RectTransform _catcher;

        public int Lane => _lane;
        public IReadOnlyList<(RectTransform rt, int lane, bool good)> Falling => _falling;
        public override float Score => _goodSpawned == 0 ? 0 : Clamp01((float)_caught / Mathf.Max(18, _goodSpawned) - _bad * 0.05f);

        protected override void Build()
        {
            for (var i = 0; i < 3; i++)
            {
                var lane = UIKit.Rect("Lane" + i, Area, new Vector2(Lanes[i], 0), new Vector2(240, 540));
                UIKit.Img(lane, new Color(1, 1, 1, 0.04f), true);
            }
            _catcher = UIKit.Rect("Catcher", Area, new Vector2(0, -215), new Vector2(230, 40));
            UIKit.Img(_catcher, UIKit.Gold, true);
            UIKit.Txt(UIKit.Fill("T", _catcher), "YOU", 18, UIKit.Dark, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Btn("Left", Area, new Vector2(-540, -200), new Vector2(120, 100), "◄", MoveLeft, null, null, 40);
            UIKit.Btn("Right", Area, new Vector2(540, -200), new Vector2(120, 100), "►", MoveRight, null, null, 40);
            MakeFeedback(new Vector2(0, 240));
        }

        public void MoveLeft() { if (Running) _lane = Mathf.Max(0, _lane - 1); }
        public void MoveRight() { if (Running) _lane = Mathf.Min(2, _lane + 1); }
        public void SetLane(int lane) { if (Running) _lane = Mathf.Clamp(lane, 0, 2); }

        protected override void Tick(float dt)
        {
            if (PressedAny(Key.A, Key.LeftArrow)) MoveLeft();
            if (PressedAny(Key.D, Key.RightArrow)) MoveRight();
            _catcher.anchoredPosition = new Vector2(Lanes[_lane], -215);

            _spawnTimer -= dt;
            if (_spawnTimer <= 0)
            {
                _spawnTimer = 0.95f;
                var good = Rng.NextDouble() < 0.8;
                var lane = RandInt(0, 3);
                var rt = UIKit.Rect(good ? "Case" : "Sack", Area, new Vector2(Lanes[lane], 250), new Vector2(120, 70));
                UIKit.Img(rt, good ? new Color(0.75f, 0.2f, 0.2f) : new Color(0.38f, 0.3f, 0.2f), true);
                UIKit.Txt(UIKit.Fill("T", rt), good ? "CASE" : "ROTTEN", 16, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
                _falling.Add((rt, lane, good));
                if (good) _goodSpawned++;
            }

            var speed = 240f / Ease;
            for (var i = _falling.Count - 1; i >= 0; i--)
            {
                var f = _falling[i];
                var p = f.rt.anchoredPosition;
                p.y -= speed * dt;
                f.rt.anchoredPosition = p;
                if (p.y > -190f) continue;
                if (f.lane == _lane)
                {
                    if (f.good) { _caught++; Say("Caught!", UIKit.Good); }
                    else { _bad++; Say("Ugh — a rotten sack!", UIKit.Danger); }
                }
                else if (f.good) { _missed++; Say("Dropped one!", UIKit.Danger); }
                Remove(f.rt.gameObject);
                _falling.RemoveAt(i);
            }
            Status = $"Caught {_caught}  ·  Missed {_missed}  ·  Rotten {_bad}";
        }
    }

    /// <summary>Construction morning shift: timing steps up the scaffolding.</summary>
    public class HollowBlocksGame : Minigame
    {
        public override string Title => "Up the Scaffolding";
        public override string Instructions =>
            "Carry hollow blocks up the scaffolding. Press Space (or click STEP) when the moving marker is inside the green zone.\nMiss and you lose your footing.";
        public override float TimeLimit => 32f;
        public const int Target = 20;

        private int _steps, _misses;
        private float _phase, _zoneX, _zoneW;
        private RectTransform _marker, _zone, _climb;

        public bool MarkerInZone => Mathf.Abs(_marker.anchoredPosition.x - _zoneX) <= _zoneW / 2f;
        public override float Score => Clamp01((float)_steps / Target - _misses * 0.03f);

        protected override void Build()
        {
            var bar = UIKit.Rect("Bar", Area, new Vector2(0, -100), new Vector2(900, 46));
            UIKit.Img(bar, new Color(0.15f, 0.16f, 0.19f), true);
            _zone = UIKit.Rect("Zone", bar, Vector2.zero, new Vector2(150, 46));
            UIKit.Img(_zone, UIKit.Good, true);
            _marker = UIKit.Rect("Marker", bar, Vector2.zero, new Vector2(12, 70));
            UIKit.Img(_marker, Color.white);

            var tower = UIKit.Rect("Scaffold", Area, new Vector2(470, 100), new Vector2(60, 300));
            UIKit.Img(tower, new Color(0.3f, 0.32f, 0.35f), true);
            _climb = UIKit.Rect("Climb", tower, Vector2.zero, new Vector2(60, 0), null, new Vector2(0.5f, 0f));
            _climb.anchorMin = _climb.anchorMax = new Vector2(0.5f, 0f);
            UIKit.Img(_climb, UIKit.Gold, true);
            UIKit.Label("Top", tower, new Vector2(0, 170), new Vector2(200, 30), "4th floor", 18, UIKit.Muted);

            UIKit.Btn("StepButton", Area, new Vector2(0, -210), new Vector2(260, 80), "STEP", TakeStep, UIKit.Gold, UIKit.Dark, 32);
            MakeFeedback(new Vector2(-150, 120));
        }

        protected override void OnBegin() => NewZone();

        private void NewZone()
        {
            _zoneW = Mathf.Clamp(150f * Ease, 60f, 260f) * (1f - 0.015f * _steps);
            _zoneX = Rand(-320f, 320f);
            _zone.anchoredPosition = new Vector2(_zoneX, 0);
            _zone.sizeDelta = new Vector2(_zoneW, 46);
        }

        public void TakeStep()
        {
            if (!Running) return;
            if (MarkerInZone)
            {
                _steps++;
                Say(_steps % 5 == 0 ? "Block delivered!" : "Up!", UIKit.Good);
                if (_steps >= Target) { End(); return; }
                NewZone();
            }
            else
            {
                _misses++;
                Say("You slip!", UIKit.Danger);
            }
        }

        protected override void Tick(float dt)
        {
            _phase += dt * (1.1f / Ease) * (1f + 0.02f * _steps);
            _marker.anchoredPosition = new Vector2(Mathf.Sin(_phase * Mathf.PI) * 430f, 0);
            if (Pressed(Key.Space)) TakeStep();
            _climb.sizeDelta = new Vector2(60, 300f * _steps / Target);
            Status = $"Steps {_steps}/{Target}  ·  Blocks {_steps / 5}  ·  Slips {_misses}";
        }
    }

    /// <summary>Construction afternoon shift: pour ingredients to the right ratio.</summary>
    public class MixCementGame : Minigame
    {
        public override string Title => "Mix the Cement";
        public override string Instructions =>
            "Fill each ingredient up to its line, then press MIX. Hold an ingredient button (or keys 1–4) to pour.\nYou can't take any out: if you overpour, DUMP the batch. 3 batches.";
        public override float TimeLimit => 50f;
        public const int Batches = 3;

        private static readonly string[] Names = { "Cement", "Sand", "Gravel", "Water" };
        private readonly float[] _amount = new float[4];
        private readonly float[] _target = new float[4];
        private readonly RectTransform[] _fills = new RectTransform[4];
        private readonly RectTransform[] _lines = new RectTransform[4];
        private readonly HoldButton[] _pour = new HoldButton[4];
        private readonly List<float> _results = new List<float>();
        private const float BarHeight = 280f;

        public float[] Amounts => _amount;
        public float[] Targets => _target;
        public override float Score
        {
            get
            {
                var sum = 0f;
                foreach (var r in _results) sum += r;
                return sum / Batches;
            }
        }

        protected override void Build()
        {
            var colors = new[] { new Color(0.6f, 0.6f, 0.62f), new Color(0.8f, 0.68f, 0.45f), new Color(0.45f, 0.42f, 0.4f), new Color(0.3f, 0.55f, 0.85f) };
            for (var i = 0; i < 4; i++)
            {
                var x = -360 + i * 240;
                var bar = UIKit.Rect(Names[i] + "Bar", Area, new Vector2(x, 70), new Vector2(110, BarHeight));
                UIKit.Img(bar, new Color(0.15f, 0.16f, 0.19f), true);
                _fills[i] = UIKit.Rect("Fill", bar, Vector2.zero, new Vector2(110, 0), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                UIKit.Img(_fills[i], colors[i], true);
                _lines[i] = UIKit.Rect("Line", bar, Vector2.zero, new Vector2(150, 5), new Vector2(0.5f, 0f));
                UIKit.Img(_lines[i], Color.white);
                var idx = i;
                var b = UIKit.Btn("Pour" + i, Area, new Vector2(x, -135), new Vector2(200, 64), $"{i + 1} · {Names[i]}", null, null, null, 22);
                _pour[idx] = b.gameObject.AddComponent<HoldButton>();
            }
            UIKit.Btn("Mix", Area, new Vector2(-120, -225), new Vector2(220, 56), "MIX", Mix, UIKit.Gold, UIKit.Dark, 26);
            UIKit.Btn("Dump", Area, new Vector2(140, -225), new Vector2(220, 56), "Dump batch", Dump, null, null, 22);
            MakeFeedback(new Vector2(0, 245));
        }

        protected override void OnBegin() => NewBatch();

        private void NewBatch()
        {
            _target[0] = Rand(15, 30);
            _target[1] = Rand(30, 55);
            _target[2] = Rand(40, 75);
            _target[3] = Rand(10, 25);
            for (var i = 0; i < 4; i++)
            {
                _amount[i] = 0;
                _lines[i].anchoredPosition = new Vector2(0, BarHeight * _target[i] / 100f);
            }
        }

        public void Pour(int i, float dt)
        {
            if (!Running) return;
            _amount[i] = Mathf.Min(100f, _amount[i] + 35f / Ease * dt);
        }

        public void Mix()
        {
            if (!Running) return;
            float err = 0, total = 0;
            for (var i = 0; i < 4; i++)
            {
                err += Mathf.Abs(_amount[i] - _target[i]);
                total += _target[i];
            }
            var acc = Clamp01(1f - err / total * 2f);
            _results.Add(acc);
            Say($"Batch {_results.Count}: {Mathf.RoundToInt(acc * 100)}% right", acc > 0.7f ? UIKit.Good : UIKit.Danger);
            if (_results.Count >= Batches) End();
            else NewBatch();
        }

        public void Dump()
        {
            if (!Running) return;
            for (var i = 0; i < 4; i++) _amount[i] = 0;
            Say("Batch dumped. Start again.", UIKit.Muted);
        }

        protected override void Tick(float dt)
        {
            var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            for (var i = 0; i < 4; i++)
            {
                if (_pour[i].Held || Held(keys[i])) Pour(i, dt);
                _fills[i].sizeDelta = new Vector2(110, BarHeight * _amount[i] / 100f);
            }
            Status = $"Batch {Mathf.Min(_results.Count + 1, Batches)}/{Batches}";
        }
    }

    /// <summary>Fish porter: deliver tubs to the buyer who is calling (reaction).</summary>
    public class FishRelayGame : Minigame
    {
        public override string Title => "Fish Tub Relay";
        public override string Instructions =>
            "Buyers shout for their tubs of fish. Click the buyer who is calling (or press 1–6) before they give up.\nDon't deliver to a stall that didn't order.";
        public override float TimeLimit => 30f;

        private static readonly string[] Buyers = { "Suki Ana", "Mang Tonyo", "Aling Doray", "Kuya Bert", "Ate Liza", "Mang Celso" };
        private readonly float[] _calling = { -1, -1, -1, -1, -1, -1 };
        private readonly Image[] _bg = new Image[6];
        private readonly RectTransform[] _timers = new RectTransform[6];
        private int _delivered, _missed, _wrong;
        private float _spawn;

        public bool IsCalling(int i) => _calling[i] >= 0;
        public override float Score => Clamp01(_delivered / 20f - _wrong * 0.03f);

        protected override void Build()
        {
            for (var i = 0; i < 6; i++)
            {
                var idx = i;
                var pos = new Vector2(-380 + (i % 3) * 380, i < 3 ? 100 : -110);
                var b = UIKit.Btn("Buyer" + i, Area, pos, new Vector2(330, 170), $"{i + 1}\n{Buyers[i]}", () => Deliver(idx), null, null, 28);
                _bg[i] = (Image)b.targetGraphic;
                _timers[i] = UIKit.Bar("Patience", b.transform, new Vector2(0, -70), new Vector2(280, 10), UIKit.Danger, out _);
                _timers[i].parent.gameObject.SetActive(false);
            }
            MakeFeedback(new Vector2(0, 240));
        }

        public void Deliver(int i)
        {
            if (!Running) return;
            if (_calling[i] >= 0)
            {
                _delivered++;
                Say($"Delivered to {Buyers[i]}!", UIKit.Good);
                Reset(i);
            }
            else
            {
                _wrong++;
                Say($"{Buyers[i]} didn't order that!", UIKit.Danger);
            }
        }

        private void Reset(int i)
        {
            _calling[i] = -1;
            _bg[i].color = UIKit.ButtonColor;
            _timers[i].parent.gameObject.SetActive(false);
        }

        protected override void Tick(float dt)
        {
            var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
            for (var k = 0; k < 6; k++)
                if (Pressed(keys[k])) Deliver(k);

            _spawn -= dt;
            if (_spawn <= 0)
            {
                _spawn = 0.85f;
                var idle = new List<int>();
                for (var i = 0; i < 6; i++) if (_calling[i] < 0) idle.Add(i);
                if (idle.Count > 0)
                {
                    var i = idle[RandInt(0, idle.Count)];
                    _calling[i] = 0;
                    _bg[i].color = UIKit.Gold;
                    _timers[i].parent.gameObject.SetActive(true);
                }
            }
            var limit = 1.7f * Ease;
            for (var i = 0; i < 6; i++)
            {
                if (_calling[i] < 0) continue;
                _calling[i] += dt;
                UIKit.SetBar(_timers[i], 1f - _calling[i] / limit);
                if (_calling[i] >= limit)
                {
                    _missed++;
                    Say($"{Buyers[i]} gave up waiting.", UIKit.Danger);
                    Reset(i);
                }
            }
            Status = $"Delivered {_delivered}  ·  Missed {_missed}  ·  Wrong {_wrong}";
        }
    }

    /// <summary>Port cargo loading: drop swinging crates into a moving hold.</summary>
    public class CraneDropGame : Minigame
    {
        public override string Title => "Crane Drop";
        public override string Instructions =>
            "Guide the crane: press Space or click DROP when the crate is above the ship's green hold.\nThe closer to the centre, the better. 8 crates.";
        public override float TimeLimit => 45f;
        public const int Crates = 8;

        private float _t, _fall = -1f;
        private int _done;
        private float _quality;
        private RectTransform _crate, _hold;
        private float _dropX;

        public float CrateX => _crate.anchoredPosition.x;
        public float HoldX => _hold.anchoredPosition.x;
        public bool Dropping => _fall >= 0f;
        public override float Score => _quality / Crates;

        protected override void Build()
        {
            var rail = UIKit.Rect("Rail", Area, new Vector2(0, 235), new Vector2(1100, 8));
            UIKit.Img(rail, UIKit.Muted);
            var hull = UIKit.Rect("Ship", Area, new Vector2(0, -245), new Vector2(1150, 60));
            UIKit.Img(hull, new Color(0.25f, 0.3f, 0.4f), true);
            _hold = UIKit.Rect("Hold", Area, new Vector2(0, -205), new Vector2(200, 40));
            UIKit.Img(_hold, UIKit.Good, true);
            _crate = UIKit.Rect("Crate", Area, new Vector2(0, 190), new Vector2(110, 80));
            UIKit.Img(_crate, new Color(0.7f, 0.45f, 0.2f), true);
            UIKit.Txt(UIKit.Fill("T", _crate), "CARGO", 18, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Btn("DropButton", Area, new Vector2(470, 120), new Vector2(200, 80), "DROP", Drop, UIKit.Gold, UIKit.Dark, 30);
            MakeFeedback(new Vector2(0, 40));
        }

        public void Drop()
        {
            if (!Running || Dropping) return;
            _fall = 0f;
            _dropX = _crate.anchoredPosition.x;
        }

        protected override void Tick(float dt)
        {
            _t += dt;
            _hold.anchoredPosition = new Vector2(Mathf.Sin(_t * 0.55f) * 260f, -205);
            if (!Dropping)
            {
                _crate.anchoredPosition = new Vector2(Mathf.Sin(_t * 1.5f / Ease) * 450f, 190);
                if (Pressed(Key.Space)) Drop();
            }
            else
            {
                _fall += dt / 0.5f;
                _crate.anchoredPosition = new Vector2(_dropX, Mathf.Lerp(190, -175, _fall));
                if (_fall >= 1f) Land();
            }
            Status = $"Crate {Mathf.Min(_done + 1, Crates)}/{Crates}  ·  Quality {Mathf.RoundToInt(_quality / Mathf.Max(1, _done) * 100)}%";
        }

        private void Land()
        {
            var off = Mathf.Abs(_dropX - _hold.anchoredPosition.x);
            float q;
            if (off <= 22f * Ease) { q = 1f; Say("Perfect drop!", UIKit.Good); }
            else if (off <= 60f * Ease) { q = 0.7f; Say("Good.", UIKit.Good); }
            else if (off <= 100f) { q = 0.4f; Say("Off-centre. The foreman frowns.", UIKit.Gold); }
            else { q = 0f; Say("SPLASH! Into the bay.", UIKit.Danger); }
            _quality += q;
            _done++;
            _fall = -1f;
            if (_done >= Crates) End();
        }
    }
}
