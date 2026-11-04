# Revision 2: Family, Minigames, Grief and the Wheel

Built by **Pagpag ▸ Build All**. Play from `Assets/Scenes/MainMenu.unity`.
**112 EditMode tests** cover this revision and earlier ones (Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All).

## 1. Stat scale (100 = best)
- Every stat for everyone (player, spouse, five children) is **Hunger, Energy and Health on 0–100**, where 100 is full or best. Everyone starts at 100.
- "Fatigue" is now **Energy**.

Drain per hour, before the difficulty multiplier:

| Stat | Awake | Asleep |
|---|---|---|
| Hunger | −3 | −1.5 |
| Energy | −2.5 | +12.5 |

Health:
- Drains slowly when hunger ≤ 15 or energy ≤ 10, and fast (−4/h) when either hits 0.
- Recovers when hunger ≥ 40 and energy ≥ 30.
- Warnings appear at hunger 25, energy 20 and health 35.

Difficulty:
- **Tagalog (Normal)** uses the base rates.
- **Bisaya (Hard)** multiplies every hunger and energy drain, and the damage from starving or collapsing, by 1.6, for all seven family members.
- Prices and wages are identical in both modes.

Saves from older builds (save version below 3) can't be continued, because the stat scale changed.

## 2. The children
Dong's real name is Eduardo, which is why the three boys are Ed, Edd and Eddie.

| Child | Age | Personality | Best at | Body |
|---|---|---|---|---|
| **Ed** | 15 | Serious, protective; left school to work | Strength ★5 | Big eater ×1.25 · tireless ×0.85 |
| **Edd** | 13 | The brain; keeps Inday's budget notebook | Smarts ★5 | Tires easily ×1.2 · a little fragile |
| **Eddie** | 11 | Loud, funny, could sell ice to a fisherman | Charm ★5 | Always hungry ×1.1 |
| **Joy** | 9 | Quiet, quick hands, misses nothing | Dexterity ★5 | Light eater ×0.75 |
| **Pia** | 6 | The bunso; sweet, with asthma | Charm ★4 | Tires ×1.4 · fragile ×1.6 |

- Each child has a full backstory, trait tags and a portrait (`ScriptableObjects/Children/*.asset`).
- The children appear in the lore viewer during character creation, on the "Your Family" intro page, in the family strip and in the Family panel.

## 3. Feeding seven people
- **Pantry:** buy family food to add servings. Each serving restores 35 hunger.

  | Where | Food | Price | Servings |
  |---|---|---|---|
  | Market | Rice and viand | ₱240 | 10 |
  | Fish Market | Fish and rice | ₱200 | 8 |
  | Sari-Sari | Rice and canned goods | ₱110 | 4 |
  | Pagpag Stall | Pagpag bundle (15% chance per serving of making someone sick) | ₱70 | 6 |
  | Community Center | Extra lugaw, mornings, once a day | free | 3 |

- **The spouse** feeds whoever is home and hungry from the pantry at 7 AM, noon and 6 PM, youngest first. If the pantry runs out, the log names who went hungry.
- **At home** you can *Serve a family meal* (everyone, you included) or *Eat from the pantry*. In the Family panel you can feed one person.
- **Balance**, measured by the economy tests:
  - A family of seven eats about 11 servings a day (₱264), more than one adult shift pays.
  - On Hard, a hard-working household roughly breaks even over ten days.
  - On Normal the same household saves a little.
  - A family that doesn't work loses someone within six days.

## 4. Children's jobs
- **Open the Family panel** with the top strip or the **Family** button, pick a child, then **Send to work…**.
- **Each job has a minimum age** (adult-only jobs are construction, the port and the canteen), a skill it tests, and expected pay, time and costs.
- **The child's skill** in that ability sets the minigame's *ease* (wider windows, slower targets) and their pay: 40% of the adult wage × 0.7–1.3. Low energy makes the game harder.
- **You play the minigame on the child's behalf.** The child is then away for the job's duration and pays its hunger and energy cost, scaled by their appetite and tiredness.

