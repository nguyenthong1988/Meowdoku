# Meowdoku — Architecture (proposal for review)

Design for the runtime gameplay, rendering, and UI layers of Meowdoku, building on
the data/level code that already exists. Board rendering is **world-space sprites**
(orthographic camera). This pass is a **scaffold**: interfaces + skeleton classes +
DI wiring; gameplay logic bodies are left as TODO. Pairs with `GAMEPLAY.md` (rules)
and `LEVEL_FORMAT.md` (data schema).

> Status: **awaiting approval before any code is written.** Nothing below is created
> yet — this document is the thing to review.

## 1. Principles

The codebase already establishes a clear style, and the new code follows it:

- **No DI / no service locator — explicit wiring.** Objects are created and connected
  by hand in one composition root (`GameSceneEntry`): `new` the concrete classes and
  pass dependencies through constructors; MonoBehaviours get their references through
  `[SerializeField]` (exactly like `Bootloader` already holds `[SerializeField] UIManager`).
  Interfaces are kept only where they aid testing/swapping — nothing is registered into
  `GameRuntime` for gameplay.
- **Framework services are resolved once, then passed by reference.** CaskFramework
  services (`IUIManager`, `IAssetManager`, **`IProfileService`**) already live in
  `GameRuntime`. The composition root reads them once
  (`GameRuntime.Get<IProfileService>()` / a `[SerializeField]`) and hands them down —
  gameplay classes still never touch the locator. See §11 for the Profile integration.
- **Pure core, dumb view.** All game rules and state live in plain C# with no
  `UnityEngine` rendering dependency (the existing `Data` / `Level` layers set this
  precedent). The world-space board only *draws* state and *forwards* input — it holds
  no rules.
- **Non-throwing, value-typed, allocation-light.** Per-cell data stays in structs;
  hot paths avoid GC; failures are reported via results/events, not exceptions
  (mirrors `LevelValidationResult` / `LevelReadResult`).
- **Async via UniTask.** Loading uses `Cysharp.Threading.Tasks`, like the rest.

## 2. Layered view

```
┌──────────────────────────────────────────────────────────────┐
│  Flow            GameFlow · GameSceneEntry (composition root)   │  orchestration + wiring
├──────────────────────────────────────────────────────────────┤
│  Board (view)         UI (HUD)                                  │  presentation
│  world-space sprites  ViewGameplay : Page + popups             │
│  BoardView/CellView   (UnityScreenNavigator)                   │
│  BoardInputReader     IUIManager                               │
├──────────────────────────────────────────────────────────────┤
│  Gameplay (core, pure C#)                                       │  rules + runtime state
│  IGameSession · BoardState · IMoveEvaluator · IHintProvider    │
├──────────────────────────────────────────────────────────────┤
│  Level   (exists)   parser · validator · reader (Addressables) │  load + validate
├──────────────────────────────────────────────────────────────┤
│  Data    (exists)   LevelData · CellData · CatPlacement · …    │  immutable level model
└──────────────────────────────────────────────────────────────┘
```

Dependency direction is strictly downward. The core knows nothing about rendering;
`Board` and `UI` depend on the core only through `IGameSession` and its C# events.

## 3. New namespaces & files

Everything lives under `Assets/_Game/Scripts`, default `Assembly-CSharp` (no asmdef
in `_Game` today), namespaces under `Cast.Game.*` to match existing code.

