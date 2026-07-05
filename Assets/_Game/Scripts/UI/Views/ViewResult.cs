
using Cysharp.Threading.Tasks;
using LitMotion;
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
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _nextButton;

        private CanvasGroup _kudoCanvasGroup, _nextButtonCanvasGroup;
        private readonly string[] _kudoTexts = new string[] { "Good!", "Great!", "Awesome!", "Perfect!", "Excellent!" };
        private Action<WinChoice> _onChoice;
        private bool _chosen;

        public override UniTask Initialize(Memory<object> args)
        {
            _kudoCanvasGroup = _kudoText.GetComponent<CanvasGroup>();
            _nextButtonCanvasGroup = _nextButton.GetComponent<CanvasGroup>();
            _kudoCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.alpha = 0f;
            _nextButtonCanvasGroup.interactable = false;
            return base.Initialize(args);
        }

        public void Setup(GameResult result, int currentLevelId, Action<WinChoice> onChoice)
        {
            _onChoice = onChoice;
            _chosen = false;
            _levelText.text = $"LEVEL {currentLevelId + 1}";

            _nextButton.onClick.RemoveAllListeners();
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

            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            _nextButton.transform.localScale = Vector3.one * 0.65f;

            var btnScaleMotion = LMotion.Create(Vector3.one * 0.65f, Vector3.one, 0.25f)
                .Bind(_nextButton.transform, (s, t) => t.localScale = s);

            var btnAlphaMotion = LMotion.Create(0.35f, 1f, 0.25f)
                .Bind(_nextButtonCanvasGroup, (a, g) => g.alpha = a);

            await UniTask.WhenAll(btnScaleMotion.ToUniTask(), btnAlphaMotion.ToUniTask());

            _nextButtonCanvasGroup.interactable = true;
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
