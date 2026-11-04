using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.Save;
using static Pagpag.Tests.TestData;

namespace Pagpag.Tests
{
    public class BlackSheepTests
    {
        private static int Sleep(RunData run, float roll)
        {
            var before = run.money;
            run.currentLocationId = "home";
            var res = WorldService.Perform(run, Loc("home"), Act("home", "sleep"), 0.99f, Cfg, Diff(run), Fam, () => roll);
            Assert.IsTrue(res.performed, res.message);
            return before - run.money;
        }

        [Test]
        public void ExactlyOneChildIsChosenAtTheStart()
        {
            for (var i = 0; i < 20; i++)
            {
                var run = NewRun();
                Assert.IsFalse(string.IsNullOrEmpty(run.blackSheepId));
                Assert.IsTrue(FamilyService.Children(run).Any(c => c.id == run.blackSheepId), "Must be one of the children");
            }
        }

        [Test]
        public void EveryChildCanBeChosen()
        {
            var run = NewRun();
            var picked = Enumerable.Range(0, 5).Select(i => FamilyService.PickBlackSheep(run, () => (i + 0.5f) / 5f)).ToList();
            CollectionAssert.AreEquivalent(new[] { "ed", "edd", "eddie", "joy", "pia" }, picked);
        }

        [Test]
        public void ChoiceSurvivesSaveAndLoad()
        {
            var dir = Path.Combine(Path.GetTempPath(), "pagpag-bs-" + Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectory = dir;
            try
            {
                var run = NewRun();
                var chosen = run.blackSheepId;
                SaveSystem.Save(run);
                Assert.IsTrue(SaveSystem.TryLoad(out var loaded));
                Assert.AreEqual(chosen, loaded.blackSheepId);
            }
            finally
            {
                SaveSystem.SaveDirectory = null;
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void SleepingLosesASensibleAmount()
        {
            foreach (var roll in new[] { 0f, 0.5f, 0.999f })
            {
                var run = NewRun();
                var taken = Sleep(run, roll);
                Assert.That(taken, Is.InRange(2, 4), $"roll {roll} with ₱500");
                Assert.AreEqual(taken, run.moneyMissing);
            }
        }

        [TestCase(1000, 12, 24)]
        [TestCase(1500, 22, 40)]
        public void SavingsLeakMoreThanCash(int money, int min, int max)
        {
            foreach (var roll in new[] { 0f, 0.999f })
            {
                var run = NewRun();
                run.money = money;
                Assert.That(Sleep(run, roll), Is.InRange(min, max), $"₱{money} roll {roll}");
            }
        }

        [Test]
        public void SavingsLeakFasterButCappedPerNight()
        {
            var run = NewRun();
            run.money = 5000;
            Assert.AreEqual(FamilyService.TheftMaxPerNight, Sleep(run, 0.99f));
        }

        [Test]
        public void TheLastPesosAreNeverTaken()
        {
            foreach (var money in new[] { 0, 3, 150, 399, FamilyService.TheftProtectedMoney })
            {
                var run = NewRun();
                run.money = money;
                Assert.AreEqual(0, Sleep(run, 0.99f), $"₱{money}");
                Assert.GreaterOrEqual(run.money, 0, "Never below zero");
            }
        }

        [Test]
        public void NapsTakeLess()
        {
            var run = NewRun();
            var nap = WorldService.Perform(run, Loc("home"), Act("home", "nap"), 0.99f, Cfg, Diff(run), Fam, () => 0.999f);
            Assert.IsTrue(nap.performed);
            Assert.That(500 - run.money, Is.InRange(1, 4));
        }

        [Test]
        public void WakeUpMessageIsNeutralAndNamesNoOne()
        {
            var run = NewRun();
            run.currentLocationId = "home";
            var res = WorldService.Perform(run, Loc("home"), Act("home", "sleep"), 0.99f, Cfg, Diff(run), Fam, () => 0.5f);
            StringAssert.Contains("is missing from your money", res.message);
            Assert.IsFalse(res.important, "Shown as a normal line, not a warning");
            foreach (var m in run.family)
                StringAssert.DoesNotContain(m.name, res.message);
            foreach (var w in res.warnings)
                StringAssert.DoesNotContain("missing", w);
        }

        [Test]
        public void NoChildrenMeansNoThief()
        {
            var run = NewRun();
            run.family.RemoveAll(m => !m.isSpouse);
            run.blackSheepId = FamilyService.PickBlackSheep(run, () => 0.5f);
            Assert.IsNull(run.blackSheepId);
            Assert.AreEqual(0, Sleep(run, 0.9f));
        }

        [Test]
        public void ADeadBlackSheepStealsNothing()
        {
            var run = NewRun();
            run.Member(run.blackSheepId).alive = false;
            Assert.AreEqual(0, Sleep(run, 0.9f));
        }

        [Test]
        public void ChoiceNeverChangesDuringTheRun()
        {
            var run = NewRun();
            var chosen = run.blackSheepId;
            for (var i = 0; i < 5; i++) Sleep(run, 0.5f);
            Assert.AreEqual(chosen, run.blackSheepId);
        }
    }
}
