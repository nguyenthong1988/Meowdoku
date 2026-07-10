using System.Collections.Generic;

namespace Cast.Game
{
    public enum HintStep : byte
    {
        RemoveWrongMark = 1,
        ExcludeAroundCharacter = 2,
        LastCellInColor = 3,
        LastCellInLine = 4,
        ColorLineLock = 5,
        GroupLock = 6,
        TrialContradiction = 7,
        ChainContradiction = 8,
        DirectReveal = 9,
    }

    public enum HintVictimKind : byte
    {
        None = 0,
        Color = 1,
        Row = 2,
        Column = 3,
    }

    public sealed class HintResult
    {
        public HintStep Step;
        public string Message;
        public sbyte ColorIndex = -1;
        public readonly List<(int Row, int Col)> FocusCells = new List<(int, int)>();
        public readonly List<(int Row, int Col)> ExcludeCells = new List<(int, int)>();
        public readonly List<(int Row, int Col)> VictimCells = new List<(int, int)>();
        public readonly List<(int Row, int Col)> ChainCells = new List<(int, int)>();
        public (int Row, int Col)? PlaceCell;
        public (int Row, int Col)? RemoveMarkCell;
        public (int Row, int Col)? TrialCell;
        public HintVictimKind VictimKind = HintVictimKind.None;
        public int VictimIndex = -1;
    }

    public static class HintColorNames
    {
        private static readonly string[] Names =
        {
            "Brown", "Khaki", "Indigo", "Red", "Green",
            "Olive", "Plum", "Purple", "Jade", "Teal",
        };

