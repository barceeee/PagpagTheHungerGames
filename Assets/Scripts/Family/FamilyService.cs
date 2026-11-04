using System;
using System.Collections.Generic;
using System.Linq;
using Pagpag.Core;
using Pagpag.Jobs;
using Pagpag.Locations;
using Pagpag.Survival;
using Pagpag.TimeSystem;
using UnityEngine;

namespace Pagpag.Family
{
    /// <summary>
    /// Household rules: creating the family, the pantry, meals, member status and children's jobs.
    /// Pure logic on <see cref="RunData"/>.
    /// </summary>
    public static class FamilyService
    {
        public const string SpouseId = "spouse";

        public static void CreateFamily(RunData run, FamilyDatabase fam, string spouseName)
        {
            run.family.Clear();
            run.family.Add(new MemberState { id = SpouseId, name = spouseName, isSpouse = true });
            foreach (var c in fam.children)
                if (c != null) run.family.Add(new MemberState { id = c.id, name = c.displayName });
        }

        public static IEnumerable<MemberState> Children(RunData run) => run.family.Where(m => !m.isSpouse);

        // =====================================================================
        // The black sheep
        // =====================================================================

        /// <summary>Picks one child uniformly at random. Returns null when there are no children.</summary>
        public static string PickBlackSheep(RunData run, Func<float> rng)
        {
            var kids = Children(run).ToList();
            if (kids.Count == 0) return null;
            var i = Mathf.Min(kids.Count - 1, Mathf.FloorToInt(rng() * kids.Count));
            return kids[i].id;
        }

        /// <summary>Money below this is never stolen (about a day and a half of food for seven).</summary>
        public const int TheftProtectedMoney = 400;
        public const int TheftMaxPerNight = 40;

        /// <summary>
        /// Money the black sheep takes while the player sleeps — skimmed from savings, never from the last pesos.
        /// Formula: amount = min(₱40, round((money − ₱400) × lerp(2%, 4%, roll))) × min(1, minutesSlept ÷ 480),
        /// rounded to the peso; nothing when money ≤ ₱400, so money can never go below zero.
        /// Examples for a full night: ₱500 → ₱2–4; ₱1,000 → ₱12–24; ₱1,500 → ₱22–40; ₱1,800+ → ₱28–40.
        /// Naps take proportionally less.
        /// Why: a struggling family (Bisaya/Hard hovers around ₱500) loses a little, keeping the game winnable, while
        /// a family that builds savings feels a steady, unexplained leak. No theft if there is no black sheep or they died.
        /// </summary>
        public static int SleepTheft(RunData run, int minutesSlept, float roll)
        {
            if (string.IsNullOrEmpty(run.blackSheepId) || minutesSlept <= 0) return 0;
            if (run.money <= TheftProtectedMoney) return 0;
            var thief = run.Member(run.blackSheepId);
            if (thief == null || !thief.alive) return 0;
            var savings = run.money - TheftProtectedMoney;
            var amount = Mathf.Min(TheftMaxPerNight, Mathf.RoundToInt(savings * Mathf.Lerp(0.02f, 0.04f, Mathf.Clamp01(roll))));
            amount = Mathf.RoundToInt(amount * Mathf.Min(1f, minutesSlept / 480f));
            return Mathf.Clamp(amount, 0, run.money);
        }
        public static int ChildrenAlive(RunData run) => Children(run).Count(m => m.alive);

        public static string Role(RunData run, MemberState m, FamilyDatabase fam)
        {
            if (m.isSpouse) return run.gender == Gender.Male ? "Your wife" : "Your husband";
            var c = fam.Get(m.id);
            return c != null ? c.role : "";
        }

        public static string StatusText(RunData run, MemberState m, GameConfig cfg)
        {
            if (!m.alive) return $"Died on Day {m.diedDay} ({m.causeOfDeath?.ToLowerInvariant()})";
            var sick = IllnessRules.Describe(m.illness, run.AbsMinute, GameManager.Instance != null ? GameManager.Instance.Family : null);
            var where = WhereText(run, m, cfg);
            return sick != null ? $"Sick: {sick}  ·  {where}" : where;
        }

