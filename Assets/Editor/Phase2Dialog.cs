using System.Collections.Generic;
using Pagpag.Locations;
using UnityEngine;

namespace Pagpag.EditorTools
{
    /// <summary>
    /// The short exchange shown before every interaction with someone (asking for work, buying, eating at a stall,
    /// talking). Applied to the location data by <see cref="Phase2Builder"/>. Actions done alone (sleeping, cooking,
    /// eating from the pantry, taking medicine) have none. {name} = the player, {spouse} = the spouse.
    /// Each reply list holds alternatives; one is picked at random.
    /// </summary>
    public static class Phase2Dialog
    {
        private struct Line
        {
            public string speaker, player;
            public string[] replies;
        }

        private static Line L(string speaker, string player, params string[] replies) => new Line { speaker = speaker, player = player, replies = replies };

        private static readonly Dictionary<string, Line> Lines = new Dictionary<string, Line>
        {
            // ---------- Home (the spouse) ----------
            ["talk_wife"] = L("Inday", "Inday, sit with me a minute.",
                "Five minutes, then I have to start the rice. What's on your mind, Dong?",
                "You look tired. Come here. The kids can wait for once."),
            ["talk_husband"] = L("Dong", "Dong, sit with me a minute.",
                "For you, always. Did I tell you what the landlord's rooster did this morning?",
                "Of course. Tell me about your day first, then I'll tell you about mine."),
            ["intimacy"] = L("{spouse}", "The children are asleep…",
                "Then let's not wake them.",
                "Come here. Just for a little while, let's forget about everything else."),
            ["leave_slums"] = L("{spouse}", "We have enough now. Let's leave Tondo.",
                "Are we really doing this? Then let's pack before either of us changes our mind.",
                "Our children will grow up somewhere else. I never thought I'd hear you say it."),

            // ---------- Neighborhood Street ----------
            ["recyclables"] = L("Mang Islaw (junk shop)", "Mang Islaw, are you buying today?",
                "Bottles one peso, cans two, cardboard by the kilo. Bring me a full sack and I'll pay.",
                "Prices are down this week, but I'll take everything you can carry. Mind the canal."),
            ["laundry"] = L("Aling Rosa", "Aling Rosa, do the households need washing done?",
                "Three families have baskets waiting. Use the faucet before the water pressure drops at noon.",
                "The Santos boys ruined their uniforms again. Scrub hard and they'll pay well."),
            ["chat"] = L("Aling Rosa", "Kumusta, Aling Rosa?",
                "Ay, the same. Rice went up again. Sit, sit. Have you heard the latest?",
                "Just the person I wanted to see. Come, I have news."),

            // ---------- Pagpag Stall ----------
            ["pagpag_chicken"] = L("Mang Itoy", "One chicken, Mang Itoy.",
                "Fresh from the bins this afternoon, washed twice. Mostly.",
                "Last pieces. Fried them again in the good oil. Well, the oil."),
            ["pagpag_rice"] = L("Mang Itoy", "Just a rice bowl tonight.",
                "Rice and a little sauce. Fills the stomach, that's what matters.",
                "Here. Eat it while it's hot. It's better hot."),
            ["pagpag_family"] = L("Mang Itoy", "A bag for the family, please.",
                "Enough for all your kids. Don't feed it to them every day, ha? I've seen what happens.",
                "Big bag tonight. Heat it well before they eat it."),

            // ---------- Public Market ----------
            ["carinderia"] = L("Ate Lorna (carinderia)", "Rice and adobo, Ate.",
                "Sit down, I'll add soup. You look like you need it.",
                "Adobo's fresh this morning. Extra rice is free if you don't tell anyone."),
            ["rice_veg"] = L("Ate Lorna (carinderia)", "Just rice and gulay today.",
                "Chopsuey or monggo? Monggo's thicker today.",
                "Healthy choice! Here, I gave you the bigger ladle."),
            ["haul_crates"] = L("Kuya Jun (vendor boss)", "Kuya Jun, any work this morning?",
                "Truck from Benguet just came in. Crates to the stalls, and don't drop the tomatoes.",
                "You again? Good, the last guy quit. Start with the cabbage."),
            ["sell_veg"] = L("Aling Nena", "Aling Nena, do you need help at the stall?",
                "Yes! Weigh honestly and smile at the customers. That's the whole secret.",
                "Come, come. The morning crowd is coming and my eyes can't read the scale anymore."),
            ["bundle_veg"] = L("Aling Nena", "Can I help bundle tomorrow's vegetables?",
                "Kangkong in bunches of ten, pechay by size. Neat ones sell faster.",
                "Please. My hands are too slow now. The twine is under the table."),
            ["market_family"] = L("Mang Dado (rice dealer)", "Two kilos of rice and some viand for the family.",
                "Seven mouths, ha? I'll throw in a little more pork. Don't tell my wife.",
                "Best price in Tondo. Here, carry it with both hands."),

            // ---------- Sari-Sari Store ----------
            ["pandesal"] = L("Aling Pacing", "Five pandesal, Aling Pacing.",
                "Still warm. Five pesos more for the coffee sachet, if you want.",
                "Here you go. The bench outside is free."),
            ["canton"] = L("Aling Pacing", "One pancit canton, please.",
                "Use the hot water in the thermos. Don't take all of it.",
                "The spicy one? Here."),
            ["sardines"] = L("Aling Pacing", "Sardines and a cup of rice.",
                "Tomato sauce or chili? I'll open it for you.",
                "Good choice. Cheap and it fills you up."),
            ["mind_store"] = L("Aling Pacing", "Do you need someone to mind the store?",
                "I have errands until lunch. The debt list is in the notebook. Nobody gets credit without asking me.",
                "Yes! Write everything down. And don't let Mang Kanor charge another bottle."),
            ["unload_store"] = L("Aling Pacing", "Is the delivery truck here yet?",
                "Twenty cases of softdrinks and three sacks of rice. Careful, the bottles are glass.",
                "It just arrived. Stack them by the door, the heavy ones at the bottom."),
            ["sari_family"] = L("Aling Pacing", "Rice and canned goods for the family.",
                "For seven? I'll add crackers for the bunso.",
                "Here. Pay me today, ha? Your name is already on the list."),

            // ---------- Construction Site ----------
            ["con_morning"] = L("Mang Berto (foreman)", "Mang Berto, do you need another back this morning?",
                "Hollow blocks to the third floor. Hard hat on, and don't climb the scaffolding like a monkey.",
                "We're behind schedule. Grab blocks and keep them coming until noon."),
            ["con_afternoon"] = L("Mang Berto (foreman)", "Any work for the afternoon shift?",
                "The mixer needs feeding: sand, gravel, cement, water. Keep the ratio right or the slab cracks.",
                "Cement's here. Mix it properly, the inspector comes tomorrow."),
            ["con_cook"] = L("Manang Cora (site cook)", "Manang Cora, can I help cook for the crew?",
                "Thirty men, one pot. Rice first, then the viand. Move fast, they eat at noon sharp.",
                "Thank God. Peel the garlic and watch the rice. Don't let it burn."),

            // ---------- Employment Office ----------
            ["register"] = L("Ms. Dela Cruz (clerk)", "I'd like to register as a job seeker.",
                "One hundred pesos for photos and forms. Fill in everything, even the parts that don't apply.",
                "Take a number. Just kidding, there's nobody else today. ₱100 for the forms."),
            ["job_board"] = L("Ms. Dela Cruz (clerk)", "Is anyone hiring?",
                "The board has everything. Most want diplomas, but read it anyway.",
                "A few postings came in this week. Look at the bottom row, those don't need college."),

            // ---------- Fish Market ----------
            ["fish_porter"] = L("Kapitan Ramon (boat owner)", "Kapitan, do you need porters?",
                "The bancas are in. Tubs to the buyers, ice on top. Run, the fish won't wait.",
                "Good catch tonight. Grab a tub, I pay by the trip."),
            ["fish_clean"] = L("Aling Bebang (fish vendor)", "Aling Bebang, any fish to clean?",
                "Galunggong, a whole tub. Scales off, guts out, keep your fingers.",
                "Yes, yes. Sharp knife's in the bucket. The buyers want them clean by six."),
            ["grilled_fish"] = L("Aling Bebang (fish vendor)", "One grilled fish with rice.",
                "Straight from the boat to the coals. You won't find fresher.",
                "The biggest one, for you. Careful of the bones."),
            ["fish_family"] = L("Aling Bebang (fish vendor)", "Fish and rice for the family.",
                "A whole tub of galunggong. Cook it tonight, it won't keep.",
                "For seven? Here, I'll add two more fish."),

            // ---------- Port ----------
            ["cargo"] = L("Sir Gomez (agency recruiter)", "Sir, I'm registered. Is there a cargo shift?",
                "Day rate's the best on the waterfront. Watch the cranes, nobody's insured here.",
                "One slot left on ship three. Sign here, and keep your head down."),
            ["canteen"] = L("Manang Luz (canteen)", "Manang Luz, do you need help in the canteen?",
                "Two hundred dockworkers at lunch. Serve fast and keep the lines moving.",
                "Apron's on the hook. The rice is ready, the soup is not."),

            // ---------- Community Center ----------
            ["lugaw"] = L("Ate Marites (volunteer)", "Is the feeding program still open?",
                "Of course. Here, one bowl with an egg. How are the children?",
                "Come, there's still some left. Take the egg, you need it."),
            ["cleanup"] = L("Mr. Santos (barangay secretary)", "Do you need volunteers for the cleanup?",
                "The court after the basketball game is a disaster. Sweep it all and the barangay pays a small stipend.",
                "Yes. Brooms are in the storage room. The captain's coming tomorrow, so make it shine."),
            ["lugaw_family"] = L("Ate Marites (volunteer)", "Is there extra lugaw for my children?",
                "Bring a container next time. Here, a full pot for the little ones.",
                "For your five? Of course. Tell them Ate Marites says hello."),

            // ---------- Train Station ----------
            ["parcels"] = L("Kuya Ramil (dispatcher)", "Kuya Ramil, any parcels to run?",
                "Fourteen today, all over the district. Addresses are on the tags. Don't get lost.",
                "Perfect timing, my runner is sick. Here's the route."),
            ["kiosk"] = L("Ms. Chua (kiosk owner)", "Ms. Chua, do you need help at the kiosk?",
                "Rush hour in ten minutes. Siopao in the steamer, coffee on the right. Smile.",
                "Yes, please. My nephew didn't show up again."),

            // ---------- Main Road ----------
            ["taho"] = L("Manong Taho", "One taho, Manong.",
                "Extra sago for you. Taho! Tahoooo!",
                "Big or small? Big. I knew it."),
            ["balut"] = L("Mang Pido (balut vendor)", "One balut, please.",
                "Hot from the basket. Salt or vinegar?",
                "This one's a good one, I can tell."),
            ["flyers"] = L("Sir Bong (lending agent)", "Do you still need someone for flyers?",
                "Five hundred flyers. Hand them to everyone, especially people who look worried.",
                "Yes. Smile, and don't read the interest rates out loud."),

            // ---------- Pharmacy ----------
            ["pharmacist"] = L("Ate Grace (pharmacist)", "Ate Grace, can you tell me what my family needs?",
                "Tell me everyone's symptoms and I'll tell you what to buy. Honestly, not what's most expensive.",
                "Sit down. Let's go through it one by one."),
            ["buy_dose"] = L("Ate Grace (pharmacist)", "Medicine for a fever, please. The generic one.",
                "One dose a day until the fever is gone. Don't stop early, even if they feel better.",
                "Generic works the same, it's just cheaper. One dose lasts a day."),
            ["buy_cure"] = L("Ate Grace (pharmacist)", "I need the strongest thing you have. The one that works right away.",
                "It's very expensive, but it cures almost anything within the hour. Are you sure?",
                "This is the branded injection. It works fast, but it costs more than a month of rice."),
        };

        /// <summary>Sets the dialog on every action that has one; also marks pagpag food for the illness rules.</summary>
        public static void Apply(IEnumerable<LocationData> locations)
        {
            var missing = new List<string>();
            foreach (var loc in locations)
                foreach (var a in loc.actions)
                {
                    a.pagpagFood = a.id == "pagpag_chicken" || a.id == "pagpag_rice";
                    if (Lines.TryGetValue(a.id, out var line))
                    {
                        a.speaker = line.speaker;
                        a.playerLine = line.player;
                        a.npcLines = line.replies;
                    }
                    else
                    {
                        a.speaker = null;
                        a.playerLine = null;
                        a.npcLines = new string[0];
                        if (loc.id != "home") missing.Add($"{loc.id}/{a.id}");
                    }
                }
            if (missing.Count > 0) Debug.LogWarning("[Dialog] Actions outside home with no dialog: " + string.Join(", ", missing));
        }
    }
}
