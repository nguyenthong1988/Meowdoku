using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererFit : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        FitToScreen();
    }

    public void FitToScreen()
    {
        if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float screenHeightWorld = cam.orthographicSize * 2f;
        float screenWidthWorld = screenHeightWorld * cam.aspect;

        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

        float scaleX = screenWidthWorld / spriteSize.x;
        float scaleY = screenHeightWorld / spriteSize.y;

        float scale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
