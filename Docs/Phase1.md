# Pagpag: The Hunger Games — Phase 1

**Scope:** main menu, character creation (Dong / Inday), name entry, difficulty selection, a persistent run, and the hardcore single-slot save.
Unity **6000.6.6f1**, URP 2D, Input System (new), uGUI. **The game is English-only.**

> **Revision 1 (2026-10-21)**
> - Starting money is a fixed ₱500 for every run.
> - The game is English-only; language selection was removed.
> - The two difficulty modes are **Tagalog (Normal)** and **Bisaya (Hard)**.
> - Dong and Inday are husband and wife, each with their own backstory.
> - The World scene is now the playable map (see [Phase2.md](Phase2.md)).

## How to build and play

1. Open the project in Unity.
2. In the menu bar, choose **Pagpag ▸ Build All**. This regenerates the placeholder art, data assets and all three scenes, and registers them in Build Settings. You can run it again at any time.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.

Developer menu items:
- **Pagpag ▸ Build World Only** rebuilds just the location data and the World scene.
- **Pagpag ▸ Developer ▸ Delete Current Run Save** wipes the single save slot.
- **Pagpag ▸ Developer ▸ Open Save Folder** opens the save folder.

## Files (Phase 1)

| Path | Purpose |
|---|---|
| `Scripts/GameManager/GameEnums.cs` | `Gender`, `Difficulty` (Normal/Hard), `DayPeriod`, `TravelMode`, `SceneNames`. |
| `Scripts/GameManager/GameEvents.cs` | Event hub: `RunStarted`, `RunLoaded`, `RunChanged`, `MessagePosted`, `RunEnded`. |
| `Scripts/GameManager/GameConfig.cs` | Global tunables, including `StartingMoney = 500` (a constant). |
| `Scripts/GameManager/DifficultySettings.cs` | Mode name and label plus the hunger and fatigue multipliers. |
| `Scripts/GameManager/GameManager.cs` | Persistent singleton: starts, continues and ends runs; handles travel and actions. |
| `Scripts/Player/RunData.cs` | Everything saved for one run. |
| `Scripts/Character/CharacterProfile.cs` | Sprites, description, lore page keys, spouse name, starting flags. |
| `Scripts/Character/CharacterDatabase.cs`, `NameRules.cs` | Character lookup and name validation. |
| `Scripts/Localization/Loc.cs`, `LocalizedText.cs` | The English string table `Resources/Localization/en.json`, with gender variants (`key.m` / `key.f`). |
| `Scripts/SaveSystem/SaveSystem.cs` | Single autosave slot plus a graveyard of dead runs. |
| `Scripts/UI/MainMenuController.cs` | New Game, Continue, Quit, and the overwrite confirmation. |
| `Scripts/UI/CharacterCreationController.cs` | Character, name, difficulty and the lore viewer. |
| `Editor/Phase1Builder.cs`, `UIBuilder.cs`, `PlaceholderArt.cs` | Build tooling. |

## Data assets

| Asset | Values |
|---|---|
| `Characters/Dong.asset` | Male, spouse Inday, lore `char.dong.lore.1–3`, starting flag `knows_mang_berto`. |
| `Characters/Inday.asset` | Female, spouse Dong, lore `char.inday.lore.1–3`, starting flag `knows_aling_nena`. |
| `Difficulty/Normal.asset` | **Tagalog (Normal)**: hunger ×1.0, fatigue ×1.0. |
| `Difficulty/Hard.asset` | **Bisaya (Hard)**: hunger ×1.6, fatigue ×1.6. |
| `Resources/GameConfig.asset` | 365 days · start 6:00 AM at home · names 2–16 characters · survival, travel and work tunables. |

**Starting money:** every run starts with exactly ₱500, whatever the character or difficulty. `GameConfig.StartingMoney` is a constant, so it can't be changed by accident in the Inspector.

**Difficulty:** the two modes differ only in how fast hunger and fatigue build up, and in how much health starving or collapsing costs. Prices, wages, travel and every other rule are identical. "Tagalog" and "Bisaya" are only the names of the modes; all text stays English.

## Dong and Inday

Dong and Inday are husband and wife who share one rented room and one budget. The player chooses whose life to live:
- **Dong** is a former dock porter. His work is heavy labor (construction, hauling, the port, parcels). It pays more per shift but is more exhausting.
- **Inday** is a fisherman's daughter from Leyte. Her work is market stalls, laundry, minding a store, the canteen and a kiosk. It pays a little less but is gentler on fatigue.

Each character has ten job options, and the other spouse appears at home as someone you can talk to. Their backstories are three pages each in `en.json` (`char.dong.lore.*`, `char.inday.lore.*`). You can read them from **Read …'s Story** in character creation, and they play as the intro of every new run.

## Testing (manual)

1. Play `MainMenu`. Everything is in English, there is no language option, and Continue is disabled when no save exists.
2. Click **New Game**. The note explains that Dong and Inday are husband and wife.
3. Switch between **Dong** and **Inday**; the preview, description and story button all change. Open **Read …'s Story**, then Close.
4. Name validation: `A` is too short, `Inday2!` has invalid characters, and `Maria Clara` is valid.
5. Pick **Tagalog (Normal)** or **Bisaya (Hard)** and read the description. The money line always says ₱500.
6. Click **Start Life**. The intro shows the character's story, then Day 1, then How to Survive.
