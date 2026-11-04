using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pagpag.Minigames;
using Pagpag.UI;
using UnityEngine;

namespace Pagpag.Tests
{
    /// <summary>
    /// Every job minigame can be played to completion: a competent bot scores well,
    /// doing nothing scores almost nothing.
    /// </summary>
    public class MinigameTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _created.Clear();
        }

        private Minigame Make(string id, float ease = 1f)
        {
            var root = new GameObject("TestArea", typeof(RectTransform));
            _created.Add(root);
            var area = (RectTransform)root.transform;
            area.sizeDelta = new Vector2(1240, 560);
            var game = MinigameFactory.Create(id, root);
            Assert.NotNull(game, id);
            game.Setup(area, new MinigameContext { minigameId = id, ease = ease, seed = 1234, wage = 100, payMultiplier = 1 });
            Assert.IsFalse(string.IsNullOrEmpty(game.Title), id);
            Assert.IsFalse(string.IsNullOrEmpty(game.Instructions), id);
            return game;
        }

        private static float Play(Minigame game, Action<Minigame, float> bot)
        {
            float? result = null;
            game.Begin(s => result = s);
            const float dt = 0.05f;
            var t = 0f;
            while (!game.Finished && t < game.TimeLimit + 5f)
            {
                bot?.Invoke(game, dt);
                if (game.Finished) break;
                game.Step(dt);
                t += dt;
            }
            Assert.IsTrue(game.Finished, game.GetType().Name + " never finished");
            Assert.IsTrue(result.HasValue, "Score reported");
            Assert.That(result.Value, Is.InRange(0f, 1f));
            return result.Value;
        }

        private static readonly Dictionary<string, Action<Minigame, float>> Bots = new Dictionary<string, Action<Minigame, float>>
        {
            ["sort_recyclables"] = (g, dt) => { var m = (SortRecyclablesGame)g; m.Choose(m.CurrentBin); },
            ["haul_balance"] = (g, dt) =>
            {
                var m = (HaulBalanceGame)g;
                var holds = m.GetComponentsInChildren<HoldButton>();
                holds[0].SetHeld(m.Tilt > 0.05f);
                holds[1].SetHeld(m.Tilt < -0.05f);
            },
            ["catch_cases"] = (g, dt) =>
            {
                var m = (CatchCasesGame)g;
                var next = m.Falling.Where(f => f.good).OrderBy(f => f.rt.anchoredPosition.y).FirstOrDefault();
                if (next.rt != null) m.SetLane(next.lane);
            },
            ["hollow_blocks"] = (g, dt) => { var m = (HollowBlocksGame)g; if (m.MarkerInZone) m.TakeStep(); },
            ["mix_cement"] = (g, dt) =>
            {
                var m = (MixCementGame)g;
                var done = true;
                for (var i = 0; i < 4; i++)
                    if (m.Amounts[i] < m.Targets[i] - 0.6f) { m.Pour(i, dt); done = false; break; }
                if (done) m.Mix();
            },
            ["fish_relay"] = (g, dt) => { var m = (FishRelayGame)g; for (var i = 0; i < 6; i++) if (m.IsCalling(i)) m.Deliver(i); },
            ["crane_drop"] = (g, dt) => { var m = (CraneDropGame)g; if (!m.Dropping && Mathf.Abs(m.CrateX - m.HoldX) < 12f) m.Drop(); },
            ["parcel_route"] = (g, dt) => { var m = (ParcelRouteGame)g; m.Deliver(m.SlotOfTarget); },
            ["sweep_court"] = (g, dt) => ((SweepCourtGame)g).SweepOldest(),
            ["hand_flyers"] = (g, dt) => ((FlyersGame)g).OfferFirst(false),
            ["scrub_laundry"] = (g, dt) =>
            {
                var m = (LaundryGame)g;
                if (m.Wringing) m.Wring(dt, true);
                else { m.Scrub(-1); m.Scrub(1); }
            },
            ["weigh_veg"] = (g, dt) => { var m = (WeighVegGame)g; m.Add(m.TargetTenths - m.CurrentTenths); m.Sell(); },
            ["bundle_match"] = (g, dt) =>
            {
                var m = (MemoryMatchGame)g;
                if (m.Previewing || m.Busy) return;
                // Flip the first card whose pair hasn't been made, then its partner.
                for (var i = 0; i < 12; i++)
                    for (var j = i + 1; j < 12; j++)
                        if (m.CardValue(i) == m.CardValue(j) && !BotMemory.Contains(i))
                        {
                            BotMemory.Add(i);
                            BotMemory.Add(j);
                            m.Flip(i);
                            m.Flip(j);
                            return;
                        }
            },
            ["make_change"] = (g, dt) =>
            {
                var m = (MakeChangeGame)g;
                if (!m.Running) return;
                foreach (var coin in new[] { 100, 50, 20, 10, 5, 1 })
                    for (var n = 0; n < 20 && m.ChangeDue - m.Given >= coin; n++) m.AddCoin(coin);
                m.Give();
            },
            ["cook_sequence"] = (g, dt) => { var m = (CookSequenceGame)g; if (!m.Showing && m.Running) m.Press(m.Sequence[m.Position]); },
            ["gut_fish"] = (g, dt) => { var m = (GutFishGame)g; m.Press(m.CurrentCue); },
            ["serve_line"] = (g, dt) => { var m = (ServeLineGame)g; for (var i = 0; i < 3; i++) if (m.WantAt(i) >= 0) m.Serve(m.WantAt(i)); },
            ["siopao_steam"] = (g, dt) => { var m = (SteamerGame)g; for (var i = 0; i < 4; i++) if (m.InZone(i)) m.Take(i); },
        };

        private static readonly HashSet<int> BotMemory = new HashSet<int>();

        [Test]
        public void EveryMinigameHasABot()
        {
            CollectionAssert.AreEquivalent(MinigameFactory.All, Bots.Keys);
        }

        [TestCaseSource(nameof(Ids))]
        public void CompetentPlayScoresWell(string id)
        {
            BotMemory.Clear();
            var score = Play(Make(id), Bots[id]);
            Debug.Log($"[Minigame] {id}: bot {score:0.00}");
            Assert.Greater(score, 0.6f, id);
        }

        [TestCaseSource(nameof(Ids))]
        public void DoingNothingScoresLow(string id)
        {
            var score = Play(Make(id), null);
            Assert.Less(score, 0.25f, id);
        }

        [Test]
        public void HigherEaseIsMoreForgiving()
        {
            // Hollow blocks: the green zone is wider for a better-suited / fresher worker.
            var hard = (HollowBlocksGame)Make("hollow_blocks", 0.6f);
            var easy = (HollowBlocksGame)Make("hollow_blocks", 1.3f);
            hard.Begin(_ => { });
            easy.Begin(_ => { });
            var zoneHard = hard.transform.GetComponentsInChildren<RectTransform>().First(r => r.name == "Zone").sizeDelta.x;
            var zoneEasy = easy.transform.GetComponentsInChildren<RectTransform>().First(r => r.name == "Zone").sizeDelta.x;
            Assert.Greater(zoneEasy, zoneHard);
        }

        public static IEnumerable<string> Ids() => MinigameFactory.All;
    }
}