        private static string WhereText(RunData run, MemberState m, GameConfig cfg)
        {
            if (SurvivalSystem.IsBusy(run, m))
                return $"Working: {m.busyJob} — back at {GameTime.Clock(m.busyUntilAbs % GameTime.MinutesPerDay)}";
            if (SurvivalSystem.IsAsleep(run, m, cfg)) return "Asleep at home";
            return m.isSpouse ? "At home with the children" : "At home";
        }

        public static bool AtHome(RunData run, MemberState m) => m.alive && !SurvivalSystem.IsBusy(run, m);
        public static bool PlayerAtHome(RunData run) => run.currentLocationId == "home";

        // =====================================================================
        // Pantry and meals
        // =====================================================================

        /// <summary>Takes one serving, safe food first. Returns false if the pantry is empty.</summary>
        public static bool TakeServing(RunData run, out bool pagpag)
        {
            pagpag = false;
            if (run.pantrySafe > 0) { run.pantrySafe--; return true; }
            if (run.pantryPagpag > 0) { run.pantryPagpag--; pagpag = true; return true; }
            return false;
        }

        /// <summary>Feeds one member a serving. Returns false if there was no food.</summary>
        public static bool FeedMember(RunData run, MemberState m, FamilyDatabase fam, DifficultySettings diff, Func<float> rng, AdvanceReport report)
        {
            if (!m.alive || !TakeServing(run, out var pagpag)) return false;
            SurvivalSystem.ApplyMemberDeltas(run, m, fam.servingRestore, 0, 0, diff, fam, report);
            if (pagpag && rng() < fam.pagpagSicknessChance)
            {
                SurvivalSystem.ApplyMemberDeltas(run, m, 0, -5, -fam.pagpagSicknessDamage, diff, fam, report, SurvivalSystem.CauseFoodPoisoning);
                report?.messages.Add($"{m.name} got sick from the pagpag.");
            }
            if (pagpag && m.alive && IllnessRules.AtePagpag(m.illness, run.day, rng(), fam))
                report?.messages.Add($"{m.name} has a high fever: typhoid, from eating pagpag day after day. Give them medicine before it gets worse.");
            return true;
        }

        /// <summary>The player eats one pantry serving.</summary>
        public static bool FeedPlayer(RunData run, FamilyDatabase fam, DifficultySettings diff, Func<float> rng, List<string> messages)
        {
            if (!TakeServing(run, out var pagpag)) return false;
            SurvivalSystem.ApplyDeltas(run, fam.servingRestore, 0, 0, diff);
            run.mealsEaten++;
            if (pagpag && rng() < fam.pagpagSicknessChance)
            {
                SurvivalSystem.ApplyDeltas(run, 0, -5, -fam.pagpagSicknessDamage, diff, SurvivalSystem.CauseFoodPoisoning);
                messages?.Add("The pagpag disagrees with you. (Health −" + Mathf.RoundToInt(fam.pagpagSicknessDamage) + ")");
            }
            if (pagpag) PlayerAtePagpag(run, fam, rng, messages);
            return true;
        }

        /// <summary>The player ate pagpag (pantry or street stall): streak warning and the typhoid roll.</summary>
        public static void PlayerAtePagpag(RunData run, FamilyDatabase fam, Func<float> rng, List<string> messages)
        {
            if (run.isDead) return;
            if (IllnessRules.AtePagpag(run.playerIllness, run.day, rng(), fam))
            {
                messages?.Add("Your head burns and your stomach cramps: typhoid fever, from eating pagpag day after day. It will get worse without medicine.");
                return;
            }
            var streak = IllnessRules.CurrentStreak(run.playerIllness, run.day);
            if (streak >= 2 && !IllnessRules.IsIll(run.playerIllness))
                messages?.Add($"That's {streak} days of pagpag in a row. Your stomach is starting to complain.");
        }

        /// <summary>Feeding order: youngest child first, the spouse last.</summary>
        public static List<MemberState> FeedingOrder(RunData run, FamilyDatabase fam) =>
            run.family.Where(m => m.alive)
                .OrderBy(m => m.isSpouse ? 1 : 0)
                .ThenBy(m => fam.Get(m.id)?.age ?? 99)
                .ToList();

