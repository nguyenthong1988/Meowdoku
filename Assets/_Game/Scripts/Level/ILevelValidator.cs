using Cast.Game.Data;

namespace Cast.Game.Level
{

    public interface ILevelValidator
    {
        LevelValidationResult Validate(LevelData level);
    }
}
