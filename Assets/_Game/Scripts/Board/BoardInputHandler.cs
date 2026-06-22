using System;
using System.Threading;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Board
{

    public sealed class BoardInputHandler : IBoardInputGate, IBoardTargeting
    {
        private readonly BoardInputReader _reader;
        private readonly BoardView _board;

        private IGameSession _session;
        private UniTaskCompletionSource<(bool, int, int)> _targetSource;

        private bool _paintModeSet;
        private bool _paintHint;

        public BoardInputMode Mode { get; private set; } = BoardInputMode.Locked;

        public BoardInputHandler(BoardInputReader reader, BoardView board)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public void Bind(IGameSession session)
        {
            _session = session;
            _reader.Bind(_board.Layout, _board.Camera);
            _reader.Gesture += OnGesture;
            _session.PhaseChanged += OnPhaseChanged;
            SetMode(BoardInputMode.Locked); 
        }

        public void Unbind()
        {
            if (_reader != null) _reader.Gesture -= OnGesture;
            if (_session != null) _session.PhaseChanged -= OnPhaseChanged;
            _session = null;
            _targetSource?.TrySetResult((false, -1, -1));
            _targetSource = null;
        }

        public void SetMode(BoardInputMode mode)
        {
            Mode = mode;
            _reader.SetEnabled(mode != BoardInputMode.Locked);
        }

        public UniTask<(bool ok, int row, int col)> PickCellAsync(CancellationToken ct = default)
        {
            _targetSource = new UniTaskCompletionSource<(bool, int, int)>();
            SetMode(BoardInputMode.Targeting);
            
            return _targetSource.Task;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            
            if (Mode == BoardInputMode.Targeting) return;
            SetMode(phase == GamePhase.Playing ? BoardInputMode.Play : BoardInputMode.Locked);
        }

        private void OnGesture(CellGesture g)
        {
            switch (Mode)
            {
                case BoardInputMode.Locked:
                    return;

                case BoardInputMode.Targeting:
                    if (g.Gesture == PointerGesture.Tap || g.Gesture == PointerGesture.DoubleTap)
                    {
                        _targetSource?.TrySetResult((true, g.Row, g.Col));
                        _targetSource = null;
                        SetMode(BoardInputMode.Play);
                    }
                    return;

                case BoardInputMode.Play:
                    if (_session == null) return;
                    switch (g.Gesture)
                    {
                        case PointerGesture.DoubleTap: _session.Reveal(g.Row, g.Col); break;
                        case PointerGesture.Tap: _session.ToggleHint(g.Row, g.Col); break;
                        case PointerGesture.DragStart: _paintModeSet = false; PaintDrag(g.Row, g.Col); break;
                        case PointerGesture.DragMove: PaintDrag(g.Row, g.Col); break;
                        case PointerGesture.DragEnd: _paintModeSet = false; break;
                    }
                    return;
            }
        }

        // Drag-paint: the first paintable cell decides the direction.
        // Starting on a normal cell paints hints (None -> Hint); starting on a hint
        // cell erases hints (Hint -> None). Revealed cells (Cat/Wrong) are skipped, so a
        // drag begun on one adopts its direction from the first paintable cell it reaches.
        private void PaintDrag(int row, int col)
        {
            if (_session == null) return;

            PlayerMark mark = _session.Board.GetMark(row, col);
            if (mark == PlayerMark.Cat || mark == PlayerMark.Wrong) return;

            if (!_paintModeSet)
            {
                _paintModeSet = true;
                _paintHint = mark == PlayerMark.None;
            }

            _session.SetHint(row, col, _paintHint);
        }
    }
}
