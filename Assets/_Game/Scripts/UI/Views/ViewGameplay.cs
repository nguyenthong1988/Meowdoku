using System;
using System.Threading;
using CaskFramework.Ads;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Cast.Game
{
    public sealed class ViewGameplay : Page
    {
        [Header("Hearts / labels")]
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private HeartBar _heartBar;
        [SerializeField] private CatCounter _catCounter;

        [Header("Booster buttons")]
        [SerializeField] private UIBooster _boosterHint;
        [SerializeField] private UIBooster _boosterReveal;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI _timerLabel;

        [Header("Other UI elements")]
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private RectTransform _topContentContainer;
        [SerializeField] private RectTransform _midContentContainer;
        [SerializeField] private RectTransform _botContentContainer;
        [SerializeField] private RectTransform _hintGroupContainer;

        private const int AppearDelayMilliseconds = 100;
        private const float AppearSlideDuration = 0.25f;

        private IGameSession _session;
        private IBoosterController _boosters;
        private BoardView _boardView;
        private IBoardInput _boardInput;
        private IUIManager _ui;
        private Action _onHomeRequested;
        private Action _onRetryRequested;
        private Action _onEntryPositioned;
        private CanvasGroup _midContentCanvasGroup;
        private Image _hintGroupImage;
        private CancellationTokenSource _timerCts;

        private void Awake()
        {
            if (_topContentContainer != null)
            {
                var pos = _topContentContainer.anchoredPosition;
                pos.y = 100f;
                _topContentContainer.anchoredPosition = pos;
            }

            if (_midContentContainer != null)
            {
                _midContentCanvasGroup = _midContentContainer.GetComponent<CanvasGroup>();
                if (_midContentCanvasGroup != null)
                    _midContentCanvasGroup.alpha = 0f;
            }

            if (_botContentContainer != null)
            {
                var pos = _botContentContainer.anchoredPosition;
                pos.y = -100f;
                _botContentContainer.anchoredPosition = pos;
            }

            if (_hintGroupContainer != null)
                _hintGroupImage = _hintGroupContainer.GetComponent<Image>();
        }

        public void Bind(IGameSession session, IBoosterController boosters, BoardView boardView,
                         IBoardInput boardInput, IUIManager ui,
                         Action onHomeRequested = null, Action onRetryRequested = null,
                         Action onEntryPositioned = null)
        {
            _boardView = boardView;
            _boardInput = boardInput;
            _ui = ui;

            if (_session != null)
                _session.PhaseChanged -= OnPhaseChanged;

            if (_boosters != null)
            {
                _boosters.BoosterStarted -= OnBoosterStarted;
                _boosters.BoosterFinished -= OnBoosterFinished;
            }

            _session = session;
            _boosters = boosters;
            _onHomeRequested = onHomeRequested;
            _onRetryRequested = onRetryRequested;
            _onEntryPositioned = onEntryPositioned;

            _session.PhaseChanged += OnPhaseChanged;

            if (_boosters != null)
            {
                _boosters.BoosterStarted += OnBoosterStarted;
                _boosters.BoosterFinished += OnBoosterFinished;
            }

            if (_heartBar != null) _heartBar.Bind(_session);
            if (_catCounter != null) _catCounter.Bind(_session);

            BindBooster(_boosterHint, OnBoosterHintClicked);
            BindBooster(_boosterReveal, OnBoosterRevealClicked);

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            RefreshLevelLabel();

#if UNITY_EDITOR || CASK_DEBUG
            SetupCheat();
#endif

            SetVisible(true);
            PlayAppearAnimation().Forget();
        }

#if UNITY_EDITOR || CASK_DEBUG
        private void SetupCheat()
        {
            if (_levelLabel == null) return;

            _levelLabel.raycastTarget = true;
            Button button = _levelLabel.GetComponent<Button>();
            if (button == null) button = _levelLabel.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenCheat);
        }

        private void OpenCheat()
        {
            _ui?.PushPopup<CheatPopup>(UIConst.PopupCheat,
                onLoad: (_, popup) => popup.Bind(CheatGoto, CheatWin, CheatLose));
        }

        private void CheatGoto(int level)
        {
            if (GameRuntime.TryGet(out IProfileService profile))
                profile.SetLevel(level + 1);
            _onRetryRequested?.Invoke();
        }

        private void CheatWin() => _session?.CheatFinish(true);

        private void CheatLose() => _session?.CheatFinish(false);
#endif

        private void BindBooster(UIBooster booster, UnityAction onClick)
        {
            if (booster == null) return;

            booster.Bind(_session, _boosters?.Inventory);

            if (booster.Button != null)
            {
                booster.Button.onClick.RemoveAllListeners();
                booster.Button.onClick.AddListener(onClick);
            }

            if (booster.AdsButton != null)
            {
                booster.AdsButton.onClick.RemoveAllListeners();
                booster.AdsButton.onClick.AddListener(onClick);
            }
        }

        private void UseBooster(BoosterType type)
        {
            _boosters?.Use(type);
        }

        private async UniTaskVoid UseBoosterWithReward(BoosterType type, AdPosition adPosition)
        {
            IBoosterInventory inventory = _boosters?.Inventory;
            if (inventory != null && inventory.Count(type) > 0)
            {
                UseBooster(type);
                return;
            }

            if (!FeatureManager.TryGet(out AdFeature adFeature))
                return;

            if (await adFeature.ShowRewardedAsync())
            {
                inventory?.Grant(type, 1);
                UseBooster(type);
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing)
                StartTimerUpdate();
            else
                StopTimerUpdate();
        }

        private void StartTimerUpdate()
        {
            StopTimerUpdate();
            if (_timerLabel == null) return;
            _timerCts = new CancellationTokenSource();
            RunTimerLoop(_timerCts.Token).Forget();
        }

        private void StopTimerUpdate()
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }

        private async UniTaskVoid RunTimerLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _session != null)
            {
                float elapsed = _session.ElapsedTime;
                int minutes = (int)(elapsed / 60f);
                int seconds = (int)(elapsed % 60f);
                _timerLabel.text = $"{minutes:D2}:{seconds:D2}";
                await UniTask.Delay(200, cancellationToken: ct, ignoreTimeScale: true);
            }
        }

        private void RefreshLevelLabel()
        {
            if (_levelLabel != null && _session != null)
                _levelLabel.text = $"Level {_session.Level.Id}";
        }

        private async UniTask PlayAppearAnimation()
        {
            await UniTask.Delay(AppearDelayMilliseconds);
            const float duration = AppearSlideDuration;

            var topMotion = LMotion.Create(_topContentContainer.anchoredPosition, new Vector2(_topContentContainer.anchoredPosition.x, -100f), duration)
                .BindToAnchoredPosition(_topContentContainer);

            var botMotion = LMotion.Create(_botContentContainer.anchoredPosition, new Vector2(_botContentContainer.anchoredPosition.x, 500f), duration)
                .BindToAnchoredPosition(_botContentContainer);

            await UniTask.WhenAll(topMotion.ToUniTask(), botMotion.ToUniTask());

            _boardInput?.SetMode(BoardInputMode.Play);
            _onEntryPositioned?.Invoke();

            if (_midContentCanvasGroup != null)
            {
                await LMotion.Create(0.85f, 1f, duration)
                    .Bind(_midContentCanvasGroup, (a, g) => g.alpha = a)
                    .ToUniTask();
            }

            if (_hintGroupImage != null)
            {
                const float hintBounceDuration = 1f;

                Color color = _hintGroupImage.color;
                color.a = 1f;
                _hintGroupImage.color = color;

                var fadeMotion = LMotion.Create(1f, 0f, hintBounceDuration / 2f)
                    .WithDelay(hintBounceDuration / 2f)
                    .Bind(_hintGroupImage, (a, img) =>
                    {
                        Color c = img.color;
                        c.a = a;
                        img.color = c;
                    });

                Vector3 baseScale = _hintGroupContainer.localScale;
                float stageDuration = hintBounceDuration / 4f;

                var bounceMotion = LSequence.Create()
                    .Append(LMotion.Create(baseScale, baseScale * 1.2f, stageDuration).WithEase(Ease.OutQuad).BindToLocalScale(_hintGroupContainer))
                    .Append(LMotion.Create(baseScale * 1.2f, baseScale * 0.95f, stageDuration).WithEase(Ease.InOutQuad).BindToLocalScale(_hintGroupContainer))
                    .Append(LMotion.Create(baseScale * 0.95f, baseScale * 1.1f, stageDuration).WithEase(Ease.OutQuad).BindToLocalScale(_hintGroupContainer))
                    .Append(LMotion.Create(baseScale * 1.1f, baseScale, stageDuration).WithEase(Ease.InOutQuad).BindToLocalScale(_hintGroupContainer))
                    .Run();

                await UniTask.WhenAll(fadeMotion.ToUniTask(), bounceMotion.ToUniTask());
            }
        }

        private void OnDestroy()
        {
            StopTimerUpdate();

            if (_session != null)
            {
                _session.PhaseChanged -= OnPhaseChanged;
            }

            if (_boosters != null)
            {
                _boosters.BoosterStarted -= OnBoosterStarted;
                _boosters.BoosterFinished -= OnBoosterFinished;
            }
        }

        private void OnBoosterStarted(BoosterType type)
        {
            if (type == BoosterType.Hint)
                SetVisible(false);
        }

        private void OnBoosterFinished(BoosterResult result)
        {
            if (result.Type == BoosterType.Hint)
                SetVisible(true);
        }

        private void OnBoosterHintClicked() => UseBoosterWithReward(BoosterType.Hint, AdPosition.Hint).Forget();

        private void OnBoosterRevealClicked() => UseBoosterWithReward(BoosterType.Reveal, AdPosition.Locate).Forget();

        private void OnSettingsButtonClicked()
        {
            if (_ui == null) return;
            OpenSettingsAsync().Forget();
        }

        private async UniTaskVoid OpenSettingsAsync()
        {
            PopupSettings popup = null;
            await _ui.PushPopupAsync<PopupSettings>(UIConst.PopupIngameSettings, onLoad: (_, p) => popup = p);
            popup?.Setup(
                onClose: () => _ui.PopPopupAsync().Forget(),
                onRetry: OnButtonRetryClicked);
        }

        private void OnButtonRetryClicked()
        {
            if (_ui == null)
            {
                _onRetryRequested?.Invoke();
                return;
            }
            CloseSettingsThenRetryAsync().Forget();
        }

        private async UniTaskVoid CloseSettingsThenRetryAsync()
        {
            await _ui.PopPopupAsync();
            _onRetryRequested?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _onHomeRequested?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup();
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
        }

        private CanvasGroup GetOrAddCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = gameObject.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            return _canvasGroup;
        }
    }
}
