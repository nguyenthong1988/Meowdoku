using Cast.Game.Flow;
using Cast.Game.Gameplay;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{

    public sealed class PopupLevelComplete : Modal
    {
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _summaryLabel;

        private UniTaskCompletionSource<ResultChoice> _choice;

        public UniTask<ResultChoice> WaitForChoiceAsync(GameResult result)
        {
            _choice = new UniTaskCompletionSource<ResultChoice>();
            if (_summaryLabel != null)
                _summaryLabel.text = $"Cleared! Hearts left: {result.HeartsLeft}";

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(() => _choice.TrySetResult(ResultChoice.Next));
            }
            return _choice.Task;
        }

        private void OnDestroy() => _choice?.TrySetResult(ResultChoice.Quit);
    }
}
