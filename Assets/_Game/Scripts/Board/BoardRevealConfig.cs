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
        [Tooltip("Per-cell move/scale time (seconds).")]
        public float CellDuration = 0.28f;

        [Tooltip("Delay added per cell — the 'rải' cascade (seconds).")]
        public float StaggerStep = 0.015f;

        [Tooltip("World units cells start away from their target.")]
        public float ScatterRadius = 1.2f;

        [Tooltip("Scale cells grow in from.")]
        public float StartScale = 0.3f;

        public RevealOrder Order = RevealOrder.DiagonalSweep;
    }
}
