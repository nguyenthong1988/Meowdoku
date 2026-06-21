using System;
using System.Collections.Generic;
using Cast.Game.Data;
using UnityEngine;

namespace Cast.Game.Level
{

    public sealed class LevelParser : ILevelParser
    {
        private const int MinSize = 3;
        private const int MaxSize = 15;

        public bool TryParse(string json, out LevelData level, out LevelValidationResult result)
        {
            level = null;
            result = new LevelValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.Add(LevelRule.Malformed, "Level JSON is null or empty.");
                return false;
            }

            RawLevel raw;
            try
            {
                raw = JsonUtility.FromJson<RawLevel>(json);
            }
            catch (Exception e)
            {
                result.Add(LevelRule.Malformed, $"Failed to deserialize level JSON: {e.Message}");
                return false;
            }

            if (raw == null)
            {
                result.Add(LevelRule.Malformed, "Level JSON deserialized to null.");
                return false;
            }

            return TryParse(raw, out level, out result);
        }

        public bool TryParse(RawLevel raw, out LevelData level, out LevelValidationResult result)
        {
            level = null;
            result = new LevelValidationResult();

            if (raw == null)
            {
                result.Add(LevelRule.Malformed, "RawLevel is null.");
                return false;
            }

            int size = raw.s;
            if (size < MinSize || size > MaxSize)
            {
                result.Add(LevelRule.BoardSize, $"Invalid board size s={size} (must be {MinSize}..{MaxSize}).");
                return false; 
            }

            string d = raw.d;
            int expected = size * size;
            if (string.IsNullOrEmpty(d) || d.Length != expected)
            {
                int actual = d?.Length ?? 0;
                result.Add(LevelRule.BoardDataLength, $"d.length ({actual}) != s*s ({expected}).");
                return false; 
            }

            if (!DifficultyExtensions.TryParse(raw.g, out Difficulty difficulty))
                result.Add(LevelRule.Malformed, $"Invalid difficulty g=\"{raw.g}\" (expected n/h/u/c).");

            Color[] colors = ResolvePalette(raw.c, result);
            int paletteLength = colors?.Length ?? -1; 

            var grid = new CellData[size, size];
            var solution = new List<CatPlacement>();
            var catColors = new HashSet<int>();
            var regions = new HashSet<int>();

            for (int i = 0; i < d.Length; i++)
            {
                int row = i / size;
                int col = i % size;
                char ch = d[i];

                if (ch == '.')
                {
                    grid[row, col] = CellData.Empty;
                    continue;
                }

                bool hasCat = ch >= 'A' && ch <= 'Z';
                bool isLower = ch >= 'a' && ch <= 'z';
                if (!hasCat && !isLower)
                {
                    result.Add(LevelRule.CharacterEncoding,
                        $"Invalid char '{ch}' at index {i} (row {row}, col {col}).");
                    grid[row, col] = CellData.Empty;
                    continue;
                }

                int colorIndex = hasCat ? ch - 'A' : ch - 'a';

                if (paletteLength >= 0 && colorIndex >= paletteLength)
                    result.Add(LevelRule.PaletteRange,
                        $"Color index {colorIndex} at index {i} is out of palette range ({paletteLength}).");

                regions.Add(colorIndex);
                grid[row, col] = new CellData((sbyte)colorIndex, hasCat);

                if (hasCat)
                {
                    if (!catColors.Add(colorIndex))
                        result.Add(LevelRule.OneCatPerColor,
                            $"Color index {colorIndex} has more than one cat (duplicate uppercase letter).");
                    solution.Add(new CatPlacement(row, col, (sbyte)colorIndex));
                }
            }

            if (!result.IsValid)
                return false;

            level = new LevelData(
                id: raw.l,
                size: size,
                difficulty: difficulty,
                grid: grid,
                colors: colors,
                solution: solution,
                regionCount: regions.Count);
            return true;
        }

        private static Color[] ResolvePalette(string[] hex, LevelValidationResult result)
        {
            if (hex == null || hex.Length == 0)
                return null;

            var colors = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++)
            {
                if (!ColorUtility.TryParseHtmlString(hex[i], out colors[i]))
                    result.Add(LevelRule.PaletteRange, $"Invalid hex color \"{hex[i]}\" at palette index {i}.");
            }
            return colors;
        }
    }
}
