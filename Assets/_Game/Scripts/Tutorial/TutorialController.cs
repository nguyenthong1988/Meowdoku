using TMPro;
using UnityEngine;

namespace Cast.Game
{
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private RectTransform _handPointer;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;

        private void Awake()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void ShowStep(string message, Vector3 worldTarget)
        {
            if (_root != null) _root.SetActive(true);
            if (_message != null) _message.text = message;
            PositionHand(worldTarget);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void PositionHand(Vector3 worldTarget)
        {
            if (_handPointer == null) return;

            Camera cam = _worldCamera != null ? _worldCamera : Camera.main;
            if (cam == null) return;

            Vector3 screenPoint = cam.WorldToScreenPoint(worldTarget);

            if (_canvas == null)
            {
                _handPointer.position = screenPoint;
                return;
            }

            RectTransform canvasRect = _canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                _handPointer.position = screenPoint;
                return;
            }

            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
                _handPointer.anchoredPosition = localPoint;
        }
    }
}
