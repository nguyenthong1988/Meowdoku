using System;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class PopupConfirmStep : TutorialStep
    {
        private readonly string _messageTop;

        private TutorialManager _manager;
        private Action _onComplete;
        private PopupTutorialHint _popup;
        private bool _completed;

        public PopupConfirmStep(string messageTop)
        {
            _messageTop = messageTop;
        }

        public override string Message => _messageTop;

        public override void Begin(TutorialManager manager, Action onComplete)
        {
            _manager = manager;
            _onComplete = onComplete;
            _completed = false;

            manager.Board.SetOverlay(false);
            manager.Input.SetMode(BoardInputMode.Locked);

            ShowPopupAsync(manager).Forget();
        }

        public override void End(TutorialManager manager)
        {
            _completed = true;
            RestoreInputMode(manager);
            ClosePopupAsync(manager).Forget();
        }

        private void RestoreInputMode(TutorialManager manager)
        {
            manager.Input.SetMode(manager.Session.Phase == GamePhase.Playing ? BoardInputMode.Play : BoardInputMode.Locked);
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
            popup.Show(_messageTop, null, OnConfirmed);
        }

        private async UniTask ClosePopupAsync(TutorialManager manager)
        {
            if (_popup == null) return;
            _popup = null;
            await manager.Ui.PopPopupAsync();
        }

        private void OnConfirmed()
        {
            if (_completed) return;
            _completed = true;
            CompleteAsync().Forget();
        }

        private async UniTaskVoid CompleteAsync()
        {
            RestoreInputMode(_manager);
            await ClosePopupAsync(_manager);

            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }
    }
}
