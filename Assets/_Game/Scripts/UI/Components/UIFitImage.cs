using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIFitImage : MonoBehaviour
{
    RectTransform _rectTransform;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        Fit();
    }

    void Fit()
    {
        var canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect;
        float canvasWidth = canvasRect.width;
        float canvasHeight = canvasRect.height;

        float size = canvasHeight > canvasWidth ? canvasHeight : canvasWidth;

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }
}