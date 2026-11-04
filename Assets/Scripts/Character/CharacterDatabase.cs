using System.Collections.Generic;
using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Characters
{
    /// <summary>Lives at Resources/CharacterDatabase so it can be found without scene references.</summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Pagpag/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        public List<CharacterProfile> characters = new List<CharacterProfile>();

        public CharacterProfile Get(Gender gender)
        {
            foreach (var c in characters)
                if (c != null && c.gender == gender) return c;
            return null;
        }

        public CharacterProfile GetById(string id)
        {
            foreach (var c in characters)
                if (c != null && c.id == id) return c;
            return null;
        }
    }
}
