using System;
using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.TimeSystem;
using UnityEngine;

namespace Pagpag.Survival
{
    /// <summary>What happened while time passed: warnings, meals, deaths.</summary>
    public class AdvanceReport
    {
        public readonly List<string> messages = new List<string>();
        public readonly List<string> deaths = new List<string>();
    }

    /// <summary>
    /// Advances the clock and applies hunger, energy and health for the player and every family member.
    /// All stats are 0–100 where 100 is best. Difficulty scales hunger/energy drain and the damage they
    /// cause (starvation and exhaustion); all other rules are the same in every mode.
    /// Grief (a child's death) makes the player's stats drain faster and recover slower for a week.
    /// Pure logic on <see cref="RunData"/> so it can be unit-tested.
    /// </summary>
    public static class SurvivalSystem
    {
        public const float Max = 100f;
        private const int StepMinutes = 10;

        public const string CauseStarvation = "Starvation";
        public const string CauseExhaustion = "Extreme exhaustion";
        public const string CauseFoodPoisoning = "Food poisoning";

        /// <summary>Drains (negative) are scaled by difficulty; restores (positive) are not.</summary>
        public static float ScaledHunger(float delta, DifficultySettings d) => delta < 0 ? delta * d.hungerRateMultiplier : delta;
        public static float ScaledEnergy(float delta, DifficultySettings d) => delta < 0 ? delta * d.fatigueRateMultiplier : delta;

        public static float DepressionDrain(RunData run, FamilyDatabase fam) =>
            run.IsDepressed && fam != null ? fam.depressionDrainMultiplier : 1f;

        public static float DepressionRecovery(RunData run, FamilyDatabase fam) =>
            run.IsDepressed && fam != null ? fam.depressionRecoveryMultiplier : 1f;

        // =====================================================================
        // Time
        // =====================================================================

        /// <summary>Pass time for everyone. Stops early if the player dies.</summary>
        public static AdvanceReport Advance(RunData run, int minutes, bool playerAsleep, GameConfig cfg, DifficultySettings diff,
            FamilyDatabase fam, Func<float> rng = null, AdvanceReport report = null)
        {
            report ??= new AdvanceReport();
            if (run.isDead || minutes <= 0) return report;
            rng ??= () => UnityEngine.Random.value;

            var before = SnapshotFamily(run);
            var remaining = minutes;
            while (remaining > 0 && !run.isDead)
            {
                var step = Mathf.Min(StepMinutes, remaining);
                remaining -= step;
                var hours = step / 60f;
                var startAbs = run.AbsMinute;

                TickPlayer(run, hours, playerAsleep, cfg, diff, fam, report);
                if (fam != null)
                    foreach (var m in run.family)
                        if (m.alive) TickMember(run, m, hours, cfg, diff, fam, report);

                AdvanceClock(run, step);
                if (fam != null)
                {
                    RunAutoMeals(run, startAbs, run.AbsMinute, fam, rng, report);
                    RunDailyIllness(run, startAbs, run.AbsMinute, cfg, fam, rng, report);
                }
            }
            if (fam != null) MemberWarnings(run, before, cfg, report);
            return report;
        }

        private static void TickPlayer(RunData run, float hours, bool asleep, GameConfig cfg, DifficultySettings diff, FamilyDatabase fam,
            AdvanceReport report = null)
        {
            var drain = DepressionDrain(run, fam);
            var recovery = DepressionRecovery(run, fam);
            var hungerRate = asleep ? cfg.hungerDrainPerHourAsleep : cfg.hungerDrainPerHourAwake;
            run.hunger = Clamp(run.hunger - hungerRate * diff.hungerRateMultiplier * drain * hours);
            run.energy = asleep
                ? Clamp(run.energy + cfg.energyRecoveryPerHourAsleep * recovery * hours)
                : Clamp(run.energy - cfg.energyDrainPerHourAwake * diff.fatigueRateMultiplier * drain * hours);

            string source = null;
            var healthBefore = run.health;
            run.health = HealthTick(run.hunger, run.energy, run.health, hours, asleep, cfg, diff, 1f, recovery, ref source);
            ApplyIllness(run.playerIllness, "You", true, run.AbsMinute, hours, fam, 1f, healthBefore,
                ref run.health, ref run.energy, ref source, report);
            if (source != null) run.lastDamageSource = source;
            CheckDeath(run);
        }

