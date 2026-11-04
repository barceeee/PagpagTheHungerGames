using System;

namespace Pagpag.Minigames
{
    /// <summary>Everything a minigame needs to know about the job and the worker.</summary>
    public class MinigameContext
    {
        public string minigameId;
        public string jobName;
        public string locationName;
        public string workerName;
        /// <summary>Short line about the worker, e.g. "Ed · Strength ★★★★★".</summary>
        public string workerNote;
        /// <summary>1 = normal. Higher = more forgiving (wider windows, slower targets). Comes from skill, energy, grief.</summary>
        public float ease = 1f;
        /// <summary>Listed wage and the multiplier applied to it (child rate, grief).</summary>
        public int wage;
        public float payMultiplier = 1f;
        /// <summary>Pay at 80% performance (shown to the player).</summary>
        public int fullPay;
        /// <summary>Pay at 100% performance.</summary>
        public int maxPay;
        public bool isChild;
        public int seed;
    }

    public struct MinigameResult
    {
        /// <summary>False if the worker walked away.</summary>
        public bool completed;
        /// <summary>Performance 0–1.</summary>
        public float score;
    }

    public class MinigameRequest
    {
        public MinigameContext context;
        public Action<MinigameResult> onComplete;
    }

    /// <summary>The intimacy event: fade to black, then the fertility wheel.</summary>
    public class WheelRequest
    {
        public string sceneText;
        public string spouseName;
        public bool playerIsInday;
        public int segments = 20;
        public float pregnancyChance = 0.05f;
        /// <summary>Outcome rolled before the spin (Random.value &lt; pregnancyChance); the wheel animates to match it.</summary>
        public bool pregnant;
        /// <summary>Called with the outcome once the wheel stops.</summary>
        public Action<bool> onComplete;
    }
}
