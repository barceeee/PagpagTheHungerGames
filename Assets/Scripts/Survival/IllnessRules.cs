using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Family;
using UnityEngine;

namespace Pagpag.Survival
{
    /// <summary>
    /// Illness for the player, the spouse and the children. Pure logic on <see cref="IllnessState"/>.
    /// Sources: pagpag eaten day after day (typhoid), and a small random daily chance (flu, dengue) that is higher
    /// for anyone starving or exhausted. Untreated, severity only rises and health falls until the person dies.
    /// </summary>
    public static class IllnessRules
    {
        public const float SeriousAt = 35f, CriticalAt = 70f;

        public static IllnessSettings Settings(FamilyDatabase fam) => fam != null ? fam.illness : null;

        public static bool IsIll(IllnessState s) => s != null && !string.IsNullOrEmpty(s.id);

        public static bool IsTreated(IllnessState s, int absMinute) => IsIll(s) && s.treatedUntilAbs > absMinute;

        public static string Stage(float severity) => severity < SeriousAt ? "mild" : severity < CriticalAt ? "serious" : "critical";

        public static string Name(IllnessState s, FamilyDatabase fam) =>
            IsIll(s) ? Settings(fam)?.Get(s.id)?.displayName ?? s.id : null;

        /// <summary>"Flu (serious, on medicine)", or null if healthy.</summary>
        public static string Describe(IllnessState s, int absMinute, FamilyDatabase fam) =>
            IsIll(s) ? $"{Name(s, fam)} ({Stage(s.severity)}{(IsTreated(s, absMinute) ? ", on medicine" : "")})" : null;

        public static bool TooSickToWork(IllnessState s, FamilyDatabase fam) =>
            IsIll(s) && Settings(fam) != null && s.severity >= Settings(fam).workBlockSeverity;

        public static bool Infect(IllnessState s, IllnessDefinition def, IllnessSettings cfg)
        {
            if (s == null || def == null || IsIll(s)) return false;
            s.id = def.id;
            s.severity = cfg.onsetSeverity;
            s.treatedUntilAbs = -1;
            return true;
        }

        public static void Clear(IllnessState s)
        {
            s.id = null;
            s.severity = 0f;
            s.treatedUntilAbs = -1;
        }

        // ---------- Time ----------

        /// <summary>What one tick of illness did.</summary>
        public struct TickResult
        {
            public float damage, energyDrain;
            public bool cured;
            /// <summary>The stage the illness just worsened into, if it crossed one.</summary>
            public string worsenedTo;
            public string illnessName;
        }

        public static TickResult Tick(IllnessState s, float hours, int absMinute, FamilyDatabase fam, float frailty)
        {
            var r = new TickResult();
            var cfg = Settings(fam);
            if (!IsIll(s) || cfg == null) return r;
            var def = cfg.Get(s.id);
            if (def == null) { Clear(s); return r; }
            r.illnessName = def.displayName;

            var before = s.severity;
            var treated = IsTreated(s, absMinute);
            s.severity = Mathf.Clamp(s.severity + (treated ? -cfg.treatedRecoveryPerDay : def.worsenPerDay) / 24f * hours, 0f, 100f);
            if (s.severity <= 0f)
            {
                Clear(s);
                r.cured = true;
                return r;
            }
            if (!treated && Stage(before) != Stage(s.severity)) r.worsenedTo = Stage(s.severity);

            var dmg = cfg.damagePerHourAtMax * def.damageScale * s.severity / 100f * frailty * hours;
            r.damage = treated ? dmg * cfg.treatedDamageMultiplier : dmg;
            r.energyDrain = cfg.energyDrainPerHourAtMax * s.severity / 100f * hours;
            return r;
        }

