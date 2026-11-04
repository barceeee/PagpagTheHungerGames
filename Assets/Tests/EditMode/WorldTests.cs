using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Jobs;
using Pagpag.Locations;
using Pagpag.Survival;
using static Pagpag.Tests.TestData;

namespace Pagpag.Tests
{
    public class WorldTests
    {
        // ---------- Stat scale and difficulty ----------

        [Test]
        public void StatsDrainDownFromHundred()
        {
            var run = NewRun();
            SurvivalSystem.Advance(run, 8 * 60, false, Cfg, Diff(run), Fam);
            Assert.Less(run.hunger, 100f);
            Assert.Less(run.energy, 100f);
            Assert.AreEqual(100f, run.health, "Fed and rested people don't lose health");
        }

        [Test]
        public void HardStarvesAndTiresNoticeablyFaster()
        {
            var normal = NewRun(d: Difficulty.Normal);
            var hard = NewRun(d: Difficulty.Hard);
            SurvivalSystem.Advance(normal, 8 * 60, false, Cfg, Diff(normal), Fam);
            SurvivalSystem.Advance(hard, 8 * 60, false, Cfg, Diff(hard), Fam);
            Assert.Greater(100 - hard.hunger, (100 - normal.hunger) * 1.5f);
            Assert.Greater(100 - hard.energy, (100 - normal.energy) * 1.5f);
            // ...for the children too
            Assert.Greater(100 - hard.Member("ed").hunger, (100 - normal.Member("ed").hunger) * 1.5f);
        }

        [Test]
        public void StarvationEventuallyKillsAndIsReportedAsCause()
        {
            foreach (var d in new[] { Difficulty.Normal, Difficulty.Hard })
            {
                var run = NewRun(d: d);
                for (var i = 0; i < 12 && !run.isDead; i++)
                {
                    SurvivalSystem.Advance(run, 8 * 60, true, Cfg, Diff(run), Fam);
                    SurvivalSystem.Advance(run, 16 * 60, false, Cfg, Diff(run), Fam);
                }
                Assert.IsTrue(run.isDead, d.ToString());
                Assert.AreEqual(EndKind.Death, run.endKind);
                Assert.AreEqual(SurvivalSystem.CauseStarvation, run.causeOfDeath);
            }
        }

        [Test]
        public void HardDiesSoonerWithoutFood()
        {
            int HoursToDeath(Difficulty d)
            {
                var run = NewRun(d: d);
                var hours = 0;
                while (!run.isDead && hours < 800)
                {
                    SurvivalSystem.Advance(run, 60, (hours % 24) < 8, Cfg, Diff(run), Fam);
                    hours++;
                }
                return hours;
            }
            Assert.Less(HoursToDeath(Difficulty.Hard), HoursToDeath(Difficulty.Normal) - 12);
        }

        [Test]
        public void SleepingRestoresEnergyAndAdvancesTheClock()
        {
            var run = NewRun();
            run.energy = 20;
            var res = WorldService.Perform(run, Loc("home"), Act("home", "sleep"), 0.99f, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed, res.message);
            Assert.Greater(run.energy, 95f);
            Assert.AreEqual(14 * 60, run.minuteOfDay);
        }

        // ---------- Map and travel ----------

        [Test]
        public void StartsAtHomeWithStartingPlacesUnlocked()
        {
            var run = NewRun();
            Assert.AreEqual("home", run.currentLocationId);
            foreach (var id in new[] { "home", "street", "pagpag", "market", "sarisari", "construction", "employment", "mainroad" })
                Assert.IsTrue(WorldService.IsUnlocked(run, Loc(id)), id);
            foreach (var id in new[] { "fishmarket", "port", "community", "train" })
                Assert.IsFalse(WorldService.IsUnlocked(run, Loc(id)), id);
        }

        [Test]
        public void WalkingIsFreeButTakesTimeAndEnergy()
        {
            var run = NewRun();
            var res = WorldService.Travel(run, Loc("market"), TravelMode.Walk, Locs, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed, res.message);
            Assert.AreEqual("market", run.currentLocationId);
            Assert.AreEqual(500, run.money);
            Assert.Greater(run.minuteOfDay, 6 * 60);
            Assert.Less(run.energy, 100f);
        }

        [Test]
        public void JeepneyCostsFareAndIsFasterForLongTrips()
        {
            var walkRun = NewRun();
            var jeepRun = NewRun();
            WorldService.Travel(walkRun, Loc("mainroad"), TravelMode.Walk, Locs, Cfg, Diff(walkRun), Fam);
            var res = WorldService.Travel(jeepRun, Loc("mainroad"), TravelMode.Jeepney, Locs, Cfg, Diff(jeepRun), Fam);
            Assert.IsTrue(res.performed, res.message);
            Assert.AreEqual(500 - Cfg.jeepneyFare, jeepRun.money);
            Assert.Less(jeepRun.minuteOfDay, walkRun.minuteOfDay);
        }

        [Test]
        public void CannotTravelToLockedPlaceUntilConditionIsMet()
        {
            var run = NewRun();
            Assert.IsFalse(WorldService.Travel(run, Loc("port"), TravelMode.Walk, Locs, Cfg, Diff(run), Fam).performed);
            run.currentLocationId = "employment";
            run.minuteOfDay = 9 * 60;
            Assert.IsTrue(WorldService.Perform(run, Loc("employment"), Act("employment", "register"), 0.99f, Cfg, Diff(run), Fam).performed);
            Assert.IsTrue(WorldService.RefreshUnlocks(run, Locs).Any(l => l.id == "port"));
            Assert.IsTrue(WorldService.Travel(run, Loc("port"), TravelMode.Walk, Locs, Cfg, Diff(run), Fam).performed);
        }

