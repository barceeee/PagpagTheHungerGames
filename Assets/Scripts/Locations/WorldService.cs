using System;
using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Jobs;
using Pagpag.Survival;
using Pagpag.TimeSystem;
using UnityEngine;

namespace Pagpag.Locations
{
    public struct TravelInfo
    {
        public bool available;
        public int minutes;
        public int fare;
        /// <summary>Energy the trip costs (already scaled by difficulty).</summary>
        public float energyCost;
    }

    public struct ActionResult
    {
        public bool performed;
        public string message;
        public bool important;
        public List<string> warnings;
    }

    /// <summary>
    /// Rules for moving around the map and doing things at locations.
    /// Pure logic on RunData + data assets; <see cref="GameManager"/> wraps it with events, minigames and saving.
    /// </summary>
    public static class WorldService
    {
        // ---------- Hours and unlocks ----------

        public static bool IsOpen(LocationData loc, int minuteOfDay) =>
            GameTime.InWindow(minuteOfDay / 60, loc.openHour, loc.closeHour);

        public static string HoursText(LocationData loc) =>
            loc.AlwaysOpen ? "Open 24 hours" : $"{GameTime.Hour(loc.openHour)} – {GameTime.Hour(loc.closeHour)}";

        public static bool IsUnlocked(RunData run, LocationData loc) =>
            !loc.unlock.lockedAtStart || run.unlockedLocations.Contains(loc.id);

        public static bool MeetsUnlock(RunData run, LocationData loc) =>
            run.day >= loc.unlock.minDay &&
            (string.IsNullOrEmpty(loc.unlock.requiredFlag) || run.HasFlag(loc.unlock.requiredFlag));

        /// <summary>Unlock every location whose condition is now met. Returns the newly unlocked ones.</summary>
        public static List<LocationData> RefreshUnlocks(RunData run, LocationDatabase db)
        {
            var unlocked = new List<LocationData>();
            foreach (var loc in db.locations)
            {
                if (loc == null || IsUnlocked(run, loc) || !MeetsUnlock(run, loc)) continue;
                run.unlockedLocations.Add(loc.id);
                unlocked.Add(loc);
            }
            return unlocked;
        }

        // ---------- Travel ----------

        public static float Distance(LocationData a, LocationData b, LocationDatabase db) =>
            Vector2.Distance(a.MapPoint, b.MapPoint) / db.mapSize.x;

        public static TravelInfo GetTravel(LocationData from, LocationData to, TravelMode mode, LocationDatabase db,
            GameConfig cfg, DifficultySettings diff)
        {
            var d = Distance(from, to, db);
            if (mode == TravelMode.Jeepney)
            {
                return new TravelInfo
                {
                    available = d >= cfg.jeepneyMinDistance,
                    minutes = RoundTo5(cfg.jeepneyBaseMinutes + d * cfg.jeepneyMinutesPerMapWidth),
                    fare = cfg.jeepneyFare,
                    energyCost = 1f * diff.fatigueRateMultiplier
                };
            }
            return new TravelInfo
            {
                available = true,
                minutes = RoundTo5(cfg.walkBaseMinutes + d * cfg.walkMinutesPerMapWidth),
                fare = 0,
                energyCost = d * cfg.walkEnergyPerMapWidth * diff.fatigueRateMultiplier
            };
        }

        public static ActionResult Travel(RunData run, LocationData to, TravelMode mode, LocationDatabase db,
            GameConfig cfg, DifficultySettings diff, FamilyDatabase fam = null)
        {
            var from = db.Get(run.currentLocationId);
            if (to == null || from == null) return Fail("Unknown destination.");
            if (to == from)
            {
                run.insideLocation = true;
                return new ActionResult { performed = true, message = $"You are at {to.displayName}.", warnings = new List<string>() };
            }
            if (!IsUnlocked(run, to)) return Fail($"{to.displayName} is locked. {to.unlock.hint}");

            var info = GetTravel(from, to, mode, db, cfg, diff);
            if (!info.available) return Fail("It's close enough to walk.");
            if (run.money < info.fare) return Fail($"You can't afford the ₱{info.fare} jeepney fare.");

            var before = SurvivalSystem.Snapshot(run);
            Spend(run, info.fare, SpendCategory.Transport);
            var report = SurvivalSystem.Advance(run, info.minutes, false, cfg, diff, fam);
            run.energy = SurvivalSystem.Clamp(run.energy - info.energyCost);
            var warnings = Merge(SurvivalSystem.WarningsSince(run, before, cfg), report);
            if (run.isDead) return new ActionResult { performed = true, message = "You collapse on the way.", important = true, warnings = warnings };

            run.currentLocationId = to.id;
            run.insideLocation = true;
            var how = mode == TravelMode.Jeepney ? $"Took a jeepney (₱{info.fare})" : "Walked";
            var msg = $"{how} to {to.displayName} — {GameTime.Duration(info.minutes)}.";
            if (!IsOpen(to, run.minuteOfDay)) msg += $" It's closed right now ({HoursText(to)}).";
            return new ActionResult { performed = true, message = msg, warnings = warnings };
        }

