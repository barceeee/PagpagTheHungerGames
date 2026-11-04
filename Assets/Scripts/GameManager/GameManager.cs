using System;
using Pagpag.Characters;
using Pagpag.Family;
using Pagpag.Jobs;
using Pagpag.Locations;
using Pagpag.Minigames;
using Pagpag.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pagpag.Core
{
    /// <summary>
    /// Persistent root of a play session. Created automatically before the first scene loads,
    /// so any scene can be played directly from the editor.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private CharacterDatabase characters;
        [SerializeField] private LocationDatabase locations;
        [SerializeField] private FamilyDatabase family;

        [Tooltip("Developer testing mode. Ignored in release builds; runs started with it are marked developerRun.")]
        [SerializeField] private bool developerMode;

        private const string EditorTestRunId = "editor-test";

        public GameConfig Config => config;
        public CharacterDatabase Characters => characters;
        public LocationDatabase Locations => locations;
        public FamilyDatabase Family => family;
        public RunData CurrentRun { get; private set; }
        /// <summary>True while a minigame or the wheel is open (prevents double starts).</summary>
        public bool EventInProgress { get; private set; }

        public CharacterProfile CurrentCharacter =>
            CurrentRun == null ? null : characters.GetById(CurrentRun.characterId);

        public CharacterProfile SpouseCharacter =>
            CurrentRun == null ? null : characters.Get(CurrentRun.gender == Gender.Male ? Gender.Female : Gender.Male);

        public DifficultySettings CurrentDifficulty =>
            config.GetDifficulty(CurrentRun?.difficulty ?? Difficulty.Normal);

        public LocationData CurrentLocation =>
            CurrentRun == null ? null : locations.Get(CurrentRun.currentLocationId);

        /// <summary>Only honoured in the editor and development builds; release builds are always hardcore.</summary>
        public bool DeveloperMode => developerMode && (Application.isEditor || Debug.isDebugBuild);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("[GameManager]").AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null) config = Resources.Load<GameConfig>("GameConfig");
            if (characters == null) characters = Resources.Load<CharacterDatabase>("CharacterDatabase");
            if (locations == null) locations = Resources.Load<LocationDatabase>("LocationDatabase");
            if (family == null) family = Resources.Load<FamilyDatabase>("FamilyDatabase");
            if (config == null || characters == null || locations == null || family == null)
                Debug.LogError("[GameManager] Missing Resources data. Run Pagpag > Build All.");
        }

        private void OnApplicationQuit() => SaveCurrentRun();

        public void SaveCurrentRun()
        {
            if (CurrentRun != null && CurrentRun.runId != EditorTestRunId) SaveSystem.Save(CurrentRun);
        }

        // ---------- Run lifecycle ----------

        /// <summary>Builds a fresh run. Every run starts with exactly <see cref="GameConfig.StartingMoney"/>.</summary>
        public RunData CreateRun(string playerName, Gender gender, Difficulty difficulty)
        {
            var run = BuildRun(config, characters, locations, family, playerName, gender, difficulty);
            run.developerRun = DeveloperMode;
            return run;
        }

        /// <summary>Pure run factory (also used by tests). Everyone's stats start at 100.</summary>
        public static RunData BuildRun(GameConfig config, CharacterDatabase characters, LocationDatabase locations, FamilyDatabase family,
            string playerName, Gender gender, Difficulty difficulty)
        {
            var profile = characters.Get(gender);
            var spouse = characters.Get(gender == Gender.Male ? Gender.Female : Gender.Male);
            var run = new RunData
            {
                runId = Guid.NewGuid().ToString("N"),
                createdUtc = DateTime.UtcNow.ToString("o"),
                playerName = playerName,
                gender = gender,
                characterId = profile.id,
                difficulty = difficulty,
                day = 1,
                minuteOfDay = config.startMinuteOfDay,
                currentLocationId = config.startLocationId,
                insideLocation = true,
                hunger = GameConfig.StartingStat,
                energy = GameConfig.StartingStat,
                health = GameConfig.StartingStat,
                money = GameConfig.StartingMoney
            };
            foreach (var flag in profile.startingFlags) run.SetFlag(flag);
            if (family != null) FamilyService.CreateFamily(run, family, spouse != null ? spouse.defaultName : "Spouse");
            run.blackSheepId = FamilyService.PickBlackSheep(run, () => UnityEngine.Random.value);
            WorldService.RefreshUnlocks(run, locations);
            return run;
        }

        public RunData StartNewRun(string playerName, Gender gender, Difficulty difficulty, bool infiniteMode = false)
        {
            SaveSystem.Delete(); // hardcore: one run at a time
            CurrentRun = CreateRun(playerName, gender, difficulty);
            CurrentRun.infiniteMode = infiniteMode && SaveSystem.InfiniteModeUnlocked;
            SaveSystem.Save(CurrentRun);
            GameEvents.RaiseRunStarted(CurrentRun);
            LoadScene(SceneNames.World);
            return CurrentRun;
        }

        public bool ContinueRun()
        {
            if (!SaveSystem.TryLoad(out var run)) return false;
            // Saves from before the black sheep existed get one chosen once, then it is saved and never re-rolled.
            if (string.IsNullOrEmpty(run.blackSheepId)) run.blackSheepId = FamilyService.PickBlackSheep(run, () => UnityEngine.Random.value);
            CurrentRun = run;
            GameEvents.RaiseRunLoaded(run);
            LoadScene(SceneNames.World);
            return true;
        }

        public void ReturnToMainMenu()
        {
            SaveCurrentRun();
            CurrentRun = null;
            EventInProgress = false;
            LoadScene(SceneNames.MainMenu);
        }

        public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);

        /// <summary>Editor convenience: playing the World scene directly resumes the save or creates a throwaway run.</summary>
        public RunData EnsureRunForTesting()
        {
            if (CurrentRun != null) return CurrentRun;
            if (SaveSystem.TryLoad(out var saved)) return CurrentRun = saved;
            CurrentRun = CreateRun(characters.Get(Gender.Male).defaultName, Gender.Male, Difficulty.Normal);
            CurrentRun.runId = EditorTestRunId;
            CurrentRun.developerRun = true;
            return CurrentRun;
        }

        // ---------- World actions ----------

        public void MarkIntroSeen()
        {
            if (CurrentRun == null) return;
            CurrentRun.introSeen = true;
            SaveCurrentRun();
        }

        public void SetInsideLocation(bool inside)
        {
            if (CurrentRun == null || CurrentRun.insideLocation == inside) return;
            CurrentRun.insideLocation = inside;
            SaveCurrentRun();
            GameEvents.RaiseRunChanged(CurrentRun);
        }

        public bool Travel(LocationData destination, TravelMode mode)
        {
            if (!CanAct()) return false;
            return Apply(WorldService.Travel(CurrentRun, destination, mode, locations, config, CurrentDifficulty, family));
        }

        /// <summary>Any action card. Jobs open their minigame; intimacy opens the wheel event.</summary>
        public bool PerformAction(LocationData location, LocationAction action)
        {
            if (!CanAct()) return false;
            if (action.IsWork) return StartJob(location, action);
            if (action.kind == ActionKind.Intimacy) return StartIntimacy(location, action);
            if (action.kind == ActionKind.LeaveSlums)
            {
                var reason = WorldService.CanDo(CurrentRun, location, action, config);
                if (reason != null) { GameEvents.PostMessage(reason, true); return false; }
                return LeaveTheSlums();
            }
            return Apply(WorldService.Perform(CurrentRun, location, action, UnityEngine.Random.value, config, CurrentDifficulty, family));
        }

        /// <summary>The player takes a job: play its minigame, then get paid by performance.</summary>
        public bool StartJob(LocationData location, LocationAction action)
        {
            if (!CanAct()) return false;
            var reason = WorldService.CanDo(CurrentRun, location, action, config);
            if (reason != null)
            {
                GameEvents.PostMessage(reason, true);
                return false;
            }

            var mult = JobRules.PlayerPayMultiplier(CurrentRun, family);
            var ctx = new MinigameContext
            {
                minigameId = action.minigameId,
                jobName = action.name,
                locationName = location.displayName,
                workerName = CurrentRun.playerName,
                workerNote = CurrentRun.IsDepressed ? "You are grieving: harder and lower pay" : $"Energy {Mathf.RoundToInt(CurrentRun.energy)}",
                ease = JobRules.PlayerEase(CurrentRun, family),
                wage = action.wage,
                payMultiplier = mult,
                fullPay = JobRules.Payout(action.wage, JobRules.FullPayScore, mult),
                maxPay = JobRules.Payout(action.wage, 1f, mult),
                seed = UnityEngine.Random.Range(0, int.MaxValue)
            };
            return OpenMinigame(ctx, result =>
                Apply(WorldService.CompleteJob(CurrentRun, location, action, result.score, !result.completed,
                    UnityEngine.Random.value, config, CurrentDifficulty, family)));
        }

        /// <summary>Send a child to a job: the player plays the minigame on their behalf; the child's skills shape it.</summary>
        public bool StartChildJob(string childId, LocationData location, LocationAction action)
        {
            if (!CanAct()) return false;
            var child = family.Get(childId);
            var member = CurrentRun.Member(childId);
            var reason = FamilyService.CanAssign(CurrentRun, child, member, location, action, config, family);
            if (reason != null)
            {
                GameEvents.PostMessage($"{member?.name}: {reason}", true);
                return false;
            }

            var mult = FamilyService.ChildPayMultiplier(child, action.skill, family);
            var ctx = new MinigameContext
            {
                minigameId = action.minigameId,
                jobName = action.name,
                locationName = location.displayName,
                workerName = child.displayName,
                workerNote = $"{action.skill} {FamilyService.Stars(child.SkillValue(action.skill))} · {FamilyService.FitLabel(child, action.skill)} · Energy {Mathf.RoundToInt(member.energy)}",
                ease = FamilyService.ChildEase(child, member, action.skill),
                wage = action.wage,
                payMultiplier = mult,
                fullPay = JobRules.Payout(action.wage, JobRules.FullPayScore, mult),
                maxPay = JobRules.Payout(action.wage, 1f, mult),
                isChild = true,
                seed = UnityEngine.Random.Range(0, int.MaxValue)
            };
            return OpenMinigame(ctx, result =>
                Apply(FamilyService.CompleteChildJob(CurrentRun, family, child, member, location, action, result.score,
                    !result.completed, UnityEngine.Random.value, config, CurrentDifficulty)));
        }

        public bool FeedMember(string memberId)
        {
            if (!CanAct()) return false;
            var m = CurrentRun.Member(memberId);
            if (m == null || !m.alive) return false;
            if (!FamilyService.PlayerAtHome(CurrentRun) || !FamilyService.AtHome(CurrentRun, m))
            {
                GameEvents.PostMessage("You can only feed someone at home, when you are home too.", true);
                return false;
            }
            var report = new Survival.AdvanceReport();
            if (!FamilyService.FeedMember(CurrentRun, m, family, CurrentDifficulty, () => UnityEngine.Random.value, report))
            {
                GameEvents.PostMessage("The pantry is empty.", true);
                return false;
            }
            return Apply(new ActionResult
            {
                performed = true, message = $"You fed {m.name}. Pantry: {CurrentRun.PantryTotal} left.", warnings = report.messages
            });
        }

        /// <summary>Give a sick spouse or child medicine from the cabinet: a dose, or the instant cure. Both of you must be home.</summary>
        public bool GiveMedicine(string memberId, bool instantCure)
        {
            if (!CanAct()) return false;
            var m = CurrentRun.Member(memberId);
            if (m == null || !m.alive) return false;
            var reason = Survival.IllnessRules.IsIll(m.illness) ? null : $"{m.name} isn't sick.";
            if (reason == null && (!FamilyService.PlayerAtHome(CurrentRun) || !FamilyService.AtHome(CurrentRun, m)))
                reason = "You can only give medicine at home, when they are home too.";
            if (reason == null && (instantCure ? CurrentRun.instantCures <= 0 : CurrentRun.medicineDoses <= 0))
                reason = instantCure ? "No instant cure at home. Buy one at the pharmacy." : "No medicine at home. Buy some at the pharmacy.";
            if (reason != null)
            {
                GameEvents.PostMessage(reason, true);
                return false;
            }
            var name = Survival.IllnessRules.Name(m.illness, family)?.ToLowerInvariant();
            string message;
            if (instantCure)
            {
                CurrentRun.instantCures--;
                Survival.IllnessRules.Clear(m.illness);
                message = $"You give {m.name} the instant cure. By evening the {name} is gone.";
            }
            else
            {
                CurrentRun.medicineDoses--;
                Survival.IllnessRules.GiveDose(m.illness, CurrentRun.AbsMinute, family);
                message = $"You give {m.name} a dose of medicine. About {Survival.IllnessRules.DosesToCure(m.illness, family)} dose(s) to recover. ({CurrentRun.medicineDoses} left)";
            }
            return Apply(new ActionResult { performed = true, message = message });
        }

        private bool StartIntimacy(LocationData location, LocationAction action)
        {
            var reason = WorldService.CanDo(CurrentRun, location, action, config);
            if (reason != null)
            {
                GameEvents.PostMessage(reason, true);
                return false;
            }
            var spouse = CurrentRun.Spouse;
            var request = new WheelRequest
            {
                spouseName = spouse.name,
                playerIsInday = CurrentRun.gender == Gender.Female,
                segments = family.wheelSegments,
                pregnancyChance = family.pregnancyChance,
                pregnant = UnityEngine.Random.value < family.pregnancyChance,
                sceneText = CurrentRun.gender == Gender.Male
                    ? $"The children are finally asleep. Inday blows out the lamp and reaches for your hand. For a little while, there are no debts, no hunger, no Tondo. Only the two of you."
                    : $"The children are finally asleep. Dong blows out the lamp and reaches for your hand. For a little while, there are no debts, no hunger, no Tondo. Only the two of you."
            };
            request.onComplete = pregnant =>
            {
                EventInProgress = false;
                Apply(WorldService.ResolveIntimacy(CurrentRun, location, action, pregnant, config, CurrentDifficulty, family));
            };
            EventInProgress = true;
            if (!GameEvents.RequestWheel(request))
            {
                EventInProgress = false;
                GameEvents.PostMessage("This event can only happen in the world.", true);
                return false;
            }
            return true;
        }

        private bool OpenMinigame(MinigameContext ctx, Action<MinigameResult> onComplete)
        {
            EventInProgress = true;
            var ok = GameEvents.RequestMinigame(new MinigameRequest
            {
                context = ctx,
                onComplete = r =>
                {
                    EventInProgress = false;
                    onComplete(r);
                }
            });
            if (!ok)
            {
                EventInProgress = false;
                GameEvents.PostMessage("Jobs can only be played in the world.", true);
            }
            return ok;
        }

        private bool CanAct() => CurrentRun != null && !CurrentRun.isDead && !CurrentRun.victoryPending && !EventInProgress;

        // ---------- Winning ----------

        /// <summary>Victory screen: keep playing this run with no day goal.</summary>
        public void ContinueInInfiniteMode()
        {
            if (CurrentRun == null || !CurrentRun.victoryPending) return;
            VictoryRules.ContinueInInfiniteMode(CurrentRun);
            SaveCurrentRun();
            GameEvents.PostMessage("Infinite Mode: there is no finish line now. Keep the family alive as long as you can.");
            GameEvents.RaiseRunChanged(CurrentRun);
        }

        /// <summary>
        /// The best ending: the family leaves Tondo with the money quota saved. Offered on the victory screen and,
        /// in Infinite Mode, at home. The run ends permanently and shows its end screen.
        /// </summary>
        public bool LeaveTheSlums()
        {
            if (CurrentRun == null || EventInProgress) return false;
            var reason = VictoryRules.LeaveBlockedReason(CurrentRun, config);
            if (reason != null)
            {
                GameEvents.PostMessage(reason, true);
                return false;
            }
            VictoryRules.LeaveTheSlums(CurrentRun, config);
            SaveSystem.BuryRun(CurrentRun);
            GameEvents.RaiseRunChanged(CurrentRun);
            GameEvents.RaiseRunEnded(CurrentRun);
            return true;
        }

        /// <summary>Victory screen: end the won run for good and go back to the main menu.</summary>
        public void FinishWonRun()
        {
            if (CurrentRun == null || !CurrentRun.victoryPending) return;
            SaveSystem.BuryRun(CurrentRun);
            ReturnToMainMenu();
        }

        private bool Apply(ActionResult result)
        {
            if (!string.IsNullOrEmpty(result.message)) GameEvents.PostMessage(result.message, result.important || !result.performed);
            if (!result.performed) return false;

            if (result.warnings != null)
                foreach (var w in result.warnings) GameEvents.PostMessage(w, true);
            foreach (var loc in WorldService.RefreshUnlocks(CurrentRun, locations))
                GameEvents.PostMessage($"New place unlocked: {loc.displayName}.");

            if (VictoryRules.Check(CurrentRun, config) == EndKind.Victory)
            {
                SaveSystem.UnlockInfiniteMode(CurrentRun.runId);
                SaveCurrentRun(); // saved while the choice is pending, so quitting shows the victory screen again
                GameEvents.RaiseRunChanged(CurrentRun);
                GameEvents.RaiseRunEnded(CurrentRun);
                return true;
            }

            if (CurrentRun.isDead)
            {
                SaveSystem.BuryRun(CurrentRun);
                GameEvents.RaiseRunChanged(CurrentRun);
                GameEvents.RaiseRunEnded(CurrentRun);
                return true;
            }
            SaveCurrentRun();
            GameEvents.RaiseRunChanged(CurrentRun);
            return true;
        }
    }
}
