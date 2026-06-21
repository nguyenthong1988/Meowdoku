namespace Cast.Game.Data
{

    public enum Difficulty : byte
    {
        Normal = 0,    
        Hard = 1,      
        Ultra = 2,     
        Challenge = 3, 
    }

    public static class DifficultyExtensions
    {

        public static bool TryParse(char code, out Difficulty difficulty)
        {
            switch (char.ToLowerInvariant(code))
            {
                case 'n': difficulty = Difficulty.Normal; return true;
                case 'h': difficulty = Difficulty.Hard; return true;
                case 'u': difficulty = Difficulty.Ultra; return true;
                case 'c': difficulty = Difficulty.Challenge; return true;
                default: difficulty = Difficulty.Normal; return false;
            }
        }

        public static bool TryParse(string code, out Difficulty difficulty)
        {
            if (!string.IsNullOrEmpty(code))
                return TryParse(code[0], out difficulty);
            difficulty = Difficulty.Normal;
            return false;
        }

        public static char ToCode(this Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return 'n';
                case Difficulty.Hard: return 'h';
                case Difficulty.Ultra: return 'u';
                case Difficulty.Challenge: return 'c';
                default: return 'n';
            }
        }
    }
}
