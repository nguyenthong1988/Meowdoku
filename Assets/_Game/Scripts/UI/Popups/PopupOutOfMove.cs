
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cast.Game
{
    public sealed class PopupOutOfMove : Modal
    {
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _reviveButton;

        private UniTaskCompletionSource<LoseChoice> _choice;

        public UniTask<LoseChoice> WaitForChoiceAsync(GameResult result)
        {
            _choice = new UniTaskCompletionSource<LoseChoice>();

            if (_titleLabel != null) _titleLabel.text = "You Lose";

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => _choice.TrySetResult(LoseChoice.Retry));
            }
            if (_reviveButton != null)
            {
                _reviveButton.onClick.RemoveAllListeners();
                _reviveButton.onClick.AddListener(() => _choice.TrySetResult(LoseChoice.Revive));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(LoseChoice.Retry);
    }
}
