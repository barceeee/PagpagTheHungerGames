using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Family
{
    /// <summary>
    /// One of Dong and Inday's five children: who they are, what they're good at,
    /// and how fast they get hungry and tired.
    /// </summary>
    [CreateAssetMenu(fileName = "Child", menuName = "Pagpag/Child Profile")]
    public class ChildProfile : ScriptableObject
    {
        public string id;
        public string displayName;
        [Range(1, 17)] public int age = 10;
        public Gender gender;
        [Tooltip("e.g. \"Eldest son\"")]
        public string role;
        public Sprite portrait;

        [Header("Personality")]
        [Tooltip("One line shown on cards.")]
        public string personality;
        [TextArea(3, 8)] public string backstory;
        [Tooltip("Shown as chips, e.g. \"Strong\", \"Big eater\".")]
        public string[] traitTags = new string[0];

        [Header("Skills (1–5): how well they do jobs that test each skill")]
        [Range(1, 5)] public int strength = 2;
        [Range(1, 5)] public int dexterity = 2;
        [Range(1, 5)] public int smarts = 2;
        [Range(1, 5)] public int charm = 2;

        [Header("Body")]
        [Tooltip("Hunger drain multiplier. 1 = an adult's rate.")]
        [Range(0.3f, 2f)] public float appetite = 0.8f;
        [Tooltip("Energy drain multiplier. Higher = tires faster.")]
        [Range(0.3f, 2f)] public float tireRate = 1f;
        [Tooltip("Health damage multiplier (and recovery divider). Higher = more fragile.")]
        [Range(0.5f, 2f)] public float frailty = 1f;

        public int SkillValue(Skill skill) => skill switch
        {
            Skill.Strength => strength,
            Skill.Dexterity => dexterity,
            Skill.Smarts => smarts,
            _ => charm
        };
    }
}
