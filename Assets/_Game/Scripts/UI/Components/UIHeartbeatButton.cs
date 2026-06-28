using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class UIHeartbeatButton : UIButton
{
    [Header("Heartbeat Animation")]
    [SerializeField] private bool heartbeatEnabled = true;
    [SerializeField] private Transform heartbeatTarget;

    [SerializeField] private Vector2 heartbeatScale = new Vector2(1.15f, 1.15f);
    [SerializeField] private float beatUpDuration = 0.15f;
    [SerializeField] private float beatDownDuration = 0.1f;
    [SerializeField] private float secondBeatUpDuration = 0.12f;
    [SerializeField] private float secondBeatDownDuration = 0.15f;
    [SerializeField] private Vector2 secondBeatScale = new Vector2(1.08f, 1.08f);

    [SerializeField] private Ease beatUpEase = Ease.OutQuad;
    [SerializeField] private Ease beatDownEase = Ease.InQuad;

    [Header("Contract Beat")]
    [SerializeField] private bool useContractBeat;
    [SerializeField] private Vector2 contractScale = new Vector2(0.9f, 0.9f);
    [SerializeField] private float contractDuration = 0.1f;
    [SerializeField] private Ease contractEase = Ease.InQuad;

    [Header("Delay Settings")]
    [SerializeField] private bool useStartDelay;
    [SerializeField] private float startDelayTime = 0.5f;
    [SerializeField] private bool useIntervalDelay = true;
    [SerializeField] private float intervalDelayTime = 0.6f;

    private Vector3 _heartbeatOriginalScale;
    private MotionHandle _heartbeatHandle;
    private CancellationTokenSource _heartbeatCts;
    private bool _isAnimating;

    protected override void Awake()
    {
        base.Awake();
        if (heartbeatTarget == null)
            heartbeatTarget = transform;
        _heartbeatOriginalScale = heartbeatTarget.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (heartbeatEnabled)
            StartHeartbeat();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopHeartbeat();
        if (heartbeatTarget != null)
            heartbeatTarget.localScale = _heartbeatOriginalScale;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopHeartbeat();
    }

    public void StartHeartbeat()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        CancelHeartbeatToken();
        _heartbeatCts = new CancellationTokenSource();

        PlayHeartbeatLoop(_heartbeatCts.Token);
    }

    public void StopHeartbeat()
    {
        _isAnimating = false;
        CancelHeartbeatToken();
        CancelHeartbeatHandle();
    }

    private async void PlayHeartbeatLoop(CancellationToken token)
    {
        try
        {
            if (useStartDelay && startDelayTime > 0f)
                await Awaitable.WaitForSecondsAsync(startDelayTime, token);

            while (!token.IsCancellationRequested && _isAnimating)
            {
                CancelHeartbeatHandle();

                var strongScale = new Vector3(
                    _heartbeatOriginalScale.x * heartbeatScale.x,
                    _heartbeatOriginalScale.y * heartbeatScale.y,
                    _heartbeatOriginalScale.z);
                var softScale = new Vector3(
                    _heartbeatOriginalScale.x * secondBeatScale.x,
                    _heartbeatOriginalScale.y * secondBeatScale.y,
                    _heartbeatOriginalScale.z);

                if (useContractBeat)
                {
                    var shrinkScale = new Vector3(
                        _heartbeatOriginalScale.x * contractScale.x,
                        _heartbeatOriginalScale.y * contractScale.y,
                        _heartbeatOriginalScale.z);

                    _heartbeatHandle = LSequence.Create()
                        .Append(LMotion.Create(_heartbeatOriginalScale, shrinkScale, contractDuration).WithEase(contractEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(shrinkScale, strongScale, beatUpDuration).WithEase(beatUpEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(strongScale, shrinkScale, beatDownDuration).WithEase(beatDownEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(shrinkScale, softScale, secondBeatUpDuration).WithEase(beatUpEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(softScale, _heartbeatOriginalScale, secondBeatDownDuration).WithEase(beatDownEase).BindToLocalScale(heartbeatTarget))
                        .Run();
                }
                else
                {
                    _heartbeatHandle = LSequence.Create()
                        .Append(LMotion.Create(_heartbeatOriginalScale, strongScale, beatUpDuration).WithEase(beatUpEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(strongScale, _heartbeatOriginalScale, beatDownDuration).WithEase(beatDownEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(_heartbeatOriginalScale, softScale, secondBeatUpDuration).WithEase(beatUpEase).BindToLocalScale(heartbeatTarget))
                        .Append(LMotion.Create(softScale, _heartbeatOriginalScale, secondBeatDownDuration).WithEase(beatDownEase).BindToLocalScale(heartbeatTarget))
                        .Run();
                }

                _heartbeatHandle = _heartbeatHandle.AddTo(gameObject);
                await _heartbeatHandle.ToAwaitable(token);

                if (useIntervalDelay && intervalDelayTime > 0f)
                    await Awaitable.WaitForSecondsAsync(intervalDelayTime, token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private void CancelHeartbeatHandle()
    {
        if (_heartbeatHandle.IsActive())
            _heartbeatHandle.Cancel();
    }

    private void CancelHeartbeatToken()
    {
        if (_heartbeatCts != null)
        {
            _heartbeatCts.Cancel();
            _heartbeatCts.Dispose();
            _heartbeatCts = null;
        }
    }

    public void SetHeartbeatEnabled(bool enabled)
    {
        heartbeatEnabled = enabled;
        if (enabled && gameObject.activeInHierarchy)
            StartHeartbeat();
        else
            StopHeartbeat();
    }

    public bool IsHeartbeatEnabled => heartbeatEnabled;
}