```
Scripts/
  Data/        (exists)
  Level/       (exists)
  Gameplay/                 ← NEW  namespace Cast.Game.Gameplay  (pure C#)
    PlayerMark.cs           enum None | Cat | Blocked
    GamePhase.cs            enum Loading | Revealing | Playing | Result
    GameResult.cs           struct: Outcome(Won|Lost) + hearts/hints/elapsed/moves
    MoveOutcome.cs          enum Placed | RemovedCat | Marked | … | RejectedIllegal | Wrong
    CellChange.cs           struct (row,col,from,to) for change events
    MoveRecord.cs           struct for the undo stack
    BoardState.cs           mutable player layer over immutable LevelData
    IMoveEvaluator.cs / MoveEvaluator.cs       the 4 rules + solution check
    IHintProvider.cs  / HintProvider.cs        next deducible cell
    GameSessionConfig.cs    hearts/hints counts
    IGameSession.cs   / GameSession.cs         lifecycle: Setup → Begin → PlayToEnd
  Booster/                  ← NEW  namespace Cast.Game.Booster  (pure C# + thin async)
    BoosterType.cs          enum (the 3 boosters) — scenario-driven
    BoosterResult.cs        struct: Applied | Cancelled | Rejected (+reason)
    BoosterContext.cs       what a booster may touch (session, board, targeting)
    IBooster.cs             one booster: CanUse + async UseAsync
    IBoosterInventory.cs / ProfileBoosterInventory.cs  counts/spend/grant over IProfileService wallet
    IBoosterService.cs   / BoosterService.cs      orchestrate + block input (async)
    Boosters/HintBooster.cs · RevealCellBooster.cs · AddHeartBooster.cs
  Board/                    ← NEW  namespace Cast.Game.Board  (MonoBehaviours)
    BoardLayout.cs          pure (row,col) ↔ world, camera fit
    BoardInputReader.cs     MonoBehaviour: reads touch/mouse, detects gestures
    BoardInputConfig.cs     tap/double-tap/long-press thresholds
    BoardInputMode.cs       enum Play | Targeting | Locked
    IBoardInputGate.cs      SetMode(mode) — how the booster service blocks input
    IBoardTargeting.cs      PickCellAsync(...) — player-picked cell for targeted boosters
    CellGesture.cs          struct/enum: (row,col,gesture)
    CellViewPool.cs         pooling via IAssetManager (InstantiateAsync/WeakRelease)
    CellView.cs             one cell: color + cat + mark sprites
    BoardView.cs            BuildAsync (spawn hidden) + PlayRevealAsync + refresh/route
    IBoardRevealAnimation.cs                    the "rải cell" effect contract
    ScatterRevealAnimation.cs / BoardRevealConfig.cs  scatter-in impl + tuning
    DefaultPalette.cs       gameDefaultPalette (fallback colors)
  UI/
    Views/
      ViewSplashScreen.cs   (exists)
      ViewGameplay.cs       ← NEW  Page: hearts, hint/undo/restart, level label
    Popups/
      PopupLevelComplete.cs ← NEW  Page (popup layer): Next
      PopupGameOver.cs      ← NEW  Page (popup layer): Retry
  Flow/                     ← NEW  namespace Cast.Game.Flow
    GameFlow.cs             orchestrates load → reveal → play → result, next/retry
    ResultChoice.cs         enum Next | Retry | Quit (what the result popup returns)
    GameSceneEntry.cs       MonoBehaviour composition root in Main scene
```

## 4. Gameplay core (the part to review most carefully)

### 4.1 State model

`LevelData` stays immutable (it carries the solution). The player's progress is a thin
mutable overlay so we never mutate the loaded level:

```csharp
public enum PlayerMark : byte { None = 0, Cat = 1, Blocked = 2 }

public sealed class BoardState
{
    public LevelData Level { get; }
    public int Size { get; }
    public int CatsPlaced { get; }

    public BoardState(LevelData level);          // all cells start None

    public PlayerMark GetMark(int row, int col);
    public void       SetMark(int row, int col, PlayerMark mark);  // raises no events itself
    public bool       IsSolved();                // every solution cat placed, nothing extra
    public void       Reset();                   // back to all-None
}
```

`PlayerMark.Blocked` is the "mark suspicious cell" note from `GAMEPLAY.md §4` (no
gameplay effect; deduction aid only).

### 4.2 Rules

The four rules and the solution check sit behind one interface so they're unit-testable
without Unity. It reuses the same logic the existing `LevelValidator` already encodes.

```csharp
public interface IMoveEvaluator
{
    // Legal under rules 1–4 given the cats already placed (region/row/col/8-neighbour)?
    bool IsLegalPlacement(BoardState state, int row, int col);

    // Is this the deduced-correct cell (an uppercase solution cell of d)?
    bool IsSolutionCell(BoardState state, int row, int col);

    // Convenience used by the session.
    MoveOutcome EvaluatePlacement(BoardState state, int row, int col);
}
```

