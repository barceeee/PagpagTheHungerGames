using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Localization;
using Pagpag.Save;
using UnityEngine;

namespace Pagpag.Tests
{
    public class LocalizationTests
    {
        [Serializable] private class Entry { public string key; public string value; }
        [Serializable] private class Table { public List<Entry> entries; }

        [Test]
        public void EnglishTableIsTheOnlyTableAndHasNoDuplicates()
        {
            var tables = Resources.LoadAll<TextAsset>("Localization");
            Assert.AreEqual(1, tables.Length, "The game is English-only");
            Assert.AreEqual("en", tables[0].name);
            var keys = new HashSet<string>();
            foreach (var e in JsonUtility.FromJson<Table>(tables[0].text).entries)
                Assert.IsTrue(keys.Add(e.key), $"Duplicate key '{e.key}'");
        }

        [Test]
        public void GenderedKeysFallBackAndOverride()
        {
            StringAssert.Contains("Mang Berto", Loc.Get("world.intro", Gender.Male, "Dong", "₱500"));
            StringAssert.Contains("Aling Nena", Loc.Get("world.intro", Gender.Female, "Inday", "₱500"));
            Assert.AreEqual(Loc.Get("menu.newgame"), Loc.Get("menu.newgame", Gender.Female));
            Assert.AreEqual("New Game", Loc.Get("menu.newgame"));
        }

        [Test]
        public void EveryCharacterHasLoreAndADescription()
        {
            var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
            foreach (var c in db.characters)
            {
                Assert.IsTrue(Loc.Has(c.descriptionKey), c.id);
                Assert.GreaterOrEqual(c.loreKeys.Length, 2, c.id + " needs a backstory");
                foreach (var k in c.loreKeys) Assert.IsTrue(Loc.Has(k), k);
            }
            // Husband and wife
            Assert.AreEqual("Inday", db.Get(Gender.Male).spouseName);
            Assert.AreEqual("Dong", db.Get(Gender.Female).spouseName);
        }
    }

    public class NameRulesTests
    {
        [TestCase("Dong", true)]
        [TestCase("  Maria   Clara ", true)]
        [TestCase("Niño", true)]
        [TestCase("D'Angelo", true)]
        [TestCase("Ana-Marie", true)]
        [TestCase("A", false)]
        [TestCase("", false)]
        [TestCase("Inday2", false)]
        [TestCase("-Dong", false)]
        [TestCase("Abcdefghijklmnopq", false)] // 17 letters
        public void Validates(string raw, bool valid)
        {
            var error = NameRules.Validate(raw, 2, 16, out _);
            Assert.AreEqual(valid, error == null, error);
        }

        [Test]
        public void CollapsesWhitespace()
        {
            NameRules.Validate("  Maria   Clara ", 2, 16, out var clean);
            Assert.AreEqual("Maria Clara", clean);
        }
    }

    public class SaveSystemTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "pagpag-tests-" + Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectory = _dir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SaveDirectory = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static RunData NewRun() => new RunData
        {
            runId = Guid.NewGuid().ToString("N"),
            playerName = "Inday",
            gender = Gender.Female,
            characterId = "inday",
            difficulty = Difficulty.Hard,
            money = 500,
            hunger = 40,
            energy = 20
        };

        [Test]
        public void RoundTrips()
        {
            var run = NewRun();
            run.SetFlag("knows_aling_nena");
            run.unlockedLocations.Add("port");
            run.currentLocationId = "market";
            SaveSystem.Save(run);
            Assert.IsTrue(SaveSystem.TryLoad(out var loaded));
            Assert.AreEqual(run.runId, loaded.runId);
            Assert.AreEqual(Gender.Female, loaded.gender);
            Assert.AreEqual("inday", loaded.characterId);
            Assert.AreEqual(Difficulty.Hard, loaded.difficulty);
            Assert.AreEqual(500, loaded.money);
            Assert.AreEqual(40f, loaded.hunger);
            Assert.AreEqual(20f, loaded.energy);
            Assert.AreEqual("market", loaded.currentLocationId);
            Assert.IsTrue(loaded.HasFlag("knows_aling_nena"));
            Assert.Contains("port", loaded.unlockedLocations);
        }