        public static string Get(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < Names.Length)
                return Names[colorIndex];
            return $"Color {colorIndex + 1}";
        }
    }

    public static class HintCalculator
    {
        private const int MaxGroupSize = 4;
        private const int MaxChainLength = 6;

        private sealed class Snapshot
        {
            public BoardState Board;
            public LevelData Level;
            public int Size;
            public int ColorCount;
            public HashSet<(int, int)> SolutionCells;
            public List<CharacterPlacement> PlacedCharacters;
            public List<CharacterPlacement> MissingCharacters;
            public bool[] ColorHasCharacter;
            public bool[] RowHasCharacter;
            public bool[] ColHasCharacter;
            public List<(int Row, int Col)>[] ColorEmpties;
            public List<(int Row, int Col)>[] RowEmpties;
            public List<(int Row, int Col)>[] ColEmpties;
            public List<(int Row, int Col)> AllEmpties;

            public bool IsEmpty(int row, int col) =>
                Level.GetCell(row, col).IsFilled && Board.GetMark(row, col) == PlayerMark.None;
        }

        public static HintResult Calculate(BoardState board)
        {
            Snapshot s = TakeSnapshot(board);
            if (s.MissingCharacters.Count == 0) return null;

            return TryRemoveWrongMark(s)
                ?? TryExcludeAroundCharacter(s)
                ?? TryLastCellInColor(s)
                ?? TryLastCellInLine(s)
                ?? TryColorLineLock(s)
                ?? TryGroupLock(s)
                ?? TryTrialContradiction(s)
                ?? TryChainContradiction(s)
                ?? DirectReveal(s);
        }

        private static Snapshot TakeSnapshot(BoardState board)
        {
            LevelData level = board.Level;
            int size = board.Size;
            int colorCount = level.RegionCount;

            var s = new Snapshot
            {
                Board = board,
                Level = level,
                Size = size,
                ColorCount = colorCount,
                SolutionCells = new HashSet<(int, int)>(),
                PlacedCharacters = new List<CharacterPlacement>(),
                MissingCharacters = new List<CharacterPlacement>(),
                ColorHasCharacter = new bool[colorCount],
                RowHasCharacter = new bool[size],
                ColHasCharacter = new bool[size],
                ColorEmpties = new List<(int, int)>[colorCount],
                RowEmpties = new List<(int, int)>[size],
                ColEmpties = new List<(int, int)>[size],
                AllEmpties = new List<(int, int)>(),
            };

            for (int i = 0; i < colorCount; i++) s.ColorEmpties[i] = new List<(int, int)>();
            for (int i = 0; i < size; i++)
            {
                s.RowEmpties[i] = new List<(int, int)>();
                s.ColEmpties[i] = new List<(int, int)>();
            }

            foreach (CharacterPlacement p in level.Solution)
                s.SolutionCells.Add((p.Row, p.Col));

            foreach (CharacterPlacement p in level.Solution)
            {
                if (board.GetMark(p.Row, p.Col) == PlayerMark.Character)
                    s.PlacedCharacters.Add(p);
                else
                    s.MissingCharacters.Add(p);
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board.GetMark(r, c) != PlayerMark.Character) continue;
                    s.RowHasCharacter[r] = true;
                    s.ColHasCharacter[c] = true;
                    sbyte color = level.GetCell(r, c).ColorIndex;
                    if (color >= 0 && color < colorCount) s.ColorHasCharacter[color] = true;
                }
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (!s.IsEmpty(r, c)) continue;
                    s.AllEmpties.Add((r, c));
                    s.RowEmpties[r].Add((r, c));
                    s.ColEmpties[c].Add((r, c));
                    sbyte color = level.GetCell(r, c).ColorIndex;
                    if (color >= 0 && color < colorCount) s.ColorEmpties[color].Add((r, c));
                }
            }

            return s;
        }

        private static HintResult TryRemoveWrongMark(Snapshot s)
        {
            for (int r = 0; r < s.Size; r++)
            {
                for (int c = 0; c < s.Size; c++)
                {
                    if (s.Board.GetMark(r, c) != PlayerMark.Hint) continue;
                    if (!s.SolutionCells.Contains((r, c))) continue;

                    var result = new HintResult
                    {
                        Step = HintStep.RemoveWrongMark,
                        Message = "You've marked the wrong lotus leaf! Tap to remove the X.",
                        ColorIndex = s.Level.GetCell(r, c).ColorIndex,
                        RemoveMarkCell = (r, c),
                    };
                    result.FocusCells.Add((r, c));
                    return result;
                }
            }
            return null;
        }

        private static HintResult TryExcludeAroundCharacter(Snapshot s)
        {
            foreach (CharacterPlacement cat in s.PlacedCharacters)
            {
                List<(int, int)> zone = CollectEmptyZone(s, cat.Row, cat.Col);
                if (zone.Count == 0) continue;

                var result = new HintResult
                {
                    Step = HintStep.ExcludeAroundCharacter,
                    Message = "This frog's row, column and neighbors can't have other frogs — exclude them.",
                    ColorIndex = cat.ColorIndex,
                };
                result.FocusCells.Add((cat.Row, cat.Col));
                result.ExcludeCells.AddRange(zone);
                return result;
            }

            foreach (CharacterPlacement cat in s.PlacedCharacters)
            {
                if (cat.ColorIndex < 0 || cat.ColorIndex >= s.ColorCount) continue;
                List<(int Row, int Col)> sameColor = s.ColorEmpties[cat.ColorIndex];
                if (sameColor.Count == 0) continue;

                var result = new HintResult
                {
                    Step = HintStep.ExcludeAroundCharacter,
                    Message = "Only one frog per color.",
                    ColorIndex = cat.ColorIndex,
                };
                result.FocusCells.Add((cat.Row, cat.Col));
                result.ExcludeCells.AddRange(sameColor);
                return result;
            }

            return null;
        }

        private static List<(int, int)> CollectEmptyZone(Snapshot s, int row, int col)
        {
            var zone = new List<(int, int)>();
            var seen = new HashSet<(int, int)>();

            for (int c = 0; c < s.Size; c++)
                AddIfEmpty(s, zone, seen, row, c);
            for (int r = 0; r < s.Size; r++)
                AddIfEmpty(s, zone, seen, r, col);
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                    AddIfEmpty(s, zone, seen, row + dr, col + dc);

            return zone;
        }

        private static void AddIfEmpty(Snapshot s, List<(int, int)> zone, HashSet<(int, int)> seen, int row, int col)
        {
            if (row < 0 || row >= s.Size || col < 0 || col >= s.Size) return;
            if (!s.IsEmpty(row, col)) return;
            if (!seen.Add((row, col))) return;
            zone.Add((row, col));
        }

        private static HintResult TryLastCellInColor(Snapshot s)
        {
            bool found = false;
            sbyte bestColor = -1;
            (int Row, int Col) bestCell = default;

            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (s.ColorHasCharacter[color]) continue;
                if (s.ColorEmpties[color].Count != 1) continue;

                (int Row, int Col) cell = s.ColorEmpties[color][0];
                if (!s.SolutionCells.Contains(cell)) continue;

                if (!found || IsEarlier(cell, bestCell))
                {
                    found = true;
                    bestColor = color;
                    bestCell = cell;
                }
            }

            if (!found) return null;

            var result = new HintResult
            {
                Step = HintStep.LastCellInColor,
                Message = $"{HintColorNames.Get(bestColor)} — only one cell left for a frog.",
                ColorIndex = bestColor,
                PlaceCell = bestCell,
            };
            result.FocusCells.Add(bestCell);
            return result;
        }

        private static bool IsEarlier((int Row, int Col) a, (int Row, int Col) b) =>
            a.Row < b.Row || (a.Row == b.Row && a.Col < b.Col);

        private static HintResult TryLastCellInLine(Snapshot s)
        {
            for (int r = 0; r < s.Size; r++)
            {
                if (s.RowHasCharacter[r]) continue;
                if (s.RowEmpties[r].Count != 1) continue;
                (int Row, int Col) cell = s.RowEmpties[r][0];
                if (!s.SolutionCells.Contains(cell)) continue;
                return LastCellInLineResult(s, cell, $"Only one lotus leaf is left in Row {r + 1}.");
            }

            for (int c = 0; c < s.Size; c++)
            {
                if (s.ColHasCharacter[c]) continue;
                if (s.ColEmpties[c].Count != 1) continue;
                (int Row, int Col) cell = s.ColEmpties[c][0];
                if (!s.SolutionCells.Contains(cell)) continue;
                return LastCellInLineResult(s, cell, $"Only one lotus leaf is left in Column {c + 1}.");
            }

            return null;
        }

        private static HintResult LastCellInLineResult(Snapshot s, (int Row, int Col) cell, string message)
        {
            var result = new HintResult
            {
                Step = HintStep.LastCellInLine,
                Message = message,
                ColorIndex = s.Level.GetCell(cell.Row, cell.Col).ColorIndex,
                PlaceCell = cell,
            };
            result.FocusCells.Add(cell);
            return result;
        }

        private static HintResult TryColorLineLock(Snapshot s)
        {
            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (s.ColorHasCharacter[color]) continue;
                List<(int Row, int Col)> empties = s.ColorEmpties[color];
                if (empties.Count < 2) continue;

                if (AllSameRow(empties, out int row))
                {
                    List<(int, int)> exclude = OtherColorEmptiesInRow(s, row, color);
                    if (exclude.Count > 0)
                        return ColorLineLockResult(s, color, empties, exclude,
                            $"{HintColorNames.Get(color)} candidates all in Row {row + 1} — exclude other colors.");
                }
                else if (AllSameCol(empties, out int col))
                {
                    List<(int, int)> exclude = OtherColorEmptiesInCol(s, col, color);
                    if (exclude.Count > 0)
                        return ColorLineLockResult(s, color, empties, exclude,
                            $"{HintColorNames.Get(color)} candidates all in Column {col + 1} — exclude other colors.");
                }
            }

            for (int r = 0; r < s.Size; r++)
            {
                if (s.RowHasCharacter[r]) continue;
                List<(int Row, int Col)> empties = s.RowEmpties[r];
                if (empties.Count < 2) continue;
                if (!AllSameColor(s, empties, out sbyte color)) continue;

                var exclude = new List<(int, int)>();
                foreach ((int Row, int Col) cell in s.ColorEmpties[color])
                    if (cell.Row != r) exclude.Add(cell);
                if (exclude.Count == 0) continue;

                return ColorLineLockResult(s, color, empties, exclude,
                    $"Row {r + 1} candidates all belong to {HintColorNames.Get(color)} — exclude other rows.");
            }

            for (int c = 0; c < s.Size; c++)
            {
                if (s.ColHasCharacter[c]) continue;
                List<(int Row, int Col)> empties = s.ColEmpties[c];
                if (empties.Count < 2) continue;
                if (!AllSameColor(s, empties, out sbyte color)) continue;

                var exclude = new List<(int, int)>();
                foreach ((int Row, int Col) cell in s.ColorEmpties[color])
                    if (cell.Col != c) exclude.Add(cell);
                if (exclude.Count == 0) continue;

                return ColorLineLockResult(s, color, empties, exclude,
                    $"Column {c + 1} candidates all belong to {HintColorNames.Get(color)} — exclude other columns.");
            }

            return null;
        }

        private static HintResult ColorLineLockResult(Snapshot s, sbyte color,
            List<(int Row, int Col)> focus, List<(int, int)> exclude, string message)
        {
            var result = new HintResult
            {
                Step = HintStep.ColorLineLock,
                Message = message,
                ColorIndex = color,
            };
            result.FocusCells.AddRange(focus);
            result.ExcludeCells.AddRange(exclude);
            return result;
        }

        private static bool AllSameRow(List<(int Row, int Col)> cells, out int row)
        {
            row = cells[0].Row;
            foreach ((int Row, int Col) cell in cells)
                if (cell.Row != row) return false;
            return true;
        }

        private static bool AllSameCol(List<(int Row, int Col)> cells, out int col)
        {
            col = cells[0].Col;
            foreach ((int Row, int Col) cell in cells)
                if (cell.Col != col) return false;
            return true;
        }

        private static bool AllSameColor(Snapshot s, List<(int Row, int Col)> cells, out sbyte color)
        {
            color = s.Level.GetCell(cells[0].Row, cells[0].Col).ColorIndex;
            foreach ((int Row, int Col) cell in cells)
                if (s.Level.GetCell(cell.Row, cell.Col).ColorIndex != color) return false;
            return color >= 0;
        }

        private static List<(int, int)> OtherColorEmptiesInRow(Snapshot s, int row, sbyte color)
        {
            var list = new List<(int, int)>();
            foreach ((int Row, int Col) cell in s.RowEmpties[row])
                if (s.Level.GetCell(cell.Row, cell.Col).ColorIndex != color) list.Add(cell);
            return list;
        }

        private static List<(int, int)> OtherColorEmptiesInCol(Snapshot s, int col, sbyte color)
        {
            var list = new List<(int, int)>();
            foreach ((int Row, int Col) cell in s.ColEmpties[col])
                if (s.Level.GetCell(cell.Row, cell.Col).ColorIndex != color) list.Add(cell);
            return list;
        }

        private static HintResult TryGroupLock(Snapshot s)
        {
            HintResult byRows = TryGroupLockAxis(s, byRow: true);
            if (byRows != null) return byRows;
            return TryGroupLockAxis(s, byRow: false);
        }

        private static HintResult TryGroupLockAxis(Snapshot s, bool byRow)
        {
            var colors = new List<sbyte>();
            var colorLines = new Dictionary<sbyte, HashSet<int>>();

            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (s.ColorHasCharacter[color]) continue;
                if (s.ColorEmpties[color].Count == 0) continue;

                var lines = new HashSet<int>();
                foreach ((int Row, int Col) cell in s.ColorEmpties[color])
                    lines.Add(byRow ? cell.Row : cell.Col);

                if (lines.Count > MaxGroupSize) continue;
                colors.Add(color);
                colorLines[color] = lines;
            }

            for (int groupSize = 2; groupSize <= MaxGroupSize; groupSize++)
            {
                HintResult result = SearchGroups(s, colors, colorLines, groupSize, byRow,
                    new List<sbyte>(), new HashSet<int>(), 0);
                if (result != null) return result;
            }

            return null;
        }

        private static HintResult SearchGroups(Snapshot s, List<sbyte> colors,
            Dictionary<sbyte, HashSet<int>> colorLines, int groupSize, bool byRow,
            List<sbyte> chosen, HashSet<int> unionLines, int startIndex)
        {
            if (chosen.Count == groupSize)
            {
                if (unionLines.Count != groupSize) return null;
                return BuildGroupLockResult(s, chosen, unionLines, byRow);
            }

            for (int i = startIndex; i < colors.Count; i++)
            {
                sbyte color = colors[i];
                var merged = new HashSet<int>(unionLines);
                merged.UnionWith(colorLines[color]);
                if (merged.Count > groupSize) continue;

                chosen.Add(color);
                HintResult result = SearchGroups(s, colors, colorLines, groupSize, byRow, chosen, merged, i + 1);
                if (result != null) return result;
                chosen.RemoveAt(chosen.Count - 1);
            }

            return null;
        }

        private static HintResult BuildGroupLockResult(Snapshot s, List<sbyte> chosen,
            HashSet<int> unionLines, bool byRow)
        {
            var chosenSet = new HashSet<sbyte>(chosen);
            var focus = new List<(int, int)>();
            var exclude = new List<(int, int)>();

            foreach (int line in unionLines)
            {
                List<(int Row, int Col)> lineEmpties = byRow ? s.RowEmpties[line] : s.ColEmpties[line];
                foreach ((int Row, int Col) cell in lineEmpties)
                {
                    sbyte cellColor = s.Level.GetCell(cell.Row, cell.Col).ColorIndex;
                    if (chosenSet.Contains(cellColor)) focus.Add(cell);
                    else exclude.Add(cell);
                }
            }

            if (exclude.Count == 0) return null;

            string axis = byRow ? "rows" : "columns";
            var result = new HintResult
            {
                Step = HintStep.GroupLock,
                Message = $"{chosen.Count} colors share {chosen.Count} {axis} — exclude other lotus leaves in these {chosen.Count} {axis}.",
            };
            result.FocusCells.AddRange(focus);
            result.ExcludeCells.AddRange(exclude);
            return result;
        }

        private static HintResult TryTrialContradiction(Snapshot s)
        {
            foreach ((int Row, int Col) trial in s.AllEmpties)
            {
                if (s.SolutionCells.Contains(trial)) continue;

                HashSet<(int, int)> locked = CollectLockedByPlacement(s, trial);
                HintResult result = FindVictim(s, trial, locked, HintStep.TrialContradiction);
                if (result != null) return result;
            }
            return null;
        }

        private static HashSet<(int, int)> CollectLockedByPlacement(Snapshot s, (int Row, int Col) trial)
        {
            var locked = new HashSet<(int, int)> { trial };

            foreach ((int Row, int Col) cell in s.RowEmpties[trial.Row]) locked.Add(cell);
            foreach ((int Row, int Col) cell in s.ColEmpties[trial.Col]) locked.Add(cell);
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = trial.Row + dr;
                    int nc = trial.Col + dc;
                    if (nr < 0 || nr >= s.Size || nc < 0 || nc >= s.Size) continue;
                    if (s.IsEmpty(nr, nc)) locked.Add((nr, nc));
                }
            }

            sbyte trialColor = s.Level.GetCell(trial.Row, trial.Col).ColorIndex;
            if (trialColor >= 0 && trialColor < s.ColorCount)
                foreach ((int Row, int Col) cell in s.ColorEmpties[trialColor]) locked.Add(cell);

            return locked;
        }

        private static HintResult FindVictim(Snapshot s, (int Row, int Col) trial,
            HashSet<(int, int)> locked, HintStep step)
        {
            sbyte trialColor = s.Level.GetCell(trial.Row, trial.Col).ColorIndex;
            string verb = step == HintStep.TrialContradiction ? "would leave" : "would eventually leave";

            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (color == trialColor) continue;
                if (s.ColorHasCharacter[color]) continue;
                List<(int Row, int Col)> empties = s.ColorEmpties[color];
                if (empties.Count == 0) continue;
                if (!AllLocked(empties, locked)) continue;

                return TrialResult(s, trial, step, HintVictimKind.Color, color, empties,
                    $"A frog here {verb} {HintColorNames.Get(color)} with no lotus leaf — exclude this cell.");
            }

            for (int r = 0; r < s.Size; r++)
            {
                if (r == trial.Row) continue;
                if (s.RowHasCharacter[r]) continue;
                List<(int Row, int Col)> empties = s.RowEmpties[r];
                if (empties.Count == 0) continue;
                if (!AllLocked(empties, locked)) continue;

                return TrialResult(s, trial, step, HintVictimKind.Row, r, empties,
                    $"A frog here {verb} Row {r + 1} with no lotus leaf — exclude this cell.");
            }

            for (int c = 0; c < s.Size; c++)
            {
                if (c == trial.Col) continue;
                if (s.ColHasCharacter[c]) continue;
                List<(int Row, int Col)> empties = s.ColEmpties[c];
                if (empties.Count == 0) continue;
                if (!AllLocked(empties, locked)) continue;

                return TrialResult(s, trial, step, HintVictimKind.Column, c, empties,
                    $"A frog here {verb} Column {c + 1} with no lotus leaf — exclude this cell.");
            }

            return null;
        }

        private static bool AllLocked(List<(int Row, int Col)> cells, HashSet<(int, int)> locked)
        {
            foreach ((int Row, int Col) cell in cells)
                if (!locked.Contains(cell)) return false;
            return true;
        }

        private static HintResult TrialResult(Snapshot s, (int Row, int Col) trial, HintStep step,
            HintVictimKind kind, int victimIndex, List<(int Row, int Col)> victimCells, string message)
        {
            var result = new HintResult
            {
                Step = step,
                Message = message,
                ColorIndex = kind == HintVictimKind.Color ? (sbyte)victimIndex : s.Level.GetCell(trial.Row, trial.Col).ColorIndex,
                TrialCell = trial,
                VictimKind = kind,
                VictimIndex = victimIndex,
            };
            result.ExcludeCells.Add(trial);
            result.VictimCells.AddRange(victimCells);
            return result;
        }

        private sealed class Simulation
        {
            public readonly Snapshot Source;
            public readonly HashSet<(int, int)> Empties;
            public readonly bool[] ColorHasCharacter;
            public readonly bool[] RowHasCharacter;
            public readonly bool[] ColHasCharacter;

            public Simulation(Snapshot s)
            {
                Source = s;
                Empties = new HashSet<(int, int)>(s.AllEmpties);
                ColorHasCharacter = (bool[])s.ColorHasCharacter.Clone();
                RowHasCharacter = (bool[])s.RowHasCharacter.Clone();
                ColHasCharacter = (bool[])s.ColHasCharacter.Clone();
            }

            public void Place(int row, int col)
            {
                Snapshot s = Source;
                RowHasCharacter[row] = true;
                ColHasCharacter[col] = true;
                sbyte color = s.Level.GetCell(row, col).ColorIndex;
                if (color >= 0 && color < s.ColorCount) ColorHasCharacter[color] = true;

                Empties.Remove((row, col));
                foreach ((int Row, int Col) cell in s.RowEmpties[row]) Empties.Remove(cell);
                foreach ((int Row, int Col) cell in s.ColEmpties[col]) Empties.Remove(cell);
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                        Empties.Remove((row + dr, col + dc));
                if (color >= 0 && color < s.ColorCount)
                    foreach ((int Row, int Col) cell in s.ColorEmpties[color]) Empties.Remove(cell);
            }

            public int CountColorEmpties(sbyte color, out (int Row, int Col) last)
            {
                last = default;
                int count = 0;
                foreach ((int Row, int Col) cell in Source.ColorEmpties[color])
                {
                    if (!Empties.Contains(cell)) continue;
                    count++;
                    last = cell;
                }
                return count;
            }

            public int CountRowEmpties(int row, out (int Row, int Col) last)
            {
                last = default;
                int count = 0;
                foreach ((int Row, int Col) cell in Source.RowEmpties[row])
                {
                    if (!Empties.Contains(cell)) continue;
                    count++;
                    last = cell;
                }
                return count;
            }

            public int CountColEmpties(int col, out (int Row, int Col) last)
            {
                last = default;
                int count = 0;
                foreach ((int Row, int Col) cell in Source.ColEmpties[col])
                {
                    if (!Empties.Contains(cell)) continue;
                    count++;
                    last = cell;
                }
                return count;
            }
        }

        private static HintResult TryChainContradiction(Snapshot s)
        {
            foreach ((int Row, int Col) trial in s.AllEmpties)
            {
                if (s.SolutionCells.Contains(trial)) continue;

                HintResult result = SimulateChain(s, trial);
                if (result != null) return result;
            }
            return null;
        }

        private static HintResult SimulateChain(Snapshot s, (int Row, int Col) trial)
        {
            var sim = new Simulation(s);
            sim.Place(trial.Row, trial.Col);
            var chain = new List<(int Row, int Col)>();

            for (int depth = 0; depth < MaxChainLength; depth++)
            {
                if (TryFindSimContradiction(sim, out HintVictimKind kind, out int index))
                {
                    if (chain.Count == 0 && depth == 0) return null;
                    return ChainResult(s, trial, chain, kind, index);
                }

                (int Row, int Col) forced = (0, 0);
                if (!TryFindForcedPlacement(sim, out forced)) return null;
                sim.Place(forced.Row, forced.Col);
                chain.Add(forced);
            }

            if (!TryFindSimContradiction(sim, out HintVictimKind finalKind, out int finalIndex)) return null;
            return ChainResult(s, trial, chain, finalKind, finalIndex);
        }

        private static bool TryFindSimContradiction(Simulation sim, out HintVictimKind kind, out int index)
        {
            Snapshot s = sim.Source;

            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (sim.ColorHasCharacter[color]) continue;
                (int Row, int Col) ignoredColor;
                if (sim.CountColorEmpties(color, out ignoredColor) == 0)
                {
                    kind = HintVictimKind.Color;
                    index = color;
                    return true;
                }
            }
            for (int r = 0; r < s.Size; r++)
            {
                if (sim.RowHasCharacter[r]) continue;
                (int Row, int Col) ignoredRow;
                if (sim.CountRowEmpties(r, out ignoredRow) == 0)
                {
                    kind = HintVictimKind.Row;
                    index = r;
                    return true;
                }
            }
            for (int c = 0; c < s.Size; c++)
            {
                if (sim.ColHasCharacter[c]) continue;
                (int Row, int Col) ignoredCol;
                if (sim.CountColEmpties(c, out ignoredCol) == 0)
                {
                    kind = HintVictimKind.Column;
                    index = c;
                    return true;
                }
            }

            kind = HintVictimKind.None;
            index = -1;
            return false;
        }

        private static bool TryFindForcedPlacement(Simulation sim, out (int Row, int Col) forced)
        {
            Snapshot s = sim.Source;

            for (sbyte color = 0; color < s.ColorCount; color++)
            {
                if (sim.ColorHasCharacter[color]) continue;
                (int Row, int Col) last;
                if (sim.CountColorEmpties(color, out last) == 1)
                {
                    forced = last;
                    return true;
                }
            }
            for (int r = 0; r < s.Size; r++)
            {
                if (sim.RowHasCharacter[r]) continue;
                (int Row, int Col) last;
                if (sim.CountRowEmpties(r, out last) == 1)
                {
                    forced = last;
                    return true;
                }
            }
            for (int c = 0; c < s.Size; c++)
            {
                if (sim.ColHasCharacter[c]) continue;
                (int Row, int Col) last;
                if (sim.CountColEmpties(c, out last) == 1)
                {
                    forced = last;
                    return true;
                }
            }

            forced = default;
            return false;
        }

        private static HintResult ChainResult(Snapshot s, (int Row, int Col) trial,
            List<(int Row, int Col)> chain, HintVictimKind victimKind, int victimIndex)
        {
            string victimText;
            var victimCells = new List<(int Row, int Col)>();
            switch (victimKind)
            {
                case HintVictimKind.Color:
                    victimText = HintColorNames.Get(victimIndex);
                    victimCells.AddRange(s.ColorEmpties[victimIndex]);
                    break;
                case HintVictimKind.Row:
                    victimText = $"Row {victimIndex + 1}";
                    victimCells.AddRange(s.RowEmpties[victimIndex]);
                    break;
                default:
                    victimText = $"Column {victimIndex + 1}";
                    victimCells.AddRange(s.ColEmpties[victimIndex]);
                    break;
            }

            var result = new HintResult
            {
                Step = HintStep.ChainContradiction,
                Message = $"A frog here would eventually leave {victimText} with no lotus leaf — exclude this cell.",
                ColorIndex = victimKind == HintVictimKind.Color ? (sbyte)victimIndex : s.Level.GetCell(trial.Row, trial.Col).ColorIndex,
                TrialCell = trial,
                VictimKind = victimKind,
                VictimIndex = victimIndex,
            };
            result.ExcludeCells.Add(trial);
            result.VictimCells.AddRange(victimCells);

            int start = chain.Count > 3 ? chain.Count - 3 : 0;
            for (int i = start; i < chain.Count; i++)
                result.ChainCells.Add(chain[i]);

            return result;
        }

        private static HintResult DirectReveal(Snapshot s)
        {
            CharacterPlacement best = default;
            bool found = false;
            int bestCount = int.MaxValue;

            foreach (CharacterPlacement cat in s.MissingCharacters)
            {
                int count = cat.ColorIndex >= 0 && cat.ColorIndex < s.ColorCount
                    ? s.ColorEmpties[cat.ColorIndex].Count
                    : int.MaxValue;
                if (!found || count < bestCount)
                {
                    found = true;
                    best = cat;
                    bestCount = count;
                }
            }

            if (!found) return null;

            var result = new HintResult
            {
                Step = HintStep.DirectReveal,
                Message = "The frog is right here.",
                ColorIndex = best.ColorIndex,
                PlaceCell = (best.Row, best.Col),
            };
            result.FocusCells.Add((best.Row, best.Col));
            return result;
        }
    }
}
