using Cast.Game.Data;

namespace Cast.Game.Level
{

    public interface ILevelParser
    {
        bool TryParse(string json, out LevelData level, out LevelValidationResult result);
        bool TryParse(RawLevel raw, out LevelData level, out LevelValidationResult result);
    }
}
