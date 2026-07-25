using System;
using System.Collections.Generic;
using CaskFramework.Config;
using UnityEngine;

namespace Cast.Game
{
    [Serializable]
    public sealed class ThemeDefinition
    {
        public string Name;
        public int Level;
        public string Background;
        public string LotusPool;
    }

    [Serializable]
    public sealed class ThemeConfig : IGameConfig
    {
        public const string AddressableKey = "ThemeConfig";

        public List<ThemeDefinition> Themes;

        public int RepeatingLevelCount = 15;
    }
}
