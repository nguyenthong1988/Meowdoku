
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CaskFramework.Audio;
using CaskFramework.Core;
using UnityScreenNavigator.Runtime.Core.Page;

namespace Cast.Game
{
    public sealed class ViewResult : Page
    {
        [SerializeField] private TextMeshProUGUI _kudoText;
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TextMeshProUGUI _timeLabel;
        [SerializeField] private GameObject _streakGroup;
        [SerializeField] private TextMeshProUGUI _currentStreakLabel;
        [SerializeField] private TextMeshProUGUI _bestStreakLabel;

        private CanvasGroup _kudoCanvasGroup, _nextButtonCanvasGroup;
        private readonly string[] _kudoTexts = new string[] { "Good!", "Great!", "Awesome!", "Perfect!", "Excellent!" };
        private Action<WinChoice> _onChoice;
        private bool _chosen;
        private GameMode _gameMode;

        public override UniTask Initialize(Memory<object> args)
        {
            _kudoCanvasGroup = _kudoText.GetComponent<CanvasGroup>();
            _nextButtonCanvasGroup = _nextButton.GetComponent<CanvasGroup>();
            _kudoCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.interactable = false;
            SetImageAlpha(0f, _characterImage);
            return base.Initialize(args);
        }

        public void Setup(GameResult result, int currentLevelId, Action<WinChoice> onChoice,
                          GameMode gameMode = GameMode.Normal, bool themeCompleted = false)
        {
            _onChoice = onChoice;
            _chosen = false;
            _gameMode = gameMode;

            if (_timeLabel != null)
            {
                int minutes = (int)(result.Elapsed / 60f);
                int seconds = (int)(result.Elapsed % 60f);
                _timeLabel.text = $"{minutes:D2}:{seconds:D2}";
            }

            bool isDaily = _gameMode == GameMode.DailyChallenge;

            if (_streakGroup != null)
                _streakGroup.SetActive(isDaily);

            if (isDaily && FeatureManager.TryGet(out DailyChallengeFeature dailyChallenge))
            {
                DailyChallengeData data = dailyChallenge.Data;
                if (_currentStreakLabel != null)
                    _currentStreakLabel.text = data.CurrentStreak.ToString();
                if (_bestStreakLabel != null)
                    _bestStreakLabel.text = data.BestStreak.ToString();
            }

            bool showNewRealm = themeCompleted && !isDaily;
            if (_gameMode == GameMode.DailyChallenge)
                _levelText.text = "DAILY CHALLENGE";
            else
                _levelText.text = showNewRealm ? "NEXT" : $"LEVEL {currentLevelId + 1}";


            _nextButton.onClick.RemoveAllListeners();
            if (isDaily)
                _nextButton.onClick.AddListener(() => Choose(WinChoice.Home));
            else if (showNewRealm)
                _nextButton.onClick.AddListener(() => Choose(WinChoice.NextTheme));
            else
                _nextButton.onClick.AddListener(() => Choose(WinChoice.Next));

            GameRuntime.Get<IAudioManager>().PlaySfx(AudioNames.SFX_WINNING);

            RunKudoAnimationAsync().Forget();
        }

        private void Choose(WinChoice choice)
        {
            if (_chosen) return;
            _chosen = true;
            Action<WinChoice> callback = _onChoice;
            _onChoice = null;
            callback?.Invoke(choice);
        }

        private async UniTaskVoid RunKudoAnimationAsync()
        {
            float kudoTextDuration = 0.25f;
            _kudoText.text = _kudoTexts[UnityEngine.Random.Range(0, _kudoTexts.Length)];
            _kudoText.transform.localScale = Vector3.one * 1.5f;
            _kudoCanvasGroup.alpha = 0.35f;

            var scaleMotion = LMotion.Create(Vector3.one * 1.5f, Vector3.one, kudoTextDuration)
                .Bind(_kudoText.transform, (s, t) => t.localScale = s);

            var alphaMotion = LMotion.Create(0.35f, 1f, kudoTextDuration)
                .Bind(_kudoCanvasGroup, (a, g) => g.alpha = a);

            await UniTask.WhenAll(scaleMotion.ToUniTask(), alphaMotion.ToUniTask());

            PlayWinConfetti();

            await RunCharacterRevealAsync();

            _nextButton.transform.localScale = Vector3.one * 0.65f;

            var btnScaleMotion = LMotion.Create(Vector3.one * 0.65f, Vector3.one, 0.25f)
                .Bind(_nextButton.transform, (s, t) => t.localScale = s);

            var btnAlphaMotion = LMotion.Create(0.35f, 1f, 0.25f)
                .Bind(_nextButtonCanvasGroup, (a, g) => g.alpha = a);

            await UniTask.WhenAll(btnScaleMotion.ToUniTask(), btnAlphaMotion.ToUniTask());

            _nextButtonCanvasGroup.interactable = true;
        }

        private async UniTask RunCharacterRevealAsync()
        {
            float revealDuration = 0.35f;
            _characterImage.transform.localScale = Vector3.one * 1.5f;
            SetImageAlpha(0.35f, _characterImage);

            var characterScaleMotion = LMotion.Create(Vector3.one * 1.5f, Vector3.one * 2f, revealDuration)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(_characterImage.transform);

            var characterAlphaMotion = LMotion.Create(0.35f, 1f, revealDuration)
                .Bind(_characterImage, SetImageAlpha);

            await UniTask.WhenAll(characterScaleMotion.ToUniTask(), characterAlphaMotion.ToUniTask());

            float rockStageDuration = 0.08f;
            var rockMotion = LSequence.Create()
                .Append(LMotion.Create(0f, -12f, rockStageDuration).WithEase(Ease.InOutSine).BindToLocalEulerAnglesZ(_characterImage.transform))
                .Append(LMotion.Create(-12f, 12f, rockStageDuration * 2f).WithEase(Ease.InOutSine).BindToLocalEulerAnglesZ(_characterImage.transform))
                .Append(LMotion.Create(12f, -6f, rockStageDuration * 1.5f).WithEase(Ease.InOutSine).BindToLocalEulerAnglesZ(_characterImage.transform))
                .Append(LMotion.Create(-6f, 0f, rockStageDuration).WithEase(Ease.InOutSine).BindToLocalEulerAnglesZ(_characterImage.transform))
                .Run();

            await rockMotion.ToUniTask();
        }

        private static void SetImageAlpha(float alpha, Image image)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void PlayWinConfetti()
        {
            if (VFXManager.Instance == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float depth = 10f;
            Vector3 leftMid = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height * 0.5f, depth));
            Vector3 rightMid = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height * 0.5f, depth));

            VFXManager.Instance.Play(VfxIds.WinConfetti, leftMid, Quaternion.Euler(-60f, 90f, -90f));
            VFXManager.Instance.Play(VfxIds.WinConfetti, rightMid, Quaternion.Euler(240f, 90f, -90f));
        }

        private void OnDestroy()
        {
            Choose(WinChoice.Next);
        }
    }
}