**Proposed move semantics** (from `GAMEPLAY.md §4`, please confirm):

- A double-tap that would break a hard rule (touches another cat, shares a row/column,
  or the region already has a cat) → `RejectedIllegal`: no placement, no heart lost,
  just a shake/feedback.
- A double-tap that is rule-legal but is **not** the unique solution cell → `Wrong`:
  costs one heart (the "wrong placement" penalty). Hearts start at 3; 0 hearts → lose.
- A double-tap on the correct cell → `Placed`.

This is the one genuinely game-feel-defining choice; it's isolated in `MoveEvaluator`
so it can be tuned without touching anything else.

### 4.3 Session — the central controller

`IGameSession` owns one playthrough and is the **only** thing the view and HUD talk to.
Its lifecycle has explicit phases so the flow can sit between *board built* and *input
on* — that gap is exactly where the reveal ("rải") animation plays. Per-move
communication is via C# events; the overall result is awaitable.

```csharp
public interface IGameSession
{
    LevelData  Level   { get; }
    BoardState Board   { get; }
    GamePhase  Phase   { get; }   // Loading → Revealing → Playing → Result
    int Hearts         { get; }
    int HeartsMax      { get; }
    int HintsRemaining { get; }

    event Action<GamePhase>           PhaseChanged;
    event Action<CellChange>          CellChanged;   // redraw one cell
    event Action<int>                 HeartsChanged; // new count
    event Action<MoveOutcome,int,int> MoveRejected;  // illegal/wrong feedback at (r,c)

    // ── lifecycle (driven by GameFlow) ──────────────────────────────
    void Setup(LevelData level);  // build BoardState, hearts=max, Phase=Loading, input OFF
    void Begin();                 // Phase=Playing, input ON  (called AFTER the reveal)
    UniTask<GameResult> PlayToEndAsync(CancellationToken ct = default);
                                  // completes when the board is solved or hearts hit 0

    // ── player actions (no-op unless Phase==Playing) ────────────────
    MoveOutcome PlaceCat(int row, int col);   // double-tap
    MoveOutcome RemoveCat(int row, int col);  // tap on a placed cat
    MoveOutcome ToggleMark(int row, int col); // long-press note
    bool Undo();                              // revert last action (MoveRecord stack)
    bool Hint();                              // reveal one deducible cell, costs a hint
    void Restart();                           // clear board to initial state
    void Dispose();
}

public readonly struct GameResult
{
    public readonly bool Won;
    public readonly int  HeartsLeft;
    public readonly int  HintsUsed;
    public readonly float Elapsed;
    public readonly int  Moves;
}
```

`GameSession` depends on `IMoveEvaluator`, `IHintProvider`, and a `GameSessionConfig`
(`HeartsMax = 3`, `HintsMax`). It maintains the undo stack of `MoveRecord` and flips
`Phase` to `Result`; `PlayToEndAsync` resolves with the `GameResult` at that point.
`Begin()` is deliberately separate from `Setup()` so input stays off during the reveal.

### 4.4 Hints

```csharp
public interface IHintProvider
{
    bool TryGetHint(BoardState state, out int row, out int col); // next solution cell to reveal
}
```

The stub returns the first not-yet-placed solution cell. A real deduction-based hint
(per `GAMEPLAY.md §4`) can replace it later without changing callers.

## 5. Board rendering (world-space sprites)

A pure layout helper keeps all coordinate math out of MonoBehaviours and testable:

```csharp
public readonly struct BoardLayout
{
    public readonly int     Size;
    public readonly float   CellSize;
    public readonly Vector2 Origin;          // world centre of cell (0,0)

    public Vector3 CellToWorld(int row, int col);
    public bool    WorldToCell(Vector3 world, out int row, out int col);

    // Compute cell size + origin so an s×s board fits the camera view with padding.
    public static BoardLayout Fit(int size, Camera cam, float padding);
}
```

### 5.1 Touch input — detection & handling

This was thin in the first draft; here it is as a first-class component. Because the
board is world-space, **nothing happens for free** — we read the pointer, decide the
gesture, convert the screen point to a board cell, and only then call the session.

