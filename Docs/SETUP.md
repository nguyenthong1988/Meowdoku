# Meowdoku — Editor Setup (wire the MonoBehaviours & prefabs)

How to take the scaffolded code (`Assets/_Game/Scripts/…`) and make a playable scene in the
Unity editor. Code can't create scenes/prefabs/addressables, so do these by hand once.

Pairs with `ARCHITECTURE.md` (what the pieces are) and `GAMEPLAY.md` / `LEVEL_FORMAT.md`.

---

## 0. Prerequisites (compile first)

1. **LitMotion** is now in `Packages/manifest.json`
   (`com.annulusgames.lit-motion`, used by `CellView` tweens). Open the project and let the
   Package Manager resolve it (needs internet the first time).
2. The ScreenNavigator types live in the **`UnityScreenNavigator.Runtime.Core.*`** namespaces
   (e.g. `UnityScreenNavigator.Runtime.Core.Page` / `.Modal`) — already used correctly by
   `ViewGameplay` / the popups / `ViewSplashScreen`.
3. `Bootloader.cs` was fixed: `ViewLayer` does not exist in this CaskFramework, so the splash push
   is `PushViewAsync("ViewSplashScreen", stack: false)`.
4. Confirm the console is error-free before continuing.

> Addressable keys are defined in code — match them exactly:
> `CellView`, `ViewGameplay`, `PopupLevelComplete`, `PopupGameOver`, and `level_1`, `level_2`, …
> (`level_{ProgressLevel}`, see `LevelAddressKeys`).

---

## 1. The CellView prefab

The board spawns one of these per cell (pooled). Sprites are optional — if left empty, runtime
placeholders (white square + circle) are used, so it works immediately.

1. Create an empty GameObject `CellView`.
2. Add three child GameObjects, each with a **SpriteRenderer**:
   - `Background` — sorting order 0
   - `Cat` — sorting order 1
   - `Mark` — sorting order 1
3. Add the **CellView** component to the root and assign:
   - `Background` → the Background SpriteRenderer
   - `Cat` → the Cat SpriteRenderer
   - `Mark` → the Mark SpriteRenderer
   - (optional) `Cat Color`, `Mark Color`
4. Drag into `Assets/_Game/Prefabs/Board/CellView.prefab`, delete from scene.
5. **Addressables**: mark the prefab Addressable, address = **`CellView`**.

> Sprites stay un-assigned for the placeholder look. To use art later, drop sprites onto the
> three renderers; the placeholder fallback only kicks in when a renderer's sprite is null.

---

## 2. The gameplay scene (Main)

Open `Assets/Scenes/Main.unity` (the scene `Boostrap` loads additively).

### 2.1 Camera
- A **Camera** with **Projection = Orthographic** (the board fits itself to this camera).
- Position it looking down +Z at the board area; `Orthographic Size` sets the zoom.

### 2.2 Board objects
1. Empty GameObject **`Board`** → add **BoardView**. Assign:
   - `Camera` → the orthographic camera
   - `Cell Root` → an empty child transform (optional; cells parent here)
   - `Cell Address` → `CellView` (default)
   - `Padding` → world-unit margin around the board (e.g. 0.5)
   - `Reveal Config` → tune the "rải" effect (duration, stagger, scatter radius, start scale, order)
2. Add **BoardInputReader** (same GameObject or a child). Tune `Config` thresholds
   (tap/double-tap/long-press/slop).

### 2.3 Composition root
Empty GameObject **`GameSceneEntry`** → add **GameSceneEntry**. Assign:
- `Board View` → the Board's BoardView
- `Input` → the BoardInputReader
- `Config` → `Hearts Max` (3), `Hints Max`

`GameSceneEntry.Start()` reads `IAssetManager` / `IUIManager` / `IProfileService` from
`GameRuntime`, `new`s the gameplay graph (no DI), and starts `profile.ProgressLevel`.

### 2.4 UI plumbing (UnityScreenNavigator)
- Ensure the **UIManager** + its view/popup containers exist in the boot flow (same setup that
  already shows `ViewSplashScreen`). `ViewGameplay` is pushed by name onto the Normal view layer;
  popups onto the Normal popup layer.
