using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cast.Game
{
    public sealed class PopupOutOfMove : ChoicePopup<LoseChoice>
    {
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _reviveButton;

        public void Setup(GameResult result)
        {
            if (_titleLabel != null) _titleLabel.text = "You Lose";

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => Choose(LoseChoice.Retry));
            }
            if (_reviveButton != null)
            {
                _reviveButton.onClick.RemoveAllListeners();
                _reviveButton.onClick.AddListener(() => Choose(LoseChoice.Revive));
                _reviveButton.interactable = true;
            }
        }
    }
}
