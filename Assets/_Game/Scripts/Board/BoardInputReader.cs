using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cast.Game
{

    public sealed class BoardInputReader : MonoBehaviour
    {
        [SerializeField] private BoardInputConfig _config = new BoardInputConfig();

        public event Action<CellGesture> Gesture;

        /// <summary>
        /// When true, a Tap is held back for DoubleTapWindow seconds so a second tap
        /// on the same cell becomes a single clean DoubleTap (no Tap fired first).
        /// When false, Tap fires immediately on release (legacy behavior, used when
        /// double taps are meaningless in the current mode, e.g. targeting).
        /// </summary>
        public bool DeferTap { get; set; }

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

        // Deferred-tap state (DeferTap == true).
        private bool _hasPendingTap;
        private bool _doubleTapCandidate;
        private int _pendingRow = -1, _pendingCol = -1;
        private float _pendingTime;

        // Legacy double-tap tracking (DeferTap == false).
        private int _lastTapRow = -1, _lastTapCol = -1;
        private float _lastTapTime = -10f;

        public void Bind(BoardLayout layout, Camera cam)
        {
            _layout = layout;
            _camera = cam;
        }

        public void SetEnabled(bool on)
        {
            _enabled = on;
            if (on) return;

            // Drop transient state so nothing leaks into the next input mode.
            _pressing = false;
            _dragging = false;
            _dragStarted = false;
            _hasPendingTap = false;
            _doubleTapCandidate = false;
            _lastTapTime = -10f;
        }

        private void Update()
        {
            if (!_enabled || _camera == null) return;

            // A held-back tap becomes a real Tap once the double-tap window closes.
            if (_hasPendingTap && Time.unscaledTime - _pendingTime > _config.DoubleTapWindow)
                FlushPendingTap();

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
            Emit(screen, PointerGesture.PressDown);

            _pressing = true;
            _longFired = false;
            _dragging = false;
            _dragStarted = false;
            _dragRow = -1;
            _dragCol = -1;
            _downScreen = screen;
            _downTime = Time.unscaledTime;

            _doubleTapCandidate = false;
            if (!_hasPendingTap) return;

            bool sameCell = !IsOverUI()
                            && ScreenToCell(screen, out int row, out int col)
                            && row == _pendingRow && col == _pendingCol;
            bool inWindow = Time.unscaledTime - _pendingTime <= _config.DoubleTapWindow;

            if (sameCell && inWindow)
            {
                // Second press landed on the same cell in time: this press is a
                // double-tap attempt. Swallow the pending Tap so it never fires.
                _hasPendingTap = false;
                _doubleTapCandidate = true;
            }
            else
            {
                // Pressed elsewhere (or too late): the first tap was a real Tap.
                FlushPendingTap();
            }
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

            if (DeferTap)
            {
                if (_doubleTapCandidate && row == _pendingRow && col == _pendingCol)
                {
                    _doubleTapCandidate = false;
                    Gesture?.Invoke(new CellGesture(row, col, PointerGesture.DoubleTap));
                    return;
                }

                // Hold this tap back; it only fires as a Tap if no second tap
                // follows within the double-tap window (see Update).
                _doubleTapCandidate = false;
                _hasPendingTap = true;
                _pendingRow = row;
                _pendingCol = col;
                _pendingTime = Time.unscaledTime;
                return;
            }

            // Immediate mode: Tap fires right away, DoubleTap fires on top of it.
            bool sameLastCell = row == _lastTapRow && col == _lastTapCol;
            bool inTapWindow = Time.unscaledTime - _lastTapTime <= _config.DoubleTapWindow;

            if (sameLastCell && inTapWindow)
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

        private void FlushPendingTap()
        {
            if (!_hasPendingTap) return;
            _hasPendingTap = false;
            Gesture?.Invoke(new CellGesture(_pendingRow, _pendingCol, PointerGesture.Tap));
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
