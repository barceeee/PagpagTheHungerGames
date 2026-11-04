# Revision 4: winning condition, Infinite Mode, leaving the slums

Before this revision the game had only losing conditions. A run went on until the player died or Inday became pregnant.

## Rules
| When | What happens |
|---|---|
| 100 full days survived (day 101, 6:00 AM), everyone alive | **Victory.** Infinite Mode is unlocked for good. The victory screen offers **Finish Run**, **Keep Playing (Infinite)** and, below them, **Leave the Slums**. |
| 100 full days survived, but a family member has died | **Run over** ("Survived 100 days, but lost N family members"). There is no unlock. |
| The player dies during the action that reaches the goal | A death, not a win. |
| Infinite Mode run (picked at creation, or continued after a win) | No day goal. The run only ends in a loss or by leaving the slums. Family deaths do not end it. |
| **Leave the Slums** with ₱100,000 or more | **Best ending:** "YOU LEFT THE SLUMS". The run ends permanently. |

- "Everyone alive" means the spouse and all five children.
- The goal and quota are `GameConfig.victoryDays` (100) and `GameConfig.leaveSlumsQuota` (₱100,000).
- Leaving the slums is offered in two places:
  - The victory screen. Below the quota, the button is disabled and reads "Leave the Slums · ₱X of ₱100,000 saved".
  - The home action **Leave the slums**, only in Infinite Mode. It needs the quota and asks for confirmation first, because it ends the run.
- A normal run cannot leave before winning, even with the money.
- The ₱100,000 quota is far above what 100 days earns. Strong simulated play reached about ₱19–20k by day 60 on Normal. In practice, leaving is a long Infinite Mode goal.

## Code
- `Scripts/GameManager/VictoryRules.cs` (new) holds the pure rules: `Check`, `ContinueInInfiniteMode`, `LeaveBlockedReason`, `LeaveTheSlums`, `DaysSurvived`.
- `GameEnums.cs` adds `EndKind.Victory`, `SurvivedWithLosses` and `LeftTheSlums`.
- `LocationAction.cs` adds `ActionKind.LeaveSlums`, appended at the end so the stored values don't change.
- `RunData.cs` adds `infiniteMode`, `victoryPending` and `won`.
  - A pending victory is saved, so quitting at the victory screen shows it again on Continue.
  - No version bump is needed: the new fields default to false.
- `SaveSystem.cs` adds `InfiniteModeUnlocked` and `UnlockInfiniteMode`.
  - The unlock is a file in the save folder (`pagpag_infinite_unlocked.txt`).
  - Deleting or burying a run never removes it.
- `GameManager.cs`:
  - `Apply` checks the goal after every action.
  - `CanAct` blocks actions while a victory is pending.
  - New methods: `ContinueInInfiniteMode`, `FinishWonRun` and `LeaveTheSlums`.
  - `StartNewRun(..., infiniteMode)` honours the flag only when Infinite Mode is unlocked.
- `WorldService.cs`: the leave action is hidden outside Infinite Mode. It shows the quota as its reason and its details ("Needs ₱100,000 saved · Ends the run").
- UI:
  - `GameOverOverlay` is reused for every ending. It has a gold title for a win or for leaving, and it adds `continueButton` and `leaveButton`.
  - The dialog is 810 tall to fit a second button row.
  - Labels set from code clear their `LocalizedText` key, so enabling the overlay doesn't reset them.
  - `CharacterCreationController` adds an **Infinite Mode** option button beside the starting-money line. Before the unlock it is disabled and reads "Infinite Mode · locked (win 100 days)".
  - `HudController` day line: "Day 12/100 · Morning", or "Day 120 (Infinite) · Morning".
  - `LocationView` confirms before leaving, through `ConfirmDialog`. `ActionCard` gives the leave action the verb "Leave".
- Builders: `Phase1Builder` (Infinite option), `Phase2Builder` (end-screen buttons, home `leave_slums` action, HUD day best-fit). Run **Pagpag > Build All** after pulling.
- Strings in `en.json`:
  - `cc.infinite.locked`, `cc.infinite.off`, `cc.infinite.on`
  - `world.victory.continue`, `world.victory.finish`, `world.victory.leave`, `world.victory.leave.locked`
  - `world.leave.confirm.title`, `world.leave.confirm.body`, `world.leave.confirm.yes`, `world.leave.confirm.no`
- `Tests/EditMode/VictoryTests.cs` (new) has 17 tests. The full suite is 152 tests, all passing.

`GameConfig.campaignDays` (365) is still unused. It is separate from `victoryDays`.
