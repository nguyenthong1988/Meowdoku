using Cast.Game.Gameplay;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{
    public sealed class PopupLose : Modal
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _messageLabel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        private UniTaskCompletionSource<LoseChoice> _choice;

        public UniTask<LoseChoice> WaitForChoiceAsync(GameResult result)
        {
            _choice = new UniTaskCompletionSource<LoseChoice>();

            if (_titleLabel != null) _titleLabel.text = "You Lose";
            if (_messageLabel != null) _messageLabel.text = $"Moves: {result.Moves}";

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => _choice.TrySetResult(LoseChoice.Retry));
            }
            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(() => _choice.TrySetResult(LoseChoice.Home));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(LoseChoice.Retry);
    }
}
