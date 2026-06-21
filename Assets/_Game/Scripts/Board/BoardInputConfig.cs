using System;
using UnityEngine;

namespace Cast.Game.Board
{
    
    [Serializable]
    public sealed class BoardInputConfig
    {
        [Tooltip("Press shorter than this (seconds) counts as a tap.")]
        public float TapMaxDuration = 0.25f;

        [Tooltip("Two taps on the same cell within this window (seconds) = double-tap.")]
        public float DoubleTapWindow = 0.30f;

        [Tooltip("Held longer than this (seconds) without moving = long-press.")]
        public float LongPressDuration = 0.40f;

        [Tooltip("Pointer movement beyond this (pixels) is treated as a drag, not a tap.")]
        public float TapSlopPixels = 24f;
    }
}
