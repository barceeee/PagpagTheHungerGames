using System.Linq;
using Pagpag.TimeSystem;

namespace Pagpag.Core
{
    /// <summary>
    /// The winning condition. Survive <see cref="GameConfig.victoryDays"/> full days (from the run's first morning)
    /// with every family member alive to win; winning unlocks Infinite Mode. Reaching the goal after someone has
    /// died ends the run without a win. Infinite Mode runs have no goal. Pure logic on <see cref="RunData"/>.
    /// </summary>
    public static class VictoryRules
    {
        /// <summary>Minutes lived since the run's first morning.</summary>
        public static int MinutesSurvived(RunData run, GameConfig cfg) => run.AbsMinute - cfg.startMinuteOfDay;

        /// <summary>Full days lived since the run's first morning.</summary>
        public static int DaysSurvived(RunData run, GameConfig cfg) => MinutesSurvived(run, cfg) / GameTime.MinutesPerDay;

        public static bool HasGoal(RunData run) => !run.infiniteMode;

        public static bool ReachedGoal(RunData run, GameConfig cfg) =>
            HasGoal(run) && MinutesSurvived(run, cfg) >= cfg.victoryDays * GameTime.MinutesPerDay;

        public static bool WholeFamilyAlive(RunData run) => run.family.All(m => m.alive);

        public static int FamilyLost(RunData run) => run.family.Count(m => !m.alive);

        /// <summary>
        /// Call after anything that passes time. Returns <see cref="EndKind.Victory"/> (run waits on the victory screen),
        /// <see cref="EndKind.SurvivedWithLosses"/> (run is over) or <see cref="EndKind.None"/>.
        /// A death in the same action wins over the goal: a dead player never wins.
        /// </summary>
        public static EndKind Check(RunData run, GameConfig cfg)
        {
            if (run.isDead || run.victoryPending || !ReachedGoal(run, cfg)) return EndKind.None;
            if (WholeFamilyAlive(run))
            {
                run.victoryPending = true;
                run.won = true;
                run.endKind = EndKind.Victory;
                return EndKind.Victory;
            }
            run.isDead = true; // the run is over (see endKind)
            run.endKind = EndKind.SurvivedWithLosses;
            var lost = FamilyLost(run);
            run.causeOfDeath = $"Survived {cfg.victoryDays} days, but lost {lost} {(lost == 1 ? "family member" : "family members")}";
            return EndKind.SurvivedWithLosses;
        }

        // ---------- Leaving the slums ----------

        /// <summary>Leaving is offered on the victory screen and, in Infinite Mode, at home.</summary>
        public static bool LeavingOffered(RunData run) => !run.isDead && (run.victoryPending || run.infiniteMode);

        public static bool MetQuota(RunData run, GameConfig cfg) => run.money >= cfg.leaveSlumsQuota;

        /// <returns>null if the family can leave now, otherwise why not.</returns>
        public static string LeaveBlockedReason(RunData run, GameConfig cfg)
        {
            if (run.isDead) return "The run has ended.";
            if (!LeavingOffered(run)) return $"Survive {cfg.victoryDays} days with the whole family first.";
            if (!MetQuota(run, cfg)) return $"You need ₱{cfg.leaveSlumsQuota:N0} to leave. You have ₱{run.money:N0}.";
            return null;
        }

        /// <summary>The family leaves Tondo for good. Returns false (and changes nothing) if they can't yet.</summary>
        public static bool LeaveTheSlums(RunData run, GameConfig cfg)
        {
            if (LeaveBlockedReason(run, cfg) != null) return false;
            run.victoryPending = false;
            run.isDead = true; // the run is over (see endKind)
            run.endKind = EndKind.LeftTheSlums;
            run.causeOfDeath = $"Left the slums with ₱{run.money:N0}";
            return true;
        }

        /// <summary>After a win: keep playing the same run with no goal.</summary>
        public static void ContinueInInfiniteMode(RunData run)
        {
            run.victoryPending = false;
            run.infiniteMode = true;
            run.endKind = EndKind.None;
        }
    }
}
