using System;
using UnityEngine;

namespace Cast.Game
{

    [Serializable]
    public sealed class GameSessionConfig
    {
        [Min(1)] public int HeartsMax = 3;
        [Min(0)] public int HintsMax = 3;
        [Min(1)] public int HintUnlockLevel = 2;
        [Min(1)] public int RevealUnlockLevel = 2;
    }
}
