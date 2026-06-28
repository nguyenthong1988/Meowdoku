using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{
    public sealed class PopupPause : Modal
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _homeButton;

        private UniTaskCompletionSource<PauseChoice> _choice;

        public UniTask<PauseChoice> WaitForChoiceAsync()
        {
            _choice = new UniTaskCompletionSource<PauseChoice>();

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(() => _choice.TrySetResult(PauseChoice.Resume));
            }
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(() => _choice.TrySetResult(PauseChoice.Restart));
            }
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => _choice.TrySetResult(PauseChoice.Settings));
            }
            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(() => _choice.TrySetResult(PauseChoice.Home));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(PauseChoice.Resume);
    }
}
