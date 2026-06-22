using System;
using UnityEngine;

namespace Cast.Game.Board
{
    
    public enum RevealOrder : byte
    {
        RowByRow = 0,
        Radial = 1,
        Random = 2,
        DiagonalSweep = 3,
    }

    [Serializable]
    public sealed class BoardRevealConfig
    {
        public float CellDuration = 0.28f;

        public float StaggerStep = 0.015f;

        public float ScatterRadius = 1.2f;

        public float StartScale = 0.3f;

        public RevealOrder Order = RevealOrder.DiagonalSweep;
    }
}
