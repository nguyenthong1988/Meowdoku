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
                        case PointerGesture.DoubleTap: _session.PlaceCat(g.Row, g.Col); break;
                        case PointerGesture.Tap: _session.RemoveCat(g.Row, g.Col); break;
                        case PointerGesture.LongPress: _session.ToggleMark(g.Row, g.Col); break;
                    }
                    return;
            }
        }
    }
}
