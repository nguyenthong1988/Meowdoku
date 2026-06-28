
using UnityScreenNavigator.Runtime.Core.Modal;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Cast.Game
{
    public sealed class PopupWin : Modal
    {
        [SerializeField] private TextMeshProUGUI _kudoText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _nextButton;

        private UniTaskCompletionSource<WinChoice> _choice;
        private CanvasGroup _kudoCanvasGroup, _nextButtonCanvasGroup;
        private readonly string[] _kudoTexts = new string[] { "Good!", "Great!", "Awesome!", "Perfect!", "Excellent!" };

        public override UniTask Initialize(Memory<object> args)
        {
            _kudoCanvasGroup = _kudoText.GetComponent<CanvasGroup>();
            _nextButtonCanvasGroup = _nextButton.GetComponent<CanvasGroup>();
            _kudoCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.interactable = false;
            return base.Initialize(args);
        }

        public UniTask<WinChoice> WaitForChoiceAsync(GameResult result, int currentLevelId)
        {
            _levelText.text = $"LEVEL {currentLevelId + 1}";

            _choice = new UniTaskCompletionSource<WinChoice>();

            _nextButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(() => _choice.TrySetResult(WinChoice.Next));

            RunKudoAnimationAsync().Forget();

            return _choice.Task;
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

            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            _nextButton.transform.localScale = Vector3.one * 0.35f;

            var btnScaleMotion = LMotion.Create(Vector3.one * 0.35f, Vector3.one, 0.25f)
                .Bind(_nextButton.transform, (s, t) => t.localScale = s);

            var btnAlphaMotion = LMotion.Create(0.35f, 1f, 0.25f)
                .Bind(_nextButtonCanvasGroup, (a, g) => g.alpha = a);

            await UniTask.WhenAll(btnScaleMotion.ToUniTask(), btnAlphaMotion.ToUniTask());

            _nextButtonCanvasGroup.interactable = true;
        }

        private void OnDestroy() => _choice?.TrySetResult(WinChoice.Next);
    }
}
