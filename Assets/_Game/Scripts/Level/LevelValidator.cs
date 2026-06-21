using System;
using System.Collections.Generic;
using Cast.Game.Data;

namespace Cast.Game.Level
{

    public sealed class LevelValidator : ILevelValidator
    {
        private const int MinSize = 3;
        private const int MaxSize = 15;

        public LevelValidationResult Validate(LevelData level)
        {
            var result = new LevelValidationResult();
            if (level == null)
            {
                result.Add(LevelRule.BoardSize, "Level is null.");
                return result;
            }

            int size = level.Size;
            if (size < MinSize || size > MaxSize)
                result.Add(LevelRule.BoardSize, $"Board size {size} out of range ({MinSize}..{MaxSize}).");

            if (level.Grid == null ||
                level.Grid.GetLength(0) != size || level.Grid.GetLength(1) != size)
            {
                result.Add(LevelRule.BoardSize, "Grid dimensions do not match Size.");
                return result; 
            }

            CheckCellsAndRegions(level, result);
            CheckSolutionRules(level, result);
            return result;
        }

        private static void CheckCellsAndRegions(LevelData level, LevelValidationResult result)
        {
            int size = level.Size;
            int paletteLength = level.Colors?.Length ?? -1; 

            var catsPerColor = new Dictionary<int, int>();

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    CellData cell = level.Grid[r, c];

                    if (!cell.IsFilled)
                    {
                        result.Add(LevelRule.EmptyCell, $"Empty cell at (row {r}, col {c}).");
                        continue;
                    }

                    if (paletteLength >= 0 && cell.ColorIndex >= paletteLength)
                        result.Add(LevelRule.PaletteRange,
                            $"Color index {cell.ColorIndex} at (row {r}, col {c}) exceeds palette ({paletteLength}).");

                    if (!catsPerColor.ContainsKey(cell.ColorIndex))
                        catsPerColor[cell.ColorIndex] = 0;

                    if (cell.HasCat)
                        catsPerColor[cell.ColorIndex]++;
                }
            }

            foreach (KeyValuePair<int, int> kv in catsPerColor)
            {
                if (kv.Value == 1)
                    continue;

                if (kv.Value > 1)
                    result.Add(LevelRule.OneCatPerColor,
                        $"Color {kv.Key} carries {kv.Value} cats (must be exactly 1).");
                else
                    result.Add(LevelRule.OneCatPerRegion,
                        $"Region (color {kv.Key}) has no cat.");
            }
        }

        private static void CheckSolutionRules(LevelData level, LevelValidationResult result)
        {
            IReadOnlyList<CatPlacement> cats = level.Solution;
            if (cats == null || cats.Count == 0)
                return;

            var rows = new HashSet<int>();
            var cols = new HashSet<int>();

            for (int i = 0; i < cats.Count; i++)
            {
                CatPlacement cat = cats[i];

                if (!rows.Add(cat.Row))
                    result.Add(LevelRule.NoSharedRow, $"Two cats share row {cat.Row}.");
                if (!cols.Add(cat.Col))
                    result.Add(LevelRule.NoSharedColumn, $"Two cats share column {cat.Col}.");

                for (int j = i + 1; j < cats.Count; j++)
                {
                    CatPlacement other = cats[j];
                    if (Math.Abs(cat.Row - other.Row) <= 1 && Math.Abs(cat.Col - other.Col) <= 1)
                        result.Add(LevelRule.NoContact,
                            $"Cats at (row {cat.Row}, col {cat.Col}) and (row {other.Row}, col {other.Col}) touch.");
                }
            }
        }
    }
}
