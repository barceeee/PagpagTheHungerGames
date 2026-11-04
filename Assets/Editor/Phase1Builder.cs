using System.Collections.Generic;
using System.IO;
using Pagpag.Characters;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Save;
using Pagpag.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static Pagpag.EditorTools.UIBuilder;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// Pagpag > Build All: regenerates placeholder art, data assets and the MainMenu,
    /// CharacterCreation and World scenes (World via <see cref="Phase2Builder"/>), then registers them in Build Settings.
    /// Safe to re-run; scenes are rebuilt from scratch, data assets are updated in place.
    /// </summary>
    public static class Phase1Builder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string DataDir = "Assets/ScriptableObjects";
        private const string ResourcesDir = "Assets/Resources";
        private const string MapSprite = "Assets/Sprites/Map/MapArt.png";

        [MenuItem("Pagpag/Build All")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            PlaceholderArt.GenerateAll();
            BuildData();
            BuildFamilyData();
            Phase2Builder.BuildLocationData();
            BuildMainMenu();
            BuildCharacterCreation();
            Phase2Builder.BuildWorldScene();
            RegisterScenes();
            // Phones and tablets play in landscape; the UI adapts to any landscape size.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorSceneManager.OpenScene($"{SceneDir}/{SceneNames.MainMenu}.unity");
            Debug.Log("[Pagpag] Game built. Press Play in the MainMenu scene.");
        }

        [MenuItem("Pagpag/Developer/Delete Current Run Save")]
        public static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log($"[Pagpag] Deleted {SaveSystem.SavePath}");
        }

        [MenuItem("Pagpag/Developer/Open Save Folder")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(SaveSystem.SavePath);

        // ---------------- Data ----------------

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void BuildData()
        {
            var dong = LoadOrCreate<CharacterProfile>($"{DataDir}/Characters/Dong.asset");
            dong.id = "dong";
            dong.gender = Gender.Male;
            dong.defaultName = "Dong";
            dong.descriptionKey = "char.dong.desc";
            dong.portrait = PlaceholderArt.LoadSprite(PlaceholderArt.CharacterDir + "/Dong_Portrait.png");
            dong.fullBody = PlaceholderArt.LoadSprite(PlaceholderArt.CharacterDir + "/Dong_Body.png");
            dong.loreKeys = new[] { "char.dong.lore.1", "char.dong.lore.2", "char.dong.lore.3" };
            dong.spouseName = "Inday";
            dong.startingFlags = new[] { "knows_mang_berto" };
            EditorUtility.SetDirty(dong);

            var inday = LoadOrCreate<CharacterProfile>($"{DataDir}/Characters/Inday.asset");
            inday.id = "inday";
            inday.gender = Gender.Female;
            inday.defaultName = "Inday";
            inday.descriptionKey = "char.inday.desc";
            inday.portrait = PlaceholderArt.LoadSprite(PlaceholderArt.CharacterDir + "/Inday_Portrait.png");
            inday.fullBody = PlaceholderArt.LoadSprite(PlaceholderArt.CharacterDir + "/Inday_Body.png");
            inday.loreKeys = new[] { "char.inday.lore.1", "char.inday.lore.2", "char.inday.lore.3" };
            inday.spouseName = "Dong";
            inday.startingFlags = new[] { "knows_aling_nena" };
            EditorUtility.SetDirty(inday);

            // Only hunger and fatigue rates differ between modes; everything else is identical.
            var normal = LoadOrCreate<DifficultySettings>($"{DataDir}/Difficulty/Normal.asset");
            normal.difficulty = Difficulty.Normal;
            normal.displayName = "Tagalog";
            normal.modeLabel = "Normal";
            normal.description = "Standard rules. Hunger and energy drain at the normal rate for you and the whole family. Recommended for your first life.";
            normal.hungerRateMultiplier = 1f;
            normal.fatigueRateMultiplier = 1f;
            EditorUtility.SetDirty(normal);

            var hard = LoadOrCreate<DifficultySettings>($"{DataDir}/Difficulty/Hard.asset");
            hard.difficulty = Difficulty.Hard;
            hard.displayName = "Bisaya";
            hard.modeLabel = "Hard";
            hard.description = "Hunger and energy drain 60% faster for you and all seven family members, and starving or collapsing hurts 60% more. Prices, wages and every other rule stay the same.";
            hard.hungerRateMultiplier = 1.6f;
            hard.fatigueRateMultiplier = 1.6f;
            EditorUtility.SetDirty(hard);

            var config = LoadOrCreate<GameConfig>($"{ResourcesDir}/GameConfig.asset");
            // Reset every tunable to the code defaults so stale values from older builds
            // (e.g. thresholds from before the 100 = best stat scale) can't linger in the asset.
            var defaults = ScriptableObject.CreateInstance<GameConfig>();
            EditorUtility.CopySerialized(defaults, config);
            Object.DestroyImmediate(defaults);
            config.name = "GameConfig";
            config.normal = normal;
            config.hard = hard;
            EditorUtility.SetDirty(config);

            var db = LoadOrCreate<CharacterDatabase>($"{ResourcesDir}/CharacterDatabase.asset");
            db.characters = new List<CharacterProfile> { dong, inday };
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
        }

        private static ChildProfile Child(string id, string name, int age, Gender gender, string role, string personality,
            string backstory, string[] traits, int str, int dex, int smarts, int charm, float appetite, float tire, float frailty)
        {
            var c = LoadOrCreate<ChildProfile>($"{DataDir}/Children/{name}.asset");
            c.id = id;
            c.displayName = name;
            c.age = age;
            c.gender = gender;
            c.role = role;
            c.personality = personality;
            c.backstory = backstory;
            c.traitTags = traits;
            c.strength = str;
            c.dexterity = dex;
            c.smarts = smarts;
            c.charm = charm;
            c.appetite = appetite;
            c.tireRate = tire;
            c.frailty = frailty;
            c.portrait = PlaceholderArt.LoadSprite(PlaceholderArt.KidPortraitPath(id));
            EditorUtility.SetDirty(c);
            return c;
        }

        /// <summary>Dong and Inday's five children and the household rules.</summary>
        private static void BuildFamilyData()
        {
            var ed = Child("ed", "Ed", 15, Gender.Male, "Eldest son",
                "Serious and protective: the second man of the house at fifteen.",
                "Ed is the first of the three Eds, all named after their father, Eduardo. He left school in Grade 9 last year to haul scrap for the junk shops so that Edd could stay in class. He never complains and never asks for anything, which worries Inday more than complaining would. He is already as strong as Dong, and he eats like it.",
                new[] { "Strong", "Big eater", "Tireless" }, 5, 3, 2, 2, 1.25f, 0.85f, 0.9f);
            var edd = Child("edd", "Edd", 13, Gender.Male, "Second son",
                "The brain of the family, and embarrassed to share a name with two brothers.",
                "Edd is top of his class and the only one who can make sense of Inday's budget notebook. He wants to be an engineer and build houses that don't flood. He tires quickly and catches every cold going around, but he can count change faster than any cashier in Tondo.",
                new[] { "Bookworm", "Quick with numbers", "Tires easily" }, 2, 3, 5, 2, 0.9f, 1.2f, 1.1f);
            var eddie = Child("eddie", "Eddie", 11, Gender.Male, "Third son",
                "Loud, funny and fearless. He could sell ice to a fisherman.",
                "By the time Eddie came along, Inday says, Dong had simply run out of names. Eddie talks his way into everything: free taho from the vendor, extra rice at the carinderia, a turn on every basketball court in the barangay. He sells ice candy after school and charms everyone he meets, just like his father.",
                new[] { "Charming", "Restless", "Always hungry" }, 2, 2, 3, 5, 1.1f, 1f, 1f);
            var joy = Child("joy", "Joy", 9, Gender.Female, "Elder daughter",
                "Quiet and careful. She follows Inday everywhere and misses nothing.",
                "Joy was born the year the floods took their first room, and Inday named her for the one thing she refused to let the water take. She hardly speaks, but her hands are never still: she folds laundry faster than Inday, sorts vegetables without being asked, and remembers where everything is.",
                new[] { "Quick hands", "Sharp memory", "Light eater" }, 1, 5, 3, 3, 0.75f, 1f, 1f);
            var pia = Child("pia", "Pia", 6, Gender.Female, "Youngest (the bunso)",
                "Sweet, sickly and everyone's favorite. Her asthma comes from the cooking smoke.",
                "Pia is the bunso, the baby of the family. Her lungs are weak from the charcoal smoke that fills the room every evening, and her inhaler is almost empty. Everyone gives her the last piece of food. She is too young for real work, but strangers can't say no to her when she hands out flyers on the main road.",
                new[] { "Sweet", "Sickly (asthma)", "Tires quickly" }, 1, 2, 2, 4, 0.65f, 1.4f, 1.6f);

            var fam = LoadOrCreate<FamilyDatabase>($"{ResourcesDir}/FamilyDatabase.asset");
            fam.children = new List<ChildProfile> { ed, edd, eddie, joy, pia };
            fam.spouseAppetite = 0.9f;
            fam.spouseTireRate = 0.8f;
            fam.spouseFrailty = 1f;
            fam.servingRestore = 35f;
            fam.autoMealHours = new[] { 7, 12, 18 };
            fam.autoMealThreshold = 75f;
            fam.pagpagSicknessChance = 0.15f;
            fam.pagpagSicknessDamage = 12f;
            fam.illness = new Pagpag.Survival.IllnessSettings(); // code defaults
            fam.childPayRate = 0.4f;
            fam.childMinEnergyToWork = 20f;
            fam.childMinHungerToWork = 20f;
            fam.depressionDays = 7;
            fam.depressionDrainMultiplier = 1.5f;
            fam.depressionRecoveryMultiplier = 0.5f;
            fam.depressionPayMultiplier = 0.6f;
            fam.depressionEaseMultiplier = 0.8f;
            fam.pregnancyChance = 0.05f;
            fam.wheelSegments = 20;
            EditorUtility.SetDirty(fam);
            AssetDatabase.SaveAssets();
        }

        // ---------------- Scenes ----------------

        internal static Canvas NewScene(string name)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return CreateSceneRoot("Canvas", new Color(0.04f, 0.05f, 0.07f));
        }

        internal static void Save(string name)
        {
            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), $"{SceneDir}/{name}.unity");
        }

        private static void BuildMainMenu()
        {
            var canvas = NewScene(SceneNames.MainMenu);
            var root = canvas.transform;

            MakeBackground(root, PlaceholderArt.LoadSprite(MapSprite), new Color(0.62f, 0.62f, 0.68f));
            AddImage(MakeStretch("Dim", root), new Color(0, 0, 0, 0.25f));
            var shade = MakeRect("LeftShade", root, MidLeft, Vector2.zero, new Vector2(1250, 0));
            shade.anchorMin = new Vector2(0, 0);
            shade.anchorMax = new Vector2(0, 1);
            shade.sizeDelta = new Vector2(1250, 0);
            AddImage(shade, Color.white, PlaceholderArt.LoadSprite(PlaceholderArt.UiDir + "/GradientLeft.png"));

            root = MakeResponsiveRoot(canvas.transform, true);

            var title = MakeLabel("Title", root, TopLeft, new Vector2(140, -70), new Vector2(1000, 200), "PAGPAG", 168, PagpagRed,
                TextAnchor.MiddleLeft, FontStyle.Bold, false, true);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            var titleOutline = title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.1f, 0.02f, 0.02f, 0.9f);
            titleOutline.effectDistance = new Vector2(4, -4);
            MakeLabel("Subtitle", root, TopLeft, new Vector2(150, -262), new Vector2(1000, 70), "THE HUNGER GAMES", 56, Cream,
                TextAnchor.MiddleLeft, FontStyle.Bold, false, true);
            MakeLabel("Tagline", root, TopLeft, new Vector2(152, -335), new Vector2(1000, 50), "menu.tagline", 32,
                new Color(0.85f, 0.79f, 0.64f), TextAnchor.MiddleLeft, FontStyle.Italic, true, true);

            var newGame = MakeButton("NewGameButton", root, TopLeft, new Vector2(150, -450), new Vector2(480, 86), "menu.newgame", 36);
            var cont = MakeButton("ContinueButton", root, TopLeft, new Vector2(150, -556), new Vector2(480, 104), "menu.continue", 36);
            var contLabel = (RectTransform)cont.transform.Find("Label");
            contLabel.offsetMin = new Vector2(8, 34);
            var contDetail = AddText(MakeRect("Detail", cont.transform, BottomCenter, new Vector2(0, 8), new Vector2(440, 32)),
                "", 22, Gold, TextAnchor.MiddleCenter, FontStyle.Normal, false);
            var quit = MakeButton("QuitButton", root, TopLeft, new Vector2(150, -680), new Vector2(480, 86), "menu.quit", 36);

            MakeLabel("Goal", root, BottomLeft, new Vector2(150, 110), new Vector2(1000, 60), "menu.goal", 26, Cream, shadow: true);
            MakeLabel("Hardcore", root, BottomLeft, new Vector2(150, 64), new Vector2(1000, 44), "menu.hardcore", 26, Danger,
                TextAnchor.MiddleLeft, FontStyle.Bold, true, true);
            MakeLabel("Version", root, BottomRight, new Vector2(-40, 30), new Vector2(600, 36), "v0.2 · Prototype", 20,
                new Color(1, 1, 1, 0.5f), TextAnchor.MiddleRight, FontStyle.Normal, false);

            // Confirmation overlay
            var confirm = MakeStretch("ConfirmPanel", root);
            AddImage(confirm, new Color(0, 0, 0, 0.78f), null, true);
            var box = MakePanel("Dialog", confirm, Center, Vector2.zero, new Vector2(860, 400));
            MakeLabel("Title", box.transform, TopCenter, new Vector2(0, -30), new Vector2(780, 60), "menu.confirm.title", 38, Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var body = MakeLabel("Body", box.transform, Center, new Vector2(0, 20), new Vector2(760, 150), "", 28, Cream,
                TextAnchor.MiddleCenter, FontStyle.Normal, false);
            var yes = MakeButton("YesButton", box.transform, BottomCenter, new Vector2(-170, 40), new Vector2(280, 76), "common.yes", 32,
                bg: new Color(0.55f, 0.16f, 0.16f));
            var no = MakeButton("NoButton", box.transform, BottomCenter, new Vector2(170, 40), new Vector2(280, 76), "common.no", 32);
            confirm.gameObject.SetActive(false);

            var ctrl = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            SetField(ctrl, "newGameButton", newGame);
            SetField(ctrl, "continueButton", cont);
            SetField(ctrl, "quitButton", quit);
            SetField(ctrl, "continueDetailText", contDetail);
            SetField(ctrl, "confirmPanel", confirm.gameObject);
            SetField(ctrl, "confirmBodyText", body);
            SetField(ctrl, "confirmYesButton", yes);
            SetField(ctrl, "confirmNoButton", no);

            Save(SceneNames.MainMenu);
        }

        private static void BuildCharacterCreation()
        {
            var canvas = NewScene(SceneNames.CharacterCreation);
            var root = canvas.transform;

            MakeBackground(root, PlaceholderArt.LoadSprite(MapSprite), new Color(0.5f, 0.5f, 0.55f));
            AddImage(MakeStretch("Dim", root), new Color(0.02f, 0.03f, 0.05f, 0.8f));
            root = MakeResponsiveRoot(canvas.transform, true);

            MakeLabel("Title", root, TopCenter, new Vector2(0, -40), new Vector2(1200, 80), "cc.title", 58, Cream,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, true);
            MakeLabel("HardcoreNote", root, TopCenter, new Vector2(0, -122), new Vector2(1200, 40), "cc.hardcore.note", 26, Danger,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Preview (left)
            var preview = MakePanel("PreviewPanel", root, MidLeft, new Vector2(100, -10), new Vector2(620, 700)).transform;
            var body = MakeRect("CharacterPreview", preview, Center, new Vector2(0, 50), new Vector2(330, 560));
            var bodyImg = AddImage(body, Color.white);
            bodyImg.preserveAspect = true;
            body.pivot = new Vector2(0.5f, 0f);
            body.anchoredPosition = new Vector2(0, -230);
            body.gameObject.AddComponent<IdleBob>();
            var previewName = MakeLabel("PreviewName", preview, BottomCenter, new Vector2(0, 30), new Vector2(560, 70), "", 46, Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold, false, true);

            // Options (right)
            var opts = MakePanel("OptionsPanel", root, MidRight, new Vector2(-100, -10), new Vector2(1020, 700)).transform;
            MakeLabel("ChooseLabel", opts, TopLeft, new Vector2(40, -20), new Vector2(600, 40), "cc.choose", 28, Gold, style: FontStyle.Bold);
            MakeLabel("CoupleNote", opts, TopLeft, new Vector2(40, -58), new Vector2(940, 54), "cc.couple", 19,
                new Color(0.85f, 0.79f, 0.64f), TextAnchor.UpperLeft, FontStyle.Italic);

            var male = CharacterOption(opts, "MaleOption", new Vector2(40, -118), "Dong", out var malePortrait);
            var female = CharacterOption(opts, "FemaleOption", new Vector2(360, -118), "Inday", out var femalePortrait);
            var desc = MakeLabel("Description", opts, TopLeft, new Vector2(680, -118), new Vector2(300, 150), "", 19, Cream,
                TextAnchor.UpperLeft, FontStyle.Italic, false);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 12;
            desc.resizeTextMaxSize = 19;
            var lore = MakeButton("LoreButton", opts, TopLeft, new Vector2(680, -274), new Vector2(300, 44), "", 22, false);

            MakeLabel("NameLabel", opts, TopLeft, new Vector2(40, -336), new Vector2(400, 40), "cc.name", 28, Gold, style: FontStyle.Bold);
            var nameInput = MakeInput("NameInput", opts, TopLeft, new Vector2(40, -376), new Vector2(560, 64), "cc.name.placeholder");
            var nameError = MakeLabel("NameError", opts, TopLeft, new Vector2(620, -376), new Vector2(360, 64), "", 20, Danger,
                TextAnchor.MiddleLeft, FontStyle.Normal, false);

            MakeLabel("DifficultyLabel", opts, TopLeft, new Vector2(40, -458), new Vector2(400, 40), "cc.difficulty", 28, Gold, style: FontStyle.Bold);
            var normalOpt = MakeOption("NormalOption", opts, TopLeft, new Vector2(40, -498), new Vector2(450, 64), "", 28, false);
            var hardOpt = MakeOption("HardOption", opts, TopLeft, new Vector2(510, -498), new Vector2(450, 64), "", 28, false);
            var diffDesc = MakeLabel("DifficultyDescription", opts, TopLeft, new Vector2(40, -574), new Vector2(940, 60), "", 21, Cream,
                TextAnchor.UpperLeft, FontStyle.Normal, false);
            var money = MakeLabel("StartingMoney", opts, TopLeft, new Vector2(40, -632), new Vector2(450, 52), "", 19,
                new Color(0.55f, 0.85f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold, false);
            money.resizeTextForBestFit = true;
            money.resizeTextMinSize = 14;
            money.resizeTextMaxSize = 19;
            var infiniteOpt = MakeOption("InfiniteOption", opts, TopLeft, new Vector2(510, -632), new Vector2(450, 52), "", 22, false);
            var infLabel = infiniteOpt.Label;
            infLabel.resizeTextForBestFit = true;
            infLabel.resizeTextMinSize = 14;
            infLabel.resizeTextMaxSize = 22;

            var back = MakeButton("BackButton", root, BottomLeft, new Vector2(100, 50), new Vector2(300, 80), "common.back", 32);
            var start = MakeButton("StartButton", root, BottomRight, new Vector2(-100, 50), new Vector2(480, 80), "cc.start", 34,
                bg: Gold, textColor: new Color(0.08f, 0.08f, 0.1f));

            // Lore overlay
            var lorePanel = MakeStretch("LorePanel", root);
            AddImage(lorePanel, new Color(0, 0, 0, 0.82f), null, true);
            var loreBox = MakePanel("Dialog", lorePanel, Center, Vector2.zero, new Vector2(1180, 820));
            var loreTitle = MakeLabel("Title", loreBox.transform, TopCenter, new Vector2(0, -26), new Vector2(1080, 60), "", 40, Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold, false);
            var loreBody = MakeLabel("Body", loreBox.transform, TopCenter, new Vector2(0, -100), new Vector2(1080, 590), "", 24, Cream,
                TextAnchor.UpperLeft, FontStyle.Normal, false);
            loreBody.resizeTextForBestFit = true;
            loreBody.resizeTextMinSize = 16;
            loreBody.resizeTextMaxSize = 24;
            loreBody.lineSpacing = 1.1f;
            var loreClose = MakeButton("CloseButton", loreBox.transform, BottomCenter, new Vector2(0, 30), new Vector2(280, 70), "common.close", 30);
            lorePanel.gameObject.SetActive(false);

            var ctrl = new GameObject("CharacterCreationController").AddComponent<CharacterCreationController>();
            SetField(ctrl, "maleOption", male);
            SetField(ctrl, "femaleOption", female);
            SetField(ctrl, "malePortrait", malePortrait);
            SetField(ctrl, "femalePortrait", femalePortrait);
            SetField(ctrl, "previewImage", bodyImg);
            SetField(ctrl, "previewNameText", previewName);
            SetField(ctrl, "descriptionText", desc);
            SetField(ctrl, "nameInput", nameInput);
            SetField(ctrl, "nameErrorText", nameError);
            SetField(ctrl, "normalOption", normalOpt);
            SetField(ctrl, "hardOption", hardOpt);
            SetField(ctrl, "difficultyDescriptionText", diffDesc);
            SetField(ctrl, "startingMoneyText", money);
            SetField(ctrl, "infiniteOption", infiniteOpt);
            SetField(ctrl, "loreButton", lore);
            SetField(ctrl, "loreButtonText", lore.GetComponentInChildren<Text>());
            SetField(ctrl, "lorePanel", lorePanel.gameObject);
            SetField(ctrl, "loreTitleText", loreTitle);
            SetField(ctrl, "loreBodyText", loreBody);
            SetField(ctrl, "loreCloseButton", loreClose);
            SetField(ctrl, "startButton", start);
            SetField(ctrl, "backButton", back);

            Save(SceneNames.CharacterCreation);
        }

        private static OptionToggle CharacterOption(Transform parent, string name, Vector2 pos, string displayName, out Image portrait)
        {
            var opt = MakeOption(name, parent, TopLeft, pos, new Vector2(300, 200), displayName, 32, false);
            var label = (RectTransform)opt.transform.Find("Label");
            label.anchorMin = new Vector2(0, 0);
            label.anchorMax = new Vector2(1, 0);
            label.pivot = new Vector2(0.5f, 0);
            label.offsetMin = new Vector2(8, 6);
            label.offsetMax = new Vector2(-8, 50);
            var frame = MakeRect("PortraitFrame", opt.transform, TopCenter, new Vector2(0, -12), new Vector2(136, 136));
            AddImage(frame, new Color(0.05f, 0.05f, 0.07f));
            var pic = MakeRect("Portrait", frame, Center, Vector2.zero, new Vector2(128, 128));
            portrait = AddImage(pic, Color.white);
            return opt;
        }

        private static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{SceneDir}/{SceneNames.MainMenu}.unity", true),
                new EditorBuildSettingsScene($"{SceneDir}/{SceneNames.CharacterCreation}.unity", true),
                new EditorBuildSettingsScene($"{SceneDir}/{SceneNames.World}.unity", true)
            };
        }
    }
}
