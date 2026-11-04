using System.Collections.Generic;
using Pagpag.Survival;
using UnityEngine;

namespace Pagpag.Family
{
    /// <summary>The children and household rules. Lives at Resources/FamilyDatabase.</summary>
    [CreateAssetMenu(fileName = "FamilyDatabase", menuName = "Pagpag/Family Database")]
    public class FamilyDatabase : ScriptableObject
    {
        public List<ChildProfile> children = new List<ChildProfile>();

        [Header("Spouse (stays home, minds the children and serves meals)")]
        [Range(0.3f, 2f)] public float spouseAppetite = 0.9f;
        [Range(0.3f, 2f)] public float spouseTireRate = 0.8f;
        [Range(0.5f, 2f)] public float spouseFrailty = 1f;

        [Header("Food")]
        [Tooltip("Hunger restored by one pantry serving.")]
        public float servingRestore = 35f;
        [Tooltip("Hours when the spouse serves meals from the pantry (to the spouse and children at home).")]
        public int[] autoMealHours = { 7, 12, 18 };
        [Tooltip("Members only eat at an auto meal if their hunger is below this.")]
        public float autoMealThreshold = 75f;
        [Range(0, 1)] public float pagpagSicknessChance = 0.15f;
        public float pagpagSicknessDamage = 12f;

        [Header("Illness and medicine (player, spouse and children)")]
        public IllnessSettings illness = new IllnessSettings();

        [Header("Children's work")]
        [Tooltip("Share of the adult wage a child earns at full performance.")]
        [Range(0.1f, 1f)] public float childPayRate = 0.4f;
        public float childMinEnergyToWork = 20f;
        public float childMinHungerToWork = 20f;

        [Header("Grief (when a child dies)")]
        public int depressionDays = 7;
        [Tooltip("Player hunger and energy drain multiplier while depressed.")]
        public float depressionDrainMultiplier = 1.5f;
        [Tooltip("Sleep and health recovery multiplier while depressed.")]
        public float depressionRecoveryMultiplier = 0.5f;
        [Tooltip("Job pay multiplier while depressed.")]
        public float depressionPayMultiplier = 0.6f;
        [Tooltip("Minigame ease multiplier while depressed (lower = harder).")]
        public float depressionEaseMultiplier = 0.8f;

        [Header("Intimacy event")]
        [Tooltip("Fertility chance on the wheel. The wheel has 20 segments; 1 is the pregnancy segment.")]
        [Range(0, 1)] public float pregnancyChance = 0.05f;
        public int wheelSegments = 20;

        public ChildProfile Get(string id)
        {
            foreach (var c in children)
                if (c != null && c.id == id) return c;
            return null;
        }
    }
}
