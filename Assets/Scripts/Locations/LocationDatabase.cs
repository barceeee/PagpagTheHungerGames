using System.Collections.Generic;
using UnityEngine;

namespace Pagpag.Locations
{
    /// <summary>All map locations. Lives at Resources/LocationDatabase.</summary>
    [CreateAssetMenu(fileName = "LocationDatabase", menuName = "Pagpag/Location Database")]
    public class LocationDatabase : ScriptableObject
    {
        public Sprite mapArt;
        [Tooltip("Size of the map art in pixels; label rects are in this space.")]
        public Vector2 mapSize = new Vector2(1250, 910);
        public List<LocationData> locations = new List<LocationData>();

        public LocationData Get(string id)
        {
            foreach (var l in locations)
                if (l != null && l.id == id) return l;
            return null;
        }
    }
}
