using System;
using UnityEngine;

namespace Cast.Game.Gameplay
{

    [Serializable]
    public sealed class GameSessionConfig
    {
        [Min(1)] public int HeartsMax = 3;
        [Min(0)] public int HintsMax = 3;
    }
}
