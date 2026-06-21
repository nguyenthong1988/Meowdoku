using System;
using CaskFramework.Assets;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Level
{

    public sealed class LevelDataReader : ILevelDataReader
    {
        private readonly IAssetManager _assetManager;
        private readonly ILevelParser _parser;

        public LevelDataReader(IAssetManager assetManager, ILevelParser parser)
        {
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public UniTask<LevelReadResult> ReadLevelAsync(int levelId) =>
            ReadLevelByKeyAsync(LevelAddressKeys.ForLevel(levelId));

        public async UniTask<LevelReadResult> ReadLevelByKeyAsync(string addressableKey)
        {
            if (string.IsNullOrEmpty(addressableKey))
                return Fail(LevelRule.Malformed, "Addressable key is null or empty.");

            string json;
            try
            {
                json = await _assetManager.GetTextAsync(addressableKey);
            }
            catch (Exception e)
            {
                return Fail(LevelRule.Malformed, $"Failed to load level text for key \"{addressableKey}\": {e.Message}");
            }

            try
            {
                if (string.IsNullOrEmpty(json))
                    return Fail(LevelRule.Malformed, $"Level text for key \"{addressableKey}\" was empty.");

                _parser.TryParse(json, out var level, out var result);
                return new LevelReadResult(level, result);
            }
            finally
            {

                _assetManager.WeakRelease(addressableKey);
            }
        }

        private static LevelReadResult Fail(LevelRule rule, string message)
        {
            var result = new LevelValidationResult();
            result.Add(rule, message);
            return new LevelReadResult(null, result);
        }
    }
}
