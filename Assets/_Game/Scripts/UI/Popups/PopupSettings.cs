using CaskFramework.Audio;
using CaskFramework.Core;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CaskFramework.UI;

namespace Cast.Game
{
    public sealed class PopupSettings : Modal
    {
        [SerializeField] private UISliderToggle _musicToggle;
        [SerializeField] private UISliderToggle _sfxToggle;
        [SerializeField] private UISliderToggle _vibrationToggle;
        [SerializeField] private Button _feedbackButton;
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _privacyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _versionText;

        private IAudioManager _audio;

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
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() =>
                {
                    GameRuntime.Get<IUIManager>().PopPopup();
                });
            }

            RefreshLabels();
            return base.Initialize(args);
        }

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