`BoardInputReader` (MonoBehaviour) does all four steps each frame:

```csharp
public enum PointerGesture : byte { Tap, DoubleTap, LongPress }

public readonly struct CellGesture
{
    public readonly int Row, Col;
    public readonly PointerGesture Gesture;
}

[Serializable]
public sealed class BoardInputConfig
{
    public float TapMaxDuration   = 0.25f; // press shorter than this = a tap
    public float DoubleTapWindow  = 0.30f; // two taps on same cell within this = double-tap
    public float LongPressDuration= 0.40f; // held longer than this (no move) = long-press
    public float TapSlopPixels    = 24f;   // move beyond this = drag, not a tap
}

public sealed class BoardInputReader : MonoBehaviour
{
    public event Action<CellGesture> Gesture;     // already resolved to a board cell

    public void Bind(BoardLayout layout, Camera cam);
    public void SetEnabled(bool on);              // disabled while loading / on win-lose
}
```

**Detection logic** (single active pointer; touch on device, mouse in editor — read via
the Input System since the project already ships `InputSystem_Actions`):

1. **Press down** → store screen position + timestamp; start a long-press timer.
2. **Still held**, hasn't moved past `TapSlopPixels`, elapsed > `LongPressDuration` →
   emit **LongPress** once, then swallow the eventual release.
3. **Release** within `TapMaxDuration` and inside the slop radius → it's a tap. If the
   *previous* tap landed on the **same cell** within `DoubleTapWindow` → emit
   **DoubleTap**; otherwise hold it as a pending single tap that promotes to **Tap** when
   the double-tap window expires.
4. Moves beyond the slop → treated as a drag and ignored (no gesture).

**Screen → cell**: `cam.ScreenToWorldPoint(pointer)` → `BoardLayout.WorldToCell(world,
out row, out col)`. If the point is outside the board, no event is raised.

**Don't steal HUD taps**: before processing, skip if
`EventSystem.current.IsPointerOverGameObject(...)` so taps on the hint/undo/restart
buttons never reach the board.

### 5.2 BoardView — drawing + routing

`BoardView` (MonoBehaviour) is the glue between `BoardInputReader` and `IGameSession`.
It holds both as `[SerializeField]` / passed-in references (no DI):

- `BuildAsync(level)` spawns one pooled `CellView` per cell (hidden) via `CellViewPool`
  and fits the camera with `BoardLayout`.
- `Bind(IGameSession session)` — subscribes to `CellChanged`/`MoveRejected`, then
  `boardInput.Bind(layout, cam)` and subscribes to `BoardInputReader.Gesture`.
- **Gesture → session routing** (per `GAMEPLAY.md §4`):
  `DoubleTap → session.PlaceCat`, `Tap → session.RemoveCat`,
  `LongPress → session.ToggleMark`.
- On `MoveRejected` it plays a shake on the offending cell; on `CellChanged` it
  refreshes just that cell.

Two-step spawn so the reveal can be staged:

```csharp
public sealed class BoardView : MonoBehaviour
{
    public UniTask BuildAsync(LevelData level);            // spawn all cells in HIDDEN state
    public UniTask PlayRevealAsync(CancellationToken ct);  // run the "rải" effect, await it
    // ... Bind(session), refresh, routing as above
}
```

### 5.3 The "rải cell" reveal effect

The opening scatter is its own swappable contract so the feel can be tuned (or replaced
with fade/wave/spiral) without touching the board or the flow:

```csharp
public interface IBoardRevealAnimation
{
    // Animate every cell from its hidden state into place. Awaited by the flow,
    // so the game only becomes playable once the cells have "landed".
    UniTask PlayAsync(IReadOnlyList<CellView> cells, BoardLayout layout,
                      CancellationToken ct = default);
}

[Serializable]
public sealed class BoardRevealConfig
{
    public float CellDuration   = 0.28f;  // per-cell move/scale time
    public float StaggerStep    = 0.015f; // delay added per cell (the "rải" cascade)
    public float ScatterRadius  = 1.2f;   // world units cells start away from target
    public float StartScale     = 0.3f;   // cells scale up from this
    public RevealOrder Order    = RevealOrder.DiagonalSweep; // RowByRow | Radial | Random | DiagonalSweep
}
```

