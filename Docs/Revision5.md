# Revision 5: a year-long goal, illness, the pharmacy, and dialog

## 1. The goal is 365 days
- `GameConfig.victoryDays` is now 365 (it was 100). Everything that mentions the goal reads that value: the HUD ("Day 12/365"), the victory screen, the end screens, and the locked Infinite Mode option.
- The rest of Revision 4 is unchanged: the win and Infinite Mode unlock, losing if someone dies before the goal, and leaving the slums with ₱100,000.

## 2. Illness
Anyone in the household can get sick: the player, the spouse or a child. An untreated illness only gets worse, and it kills.

| Illness | Comes from | Worsens per day | Damage |
|---|---|---|---|
| Typhoid fever | Eating pagpag day after day | +10 | ×1.0 |
| Flu | Random daily roll (weight 3) | +6 | ×0.8 |
| Dengue fever | Random daily roll (weight 1) | +12 | ×1.3 |

- **Severity** runs from 0 to 100. It starts at 20, becomes "serious" at 35 and "critical" at 70.
- **Damage:** up to 1.2 health per hour at severity 100, scaled by the illness and the person's frailty. Children die faster.
- **Effects while sick:**
  - No natural healing.
  - Extra energy drain.
  - At severity 60 or more, nobody can work. Children can't be sent to jobs either.
- **Untreated:** the flu kills an adult in about 9 days, typhoid in about 7, dengue in about 5. The death cause is the illness name. A family member's death starts the usual grief week, and it rules out the 365-day win.
- **Pagpag habit:**
  - Each pagpag meal rolls for typhoid. This covers street pagpag (chicken, rice bowl), pantry pagpag eaten by the player, and pagpag servings fed to the family, including the spouse's auto-meals.
  - The chance is 2% on the first day, plus 6% for each extra day in a row that the same person eats pagpag, capped at 60%. One day without pagpag resets the streak.
  - From day 2 of a streak the player is warned ("That's 3 days of pagpag in a row…").
  - The old one-off food-poisoning risk is still there on top.
- **Random illness:** a 1.2% daily chance at 6 AM for each person, plus 3% if that person is starving or exhausted at the roll. It is the same in every difficulty mode.
- **Where it shows:**
  - HUD status line: "SICK · Flu (serious, on medicine)".
  - Family panel status: "Sick: Typhoid fever (mild) · At home". The row turns red once an illness is serious.
  - Log messages when someone falls ill, gets worse or recovers.
- Tuning lives in `FamilyDatabase.illness` (`Scripts/Survival/IllnessSettings.cs`). The rules are in `Scripts/Survival/IllnessRules.cs`.

## 3. Pharmacy and medicine
- **Pharmacy** is a new location: Ate Grace's botika, open 8 AM–9 PM and available from Day 1. It sits on the map below the Construction Site.

| Action | Price | Effect |
|---|---|---|
| Ask the pharmacist | Free | Says who is sick, how badly, and how many doses each person needs |
| Generic medicine (1 dose) | ₱120 | Goes into the medicine cabinet at home |
| Instant cure (branded) | ₱2,500 | Goes into the cabinet; cures any illness at once |

- **One dose** works for 24 hours. While it works, severity drops by 22 per day instead of rising, and the illness does only 40% of its damage. Doses stack.
- **How many doses:** a fresh illness needs about one dose; a critical one needs four or five. The instant cure costs about 20 doses' worth.
- **Using medicine:**
  - The player: home actions **Take medicine** and **Take the instant cure**. They only appear while you are sick.
  - The spouse and children: **Give medicine (N at home)** and **Instant cure (N at home)** buttons in the family panel. You must both be home.
- The family panel header shows the cabinet: "Medicine: 2 dose(s), 0 cure(s)".

## 4. Dialog with every interaction
- **What it covers:** every action at every location outside home (asking for work, buying, eating at a stall, talking, registering, asking the pharmacist), plus talking with your spouse, the quiet night, and leaving the slums.
- **What it looks like:** a short exchange, with your line and the other person's reply. Each person has their own name, for example Mang Islaw at the junk shop, Kuya Jun the vendor boss, Mang Berto the foreman, Ate Grace the pharmacist.
- **Buttons:** **Start work** / **Buy** / **Eat** / **Go on** / **Pay ₱100** to go ahead, or **Never mind**.
- Each person has two alternative replies, picked at random.
- Actions you do alone (sleeping, cooking, eating from the pantry, taking medicine) have no dialog.
- **Code:**
  - The dialog reuses the shared `ConfirmDialog` through `ShowConversation`: a wider frame, a gold go-ahead button, and focus on it. Leaving the slums keeps the red button and the safe focus.
  - The lines live in `Editor/Phase2Dialog.cs`. `LocationAction.speaker`, `playerLine` and `npcLines` are filled by **Build All**, and `{name}` and `{spouse}` are replaced at runtime.

## 5. Balance check (365-day simulation, careful play)
- **Careful play** means 80–90% minigame scores, safe food only, medicine bought as soon as anyone falls ill, and children working.

| Mode | Character | Result |
|---|---|---|
| Normal | Dong | Won day 365, all 7 alive, ₱113k (enough to leave the slums) |
| Normal | Inday | Won, all 7 alive, ₱91k |
| Hard | Inday | Won, all 7 alive, ₱28k |
| Hard | Dong | Reached day 365, but lost Edd on day 15 to exhaustion from overwork, so no win |

- About 25–80 illnesses over the year, 30–70 doses used.
- The same Normal Dong player **without medicine** died of flu on day 40.

## Tests
- `Tests/EditMode/IllnessTests.cs` (new) has 20 tests:
  - pagpag streaks for the player and for children
  - safe food never causes typhoid
  - the random daily roll
  - each illness kills untreated (player, spouse, child)
  - no healing while sick
  - too sick to work
  - doses cure
  - pharmacy buy and take
  - price gap between doses and the cure
  - diagnosis
  - save round-trip
  - dialog on every non-home action
  - only stall pagpag counts as pagpag
- `VictoryTests` now expects 365 days.
- The full suite is 174 tests, all passing.
