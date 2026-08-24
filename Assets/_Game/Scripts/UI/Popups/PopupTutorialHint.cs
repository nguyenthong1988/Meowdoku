using System;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace Cast.Game
{
    public sealed class PopupTutorialHint : Modal
    {
        private const float ContentFadeFrom = 0.35f;
        private const float ContentFadeTo = 1f;
        private const float ContentScaleFrom = 0.85f;
        private const float ContentScaleTo = 1f;
        private const float ContentAnimDuration = 0.1f;
        private const float ConfirmPulseScale = 0.88f;
        private const float ConfirmPulseDuration = 0.55f;

        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _messageTextTop;
        [SerializeField] private TMP_Text _messageTextBot;
        [SerializeField] private Transform _contentTop;
        [SerializeField] private Transform _contentBot;

        private Action _onConfirm;
        private bool _confirmed;

        private CanvasGroup _contentTopCanvasGroup;
        private CanvasGroup _contentBotCanvasGroup;
        private Button _botButton;
        private MotionHandle _confirmPulseHandle;

        public bool Applied { get; private set; }

        private void Awake()
        {
            _contentTopCanvasGroup = GetOrAddCanvasGroup(_contentTop);
            _contentBotCanvasGroup = GetOrAddCanvasGroup(_contentBot);
            if (_contentBot != null)
                _botButton = _contentBot.GetComponent<Button>();
        }

        public void SetBotButtonCallback(Action onClick)
        {
            if (_botButton == null) return;
            _botButton.onClick.RemoveAllListeners();
            if (onClick != null)
                _botButton.onClick.AddListener(() => onClick());
        }

        public void Show(string messageTop, string messageBot, Action onConfirm)
        {
            if (onConfirm == null)
            {
                StopConfirmPulse();
                _confirmButton.gameObject.SetActive(false);
            }
            else
            {
                _confirmButton.gameObject.SetActive(true);
                SetConfirmCallback(onConfirm);
                StartConfirmPulse();
            }

            if (string.IsNullOrEmpty(messageTop))
            {
                _contentTop.gameObject.SetActive(false);
            }
            else
            {
                _messageTextTop.text = messageTop;
                PlayAppearAnimation(_contentTop, _contentTopCanvasGroup);
            }
            if (string.IsNullOrEmpty(messageBot))
            {
                _contentBot.gameObject.SetActive(false);
            }
            else
            {
                _messageTextBot.text = messageBot;
                PlayAppearAnimation(_contentBot, _contentBotCanvasGroup);
            }
        }

        private void PlayAppearAnimation(Transform content, CanvasGroup canvasGroup)
        {
            content.gameObject.SetActive(true);
            content.localScale = Vector3.one * ContentScaleFrom;
            canvasGroup.alpha = ContentFadeFrom;

            LMotion.Create(ContentFadeFrom, ContentFadeTo, ContentAnimDuration)
                .Bind(canvasGroup, (a, g) => g.alpha = a)
                .AddTo(content.gameObject);

            LMotion.Create(ContentScaleFrom, ContentScaleTo, ContentAnimDuration)
                .Bind(content, (s, t) => t.localScale = Vector3.one * s)
                .AddTo(content.gameObject);
        }

        private void StartConfirmPulse()
        {
            StopConfirmPulse();
            if (_confirmButton == null) return;

            Transform target = _confirmButton.transform;
            target.localScale = Vector3.one;

            _confirmPulseHandle = LMotion.Create(1f, ConfirmPulseScale, ConfirmPulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .Bind(target, (scale, t) => t.localScale = Vector3.one * scale)
                .AddTo(_confirmButton.gameObject);
        }

        private void StopConfirmPulse()
        {
            if (_confirmPulseHandle.IsActive())
                _confirmPulseHandle.Cancel();

            if (_confirmButton != null)
                _confirmButton.transform.localScale = Vector3.one;
        }

        private static CanvasGroup GetOrAddCanvasGroup(Transform target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        public void SetConfirmCallback(Action onConfirm)
        {
            _onConfirm = onConfirm;
            _confirmed = false;
            Applied = false;
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        private void OnConfirmButtonClicked()
        {
            Applied = true;
            StopConfirmPulse();
            Confirm();
        }

        private void Confirm()
        {
            if (_confirmed) return;
            _confirmed = true;
            Action callback = _onConfirm;
            _onConfirm = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            StopConfirmPulse();
            Confirm();
        }
    }
}
