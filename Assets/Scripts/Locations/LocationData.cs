using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pagpag.Locations
{
    [Serializable]
    public class UnlockCondition
    {
        public bool lockedAtStart;
        [Min(1)] public int minDay = 1;
        public string requiredFlag;
        [TextArea] public string hint;
    }

    /// <summary>A destination on the world map with its own screen, hours and actions.</summary>
    [CreateAssetMenu(fileName = "Location", menuName = "Pagpag/Location")]
    public class LocationData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;

        [Header("Map")]
        [Tooltip("Label rectangle on the map art in pixels (x, y from the top-left, width, height).")]
        public Rect mapLabelRect;
        [Tooltip("Centre of the location's close-up background, in map pixels (top-left origin).")]
        public Vector2 sceneCenter;
        [Tooltip("Fraction of the map width shown on the location screen.")]
        [Range(0.1f, 1f)] public float sceneZoom = 0.36f;

        [Header("Hours (24h). open == close means always open; may wrap past midnight.")]
        [Range(0, 24)] public int openHour;
        [Range(0, 24)] public int closeHour = 24;

        public UnlockCondition unlock = new UnlockCondition();
        public List<LocationAction> actions = new List<LocationAction>();

        public Vector2 MapPoint => mapLabelRect.center;
        public bool AlwaysOpen => openHour == closeHour || (openHour == 0 && closeHour == 24);
    }
}
