using CaskFramework.Audio;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Cast.Game
{
    public sealed class PopupSettings : Modal
    {
        [SerializeField] private UISliderToggle _musicToggle;
        [SerializeField] private UISliderToggle _sfxToggle;
        [SerializeField] private UISliderToggle _vibrationToggle;
        [SerializeField] private Button _feedbackButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _privacyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _versionText;

        private IAudioManager _audio;
        private Action _onFeedbackClicked;
        private Action _onRetryClicked;

        public override UniTask Initialize(Memory<object> args)
        {
            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.RemoveAllListeners();
                _musicToggle.onValueChanged.AddListener(ToggleMusic);
                _musicToggle.Init(_audio?.IsMusicOn ?? true);
            }
            if (_sfxToggle != null)
            {
                _sfxToggle.onValueChanged.RemoveAllListeners();
                _sfxToggle.onValueChanged.AddListener(ToggleSfx);
                _sfxToggle.Init(_audio?.IsSfxOn ?? true);
            }
            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.RemoveAllListeners();
                _vibrationToggle.onValueChanged.AddListener(ToggleVibration);
                _vibrationToggle.Init(true);
            }

            RefreshLabels();
            return base.Initialize(args);
        }

        public void Setup(Action onClose = null, Action onFeedback = null, Action onRetry = null)
        {
            _onFeedbackClicked = onFeedback;
            _onRetryClicked = onRetry;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => onClose?.Invoke());
            }

            if (_feedbackButton != null)
            {
                _feedbackButton.onClick.RemoveAllListeners();
                _feedbackButton.onClick.AddListener(OnButtonFeedBackClicked);
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(OnButtonRetryClicked);
            }

            if (_termsButton != null)
            {
                _termsButton.onClick.RemoveAllListeners();
                _termsButton.onClick.AddListener(() => Application.OpenURL("https://www.example.com/terms"));
            }

            if (_privacyButton != null)
            {
                _privacyButton.onClick.RemoveAllListeners();
                _privacyButton.onClick.AddListener(() => Application.OpenURL("https://www.example.com/privacy"));
            }
        }

        private void OnButtonFeedBackClicked() => _onFeedbackClicked?.Invoke();

        private void OnButtonRetryClicked() => _onRetryClicked?.Invoke();

        private void ToggleMusic(bool isOn)
        {
            _audio?.ToggleMusic();
            RefreshLabels();
        }

        private void ToggleSfx(bool isOn)
        {
            _audio?.ToggleSfx();
            RefreshLabels();
        }

        private void ToggleVibration(bool isOn)
        {
            RefreshLabels();
        }

        private void RefreshLabels()
        {
        }
    }
}