`ScatterRevealAnimation` (the default impl) places each `CellView` at a random offset
within `ScatterRadius` at `StartScale`/alpha 0, then tweens them to their
`BoardLayout.CellToWorld(row,col)` target with a per-cell `StaggerStep` delay following
`Order`. `CellView` exposes `SetHidden(offset, scale)` and `AnimateIn(target, duration)`
hooks for it. (Tweens via the project's existing tween lib or a small UniTask-based
lerp — confirm which you prefer.)

`CellView` (MonoBehaviour, pooled) holds three `SpriteRenderer`s — colored background,
cat, mark — and exposes `SetColor`, `SetMark`, `PlayPlace`, `PlayShake`. Colors come
from `level.Colors`, falling back to `DefaultPalette.Colors` when the level used the
default palette (`LevelData.Colors == null`, per `LEVEL_FORMAT.md §c`). Pooling is
backed by `IAssetManager` (`InstantiateAsync` / `WeakRelease`), consistent with how
the framework already loads prefabs.

## 6. HUD & popups (UnityScreenNavigator)

`ViewGameplay : Page` is the on-screen overlay (a UI Canvas on top of the world-space
board). It binds to the same `IGameSession`: renders the heart row from
`HeartsChanged`, wires the Hint / Undo / Restart buttons to the session, shows the
level label, and on `Solved` / `GameOver` pushes a popup through `IUIManager`
(`PushPopupAsync`). `PopupLevelComplete` (Next) and `PopupGameOver` (Retry) are thin
`Page`s on the popup layer that call back into `IGameFlow`. This matches the existing
`ViewSplashScreen` + `Bootloader.PushViewAsync(...)` pattern.

## 7. Session flow: load → reveal → play → result

This is the spine you asked for. `GameFlow` runs one async sequence and owns the phase
transitions; everything else just reacts.

```
GamePhase:   Loading ─▶ Revealing ─▶ Playing ─▶ Result ─┐
                ▲                                         │ Next  → next level
                └────────────── Retry (same level) ──────┘ Quit  → menu
```

```csharp
public sealed class GameFlow            // plain class, constructed by hand
{
    // No local level counter: the current level IS profile.ProgressLevel (1-based).
    public int CurrentLevelId => _profile.ProgressLevel;
    public GameFlow(ILevelDataReader reader, IGameSession session,
                    BoardView board, IUIManager ui, IProfileService profile);

    public UniTask StartLevelAsync();   // loads CurrentLevelId
    public UniTask NextLevelAsync();    // profile.Advance(), then StartLevelAsync()
    public UniTask RetryAsync();        // same level, replay from reveal
}
```

Progression lives in CaskFramework's `IProfileService`: `ProgressLevel` is the level to
load (and the number shown to the player); winning calls `profile.Advance()` which also
persists. `LevelAddressKeys.ForLevel(CurrentLevelId)` resolves the addressable.

The whole sequence, step by step:

```csharp
async UniTask StartLevelAsync()
{
    // 1) LOAD  (Phase = Loading) — level id comes from the profile
    LevelReadResult read = await _reader.ReadLevelAsync(_profile.ProgressLevel);
    if (!read.Success) { /* surface read.Validation, bail */ return; }

    _session.Setup(read.Level);          // BoardState built, hearts = max, input OFF
    await _board.BuildAsync(read.Level); // spawn cells HIDDEN
    _board.Bind(_session);
    await _ui.PushViewAsync("ViewGameplay", stack: false, layer: ViewLayer.Normal);

    // 2) REVEAL — the "rải cell" effect  (Phase = Revealing)
    await _board.PlayRevealAsync(_ct);   // cells scatter into place; awaited

    // 3) PLAY  (Phase = Playing)
    _session.Begin();                    // input ON
    GameResult result = await _session.PlayToEndAsync(_ct); // resolves on win/lose

    // 4) RESULT  (Phase = Result)
    // Won → popup offers Next (→ NextLevelAsync advances); Lost → offers Retry.
    ResultChoice choice = await ShowResultAsync(result);
    if (choice == ResultChoice.Next)  await NextLevelAsync();  // Advance() lives here
    else if (choice == ResultChoice.Retry) await RetryAsync();
}

async UniTask<ResultChoice> ShowResultAsync(GameResult r)
{
    string popup = r.Won ? "PopupLevelComplete" : "PopupGameOver";
    await _ui.PushPopupAsync(popup, /* pass r, await user button */ …);
    return /* Next | Retry | Quit from the popup */;
}
```

