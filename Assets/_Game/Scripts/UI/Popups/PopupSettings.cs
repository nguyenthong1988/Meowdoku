using CaskFramework.Audio;
using CaskFramework.Core;
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{
    public sealed class PopupSettings : Modal
    {
        [SerializeField] private Button _musicButton;
        [SerializeField] private Text _musicLabel;
        [SerializeField] private Button _sfxButton;
        [SerializeField] private Text _sfxLabel;
        [SerializeField] private Button _closeButton;

        private UniTaskCompletionSource _closed;
        private IAudioManager _audio;

        public UniTask WaitForCloseAsync()
        {
            _closed = new UniTaskCompletionSource();
            GameRuntime.TryGet(out _audio);

            if (_musicButton != null)
            {
                _musicButton.onClick.RemoveAllListeners();
                _musicButton.onClick.AddListener(ToggleMusic);
            }
            if (_sfxButton != null)
            {
                _sfxButton.onClick.RemoveAllListeners();
                _sfxButton.onClick.AddListener(ToggleSfx);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => _closed.TrySetResult());
            }

            RefreshLabels();
            return _closed.Task;
        }

        private void ToggleMusic()
        {
            _audio?.ToggleMusic();
            RefreshLabels();
        }

        private void ToggleSfx()
        {
            _audio?.ToggleSfx();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            bool musicOn = _audio == null || _audio.IsMusicOn;
            bool sfxOn = _audio == null || _audio.IsSfxOn;
            if (_musicLabel != null) _musicLabel.text = musicOn ? "Music: On" : "Music: Off";
            if (_sfxLabel != null) _sfxLabel.text = sfxOn ? "Sound: On" : "Sound: Off";
        }

        private void OnDestroy() => _closed?.TrySetResult();
    }
}