## 5. Health panels and death
- **The family strip** shows every member's portrait, three mini-bars and live status. A card turns red when someone is critical.
- **The Family panel** shows full bars, traits, skills, personality, backstory and status (at home, asleep, working until…, or died on Day N of …).
- **Children and the spouse die** when health reaches 0, from starvation, collapse, food poisoning or injury. Death is permanent.

## 6. Grief (depression)
- A death in the family starts **7 in-game days** of depression; another death refreshes it.
- While it lasts:
  - hunger and energy drain ×1.5;
  - sleep and health recovery ×0.5;
  - job pay ×0.6;
  - minigames are harder (ease ×0.8).
- **Indicators:** the HUD shows "DEPRESSED · Xd Yh" with a tinted, pulsing portrait, and a pulsing purple badge in the family strip shows who you are grieving and the time left.

## 7. Job minigames (18, one per job)
Pay = wage × min(performance ÷ 80%, 125%). Below 15% performance pays nothing. Walking away pays nothing, still uses the shift, and costs half the time.

| Job | Minigame | Mechanic |
|---|---|---|
| Collect recyclables | Sort the Kalakal | Sort items into 4 sacks before they time out |
| Haul crates | Balance the Crates | Hold to lean against a wobbling stack while walking |
| Unload deliveries | Catch the Cases | Move between 3 lanes to catch cases; dodge rotten sacks |
| Construction morning | Up the Scaffolding | Timing: step when the marker is in the green |
| Construction afternoon | Mix the Cement | Hold to pour 4 ingredients to target lines; 3 batches |
| Fish porter | Fish Tub Relay | Reaction: deliver to whichever buyer is calling |
| Port cargo | Crane Drop | Drop a swinging crate into a moving hold |
| Parcel delivery | Parcel Runs | Match addresses to shops on a reshuffling street |
| Volunteer cleanup | Sweep the Court | Click piles of dirt until gone |
| Hand out flyers | Hand Out Flyers | Click passers-by; avoid people in a hurry |
| Laundry | Scrub and Wring | Alternate left/right to scrub, then hold to wring |
| Help Aling Nena | Weigh and Sell | Hit each customer's exact weight |
| Bundle vegetables | Bundle Match | Memory pairs after a short preview |
| Mind the store | Make Change | Give exact change with coins and bills |
| Cook for the crew | Cook for the Crew | Memorize recipe steps and repeat them |
| Clean fish | Clean the Fish | Arrow-key prompts against a timer |
| Port canteen | Serve the Line | Serve the dish each worker wants before they leave |
| Food kiosk | Siopao Steamers | Take siopao out of 4 steamers inside a green window |

Controls are mouse plus keyboard shortcuts, as listed in each game's briefing. Code: `Scripts/Minigames/*`.

## 8. "A quiet night together"
- **When:** at home, 8 PM–2 AM, once a day, while your spouse is alive.
- **The scene** fades to black with a short, non-explicit line of text.
- **The wheel:** 20 equal segments, exactly 1 of them red (pregnancy), so the visual odds are exactly 5%. The outcome is rolled first (`Random.value < 0.05`) and the wheel stops inside a matching segment.
- **If Inday becomes pregnant**, the run is lost, whether you play as Dong ("Inday is pregnant") or as Inday ("You are pregnant"). A game-over screen explains why.
- **Otherwise** you and your spouse get a small energy and health boost.

## Key files
`Scripts/Family/` (ChildProfile, FamilyDatabase, FamilyService) · `Scripts/Jobs/JobRules.cs` · `Scripts/Survival/SurvivalSystem.cs` · `Scripts/Minigames/` · `Scripts/UI/World/` (FamilyStrip, FamilyPanel, WheelOverlay) · `Scripts/UI/UIKit.cs` · `Editor/Phase1Builder.cs` (children) · `Editor/Phase2Builder.cs` (jobs, food, scene) · `Tests/EditMode/*`.
