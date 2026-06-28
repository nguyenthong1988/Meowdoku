using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public interface ILevelDataReader
    {   
        UniTask<LevelReadResult> ReadLevelAsync(int levelId);
        UniTask<LevelReadResult> ReadLevelByKeyAsync(string addressableKey);
    }
}
