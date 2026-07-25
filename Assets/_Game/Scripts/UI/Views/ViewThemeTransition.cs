using System;
using CaskFramework.Core;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;

namespace Cast.Game
{
    public sealed class ViewThemeTransition : Page
    {
        [SerializeField] private CanvasGroup _textGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _levelRangeText;
        [SerializeField] private Button _continueButton;
        [SerializeField] private float _transitionDelay = 0.3f;
        [SerializeField] private float _themeTransitionDuration = 1f;
        [SerializeField] private float _textFadeDuration = 0.4f;
        [SerializeField] private float _buttonFadeDuration = 0.25f;

        private CanvasGroup _continueButtonCanvasGroup;
        private Action _onContinue;
        private bool _continued;

        public override UniTask Initialize(Memory<object> args)
        {
            _continueButtonCanvasGroup = _continueButton.GetComponent<CanvasGroup>();
            _textGroup.alpha = 0f;
            _continueButtonCanvasGroup.alpha = 0f;
            _continueButtonCanvasGroup.interactable = false;
            return base.Initialize(args);
        }

        public void Setup(ThemeFeature theme, string newBackgroundKey,
                          int startLevel, int endLevel, Action onContinue)
        {
            _onContinue = onContinue;
            _continued = false;

            if (_titleText != null) _titleText.text = "NEW REALM";
            if (_levelRangeText != null) _levelRangeText.text = $"LEVEL {startLevel} - {endLevel}";

            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(Continue);

            RunTransitionAsync(theme, newBackgroundKey).Forget();
        }

        private async UniTaskVoid RunTransitionAsync(ThemeFeature theme, string newBackgroundKey)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_transitionDelay));

            if (theme != null)
                await theme.TransitionToAsync(newBackgroundKey, _themeTransitionDuration);

            await LMotion.Create(0f, 1f, _textFadeDuration)
                .Bind(_textGroup, (a, g) => g.alpha = a)
                .ToUniTask();

            _continueButton.transform.localScale = Vector3.one * 0.65f;

            var buttonScaleMotion = LMotion.Create(Vector3.one * 0.65f, Vector3.one, _buttonFadeDuration)
                .Bind(_continueButton.transform, (s, t) => t.localScale = s);

            var buttonAlphaMotion = LMotion.Create(0f, 1f, _buttonFadeDuration)
                .Bind(_continueButtonCanvasGroup, (a, g) => g.alpha = a);

            await UniTask.WhenAll(buttonScaleMotion.ToUniTask(), buttonAlphaMotion.ToUniTask());

            _continueButtonCanvasGroup.interactable = true;
        }

        private void Continue()
        {
            if (_continued) return;
            _continued = true;
            Action callback = _onContinue;
            _onContinue = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            Continue();
        }
    }
}
