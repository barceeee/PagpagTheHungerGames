using System.Linq;
using NUnit.Framework;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.Survival;
using UnityEngine;
using static Pagpag.Tests.TestData;

namespace Pagpag.Tests
{
    /// <summary>
    /// Plays simplified days with a sensible strategy to check that feeding seven people from ₱500
    /// is tense but winnable, and that doing nothing fails.
    /// </summary>
    public class EconomyTests
    {
        private const float PlayerScore = 0.75f;
        private const float ChildScore = 0.7f;

        private static void WaitUntil(RunData run, int hour)
        {
            var target = hour * 60;
            var minutes = target - run.minuteOfDay;
            if (minutes < 0) minutes += 24 * 60;
            SurvivalSystem.Advance(run, minutes, false, Cfg, Diff(run), Fam, () => 0.5f);
        }

        private static void Go(RunData run, string loc) =>
            WorldService.Travel(run, Loc(loc), TravelMode.Walk, Locs, Cfg, Diff(run), Fam);

        private static bool Do(RunData run, string loc, string action) =>
            WorldService.Perform(run, Loc(loc), Act(loc, action), 0.99f, Cfg, Diff(run), Fam, () => 0.5f).performed;

        private static void PlayerJob(RunData run, string loc, string action)
        {
            if (WorldService.CanDo(run, Loc(loc), Act(loc, action), Cfg) == null)
                WorldService.CompleteJob(run, Loc(loc), Act(loc, action), PlayerScore, false, 0.99f, Cfg, Diff(run), Fam);
        }

        private static void ChildJob(RunData run, string child, string loc, string action)
        {
            var c = Fam.Get(child);
            var m = run.Member(child);
            if (m.energy < 50 || m.hunger < 40) return; // a careful parent doesn't send a tired or hungry child
            if (FamilyService.CanAssign(run, c, m, Loc(loc), Act(loc, action), Cfg, Fam) == null)
                FamilyService.CompleteChildJob(run, Fam, c, m, Loc(loc), Act(loc, action), ChildScore, false, 0.99f, Cfg, Diff(run));
        }

        private static void EatIfHungry(RunData run, float below)
        {
            if (run.hunger < below && run.PantryTotal > 0 && run.currentLocationId == "home") Do(run, "home", "pantry_meal");
        }

        /// <summary>One day of a sensible, hard-working household. Starts at 6:00 at home.</summary>
        private static void PlayDay(RunData run, bool dong)
        {
            EatIfHungry(run, 75);
            Go(run, "market");
            for (var i = 0; i < 3 && run.PantryTotal < 14 && run.money >= 240; i++) // food before savings
                if (!Do(run, "market", "market_family")) break;
            ChildJob(run, "ed", "market", "haul_crates");
            ChildJob(run, "eddie", "market", "sell_veg");
            if (dong) { Go(run, "construction"); PlayerJob(run, "construction", "con_morning"); }
            else PlayerJob(run, "market", "sell_veg");
            if (run.isDead) return;
            Go(run, "home");
            EatIfHungry(run, 75);
            if (run.minuteOfDay < 12 * 60) WaitUntil(run, 12);
            ChildJob(run, "edd", "sarisari", "mind_store");
            if (run.energy < 45) Do(run, "home", "nap");
            if (run.energy >= 60 && run.hunger >= 50)
            {
                if (dong) { Go(run, "construction"); PlayerJob(run, "construction", "con_afternoon"); }
                else { Go(run, "street"); PlayerJob(run, "street", "laundry"); }
                Go(run, "home");
            }
            if (run.minuteOfDay < 13 * 60) WaitUntil(run, 13);
            ChildJob(run, "joy", "market", "bundle_veg");
            if (run.isDead) return;
            if (run.money >= 110 && run.PantryTotal < 8) { Go(run, "sarisari"); Do(run, "sarisari", "sari_family"); Go(run, "home"); }
            EatIfHungry(run, 60);
            if (run.minuteOfDay < 19 * 60) WaitUntil(run, 19);
            if (run.PantryTotal > 0) Do(run, "home", "family_meal");
            WaitUntil(run, 21);
            Do(run, "home", "sleep");     // until 5:00
            WaitUntil(run, 6);
        }

        [TestCase(Gender.Male, Difficulty.Normal)]
        [TestCase(Gender.Female, Difficulty.Normal)]
        [TestCase(Gender.Male, Difficulty.Hard)]
        [TestCase(Gender.Female, Difficulty.Hard)]
        public void HardWorkingFamilySurvivesTenDays(Gender g, Difficulty d)
        {
            var run = NewRun(g, d);
            var minMoney = run.money;
            for (var day = 0; day < 10 && !run.isDead; day++)
            {
                PlayDay(run, g == Gender.Male);
                minMoney = Mathf.Min(minMoney, run.money);
            }
            var dead = run.family.Where(m => !m.alive).Select(m => m.name).ToList();
            Debug.Log($"[Economy] {g}/{d}: day {run.day}, money ₱{run.money} (low ₱{minMoney}), pantry {run.PantryTotal}, " +
                      $"player H{run.hunger:0}/E{run.energy:0}/HP{run.health:0}, earned ₱{run.totalEarned}, food ₱{run.foodSpent}, dead: {string.Join(",", dead)}");
            Assert.IsFalse(run.isDead, "Player died");
            CollectionAssert.IsEmpty(dead, "Family members died");
        }

        [Test]
        public void HardIsTighterThanNormal()
        {
            var normal = NewRun(Gender.Male, Difficulty.Normal);
            var hard = NewRun(Gender.Male, Difficulty.Hard);
            for (var day = 0; day < 6; day++)
            {
                PlayDay(normal, true);
                PlayDay(hard, true);
            }
            Assert.Less(hard.money + hard.PantryTotal * 24, normal.money + normal.PantryTotal * 24, "Hard leaves less money and food");
        }

        [Test]
        public void ADayOfFoodForSevenCostsMoreThanOneShiftPays()
        {
            // Tension: one adult shift alone can't feed the family; someone else must work too.
            var run = NewRun(Gender.Female, Difficulty.Normal);
            var start = run.money;
            run.pantrySafe = 40;
            for (var i = 0; i < 2; i++)
            {
                WaitUntil(run, 21);
                Do(run, "home", "family_meal");
                Do(run, "home", "sleep");
                WaitUntil(run, 6);
            }
            var used = 40 - run.pantrySafe;
            var costPerDay = used / 2f * (240f / 10f);
            var oneShift = Act("market", "sell_veg").wage;
            Debug.Log($"[Economy] servings/day {used / 2f:0.0}, cost/day ₱{costPerDay:0}, one Inday shift ₱{oneShift}");
            Assert.Greater(costPerDay, oneShift);
        }

        [Test]
        public void DoingNothingStarvesTheFamily()
        {
            var run = NewRun(Gender.Male, Difficulty.Normal);
            for (var day = 0; day < 6 && !run.isDead; day++)
            {
                Go(run, "market");
                if (run.money >= 240) Do(run, "market", "market_family"); // spend savings, never earn
                Go(run, "home");
                WaitUntil(run, 21);
                Do(run, "home", "sleep");
                WaitUntil(run, 6);
            }
            Assert.IsTrue(run.isDead || run.family.Any(m => !m.alive), "Without work, someone dies within six days");
        }
    }
}
