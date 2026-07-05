using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public sealed class PopupTutorialHint : Modal
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _messageTextTop;
        [SerializeField] private TMP_Text _messageTextBot;
        [SerializeField] private Transform _contentTop;
        [SerializeField] private Transform _contentBot;

        private Action _onConfirm;
        private bool _confirmed;

        public bool Applied { get; private set; }

        public void Show(string messageTop, string messageBot, Action onConfirm)
        {
            if (onConfirm == null)
            {
                _confirmButton.gameObject.SetActive(false);
            }
            else
            {
                _confirmButton.gameObject.SetActive(true);
                SetConfirmCallback(onConfirm);
            }

            if (string.IsNullOrEmpty(messageTop))
            {
                _contentTop.gameObject.SetActive(false);
            }
            else
            {
                _contentTop.gameObject.SetActive(true);
                _messageTextTop.text = messageTop;
            }
            if (string.IsNullOrEmpty(messageBot))
            {
                _contentBot.gameObject.SetActive(false);
            }
            else
            {
                _contentBot.gameObject.SetActive(true);
                _messageTextBot.text = messageBot;
            }
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
