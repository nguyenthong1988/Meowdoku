using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISliderToggle : MonoBehaviour, IPointerClickHandler
{
    public enum OnSide { Left, Right }

    [SerializeField] private OnSide _onSide = OnSide.Right;
    [SerializeField] private RectTransform _handler;
    [SerializeField] private Image _targetImage;
    [SerializeField] private Color _onColor = Color.white;
    [SerializeField] private Color _offColor = Color.gray;
    [SerializeField] private float _leftPadding = 4f;
    [SerializeField] private float _rightPadding = 4f;
    [SerializeField] private float _slideDuration = 0.2f;
    [SerializeField] private UnityEvent<bool> _onValueChanged;

    private bool _isOn;
    private MotionHandle _positionHandle;
    private MotionHandle _colorHandle;

    public bool IsOn => _isOn;

    public UnityEvent<bool> onValueChanged => _onValueChanged;

    public void Init(bool isOn)
    {
        _isOn = isOn;
        ApplyHandlerPositionImmediate();
        ApplyColorImmediate();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SetValue(!_isOn);
    }

    public void SetValue(bool isOn)
    {
        _isOn = isOn;
        AnimateHandler();
        AnimateColor();
        _onValueChanged?.Invoke(_isOn);
    }

    private float HandlerTargetX()
    {
        if (_targetImage == null) return 0f;
        float halfWidth = _targetImage.rectTransform.rect.width * 0.5f;
        bool goRight = _isOn == (_onSide == OnSide.Right);
        return goRight ? halfWidth - _rightPadding : -halfWidth + _leftPadding;
    }

    private void ApplyHandlerPositionImmediate()
    {
        if (_handler == null) return;
        var pos = _handler.anchoredPosition;
        pos.x = HandlerTargetX();
        _handler.anchoredPosition = pos;
    }

    private void ApplyColorImmediate()
    {
        if (_targetImage == null) return;
        _targetImage.color = _isOn ? _onColor : _offColor;
    }

    private void AnimateHandler()
    {
        if (_handler == null) return;
        if (_positionHandle.IsActive()) _positionHandle.Cancel();

        Vector2 from = _handler.anchoredPosition;
        Vector2 to = new(HandlerTargetX(), from.y);
        _positionHandle = LMotion.Create(from, to, _slideDuration)
            .WithEase(Ease.OutQuad)
            .BindToAnchoredPosition(_handler)
            .AddTo(gameObject);
    }

    private void AnimateColor()
    {
        if (_targetImage == null) return;
        if (_colorHandle.IsActive()) _colorHandle.Cancel();

        Color targetColor = _isOn ? _onColor : _offColor;
        _colorHandle = LMotion.Create(_targetImage.color, targetColor, _slideDuration)
            .WithEase(Ease.OutQuad)
            .BindToColor(_targetImage)
            .AddTo(gameObject);
    }
}
