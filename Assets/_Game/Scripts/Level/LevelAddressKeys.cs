namespace Cast.Game.Level
{

    public static class LevelAddressKeys
    {
        
        public const string LevelKeyPrefix = "level_";

        public static string ForLevel(int levelId) => LevelKeyPrefix + levelId;
    }
}