        [Test]
        public void DeadRunCannotBeRestoredFromBackup()
        {
            var run = NewRun();
            SaveSystem.Save(run);
            var backup = File.ReadAllText(SaveSystem.SavePath);

            SaveSystem.BuryRun(run);
            Assert.IsFalse(SaveSystem.HasSave());

            File.WriteAllText(SaveSystem.SavePath, backup); // player copies an old file back
            Assert.IsFalse(SaveSystem.TryLoad(out _));
            Assert.IsFalse(File.Exists(SaveSystem.SavePath), "Restored save of a dead run should be removed");
        }

        [Test]
        public void OldSaveVersionIsRefused()
        {
            var run = NewRun();
            run.saveVersion = 2;
            Directory.CreateDirectory(_dir);
            File.WriteAllText(SaveSystem.SavePath, JsonUtility.ToJson(run));
            Assert.IsFalse(SaveSystem.TryLoad(out _));
        }

        [Test]
        public void FamilyRoundTrips()
        {
            var run = TestData.NewRun(Gender.Female, Difficulty.Hard);
            run.runId = Guid.NewGuid().ToString("N");
            run.family[3].hunger = 12;
            run.family[5].alive = false;
            run.pantrySafe = 7;
            run.pantryPagpag = 2;
            run.depressionEndAbs = 9999;
            SaveSystem.Save(run);
            Assert.IsTrue(SaveSystem.TryLoad(out var loaded));
            Assert.AreEqual(6, loaded.family.Count);
            Assert.AreEqual(12f, loaded.family[3].hunger);
            Assert.IsFalse(loaded.family[5].alive);
            Assert.AreEqual(9, loaded.PantryTotal);
            Assert.AreEqual(9999, loaded.depressionEndAbs);
        }

        [Test]
        public void CorruptSaveIsIgnored()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(SaveSystem.SavePath, "{ not json");
            Assert.IsFalse(SaveSystem.HasSave());
        }
    }

    public class GameDataTests
    {
        [Test]
        public void BothCharactersExistAndStartBalanced()
        {
            var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
            Assert.NotNull(db, "Run Pagpag > Build All first");
            var dong = db.Get(Gender.Male);
            var inday = db.Get(Gender.Female);
            Assert.NotNull(dong);
            Assert.NotNull(inday);
            Assert.NotNull(dong.portrait); Assert.NotNull(dong.fullBody);
            Assert.NotNull(inday.portrait); Assert.NotNull(inday.fullBody);
            Assert.AreEqual(dong.startingFlags.Length, inday.startingFlags.Length, "Each starts with the same number of contacts");
        }

        [Test]
        public void TwoDifficultyModesNamedTagalogAndBisaya()
        {
            var config = Resources.Load<GameConfig>("GameConfig");
            Assert.AreEqual("Tagalog (Normal)", config.normal.FullName);
            Assert.AreEqual("Bisaya (Hard)", config.hard.FullName);
            Assert.AreEqual(1f, config.normal.hungerRateMultiplier, "Normal uses the standard rate");
            Assert.AreEqual(1f, config.normal.fatigueRateMultiplier);
            Assert.GreaterOrEqual(config.hard.hungerRateMultiplier, 1.5f, "Hard must be noticeably harsher");
            Assert.GreaterOrEqual(config.hard.fatigueRateMultiplier, 1.5f);
            Assert.AreEqual(365, config.campaignDays);
        }

        [TestCase(Gender.Male, Difficulty.Normal)]
        [TestCase(Gender.Male, Difficulty.Hard)]
        [TestCase(Gender.Female, Difficulty.Normal)]
        [TestCase(Gender.Female, Difficulty.Hard)]
        public void EveryRunStartsWithExactly500Pesos(Gender gender, Difficulty difficulty)
        {
            var run = TestData.NewRun(gender, difficulty);
            Assert.AreEqual(500, GameConfig.StartingMoney);
            Assert.AreEqual(500, run.money);
            Assert.AreEqual(100f, run.hunger, "All stats start at 100");
            Assert.AreEqual(100f, run.energy);
            Assert.AreEqual(100f, run.health);
            foreach (var m in run.family)
            {
                Assert.AreEqual(100f, m.hunger, m.name);
                Assert.AreEqual(100f, m.energy, m.name);
                Assert.AreEqual(100f, m.health, m.name);
            }
            Assert.AreEqual(gender, run.gender);
            Assert.AreEqual(difficulty, run.difficulty);
            Assert.AreEqual(gender == Gender.Male ? "dong" : "inday", run.characterId);
        }
    }
}
