using System;
using CaskFramework.Core;
using CaskFramework.Haptic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class PersistentHintStep : TutorialStep
    {
        private readonly string _messageTop;
        private readonly string _messageBot;

        private TutorialManager _manager;
        private PopupTutorialHint _popup;
        private Action _onComplete;
        private bool _completed;

        public PersistentHintStep(string messageTop, string messageBot)
        {
            _messageTop = messageTop;
            _messageBot = messageBot;
        }

        public override string Message => _messageTop;

        public override void Begin(TutorialManager manager, Action onComplete)
        {
            _manager = manager;
            _onComplete = onComplete;
            _completed = false;

            manager.Session.Ended += OnGameEnded;

            PopupTutorialHint activePopup = manager.ActivePopup;
            if (activePopup != null)
            {
                manager.ActivePopup = null;
                _popup = activePopup;
                activePopup.Show(_messageTop, _messageBot, null);
                activePopup.SetBotButtonCallback(OnHintButtonClicked);
                return;
            }

            ShowPopupAsync(manager).Forget();
        }

        public override void End(TutorialManager manager)
        {
            _completed = true;
            manager.Session.Ended -= OnGameEnded;
            CleanupPopup();
            ClosePopupAsync(manager).Forget();
        }

        private async UniTaskVoid ShowPopupAsync(TutorialManager manager)
        {
            PopupTutorialHint popup = null;
            await manager.Ui.PushPopupAsync<PopupTutorialHint>(UIConst.PopupTutorialHint, onLoad: (_, p) => popup = p);
            if (popup == null) return;

            if (_completed)
            {
                await manager.Ui.PopPopupAsync();
                return;
            }

            _popup = popup;
            popup.Show(_messageTop, _messageBot, null);
            popup.SetBotButtonCallback(OnHintButtonClicked);
        }

        private void OnHintButtonClicked()
        {
            if (_completed) return;

            HintResult hint = _manager.Session.GetHint();
            if (hint == null) return;

            ApplyHint(_manager.Session, hint);
            GameRuntime.Get<IHapticService>()?.Play(HapticType.Success);
        }

        private static void ApplyHint(IGameSession session, HintResult hint)
        {
            if (hint.RemoveMarkCell.HasValue)
                session.SetHint(hint.RemoveMarkCell.Value.Row, hint.RemoveMarkCell.Value.Col, false);

            foreach (var (row, col) in hint.ExcludeCells)
                session.SetHint(row, col, true);

            if (hint.PlaceCell.HasValue)
                session.Reveal(hint.PlaceCell.Value.Row, hint.PlaceCell.Value.Col);
        }

        private void OnGameEnded(GameResult result)
        {
            if (_completed) return;
            _completed = true;
            _manager.Session.Ended -= OnGameEnded;
            CompleteAsync().Forget();
        }

        private async UniTaskVoid CompleteAsync()
        {
            CleanupPopup();
            await ClosePopupAsync(_manager);

            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }

        private void CleanupPopup()
        {
            if (_popup != null)
                _popup.SetBotButtonCallback(null);
        }

        private async UniTask ClosePopupAsync(TutorialManager manager)
        {
            if (_popup == null) return;
            _popup = null;
            await manager.Ui.PopPopupAsync();
        }
    }
}
