using System;
using System.Collections.Generic;
using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Localization
{
    /// <summary>
    /// The game is English-only. All UI and story text lives in Resources/Localization/en.json,
    /// outside gameplay code. Gender variants: a key "x" may be overridden by "x.m" or "x.f";
    /// <see cref="Get(string, Gender, object[])"/> tries the gendered key first.
    /// </summary>
    public static class Loc
    {
        [Serializable] private class Entry { public string key; public string value; }
        [Serializable] private class Table { public List<Entry> entries = new List<Entry>(); }

        public const string TablePath = "Localization/en";

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        private static readonly HashSet<string> ReportedMissing = new HashSet<string>();
        private static bool _loaded;

        public static bool Has(string key)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(key) && Strings.ContainsKey(key);
        }

        public static string Get(string key, params object[] args)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!Strings.TryGetValue(key, out var value))
            {
                if (ReportedMissing.Add(key)) Debug.LogWarning($"[Loc] Missing key '{key}'");
                return $"[{key}]";
            }
            return args == null || args.Length == 0 ? value : string.Format(value, args);
        }

        public static string Get(string key, Gender gender, params object[] args)
        {
            var gendered = key + (gender == Gender.Female ? ".f" : ".m");
            return Has(gendered) ? Get(gendered, args) : Get(key, args);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            Strings.Clear();
            var asset = Resources.Load<TextAsset>(TablePath);
            if (asset == null)
            {
                Debug.LogError($"[Loc] Missing Resources/{TablePath}.json");
                return;
            }
            foreach (var e in JsonUtility.FromJson<Table>(asset.text).entries)
                if (!string.IsNullOrEmpty(e.key)) Strings[e.key] = e.value;
        }

#if UNITY_EDITOR
        // Enter Play Mode without domain reload still starts from a fresh table.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Strings.Clear();
            ReportedMissing.Clear();
        }
#endif
    }
}
