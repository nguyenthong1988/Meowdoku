using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;

public class ViewSplashScreen : Page
{
    [SerializeField] private Image _slider;
    [SerializeField] private TMPro.TextMeshProUGUI _loadingText;

    [SerializeField] private RectTransform _viewRect;
    private float _progress = 0;
    private float _nextProgress = 0;
    private bool _canStartLoading = false;
    private float _speedMultiplier = 1;
    private Action _completeAction = null;

    private void UpdateUI()
    {
        if (!ReferenceEquals(_slider, null))
        {
            _slider.fillAmount = _progress;
        }

        UpdateLoadingText();
    }

    public override async UniTask Initialize(Memory<object> args)
    {
        UpdateUI();
        await UniTask.CompletedTask;
    }

    public override void DidPushEnter(Memory<object> args)
    {
        _canStartLoading = true;
        UpdateLoadingText();
    }
    
    public void SetCompleteAction(Action completeAction)
    {
        if (_progress == 1)
        {
            completeAction?.Invoke();
        }
        else
        {
            _completeAction = completeAction;
        }
    }

    public void SetPercentage(float percentage, float speedMultiplier = 1f)
    {
        if (_progress > percentage)
        {
            _progress = percentage;
        }

        _nextProgress = percentage;
        _speedMultiplier = speedMultiplier;
        UpdateUI();
    }

    public override void DidPopEnter(System.Memory<object> args)
    {
        if (_progress < _nextProgress)
        {
            _progress = _nextProgress;
            UpdateUI();
        }
    }
    void Update()
    {
        if (_canStartLoading)
        {
            if (_progress < _nextProgress)
            {
                _progress += Time.deltaTime / _speedMultiplier;
                if (_progress >= _nextProgress)
                {
                    _progress = _nextProgress;
                    if (_nextProgress >= 1f)
                    {
                        _completeAction?.Invoke();
                        _canStartLoading = false;
                    }
                }
                UpdateUI();
            }
        }
    }

    void UpdateLoadingText()
    {
        _loadingText.SetText($"Loading... {Mathf.FloorToInt(_progress * 100)}%");
    }
}
