using System;
using CaskFramework.Ads;
using CaskFramework.Core;
using CaskFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public sealed class CheatPopup : Modal
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _adsDebugButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _errorText;

        private Action<int> _onGoto;
        private Action _onWin;
        private Action _onLose;

        public void Bind(Action<int> onGoto, Action onWin, Action onLose)
        {
            _onGoto = onGoto;
            _onWin = onWin;
            _onLose = onLose;

            if (_inputField != null) _inputField.text = string.Empty;
            if (_errorText != null) _errorText.gameObject.SetActive(false);

            if (_submitButton != null)
            {
                _submitButton.onClick.RemoveAllListeners();
                _submitButton.onClick.AddListener(OnSubmit);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Hide);
            }

            if (_adsDebugButton != null)
            {
                _adsDebugButton.onClick.RemoveAllListeners();
                _adsDebugButton.onClick.AddListener(ShowAdDebugger);
            }
        }

        private void OnSubmit()
        {
            string command = _inputField != null ? _inputField.text : null;
            if (string.IsNullOrWhiteSpace(command)) return;

            string[] data = command.Trim().Split(' ');
            switch (data[0].ToLower())
            {
                case "goto":
                case "level":
                    if (data.Length > 1 && int.TryParse(data[1], out int level))
                    {
                        Action<int> callback = _onGoto;
                        Hide();
                        callback?.Invoke(Mathf.Max(1, level));
                    }
                    else
                    {
                        ShowError("Usage: goto <level>");
                    }
                    break;

                case "win":
                    CloseThen(_onWin);
                    break;

                case "lose":
                    CloseThen(_onLose);
                    break;

                case "addebug":
                    CloseThen(ShowAdDebugger);
                    break;

                case "adon":
                    CloseThen(() => SetAdsEnabled(true));
                    break;

                case "adoff":
                    CloseThen(() => SetAdsEnabled(false));
                    break;

                default:
                    ShowError($"Invalid command: {command}");
                    break;
            }
        }

        private void CloseThen(Action action)
        {
            Hide();
            action?.Invoke();
        }

        private void ShowAdDebugger()
        {
            if (GameRuntime.TryGet(out IAdsService ads))
                ads.ShowDebugger();
        }

        private void SetAdsEnabled(bool enabled)
        {
            if (GameRuntime.TryGet(out IAdsService ads))
                ads.TurnOffAds(!enabled);
        }

        private void ShowError(string message)
        {
            if (_errorText == null) return;
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (GameRuntime.TryGet(out IUIManager ui))
                ui.PopPopup();
        }
    }
}
