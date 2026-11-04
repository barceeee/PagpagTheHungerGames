using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.Survival;
using Pagpag.TimeSystem;

namespace Pagpag.Tests
{
    /// <summary>Illness (pagpag streaks, random illness, death), the pharmacy and medicine, and the NPC dialog data.</summary>
    public class IllnessTests
    {
        private static FamilyDatabase Fam => TestData.Fam;
        private static IllnessSettings Cfg => Fam.illness;

        /// <summary>Keeps everyone fed and rested so only the illness hurts.</summary>
        private static void KeepFed(RunData r)
        {
            r.hunger = r.energy = 100;
            foreach (var m in r.family) { m.hunger = m.energy = 100; }
        }

        private static void PassDays(RunData r, int days, System.Func<float> rng = null)
        {
            for (var h = 0; h < days * 24 && !r.isDead; h++)
            {
                KeepFed(r);
                SurvivalSystem.Advance(r, 60, false, TestData.Cfg, TestData.Diff(r), Fam, rng ?? (() => 0.99f));
            }
        }

        // ---------- Getting sick ----------

        [Test]
        public void PagpagEveryDayMakesTyphoidLikelierEachDay()
        {
            var s = new IllnessState();
            var day1 = IllnessRules.PagpagChance(s, 1, Fam);
            IllnessRules.AtePagpag(s, 1, 0.99f, Fam);
            var day2 = IllnessRules.PagpagChance(s, 2, Fam);
            IllnessRules.AtePagpag(s, 2, 0.99f, Fam);
            var day3 = IllnessRules.PagpagChance(s, 3, Fam);
            Assert.Less(day1, day2);
            Assert.Less(day2, day3);
            Assert.AreEqual(2, IllnessRules.CurrentStreak(s, 2));
        }

        [Test]
        public void ADayWithoutPagpagResetsTheStreak()
        {
            var s = new IllnessState();
            for (var d = 1; d <= 4; d++) IllnessRules.AtePagpag(s, d, 0.99f, Fam);
            Assert.AreEqual(Cfg.pagpagBaseChance, IllnessRules.PagpagChance(s, 6, Fam), 1e-5, "Day 5 skipped: back to the first-day chance");
        }

        [Test]
        public void EatingStreetPagpagCanCauseTyphoid()
        {
            var r = TestData.NewRun();
            r.currentLocationId = "pagpag";
            r.minuteOfDay = 17 * 60;
            r.playerIllness.lastPagpagDay = r.day - 1;
            r.playerIllness.pagpagStreak = 10; // ate it for ten days straight
            var res = WorldService.Perform(r, TestData.Loc("pagpag"), TestData.Act("pagpag", "pagpag_rice"), 0.99f, TestData.Cfg, TestData.Diff(r), Fam, () => 0f);
            Assert.IsTrue(res.performed);
            Assert.AreEqual("typhoid", r.playerIllness.id);
            Assert.IsTrue(res.warnings.Any(w => w.Contains("typhoid")), string.Join(" / ", res.warnings));
        }

        [Test]
        public void FeedingTheChildrenPagpagCanMakeThemSick()
        {
            var r = TestData.NewRun();
            r.pantrySafe = 0;
            r.pantryPagpag = 10;
            var kid = r.family.First(m => !m.isSpouse);
            kid.illness.lastPagpagDay = r.day - 1;
            kid.illness.pagpagStreak = 10;
            var report = new AdvanceReport();
            FamilyService.FeedMember(r, kid, Fam, TestData.Diff(r), () => 0f, report);
            Assert.AreEqual("typhoid", kid.illness.id);
        }

        [Test]
        public void SafeFoodNeverCausesTyphoid()
        {
            var r = TestData.NewRun();
            r.pantrySafe = 50;
            var kid = r.family.First(m => !m.isSpouse);
            for (var i = 0; i < 30; i++) FamilyService.FeedMember(r, kid, Fam, TestData.Diff(r), () => 0f, new AdvanceReport());
            Assert.IsFalse(IllnessRules.IsIll(kid.illness));
        }

