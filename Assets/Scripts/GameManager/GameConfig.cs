using UnityEngine;

namespace Pagpag.Core
{
    /// <summary>
    /// Global tunables. Lives at Resources/GameConfig.
    /// Every stat is 0–100 where 100 is full/best, and everyone starts at 100.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Pagpag/Game Config")]
    public class GameConfig : ScriptableObject
    {
        /// <summary>Every run starts with exactly this much money — no bonuses or randomization.</summary>
        public const int StartingMoney = 500;
        /// <summary>Every stat of every family member starts here.</summary>
        public const float StartingStat = 100f;

        [Header("Campaign")]
        [Min(1)] public int campaignDays = 365;
        [Tooltip("Survive this many full days with the whole family alive to win and unlock Infinite Mode.")]
        [Min(1)] public int victoryDays = 365;
        [Tooltip("Money needed to leave the slums (after a win, or any time in Infinite Mode). Leaving ends the run.")]
        [Min(1)] public int leaveSlumsQuota = 100000;
        [Range(0, 1439)] public int startMinuteOfDay = 6 * 60;
        public string startLocationId = "home";

        [Header("Character creation")]
        [Min(1)] public int minNameLength = 2;
        [Min(2)] public int maxNameLength = 16;

        [Header("Difficulty modes")]
        public DifficultySettings normal;
        public DifficultySettings hard;

        [Header("Drain and recovery per in-game hour (adult rate, before difficulty multiplier)")]
        public float hungerDrainPerHourAwake = 3f;
        public float hungerDrainPerHourAsleep = 1.5f;
        public float energyDrainPerHourAwake = 2.5f;
        public float energyRecoveryPerHourAsleep = 12.5f;

        [Header("Health")]
        [Tooltip("Hunger at/below this starts hurting slowly; at 0 it is starvation.")]
        public float hungerDamageThreshold = 15f;
        [Tooltip("Energy at/below this starts hurting slowly; at 0 it is collapse.")]
        public float energyDamageThreshold = 10f;
        public float minorDamagePerHour = 1f;
        [Tooltip("Damage per hour while hunger or energy is at 0 (starvation / collapse).")]
        public float severeDamagePerHour = 4f;
        [Tooltip("Health only recovers while hunger and energy are at least these.")]
        public float regenMinHunger = 40f;
        public float regenMinEnergy = 30f;
        public float healthRegenPerHourAwake = 0.5f;
        public float healthRegenPerHourAsleep = 1.5f;

        [Header("Warnings (at or below)")]
        public float hungerWarning = 25f;
        public float energyWarning = 20f;
        public float healthWarning = 35f;

        [Header("Work limits (player)")]
        public float minEnergyToWork = 15f;
        public float minHungerToWork = 10f;

        [Header("Travel (distances are fractions of the map width)")]
        public int walkBaseMinutes = 5;
        public float walkMinutesPerMapWidth = 60f;
        public float walkEnergyPerMapWidth = 8f;
        public int jeepneyFare = 13;
        public int jeepneyBaseMinutes = 8;
        public float jeepneyMinutesPerMapWidth = 20f;
        public float jeepneyMinDistance = 0.3f;

        [Header("Night: family members not working sleep between these hours")]
        [Range(0, 23)] public int familyBedHour = 21;
        [Range(0, 23)] public int familyWakeHour = 6;

        public DifficultySettings GetDifficulty(Difficulty d) => d == Difficulty.Hard ? hard : normal;
    }
}
