using System;
using System.IO;
using Pagpag.Core;
using UnityEngine;

namespace Pagpag.Save
{
    /// <summary>
    /// Hardcore save model: exactly one autosave slot, overwritten in place.
    /// There is no manual save/load list. On death the slot is deleted and the run id is
    /// recorded in a graveyard, so a copied-back save file for that run is refused.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "pagpag_run.json";
        private const string GraveyardFileName = "pagpag_graveyard.txt";
        private const string InfiniteUnlockFileName = "pagpag_infinite_unlocked.txt";

        private static string _directoryOverride;

        /// <summary>Defaults to Application.persistentDataPath. Tests point this at a temp folder.</summary>
        public static string SaveDirectory
        {
            get => _directoryOverride ?? Application.persistentDataPath;
            set => _directoryOverride = value;
        }

        public static string SavePath => Path.Combine(SaveDirectory, FileName);
        private static string GraveyardPath => Path.Combine(SaveDirectory, GraveyardFileName);

        public static bool HasSave() => TryLoad(out _);

        public static void Save(RunData run)
        {
            if (run == null || run.isDead) return;
            Directory.CreateDirectory(SaveDirectory);
            var tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(run, true));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tmp, SavePath);
        }

        public static bool TryLoad(out RunData run)
        {
            run = null;
            if (!File.Exists(SavePath)) return false;
            try
            {
                run = JsonUtility.FromJson<RunData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Unreadable save: {e.Message}");
                return false;
            }
            if (run == null || string.IsNullOrEmpty(run.runId)) return false;
            if (run.saveVersion < RunData.CurrentVersion)
            {
                // Older saves use a different stat scale and have no family; they can't be continued.
                Debug.LogWarning($"[Save] Save version {run.saveVersion} is from an older build and can't be continued.");
                Delete();
                run = null;
                return false;
            }
            if (run.isDead || IsBuried(run.runId))
            {
                Delete();
                run = null;
                return false;
            }
            return true;
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        /// <summary>Called by the death flow (Phase 5). Permanently ends the run.</summary>
        public static void BuryRun(RunData run)
        {
            if (run == null) return;
            run.isDead = true;
            if (!IsBuried(run.runId))
            {
                Directory.CreateDirectory(SaveDirectory);
                File.AppendAllText(GraveyardPath, run.runId + Environment.NewLine);
            }
            Delete();
        }

        /// <summary>Set once by winning a run; never cleared by deleting or burying a run.</summary>
        public static bool InfiniteModeUnlocked => File.Exists(Path.Combine(SaveDirectory, InfiniteUnlockFileName));

        public static void UnlockInfiniteMode(string runId)
        {
            if (InfiniteModeUnlocked) return;
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(Path.Combine(SaveDirectory, InfiniteUnlockFileName), $"{runId} {DateTime.UtcNow:o}");
        }

        private static bool IsBuried(string runId) =>
            File.Exists(GraveyardPath) && Array.IndexOf(File.ReadAllLines(GraveyardPath), runId) >= 0;
    }
}
