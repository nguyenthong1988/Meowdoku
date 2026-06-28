using System;
using CaskFramework.Profile;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaskFramework.Core;
using CaskFramework.UI;

namespace Cast.Game
{
    public sealed class ViewHome : Page
    {
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        private const string CoinKey = "coin";

        private Action _onPlayClicked;
        private IProfileService _profile;

        public void Setup(Action onPlayClicked, IProfileService profile)
        {
            _onPlayClicked = onPlayClicked;
            _profile = profile;

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => 
                {
                    _onPlayClicked?.Invoke();
                    _onPlayClicked = null;
                });
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => 
                {
                    GameRuntime.Get<IUIManager>().PushPopup("PopupSettings");
                });
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_profile == null) return;
            if (_levelLabel != null) _levelLabel.text = $"Level {_profile.ProgressLevel}";
        }
    }
}
