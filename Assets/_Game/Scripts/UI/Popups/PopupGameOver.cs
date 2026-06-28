
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{

    public sealed class PopupGameOver : Modal
    {
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _quitButton;

        private UniTaskCompletionSource<ResultChoice> _choice;

        public UniTask<ResultChoice> WaitForChoiceAsync(GameResult result)
        {
            _choice = new UniTaskCompletionSource<ResultChoice>();

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => _choice.TrySetResult(ResultChoice.Retry));
            }
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(() => _choice.TrySetResult(ResultChoice.Quit));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(ResultChoice.Quit);
    }
}
