using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public sealed class PopupBoosterHint : Modal
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Image _colorSwatch;

        private Action _onConfirm;
        private bool _confirmed;

        public bool Applied { get; private set; }

        public void Show(string message, Color color)
        {
            if (_messageText != null) _messageText.text = message;
            if (_colorSwatch != null) _colorSwatch.color = color;
        }

        public void SetConfirmCallback(Action onConfirm)
        {
            _onConfirm = onConfirm;
            _confirmed = false;
            Applied = false;
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        private void OnConfirmButtonClicked()
        {
            Applied = true;
            Confirm();
        }

        private void Confirm()
        {
            if (_confirmed) return;
            _confirmed = true;
            Action callback = _onConfirm;
            _onConfirm = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            Confirm();
        }
    }
}
