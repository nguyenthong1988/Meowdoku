# Meowdoku Level Format (`minimal` schema)

Spec for the JSON produced by the Meowdoku Level Editor. Written so another agent/program can parse a level file into game data without ambiguity.

## File

- One JSON object per level, UTF-8.
- Suggested filename: `level_<l>.json` (e.g. `level_1.json`).
- The root variable name when embedding is `minimal`.

## Schema

```json
{ "l": 1, "s": 9, "g": "n", "c": [], "d": "aab.cAB..." }
```

| Field | Type | Meaning | Constraints |
|-------|------|---------|-------------|
| `l` | int | Level id | ≥ 1, unique per level |
| `s` | int | Board size. Board is square `s × s` | 3–15 |
| `g` | string | Difficulty grade | one of `n`, `h`, `u`, `c` |
| `c` | string[] | Color palette as hex strings | usually **empty** — see Colors |
| `d` | string | Board data, row-major | `d.length === s * s` |

### `g` — difficulty

| Value | Meaning   |
|-------|-----------|
| `n`   | normal    |
| `h`   | hard      |
| `u`   | ultra     |
| `c`   | challenge |

### `c` — colors

The editor exports `c` as an **empty array** `[]`. The game is expected to supply
its own default color list and map it **by index order**: data letter `a` → color 0,
`b` → color 1, `c` → color 2, etc.

`c` MAY contain explicit hex colors (e.g. `["#F2C14E","#E89BB0",...]`). When present,
index into it the same way (letter `a` = `c[0]`). When absent or empty, fall back to
the game's default palette. Parsers must accept both.

## `d` — board data string

Row-major. The cell at row `r`, column `col` is character index `r * s + col`
(0-based). Each character encodes one cell:

| Character | Meaning |
|-----------|---------|
| `.` | empty cell — no color, no character |
| `a`–`z` (lowercase) | filled cell, color index = `char - 'a'` (0–25), **no** character |
| `A`–`Z` (uppercase) | filled cell, color index = `lowercase(char) - 'a'`, **has** a character (a "cat") on it |

So a cell is "filled" iff it is not `.`. A cell "has a character" iff it is an
uppercase letter. The character always sits on a colored cell (uppercase implies filled).

### Decode one cell

```
function parseCell(ch):
    if ch == '.':
        return { empty: true,  colorIndex: -1, hasChar: false }
    lower = toLowerCase(ch)
    return {
        empty: false,
        colorIndex: charCode(lower) - charCode('a'),   # 0..25
        hasChar: (ch != lower)                          # uppercase => true
    }
```

### Encode one cell

```
function encodeCell(colorIndex, hasChar):
    if colorIndex < 0: return '.'
    lower = char(charCode('a') + colorIndex)
    return hasChar ? toUpperCase(lower) : lower
```

### Read the whole board into a grid

```
grid = 2D array [s][s]
for i in 0 .. s*s-1:
    r = i / s        # integer division
    col = i % s
    grid[r][col] = parseCell(d[i])
```

## Rules / invariants

1. `d.length` MUST equal `s * s`. Reject the file otherwise.
2. `colorIndex` for any cell must be within the available palette
   (`< c.length` if `c` is non-empty, otherwise `< game default palette length`).
3. **At most one character per color.** Across the whole board, each color index may
   carry at most one uppercase letter. (e.g. there can be at most one `A`, one `B`, …)
4. A character only appears on a colored cell (guaranteed by the encoding: uppercase
   letters are always "filled").

## Worked example

```json
{ "l": 3, "s": 3, "g": "h", "c": [], "d": "ab.AbcC.." }
```

`s = 3`, so the board is 3×3 and `d` has 9 chars. Laid out row-major:

```
index:  0 1 2 | 3 4 5 | 6 7 8
char:   a b . | A b c | C . .

row 0:  a            b            .
        color0       color1       empty
row 1:  A            b            c
        color0+char  color1       color2
row 2:  C            .            .
        color2+char  empty        empty
```

Characters present: `A` (color 0) and `C` (color 2) — one per color, valid.

## Minimal parser (JS reference)

```js
function parseLevel(minimal) {
  const { l, s, g, c, d } = minimal;
  if (!Number.isInteger(s) || s < 3 || s > 15) throw new Error('bad s');
  if (!['n','h','u','c'].includes(g)) throw new Error('bad g');
  if (typeof d !== 'string' || d.length !== s * s) throw new Error('bad d length');

  const palette = Array.isArray(c) && c.length ? c : null; // null => use game default

  const cells = [];
  for (let i = 0; i < d.length; i++) {
    const ch = d[i];
    if (ch === '.') { cells.push({ empty: true }); continue; }
    const lower = ch.toLowerCase();
    cells.push({
      empty: false,
      colorIndex: lower.charCodeAt(0) - 97,
      hasChar: ch !== lower,
      row: Math.floor(i / s),
      col: i % s,
    });
  }
  return { l, s, g, palette, cells };
}
```
