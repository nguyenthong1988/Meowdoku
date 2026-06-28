using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UISafeZone : MonoBehaviour
{
    public enum ApplyMode
    {
        Anchor,
        Offset
    }

    public enum SimDevice
    {
        None,
        IPhoneX,
        IPhoneXsMax,
        Pixel3XlLsl,
        Pixel3XlLsr
    }

    private const float FALLBACK_DPI = 160f;
    private const float MIN_TABLET_DIAGONAL_INCHES = 7f;
    private const float MAX_TABLET_ASPECT_RATIO = 2f;

    private static bool s_isTabletChecked;
    private static bool s_isTablet;

    public static SimDevice Sim { get; set; } = SimDevice.None;

    public static bool IsTablet
    {
        get
        {
            if (!s_isTabletChecked)
                DetectTablet();
            return s_isTablet;
        }
    }

    [SerializeField] private ApplyMode _applyMode = ApplyMode.Anchor;
    [SerializeField] private bool _safeTop = true;
    [SerializeField] private bool _safeBottom = true;
    [SerializeField] private float _topOffset;
    [SerializeField] private float _bottomOffset;

    private RectTransform _panel;
    private CanvasScaler _canvasScaler;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private ScreenOrientation _lastOrientation;

    private readonly Rect[] _simIPhoneX =
    {
            new Rect(0f, 102f / 2436f, 1f, 2202f / 2436f),
            new Rect(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f)
        };

    private readonly Rect[] _simIPhoneXsMax =
    {
            new Rect(0f, 102f / 2688f, 1f, 2454f / 2688f),
            new Rect(132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f)
        };

    private readonly Rect[] _simPixel3XlLsl =
    {
            new Rect(0f, 0f, 1f, 2789f / 2960f),
            new Rect(0f, 0f, 2789f / 2960f, 1f)
        };

    private readonly Rect[] _simPixel3XlLsr =
    {
            new Rect(0f, 0f, 1f, 2789f / 2960f),
            new Rect(171f / 2960f, 0f, 2789f / 2960f, 1f)
        };

    public RectTransform Panel
    {
        get
        {
            if (_panel == null)
                _panel = GetComponent<RectTransform>();
            return _panel;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DetectTablet()
    {
        if (s_isTabletChecked) return;

        float dpi = Screen.dpi > 0 ? Screen.dpi : FALLBACK_DPI;
        float diagonalPixels = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float diagonalInches = diagonalPixels / dpi;

        float longerSide = Mathf.Max(Screen.width, Screen.height);
        float shorterSide = Mathf.Min(Screen.width, Screen.height);
        float aspectRatio = shorterSide > 0 ? longerSide / shorterSide : 999f;

        s_isTablet = diagonalInches >= MIN_TABLET_DIAGONAL_INCHES && aspectRatio <= MAX_TABLET_ASPECT_RATIO;
        s_isTabletChecked = true;
    }

    private void Awake()
    {
        if (Panel == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_applyMode == ApplyMode.Offset)
            _canvasScaler = GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();

        ApplySafeArea();
    }

    private void Update()
    {
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        Rect safeArea = GetSafeArea();

        if (safeArea == _lastSafeArea
            && Screen.width == _lastScreenSize.x
            && Screen.height == _lastScreenSize.y
            && Screen.orientation == _lastOrientation)
            return;

        _lastScreenSize.x = Screen.width;
        _lastScreenSize.y = Screen.height;
        _lastOrientation = Screen.orientation;
        _lastSafeArea = safeArea;

        switch (_applyMode)
        {
            case ApplyMode.Anchor:
                ApplyByAnchor(safeArea);
                break;
            case ApplyMode.Offset:
                ApplyByOffset(safeArea);
                break;
        }
    }

    private Rect GetSafeArea()
    {
        Rect safeArea = Screen.safeArea;

        if (!Application.isEditor || Sim == SimDevice.None)
            return safeArea;

        bool isPortrait = Screen.height > Screen.width;

        Rect[] simData = Sim switch
        {
            SimDevice.IPhoneX => _simIPhoneX,
            SimDevice.IPhoneXsMax => _simIPhoneXsMax,
            SimDevice.Pixel3XlLsl => _simPixel3XlLsl,
            SimDevice.Pixel3XlLsr => _simPixel3XlLsr,
            _ => null
        };

        if (simData == null)
            return safeArea;

        Rect normalized = isPortrait ? simData[0] : simData[1];
        return new Rect(
            Screen.width * normalized.x,
            Screen.height * normalized.y,
            Screen.width * normalized.width,
            Screen.height * normalized.height
        );
    }

    private void ApplyByAnchor(Rect safeArea)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        float anchorMinY = _safeBottom
            ? (safeArea.y + _bottomOffset) / Screen.height
            : 0f;

        float anchorMaxY = _safeTop
            ? 1f - (Screen.height - safeArea.y - safeArea.height + _topOffset) / Screen.height
            : 1f;

        if (anchorMinY >= 0 && anchorMaxY >= 0)
        {
            Panel.anchorMin = new Vector2(Panel.anchorMin.x, anchorMinY);
            Panel.anchorMax = new Vector2(Panel.anchorMax.x, anchorMaxY);
        }
    }

    private void ApplyByOffset(Rect safeArea)
    {
        if (_canvasScaler == null)
        {
            _canvasScaler = GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
            if (_canvasScaler == null) return;
        }

        float referenceHeight = _canvasScaler.referenceResolution.y;

        if (_safeBottom)
        {
            float bottomPixels = safeArea.y + _bottomOffset;
            float bottomUnits = referenceHeight * (bottomPixels / Screen.height);
            Panel.offsetMin = new Vector2(Panel.offsetMin.x, bottomUnits);
        }

        if (_safeTop)
        {
            float topPixels = Screen.height - safeArea.y - safeArea.height + _topOffset;
            float topUnits = referenceHeight * (topPixels / Screen.height);
            Panel.offsetMax = new Vector2(Panel.offsetMax.x, -topUnits);
        }
    }
}