        // ---------- Actions ----------

        public static bool IsVisible(RunData run, LocationAction a)
        {
            if (!string.IsNullOrEmpty(a.character) && a.character != run.characterId) return false;
            if (!string.IsNullOrEmpty(a.hiddenIfFlag) && run.HasFlag(a.hiddenIfFlag)) return false;
            if (a.requiresSpouse && (run.Spouse == null || !run.Spouse.alive)) return false;
            if (a.kind == ActionKind.LeaveSlums && !run.infiniteMode) return false;
            if (a.kind == ActionKind.TakeMedicine && !IllnessRules.IsIll(run.playerIllness)) return false;
            return true;
        }

        /// <returns>null if the action can be started now, otherwise the reason it can't.</returns>
        public static string CanDo(RunData run, LocationData loc, LocationAction a, GameConfig cfg)
        {
            var fam = GameManager.Instance != null ? GameManager.Instance.Family : Resources.Load<FamilyDatabase>("FamilyDatabase");
            if (run.isDead) return "The run has ended.";
            if (!IsVisible(run, a)) return "Not available to you.";
            if (run.currentLocationId != loc.id) return "You are not here.";
            if (!IsOpen(loc, run.minuteOfDay)) return $"Closed. Hours: {HoursText(loc)}.";
            if (a.kind == ActionKind.LeaveSlums) return VictoryRules.LeaveBlockedReason(run, cfg);
            if (!GameTime.InWindow(run.minuteOfDay / 60, a.startHourFrom, a.startHourTo))
                return $"Only available {GameTime.Hour(a.startHourFrom)} – {GameTime.Hour(a.startHourTo)}.";
            if (!string.IsNullOrEmpty(a.requiresFlag) && !run.HasFlag(a.requiresFlag)) return "Not available to you yet.";
            if (a.oncePerDay && run.UsedToday(a.id)) return "Already done today. Come back tomorrow.";
            if (run.money < a.cost) return $"Not enough money (₱{a.cost}).";
            if ((a.kind == ActionKind.FamilyMeal || a.kind == ActionKind.PantryMeal) && run.PantryTotal <= 0)
                return "The pantry is empty. Buy food for the family first.";
            if (a.kind == ActionKind.TakeMedicine)
            {
                if (a.instantCure ? run.instantCures <= 0 : run.medicineDoses <= 0)
                    return a.instantCure ? "No instant cure at home. Buy one at the pharmacy." : "No medicine at home. Buy some at the pharmacy.";
                if (!IllnessRules.IsIll(run.playerIllness)) return "You aren't sick.";
            }
            if (a.IsWork)
            {
                if (IllnessRules.TooSickToWork(run.playerIllness, fam)) return "Too sick to work. Take medicine first.";
                if (run.energy < cfg.minEnergyToWork) return "Too exhausted to work. Rest first.";
                if (run.hunger < cfg.minHungerToWork) return "Too hungry to work. Eat first.";
            }
            return null;
        }

