using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Characters
{
    /// <summary>
    /// Data for a selectable player character. Gameplay scripts read gender-dependent
    /// conditions from here (and from <see cref="RunData.gender"/>) instead of branching per character.
    /// Both characters start with the same money (<see cref="GameConfig.StartingMoney"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterProfile", menuName = "Pagpag/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public string id = "dong";
        public Gender gender = Gender.Male;
        public string defaultName = "Dong";

        [Header("Art")]
        public Sprite portrait;
        public Sprite fullBody;

        [Header("Text (keys in Resources/Localization/en.json)")]
        [Tooltip("Short summary shown on the character card.")]
        public string descriptionKey = "char.dong.desc";
        [Tooltip("Backstory pages, shown in character creation and the intro. {0} = player name.")]
        public string[] loreKeys = new string[0];

        [Header("Household")]
        [Tooltip("Dong and Inday are husband and wife sharing one room and one budget. The player lives as one of them.")]
        public string spouseName = "Inday";

        [Header("Starting circumstances")]
        [Tooltip("Flags set at the start of a run, e.g. an NPC this character already knows.")]
        public string[] startingFlags = new string[0];
    }
}
