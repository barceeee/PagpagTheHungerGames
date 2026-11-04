using UnityEngine;

namespace Pagpag.Core
{
    /// <summary>
    /// A difficulty mode. Only the starvation (hunger) and exhaustion (energy) rates differ between
    /// modes, for the player and the whole family; every other rule, price and wage is identical.
    /// </summary>
    [CreateAssetMenu(fileName = "Difficulty", menuName = "Pagpag/Difficulty Settings")]
    public class DifficultySettings : ScriptableObject
    {
        public Difficulty difficulty;
        [Tooltip("Mode name shown to the player, e.g. \"Tagalog\".")]
        public string displayName = "Tagalog";
        [Tooltip("Mode label shown next to the name, e.g. \"Normal\".")]
        public string modeLabel = "Normal";
        [TextArea] public string description;

        [Header("Survival rates (multiply every hunger and energy drain, and starvation/exhaustion damage)")]
        [Min(0.1f)] public float hungerRateMultiplier = 1f;
        [Min(0.1f)] public float fatigueRateMultiplier = 1f;

        public string FullName => $"{displayName} ({modeLabel})";
    }
}
