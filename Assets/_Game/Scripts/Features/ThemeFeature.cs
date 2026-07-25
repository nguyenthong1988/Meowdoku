using System;
using System.Collections.Generic;
using CaskFramework.Assets;
using CaskFramework.Config;
using CaskFramework.Core;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Cast.Game
{
    public readonly struct ThemeInfo
    {
        public readonly int Index;
        public readonly int StartLevel;
        public readonly int EndLevel;
        public readonly string Name;
        public readonly string BackgroundKey;
        public readonly string LotusPoolKey;

        public ThemeInfo(int index, int startLevel, int endLevel, string name,
                         string backgroundKey, string lotusPoolKey)
        {
            Index = index;
            StartLevel = startLevel;
            EndLevel = endLevel;
            Name = name;
            BackgroundKey = backgroundKey;
            LotusPoolKey = lotusPoolKey;
        }
    }

    public sealed class ThemeFeature : MonoBehaviour, IFeature
    {
        private const string FallbackBackgroundKey = "BG1";
        private const string HomeBackgroundKey = "__home__";

        [SerializeField] private SpriteRenderer _mainRenderer;
        [SerializeField] private SpriteRenderer _transitionRenderer;
        [SerializeField] private Sprite _homeBackground;
        [SerializeField] private float _fadeDuration = 0.6f;

        private SpriteRendererFit _mainFit;
        private SpriteRendererFit _transitionFit;
        private ThemeConfig _config;
        private IAssetManager _assets;
        private string _currentBackgroundKey;

        private void Awake()
        {
            if (_mainRenderer != null)
                _mainFit = _mainRenderer.GetComponent<SpriteRendererFit>();
            if (_transitionRenderer != null)
            {
                _transitionFit = _transitionRenderer.GetComponent<SpriteRendererFit>();
                _transitionRenderer.sprite = null;
                _transitionRenderer.gameObject.SetActive(false);
            }
        }

        public void Initialize(IServiceContext context)
        {
            if (context.TryGet(out IConfigService configService) && configService.HasConfig<ThemeConfig>())
                _config = configService.GetConfig<ThemeConfig>();
            else
            {
                Debug.LogWarning("[ThemeFeature] ThemeConfig not found in IConfigService, using default values.");
                _config = new ThemeConfig();
            }

            context.TryGet(out _assets);
        }

        public async UniTask ApplyThemeAsync(int levelNumber)
        {
            if (_assets == null) return;

            string key = GetThemeInfoForLevel(levelNumber).BackgroundKey;
            if (string.IsNullOrEmpty(key) || key == _currentBackgroundKey) return;

            Sprite sprite = await LoadSpriteAsync(key);
            if (sprite == null) return;

            await ApplyBackgroundAsync(key, sprite);
        }

        public UniTask ApplyHomeAsync()
        {
            return ApplyBackgroundAsync(HomeBackgroundKey, _homeBackground);
        }

        private UniTask ApplyBackgroundAsync(string key, Sprite sprite)
        {
            if (_mainRenderer == null || sprite == null) return UniTask.CompletedTask;
            if (key == _currentBackgroundKey) return UniTask.CompletedTask;

            bool firstTime = string.IsNullOrEmpty(_currentBackgroundKey);
            _currentBackgroundKey = key;

            if (firstTime || _transitionRenderer == null)
            {
                SetSprite(_mainRenderer, _mainFit, sprite, 1f);
                return UniTask.CompletedTask;
            }

            return CrossfadeAsync(sprite, _fadeDuration);
        }

        public async UniTask TransitionToAsync(string backgroundKey, float duration)
        {
            if (_assets == null || string.IsNullOrEmpty(backgroundKey)) return;
            if (backgroundKey == _currentBackgroundKey) return;

            Sprite sprite = await LoadSpriteAsync(backgroundKey);
            if (sprite == null) return;

            bool firstTime = string.IsNullOrEmpty(_currentBackgroundKey);
            _currentBackgroundKey = backgroundKey;

            if (_mainRenderer == null) return;

            if (firstTime || _transitionRenderer == null)
            {
                SetSprite(_mainRenderer, _mainFit, sprite, 1f);
                return;
            }

            await CrossfadeAsync(sprite, duration);
        }

        private async UniTask CrossfadeAsync(Sprite target, float duration)
        {
            SetSprite(_transitionRenderer, _transitionFit, target, 0f);
            _transitionRenderer.gameObject.SetActive(true);

            await UniTask.WhenAll(
                FadeAsync(_mainRenderer, 1f, 0f, duration),
                FadeAsync(_transitionRenderer, 0f, 1f, duration));

            SetSprite(_mainRenderer, _mainFit, target, 1f);

            SetAlpha(_transitionRenderer, 0f);
            _transitionRenderer.sprite = null;
            _transitionRenderer.gameObject.SetActive(false);
        }

        private async UniTask<Sprite> LoadSpriteAsync(string key)
        {
            try
            {
                return await _assets.GetAssetAsync<Sprite>(key, key);
            }
            catch (Exception err)
            {
                Debug.LogError($"[ThemeFeature] Failed to load background '{key}': {err.Message}");
                return null;
            }
        }

        private UniTask FadeAsync(SpriteRenderer renderer, float from, float to, float duration)
        {
            return LMotion.Create(from, to, duration)
                .Bind(renderer, (a, r) => SetAlpha(r, a))
                .ToUniTask();
        }

        private static void SetSprite(SpriteRenderer renderer, SpriteRendererFit fit, Sprite sprite, float alpha)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            SetAlpha(renderer, alpha);
            if (fit != null) fit.FitToScreen();
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        public int DefinedThemeCount => _config.Themes?.Count ?? 0;

        public int GetThemeIndex(int levelNumber)
        {
            if (levelNumber < 1) levelNumber = 1;

            int definedCount = DefinedThemeCount;
            if (definedCount == 0)
                return 1;

            for (int i = 0; i < definedCount - 1; i++)
            {
                if (levelNumber < StartLevelOf(i + 1))
                    return i + 1;
            }

            int lastStart = StartLevelOf(definedCount - 1);
            if (levelNumber < lastStart)
                return 1;

            int blocksPastLast = (levelNumber - lastStart) / RepeatingLevelCount;
            return definedCount + blocksPastLast;
        }

        public int GetThemeStartLevel(int themeIndex)
        {
            if (themeIndex < 1) themeIndex = 1;

            int definedCount = DefinedThemeCount;
            if (definedCount == 0)
                return 1 + (themeIndex - 1) * RepeatingLevelCount;

            if (themeIndex <= definedCount)
                return StartLevelOf(themeIndex - 1);

            int lastStart = StartLevelOf(definedCount - 1);
            return lastStart + (themeIndex - definedCount) * RepeatingLevelCount;
        }

        public int GetThemeEndLevel(int themeIndex)
        {
            return GetThemeStartLevel(themeIndex + 1) - 1;
        }

        public bool IsLastLevelOfTheme(int levelNumber)
        {
            return levelNumber == GetThemeEndLevel(GetThemeIndex(levelNumber));
        }

        public ThemeInfo GetThemeInfo(int themeIndex)
        {
            if (themeIndex < 1) themeIndex = 1;
            ThemeDefinition definition = DefinitionOf(themeIndex);
            return new ThemeInfo(
                themeIndex,
                GetThemeStartLevel(themeIndex),
                GetThemeEndLevel(themeIndex),
                definition?.Name ?? $"Realm {themeIndex}",
                string.IsNullOrEmpty(definition?.Background) ? FallbackBackgroundKey : definition.Background,
                definition?.LotusPool);
        }

        public ThemeInfo GetThemeInfoForLevel(int levelNumber)
        {
            return GetThemeInfo(GetThemeIndex(levelNumber));
        }

        private ThemeDefinition DefinitionOf(int themeIndex)
        {
            int definedCount = DefinedThemeCount;
            if (definedCount == 0)
                return null;
            return _config.Themes[(themeIndex - 1) % definedCount];
        }

        private int StartLevelOf(int definedIndex)
        {
            ThemeDefinition definition = _config.Themes[definedIndex];
            return definition != null && definition.Level > 0 ? definition.Level : 1;
        }

        private int RepeatingLevelCount => _config.RepeatingLevelCount > 0 ? _config.RepeatingLevelCount : 1;
    }
}