- Make sure there is an **EventSystem** in the scene (needed for UI buttons and for the board to
  ignore taps that land on the HUD).

---

## 3. UI prefabs (Canvas)

### 3.1 ViewGameplay (a `Page`)
1. Build a Canvas-based prefab with the HUD widgets, add the **ViewGameplay** component, assign:
   - `Level Label` (Text), `Hearts Root` (Transform)
   - `Hint Button`, `Undo Button`, `Restart Button`
   - `Booster Hint`, `Booster Reveal Cell`, `Booster Add Heart` (Buttons)
2. **Addressables**: address = **`ViewGameplay`**.

### 3.2 Result popups (each a `Modal`)
- `PopupLevelComplete` — add the component, assign `Next Button` (+ optional `Summary Label`).
  Addressable address = **`PopupLevelComplete`**.
- `PopupGameOver` — assign `Retry Button` (+ optional `Quit Button`).
  Addressable address = **`PopupGameOver`**.

The flow grabs the instance on load and awaits its button; no extra wiring needed.

---

## 4. Levels (Addressables)

1. Author level JSON per `LEVEL_FORMAT.md`, e.g. `Assets/_Game/Levels/level_1.json`:
   ```json
   { "l": 1, "s": 5, "g": "n", "c": [], "d": "Ab.c.dE...." }
   ```
2. Import as a **TextAsset**, mark Addressable, address = **`level_1`** (then `level_2`, …).
3. `GameFlow` loads `level_{ProgressLevel}`; a fresh profile starts at `ProgressLevel = 1`.

---

## 5. Run & verify

Play from the boot scene (`Boostrap`). Expected:

1. Board builds and the cells **scatter in** (LitMotion reveal). Input is off until it finishes.
2. **Double-tap** a cell → place a cat (correct = stays; wrong = a heart is lost + shake;
   rule-illegal = shake, no heart). **Tap** a cat → remove. **Long-press** → toggle a note.
3. Lose all hearts → **PopupGameOver** (Retry). Solve the board → **PopupLevelComplete** (Next →
   `ProgressLevel++`).
4. **Boosters** (HUD buttons): Hint reveals a cat; Reveal Cell enters targeting (next tap picks a
   region, its cat is placed); Add Heart refills one. Board input is blocked while a booster runs.

---

## 6. Field reference (inspector labels strip the leading `_`)

| Component | Field | Type / note |
|---|---|---|
| GameSceneEntry | Board View / Input | BoardView / BoardInputReader |
| | Config | HeartsMax, HintsMax |
| BoardView | Camera | orthographic Camera |
| | Cell Root | Transform (cell parent) |
| | Cell Address | addressable key (`CellView`) |
| | Padding | float |
| | Reveal Config | CellDuration, StaggerStep, ScatterRadius, StartScale, Order |
| BoardInputReader | Config | TapMaxDuration, DoubleTapWindow, LongPressDuration, TapSlopPixels |
| CellView | Background / Cat / Mark | SpriteRenderer ×3 |
| | Cat Color / Mark Color | Color |
| ViewGameplay | Level Label / Hearts Root | Text / Transform |
| | Hint/Undo/Restart Button | Button |
| | Booster Hint/RevealCell/AddHeart | Button |
| PopupLevelComplete | Next Button / Summary Label | Button / Text |
| PopupGameOver | Retry Button / Quit Button | Button |

---

## 7. Still TODO (left as stubs / notes)

- **Hint vs booster Hint** share the session's `HintsRemaining` allowance — decide if the booster
  should bypass it.
- **Targeting cancel**: `BoardInputHandler.PickCellAsync` doesn't yet honor `CancellationToken`.
- **Reveal ordering**: only `Random` is implemented; other `RevealOrder` values fall back to
  row-major.
- **Booster/heart visual feedback** (heart pop, reveal highlight) are marked TODO.
- **Granting starting boosters** (e.g. `profile.TryClaimOnce`) isn't wired yet — booster counts
  start at 0, so grant some via the Profile wallet to test.
