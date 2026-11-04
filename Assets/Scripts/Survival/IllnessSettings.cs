using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pagpag.Survival
{
    /// <summary>One illness anyone in the household can catch.</summary>
    [Serializable]
    public class IllnessDefinition
    {
        public string id;
        public string displayName;
        [Tooltip("Severity gained per day without medicine (severity is 0–100; at 0 the illness is gone).")]
        public float worsenPerDay = 8f;
        [Tooltip("Multiplies the health damage the illness does.")]
        public float damageScale = 1f;
        [Tooltip("Relative chance of being the one caught on a random daily roll. 0 = never random (e.g. typhoid comes only from pagpag).")]
        public float randomWeight = 1f;
        [TextArea] public string symptoms;
    }

    /// <summary>
    /// Illness rules. Untreated illness only gets worse and keeps damaging health until it kills.
    /// A medicine dose reverses it for a day; an instant cure ends it at once. The same in every difficulty mode.
    /// </summary>
    [Serializable]
    public class IllnessSettings
    {
        public List<IllnessDefinition> illnesses = new List<IllnessDefinition>
        {
            new IllnessDefinition
            {
                id = "typhoid", displayName = "Typhoid fever", worsenPerDay = 10f, damageScale = 1f, randomWeight = 0f,
                symptoms = "High fever, stomach pain and weakness, from eating dirty, recooked food."
            },
            new IllnessDefinition
            {
                id = "flu", displayName = "Flu", worsenPerDay = 6f, damageScale = 0.8f, randomWeight = 3f,
                symptoms = "Fever, chills and a cough that won't stop."
            },
            new IllnessDefinition
            {
                id = "dengue", displayName = "Dengue fever", worsenPerDay = 12f, damageScale = 1.3f, randomWeight = 1f,
                symptoms = "A mosquito-borne fever: rash, joint pain and bleeding gums. It gets dangerous fast."
            }
        };

        [Header("Getting sick")]
        [Tooltip("Severity an illness starts at.")]
        public float onsetSeverity = 20f;
        [Tooltip("Hour of the daily roll for random illness (flu, dengue).")]
        [Range(0, 23)] public int dailyCheckHour = 6;
        [Tooltip("Daily chance of catching a random illness.")]
        [Range(0, 1)] public float randomChancePerDay = 0.012f;
        [Tooltip("Extra daily chance when that person is starving or exhausted at the roll.")]
        [Range(0, 1)] public float weakExtraChancePerDay = 0.03f;

        [Header("Pagpag eaten day after day")]
        [Tooltip("The illness pagpag causes.")]
        public string pagpagIllnessId = "typhoid";
        [Tooltip("Chance per pagpag meal on the first day of a pagpag streak.")]
        [Range(0, 1)] public float pagpagBaseChance = 0.02f;
        [Tooltip("Added per extra day in a row that the same person eats pagpag.")]
        [Range(0, 1)] public float pagpagPerStreakDay = 0.06f;
        [Range(0, 1)] public float pagpagMaxChance = 0.6f;

        [Header("Effects")]
        [Tooltip("Health lost per hour at severity 100 (scaled by the illness and the person's frailty).")]
        public float damagePerHourAtMax = 1.2f;
        [Tooltip("Extra energy drained per hour at severity 100.")]
        public float energyDrainPerHourAtMax = 1.5f;
        [Tooltip("At or above this severity, nobody can work.")]
        public float workBlockSeverity = 60f;

        [Header("Medicine")]
        [Tooltip("Hours one medicine dose works for.")]
        public int doseHours = 24;
        [Tooltip("Severity removed per day while a dose is working.")]
        public float treatedRecoveryPerDay = 22f;
        [Tooltip("Illness damage multiplier while a dose is working.")]
        [Range(0, 1)] public float treatedDamageMultiplier = 0.4f;

        public IllnessDefinition Get(string id) => illnesses.Find(i => i.id == id);
    }
}
