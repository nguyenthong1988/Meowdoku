using CaskFramework.Profile;
using CaskFramework.Core;
using UnityScreenNavigator.Runtime.Core.Page;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{
    public sealed class ViewHome : Page
    {
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _coinLabel;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        private const string CoinKey = "coin";

        private UniTaskCompletionSource<HomeChoice> _choice;
        private IProfileService _profile;

        public UniTask<HomeChoice> WaitForChoiceAsync(IProfileService profile)
        {
            _profile = profile;
            _choice = new UniTaskCompletionSource<HomeChoice>();

            Refresh();

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => _choice.TrySetResult(HomeChoice.Play));
            }
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => _choice.TrySetResult(HomeChoice.Settings));
            }
            return _choice.Task;
        }

        public void Refresh()
        {
            if (_profile == null) return;
            if (_levelLabel != null) _levelLabel.text = $"Level {_profile.ProgressLevel}";
            if (_coinLabel != null) _coinLabel.text = _profile.GetBalance(CoinKey).ToString();
        }

        private void OnDestroy() => _choice?.TrySetResult(HomeChoice.Play);
    }
}
