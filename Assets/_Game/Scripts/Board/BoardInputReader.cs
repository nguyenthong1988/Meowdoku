using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cast.Game.Board
{

    public sealed class BoardInputReader : MonoBehaviour
    {
        [SerializeField] private BoardInputConfig _config = new BoardInputConfig();

        public event Action<CellGesture> Gesture;

        private BoardLayout _layout;
        private Camera _camera;
        private bool _enabled;

        private bool _pressing;
        private bool _longFired;
        private Vector2 _downScreen;
        private float _downTime;

        private bool _dragging;
        private bool _dragStarted;
        private int _dragRow = -1, _dragCol = -1;

        private int _lastTapRow = -1, _lastTapCol = -1;
        private float _lastTapTime = -10f;

        public void Bind(BoardLayout layout, Camera cam)
        {
            _layout = layout;
            _camera = cam;
        }

        public void SetEnabled(bool on) => _enabled = on;

        private void Update()
        {
            if (!_enabled || _camera == null) return;

            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            bool isDown = pointer.press.isPressed;
            Vector2 screen = pointer.position.ReadValue();

            if (isDown && !_pressing)
                OnPressDown(screen);
            else if (isDown && _pressing)
                OnPressHold(screen);
            else if (!isDown && _pressing)
                OnPressUp(screen);
        }

        private void OnPressDown(Vector2 screen)
        {
            _pressing = true;
            _longFired = false;
            _dragging = false;
            _dragStarted = false;
            _dragRow = -1;
            _dragCol = -1;
            _downScreen = screen;
            _downTime = Time.unscaledTime;
        }

        private void OnPressHold(Vector2 screen)
        {
            if (_longFired) return;

            if (_dragging)
            {
                PumpDrag(screen);
                return;
            }

            if (MovedBeyondSlop(screen))
            {
                _dragging = true;
                PumpDrag(_downScreen);   // first cell = where the finger went down
                PumpDrag(screen);
                return;
            }

            if (Time.unscaledTime - _downTime >= _config.LongPressDuration)
            {
                _longFired = true;
                Emit(screen, PointerGesture.LongPress);
            }
        }

        private void OnPressUp(Vector2 screen)
        {
            _pressing = false;
            if (_dragging)
            {
                _dragging = false;
                _dragStarted = false;
                Gesture?.Invoke(new CellGesture(_dragRow, _dragCol, PointerGesture.DragEnd));
                return;
            }
            if (_longFired) return;
            if (MovedBeyondSlop(screen)) return;
            if (Time.unscaledTime - _downTime > _config.TapMaxDuration) return;

            if (IsOverUI()) return;
            if (!ScreenToCell(screen, out int row, out int col)) return;

            bool sameCell = row == _lastTapRow && col == _lastTapCol;
            bool inWindow = Time.unscaledTime - _lastTapTime <= _config.DoubleTapWindow;

            if (sameCell && inWindow)
            {
                _lastTapTime = -10f; 
                Gesture?.Invoke(new CellGesture(row, col, PointerGesture.DoubleTap));
                return;
            }

            _lastTapRow = row;
            _lastTapCol = col;
            _lastTapTime = Time.unscaledTime;
            Gesture?.Invoke(new CellGesture(row, col, PointerGesture.Tap));
        }

        private void Emit(Vector2 screen, PointerGesture gesture)
        {
            if (IsOverUI()) return;
            if (ScreenToCell(screen, out int row, out int col))
                Gesture?.Invoke(new CellGesture(row, col, gesture));
        }

        private void PumpDrag(Vector2 screen)
        {
            if (IsOverUI()) return;
            if (!ScreenToCell(screen, out int row, out int col)) return;
            if (row == _dragRow && col == _dragCol) return;

            _dragRow = row;
            _dragCol = col;
            if (!_dragStarted)
            {
                _dragStarted = true;
                Gesture?.Invoke(new CellGesture(row, col, PointerGesture.DragStart));
            }
            else
            {
                Gesture?.Invoke(new CellGesture(row, col, PointerGesture.DragMove));
            }
        }

        private bool MovedBeyondSlop(Vector2 screen) =>
            (screen - _downScreen).sqrMagnitude > _config.TapSlopPixels * _config.TapSlopPixels;

        private static readonly Plane BoardPlane = new Plane(Vector3.forward, Vector3.zero);

        private bool ScreenToCell(Vector2 screen, out int row, out int col)
        {
            Ray ray = _camera.ScreenPointToRay(screen);
            if (BoardPlane.Raycast(ray, out float enter))
                return _layout.WorldToCell(ray.GetPoint(enter), out row, out col);

            row = -1;
            col = -1;
            return false;
        }

        private static bool IsOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