        [Test]
        public void RandomIllnessCanStrikeAnyoneAtTheDailyCheck()
        {
            var r = TestData.NewRun();
            SurvivalSystem.Advance(r, 24 * 60, false, TestData.Cfg, TestData.Diff(r), Fam, () => 0f);
            Assert.IsTrue(IllnessRules.IsIll(r.playerIllness));
            Assert.IsTrue(r.family.All(m => !m.alive || IllnessRules.IsIll(m.illness)));
            Assert.AreNotEqual("typhoid", r.playerIllness.id, "Typhoid only comes from pagpag");
        }

        [Test]
        public void HealthyAndLuckyStaysHealthy()
        {
            var r = TestData.NewRun();
            PassDays(r, 10);
            Assert.IsFalse(IllnessRules.IsIll(r.playerIllness));
        }

        // ---------- Untreated illness kills ----------

        [Test]
        public void UntreatedIllnessKillsThePlayer([Values("flu", "typhoid", "dengue")] string illness)
        {
            var r = TestData.NewRun();
            IllnessRules.Infect(r.playerIllness, Cfg.Get(illness), Cfg);
            PassDays(r, 30);
            Assert.IsTrue(r.isDead, $"Still alive: health {r.health}, severity {r.playerIllness.severity}");
            Assert.AreEqual(Cfg.Get(illness).displayName, r.causeOfDeath);
        }

        [Test]
        public void UntreatedIllnessKillsTheSpouseAndChildren([Values] bool spouse)
        {
            var r = TestData.NewRun();
            var m = r.family.First(x => x.isSpouse == spouse);
            IllnessRules.Infect(m.illness, Cfg.Get("flu"), Cfg);
            PassDays(r, 30);
            Assert.IsFalse(m.alive);
            Assert.AreEqual("Flu", m.causeOfDeath);
            Assert.AreEqual(m.name, r.depressionCause, "A death in the family starts the grief week");
            Assert.IsFalse(r.isDead, "Only that family member died");
        }

        [Test]
        public void TheSickDoNotHeal()
        {
            var r = TestData.NewRun();
            r.health = 60;
            IllnessRules.Infect(r.playerIllness, Cfg.Get("flu"), Cfg);
            KeepFed(r);
            SurvivalSystem.Advance(r, 8 * 60, true, TestData.Cfg, TestData.Diff(r), Fam, () => 0.99f);
            Assert.Less(r.health, 60f);
        }

        [Test]
        public void SeriousIllnessStopsWork()
        {
            var r = TestData.NewRun();
            IllnessRules.Infect(r.playerIllness, Cfg.Get("flu"), Cfg);
            r.playerIllness.severity = Cfg.workBlockSeverity;
            r.currentLocationId = "street";
            r.minuteOfDay = 8 * 60;
            var job = TestData.Loc("street").actions.First(a => a.IsWork && WorldService.IsVisible(r, a));
            StringAssert.Contains("sick", WorldService.CanDo(r, TestData.Loc("street"), job, TestData.Cfg));
        }

        // ---------- Medicine ----------

        [Test]
        public void TheMildIllnessIsCuredByDosesBeforeItKills()
        {
            var r = TestData.NewRun();
            IllnessRules.Infect(r.playerIllness, Cfg.Get("dengue"), Cfg);
            var doses = 0;
            for (var day = 0; day < 10 && IllnessRules.IsIll(r.playerIllness); day++)
            {
                IllnessRules.GiveDose(r.playerIllness, r.AbsMinute, Fam);
                doses++;
                PassDays(r, 1);
            }
            Assert.IsFalse(IllnessRules.IsIll(r.playerIllness));
            Assert.IsFalse(r.isDead);
            Assert.LessOrEqual(doses, 2);
        }

