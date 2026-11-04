using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Save;
using Pagpag.Survival;
using Pagpag.TimeSystem;

namespace Pagpag.Tests
{
    /// <summary>The winning condition: a year (365 days) with the whole family alive unlocks Infinite Mode.</summary>
    public class VictoryTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "pagpag-tests-" + Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectory = _dir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SaveDirectory = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        /// <summary>Moves the clock to <paramref name="minutes"/> after the run's first morning, without simulating survival.</summary>
        private static void SetSurvived(RunData r, int minutes)
        {
            var abs = TestData.Cfg.startMinuteOfDay + minutes;
            r.day = abs / GameTime.MinutesPerDay + 1;
            r.minuteOfDay = abs % GameTime.MinutesPerDay;
        }

        private static int Goal => TestData.Cfg.victoryDays * GameTime.MinutesPerDay;

        [Test]
        public void GoalIsAYear() => Assert.AreEqual(365, TestData.Cfg.victoryDays);

        [Test]
        public void NothingHappensBeforeTheGoal()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal - 1);
            Assert.AreEqual(EndKind.None, VictoryRules.Check(r, TestData.Cfg));
            Assert.IsFalse(r.victoryPending);
            Assert.IsFalse(r.isDead);
        }

        [Test]
        public void HundredDaysWithEveryoneAliveWins([Values] Gender g, [Values] Difficulty d)
        {
            var r = TestData.NewRun(g, d);
            SetSurvived(r, Goal);
            Assert.AreEqual(EndKind.Victory, VictoryRules.Check(r, TestData.Cfg));
            Assert.IsTrue(r.victoryPending && r.won);
            Assert.IsFalse(r.isDead, "A win is not a death: the run can go on in Infinite Mode");
            Assert.AreEqual(TestData.Cfg.victoryDays, VictoryRules.DaysSurvived(r, TestData.Cfg));
        }

        [Test]
        public void ReachingTheGoalAfterADeathEndsTheRunWithoutAWin([Values] bool spouse)
        {
            var r = TestData.NewRun();
            r.family.First(m => m.isSpouse == spouse).alive = false;
            SetSurvived(r, Goal);
            Assert.AreEqual(EndKind.SurvivedWithLosses, VictoryRules.Check(r, TestData.Cfg));
            Assert.IsTrue(r.isDead);
            Assert.IsFalse(r.won);
            StringAssert.Contains("lost 1 family member", r.causeOfDeath);
        }

        [Test]
        public void ADeadPlayerNeverWins()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal);
            r.isDead = true;
            r.endKind = EndKind.Death;
            Assert.AreEqual(EndKind.None, VictoryRules.Check(r, TestData.Cfg));
            Assert.AreEqual(EndKind.Death, r.endKind);
        }

        [Test]
        public void SurvivingTheLastNightWinsThroughTheRealRules()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal - 8 * 60);
            SurvivalSystem.Advance(r, 8 * 60, true, TestData.Cfg, TestData.Diff(r), TestData.Fam, () => 0.99f);
            Assert.IsFalse(r.isDead);
            Assert.AreEqual(EndKind.Victory, VictoryRules.Check(r, TestData.Cfg));
        }

        [Test]
        public void InfiniteModeHasNoGoalAndNeverEndsInAWin()
        {
            var r = TestData.NewRun();
            r.infiniteMode = true;
            SetSurvived(r, Goal * 5);
            Assert.AreEqual(EndKind.None, VictoryRules.Check(r, TestData.Cfg));
            r.family[0].alive = false;
            Assert.AreEqual(EndKind.None, VictoryRules.Check(r, TestData.Cfg), "A death does not end an Infinite Mode run either");
            Assert.IsFalse(r.isDead);
        }

        [Test]
        public void ContinuingAfterAWinSwitchesTheRunToInfiniteMode()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal);
            VictoryRules.Check(r, TestData.Cfg);
            VictoryRules.ContinueInInfiniteMode(r);
            Assert.IsFalse(r.victoryPending);
            Assert.IsTrue(r.infiniteMode && r.won);
            Assert.AreEqual(EndKind.None, r.endKind);
            SetSurvived(r, Goal * 2);
            Assert.AreEqual(EndKind.None, VictoryRules.Check(r, TestData.Cfg), "The goal is not checked twice");
        }

        [Test]
        public void APendingVictorySurvivesSaveAndLoad()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal);
            VictoryRules.Check(r, TestData.Cfg);
            SaveSystem.Save(r);
            Assert.IsTrue(SaveSystem.TryLoad(out var loaded));
            Assert.IsTrue(loaded.victoryPending);
        }

        [Test]
        public void InfiniteModeUnlockIsPermanent()
        {
            Assert.IsFalse(SaveSystem.InfiniteModeUnlocked);
            var r = TestData.NewRun();
            SaveSystem.Save(r);
            SaveSystem.UnlockInfiniteMode(r.runId);
            Assert.IsTrue(SaveSystem.InfiniteModeUnlocked);
            SaveSystem.BuryRun(r);
            SaveSystem.Delete();
            Assert.IsTrue(SaveSystem.InfiniteModeUnlocked, "Ending or deleting a run never takes the unlock away");
        }

        // ---------- Leaving the slums ----------

        private static RunData WonRun()
        {
            var r = TestData.NewRun();
            SetSurvived(r, Goal);
            VictoryRules.Check(r, TestData.Cfg);
            return r;
        }

        [Test]
        public void QuotaIsOneHundredThousandPesos() => Assert.AreEqual(100000, TestData.Cfg.leaveSlumsQuota);

        [Test]
        public void CannotLeaveBelowTheQuota()
        {
            var r = WonRun();
            r.money = 99999;
            StringAssert.Contains("₱100,000", VictoryRules.LeaveBlockedReason(r, TestData.Cfg));
            Assert.IsFalse(VictoryRules.LeaveTheSlums(r, TestData.Cfg));
            Assert.IsTrue(r.victoryPending, "A refused attempt changes nothing");
            Assert.IsFalse(r.isDead);
        }

        [Test]
        public void LeavingWithTheQuotaFromTheVictoryScreenEndsTheRun()
        {
            var r = WonRun();
            r.money = 100000;
            Assert.IsNull(VictoryRules.LeaveBlockedReason(r, TestData.Cfg));
            Assert.IsTrue(VictoryRules.LeaveTheSlums(r, TestData.Cfg));
            Assert.IsTrue(r.isDead && r.won);
            Assert.IsFalse(r.victoryPending);
            Assert.AreEqual(EndKind.LeftTheSlums, r.endKind);
        }

        [Test]
        public void NormalRunsCannotLeaveBeforeWinningEvenWithTheMoney()
        {
            var r = TestData.NewRun();
            r.money = 500000;
            Assert.IsNotNull(VictoryRules.LeaveBlockedReason(r, TestData.Cfg));
            Assert.IsFalse(VictoryRules.LeaveTheSlums(r, TestData.Cfg));
        }

        [Test]
        public void TheHomeActionIsOnlyThereInInfiniteModeAndNeedsTheQuota()
        {
            var home = TestData.Loc("home");
            var leave = home.actions.Single(a => a.kind == Pagpag.Locations.ActionKind.LeaveSlums);
            var r = TestData.NewRun();
            Assert.IsFalse(Pagpag.Locations.WorldService.IsVisible(r, leave), "Hidden in a normal run");
            r.infiniteMode = true;
            Assert.IsTrue(Pagpag.Locations.WorldService.IsVisible(r, leave));
            r.money = 99999;
            Assert.IsNotNull(Pagpag.Locations.WorldService.CanDo(r, home, leave, TestData.Cfg));
            r.money = 100000;
            Assert.IsNull(Pagpag.Locations.WorldService.CanDo(r, home, leave, TestData.Cfg));
            Assert.IsFalse(Pagpag.Locations.WorldService.Perform(r, home, leave, 0.5f, TestData.Cfg, TestData.Diff(r), TestData.Fam).performed,
                "Leaving goes through GameManager (with a confirm), never the generic action path");
        }

        [Test]
        public void NewStringsExist()
        {
            foreach (var key in new[] { "cc.infinite.locked", "cc.infinite.off", "cc.infinite.on", "world.victory.continue", "world.victory.finish",
                         "world.victory.leave", "world.victory.leave.locked", "world.leave.confirm.title", "world.leave.confirm.body",
                         "world.leave.confirm.yes", "world.leave.confirm.no", "dialog.never.mind", "dialog.go.work", "dialog.go.eat",
                         "dialog.go.buy", "dialog.go.pay", "dialog.go.stay", "dialog.go.talk" })
                Assert.IsTrue(Loc.Has(key), key);
        }
    }
}
