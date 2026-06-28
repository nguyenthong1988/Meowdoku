using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public sealed class PopupBoosterHint : Modal
    {
        [SerializeField] private Button _confirmButton;

        private UniTaskCompletionSource _tcs;

        public UniTask WaitForConfirmAsync()
        {
            _tcs = new UniTaskCompletionSource();
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(() => _tcs.TrySetResult());
            return _tcs.Task;
        }

        private void OnDestroy() => _tcs?.TrySetResult();
    }
}