        /// <summary>
        /// Illness on top of the normal health rule: no healing while sick, extra damage and fatigue, and the
        /// messages when it worsens or clears. Starvation and exhaustion stay the named cause when they also hurt.
        /// </summary>
        private static void ApplyIllness(IllnessState ill, string who, bool isPlayer, int absMinute, float hours, FamilyDatabase fam,
            float frailty, float healthBefore, ref float health, ref float energy, ref string source, AdvanceReport report)
        {
            if (fam == null || !IllnessRules.IsIll(ill)) return;
            var t = IllnessRules.Tick(ill, hours, absMinute, fam, frailty);
            if (t.cured)
            {
                report?.messages.Add(isPlayer ? $"You have recovered from {t.illnessName.ToLowerInvariant()}." : $"{who} has recovered from {t.illnessName.ToLowerInvariant()}.");
                return;
            }
            if (health > healthBefore) health = healthBefore; // the sick don't heal
            health = Clamp(health - t.damage);
            energy = Clamp(energy - t.energyDrain);
            if (t.damage > 0f) source ??= t.illnessName;
            if (t.worsenedTo != null)
                report?.messages.Add(isPlayer
                    ? $"Your {t.illnessName.ToLowerInvariant()} is getting worse ({t.worsenedTo}). Get medicine from the pharmacy."
                    : $"{who}'s {t.illnessName.ToLowerInvariant()} is getting worse ({t.worsenedTo}). Give them medicine.");
        }

        /// <summary>Once a day (at the check hour) anyone may come down with a random illness; the weak more often.</summary>
        private static void RunDailyIllness(RunData run, int fromAbs, int toAbs, GameConfig cfg, FamilyDatabase fam, Func<float> rng, AdvanceReport report)
        {
            var settings = IllnessRules.Settings(fam);
            if (settings == null) return;
            var minute = settings.dailyCheckHour * 60;
            for (var d = fromAbs / GameTime.MinutesPerDay; d <= toAbs / GameTime.MinutesPerDay; d++)
            {
                var abs = d * GameTime.MinutesPerDay + minute;
                if (abs <= fromAbs || abs > toAbs) continue;
                if (run.isDead) return;
                var weak = run.hunger <= cfg.hungerWarning || run.energy <= cfg.energyWarning;
                var caught = IllnessRules.RollDaily(run.playerIllness, weak, rng(), rng(), fam);
                if (caught != null) report.messages.Add($"You have come down with {caught.displayName.ToLowerInvariant()}. {caught.symptoms} It will get worse without medicine.");
                foreach (var m in run.family)
                {
                    if (!m.alive) continue;
                    weak = m.hunger <= cfg.hungerWarning || m.energy <= cfg.energyWarning;
                    caught = IllnessRules.RollDaily(m.illness, weak, rng(), rng(), fam);
                    if (caught != null) report.messages.Add($"{m.name} has come down with {caught.displayName.ToLowerInvariant()}. {caught.symptoms} Give them medicine before it gets worse.");
                }
            }
        }

        private static void TickMember(RunData run, MemberState m, float hours, GameConfig cfg, DifficultySettings diff,
            FamilyDatabase fam, AdvanceReport report)
        {
            Body(m, fam, out var appetite, out var tireRate, out var frailty);
            var asleep = IsAsleep(run, m, cfg);
            var hungerRate = asleep ? cfg.hungerDrainPerHourAsleep : cfg.hungerDrainPerHourAwake;
            m.hunger = Clamp(m.hunger - hungerRate * appetite * diff.hungerRateMultiplier * hours);
            m.energy = asleep
                ? Clamp(m.energy + cfg.energyRecoveryPerHourAsleep * hours)
                : Clamp(m.energy - cfg.energyDrainPerHourAwake * tireRate * diff.fatigueRateMultiplier * hours);

            string source = null;
            var healthBefore = m.health;
            m.health = HealthTick(m.hunger, m.energy, m.health, hours, asleep, cfg, diff, frailty, 1f, ref source);
            ApplyIllness(m.illness, m.name, false, run.AbsMinute, hours, fam, frailty, healthBefore,
                ref m.health, ref m.energy, ref source, report);
            if (source != null) m.lastDamageSource = source;
            CheckMemberDeath(run, m, fam, report);
        }