        /// <summary>The spouse serves a meal from the pantry to everyone at home who is hungry.</summary>
        public static void AutoMeal(RunData run, FamilyDatabase fam, Func<float> rng, AdvanceReport report, int hour)
        {
            var spouse = run.Spouse;
            var hungry = FeedingOrder(run, fam).Where(m => AtHome(run, m) && m.hunger < fam.autoMealThreshold).ToList();
            if (hungry.Count == 0) return;
            var meal = hour < 10 ? "breakfast" : hour < 15 ? "lunch" : "dinner";

            if (spouse == null || !spouse.alive)
            {
                report.messages.Add($"No one was home to serve {meal}. Feed the children yourself.");
                return;
            }

            var diff = DefaultDiff;
            var fed = new List<string>();
            var missed = new List<string>();
            foreach (var m in hungry)
            {
                if (FeedMember(run, m, fam, diff, rng, report)) fed.Add(m.name);
                else missed.Add(m.name);
            }
            if (fed.Count > 0)
                report.messages.Add($"{spouse.name} served {meal}: {string.Join(", ", fed)} ate. Pantry: {run.PantryTotal} left.");
            if (missed.Count > 0)
                report.messages.Add($"The pantry ran out at {meal}! {string.Join(", ", missed)} went hungry.");
        }

        /// <summary>The player serves everyone at home (including themselves) from the pantry.</summary>
        public static string ServeFamilyMeal(RunData run, FamilyDatabase fam, DifficultySettings diff, Func<float> rng, AdvanceReport report)
        {
            var fed = new List<string>();
            var missed = new List<string>();
            if (run.hunger < 90f)
            {
                var msgs = new List<string>();
                if (FeedPlayer(run, fam, diff, rng, msgs)) fed.Add("you");
                else missed.Add("you");
                report.messages.AddRange(msgs);
            }
            foreach (var m in FeedingOrder(run, fam).Where(m => AtHome(run, m) && m.hunger < 90f))
            {
                if (FeedMember(run, m, fam, diff, rng, report)) fed.Add(m.name);
                else missed.Add(m.name);
            }
            if (fed.Count == 0 && missed.Count == 0) return "Everyone at home is already full.";
            var msg = fed.Count > 0 ? $"You served a family meal. {string.Join(", ", fed)} ate." : "";
            if (missed.Count > 0) msg += $" Not enough food for {string.Join(", ", missed)}.";
            return (msg + $" Pantry: {run.PantryTotal} left.").Trim();
        }

        // Difficulty only affects drains, which feeding never applies; a neutral instance is enough.
        private static DifficultySettings _defaultDiff;
        private static DifficultySettings DefaultDiff
        {
            get
            {
                if (_defaultDiff == null)
                {
                    _defaultDiff = ScriptableObject.CreateInstance<DifficultySettings>();
                    _defaultDiff.hideFlags = HideFlags.HideAndDontSave;
                }
                return _defaultDiff;
            }
        }

        // =====================================================================
        // Children's jobs
        // =====================================================================

        /// <returns>null if the child can take this job now, otherwise the reason.</returns>
        public static string CanAssign(RunData run, ChildProfile c, MemberState m, LocationData loc, LocationAction a,
            GameConfig cfg, FamilyDatabase fam)
        {
            if (run.isDead) return "The run has ended.";
            if (c == null || m == null || !m.alive) return $"{m?.name} has died.";
            if (!a.ChildrenCanDo) return "Adults only.";
            if (c.age < a.childMinAge) return $"Too young (age {a.childMinAge}+).";
            if (SurvivalSystem.IsBusy(run, m)) return $"Busy until {GameTime.Clock(m.busyUntilAbs % GameTime.MinutesPerDay)}.";
            if (!WorldService.IsUnlocked(run, loc)) return $"{loc.displayName} is locked.";
            if (!WorldService.IsOpen(loc, run.minuteOfDay)) return $"{loc.displayName} is closed now.";
            if (!GameTime.InWindow(run.minuteOfDay / 60, a.startHourFrom, a.startHourTo))
                return $"Only {GameTime.Hour(a.startHourFrom)} – {GameTime.Hour(a.startHourTo)}.";
            if (run.UsedToday(a.id, m.id)) return "Already did this job today.";
            if (m.energy < fam.childMinEnergyToWork) return "Too tired to work.";
            if (m.hunger < fam.childMinHungerToWork) return "Too hungry to work.";
            if (IllnessRules.TooSickToWork(m.illness, fam)) return "Too sick to work. Give them medicine.";
            return null;
        }

