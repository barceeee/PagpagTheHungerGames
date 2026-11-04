# Revision 3: black sheep, menu confirm, drawer, action buttons, family menu, responsive UI

The brief mentioned an "inventory and production tracker" and browser terms. This repository is the Unity uGUI game *Pagpag: The Hunger Games*, so every change was made in the existing Unity code:
- "Browser" maps to the Unity Game view or the Device Simulator.
- "Sidebar", "modal" and "hamburger" are uGUI panels built by the editor builders (Pagpag > Build All).

## 1. Black sheep (hidden thief child)
- `Scripts/Player/RunData.cs` adds `blackSheepId`, which is saved in the run JSON, plus `moneyMissing`, a running total.
- `Scripts/Family/FamilyService.cs`:
  - `PickBlackSheep` picks a child uniformly at random.
  - `SleepTheft` computes the amount taken; its formula is documented in the code.
- `Scripts/Locations/WorldService.cs`: Sleep and Nap call the theft and append a neutral message: "When you wake up, ₱N is missing from your money."
- `Scripts/GameManager/GameManager.cs` picks the black sheep at new game. Older saves get one assigned once, on Continue.
- `Tests/EditMode/BlackSheepTests.cs` is new. `EconomyTests.cs` has adjusted bot thresholds.

Theft formula:

`amount = min(₱40, round((money − ₱400) × lerp(2%, 4%, roll))) × min(1, minutesSlept / 480)`

- Nothing is taken when money ≤ ₱400. Money never drops below 0.
- Examples:

  | Money | Taken |
  |---|---|
  | ₱500 | ₱2–4 |
  | ₱1,000 | ₱12–24 |
  | ₱1,500 | ₱22–40 |

- If there are no children, or the black sheep has died, nothing is taken.
- The UI never shows the black sheep's identity.

## 2. Menu confirmation
- `Scripts/UI/ConfirmDialog.cs` is new. It is a modal with a dimmed backdrop, Cancel focused by default, Tab/arrow focus kept inside the dialog, Escape to cancel, a backdrop click to cancel, and Enter/Space to activate the focused button.
- `Scripts/UI/World/WorldController.cs`: Menu now calls `ConfirmReturnToMenu()`.
- `Scripts/UI/FocusRing.cs` is new: a visible keyboard-focus outline.

## 3. Locations drawer
- `Scripts/UI/World/SideDrawer.cs` is new. The drawer slides in from the right, is collapsed by default, closes with ✕ or Escape, and overlays the map.
- `Editor/Phase2Builder.cs`: `BuildDrawer` replaces `BuildSidebar`.
- The drawer opens on a pin click and closes on travel or entering a location.

## 4. Action buttons
- `Scripts/UI/World/ActionButtonStyle.cs` is new. It gives the buttons distinct normal, hover, pressed and disabled states, plus a border.
- The cards are opaque, with a gold frame and a drop shadow, so they stay readable over any background art.
- Contrast is about 8:1 normal, 12:1 hover and 5.5:1 disabled, all at or above WCAG AA.

## 5. Family hamburger menu
- `Scripts/UI/World/FamilyMenu.cs` is new; it replaces `FamilyStrip.cs`.
- It sits top-left under the profile and opens a scrollable dropdown with:
  - the pantry;
  - each member's portrait, status and three stat bars;
  - critical and dead styling;
  - a red alert dot on the button.
- Clicking a row opens the existing FamilyPanel.

## 6. Responsive layout
- New components in `Scripts/UI/Responsive/`: `ResponsiveCanvas`, `FitInParent`, `DockBelow`, `SafeAreaFitter`.
- Wide screens give the extra width to the map. Narrow screens scale down proportionally.
- Phones use "compact" mode, which draws the UI 1.3× larger for touch.
- Mobile builds are landscape only.
- `Editor/UIBuilder.cs` and `Editor/Phase1Builder.cs` wire these into every scene. FamilyPanel, MinigameHost and WheelOverlay fit their windows to the screen.

## Verification
- 132 EditMode tests pass: 125 in the core/world/family/minigame/black-sheep groups and 7 in the economy group.
- Captures checked at 1920×1080, 3440×1440, 2340×1080 (phone, compact) and 1024×768.

## Known limits
- At 4:3 the Locations toggle sits over the Train Station pin. The pin is still reachable from the drawer list.
- The physical tap size on very small phones depends on the device's DPI.
