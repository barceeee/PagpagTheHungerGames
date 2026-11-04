using System.Linq;
using NUnit.Framework;
using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using UnityEngine;

namespace Pagpag.Tests
{
    /// <summary>Shared access to the built data assets for tests.</summary>
    public static class TestData
    {
        public static GameConfig Cfg => Resources.Load<GameConfig>("GameConfig");
        public static CharacterDatabase Chars => Resources.Load<CharacterDatabase>("CharacterDatabase");
        public static LocationDatabase Locs => Resources.Load<LocationDatabase>("LocationDatabase");
        public static FamilyDatabase Fam => Resources.Load<FamilyDatabase>("FamilyDatabase");

        public static RunData NewRun(Gender g = Gender.Male, Difficulty d = Difficulty.Normal)
        {
            Assert.NotNull(Locs, "Run Pagpag > Build All first");
            return GameManager.BuildRun(Cfg, Chars, Locs, Fam, "Test", g, d);
        }

        public static DifficultySettings Diff(RunData r) => Cfg.GetDifficulty(r.difficulty);
        public static LocationData Loc(string id) => Locs.Get(id);
        public static LocationAction Act(string loc, string id) => Loc(loc).actions.First(a => a.id == id);
    }
}
