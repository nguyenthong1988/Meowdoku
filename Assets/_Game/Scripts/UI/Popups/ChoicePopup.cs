using System;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public abstract class ChoicePopup<TChoice> : Modal
    {
        private Action<TChoice> _callback;
        private TChoice _fallback;
        private bool _chosen;

        public void SetChoiceCallback(Action<TChoice> callback, TChoice fallbackOnDestroy)
        {
            _callback = callback;
            _fallback = fallbackOnDestroy;
            _chosen = false;
        }

        protected void Choose(TChoice choice)
        {
            if (_chosen) return;
            _chosen = true;
            Action<TChoice> callback = _callback;
            _callback = null;
            callback?.Invoke(choice);
        }

        protected virtual void OnDestroy()
        {
            if (_chosen) return;
            Choose(_fallback);
        }
    }
}
