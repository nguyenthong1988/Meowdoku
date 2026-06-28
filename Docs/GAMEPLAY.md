# Meowdoku — Gameplay Spec

Gameplay description for agents/programs that generate, validate, or solve Meowdoku
levels. Pairs with `LEVEL_FORMAT.md` (the data schema). All rules below are derived
from the official store listing and the public game guide (see Sources).

> Note on terminology: in our editor/data the placed piece is called a **character**
> (currently drawn as a cat). In the original game it is always a **cat**. They are the
> same thing. A "colored region" = all board cells that share one color index.

## 1. Concept

Meowdoku is a pure-logic puzzle blending **Sudoku-style placement** with
**Minesweeper-style deduction**. The board is a square grid partitioned into colored
regions. The player places one cat per region such that no two cats conflict. Every
level is solvable by deduction alone — there is a unique solution and no guessing is
required.

## 2. The four rules (a legal solution must satisfy all)

1. **One cat per region.** Every colored region contains exactly one cat.
2. **No shared row.** No two cats are in the same row.
3. **No shared column.** No two cats are in the same column.
4. **No contact.** No two cats are adjacent in any of the 8 directions — horizontally,
   vertically, **or diagonally**. A cat blocks all 8 surrounding cells.

A cat placement is "correct" only if it is forced by some combination of: region count,
row pressure, column pressure, and the no-touching rule.

## 3. How a level file encodes the puzzle

Using the `minimal` schema (`LEVEL_FORMAT.md`):

- **Regions** come from the **color index** of each cell in `d`. All cells with the
  same lowercase/uppercase letter base (e.g. `a`/`A`) belong to the same region.
- **The solution** is encoded by the **uppercase** letters in `d`: an uppercase letter
  marks the cell where that region's cat sits. Because each color has at most one
  character (one uppercase letter), uppercase cells = the solved cat positions.
- A blank puzzle to hand to the player = the same board with all letters lowercased
  (regions/colors shown, cats hidden). The uppercase set is the answer key.

So one level string carries **both** the puzzle layout (colors/regions) and its
solution (cat positions). To present an unsolved board, render colors only and ignore
the uppercase flags; to check the player, compare against the uppercase cells.

### Implication: number of regions = number of cats = grid lines used

For an `s × s` board there are exactly as many cats as colored regions. Rules 2 and 3
imply at most one cat per row and per column, so a fully-solvable standard board
typically has **`s` regions and `s` cats** (one per row and one per column, like
Sudoku/N-queens). Region count can differ in special layouts, but a valid level must
keep one-cat-per-region compatible with the row/column/diagonal limits.

## 4. Controls & interface (reference, mobile game)

- **Double-tap** an empty cell to place a cat (only succeeds if it passes region, row,
  column, and diagonal checks).
- **Tap** a placed cat to remove it.
- **Mark** suspicious cells to block positions that cannot hold a cat (notes; helps
  deduction, no gameplay penalty).
- **Hint** — reveals/assists a next deduction.
- **Undo** — revert the last action.
- **Restart** — clear the board to its initial state.
- **Hearts** — the player has **3** hearts. A wrong placement costs one heart. Running
  out of hearts fails the run.

## 5. Difficulty

The store/guide expose **Normal**, **Hard**, **Ultra**. Our editor adds a fourth grade
**Challenge**. Mapped to the `g` field:

| `g` | Name      |
|-----|-----------|
| `n` | Normal    |
| `h` | Hard      |
| `u` | Ultra     |
| `c` | Challenge |

Difficulty is a design/label attribute of a level; it does not change the four rules.
Harder grades generally mean larger boards and/or longer deduction chains.

## 6. Validation checklist for a generated level

A level is **valid** if its solution (uppercase cells of `d`) satisfies:

- [ ] `d.length === s * s`.
- [ ] Every region (color index present in `d`) contains **exactly one** cat
      (exactly one uppercase letter for that index).
- [ ] No two cats share a row.
- [ ] No two cats share a column.
- [ ] No two cats are in each other's 8-neighborhood (no diagonal/orthogonal contact).
- [ ] Every non-empty cell's color index is within the palette
      (`< c.length`, or `< gameDefaultPalette.length` when `c` is empty).
- [ ] (Strongly recommended) The puzzle has a **unique** solution reachable by
      deduction without guessing.

### Solution-checker pseudocode

```js
// cats: array of {row, col} taken from uppercase cells of d
function isLegalSolution(cats, regionOfCell /* (r,c)->regionId */, regionIds) {
  const rows = new Set(), cols = new Set(), perRegion = {};
  for (const {row, col} of cats) {
    if (rows.has(row) || cols.has(col)) return false;   // rules 2 & 3
    rows.add(row); cols.add(col);
    const reg = regionOfCell(row, col);
    perRegion[reg] = (perRegion[reg] || 0) + 1;
  }
  // rule 1: each region exactly one cat
  for (const id of regionIds) if (perRegion[id] !== 1) return false;
  // rule 4: no two cats touch (incl. diagonal)
  for (let i = 0; i < cats.length; i++)
    for (let j = i + 1; j < cats.length; j++) {
      const a = cats[i], b = cats[j];
      if (Math.abs(a.row - b.row) <= 1 && Math.abs(a.col - b.col) <= 1) return false;
    }
  return true;
}
```

## 7. Design notes / feel

- Zen, no hard timer pressure; short focused sessions.
- Minimalist visuals: soft pastel colors, simple cats, tidy boards.
- Daily puzzles and a leaderboard for completion time exist in the live game (out of
  scope for the level data format, but useful context).

---

## Sources

- [Meowdoku Game Guide — meowdoku.org](https://www.meowdoku.org/) (rules, controls, strategy)
- [Meowdoku: Brain Puzzle Games — Google Play](https://play.google.com/store/apps/details?id=com.oakever.meowdoku&hl=en) (official description, hearts/double-tap, difficulty)
