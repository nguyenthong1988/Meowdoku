using System.Collections.Generic;
using UnityEngine;

namespace Cast.Game.Data
{

    public sealed class LevelData
    {
        
        public int Id { get; }

        public int Size { get; }

        public Difficulty Difficulty { get; }

        public CellData[,] Grid { get; }

        public Color[] Colors { get; }

        public IReadOnlyList<CatPlacement> Solution { get; }

        public int RegionCount { get; }

        public LevelData(
            int id,
            int size,
            Difficulty difficulty,
            CellData[,] grid,
            Color[] colors,
            IReadOnlyList<CatPlacement> solution,
            int regionCount)
        {
            Id = id;
            Size = size;
            Difficulty = difficulty;
            Grid = grid;
            Colors = colors;
            Solution = solution;
            RegionCount = regionCount;
        }

        public CellData GetCell(int row, int col) => Grid[row, col];

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Size && col >= 0 && col < Size;
    }
}
