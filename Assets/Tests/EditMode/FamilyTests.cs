using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.Survival;
using Pagpag.UI;
using static Pagpag.Tests.TestData;

namespace Pagpag.Tests
{
    public class FamilyTests
    {
        [Test]
        public void FiveChildrenWithRequiredNamesAndUniqueProfiles()
        {
            var names = Fam.children.Select(c => c.displayName).ToList();
            Assert.AreEqual(5, names.Count);
            CollectionAssert.Contains(names, "Ed");
            CollectionAssert.Contains(names, "Edd");
            CollectionAssert.Contains(names, "Eddie");
            Assert.AreEqual(5, names.Distinct().Count());
            foreach (var c in Fam.children)
            {
                Assert.IsFalse(string.IsNullOrEmpty(c.personality), c.displayName);
                Assert.Greater(c.backstory.Length, 100, c.displayName);
                Assert.GreaterOrEqual(c.traitTags.Length, 2, c.displayName);
                Assert.NotNull(c.portrait, c.displayName);
            }
            // Each child is best at something different
            var bestSkills = Fam.children.Select(c => new[] { c.strength, c.dexterity, c.smarts, c.charm }.Max()).ToList();
            Assert.IsTrue(bestSkills.All(v => v >= 4));
        }

        [Test]
        public void RunHasSpouseAndFiveChildren()
        {
            var dong = NewRun(Gender.Male);
            Assert.AreEqual(6, dong.family.Count);
            Assert.AreEqual("Inday", dong.Spouse.name);
            var inday = NewRun(Gender.Female);
            Assert.AreEqual("Dong", inday.Spouse.name);
        }

        [Test]
        public void ChildrenGetHungryAtDifferentRates()
        {
            var run = NewRun();
            SurvivalSystem.Advance(run, 8 * 60, false, Cfg, Diff(run), Fam);
            Assert.Less(run.Member("ed").hunger, run.Member("pia").hunger, "Ed (big eater) gets hungrier than Pia");
            Assert.Less(run.Member("pia").energy, run.Member("ed").energy, "Pia (sickly) tires faster than Ed");
        }

        [Test]
        public void SpouseServesMealsFromThePantry()
        {
            var run = NewRun();
            run.pantrySafe = 20;
            foreach (var m in run.family) m.hunger = 50;
            SurvivalSystem.Advance(run, 90, false, Cfg, Diff(run), Fam, () => 0.99f); // crosses 7:00
            Assert.Less(run.pantrySafe, 20);
            Assert.IsTrue(run.family.All(m => m.hunger > 70), "Everyone at home ate");
        }

        [Test]
        public void EmptyPantryMeansHungryChildren()
        {
            var run = NewRun();
            foreach (var m in run.family) m.hunger = 50;
            var report = SurvivalSystem.Advance(run, 90, false, Cfg, Diff(run), Fam);
            Assert.IsTrue(report.messages.Any(s => s.Contains("ran out")));
            Assert.IsTrue(run.family.All(m => m.hunger < 50));
        }

        [Test]
        public void PagpagCanMakeChildrenSick()
        {
            var run = NewRun();
            run.pantryPagpag = 10;
            var pia = run.Member("pia");
            pia.hunger = 40;
            FamilyService.FeedMember(run, pia, Fam, Diff(run), () => 0f, new AdvanceReport());
            Assert.Less(pia.health, 100f);
            Assert.Greater(pia.hunger, 70f);
        }

        [Test]
        public void NeglectedChildDiesPermanentlyAndCausesDepression()
        {
            var run = NewRun();
            var pia = run.Member("pia");
            pia.hunger = 0;
            pia.health = 5;
            var report = SurvivalSystem.Advance(run, 6 * 60, false, Cfg, Diff(run), Fam);
            Assert.IsFalse(pia.alive);
            Assert.AreEqual(SurvivalSystem.CauseStarvation, pia.causeOfDeath);
            Assert.Contains("Pia", report.deaths);
            Assert.IsTrue(run.IsDepressed);
            Assert.AreEqual("Pia", run.depressionCause);
            Assert.AreEqual(1, run.childDeaths);
            Assert.IsFalse(run.isDead, "The player is still alive");

            // Permanent: feeding does nothing, time does nothing
            run.pantrySafe = 5;
            Assert.IsFalse(FamilyService.FeedMember(run, pia, Fam, Diff(run), () => 0.99f, new AdvanceReport()));
            SurvivalSystem.Advance(run, 24 * 60, true, Cfg, Diff(run), Fam);
            Assert.IsFalse(pia.alive);
            Assert.AreEqual(0f, pia.health);
        }

