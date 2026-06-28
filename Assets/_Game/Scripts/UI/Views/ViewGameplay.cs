

using UnityScreenNavigator.Runtime.Core.Page;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CaskFramework.Core;
using CaskFramework.UI;

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

        private IGameSession _session;
        private IBoosterController _boosters;
        private BoardView _boardView;
        private Action _onHomeRequested;
        private Action _onRetryRequested;

        public void Bind(IGameSession session, IBoosterController boosters, BoardView boardView, Action onHomeRequested = null, Action onRetryRequested = null)
        {
            _boardView = boardView;
            if (_session != null)
                _session.PhaseChanged -= OnPhaseChanged;

            _session = session;
            _boosters = boosters;
            _onHomeRequested = onHomeRequested;
            _onRetryRequested = onRetryRequested;

            _session.PhaseChanged += OnPhaseChanged;

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
        }

        private UniTask UseBoosterAsync(BoosterType type) =>
            _boosters != null ? _boosters.UseAsync(type) : UniTask.CompletedTask;

        private void OnPhaseChanged(GamePhase phase)
        {

        }

        private void RefreshLevelLabel()
        {
            if (_levelLabel != null && _session != null)
                _levelLabel.text = $"Level {_session.Level.Id}";
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnBoosterHintClicked() => RunBoosterHintAsync().Forget();

        private async UniTaskVoid RunBoosterHintAsync()
        {
            var ui = GameRuntime.Get<IUIManager>();

            if (_boardView != null) _boardView.SetOverlay(true);
            SetVisible(false);

            await UseBoosterAsync(BoosterType.Hint);
            if (_boardView != null) _boardView.SetHintCellsSortingLayer("UI");

            PopupBoosterHint popup = null;
            await ui.PushPopupAsync<PopupBoosterHint>(UIConst.PopupBoosterHint, onLoad: (_, p) => popup = p);

            if (popup != null)
                await popup.WaitForConfirmAsync();

            await ui.PopPopupAsync();

            if (_boardView != null) _boardView.SetHintCellsSortingLayer("Gameplay");
            if (_boardView != null) _boardView.SetOverlay(false);
            SetVisible(true);
        }

        private void OnBoosterRevealClicked()
        {
            UseBoosterAsync(BoosterType.Reveal).Forget();
        }

        private void OnSettingsButtonClicked()
        {
            var ui = GameRuntime.Get<IUIManager>();
            ui.PushPopup<PopupSettings>(UIConst.PopupIngameSettings, onLoad: (_, p) =>
                p.Setup(
                    onClose: () => ui.PopPopup(),
                    onRetry: OnButtonRetryClicked
                ));
        }

        private void OnButtonRetryClicked()
        {
            GameRuntime.Get<IUIManager>().PopPopup();
            _onRetryRequested?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _onHomeRequested?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
