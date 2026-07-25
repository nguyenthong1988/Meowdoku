using System;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaskFramework.Core;
using CaskFramework.Audio;

namespace Cast.Game
{
    public sealed class ViewHome : Page
    {
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _dailyChallengeButton;
        [SerializeField] private TextMeshProUGUI _streakLabel;
        [SerializeField] private GameObject _dailyChallengeCompleted;

        private const string CoinKey = "coin";

        private Action _onPlayClicked;
        private Action _onDailyChallengeClicked;
        private IProfileService _profile;
        private IUIManager _ui;

        private CanvasGroup _buttonPlayCanvasGroup;

        private void Awake()
        {
            _buttonPlayCanvasGroup = _playButton.GetComponent<CanvasGroup>();
            if (_buttonPlayCanvasGroup)
            {
                _buttonPlayCanvasGroup.alpha = 0f;
                _buttonPlayCanvasGroup.interactable = false;
            }
        }

        public void Setup(Action onPlayClicked, Action onDailyChallengeClicked,
                          IProfileService profile, IUIManager ui = null)
        {
            GameRuntime.Get<IAudioManager>().PlayMusic(AudioNames.BGM_BACKGROUND, 0.5f);

            _onPlayClicked = onPlayClicked;
            _onDailyChallengeClicked = onDailyChallengeClicked;
            _profile = profile;
            _ui = ui;

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() =>
                {
                    _onPlayClicked?.Invoke();
                    _onPlayClicked = null;
                });
            }

            FeatureManager.TryGet(out DailyChallengeFeature dailyChallenge);

            if (_dailyChallengeButton != null)
            {
                _dailyChallengeButton.onClick.RemoveAllListeners();
                bool available = dailyChallenge != null && dailyChallenge.IsAvailableToday();
                _dailyChallengeButton.interactable = available;

                if (_dailyChallengeCompleted != null)
                    _dailyChallengeCompleted.SetActive(!available);

                _dailyChallengeButton.onClick.AddListener(() =>
                {
                    _onDailyChallengeClicked?.Invoke();
                    _onDailyChallengeClicked = null;
                });
            }

            if (_streakLabel != null && dailyChallenge != null)
                _streakLabel.text = dailyChallenge.Data.CurrentStreak.ToString();

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_profile == null) return;
            if (_levelLabel != null) _levelLabel.text = $"Level {_profile.CurrentLevelId()}";
        }

        public override void DidPushEnter(Memory<object> args)
        {
            PlayAppearAnimation();
        }

        private void PlayAppearAnimation()
        {
            if (_buttonPlayCanvasGroup == null) return;

            var buttonTransform = _playButton.transform;
            buttonTransform.localScale = Vector3.one * 0.65f;

            LMotion.Create(0f, 1f, 0.25f)
                .Bind(_buttonPlayCanvasGroup, (a, g) => g.alpha = a)
                .AddTo(_playButton.gameObject);

            LMotion.Create(0.65f, 1f, 0.25f)
                .WithEase(Ease.OutBack)
                .WithOnComplete(() => _buttonPlayCanvasGroup.interactable = true)
                .Bind(buttonTransform, (s, t) => t.localScale = Vector3.one * s)
                .AddTo(_playButton.gameObject);
        }

        private void OnSettingsButtonClicked()
        {
            if (_ui == null) return;
            OpenSettingsAsync().Forget();
        }

        private async UniTaskVoid OpenSettingsAsync()
        {
            PopupSettings popup = null;
            await _ui.PushPopupAsync<PopupSettings>(UIConst.PopupSettings, onLoad: (_, p) => popup = p);
            popup?.Setup(
                onClose: () => _ui.PopPopupAsync().Forget(),
                onFeedback: OnButtonFeedBackClicked);
        }

        private void OnButtonFeedBackClicked()
        {
        }
    }
}