        /// <summary>Non-work actions. Jobs go through <see cref="CompleteJob"/>; intimacy through <see cref="ResolveIntimacy"/>.</summary>
        /// <param name="roll">Random value in [0,1) used for the action's risk.</param>
        public static ActionResult Perform(RunData run, LocationData loc, LocationAction a, float roll,
            GameConfig cfg, DifficultySettings diff, FamilyDatabase fam = null, Func<float> rng = null)
        {
            if (a.IsWork) return Fail("Jobs are done by playing their minigame.");
            if (a.kind == ActionKind.Intimacy) return Fail("This happens through the evening event.");
            if (a.kind == ActionKind.LeaveSlums) return Fail("Leaving the slums ends the run; it is confirmed first.");
            var reason = CanDo(run, loc, a, cfg);
            if (reason != null) return Fail(reason);
            rng ??= () => UnityEngine.Random.value;

            var before = SurvivalSystem.Snapshot(run);
            Spend(run, a.cost, a.IsFood ? SpendCategory.Food : SpendCategory.Other);
            if (a.oncePerDay) run.MarkUsedToday(a.id);

            var report = SurvivalSystem.Advance(run, a.durationMinutes, a.kind == ActionKind.Sleep, cfg, diff, fam, rng);
            if (run.isDead)
                return new ActionResult { performed = true, message = "You never finish. Your body gives out.", important = true, warnings = report.messages };

            var message = a.resultMessage;
            switch (a.kind)
            {
                case ActionKind.Provision:
                    if (a.pagpagServings) run.pantryPagpag += a.servings;
                    else run.pantrySafe += a.servings;
                    message = $"{a.resultMessage} (+{a.servings} servings · pantry {run.PantryTotal})";
                    break;
                case ActionKind.FamilyMeal:
                    message = fam != null ? FamilyService.ServeFamilyMeal(run, fam, diff, rng, report) : a.resultMessage;
                    break;
                case ActionKind.PantryMeal:
                    if (fam != null && FamilyService.FeedPlayer(run, fam, diff, rng, report.messages))
                        message = $"{a.resultMessage} Pantry: {run.PantryTotal} left.";
                    break;
                case ActionKind.Eat:
                    run.mealsEaten++;
                    break;
                case ActionKind.BuyMedicine:
                    if (a.instantCure) run.instantCures++;
                    else run.medicineDoses += Mathf.Max(1, a.servings);
                    message = $"{a.resultMessage} (Medicine at home: {run.medicineDoses} dose(s), {run.instantCures} instant cure(s))";
                    break;
                case ActionKind.TakeMedicine:
                    message = TakeMedicine(run, a, fam);
                    break;
                case ActionKind.Consult:
                    message = $"{a.resultMessage} {IllnessRules.Diagnosis(run, fam)}";
                    break;
            }
            var pagpagNotes = new List<string>();
            if (a.pagpagFood && fam != null) FamilyService.PlayerAtePagpag(run, fam, rng, pagpagNotes);
            report.messages.AddRange(pagpagNotes);

            SurvivalSystem.ApplyDeltas(run, a.hunger, a.energy, a.health, diff);
            var missingNote = "";
            if (a.kind == ActionKind.Sleep)
            {
                // Someone takes money while the player sleeps. The player is only told it's missing.
                var taken = FamilyService.SleepTheft(run, a.durationMinutes, rng());
                if (taken > 0)
                {
                    run.money -= taken;
                    run.moneyMissing += taken;
                    missingNote = $" When you wake up, ₱{taken} is missing from your money.";
                }
            }
            if (a.kind == ActionKind.Talk && a.requiresSpouse && run.Spouse != null)
                SurvivalSystem.ApplyMemberDeltas(run, run.Spouse, 0, Mathf.Max(0, a.energy), Mathf.Max(0, a.health), diff, fam, report);
            run.SetFlag(a.setsFlag);
            message = ContactMessage(run, a, message) + missingNote;
            var important = false;

            if (a.riskChance > 0 && roll < a.riskChance)
            {
                SurvivalSystem.ApplyDeltas(run, 0, -Mathf.Abs(a.riskEnergy), -Mathf.Abs(a.riskHealth), diff, a.riskCause);
                message += " " + a.riskMessage;
                important = true;
                if (run.isDead) message += " It was too much for your body.";
            }

            var warnings = Merge(SurvivalSystem.WarningsSince(run, before, cfg), report);
            return new ActionResult { performed = true, message = message.Trim(), important = important, warnings = warnings };
        }