        /// <summary>Minigame ease for a child: skill in the job's ability and current energy.</summary>
        public static float ChildEase(ChildProfile c, MemberState m, Skill skill) =>
            (0.7f + 0.15f * (c.SkillValue(skill) - 1)) * (0.75f + 0.25f * m.energy / 100f);

        /// <summary>Share of the adult wage this child earns at full performance.</summary>
        public static float ChildPayMultiplier(ChildProfile c, Skill skill, FamilyDatabase fam) =>
            fam.childPayRate * (0.7f + 0.15f * (c.SkillValue(skill) - 1));

        public static int ChildFullPay(ChildProfile c, LocationAction a, FamilyDatabase fam) =>
            JobRules.Payout(a.wage, JobRules.FullPayScore, ChildPayMultiplier(c, a.skill, fam));

        public static string FitLabel(ChildProfile c, Skill skill)
        {
            var v = c.SkillValue(skill);
            return v >= 4 ? "Great fit" : v == 3 ? "Fair fit" : "Poor fit";
        }

        public static string Stars(int value) => new string('★', value) + new string('☆', 5 - value);

        /// <summary>Applies the result of a child's job minigame.</summary>
        public static ActionResult CompleteChildJob(RunData run, FamilyDatabase fam, ChildProfile c, MemberState m, LocationData loc,
            LocationAction a, float score, bool quit, float roll, GameConfig cfg, DifficultySettings diff)
        {
            var reason = CanAssign(run, c, m, loc, a, cfg, fam);
            if (reason != null) return new ActionResult { performed = false, message = reason, important = true, warnings = new List<string>() };

            var report = new AdvanceReport();
            run.MarkUsedToday(a.id, m.id);
            run.minigamesPlayed++;

            if (quit)
            {
                m.busyUntilAbs = run.AbsMinute + a.durationMinutes / 2;
                m.busyJob = a.name;
                SurvivalSystem.ApplyMemberDeltas(run, m, a.hunger * 0.5f, a.energy * 0.5f, 0, diff, fam, report);
                return new ActionResult
                {
                    performed = true, important = true, warnings = report.messages,
                    message = $"{c.displayName} gave up on \"{a.name}\" and came home with nothing."
                };
            }

            m.busyUntilAbs = run.AbsMinute + a.durationMinutes;
            m.busyJob = a.name;
            SurvivalSystem.ApplyMemberDeltas(run, m, a.hunger, a.energy, a.health, diff, fam, report);

            var pay = JobRules.Payout(a.wage, score, ChildPayMultiplier(c, a.skill, fam));
            run.money += pay;
            run.totalEarned += pay;
            if (pay > 0) run.childShiftsWorked++;

            var message = pay > 0
                ? $"{c.displayName} worked \"{a.name}\": {JobRules.Grade(score)} ({Mathf.RoundToInt(score * 100)}%) — earned ₱{pay}. Back at {GameTime.Clock(m.busyUntilAbs % GameTime.MinutesPerDay)}."
                : $"{c.displayName} did badly at \"{a.name}\" ({Mathf.RoundToInt(score * 100)}%) and wasn't paid.";
            var important = pay == 0;

            if (a.riskChance > 0 && roll < a.riskChance)
            {
                SurvivalSystem.ApplyMemberDeltas(run, m, 0, -Mathf.Abs(a.riskEnergy), -Mathf.Abs(a.riskHealth), diff, fam, report, a.riskCause);
                message += $" {c.displayName} got hurt on the job!";
                important = true;
            }
            return new ActionResult { performed = true, message = message, important = important, warnings = report.messages };
        }
    }
}
