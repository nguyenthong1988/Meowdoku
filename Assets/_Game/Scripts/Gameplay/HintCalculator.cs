using System.Collections.Generic;
using System.Linq;

namespace Cast.Game
{
    public enum HintReason
    {
        SingleCell,
        RowSingleColor,
        RowMultiColor,
        ColumnSingleColor,
        ColumnMultiColor,
        Cluster,
        Exact
    }

    public sealed class HintResult
    {
        public IReadOnlyList<(int Row, int Col)> Cells { get; }
        public sbyte ColorIndex { get; }
        public HintReason Reason { get; }
        public string Message { get; }

        public HintResult(IReadOnlyList<(int Row, int Col)> cells, sbyte colorIndex, HintReason reason, string message)
        {
            Cells = cells;
            ColorIndex = colorIndex;
            Reason = reason;
            Message = message;
        }
    }

    public static class HintMessages
    {
        public const string SingleCell = "Only one possible cell remains for this color.";
        public const string RowSingleColor = "The cat in this row must be in the highlighted color.";
        public const string RowMultiColor = "The cat in this row hides in one of these colors.";
        public const string ColumnSingleColor = "The cat in this column must be in the highlighted color.";
        public const string ColumnMultiColor = "The cat in this column hides in one of these colors.";
        public const string Cluster = "Focus on this cluster - the cat is likely nearby.";
        public const string Exact = "The cat is right here.";

        public static string For(HintReason reason)
        {
            switch (reason)
            {
                case HintReason.SingleCell: return SingleCell;
                case HintReason.RowSingleColor: return RowSingleColor;
                case HintReason.RowMultiColor: return RowMultiColor;
                case HintReason.ColumnSingleColor: return ColumnSingleColor;
                case HintReason.ColumnMultiColor: return ColumnMultiColor;
                case HintReason.Cluster: return Cluster;
                case HintReason.Exact: return Exact;
                default: return string.Empty;
            }
        }
    }

    public static class HintCalculator
    {
        public static HintResult Calculate(BoardState board, IReadOnlyCollection<sbyte> hintedColors)
        {
            LevelData level = board.Level;
            IReadOnlyList<CatPlacement> solution = level.Solution;

            var revealedCats = new List<CatPlacement>();
            var unrevealedCats = new List<CatPlacement>();
            for (int i = 0; i < solution.Count; i++)
            {
                CatPlacement p = solution[i];
                if (board.GetMark(p.Row, p.Col) == PlayerMark.Character)
                    revealedCats.Add(p);
                else
                    unrevealedCats.Add(p);
            }

            if (unrevealedCats.Count == 0) return null;

            var unrevealedCatCells = new HashSet<(int, int)>();
            foreach (CatPlacement cat in unrevealedCats)
                unrevealedCatCells.Add((cat.Row, cat.Col));

            var excludedRows = new HashSet<int>();
            var excludedCols = new HashSet<int>();
            var excludedColors = new HashSet<sbyte>();
            var excludedNeighborhood = new HashSet<(int, int)>();

            foreach (CatPlacement cat in revealedCats)
            {
                excludedRows.Add(cat.Row);
                excludedCols.Add(cat.Col);
                excludedColors.Add(cat.ColorIndex);
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int nr = cat.Row + dr;
                        int nc = cat.Col + dc;
                        if (board.InBounds(nr, nc))
                            excludedNeighborhood.Add((nr, nc));
                    }
                }
            }

            var candidates = new List<(int Row, int Col, sbyte Color)>();
            var candidateSet = new HashSet<(int, int)>();
            int size = board.Size;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    CellData cell = level.GetCell(r, c);
                    if (!cell.IsFilled) continue;
                    if (excludedColors.Contains(cell.ColorIndex)) continue;
                    if (board.GetMark(r, c) == PlayerMark.Character) continue;
                    if (excludedRows.Contains(r)) continue;
                    if (excludedCols.Contains(c)) continue;
                    if (excludedNeighborhood.Contains((r, c))) continue;

