using System.Collections.Generic;
using Pagpag.Core;
using Pagpag.Family;
using Pagpag.Locations;
using Pagpag.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Pagpag.EditorTools.UIBuilder;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// Phase 2: location data assets and the World scene (map, location screens, HUD, intro, game over).
    /// Called from Pagpag > Build All, or on its own from Pagpag > Build World Only.
    /// </summary>
    public static class Phase2Builder
    {
        private const string LocationDir = "Assets/ScriptableObjects/Locations";
        private const string MapSprite = "Assets/Sprites/Map/MapArt.png";
        private static readonly Vector2 MapSize = new Vector2(1250, 910);

        [MenuItem("Pagpag/Build World Only")]
        public static void BuildWorldOnly()
        {
            BuildLocationData();
            BuildWorldScene();
        }

        // =====================================================================
        // Location data
        // =====================================================================

        /// <param name="hungerCost">Hunger drained by the shift (positive number).</param>
        /// <param name="energyCost">Energy drained by the shift (positive number).</param>
        private static LocationAction Work(string id, string name, string description, int minutes, int wage, float hungerCost, float energyCost,
            int from, int to, string character, string result)
            => new LocationAction
            {
                id = id, name = name, description = description, kind = ActionKind.Work, durationMinutes = minutes, wage = wage,
                hunger = -Mathf.Abs(hungerCost), energy = -Mathf.Abs(energyCost), startHourFrom = from, startHourTo = to, oncePerDay = true,
                character = character, setsFlag = "worked_any", resultMessage = result
            };

        /// <summary>Attach the job's minigame, the skill it tests, and the youngest child age allowed (0 = adults only).</summary>
        private static LocationAction Game(this LocationAction a, string minigame, Skill skill, int childMinAge)
        {
            a.minigameId = minigame;
            a.skill = skill;
            a.childMinAge = childMinAge;
            return a;
        }

        private static LocationAction Provision(string id, string name, string description, int cost, int servings, bool pagpag, string result,
            int from = 0, int to = 24)
            => new LocationAction
            {
                id = id, name = name, description = description, kind = ActionKind.Provision, cost = cost, durationMinutes = 10,
                servings = servings, pagpagServings = pagpag, startHourFrom = from, startHourTo = to, resultMessage = result
            };

        /// <param name="restore">Hunger restored (sign ignored).</param>
        private static LocationAction Food(string id, string name, string description, int cost, int minutes, float restore, float health,
            string result, int from = 0, int to = 24, string character = null)
            => new LocationAction
            {
                id = id, name = name, description = description, kind = ActionKind.Eat, cost = cost, durationMinutes = minutes,
                hunger = Mathf.Abs(restore), health = health, startHourFrom = from, startHourTo = to, character = character, resultMessage = result
            };

        private static LocationData Location(string id, string name, string description, Rect label, Vector2 center, int open, int close,
            params LocationAction[] actions)
        {
            var loc = Phase1Builder.LoadOrCreate<LocationData>($"{LocationDir}/{id}.asset");
            loc.id = id;
            loc.displayName = name;
            loc.description = description;
            loc.mapLabelRect = label;
            loc.sceneCenter = center;
            loc.sceneZoom = 0.36f;
            loc.openHour = open;
            loc.closeHour = close;
            loc.unlock = new UnlockCondition();
            loc.actions = new List<LocationAction>(actions);
            return loc;
        }

        private static void Lock(LocationData loc, string flag, int minDay, string hint)
        {
            loc.unlock = new UnlockCondition { lockedAtStart = true, requiredFlag = flag, minDay = minDay, hint = hint };
        }

        public static void BuildLocationData()
        {
            const string dong = "dong", inday = "inday";

            var home = Location("home", "Your Room",
                "A rented 2-by-3-meter room on stilts above the water, home to Dong, Inday and their five children. ₱1,800 a month, a leaking roof, one electric fan, and the only place you can sleep in peace.",
                new Rect(90, 487, 162, 45), new Vector2(180, 590), 0, 24,
                new LocationAction
                {
                    id = "sleep", name = "Sleep (8 hours)", kind = ActionKind.Sleep, durationMinutes = 480,
                    description = "Lie down on the thin foam mattress and sleep the night through.",
                    resultMessage = "You wake up stiff, but rested."
                },
                new LocationAction
                {
                    id = "nap", name = "Nap (2 hours)", kind = ActionKind.Sleep, durationMinutes = 120,
                    description = "A short rest in the heat of the day.", resultMessage = "The nap takes the edge off."
                },
                Food("cook_dong", "Cook rice and instant noodles", "Dong's way of cooking: a pot of rice stretched with a pack of noodles. Fast and filling.",
                    30, 30, -32, 0, "You eat straight from the pot. It's enough.", character: dong),
                Food("cook_inday", "Cook rice and dried fish (tuyo)", "Inday cooks the way her mother taught her. Cheap, filling and done properly.",
                    40, 45, -45, 2, "Rice, tuyo and a little vinegar. It tastes like home.", character: inday),
                new LocationAction
                {
                    id = "talk_wife", name = "Talk with Inday", kind = ActionKind.Talk, durationMinutes = 20, energy = 6, health = 1,
                    oncePerDay = true, character = dong, requiresSpouse = true, description = "Spend a few minutes with your wife.",
                    resultMessage = "Inday goes over the budget with you, then makes you laugh about the landlord's rooster. For a moment, things feel manageable."
                },
                new LocationAction
                {
                    id = "talk_husband", name = "Talk with Dong", kind = ActionKind.Talk, durationMinutes = 20, energy = 6, health = 1,
                    oncePerDay = true, character = inday, requiresSpouse = true, description = "Spend a few minutes with your husband.",
                    resultMessage = "Dong tells you about his day in far too much detail and promises things will get better. You almost believe him, and you feel lighter."
                },
                new LocationAction
                {
                    id = "family_meal", name = "Serve a family meal", kind = ActionKind.FamilyMeal, durationMinutes = 30,
                    description = "Feed everyone at home who is hungry, you included: one pantry serving each, youngest first."
                },
                new LocationAction
                {
                    id = "pantry_meal", name = "Eat from the pantry", kind = ActionKind.PantryMeal, durationMinutes = 10,
                    description = "Take one serving from the family pantry for yourself.",
                    resultMessage = "You eat quickly, standing up."
                },
                new LocationAction
                {
                    id = "intimacy", name = "A quiet night together", kind = ActionKind.Intimacy, durationMinutes = 60,
                    energy = 5, health = 2, oncePerDay = true, requiresSpouse = true, startHourFrom = 20, startHourTo = 2,
                    description = "When the children are asleep. Fades to black, then the wheel decides: a 5% chance of a sixth child, which the family could not survive.",
                    resultMessage = "A rare, quiet night. You both feel a little lighter."
                },
                new LocationAction
                {
                    // Only shown while the player is sick (see WorldService.IsVisible).
                    id = "take_dose", name = "Take medicine", kind = ActionKind.TakeMedicine, durationMinutes = 10,
                    description = "Take one dose from the medicine cabinet. For a day the illness eases instead of getting worse."
                },
                new LocationAction
                {
                    id = "take_cure", name = "Take the instant cure", kind = ActionKind.TakeMedicine, instantCure = true, durationMinutes = 10,
                    description = "Use the expensive instant cure from the cabinet. Any illness is gone within the hour."
                },
                new LocationAction
                {
                    // Infinite Mode only (see WorldService.IsVisible); the quota is GameConfig.leaveSlumsQuota.
                    id = "leave_slums", name = "Leave the slums", kind = ActionKind.LeaveSlums, durationMinutes = 0,
                    description = "Pack up the family and leave Tondo for good. You need ₱100,000 saved. This ends the run.",
                    resultMessage = "You leave the slums behind."
                });

            var street = Location("street", "Neighborhood Street",
                "Narrow alleys of patched tin roofs, laundry lines and kids playing basketball at a rim nailed to a post. Everyone knows everyone's business here.",
                new Rect(530, 178, 142, 42), new Vector2(600, 270), 5, 23,
                Work("recyclables", "Collect recyclables (kalakal)", "Dig bottles, cans and cardboard out of the canals and sell them at the junk shop.",
                    120, 70, 5, 14, 6, 20, dong, "You sell a sack of bottles and cardboard at the junk shop.").Game("sort_recyclables", Skill.Smarts, 8),
                Work("laundry", "Wash the neighbors' laundry", "Scrub other families' clothes by hand at the communal faucet.",
                    180, 120, 6, 16, 6, 15, inday, "Your hands are raw, but three households pay you.").Game("scrub_laundry", Skill.Dexterity, 9),
                new LocationAction
                {
                    id = "chat", name = "Chat with the neighbors", kind = ActionKind.Talk, durationMinutes = 30, oncePerDay = true,
                    description = "Catch up with the neighbors on the corner bench.", setsFlag = "met_neighbors",
                    resultMessage = "Aling Rosa mentions that the Community Center gives out free lugaw every morning. \"Tell them I sent you.\""
                });

            var pagpag = Location("pagpag", "Pagpag Stall",
                "Mang Itoy's stall sells pagpag: leftover chicken and rice salvaged from fast-food bins, washed and recooked. Cheap and filling. Not always safe.",
                new Rect(396, 418, 149, 44), new Vector2(480, 500), 16, 24,
                new LocationAction
                {
                    id = "pagpag_chicken", name = "Recooked pagpag chicken", kind = ActionKind.Eat, cost = 25, durationMinutes = 15, hunger = 35,
                    description = "Fried again in old oil. The cheapest way to fill your stomach.",
                    resultMessage = "Salty, greasy and filling.",
                    riskChance = 0.15f, riskHealth = 12, riskEnergy = 8, riskCause = "Food poisoning",
                    riskMessage = "Your stomach turns violently. That batch had gone bad. (Health −12)"
                },
                new LocationAction
                {
                    id = "pagpag_rice", name = "Pagpag rice bowl", kind = ActionKind.Eat, cost = 15, durationMinutes = 10, hunger = 20,
                    description = "Leftover rice with scraps of meat and sauce.",
                    resultMessage = "It's not much, but it quiets your stomach.",
                    riskChance = 0.08f, riskHealth = 8, riskEnergy = 5, riskCause = "Food poisoning",
                    riskMessage = "Something in it was spoiled. You spend an hour doubled over. (Health −8)"
                },
                Provision("pagpag_family", "Pagpag bundle for the family", "A plastic bag of recooked leftovers. Feeds a lot of people for very little, but each serving has a 15% chance of making someone sick.",
                    70, 6, true, "Mang Itoy fills a bag for the kids."));

            var market = Location("market", "Public Market",
                "A maze of wet stalls under tarpaulin: fish, vegetables, rice by the kilo, and carinderias serving hot meals to vendors and porters.",
                new Rect(612, 297, 163, 43), new Vector2(700, 420), 5, 19,
                Food("carinderia", "Carinderia meal (rice and adobo)", "Rice, adobo and a bowl of soup at a carinderia.", 70, 30, -55, 3,
                    "A real meal. You feel human again."),
                Food("rice_veg", "Rice and vegetables (gulay)", "Rice with a ladle of chopsuey or monggo.", 45, 30, -40, 2,
                    "Simple and warm."),
                Work("haul_crates", "Haul crates for the vendors", "Carry crates of produce from the delivery trucks to the stalls.",
                    180, 180, 8, 24, 5, 10, dong, "Your shoulders burn, but the vendors pay in cash.").Game("haul_balance", Skill.Strength, 13),
                Work("sell_veg", "Help Aling Nena sell vegetables", "Arrange, weigh and sell vegetables at Aling Nena's stall.",
                    240, 200, 7, 14, 6, 11, inday, "You sell vegetables until the morning crowd thins out.").Game("weigh_veg", Skill.Charm, 9),
                Work("bundle_veg", "Sort and bundle vegetables", "Trim, sort and tie tomorrow's vegetables into bundles for the vendors.",
                    180, 150, 5, 10, 13, 16, inday, "Rows of neat bundles, ready for the morning.").Game("bundle_match", Skill.Dexterity, 6),
                Provision("market_family", "Rice and viand for the family", "Two kilos of rice and a big bag of vegetables and pork. The best value for a household.",
                    240, 10, false, "You carry home enough for everyone.", 5, 19));
            market.actions[3].contactFlag = "knows_aling_nena";
            market.actions[3].contactMessage = "Aling Nena slips you an extra bunch of kangkong \"for Dong.\" You sell out before noon.";

            var sarisari = Location("sarisari", "Sari-Sari Store",
                "Aling Pacing's little store sells everything in single servings: one egg, one sachet of coffee, one cigarette. She keeps a list of who owes her.",
                new Rect(1000, 319, 160, 45), new Vector2(1070, 410), 6, 22,
                Food("pandesal", "Pandesal (5 pieces)", "Bread rolls, still warm in the morning.", 15, 10, -15, 0, "You eat them on the bench outside."),
                Food("canton", "Instant pancit canton", "A pack of instant noodles. Aling Pacing lets you use her hot water.", 20, 15, -20, 0, "Salty and quick."),
                Food("sardines", "Sardines and rice", "A can of sardines in tomato sauce over a cup of rice.", 40, 20, -35, 1, "Cheap protein. Not bad at all."),
                Work("mind_store", "Mind the store for Aling Pacing", "Watch the counter and keep the debt list while Aling Pacing runs errands.",
                    180, 130, 5, 8, 12, 18, inday, "Aling Pacing counts the till, nods, and pays you.").Game("make_change", Skill.Smarts, 11),
                Work("unload_store", "Unload store deliveries", "Carry cases of softdrinks and sacks of rice from the delivery truck.",
                    60, 70, 4, 12, 8, 16, dong, "Twenty cases of softdrinks later, Aling Pacing pays you.").Game("catch_cases", Skill.Strength, 12),
                Provision("sari_family", "Rice and canned goods for the family", "A kilo of rice and two cans of sardines. Pricier per serving than the market, but open late.",
                    110, 4, false, "Aling Pacing adds a pack of crackers for the bunso."));

            var construction = Location("construction", "Construction Site",
                "A four-story building going up on Mang Berto's watch. Hollow blocks, rebar and cement mixers. Workers are paid in cash at the end of each shift.",
                new Rect(622, 555, 184, 45), new Vector2(720, 680), 6, 17,
                Work("con_morning", "Morning shift: carry hollow blocks", "Haul hollow blocks up the scaffolding. Must start by 9 AM.",
                    240, 260, 10, 30, 6, 9, dong, "You haul hollow blocks up the scaffolding until noon.").Game("hollow_blocks", Skill.Strength, 0),
                Work("con_afternoon", "Afternoon shift: mix cement", "Shovel sand, gravel and cement into the mixer. Must start by 2 PM.",
                    240, 240, 10, 28, 12, 14, dong, "Your arms are grey with cement dust by the end of the shift.").Game("mix_cement", Skill.Strength, 0),
                Work("con_cook", "Cook lunch for the crew", "Help the site cook prepare rice and viand for thirty workers.",
                    180, 170, 4, 12, 8, 11, inday, "The crew eats everything. Mang Berto asks how Dong is holding up.").Game("cook_sequence", Skill.Smarts, 0));
            construction.actions[0].contactFlag = "knows_mang_berto";
            construction.actions[0].contactMessage = "Mang Berto claps you on the back. \"Your father carried twice that.\" You finish the shift and get paid in cash.";

            var employment = Location("employment", "Employment Office",
                "The Public Employment Service Office. Long lines, ceiling fans, and a job board full of postings asking for \"college graduate, 18 to 25, pleasing personality.\"",
                new Rect(948, 464, 177, 43), new Vector2(1020, 560), 8, 17,
                new LocationAction
                {
                    id = "register", name = "Register as a job seeker", kind = ActionKind.Service, cost = 100, durationMinutes = 60,
                    description = "Pay for ID photos and forms, then register. Registered workers can apply for jobs at the port.",
                    setsFlag = "registered_jobseeker", hiddenIfFlag = "registered_jobseeker",
                    resultMessage = "Your registration is stamped. The clerk says the port is hiring through an agency."
                },
                new LocationAction
                {
                    id = "job_board", name = "Read the job board", kind = ActionKind.Talk, durationMinutes = 20,
                    description = "See who is hiring.",
                    resultMessage = "A warehouse is hiring regular workers with a solid work record. Keep working, and keep your record clean."
                });

            var fish = Location("fishmarket", "Fish Market",
                "The fish landing before dawn: bancas unloading tubs of galunggong, ice everywhere, and buyers shouting prices.",
                new Rect(216, 208, 160, 44), new Vector2(300, 300), 3, 14,
                Work("fish_porter", "Fish porter", "Carry tubs of fish and ice from the boats to the buyers. Starts before dawn.",
                    180, 210, 8, 26, 3, 8, dong, "You smell of fish and ice water, but you're paid on the spot.").Game("fish_relay", Skill.Strength, 14),
                Work("fish_clean", "Clean and gut fish", "Scale and gut fish for the vendors, fast and careful.",
                    180, 190, 6, 16, 4, 9, inday, "Your knife work impresses the vendors. One asks you to come back tomorrow.").Game("gut_fish", Skill.Dexterity, 11),
                Food("grilled_fish", "Grilled fish and rice", "Fresh galunggong grilled over coals.", 55, 30, -50, 3, "Fresh fish. A small luxury.", 5, 14),
                Provision("fish_family", "Fish and rice for the family", "A tub of galunggong straight off the boat and a kilo of rice.",
                    200, 8, false, "Fresh fish for everyone tonight.", 5, 14));
            Lock(fish, "worked_any", 1, "Do any paid work first. Word gets around that you're willing to work.");

            var port = Location("port", "Port",
                "North Harbor. Container cranes, forklifts and agency recruiters choosing day laborers at the gate.",
                new Rect(95, 88, 140, 47), new Vector2(170, 150), 6, 18,
                Work("cargo", "Cargo loading shift", "Load cargo on the ships. Heavy, dangerous, and the best day rate on the waterfront.",
                    360, 480, 14, 42, 6, 9, dong, "You load cargo where your father used to work. The forklifts do the rest of what he used to do.").Game("crane_drop", Skill.Strength, 0),
                Work("canteen", "Work the port canteen", "Cook and serve meals to dockworkers on a long shift.",
                    300, 340, 6, 22, 6, 10, inday, "Two hundred dockworkers fed. A few of them knew Dong.").Game("serve_line", Skill.Charm, 0));
            port.actions[0].riskChance = 0.05f;
            port.actions[0].riskHealth = 18;
            port.actions[0].riskCause = "Injury";
            port.actions[0].riskMessage = "A crate swings loose and slams into your side. (Health −18)";
            Lock(port, "registered_jobseeker", 1, "Register as a job seeker at the Employment Office.");

            var community = Location("community", "Community Center",
                "The barangay hall and covered court. A feeding program in the morning, meetings in the afternoon, and a bulletin board of odd jobs.",
                new Rect(868, 127, 194, 43), new Vector2(930, 230), 7, 17,
                Food("lugaw", "Free lugaw (feeding program)", "Rice porridge from the barangay feeding program. Mornings only.", 0, 30, -30, 1,
                    "Warm lugaw with a boiled egg. The volunteers ask about your family.", 7, 10),
                Work("cleanup", "Volunteer cleanup", "Sweep and clean the covered court for a small stipend.",
                    120, 100, 5, 12, 13, 15, null, "The court is clean, and the barangay secretary hands you an envelope.").Game("sweep_court", Skill.Dexterity, 8),
                Provision("lugaw_family", "Extra lugaw for the children", "The volunteers fill a pot for the kids at home. Free, mornings only, once a day.",
                    0, 3, false, "\"For the little ones,\" the volunteer says.", 7, 10));
            community.actions[0].oncePerDay = true;
            community.actions[2].oncePerDay = true;
            Lock(community, "met_neighbors", 1, "Chat with the neighbors on the Neighborhood Street.");

            var train = Location("train", "Train Station",
                "The last elevated railway stop before the slums. Commuters, vendors, and parcel couriers running between trains.",
                new Rect(1068, 47, 154, 48), new Vector2(1150, 130), 6, 22,
                Work("parcels", "Parcel delivery runs", "Run parcels from the station to shops around the district.",
                    240, 300, 10, 28, 7, 14, dong, "Fourteen deliveries and two angry dogs later, you're paid.").Game("parcel_route", Skill.Smarts, 11),
                Work("kiosk", "Food kiosk helper", "Serve siopao and coffee to commuters at the station kiosk.",
                    240, 270, 7, 16, 7, 14, inday, "The rush hour crowd is endless, but the owner pays well.").Game("siopao_steam", Skill.Dexterity, 13));
            Lock(train, null, 4, "The station contractors only hire faces they've seen around. Available from Day 4.");

            var mainroad = Location("mainroad", "Main Road",
                "The highway where jeepneys, tricycles and trucks fight for space. Vendors sell taho and balut along the sidewalk.",
                new Rect(1090, 638, 142, 44), new Vector2(1100, 730), 0, 24,
                Food("taho", "Taho", "Silken tofu with syrup and sago, from the vendor's buckets.", 20, 10, -12, 0, "Sweet and warm.", 6, 11),
                Food("balut", "Balut", "A boiled duck egg with salt and vinegar.", 25, 10, -18, 1, "Rich and filling.", 18, 24),
                Work("flyers", "Hand out flyers", "Pass out flyers for a lending company to passing commuters.",
                    120, 90, 5, 10, 8, 16, null, "Most people ignore you, but the agent pays anyway.").Game("hand_flyers", Skill.Charm, 6));

            var pharmacy = Location("pharmacy", "Pharmacy",
                "Ate Grace's small botika by the road: a counter, a glass case of medicine, and a licensed pharmacist who tells you the truth about prices.",
                new Rect(700, 668, 160, 44), new Vector2(780, 760), 8, 21,
                new LocationAction
                {
                    id = "pharmacist", name = "Ask the pharmacist", kind = ActionKind.Consult, durationMinutes = 15,
                    description = "Ate Grace checks who in the family is sick, how bad it is, and how much medicine they need.",
                    resultMessage = "Ate Grace writes it down for you."
                },
                new LocationAction
                {
                    id = "buy_dose", name = "Generic medicine (1 dose)", kind = ActionKind.BuyMedicine, cost = 120, servings = 1, durationMinutes = 10,
                    description = "A day's dose of generic medicine for the cabinet at home. Each dose makes an illness ease for a day; a mild one needs one or two.",
                    resultMessage = "Ate Grace wraps the tablets in paper and writes the dosage on it."
                },
                new LocationAction
                {
                    id = "buy_cure", name = "Instant cure (branded)", kind = ActionKind.BuyMedicine, instantCure = true, cost = 2500, durationMinutes = 10,
                    description = "The expensive branded treatment. Cures any illness at once, for anyone in the family. Kept in the cabinet until you use it.",
                    resultMessage = "Ate Grace hands you a small sealed box. \"Keep it cool. Use it only if you must.\""
                });

            var all = new List<LocationData> { home, street, pagpag, market, sarisari, construction, employment, mainroad, fish, port, community, train, pharmacy };
            Phase2Dialog.Apply(all);
            foreach (var l in all) EditorUtility.SetDirty(l);

            var db = Phase1Builder.LoadOrCreate<LocationDatabase>("Assets/Resources/LocationDatabase.asset");
            db.mapArt = PlaceholderArt.LoadSprite(MapSprite);
            db.mapSize = MapSize;
            db.locations = all;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        // World scene
        // =====================================================================

        private static Sprite Rounded => PlaceholderArt.LoadSprite(PlaceholderArt.RoundedSprite);
        private static Sprite Circle => PlaceholderArt.LoadSprite(PlaceholderArt.CircleSprite);
        private static Sprite Ring => PlaceholderArt.LoadSprite(PlaceholderArt.RingSprite);

        private static Image RoundedImage(RectTransform rt, Color color, bool raycast = false)
        {
            var img = AddImage(rt, color, Rounded, raycast);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            return img;
        }

        private static void SetList<T>(Object target, string field, IList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Region(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = MakeStretch(name, parent);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static void BuildWorldScene()
        {
            var db = AssetDatabase.LoadAssetAtPath<LocationDatabase>("Assets/Resources/LocationDatabase.asset");
            var canvas = Phase1Builder.NewScene(SceneNames.World);
            AddImage(MakeStretch("Backdrop", canvas.transform), new Color(0.04f, 0.05f, 0.07f));
            var root = MakeResponsiveRoot(canvas.transform, false);

            // ---- Main area: the whole width below the HUD (locations are a drawer over it) ----
            var area = MakeStretch("MainArea", root);

            // Map view
            var mapView = MakeStretch("MapView", area);
            var mapRt = MakeStretch("Map", mapView, 6);
            var mapImg = AddImage(mapRt, Color.white, db.mapArt);
            var fitter = mapRt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = MapSize.x / MapSize.y;

            var hotspots = new List<MapHotspot>();
            foreach (var loc in db.locations) hotspots.Add(BuildHotspot(mapRt, loc));

            // Legend: centred between the family menu (left) and the drawer toggle (right)
            var legendBg = MakeRect("Legend", mapView, TopCenter, new Vector2(0, -12), new Vector2(980, 34));
            AddFit(legendBg, new Vector2(980, 34), false, false, new Vector2(900, 0));
            RoundedImage(legendBg, new Color(0.03f, 0.04f, 0.06f, 0.82f));
            AddText(MakeStretch("Text", legendBg, 4), "world.legend", 16, Cream, TextAnchor.MiddleCenter);

            // Location view
            var locView = MakeStretch("LocationView", area);
            var bgRt = MakeStretch("Background", locView);
            bgRt.gameObject.AddComponent<RawImage>().raycastTarget = false;
            var crop = bgRt.gameObject.AddComponent<MapCropView>();
            AddImage(MakeStretch("Dim", locView), new Color(0.02f, 0.03f, 0.05f, 0.45f));
            var shade = Region("LeftShade", locView, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(900, 0));
            AddImage(shade, Color.white, PlaceholderArt.LoadSprite(PlaceholderArt.UiDir + "/GradientLeft.png"));

            // Content laid out at 1920x980: shrinks on small screens, spreads to the corners on large ones.
            var content = FitDesign("Content", locView, new Vector2(1920, 980), true);
            var locTitle = MakeLabel("Title", content, TopLeft, new Vector2(40, -96), new Vector2(700, 64), "", 48, Cream,
                TextAnchor.MiddleLeft, FontStyle.Bold, false, true);
            var locHours = MakeLabel("Hours", content, TopLeft, new Vector2(42, -162), new Vector2(700, 32), "", 22, Cream,
                TextAnchor.MiddleLeft, FontStyle.Bold, false, true);
            var locDesc = MakeLabel("Description", content, TopLeft, new Vector2(42, -200), new Vector2(660, 130), "", 21,
                new Color(0.88f, 0.85f, 0.78f), TextAnchor.UpperLeft, FontStyle.Italic, false, true);
            locDesc.resizeTextForBestFit = true;
            locDesc.resizeTextMinSize = 14;
            locDesc.resizeTextMaxSize = 21;
            var returnBtn = MakeButton("ReturnButton", content, TopLeft, new Vector2(42, -344), new Vector2(300, 64), "world.return", 26);
            returnBtn.gameObject.AddComponent<FocusRing>();

            var bodyRt = MakeRect("PlayerBody", content, BottomLeft, new Vector2(60, 20), new Vector2(280, 460));
            var body = AddImage(bodyRt, Color.white);
            body.preserveAspect = true;
            bodyRt.gameObject.AddComponent<IdleBob>();

            var actionsPanel = MakeRect("ActionsPanel", content, TopRight, new Vector2(-24, -92), new Vector2(780, 736));
            var headerBg = MakeRect("HeaderBg", actionsPanel, TopLeft, Vector2.zero, new Vector2(330, 44));
            RoundedImage(headerBg, ActionHeaderColor);
            AddText(MakeStretch("Header", headerBg, 6), "world.actions", 26, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            var container = MakeRect("Actions", actionsPanel, TopLeft, new Vector2(0, -52), new Vector2(780, 684));
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            var empty = MakeLabel("Empty", actionsPanel, TopLeft, new Vector2(4, -60), new Vector2(700, 40), "world.actions.empty", 22, Cream,
                style: FontStyle.Italic);
            var template = BuildCardTemplate(locView);

            var locationView = locView.gameObject.AddComponent<LocationView>();
            SetField(locationView, "background", crop);
            SetField(locationView, "titleText", locTitle);
            SetField(locationView, "hoursText", locHours);
            SetField(locationView, "descriptionText", locDesc);
            SetField(locationView, "playerBody", body);
            SetField(locationView, "actionsContainer", container);
            SetField(locationView, "cardTemplate", template);
            SetField(locationView, "emptyText", empty);
            SetField(locationView, "returnButton", returnBtn);

            // Message log (shared by map and location views); shrinks with the screen
            var logRt = MakeRect("MessageLog", area, BottomLeft, new Vector2(360, 14), new Vector2(1140, 124));
            AddFit(logRt, new Vector2(1140, 124), false, false, new Vector2(380, 0));
            RoundedImage(logRt, new Color(0.03f, 0.04f, 0.06f, 0.86f));
            var logText = AddText(MakeStretch("Text", logRt, 12), "", 19, Cream, TextAnchor.UpperLeft, FontStyle.Normal, false);
            logText.verticalOverflow = VerticalWrapMode.Truncate;
            var log = logRt.gameObject.AddComponent<MessageLog>();
            SetField(log, "text", logText);

            // ---- HUD, drawer, family menu, overlays ----
            var hud = BuildHud(root, out var menuButton);
            var hudRt = (RectTransform)hud.transform;
            Dock(area, hudRt, DockBelow.Mode.TopEdge, 0);

            var listItems = new List<LocationListItem>();
            var drawerFields = BuildDrawer(root, area, hudRt, db, listItems);

            var familyMenu = MakeRect("FamilyMenu", root, TopLeft, new Vector2(16, -112), new Vector2(440, 64));
            familyMenu.gameObject.AddComponent<FamilyMenu>();
            Dock(familyMenu, hudRt, DockBelow.Mode.Position, 12);

            MakeStretch("FamilyPanel", root).gameObject.AddComponent<FamilyPanel>();
            MakeStretch("MinigameHost", root).gameObject.AddComponent<Pagpag.Minigames.MinigameHost>();
            MakeStretch("WheelOverlay", root).gameObject.AddComponent<WheelOverlay>();
            MakeStretch("ConfirmDialog", root).gameObject.AddComponent<ConfirmDialog>();
            var intro = BuildIntro(root);
            var gameOver = BuildGameOver(root);

            var ctrl = new GameObject("WorldController").AddComponent<WorldController>();
            SetField(ctrl, "hud", hud);
            SetField(ctrl, "mapView", mapView.gameObject);
            SetField(ctrl, "mapImage", mapImg);
            SetField(ctrl, "locationView", locationView);
            SetField(ctrl, "intro", intro);
            SetField(ctrl, "gameOver", gameOver);
            SetField(ctrl, "menuButton", menuButton);
            SetField(ctrl, "roundedSprite", Rounded);
            SetField(ctrl, "circleSprite", Circle);
            SetField(ctrl, "ringSprite", Ring);
            SetList(ctrl, "hotspots", hotspots);
            SetList(ctrl, "listItems", listItems);
            foreach (var (field, value) in drawerFields) SetField(ctrl, field, value);

            Phase1Builder.Save(SceneNames.World);
        }

        private static readonly Color ActionHeaderColor = new Color(0.04f, 0.05f, 0.07f, 0.95f);
        private static readonly Color ActionCardFill = new Color(0.06f, 0.08f, 0.11f, 1f);
        private static readonly Color DrawerColor = new Color(0.06f, 0.08f, 0.11f, 1f);

        private static void Dock(RectTransform rt, RectTransform target, DockBelow.Mode mode, float gap)
        {
            var dock = rt.gameObject.AddComponent<DockBelow>();
            dock.target = target;
            dock.mode = mode;
            dock.gap = gap;
        }

        private static MapHotspot BuildHotspot(RectTransform map, LocationData loc)
        {
            const float pad = 6f, minWidth = 196f;
            var r = loc.mapLabelRect;
            if (r.width < minWidth) r.width = minWidth; // room for name + status on narrow painted labels
            var rt = MakeStretch("Hotspot_" + loc.id, map);
            rt.anchorMin = new Vector2((r.xMin - pad) / MapSize.x, 1f - (r.yMax + pad) / MapSize.y);
            rt.anchorMax = new Vector2((r.xMax + pad) / MapSize.x, 1f - (r.yMin - pad) / MapSize.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var hit = AddImage(rt, new Color(0, 0, 0, 0), null, true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<HoverScale>();

            var frame = MakeStretch("SelectedFrame", rt, -4);
            RoundedImage(frame, Gold);
            RoundedImage(MakeStretch("Fill", rt), new Color(0.06f, 0.07f, 0.09f, 1f));

            var ring = MakeRect("HereRing", rt, MidLeft, new Vector2(4, 0), new Vector2(42, 42));
            ring.pivot = new Vector2(0, 0.5f);
            AddImage(ring, WorldController.HereColor, Ring);
            var marker = MakeRect("Marker", rt, MidLeft, new Vector2(11, 0), new Vector2(28, 28));
            marker.pivot = new Vector2(0, 0.5f);
            var markerImg = AddImage(marker, Gold, Circle);

            var nameRt = Region("Name", rt, new Vector2(0, 0.44f), Vector2.one, new Vector2(48, 0), new Vector2(-6, -2));
            var name = AddText(nameRt, loc.displayName, 19, Color.white, TextAnchor.LowerLeft, FontStyle.Bold, false);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 10;
            name.resizeTextMaxSize = 19;
            var statusRt = Region("Status", rt, Vector2.zero, new Vector2(1, 0.46f), new Vector2(48, 3), new Vector2(-6, 0));
            var status = AddText(statusRt, "", 14, Gold, TextAnchor.UpperLeft, FontStyle.Normal, false);
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 9;
            status.resizeTextMaxSize = 14;

            var hs = rt.gameObject.AddComponent<MapHotspot>();
            SetField(hs, "locationId", loc.id);
            SetField(hs, "marker", markerImg);
            SetField(hs, "nameText", name);
            SetField(hs, "statusText", status);
            SetField(hs, "selectedFrame", frame.gameObject);
            SetField(hs, "hereRing", ring.gameObject);
            frame.gameObject.SetActive(false);
            return hs;
        }

        private static ActionCard BuildCardTemplate(Transform parent)
        {
            // Opaque card with a gold border and a drop shadow, so it reads over any location background.
            var rt = MakeRect("ActionCardTemplate", parent, TopLeft, Vector2.zero, new Vector2(780, 88));
            RoundedImage(rt, new Color(Gold.r, Gold.g, Gold.b, 0.55f));
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(4, -4);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            var fill = MakeStretch("Fill", rt, 2);
            RoundedImage(fill, ActionCardFill);

            var name = MakeLabel("Name", fill, TopLeft, new Vector2(18, -6), new Vector2(570, 30), "", 23, Cream,
                style: FontStyle.Bold, localized: false);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 16;
            name.resizeTextMaxSize = 23;
            var details = MakeLabel("Details", fill, TopLeft, new Vector2(18, -37), new Vector2(570, 22), "", 16, Gold,
                style: FontStyle.Bold, localized: false);
            details.resizeTextForBestFit = true;
            details.resizeTextMinSize = 12;
            details.resizeTextMaxSize = 16;
            var desc = MakeLabel("Description", fill, TopLeft, new Vector2(18, -60), new Vector2(570, 24), "", 16, Cream,
                TextAnchor.UpperLeft, FontStyle.Italic, false);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 12;
            desc.resizeTextMaxSize = 16;

            var button = MakeButton("DoButton", fill, MidRight, new Vector2(-12, 0), new Vector2(170, 66), "", 26, false,
                ActionButtonStyle.Normal, ActionButtonStyle.Text);
            var border = button.GetComponent<Outline>();
            border.effectColor = new Color(0.05f, 0.04f, 0.02f, 0.9f);
            border.effectDistance = new Vector2(2, -2);
            var style = button.gameObject.AddComponent<ActionButtonStyle>();
            SetField(style, "background", button.targetGraphic);
            SetField(style, "label", button.GetComponentInChildren<Text>());
            SetField(style, "border", border);
            button.gameObject.AddComponent<FocusRing>();

            var card = rt.gameObject.AddComponent<ActionCard>();
            SetField(card, "nameText", name);
            SetField(card, "detailsText", details);
            SetField(card, "descriptionText", desc);
            SetField(card, "doButton", button);
            SetField(card, "doLabel", button.GetComponentInChildren<Text>());
            SetField(card, "group", group);
            rt.gameObject.SetActive(false);
            return card;
        }

        private static HudController BuildHud(Transform root, out Button menuButton)
        {
            // Designed at 1920x100: shrinks on narrow screens, stretches on wide ones (menu stays at the right edge).
            var hud = MakeRect("HUD", root, TopLeft, Vector2.zero, new Vector2(1920, 100));
            AddFit(hud, new Vector2(1920, 100), false, true);
            AddImage(hud, new Color(0.05f, 0.06f, 0.08f, 0.98f));
            var line = Region("Line", hud, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 2));
            AddImage(line, new Color(Gold.r, Gold.g, Gold.b, 0.5f));

            var pframe = MakeRect("PortraitFrame", hud, MidLeft, new Vector2(16, 0), new Vector2(78, 78));
            AddImage(pframe, Gold);
            var portrait = AddImage(MakeRect("Portrait", pframe, Center, Vector2.zero, new Vector2(72, 72)), Color.white);

            var name = MakeLabel("Name", hud, MidLeft, new Vector2(108, 16), new Vector2(240, 40), "", 28, Cream, style: FontStyle.Bold, localized: false);
            var diff = MakeLabel("Difficulty", hud, MidLeft, new Vector2(108, -20), new Vector2(240, 28), "", 17, Gold, localized: false);
            var day = MakeLabel("Day", hud, MidLeft, new Vector2(360, 16), new Vector2(300, 36), "", 24, Cream, style: FontStyle.Bold, localized: false);
            day.resizeTextForBestFit = true;
            day.resizeTextMinSize = 16;
            day.resizeTextMaxSize = 24;
            var time = MakeLabel("Time", hud, MidLeft, new Vector2(360, -20), new Vector2(300, 30), "", 22, Cream, localized: false);
            MakeLabel("City", hud, MidLeft, new Vector2(680, 18), new Vector2(280, 26), "hud.location", 16, new Color(1, 1, 1, 0.55f));
            var location = MakeLabel("Location", hud, MidLeft, new Vector2(680, -12), new Vector2(290, 36), "", 24, Cream,
                style: FontStyle.Bold, localized: false);
            var money = MakeLabel("Money", hud, MidLeft, new Vector2(980, 0), new Vector2(190, 50), "", 34,
                new Color(0.55f, 0.85f, 0.45f), style: FontStyle.Bold, localized: false);

            var health = StatBar(hud, "HealthBar", 1180, "hud.health", new Color(0.85f, 0.2f, 0.2f));
            var hunger = StatBar(hud, "HungerBar", 1390, "hud.hunger", new Color(0.95f, 0.58f, 0.15f));
            var fatigue = StatBar(hud, "FatigueBar", 1600, "hud.fatigue", new Color(0.92f, 0.78f, 0.25f));

            menuButton = MakeButton("MenuButton", hud, MidRight, new Vector2(-14, 0), new Vector2(110, 64), "world.menu", 24);
            menuButton.gameObject.AddComponent<FocusRing>();

            var ctrl = hud.gameObject.AddComponent<HudController>();
            SetField(ctrl, "portrait", portrait);
            SetField(ctrl, "nameText", name);
            SetField(ctrl, "difficultyText", diff);
            SetField(ctrl, "dayText", day);
            SetField(ctrl, "timeText", time);
            SetField(ctrl, "locationText", location);
            SetField(ctrl, "moneyText", money);
            SetField(ctrl, "healthBar", health);
            SetField(ctrl, "hungerBar", hunger);
            SetField(ctrl, "fatigueBar", fatigue);
            return ctrl;
        }

        private static StatBar StatBar(Transform hud, string name, float x, string labelKey, Color color)
        {
            var rt = MakeRect(name, hud, MidLeft, new Vector2(x, 0), new Vector2(190, 70));
            var label = MakeLabel("Label", rt, TopLeft, new Vector2(0, -6), new Vector2(120, 28), labelKey, 19, Cream, style: FontStyle.Bold);
            var value = MakeLabel("Value", rt, TopRight, new Vector2(0, -6), new Vector2(70, 28), "", 19, Cream,
                TextAnchor.MiddleRight, FontStyle.Bold, false);
            var bg = MakeRect("Track", rt, BottomLeft, new Vector2(0, 10), new Vector2(190, 18));
            RoundedImage(bg, new Color(0.15f, 0.16f, 0.19f));
            var fill = Region("Fill", bg, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var fillImg = RoundedImage(fill, color);

            var bar = rt.gameObject.AddComponent<StatBar>();
            SetField(bar, "fill", fill);
            SetField(bar, "fillImage", fillImg);
            SetField(bar, "label", label);
            SetField(bar, "valueText", value);
            return bar;
        }

        /// <summary>The location list and travel panel, in a drawer that slides in from the right (closed by default).</summary>
        private static List<(string, Object)> BuildDrawer(Transform root, RectTransform area, RectTransform hud, LocationDatabase db,
            List<LocationListItem> items)
        {
            const float rowH = 52f, rowStep = 56f, inner = 380f;

            var toggle = MakeButton("DrawerToggle", area, TopRight, new Vector2(-16, -12), new Vector2(230, 64), "world.locations.toggle", 24,
                bg: Gold, textColor: new Color(0.08f, 0.08f, 0.1f));
            toggle.gameObject.AddComponent<FocusRing>();

            var side = Region("LocationDrawer", root, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-420, 0), Vector2.zero);
            side.pivot = new Vector2(1, 0.5f);
            Dock(side, hud, DockBelow.Mode.TopEdge, 0);
            AddImage(side, DrawerColor, null, true);
            var edge = Region("Edge", side, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
            AddImage(edge, new Color(Gold.r, Gold.g, Gold.b, 0.6f));
            side.gameObject.AddComponent<CanvasGroup>();

            MakeLabel("Header", side, TopLeft, new Vector2(20, -14), new Vector2(170, 44), "world.locations", 28, Cream, style: FontStyle.Bold);
            var familyButton = MakeButton("FamilyButton", side, TopRight, new Vector2(-84, -10), new Vector2(130, 52), "world.family", 22,
                bg: Gold, textColor: new Color(0.08f, 0.08f, 0.1f));
            familyButton.gameObject.AddComponent<FocusRing>();
            var close = MakeButton("CloseDrawer", side, TopRight, new Vector2(-16, -8), new Vector2(56, 56), "✕", 28, false);
            close.gameObject.AddComponent<FocusRing>();

            // Scrollable body so every row stays reachable on short screens.
            var viewport = Region("Viewport", side, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -72));
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport, new Color(0, 0, 0, 0.01f), null, true);
            var body = Region("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), Vector2.zero);
            body.pivot = new Vector2(0.5f, 1f);
            body.anchoredPosition = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = body;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var y = 0f;
            foreach (var loc in db.locations)
            {
                var row = MakeRect("Item_" + loc.id, body, TopLeft, new Vector2(20, y), new Vector2(inner, rowH));
                var bg = RoundedImage(row, new Color(0.1f, 0.13f, 0.17f), true);
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                var colors = btn.colors;
                colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
                colors.colorMultiplier = 1.3f;
                btn.colors = colors;
                row.gameObject.AddComponent<FocusRing>();
                var dot = MakeRect("Dot", row, MidLeft, new Vector2(12, 0), new Vector2(14, 14));
                dot.pivot = new Vector2(0, 0.5f);
                var dotImg = AddImage(dot, Gold, Circle);
                var name = MakeLabel("Name", row, MidLeft, new Vector2(34, 0), new Vector2(200, 40), "", 20, Cream, localized: false);
                name.rectTransform.pivot = new Vector2(0, 0.5f);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 12;
                name.resizeTextMaxSize = 20;
                var status = MakeLabel("Status", row, MidRight, new Vector2(-10, 0), new Vector2(150, 40), "", 15, Gold,
                    TextAnchor.MiddleRight, FontStyle.Normal, false);
                status.resizeTextForBestFit = true;
                status.resizeTextMinSize = 10;
                status.resizeTextMaxSize = 15;

                var item = row.gameObject.AddComponent<LocationListItem>();
                SetField(item, "locationId", loc.id);
                SetField(item, "background", bg);
                SetField(item, "dot", dotImg);
                SetField(item, "nameText", name);
                SetField(item, "statusText", status);
                items.Add(item);
                y -= rowStep;
            }

            // Selected location info + travel
            var top = y - 8f;
            var thumbFrame = MakeRect("ThumbFrame", body, TopLeft, new Vector2(20, top), new Vector2(inner, 130));
            AddImage(thumbFrame, new Color(Gold.r, Gold.g, Gold.b, 0.5f));
            var thumbRt = MakeStretch("Thumb", thumbFrame, 2);
            thumbRt.gameObject.AddComponent<RawImage>().raycastTarget = false;
            var thumb = thumbRt.gameObject.AddComponent<MapCropView>();

            var infoName = MakeLabel("InfoName", body, TopLeft, new Vector2(20, top - 138), new Vector2(inner, 36), "", 28, Cream,
                style: FontStyle.Bold, localized: false);
            var infoStatus = MakeLabel("InfoStatus", body, TopLeft, new Vector2(20, top - 176), new Vector2(inner, 26), "", 17, Gold, localized: false);
            infoStatus.resizeTextForBestFit = true;
            infoStatus.resizeTextMinSize = 11;
            infoStatus.resizeTextMaxSize = 17;
            var infoDesc = MakeLabel("InfoDescription", body, TopLeft, new Vector2(20, top - 206), new Vector2(inner, 84), "", 16,
                new Color(0.85f, 0.83f, 0.78f), TextAnchor.UpperLeft, FontStyle.Italic, false);
            infoDesc.resizeTextForBestFit = true;
            infoDesc.resizeTextMinSize = 11;
            infoDesc.resizeTextMaxSize = 16;
            var infoDetail = MakeLabel("InfoDetail", body, TopLeft, new Vector2(20, top - 294), new Vector2(inner, 52), "", 16, Cream,
                TextAnchor.UpperLeft, FontStyle.Normal, false);
            infoDetail.resizeTextForBestFit = true;
            infoDetail.resizeTextMinSize = 11;
            infoDetail.resizeTextMaxSize = 16;
            var primary = MakeButton("PrimaryButton", body, TopLeft, new Vector2(20, top - 352), new Vector2(inner, 60), "", 24, false,
                Gold, new Color(0.08f, 0.08f, 0.1f));
            primary.gameObject.AddComponent<FocusRing>();
            var secondary = MakeButton("SecondaryButton", body, TopLeft, new Vector2(20, top - 420), new Vector2(inner, 60), "", 22, false);
            secondary.gameObject.AddComponent<FocusRing>();
            body.sizeDelta = new Vector2(0, -(top - 420 - 60 - 24));

            var drawer = side.gameObject.AddComponent<SideDrawer>();
            SetField(drawer, "toggleButton", toggle);
            SetField(drawer, "closeButton", close);

            return new List<(string, Object)>
            {
                ("drawer", drawer),
                ("familyButton", familyButton),
                ("infoThumbnail", thumb), ("infoName", infoName), ("infoStatus", infoStatus), ("infoDescription", infoDesc),
                ("infoDetail", infoDetail), ("primaryButton", primary), ("primaryLabel", primary.GetComponentInChildren<Text>()),
                ("secondaryButton", secondary), ("secondaryLabel", secondary.GetComponentInChildren<Text>())
            };
        }

        private static IntroOverlay BuildIntro(Transform root)
        {
            var rt = MakeStretch("IntroOverlay", root);
            AddImage(rt, new Color(0.03f, 0.04f, 0.06f, 1f), null, true);
            var page = FitDesign("Content", rt, new Vector2(1920, 1080), true);
            var bodyRt = MakeRect("Body", page, MidLeft, new Vector2(170, -40), new Vector2(340, 600));
            var body = AddImage(bodyRt, Color.white);
            body.preserveAspect = true;
            bodyRt.gameObject.AddComponent<IdleBob>();

            var title = MakeLabel("Title", page, TopLeft, new Vector2(620, -130), new Vector2(1160, 76), "", 52, Gold,
                TextAnchor.MiddleLeft, FontStyle.Bold, false, true);
            var text = MakeLabel("Text", page, TopLeft, new Vector2(620, -230), new Vector2(1160, 600), "", 28, Cream,
                TextAnchor.UpperLeft, FontStyle.Normal, false);
            text.lineSpacing = 1.15f;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 28;
            var pageNo = MakeLabel("Page", page, BottomLeft, new Vector2(620, 110), new Vector2(300, 40), "", 22, new Color(1, 1, 1, 0.5f),
                localized: false);
            var next = MakeButton("NextButton", page, BottomRight, new Vector2(-140, 90), new Vector2(300, 80), "", 32, false,
                Gold, new Color(0.08f, 0.08f, 0.1f));

            var overlay = rt.gameObject.AddComponent<IntroOverlay>();
            SetField(overlay, "bodyImage", body);
            SetField(overlay, "titleText", title);
            SetField(overlay, "bodyText", text);
            SetField(overlay, "pageText", pageNo);
            SetField(overlay, "nextButton", next);
            SetField(overlay, "nextLabel", next.GetComponentInChildren<Text>());
            rt.gameObject.SetActive(false);
            return overlay;
        }

        private static GameOverOverlay BuildGameOver(Transform root)
        {
            var rt = MakeStretch("GameOverOverlay", root);
            AddImage(rt, new Color(0.05f, 0.01f, 0.01f, 0.93f), null, true);
            // 810 tall: room for the victory screen's second button row.
            var box = MakePanel("Dialog", rt, Center, Vector2.zero, new Vector2(1100, 810));
            AddFit((RectTransform)box.transform.parent, new Vector2(1100, 810), true, false, new Vector2(48, 48));
            var title = MakeLabel("Title", box.transform, TopCenter, new Vector2(0, -30), new Vector2(1000, 90), "GAME OVER", 72, PagpagRed,
                TextAnchor.MiddleCenter, FontStyle.Bold, false, true);
            var note = MakeLabel("Note", box.transform, TopCenter, new Vector2(0, -122), new Vector2(1000, 34), "", 22, Danger,
                TextAnchor.MiddleCenter, FontStyle.Italic, false);
            var cause = MakeLabel("Cause", box.transform, TopCenter, new Vector2(0, -164), new Vector2(1000, 130), "", 28, Cream,
                TextAnchor.MiddleCenter, FontStyle.Bold, false);
            cause.resizeTextForBestFit = true;
            cause.resizeTextMinSize = 16;
            cause.resizeTextMaxSize = 28;
            var stats = MakeLabel("Stats", box.transform, TopCenter, new Vector2(0, -310), new Vector2(940, 280), "", 22, Cream,
                TextAnchor.UpperLeft, FontStyle.Normal, false);
            stats.lineSpacing = 1.2f;
            var menu = MakeButton("MenuButton", box.transform, BottomCenter, new Vector2(0, 36), new Vector2(420, 76), "world.gameover.menu", 30);
            // Shown only after a win; the menu button then moves left of it (see GameOverOverlay).
            var keepPlaying = MakeButton("ContinueButton", box.transform, BottomCenter, new Vector2(230, GameOverOverlay.UpperRowY), new Vector2(420, 76),
                "world.victory.continue", 28, bg: Gold, textColor: new Color(0.08f, 0.08f, 0.1f));
            keepPlaying.gameObject.SetActive(false);
            // Below the two: leave the slums, enabled only with the money quota saved.
            var leave = MakeButton("LeaveButton", box.transform, BottomCenter, new Vector2(0, GameOverOverlay.LowerRowY), new Vector2(880, 76),
                "world.victory.leave", 28);
            leave.gameObject.SetActive(false);

            var overlay = rt.gameObject.AddComponent<GameOverOverlay>();
            SetField(overlay, "titleText", title);
            SetField(overlay, "noteText", note);
            SetField(overlay, "causeText", cause);
            SetField(overlay, "statsText", stats);
            SetField(overlay, "menuButton", menu);
            SetField(overlay, "continueButton", keepPlaying);
            SetField(overlay, "leaveButton", leave);
            rt.gameObject.SetActive(false);
            return overlay;
        }
    }
}
