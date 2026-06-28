using System.Collections;
using UnityEngine;

public enum SnapAnchor
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererSnap : MonoBehaviour
{
    [SerializeField] SnapAnchor snapAnchor = SnapAnchor.BottomLeft;
    [SerializeField] float paddingX;
    [SerializeField] float paddingY;

    private void Start()
    {
        StartCoroutine(WaitForCameraAndSnap());
    }

    private IEnumerator WaitForCameraAndSnap()
    {
        yield return new WaitUntil(() => Camera.main != null);
        Snap();
    }

    public void Snap()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError($"[SpriteRendererSnap] No camera tagged 'MainCamera' found.", this);
            return;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector2 pivot = sr.sprite.pivot / sr.sprite.pixelsPerUnit;
        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector2 scaledSize = new Vector2(spriteSize.x * transform.localScale.x, spriteSize.y * transform.localScale.y);

        float pivotOffsetX = (pivot.x - spriteSize.x * 0.5f) * transform.localScale.x;
        float pivotOffsetY = (pivot.y - spriteSize.y * 0.5f) * transform.localScale.y;

        float left   = -halfW + scaledSize.x * 0.5f - pivotOffsetX + paddingX;
        float right  =  halfW - scaledSize.x * 0.5f - pivotOffsetX - paddingX;
        float top    =  halfH - scaledSize.y * 0.5f - pivotOffsetY - paddingY;
        float bottom = -halfH + scaledSize.y * 0.5f - pivotOffsetY + paddingY;

        Vector3 pos = transform.position;

        switch (snapAnchor)
        {
            case SnapAnchor.Left:
                pos.x = left;
                break;
            case SnapAnchor.Right:
                pos.x = right;
                break;
            case SnapAnchor.Top:
                pos.y = top;
                break;
            case SnapAnchor.Bottom:
                pos.y = bottom;
                break;
            case SnapAnchor.TopLeft:
                pos.x = left;
                pos.y = top;
                break;
            case SnapAnchor.TopRight:
                pos.x = right;
                pos.y = top;
                break;
            case SnapAnchor.BottomLeft:
                pos.x = left;
                pos.y = bottom;
                break;
            case SnapAnchor.BottomRight:
                pos.x = right;
                pos.y = bottom;
                break;
        }

        transform.position = pos;
    }
}