Each numbered step maps 1:1 to your flow: **load level → rải → chơi game → result**.
The reveal sits between *build* and *begin*, which is the only reason `Setup`/`Begin`
are split. `RetryAsync` reuses the loaded `LevelData` (re-`Setup` + re-reveal); only
`NextLevelAsync` hits the reader again.

**Composition root** — `GameSceneEntry` (MonoBehaviour in `Main.unity`) is the single
place wiring happens. It holds scene objects via `[SerializeField]` and `new`s the rest;
no container, no `GameRuntime` registration:

```csharp
public sealed class GameSceneEntry : MonoBehaviour
{
    [SerializeField] BoardView        _boardView;
    [SerializeField] BoardInputReader _input;
    [SerializeField] UIManager        _uiManager;     // direct ref, like Bootloader
    [SerializeField] AssetManager     _assetManager;
    [SerializeField] GameSessionConfig _config;

    async void Start()
    {
        var profile   = GameRuntime.Get<IProfileService>();   // framework service, read once
        var parser    = new LevelParser();
        var reader    = new LevelDataReader(_assetManager, parser);
        var evaluator = new MoveEvaluator();
        var hints     = new HintProvider();
        var session   = new GameSession(evaluator, hints, _config);
        var inventory = new ProfileBoosterInventory(profile);
        var boosters  = new BoosterService(session, _boardView, inventory /*, boosters[] */);
        var flow      = new GameFlow(reader, session, _boardView, _uiManager, profile);

        _boardView.AttachInput(_input);               // BoardView already has the input ref too
        await flow.StartLevelAsync();                 // loads profile.ProgressLevel
    }
}
```

`ViewGameplay` and the popups get the `IGameSession` / `GameFlow` / `IBoosterService`
handed to them in `Bind(...)` right after they're pushed — again by reference, not
resolved from a locator. `IProfileService` is the one thing read from `GameRuntime`,
and only here at the root.

## 8. Per-action flows (inside the Playing phase)

The macro sequence is §7; these are the in-play interactions, all gated on
`Phase == Playing`:

- **Place cat** — DoubleTap → `BoardInputReader.Gesture` → `BoardView` → `session.PlaceCat`
  → `MoveEvaluator` decides `Placed` / `Wrong` / `RejectedIllegal`. `Placed` →
  `CellChanged` (board draws cat) + push `MoveRecord`. `Wrong` → `HeartsChanged` +
  `MoveRejected` (shake); if hearts hit 0 → `Phase = Result`, `PlayToEndAsync` resolves
  `Won=false`. After any placement, if `Board.IsSolved()` → `Phase = Result`,
  `PlayToEndAsync` resolves `Won=true`.
- **Remove cat** — Tap on a placed cat → `session.RemoveCat` → `CellChanged`.
- **Mark** — LongPress → `session.ToggleMark` → `CellChanged` (no penalty).
- **Undo / Restart** — pop `MoveRecord` / `BoardState.Reset()` → `CellChanged` events.
- **Win / Lose handoff** — `PlayToEndAsync` returns the `GameResult`; `GameFlow`
  shows `PopupLevelComplete` (Next → `NextLevelAsync`) or `PopupGameOver`
  (Retry → `RetryAsync`).

## 9. Boosters (async + input blocking)

Boosters are consumables the player spends during the **Playing** phase. The design has
three pieces: a generic `IBooster` contract, an inventory, and a `BoosterService` that
runs a booster **asynchronously while normal board input is blocked**.

### 9.1 Why blocking matters

A booster animates and may wait for the player to pick a target. During that whole time
a stray double-tap must not place a cat. So the service flips the board into a non-play
input mode and only restores `Play` in a `finally` — input is guaranteed to come back
even if the booster is cancelled or throws.