        [Test]
        public void BuyingAndTakingMedicineThroughTheActions()
        {
            var r = TestData.NewRun();
            var pharmacy = TestData.Loc("pharmacy");
            var home = TestData.Loc("home");
            Assert.IsTrue(Pagpag.Locations.WorldService.IsUnlocked(r, pharmacy), "Open from the start");
            r.currentLocationId = "pharmacy";
            r.minuteOfDay = 10 * 60;
            r.money = 5000;

            WorldService.Perform(r, pharmacy, TestData.Act("pharmacy", "buy_dose"), 0.99f, TestData.Cfg, TestData.Diff(r), Fam, () => 0.99f);
            WorldService.Perform(r, pharmacy, TestData.Act("pharmacy", "buy_cure"), 0.99f, TestData.Cfg, TestData.Diff(r), Fam, () => 0.99f);
            Assert.AreEqual(1, r.medicineDoses);
            Assert.AreEqual(1, r.instantCures);
            Assert.AreEqual(5000 - 120 - 2500, r.money);

            var takeDose = home.actions.First(a => a.id == "take_dose");
            Assert.IsFalse(WorldService.IsVisible(r, takeDose), "Only shown while sick");
            IllnessRules.Infect(r.playerIllness, Cfg.Get("flu"), Cfg);
            Assert.IsTrue(WorldService.IsVisible(r, takeDose));

            r.currentLocationId = "home";
            WorldService.Perform(r, home, takeDose, 0.99f, TestData.Cfg, TestData.Diff(r), Fam, () => 0.99f);
            Assert.AreEqual(0, r.medicineDoses);
            Assert.IsTrue(IllnessRules.IsTreated(r.playerIllness, r.AbsMinute));
            Assert.IsNotNull(WorldService.CanDo(r, home, takeDose, TestData.Cfg), "Cabinet is empty now");

            WorldService.Perform(r, home, home.actions.First(a => a.id == "take_cure"), 0.99f, TestData.Cfg, TestData.Diff(r), Fam, () => 0.99f);
            Assert.IsFalse(IllnessRules.IsIll(r.playerIllness), "The instant cure ends it at once");
            Assert.AreEqual(0, r.instantCures);
        }

        [Test]
        public void TheInstantCureCostsFarMoreThanACourseOfDoses()
        {
            var dose = TestData.Act("pharmacy", "buy_dose").cost;
            var cure = TestData.Act("pharmacy", "buy_cure").cost;
            Assert.Greater(cure, dose * 10);
        }

        [Test]
        public void ThePharmacistNamesWhoIsSick()
        {
            var r = TestData.NewRun();
            var kid = r.family.First(m => !m.isSpouse);
            IllnessRules.Infect(kid.illness, Cfg.Get("flu"), Cfg);
            StringAssert.Contains(kid.name, IllnessRules.Diagnosis(r, Fam));
            StringAssert.Contains("Nobody", IllnessRules.Diagnosis(TestData.NewRun(), Fam));
        }

        [Test]
        public void IllnessSurvivesSaveAndLoad()
        {
            var r = TestData.NewRun();
            IllnessRules.Infect(r.family[2].illness, Cfg.Get("dengue"), Cfg);
            var copy = UnityEngine.JsonUtility.FromJson<RunData>(UnityEngine.JsonUtility.ToJson(r));
            Assert.AreEqual("dengue", copy.family[2].illness.id);
            Assert.AreEqual(Cfg.onsetSeverity, copy.family[2].illness.severity, 1e-4);
        }

        // ---------- Dialog ----------

        [Test]
        public void EveryInteractionOutsideHomeHasDialog()
        {
            foreach (var loc in TestData.Locs.locations.Where(l => l.id != "home"))
                foreach (var a in loc.actions)
                {
                    Assert.IsTrue(a.HasDialog, $"{loc.id}/{a.id} has no dialog");
                    Assert.IsFalse(string.IsNullOrEmpty(a.speaker), $"{loc.id}/{a.id} has no speaker");
                }
        }

        [Test]
        public void TalkingToTheSpouseHasDialogButSleepingDoesNot()
        {
            var home = TestData.Loc("home");
            Assert.IsTrue(home.actions.First(a => a.id == "talk_wife").HasDialog);
            Assert.IsFalse(home.actions.First(a => a.id == "sleep").HasDialog);
            Assert.IsFalse(home.actions.First(a => a.id == "take_dose").HasDialog);
        }

        [Test]
        public void OnlyPagpagStallFoodCountsAsPagpag()
        {
            var pagpagFoods = TestData.Locs.locations.SelectMany(l => l.actions).Where(a => a.pagpagFood).Select(a => a.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "pagpag_chicken", "pagpag_rice" }, pagpagFoods);
        }
    }
}