        [Test]
        public void DepressionLastsExactlyOneWeek()
        {
            var run = NewRun();
            SurvivalSystem.StartDepression(run, "Edd", Fam);
            Assert.AreEqual(7 * 24 * 60, run.DepressionMinutesLeft);
            run.day += 6;
            Assert.IsTrue(run.IsDepressed);
            run.day += 1;
            Assert.IsFalse(run.IsDepressed);
        }

        [Test]
        public void DepressionMakesStatsDecayFasterAndRecoverSlower()
        {
            var normal = NewRun();
            var grieving = NewRun();
            SurvivalSystem.StartDepression(grieving, "Ed", Fam);
            SurvivalSystem.Advance(normal, 6 * 60, false, Cfg, Diff(normal), null);
            SurvivalSystem.Advance(grieving, 6 * 60, false, Cfg, Diff(grieving), Fam);
            Assert.Less(grieving.hunger, normal.hunger - 5, "Faster hunger");
            Assert.Less(grieving.energy, normal.energy - 4, "Faster tiredness");

            normal.energy = grieving.energy = 10;
            SurvivalSystem.Advance(normal, 4 * 60, true, Cfg, Diff(normal), null);
            SurvivalSystem.Advance(grieving, 4 * 60, true, Cfg, Diff(grieving), Fam);
            Assert.Less(grieving.energy, normal.energy - 15, "Slower recovery");
        }

        [Test]
        public void DepressionReducesPay()
        {
            var normal = NewRun();
            var grieving = NewRun();
            SurvivalSystem.StartDepression(grieving, "Ed", Fam);
            foreach (var r in new[] { normal, grieving })
            {
                r.currentLocationId = "construction";
                r.minuteOfDay = 7 * 60;
                WorldService.CompleteJob(r, Loc("construction"), Act("construction", "con_morning"), 0.8f, false, 0.99f, Cfg, Diff(r), Fam);
            }
            Assert.Less(grieving.money - 500, (normal.money - 500) * 0.7f);
        }

        // ---------- Children's jobs ----------

        [Test]
        public void ChildrenCanOnlyTakeAgeAppropriateJobs()
        {
            var run = NewRun();
            run.minuteOfDay = 7 * 60;
            var pia = Fam.Get("pia");
            var ed = Fam.Get("ed");
            var haul = Act("market", "haul_crates");
            StringAssert.Contains("Too young", FamilyService.CanAssign(run, pia, run.Member("pia"), Loc("market"), haul, Cfg, Fam));
            Assert.IsNull(FamilyService.CanAssign(run, ed, run.Member("ed"), Loc("market"), haul, Cfg, Fam));
            StringAssert.Contains("Adults only", FamilyService.CanAssign(run, ed, run.Member("ed"), Loc("construction"), Act("construction", "con_morning"), Cfg, Fam));
        }

        [Test]
        public void TraitsChangePerformanceAndPay()
        {
            var ed = Fam.Get("ed");
            var edd = Fam.Get("edd");
            var full = new MemberState();
            Assert.Greater(FamilyService.ChildEase(ed, full, Skill.Strength), FamilyService.ChildEase(edd, full, Skill.Strength));
            Assert.Greater(FamilyService.ChildEase(edd, full, Skill.Smarts), FamilyService.ChildEase(ed, full, Skill.Smarts));
            Assert.Greater(FamilyService.ChildPayMultiplier(ed, Skill.Strength, Fam), FamilyService.ChildPayMultiplier(edd, Skill.Strength, Fam));
            var tired = new MemberState { energy = 20 };
            Assert.Less(FamilyService.ChildEase(ed, tired, Skill.Strength), FamilyService.ChildEase(ed, full, Skill.Strength));
        }

