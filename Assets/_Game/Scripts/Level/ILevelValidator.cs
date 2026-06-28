using Cast.Game;

namespace Cast.Game
{

    public interface ILevelValidator
    {
        LevelValidationResult Validate(LevelData level);
    }
}