```csharp
public enum BoardInputMode : byte { Play, Targeting, Locked }

public interface IBoardInputGate          // BoardView implements this
{
    BoardInputMode Mode { get; }
    void SetMode(BoardInputMode mode);     // Locked = ignore all; Targeting = capture one cell
}
```

### 9.2 The booster contract

Some boosters are instant (no target), some need the player to point at a cell. Both are
the same interface; the targeted ones await a pick through `IBoardTargeting`.

```csharp
public enum BoosterType : byte
{
    Hint       = 0,  // auto-target: reveal the next deducible cat        (instant)
    RevealCell = 1,  // player taps a cell → place its region's correct cat (targeted)
    AddHeart   = 2,  // refill one heart                                   (instant)
}

public readonly struct BoosterResult
{
    public readonly BoosterType Type;
    public readonly bool   Applied;     // false when rejected or cancelled
    public readonly string Reason;
    public static BoosterResult Ok(BoosterType t);
    public static BoosterResult Rejected(BoosterType t, string reason);
    public static BoosterResult Cancelled(BoosterType t);
}

public sealed class BoosterContext      // the surface a booster is allowed to use
{
    public IGameSession    Session   { get; }
    public BoardView       Board     { get; }
    public IBoardTargeting Targeting { get; }   // request a player-picked cell
}

public interface IBooster
{
    BoosterType Type           { get; }
    bool        RequiresTarget { get; }
    bool        CanUse(IGameSession session);                 // hearts<max, hints>0, not solved, …
    UniTask<BoosterResult> UseAsync(BoosterContext ctx, CancellationToken ct);
}

public interface IBoardTargeting
{
    // Enter Targeting mode and resolve with the cell the player taps (or cancel).
    UniTask<(bool ok, int row, int col)> PickCellAsync(CancellationToken ct = default);
}
```

### 9.3 Inventory + service

The inventory is **not a new store** — it is a thin typed view over the CaskFramework
`IProfileService` wallet (§11). `BoosterType` becomes a wallet key via
`ProfileKey.Of(type)`, so counts persist for free and the wallet's animated *display*
value drives the HUD badge.

```csharp
public interface IBoosterInventory
{
    int  Count(BoosterType type);
    bool TrySpend(BoosterType type);
    void Grant(BoosterType type, int amount);
    event Action<BoosterType,int> CountChanged;   // HUD badges
}

// Default impl wraps IProfileService:
//   Count(t)     => profile.GetBalance(t)              // enum overload, key = "BoosterType.Hint"
//   TrySpend(t)  => profile.Sink(t, 1)
//   Grant(t,n)   => profile.Source(t, n)
//   CountChanged <= profile.OnBalanceChanged, filtered with key.Is(t)
// Starting boosters are granted once via profile.TryClaimOnce(bit).

public interface IBoosterService
{
    UniTask<BoosterResult> UseAsync(BoosterType type, CancellationToken ct = default);
    bool IsBusy { get; }                           // true while a booster runs
    event Action<BoosterType>   BoosterStarted;
    event Action<BoosterResult> BoosterFinished;
}
```

The service is where **async + input blocking** live:

```csharp
async UniTask<BoosterResult> UseAsync(BoosterType type, CancellationToken ct)
{
    if (IsBusy) return BoosterResult.Rejected(type, "busy");
    var booster = _boosters[type];
    if (_inventory.Count(type) <= 0 || !booster.CanUse(_ctx.Session))
        return BoosterResult.Rejected(type, "unavailable");

    IsBusy = true;
    BoosterStarted?.Invoke(type);
    _inputGate.SetMode(BoardInputMode.Locked);     // ← block normal gameplay input
    try
    {
        BoosterResult r = await booster.UseAsync(_ctx, ct);   // may flip to Targeting inside
        if (r.Applied) _inventory.TrySpend(type);
        return r;
    }
    finally
    {
        _inputGate.SetMode(BoardInputMode.Play);   // ← always restore input
        IsBusy = false;
        BoosterFinished?.Invoke(/* r */);
    }
}
```

### 9.4 The three boosters (scenario-driven — swap as you like)