        /// <summary>Shared health rule for any body. Returns the new health.</summary>
        private static float HealthTick(float hunger, float energy, float health, float hours, bool asleep, GameConfig cfg,
            DifficultySettings diff, float frailty, float recovery, ref string source)
        {
            var damage = 0f;
            if (hunger <= 0f) { damage += cfg.severeDamagePerHour * diff.hungerRateMultiplier; source = CauseStarvation; }
            else if (hunger <= cfg.hungerDamageThreshold) { damage += cfg.minorDamagePerHour * diff.hungerRateMultiplier; source = CauseStarvation; }

            if (energy <= 0f) { damage += cfg.severeDamagePerHour * diff.fatigueRateMultiplier; source ??= CauseExhaustion; }
            else if (energy <= cfg.energyDamageThreshold) { damage += cfg.minorDamagePerHour * diff.fatigueRateMultiplier; source ??= CauseExhaustion; }

            if (damage > 0) return Clamp(health - damage * frailty * hours);
            if (hunger >= cfg.regenMinHunger && energy >= cfg.regenMinEnergy)
                return Clamp(health + (asleep ? cfg.healthRegenPerHourAsleep : cfg.healthRegenPerHourAwake) * recovery / frailty * hours);
            return health;
        }

        // =====================================================================
        // Instant changes
        // =====================================================================

        /// <summary>Instant change to the player from an action (meal, work, injury). Drains are scaled by difficulty.</summary>
        public static void ApplyDeltas(RunData run, float hunger, float energy, float health, DifficultySettings diff, string damageSource = null)
        {
            run.hunger = Clamp(run.hunger + ScaledHunger(hunger, diff));
            run.energy = Clamp(run.energy + ScaledEnergy(energy, diff));
            if (health < 0 && !string.IsNullOrEmpty(damageSource)) run.lastDamageSource = damageSource;
            run.health = Clamp(run.health + health);
            CheckDeath(run);
        }

        /// <summary>Instant change to a family member (meal, child job, sickness). Drains scaled by difficulty and the member's body.</summary>
        public static void ApplyMemberDeltas(RunData run, MemberState m, float hunger, float energy, float health, DifficultySettings diff,
            FamilyDatabase fam, AdvanceReport report, string damageSource = null)
        {
            if (!m.alive) return;
            Body(m, fam, out var appetite, out var tireRate, out var frailty);
            m.hunger = Clamp(m.hunger + (hunger < 0 ? ScaledHunger(hunger, diff) * appetite : hunger));
            m.energy = Clamp(m.energy + (energy < 0 ? ScaledEnergy(energy, diff) * tireRate : energy));
            if (health < 0)
            {
                if (!string.IsNullOrEmpty(damageSource)) m.lastDamageSource = damageSource;
                m.health = Clamp(m.health + health * frailty);
            }
            else m.health = Clamp(m.health + health);
            CheckMemberDeath(run, m, fam, report);
        }

        // =====================================================================
        // Death and grief
        // =====================================================================

        public static void CheckDeath(RunData run)
        {
            if (run.isDead || run.health > 0f) return;
            run.health = 0f;
            run.isDead = true;
            run.endKind = EndKind.Death;
            run.causeOfDeath = string.IsNullOrEmpty(run.lastDamageSource) ? CauseStarvation : run.lastDamageSource;
        }

        private static void CheckMemberDeath(RunData run, MemberState m, FamilyDatabase fam, AdvanceReport report)
        {
            if (!m.alive || m.health > 0f) return;
            m.health = 0f;
            m.alive = false;
            m.busyUntilAbs = -1;
            m.diedDay = run.day;
            m.causeOfDeath = string.IsNullOrEmpty(m.lastDamageSource) ? CauseStarvation : m.lastDamageSource;
            report?.deaths.Add(m.name);
            if (m.isSpouse)
            {
                report?.messages.Add($"{m.name} has died ({m.causeOfDeath.ToLowerInvariant()}). The children have only you now.");
            }
            else
            {
                run.childDeaths++;
                report?.messages.Add($"{m.name} has died ({m.causeOfDeath.ToLowerInvariant()}). Nothing will ever be the same.");
            }
            StartDepression(run, m.name, fam);
        }

        /// <summary>A death in the family: one full in-game week of depression (refreshed by another death).</summary>
        public static void StartDepression(RunData run, string cause, FamilyDatabase fam)
        {
            var days = fam != null ? fam.depressionDays : 7;
            run.depressionEndAbs = Mathf.Max(run.depressionEndAbs, run.AbsMinute + days * GameTime.MinutesPerDay);
            run.depressionCause = cause;
        }

