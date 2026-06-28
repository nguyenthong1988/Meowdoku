using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CaskFramework.Haptic;

public class UIButton : Button
{
    [SerializeField] string sfxName = "UI_Button_Click";
    [SerializeField] bool hasHaptic = true;
    [SerializeField] private HapticType hapticType = HapticType.Button;
    [SerializeField] private bool clickAnimation = true;
    [SerializeField] private Vector2 pressedScale = new Vector2(0.9f, 0.9f);
    [SerializeField] private float pressDownDuration = 0.125f;
    [SerializeField] private float pressUpDuration = 0.125f;
    [SerializeField] private Ease pressDownEase = Ease.OutQuad;
    [SerializeField] private Ease pressUpEase = Ease.OutQuad;

    private Vector3 _originalScale;
    [SerializeField] private Transform targetTransform;
    private MotionHandle _motionHandle;
    private CancellationTokenSource _cts;

    protected override void Awake()
    {
        base.Awake();
        if (targetTransform == null)
            targetTransform = transform;
        _originalScale = targetTransform.localScale;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (!IsInteractable() || !clickAnimation) return;

        CancelMotion();
        var pressedTarget = new Vector3(pressedScale.x, pressedScale.y, _originalScale.z);
        _motionHandle = LMotion.Create(targetTransform.localScale, pressedTarget, pressDownDuration)
            .WithEase(pressDownEase)
            .WithOnComplete(() =>
            {
                _motionHandle = LMotion.Create(targetTransform.localScale, _originalScale, pressUpDuration)
                    .WithEase(pressUpEase)
                    .BindToLocalScale(targetTransform)
                    .AddTo(gameObject);
            })
            .BindToLocalScale(targetTransform)
            .AddTo(gameObject);
    }

    private bool IsPointerOver(PointerEventData eventData)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelMotion();
        if (targetTransform != null)
            targetTransform.localScale = _originalScale;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CancelMotion();
        CancelToken();
    }

    private void CancelMotion()
    {
        if (_motionHandle.IsActive())
            _motionHandle.Cancel();
    }

    public CancellationToken GetCancellationToken()
    {
        if (_cts == null || _cts.IsCancellationRequested)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }
        return _cts.Token;
    }

    public void CancelToken()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        OnButtonPressed();
        base.OnSubmit(eventData);
    }
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnButtonPressed();
        }
        
        base.OnPointerClick(eventData);
        
    }

    protected virtual void OnButtonPressed()
    {
        if (!IsActive() || !IsInteractable()) return;
        
        // if (ReferenceEquals(SoundHelper.Instance, null)) return;
        // SoundHelper.Instance.PlaySfx(sfxName);
        // if (hasHaptic)
        //     SoundHelper.Instance.TriggerHaptic(hapticType);
    }
}