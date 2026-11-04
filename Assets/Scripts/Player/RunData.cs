using System;
using System.Collections.Generic;

namespace Pagpag.Core
{
    /// <summary>
    /// One person's illness and pagpag habit. Empty <see cref="id"/> = healthy.
    /// See <see cref="Pagpag.Survival.IllnessRules"/>.
    /// </summary>
    [Serializable]
    public class IllnessState
    {
        public string id;
        /// <summary>0–100. Rises every hour without medicine; the illness ends when it reaches 0.</summary>
        public float severity;
        /// <summary>Absolute minute until which a medicine dose is working.</summary>
        public int treatedUntilAbs = -1;
        /// <summary>Days in a row this person has eaten pagpag (the longer, the likelier typhoid).</summary>
        public int pagpagStreak;
        public int lastPagpagDay = -99;
    }

    /// <summary>
    /// Stats of one family member other than the player (the spouse and the five children).
    /// Every stat is 0–100 where 100 is full/best.
    /// </summary>
    [Serializable]
    public class MemberState
    {
        public string id;
        public string name;
        public bool isSpouse;
        public float hunger = 100f;
        public float energy = 100f;
        public float health = 100f;
        public bool alive = true;
        /// <summary>Absolute minute (see <see cref="RunData.AbsMinute"/>) until which the member is away working.</summary>
        public int busyUntilAbs = -1;
        public string busyJob;
        public string lastDamageSource;
        public string causeOfDeath;
        public int diedDay;
        public IllnessState illness = new IllnessState();
    }

    /// <summary>
    /// Everything that belongs to one hardcore run. Serialized to JSON by <see cref="Pagpag.Save.SaveSystem"/>.
    /// Bump <see cref="CurrentVersion"/> when fields change meaning (older saves are then refused).
    /// </summary>
    [Serializable]
    public class RunData
    {
        /// <summary>v3: stats use 100 = best, family members, pantry, depression.</summary>
        public const int CurrentVersion = 3;

        public int saveVersion = CurrentVersion;
        public string runId;
        public string createdUtc;

        // Character
        public string playerName;
        public Gender gender;
        public string characterId;
        public Difficulty difficulty;

        // Time and place
        public int day = 1;
        public int minuteOfDay = 6 * 60;
        public string currentLocationId = "home";
        public bool insideLocation = true;
        public bool introSeen;

        // Player stats (0–100, 100 = full/best)
        public float hunger = 100f;
        public float energy = 100f;
        public float health = 100f;
        public IllnessState playerIllness = new IllnessState();

        // Medicine cabinet at home (bought at the pharmacy)
        public int medicineDoses;
        public int instantCures;

        // Family: the spouse and the five children
        public List<MemberState> family = new List<MemberState>();
        /// <summary>Household food, in servings. Pagpag servings are cheaper but can make people sick.</summary>
        public int pantrySafe;
        public int pantryPagpag;
        /// <summary>
        /// The child who secretly steals while the player sleeps. Chosen once when the run is created and saved
        /// with it, so it never re-rolls. Never shown in the UI. Empty when the family has no children.
        /// </summary>
        public string blackSheepId;

        // Grief
        public int depressionEndAbs = -1;
        public string depressionCause;
        public int childDeaths;

        // Economy and records
        public int money;
        public int totalEarned;
        public int totalSpent;
        public int foodSpent;
        public int transportSpent;
        public int otherSpent;
        public int shiftsWorked;
        public int childShiftsWorked;
        public int mealsEaten;
        public int minigamesPlayed;
        /// <summary>Total taken by the black sheep (not shown to the player as theft).</summary>
        public int moneyMissing;

        // Progression
        public List<string> flags = new List<string>();
        public List<string> unlockedLocations = new List<string>();
        /// <summary>"day:workerId:actionId" entries for once-per-day actions.</summary>
        public List<string> dailyUses = new List<string>();

        // Hardcore bookkeeping
        public bool isDead;
        public EndKind endKind;
        public string causeOfDeath;
        public string lastDamageSource;
        public bool developerRun;

        // Winning
        /// <summary>No day goal: the run only ends in a loss. Chosen at creation (once unlocked) or after a win.</summary>
        public bool infiniteMode;
        /// <summary>The goal was just reached with everyone alive; the victory screen waits for the player's choice.</summary>
        public bool victoryPending;
        /// <summary>This run reached the goal and won (it may continue in Infinite Mode).</summary>
        public bool won;

        public const string PlayerWorker = "player";

        public int AbsMinute => (day - 1) * 24 * 60 + minuteOfDay;
        public bool IsDepressed => depressionEndAbs > AbsMinute && !isDead;
        public int DepressionMinutesLeft => IsDepressed ? depressionEndAbs - AbsMinute : 0;
        public int PantryTotal => pantrySafe + pantryPagpag;
        /// <summary>The run is over (death or another losing condition).</summary>
        public bool IsOver => isDead;

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        public MemberState Member(string id) => family.Find(m => m.id == id);
        public MemberState Spouse => family.Find(m => m.isSpouse);

        public bool UsedToday(string actionId, string worker = PlayerWorker) =>
            dailyUses.Contains($"{day}:{worker}:{actionId}");

        public void MarkUsedToday(string actionId, string worker = PlayerWorker)
        {
            dailyUses.RemoveAll(e => !e.StartsWith(day + ":", StringComparison.Ordinal));
            dailyUses.Add($"{day}:{worker}:{actionId}");
        }

        /// <summary>True if this worker already did any of the given actions today.</summary>
        public bool WorkerUsedAnyToday(string worker) =>
            dailyUses.Exists(e => e.StartsWith($"{day}:{worker}:", StringComparison.Ordinal));
    }
}