        // =====================================================================
        // Family helpers
        // =====================================================================

        public static void Body(MemberState m, FamilyDatabase fam, out float appetite, out float tireRate, out float frailty)
        {
            if (m.isSpouse || fam == null)
            {
                appetite = fam != null ? fam.spouseAppetite : 1f;
                tireRate = fam != null ? fam.spouseTireRate : 1f;
                frailty = fam != null ? fam.spouseFrailty : 1f;
                return;
            }
            var c = fam.Get(m.id);
            appetite = c != null ? c.appetite : 1f;
            tireRate = c != null ? c.tireRate : 1f;
            frailty = c != null ? c.frailty : 1f;
        }

        public static bool IsBusy(RunData run, MemberState m) => m.alive && m.busyUntilAbs > run.AbsMinute;

        public static bool IsAsleep(RunData run, MemberState m, GameConfig cfg) =>
            m.alive && !IsBusy(run, m) && GameTime.InWindow(run.minuteOfDay / 60, cfg.familyBedHour, cfg.familyWakeHour);

        private static void RunAutoMeals(RunData run, int fromAbs, int toAbs, FamilyDatabase fam, Func<float> rng, AdvanceReport report)
        {
            foreach (var hour in fam.autoMealHours)
            {
                // Did we cross hour:00 of any day between fromAbs (exclusive) and toAbs (inclusive)?
                var mealMinute = hour * 60;
                var firstDay = fromAbs / GameTime.MinutesPerDay;
                var lastDay = toAbs / GameTime.MinutesPerDay;
                for (var d = firstDay; d <= lastDay; d++)
                {
                    var abs = d * GameTime.MinutesPerDay + mealMinute;
                    if (abs > fromAbs && abs <= toAbs) FamilyService.AutoMeal(run, fam, rng, report, hour);
                }
            }
        }

        private static Dictionary<string, (float h, float e, float hp)> SnapshotFamily(RunData run)
        {
            var d = new Dictionary<string, (float, float, float)>();
            foreach (var m in run.family) d[m.id] = (m.hunger, m.energy, m.health);
            return d;
        }

        private static void MemberWarnings(RunData run, Dictionary<string, (float h, float e, float hp)> before, GameConfig cfg, AdvanceReport report)
        {
            foreach (var m in run.family)
            {
                if (!m.alive || !before.TryGetValue(m.id, out var b)) continue;
                if (b.h > cfg.hungerWarning && m.hunger <= cfg.hungerWarning) report.messages.Add($"{m.name} is very hungry.");
                if (b.e > cfg.energyWarning && m.energy <= cfg.energyWarning) report.messages.Add($"{m.name} is exhausted.");
                if (b.hp > cfg.healthWarning && m.health <= cfg.healthWarning) report.messages.Add($"{m.name}'s health is critical!");
            }
        }

        private static void AdvanceClock(RunData run, int minutes)
        {
            run.minuteOfDay += minutes;
            while (run.minuteOfDay >= GameTime.MinutesPerDay)
            {
                run.minuteOfDay -= GameTime.MinutesPerDay;
                run.day++;
            }
        }

        // =====================================================================
        // Player warnings
        // =====================================================================

        public struct Stats { public float hunger, energy, health; }

        public static Stats Snapshot(RunData r) => new Stats { hunger = r.hunger, energy = r.energy, health = r.health };

        /// <summary>Warnings for critical thresholds crossed since <paramref name="before"/>.</summary>
        public static List<string> WarningsSince(RunData run, Stats before, GameConfig cfg)
        {
            var warnings = new List<string>();
            if (run.isDead) return warnings;
            if (before.hunger > 0 && run.hunger <= 0)
                warnings.Add("You are starving. Your health is falling fast — eat something now.");
            else if (before.hunger > cfg.hungerWarning && run.hunger <= cfg.hungerWarning)
                warnings.Add("Your stomach is cramping. You need to eat soon.");

            if (before.energy > 0 && run.energy <= 0)
                warnings.Add("You are collapsing from exhaustion. Rest immediately.");
            else if (before.energy > cfg.energyWarning && run.energy <= cfg.energyWarning)
                warnings.Add("You are exhausted. Go home and sleep.");

            if (before.health > cfg.healthWarning && run.health <= cfg.healthWarning)
                warnings.Add("Your health is critical. One more bad day could be your last.");
            return warnings;
        }

        public static float Clamp(float v) => Mathf.Clamp(v, 0f, Max);
    }
}
