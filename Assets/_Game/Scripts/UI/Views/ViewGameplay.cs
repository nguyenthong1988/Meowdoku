

using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

        [Header("Other UI elements")]
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private RectTransform _topContentContainer;
        [SerializeField] private RectTransform _midContentContainer;
        [SerializeField] private RectTransform _botContentContainer;

        private IGameSession _session;
        private IBoosterController _boosters;
        private BoardView _boardView;
        private IBoardInput _boardInput;
        private IUIManager _ui;
        private Action _onHomeRequested;
        private Action _onRetryRequested;
        private Action _onEntryPositioned;
        private CanvasGroup _midContentCanvasGroup;
        private CanvasGroup _canvasGroup;

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
        }

        public void Bind(IGameSession session, IBoosterController boosters, BoardView boardView, IBoardInput boardInput, IUIManager ui, Action onHomeRequested = null, Action onRetryRequested = null, Action onEntryPositioned = null)
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

            if (_boosterHint != null)
            {
                _boosterHint.Bind(_session);
                _boosterHint.Button.onClick.RemoveAllListeners();
                _boosterHint.Button.onClick.AddListener(OnBoosterHintClicked);
            }
            
            if (_boosterReveal != null)
            {
                _boosterReveal.Bind(_session);
                _boosterReveal.Button.onClick.RemoveAllListeners();
                _boosterReveal.Button.onClick.AddListener(OnBoosterRevealClicked);
            }

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

            PlayAppearAnimation().Forget();
        }

        private void UseBooster(BoosterType type)
        {
            _boosters?.Use(type);
        }

        private void OnPhaseChanged(GamePhase phase)
        {

        }

        private void RefreshLevelLabel()
        {
            if (_levelLabel != null && _session != null)
                _levelLabel.text = $"Level {_session.Level.Id}";
        }

        private async UniTask PlayAppearAnimation()
        {
            const float duration = 0.25f;

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
        }

        private void OnDestroy()
        {
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

        private void OnBoosterHintClicked() => UseBooster(BoosterType.Hint);

        private void OnBoosterRevealClicked() => UseBooster(BoosterType.Reveal);

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