        /// <summary>Daily roll for a random illness (not typhoid). Returns the illness caught, or null.</summary>
        public static IllnessDefinition RollDaily(IllnessState s, bool weak, float roll, float pick, FamilyDatabase fam)
        {
            var cfg = Settings(fam);
            if (cfg == null || IsIll(s)) return null;
            var chance = cfg.randomChancePerDay + (weak ? cfg.weakExtraChancePerDay : 0f);
            if (roll >= chance) return null;
            var total = 0f;
            foreach (var d in cfg.illnesses) total += Mathf.Max(0f, d.randomWeight);
            if (total <= 0f) return null;
            var target = pick * total;
            foreach (var d in cfg.illnesses)
            {
                var w = Mathf.Max(0f, d.randomWeight);
                if (w <= 0f) continue;
                if (target < w) return Infect(s, d, cfg) ? d : null;
                target -= w;
            }
            return null;
        }

        // ---------- Pagpag ----------

        /// <summary>Days in a row this person has eaten pagpag, counting today if they eat it now.</summary>
        public static int StreakIfEatenOn(IllnessState s, int day) =>
            s.lastPagpagDay == day ? s.pagpagStreak : s.lastPagpagDay == day - 1 ? s.pagpagStreak + 1 : 1;

        /// <summary>The current streak (0 if the person didn't eat pagpag yesterday or today).</summary>
        public static int CurrentStreak(IllnessState s, int day) => s.lastPagpagDay >= day - 1 ? s.pagpagStreak : 0;

        public static float PagpagChance(IllnessState s, int day, FamilyDatabase fam)
        {
            var cfg = Settings(fam);
            if (cfg == null) return 0f;
            return Mathf.Min(cfg.pagpagMaxChance, cfg.pagpagBaseChance + cfg.pagpagPerStreakDay * (StreakIfEatenOn(s, day) - 1));
        }

        /// <summary>Someone ate a pagpag meal. Updates their streak; returns true if they caught the pagpag illness.</summary>
        public static bool AtePagpag(IllnessState s, int day, float roll, FamilyDatabase fam)
        {
            var cfg = Settings(fam);
            if (s == null || cfg == null) return false;
            var chance = PagpagChance(s, day, fam);
            s.pagpagStreak = StreakIfEatenOn(s, day);
            s.lastPagpagDay = day;
            return roll < chance && Infect(s, cfg.Get(cfg.pagpagIllnessId), cfg);
        }

        // ---------- Medicine ----------

        /// <summary>One dose: the illness reverses for <see cref="IllnessSettings.doseHours"/>. Doses stack.</summary>
        public static void GiveDose(IllnessState s, int absMinute, FamilyDatabase fam)
        {
            var hours = Settings(fam)?.doseHours ?? 24;
            s.treatedUntilAbs = Mathf.Max(s.treatedUntilAbs, absMinute) + hours * 60;
        }

        /// <summary>Doses needed to cure, if each dose runs its full time.</summary>
        public static int DosesToCure(IllnessState s, FamilyDatabase fam)
        {
            var cfg = Settings(fam);
            if (!IsIll(s) || cfg == null) return 0;
            var perDose = cfg.treatedRecoveryPerDay * cfg.doseHours / 24f;
            return Mathf.CeilToInt(s.severity / perDose);
        }

        // ---------- Whole household ----------

        /// <summary>Everyone in the run with their illness state, the player first.</summary>
        public static IEnumerable<(string name, IllnessState state, MemberState member)> Bodies(RunData run)
        {
            yield return ("You", run.playerIllness, null);
            foreach (var m in run.family)
                if (m.alive) yield return (m.name, m.illness, m);
        }

        /// <summary>The pharmacist's read of the household: who is sick and what they need.</summary>
        public static string Diagnosis(RunData run, FamilyDatabase fam)
        {
            var lines = new List<string>();
            foreach (var (name, state, _) in Bodies(run))
            {
                if (!IsIll(state)) continue;
                var who = name == "You" ? "You have" : $"{name} has";
                lines.Add($"{who} {Describe(state, run.AbsMinute, fam).ToLowerInvariant()}: about {DosesToCure(state, fam)} dose(s) of medicine to recover.");
            }
            return lines.Count == 0
                ? "Nobody in your family is sick right now. Keep the kids off pagpag and they'll stay that way."
                : string.Join(" ", lines);
        }
    }
}