        [Test]
        public void ChildJobPaysTheFamilyAndTiresTheChild()
        {
            var run = NewRun();
            run.minuteOfDay = 7 * 60;
            var m = run.Member("ed");
            var res = FamilyService.CompleteChildJob(run, Fam, Fam.Get("ed"), m, Loc("market"), Act("market", "haul_crates"), 0.8f, false, 0.99f, Cfg, Diff(run));
            Assert.IsTrue(res.performed, res.message);
            Assert.Greater(run.money, 500);
            Assert.Less(m.energy, 85f);
            Assert.IsTrue(SurvivalSystem.IsBusy(run, m));
            Assert.IsNotNull(FamilyService.CanAssign(run, Fam.Get("ed"), m, Loc("market"), Act("market", "haul_crates"), Cfg, Fam), "Busy");
        }

        [Test]
        public void OverworkedChildCanDie()
        {
            var run = NewRun();
            var m = run.Member("pia");
            m.energy = 0;
            m.hunger = 0;
            m.health = 30;
            SurvivalSystem.Advance(run, 12 * 60, false, Cfg, Diff(run), Fam);
            Assert.IsFalse(m.alive);
        }

        // ---------- Intimacy and the wheel ----------

        [Test]
        public void WheelOddsAreExactlyFivePercent()
        {
            Assert.AreEqual(0.05f, Fam.pregnancyChance, 1e-6f);
            Assert.AreEqual(20, Fam.wheelSegments);
            Assert.AreEqual(1f / Fam.wheelSegments, Fam.pregnancyChance, 1e-6f, "One segment out of twenty");
        }

        [Test]
        public void WheelLandsOnTheRolledSegment()
        {
            for (var i = 0; i < 20; i++)
            {
                var angle = 360f * 6 + WheelOverlay.SegmentCenter(i, 20);
                Assert.AreEqual(i, WheelOverlay.SegmentAtPointer(angle, 20));
                Assert.AreEqual(i, WheelOverlay.SegmentAtPointer(angle + 6f, 20));
                Assert.AreEqual(i, WheelOverlay.SegmentAtPointer(angle - 6f, 20));
            }
        }

        [TestCase(Gender.Male)]
        [TestCase(Gender.Female)]
        public void PregnancyLosesTheGameForEitherCharacter(Gender g)
        {
            var run = NewRun(g);
            run.minuteOfDay = 21 * 60;
            var res = WorldService.ResolveIntimacy(run, Loc("home"), Act("home", "intimacy"), true, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed);
            Assert.IsTrue(run.isDead);
            Assert.AreEqual(EndKind.Pregnancy, run.endKind);
            StringAssert.Contains(g == Gender.Female ? "You are pregnant" : "Inday is pregnant", run.causeOfDeath);
        }

        [Test]
        public void NotPregnantContinuesWithASmallBoost()
        {
            var run = NewRun();
            run.minuteOfDay = 21 * 60;
            run.energy = 50;
            var res = WorldService.ResolveIntimacy(run, Loc("home"), Act("home", "intimacy"), false, Cfg, Diff(run), Fam);
            Assert.IsTrue(res.performed);
            Assert.IsFalse(run.isDead);
            Assert.IsNotNull(WorldService.CanDo(run, Loc("home"), Act("home", "intimacy"), Cfg), "Once a day");
        }

        [Test]
        public void IntimacyOnlyAtNightAndNeedsTheSpouse()
        {
            var run = NewRun();
            run.minuteOfDay = 10 * 60;
            Assert.IsNotNull(WorldService.CanDo(run, Loc("home"), Act("home", "intimacy"), Cfg));
            run.minuteOfDay = 21 * 60;
            Assert.IsNull(WorldService.CanDo(run, Loc("home"), Act("home", "intimacy"), Cfg));
            run.Spouse.alive = false;
            Assert.IsFalse(WorldService.IsVisible(run, Act("home", "intimacy")));
        }
    }
}