        [Test]
        public void ClosedPlacesBlockActions()
        {
            var run = NewRun();
            run.currentLocationId = "pagpag";
            run.minuteOfDay = 9 * 60;
            Assert.IsNotNull(WorldService.CanDo(run, Loc("pagpag"), Act("pagpag", "pagpag_chicken"), Cfg));
            run.minuteOfDay = 18 * 60;
            Assert.IsNull(WorldService.CanDo(run, Loc("pagpag"), Act("pagpag", "pagpag_chicken"), Cfg));
        }

        [Test]
        public void EatingCostsMoneyAndRestoresHunger()
        {
            var run = NewRun();
            run.currentLocationId = "market";
            run.minuteOfDay = 8 * 60;
            run.hunger = 30;
            var res = WorldService.Perform(run, Loc("market"), Act("market", "carinderia"), 0.99f, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed, res.message);
            Assert.AreEqual(430, run.money);
            Assert.Greater(run.hunger, 75f);
        }

        // ---------- Jobs pay only through minigames ----------

        [Test]
        public void EveryJobHasADistinctMinigame()
        {
            var jobs = Locs.locations.SelectMany(l => l.actions).Where(a => a.IsWork).ToList();
            Assert.AreEqual(18, jobs.Count);
            foreach (var j in jobs)
                CollectionAssert.Contains(Pagpag.Minigames.MinigameFactory.All, j.minigameId, j.id);
            Assert.AreEqual(jobs.Count, jobs.Select(j => j.minigameId).Distinct().Count(), "Each job has its own minigame");
        }

        [Test]
        public void WorkCannotBePerformedWithoutItsMinigame()
        {
            var run = NewRun();
            run.currentLocationId = "construction";
            run.minuteOfDay = 7 * 60;
            var res = WorldService.Perform(run, Loc("construction"), Act("construction", "con_morning"), 0.5f, Cfg, Diff(run), Fam);
            Assert.IsFalse(res.performed);
            Assert.AreEqual(500, run.money);
        }

        [TestCase(1f, 325)]
        [TestCase(0.8f, 260)]
        [TestCase(0.4f, 130)]
        [TestCase(0.1f, 0)]
        public void PayDependsOnPerformance(float score, int expected)
        {
            var run = NewRun();
            run.currentLocationId = "construction";
            run.minuteOfDay = 7 * 60;
            var res = WorldService.CompleteJob(run, Loc("construction"), Act("construction", "con_morning"), score, false, 0.99f, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed, res.message);
            Assert.AreEqual(500 + expected, run.money, res.message);
        }

        [Test]
        public void WalkingAwayPaysNothingAndUsesTheShift()
        {
            var run = NewRun();
            run.currentLocationId = "construction";
            run.minuteOfDay = 7 * 60;
            var res = WorldService.CompleteJob(run, Loc("construction"), Act("construction", "con_morning"), 1f, true, 0.99f, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed);
            Assert.AreEqual(500, run.money);
            Assert.IsNotNull(WorldService.CanDo(run, Loc("construction"), Act("construction", "con_morning"), Cfg));
        }

        [Test]
        public void SameWorkDrainsMoreOnHardButPaysTheSame()
        {
            var normal = NewRun(d: Difficulty.Normal);
            var hard = NewRun(d: Difficulty.Hard);
            foreach (var r in new[] { normal, hard })
            {
                r.currentLocationId = "construction";
                r.minuteOfDay = 7 * 60;
                Assert.IsTrue(WorldService.CompleteJob(r, Loc("construction"), Act("construction", "con_morning"), 0.8f, false, 0.99f, Cfg, Diff(r), Fam).performed);
            }
            Assert.AreEqual(normal.money, hard.money);
            Assert.Less(hard.energy, normal.energy - 15);
        }

        [Test]
        public void WorkIsBlockedWhenExhausted()
        {
            var run = NewRun();
            run.currentLocationId = "construction";
            run.minuteOfDay = 7 * 60;
            run.energy = 10;
            StringAssert.Contains("exhausted", WorldService.CanDo(run, Loc("construction"), Act("construction", "con_morning"), Cfg));
        }

        [Test]
        public void DongAndIndaySeeDifferentJobs()
        {
            var dong = NewRun(Gender.Male);
            var inday = NewRun(Gender.Female);
            Assert.IsTrue(WorldService.IsVisible(dong, Act("construction", "con_morning")));
            Assert.IsFalse(WorldService.IsVisible(inday, Act("construction", "con_morning")));
            Assert.IsTrue(WorldService.IsVisible(inday, Act("construction", "con_cook")));
            Assert.IsFalse(WorldService.IsVisible(dong, Act("construction", "con_cook")));
        }

        [Test]
        public void PayoutRules()
        {
            Assert.AreEqual(0, JobRules.Payout(200, 0.1f));
            Assert.AreEqual(200, JobRules.Payout(200, 0.8f));
            Assert.AreEqual(250, JobRules.Payout(200, 1f));
            Assert.AreEqual(120, JobRules.Payout(200, 0.8f, 0.6f), "Grief multiplier");
        }
    }
}