        private static string TakeMedicine(RunData run, LocationAction a, FamilyDatabase fam)
        {
            var name = IllnessRules.Name(run.playerIllness, fam)?.ToLowerInvariant();
            if (a.instantCure)
            {
                run.instantCures--;
                IllnessRules.Clear(run.playerIllness);
                return $"The instant cure works within the hour. The {name} is gone.";
            }
            run.medicineDoses--;
            IllnessRules.GiveDose(run.playerIllness, run.AbsMinute, fam);
            return $"You take a dose. For the next day the {name} will ease instead of getting worse. " +
                   $"About {IllnessRules.DosesToCure(run.playerIllness, fam)} dose(s) to recover. ({run.medicineDoses} left at home)";
        }

        /// <summary>
        /// Applies a finished (or abandoned) job minigame for the player. Pay depends on performance;
        /// walking away pays nothing, still uses the day's shift and half the time.
        /// </summary>
        public static ActionResult CompleteJob(RunData run, LocationData loc, LocationAction a, float score, bool quit, float roll,
            GameConfig cfg, DifficultySettings diff, FamilyDatabase fam = null)
        {
            if (!a.IsWork) return Fail("Not a job.");
            var reason = CanDo(run, loc, a, cfg);
            if (reason != null) return Fail(reason);

            var before = SurvivalSystem.Snapshot(run);
            if (a.oncePerDay) run.MarkUsedToday(a.id);
            run.minigamesPlayed++;

            if (quit)
            {
                var halfReport = SurvivalSystem.Advance(run, a.durationMinutes / 2, false, cfg, diff, fam);
                SurvivalSystem.ApplyDeltas(run, a.hunger * 0.5f, a.energy * 0.5f, 0, diff);
                return new ActionResult
                {
                    performed = true, important = true,
                    message = $"You walked off the job at {loc.displayName}. No pay.",
                    warnings = Merge(SurvivalSystem.WarningsSince(run, before, cfg), halfReport)
                };
            }

            var report = SurvivalSystem.Advance(run, a.durationMinutes, false, cfg, diff, fam);
            if (run.isDead)
                return new ActionResult { performed = true, message = "You never finish the shift. Your body gives out.", important = true, warnings = report.messages };

            SurvivalSystem.ApplyDeltas(run, a.hunger, a.energy, a.health, diff);
            var pay = JobRules.Payout(a.wage, score, JobRules.PlayerPayMultiplier(run, fam));
            run.money += pay;
            run.totalEarned += pay;
            if (pay > 0)
            {
                run.shiftsWorked++;
                run.SetFlag(a.setsFlag);
            }

            var message = pay > 0
                ? $"{ContactMessage(run, a, a.resultMessage)} {JobRules.Grade(score)} work ({Mathf.RoundToInt(score * 100)}%): +₱{pay}."
                : $"You did badly ({Mathf.RoundToInt(score * 100)}%). The boss refuses to pay.";
            if (pay > 0 && run.IsDepressed) message += " (Grief: reduced pay)";
            var important = pay == 0;

            if (a.riskChance > 0 && roll < a.riskChance)
            {
                SurvivalSystem.ApplyDeltas(run, 0, -Mathf.Abs(a.riskEnergy), -Mathf.Abs(a.riskHealth), diff, a.riskCause);
                message += " " + a.riskMessage;
                important = true;
                if (run.isDead) message += " It was too much for your body.";
            }

            var warnings = Merge(SurvivalSystem.WarningsSince(run, before, cfg), report);
            return new ActionResult { performed = true, message = message.Trim(), important = important, warnings = warnings };
        }

        /// <summary>
        /// Result of the spin-the-wheel fertility event. If Inday becomes pregnant the run is lost
        /// (whether the player is Dong or Inday).
        /// </summary>
        public static ActionResult ResolveIntimacy(RunData run, LocationData loc, LocationAction a, bool pregnant,
            GameConfig cfg, DifficultySettings diff, FamilyDatabase fam = null)
        {
            var reason = CanDo(run, loc, a, cfg);
            if (reason != null) return Fail(reason);
            run.MarkUsedToday(a.id);
            var report = SurvivalSystem.Advance(run, a.durationMinutes, false, cfg, diff, fam);
            if (run.isDead) return new ActionResult { performed = true, message = "", warnings = report.messages };

            if (pregnant)
            {
                run.isDead = true; // the run is over (see endKind)
                run.endKind = EndKind.Pregnancy;
                run.causeOfDeath = run.gender == Gender.Female ? "You are pregnant" : "Inday is pregnant";
                return new ActionResult
                {
                    performed = true, important = true, warnings = report.messages,
                    message = run.gender == Gender.Female
                        ? "Weeks later, the test shows two lines. You are pregnant with a sixth child."
                        : "Weeks later, Inday shows you the test. Two lines. A sixth child is coming."
                };
            }

            SurvivalSystem.ApplyDeltas(run, a.hunger, a.energy, a.health, diff);
            if (run.Spouse != null) SurvivalSystem.ApplyMemberDeltas(run, run.Spouse, 0, a.energy, a.health, diff, fam, report);
            return new ActionResult { performed = true, message = a.resultMessage, warnings = report.messages };
        }

