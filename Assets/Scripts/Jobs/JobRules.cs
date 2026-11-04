using Pagpag.Core;
using Pagpag.Family;
using UnityEngine;

namespace Pagpag.Jobs
{
    /// <summary>
    /// How minigame performance turns into pay, and how a worker's condition changes difficulty.
    /// Every job pays only through its minigame.
    /// </summary>
    public static class JobRules
    {
        /// <summary>Below this performance the employer pays nothing.</summary>
        public const float MinScoreForPay = 0.15f;
        /// <summary>Performance that earns the full listed wage.</summary>
        public const float FullPayScore = 0.8f;
        /// <summary>Best possible pay as a multiple of the listed wage (perfect game).</summary>
        public const float MaxPayFactor = 1.25f;

        /// <param name="score">Minigame performance, 0–1.</param>
        /// <param name="multiplier">Child pay rate, grief penalty, etc.</param>
        public static int Payout(int wage, float score, float multiplier = 1f)
        {
            if (score < MinScoreForPay || wage <= 0) return 0;
            var factor = Mathf.Min(score / FullPayScore, MaxPayFactor);
            var pay = wage * factor * multiplier;
            return Mathf.Max(5, Mathf.RoundToInt(pay / 5f) * 5);
        }

        public static float PlayerPayMultiplier(RunData run, FamilyDatabase fam) =>
            run.IsDepressed && fam != null ? fam.depressionPayMultiplier : 1f;

        /// <summary>1 = normal. Tired or grieving workers get tighter windows and faster targets.</summary>
        public static float PlayerEase(RunData run, FamilyDatabase fam) =>
            (0.75f + 0.25f * run.energy / 100f) * (run.IsDepressed && fam != null ? fam.depressionEaseMultiplier : 1f);

        public static string Grade(float score)
        {
            if (score >= 0.95f) return "Perfect";
            if (score >= 0.8f) return "Great";
            if (score >= 0.6f) return "Good";
            if (score >= 0.35f) return "Poor";
            if (score >= MinScoreForPay) return "Bad";
            return "Failed";
        }
    }
}