                    candidates.Add((r, c, cell.ColorIndex));
                    candidateSet.Add((r, c));
                }
            }

            if (candidates.Count == 0) return null;

            HintResult single = TrySingleCell(candidates, unrevealedCatCells);
            if (single != null) return single;

            HintResult lineResult = TryLines(candidates, unrevealedCats, unrevealedCatCells, hintedColors);
            if (lineResult != null) return lineResult;

            HintResult cluster = TryCluster(candidates, candidateSet, unrevealedCatCells);
            if (cluster != null) return cluster;

            return Exact(candidates, unrevealedCats, candidateSet);
        }

        private static bool ContainsCat(IEnumerable<(int Row, int Col)> cells, HashSet<(int, int)> unrevealedCatCells)
        {
            foreach (var cell in cells)
                if (unrevealedCatCells.Contains(cell))
                    return true;
            return false;
        }

        private static HintResult TrySingleCell(
            List<(int Row, int Col, sbyte Color)> candidates,
            HashSet<(int, int)> unrevealedCatCells)
        {
            var byColor = new Dictionary<sbyte, List<(int Row, int Col)>>();
            foreach (var cand in candidates)
            {
                if (!byColor.TryGetValue(cand.Color, out var list))
                {
                    list = new List<(int, int)>();
                    byColor[cand.Color] = list;
                }
                list.Add((cand.Row, cand.Col));
            }

            sbyte bestColor = -1;
            (int Row, int Col) bestCell = default;
            bool found = false;
            foreach (var kvp in byColor)
            {
                if (kvp.Value.Count != 1) continue;
                if (!unrevealedCatCells.Contains(kvp.Value[0])) continue;
                if (!found || kvp.Key < bestColor)
                {
                    found = true;
                    bestColor = kvp.Key;
                    bestCell = kvp.Value[0];
                }
            }

            if (!found) return null;
            return new HintResult(new[] { bestCell }, bestColor, HintReason.SingleCell, HintMessages.SingleCell);
        }

        private static HintResult TryLines(
            List<(int Row, int Col, sbyte Color)> candidates,
            List<CatPlacement> unrevealedCats,
            HashSet<(int, int)> unrevealedCatCells,
            IReadOnlyCollection<sbyte> hintedColors)
        {
            var rowResult = BestLine(candidates, unrevealedCats, unrevealedCatCells, hintedColors, byRow: true);
            if (rowResult.HasValue)
            {
                if (rowResult.Value.Cells.Count != 1)
                    return ToLineHint(rowResult.Value, byRow: true);
            }

            var colResult = BestLine(candidates, unrevealedCats, unrevealedCatCells, hintedColors, byRow: false);
            if (colResult.HasValue)
            {
                if (colResult.Value.Cells.Count != 1)
                    return ToLineHint(colResult.Value, byRow: false);
            }

            return null;
        }

        private struct LineSelection
        {
            public List<(int Row, int Col)> Cells;
            public sbyte Color;
            public bool SingleColor;
        }

        private struct LineRank
        {
            public int Key;
            public bool SingleColor;
            public int Score;
            public List<(int Row, int Col, sbyte Color)> Cells;
        }

        private static LineSelection? BestLine(
            List<(int Row, int Col, sbyte Color)> candidates,
            List<CatPlacement> unrevealedCats,
            HashSet<(int, int)> unrevealedCatCells,
            IReadOnlyCollection<sbyte> hintedColors,
            bool byRow)
        {
            var relevantLines = new HashSet<int>();
            foreach (CatPlacement cat in unrevealedCats)
                relevantLines.Add(byRow ? cat.Row : cat.Col);

            var byLine = new Dictionary<int, List<(int Row, int Col, sbyte Color)>>();
            foreach (var cand in candidates)
            {
                int key = byRow ? cand.Row : cand.Col;
                if (!relevantLines.Contains(key)) continue;
                if (!byLine.TryGetValue(key, out var list))
                {
                    list = new List<(int, int, sbyte)>();
                    byLine[key] = list;
                }
                list.Add(cand);
            }

            var ranked = new List<LineRank>();
            foreach (var kvp in byLine)
            {
                var lineCands = kvp.Value;
                if (lineCands.Count == 0) continue;

                var colorCounts = new Dictionary<sbyte, int>();
                foreach (var cand in lineCands)
                {
                    colorCounts.TryGetValue(cand.Color, out int count);
                    colorCounts[cand.Color] = count + 1;
                }

                bool singleColor = colorCounts.Count == 1;
                int score = 0;
                foreach (var pair in colorCounts)
                    if (pair.Value > score) score = pair.Value;

                ranked.Add(new LineRank { Key = kvp.Key, SingleColor = singleColor, Score = score, Cells = lineCands });
            }

            if (ranked.Count == 0) return null;

            ranked.Sort(CompareLineRank);

            foreach (LineRank line in ranked)
            {
                LineSelection? selection = BuildLineSelection(line, unrevealedCatCells, hintedColors);
                if (selection.HasValue) return selection;
            }

            return null;
        }

        private static int CompareLineRank(LineRank a, LineRank b)
        {
            if (a.SingleColor != b.SingleColor) return a.SingleColor ? -1 : 1;
            if (a.Score != b.Score) return b.Score - a.Score;
            return a.Key - b.Key;
        }

        private static LineSelection? BuildLineSelection(
            LineRank line,
            HashSet<(int, int)> unrevealedCatCells,
            IReadOnlyCollection<sbyte> hintedColors)
        {
            if (line.SingleColor)
            {
                var cells = new List<(int, int)>();
                foreach (var cand in line.Cells)
                    cells.Add((cand.Row, cand.Col));
                if (!ContainsCat(cells, unrevealedCatCells)) return null;
                return new LineSelection { SingleColor = true, Cells = cells, Color = line.Cells[0].Color };
            }

            var filtered = new List<(int Row, int Col, sbyte Color)>();
            foreach (var cand in line.Cells)
            {
                if (hintedColors != null && hintedColors.Contains(cand.Color)) continue;
                filtered.Add(cand);
            }
            if (filtered.Count == 0) filtered = line.Cells;

            if (!ContainsCatCandidates(filtered, unrevealedCatCells))
                filtered = line.Cells;

            if (!ContainsCatCandidates(filtered, unrevealedCatCells))
                return null;

            var counts = new Dictionary<sbyte, int>();
            foreach (var cand in filtered)
            {
                counts.TryGetValue(cand.Color, out int count);
                counts[cand.Color] = count + 1;
            }
            sbyte dominant = filtered[0].Color;
            int dominantCount = -1;
            foreach (var kvp in counts)
            {
                if (kvp.Value > dominantCount || (kvp.Value == dominantCount && kvp.Key < dominant))
                {
                    dominantCount = kvp.Value;
                    dominant = kvp.Key;
                }
            }

            var resultCells = new List<(int, int)>();
            foreach (var cand in filtered)
                resultCells.Add((cand.Row, cand.Col));
            return new LineSelection { SingleColor = false, Cells = resultCells, Color = dominant };
        }

        private static bool ContainsCatCandidates(
            List<(int Row, int Col, sbyte Color)> cands,
            HashSet<(int, int)> unrevealedCatCells)
        {
            foreach (var cand in cands)
                if (unrevealedCatCells.Contains((cand.Row, cand.Col)))
                    return true;
            return false;
        }

        private static HintResult ToLineHint(LineSelection selection, bool byRow)
        {
            HintReason reason;
            if (byRow)
                reason = selection.SingleColor ? HintReason.RowSingleColor : HintReason.RowMultiColor;
            else
                reason = selection.SingleColor ? HintReason.ColumnSingleColor : HintReason.ColumnMultiColor;

            return new HintResult(selection.Cells, selection.Color, reason, HintMessages.For(reason));
        }

        private static HintResult TryCluster(
            List<(int Row, int Col, sbyte Color)> candidates,
            HashSet<(int, int)> candidateSet,
            HashSet<(int, int)> unrevealedCatCells)
        {
            var colorAt = new Dictionary<(int, int), sbyte>();
            foreach (var cand in candidates)
                colorAt[(cand.Row, cand.Col)] = cand.Color;

            int bestCount = -1;
            (int Row, int Col, sbyte Color) bestCand = default;
            List<(int, int)> bestCells = null;

            foreach (var cand in candidates)
            {
                var clusterCells = new List<(int, int)> { (cand.Row, cand.Col) };
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        var pos = (cand.Row + dr, cand.Col + dc);
                        if (!candidateSet.Contains(pos)) continue;
                        if (colorAt[pos] != cand.Color) continue;
                        clusterCells.Add(pos);
                    }
                }

                int count = clusterCells.Count - 1;
                if (count < 1) continue;
                if (!ContainsCat(clusterCells, unrevealedCatCells)) continue;

                bool better;
                if (count > bestCount)
                {
                    better = true;
                }
                else if (count == bestCount)
                {
                    better = cand.Row < bestCand.Row ||
                             (cand.Row == bestCand.Row && cand.Col < bestCand.Col);
                }
                else
                {
                    better = false;
                }

                if (better)
                {
                    bestCount = count;
                    bestCand = cand;
                    bestCells = clusterCells;
                }
            }

            if (bestCount < 1) return null;

            return new HintResult(bestCells, bestCand.Color, HintReason.Cluster, HintMessages.Cluster);
        }

        private static HintResult Exact(
            List<(int Row, int Col, sbyte Color)> candidates,
            List<CatPlacement> unrevealedCats,
            HashSet<(int, int)> candidateSet)
        {
            var colorCandidateCount = new Dictionary<sbyte, int>();
            foreach (var cand in candidates)
            {
                colorCandidateCount.TryGetValue(cand.Color, out int count);
                colorCandidateCount[cand.Color] = count + 1;
            }

            var inCandidates = new List<CatPlacement>();
            foreach (CatPlacement cat in unrevealedCats)
                if (candidateSet.Contains((cat.Row, cat.Col)))
                    inCandidates.Add(cat);

            List<CatPlacement> pool = inCandidates.Count > 0 ? inCandidates : unrevealedCats;

            bool found = false;
            CatPlacement best = default;
            int bestCount = int.MaxValue;
            foreach (CatPlacement cat in pool)
            {
                colorCandidateCount.TryGetValue(cat.ColorIndex, out int count);
                if (!found || count < bestCount || (count == bestCount && cat.ColorIndex < best.ColorIndex))
                {
                    found = true;
                    best = cat;
                    bestCount = count;
                }
            }

            return new HintResult(
                new[] { (best.Row, best.Col) },
                best.ColorIndex,
                HintReason.Exact,
                HintMessages.Exact);
        }
    }
}
