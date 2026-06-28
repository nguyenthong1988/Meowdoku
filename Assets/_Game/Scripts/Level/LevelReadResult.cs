using Cast.Game;

namespace Cast.Game
{

    public readonly struct LevelReadResult
    {
        public readonly LevelData Level;
        public readonly LevelValidationResult Validation;

        public LevelReadResult(LevelData level, LevelValidationResult validation)
        {
            Level = level;
            Validation = validation;
        }

        public bool Success => Level != null && Validation != null && Validation.IsValid;
    }
}