        /// <summary>One-line summary of an action's price, time and effects for the UI.</summary>
        public static string Describe(LocationAction a, GameConfig cfg, DifficultySettings diff)
        {
            var parts = new List<string>();
            if (a.cost > 0 && a.kind != ActionKind.BuyMedicine) parts.Add($"₱{a.cost}");
            if (a.IsWork) parts.Add($"Pay up to ₱{JobRules.Payout(a.wage, 1f)} (minigame)");
            if (a.kind == ActionKind.Provision) parts.Add($"+{a.servings} servings{(a.pagpagServings ? " (pagpag)" : "")}");
            if (a.kind == ActionKind.LeaveSlums) parts.Add($"Needs ₱{cfg.leaveSlumsQuota:N0} saved  ·  Ends the run");
            else if (a.kind == ActionKind.BuyMedicine)
            {
                parts.Add($"₱{a.cost:N0}");
                parts.Add(a.instantCure ? "Cures any illness at once" : $"{Mathf.Max(1, a.servings)} dose(s) for the home cabinet");
            }
            else if (a.kind == ActionKind.TakeMedicine) parts.Add(a.instantCure ? "Uses 1 instant cure" : "Uses 1 dose");
            else if (a.cost == 0 && a.wage == 0 && a.kind != ActionKind.Provision) parts.Add("Free");
            if (a.durationMinutes > 0) parts.Add(GameTime.Duration(a.durationMinutes));
            AddStat(parts, "Hunger", SurvivalSystem.ScaledHunger(a.hunger, diff));
            AddStat(parts, "Energy", a.kind == ActionKind.Sleep
                ? a.durationMinutes / 60f * cfg.energyRecoveryPerHourAsleep
                : SurvivalSystem.ScaledEnergy(a.energy, diff));
            AddStat(parts, "Health", a.health);
            if (a.riskChance > 0) parts.Add($"Risk {Mathf.RoundToInt(a.riskChance * 100)}%");
            if (a.oncePerDay) parts.Add("Once a day");
            return string.Join("  ·  ", parts);
        }

        // ---------- Helpers ----------

        public enum SpendCategory { Food, Transport, Other }

        public static void Spend(RunData run, int amount, SpendCategory category)
        {
            if (amount <= 0) return;
            run.money -= amount;
            run.totalSpent += amount;
            switch (category)
            {
                case SpendCategory.Food: run.foodSpent += amount; break;
                case SpendCategory.Transport: run.transportSpent += amount; break;
                default: run.otherSpent += amount; break;
            }
        }

        private static string ContactMessage(RunData run, LocationAction a, string fallback)
        {
            var useContact = !string.IsNullOrEmpty(a.contactFlag) && run.HasFlag(a.contactFlag) && !string.IsNullOrEmpty(a.contactMessage);
            return useContact ? a.contactMessage : fallback;
        }

        private static List<string> Merge(List<string> warnings, AdvanceReport report)
        {
            if (report != null) warnings.AddRange(report.messages);
            return warnings;
        }

        private static void AddStat(List<string> parts, string label, float value)
        {
            var v = Mathf.RoundToInt(value);
            if (v != 0) parts.Add($"{label} {(v > 0 ? "+" : "−")}{Mathf.Abs(v)}");
        }

        private static int RoundTo5(float minutes) => Mathf.Max(5, Mathf.RoundToInt(minutes / 5f) * 5);

        private static ActionResult Fail(string reason) =>
            new ActionResult { performed = false, message = reason, important = true, warnings = new List<string>() };
    }
}
