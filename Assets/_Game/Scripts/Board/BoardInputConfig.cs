using System;
using UnityEngine;

namespace Cast.Game
{
    
    [Serializable]
    public sealed class BoardInputConfig
    {
        public float TapMaxDuration = 0.25f;

        public float DoubleTapWindow = 0.30f;

        public float LongPressDuration = 0.40f;

        public float TapSlopPixels = 24f;
    }
}
