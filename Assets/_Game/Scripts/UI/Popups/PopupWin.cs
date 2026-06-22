using Cast.Game.Gameplay;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{
    public sealed class PopupWin : Modal
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _heartsLabel;
        [SerializeField] private Text _movesLabel;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _homeButton;

        private UniTaskCompletionSource<WinChoice> _choice;

        public UniTask<WinChoice> WaitForChoiceAsync(GameResult result)
        {
            _choice = new UniTaskCompletionSource<WinChoice>();

            if (_titleLabel != null) _titleLabel.text = "You Win!";
            if (_heartsLabel != null) _heartsLabel.text = $"Hearts left: {result.HeartsLeft}";
            if (_movesLabel != null) _movesLabel.text = $"Moves: {result.Moves}";

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(() => _choice.TrySetResult(WinChoice.Next));
            }
            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveAllListeners();
                _replayButton.onClick.AddListener(() => _choice.TrySetResult(WinChoice.Replay));
            }
            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(() => _choice.TrySetResult(WinChoice.Home));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(WinChoice.Next);
    }
}