- **HintBooster** *(instant)* — `IHintProvider.TryGetHint` → highlight + place the cat,
  await the animation, done. Input stays `Locked` the whole time.
- **RevealCellBooster** *(targeted)* — `await ctx.Targeting.PickCellAsync(ct)`; the board
  is in `Targeting` mode so exactly one tap is captured (everything else ignored). Then
  place that region's correct cat and await the reveal animation. Cancel → `Cancelled`,
  nothing spent.
- **AddHeartBooster** *(instant)* — if hearts < max, `session` refills one heart, await
  the heart pop animation.

`ViewGameplay` shows one button per booster with its inventory badge; buttons are
disabled while `IBoosterService.IsBusy` (and when `Count == 0`). The whole thing is
created in `GameSceneEntry` and handed `session` + `boardView` + `inventory` by
reference — no DI.

## 10. Open decisions (please confirm before scaffolding)

1. **Heart penalty model** (§4.2): rule-illegal taps rejected free; rule-legal-but-wrong
   taps cost a heart. OK?
2. **Session lifetime**: reuse one `GameSession` and `Start(level)` per level (simple,
   single board) vs. a fresh instance each level. Proposal: reuse + `Start(level)`.
3. **Default palette source**: a static `DefaultPalette` in code vs. a `ScriptableObject`
   asset. Proposal: static now, swappable later.
4. **Mark gesture**: long-press for `ToggleMark` (no dedicated mark button in the HUD).
   OK, or add a mode toggle button instead?
5. **Input backend**: read touch/mouse via the Input System (`InputSystem_Actions` is
   already in the project). OK, or prefer legacy `Input.touches`?
6. **The 3 boosters** (§9.4): proposal is Hint / RevealCell / AddHeart. Confirm these or
   tell me your three scenarios so the `BoosterType` enum + impls match.

7. **Profile-backed progression & wallet** (§11): use `IProfileService.ProgressLevel`
   as the level id and `Advance()` on win; back booster counts with the Profile wallet
   keyed by `BoosterType`. OK? Also: should hearts be per-run only (proposal) or also
   persisted/refillable via the wallet?

Once these are confirmed I'll create the files in §3 as interfaces + compiling stubs
(method bodies `// TODO` or returning `NoOp`) plus the `GameSceneEntry` composition
root (manual wiring, no DI), leaving prefabs/scene hookup for the Unity editor.

## 11. CaskFramework integration (incl. the Profile feature)

Meowdoku consumes these framework services; none are re-implemented:

- **`IUIManager`** — push `ViewGameplay` and the result popups (already used by
  `Bootloader`).
- **`IAssetManager`** — load level JSON (`LevelDataReader`, exists) and pool `CellView`
  prefabs (`GetTextAsync` / `InstantiateAsync` / `WeakRelease`).
- **`IProfileService`** (the feature you added, `CaskFramework.Profile`) — used two ways:
  - **Progression.** `ProgressLevel` (1-based, persisted) is the current level id; the
    flow loads `LevelAddressKeys.ForLevel(ProgressLevel)` and calls `Advance()` on a win
    (it persists automatically). `OnLevelChanged` can refresh the HUD level label.
  - **Booster wallet.** The generic `key → amount` wallet stores booster counts. The
    game owns the `BoosterType` enum; `ProfileServiceExtensions` give type-safe calls —
    `profile.GetBalance(BoosterType.Hint)`, `profile.Sink(BoosterType.Hint, 1)`,
    `profile.Source(...)` — with keys produced by `ProfileKey.Of`. The animated
    **display** value (`GetDisplay` / `AddDisplay` / `OnDisplayChanged`) drives HUD badge
    count-ups; `TryClaimOnce(bit)` grants starting boosters exactly once.

Resolution stays at the composition root only: `GameRuntime.Get<IProfileService>()` in
`GameSceneEntry`, then passed by reference — gameplay code never calls the locator.

> When scaffolding I'll re-read `CaskFramework/Runtime/Profile/*` (and its `CLAUDE.md`
> conventions) so the stubs match the real `IProfileService` / `ProfileKey` /
> `ProfileServiceExtensions` signatures exactly.
