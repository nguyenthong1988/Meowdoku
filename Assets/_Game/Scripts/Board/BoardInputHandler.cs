using System;
using System.Collections.Generic;
using System.Threading;
using CaskFramework.Core;
using CaskFramework.Haptic;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class BoardInputHandler : IBoardInput, IBoardTargeting
    {
        private readonly BoardInputReader _reader;
        private readonly BoardView _board;

        private IGameSession _session;
        private UniTaskCompletionSource<(bool, int, int)> _targetSource;

        private IHapticService _haptic;

        private bool _paintModeSet;
        private bool _paintHint;

        private readonly HashSet<(int, int)> _hintPreviewCells = new HashSet<(int, int)>();
        private Action<int, int> _hintPreviewTap;
        private Action<int, int> _hintPreviewDoubleTap;
        private Action<int, int> _hintPreviewDrag;

        public BoardInputMode Mode { get; private set; } = BoardInputMode.Locked;

        private IHapticService Haptic => _haptic ??= GameRuntime.Get<IHapticService>();

        public BoardInputHandler(BoardInputReader reader, BoardView board)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public void Bind(IGameSession session)
        {
            Unbind();
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
            _hintPreviewCells.Clear();
            _hintPreviewTap = null;
            _hintPreviewDoubleTap = null;
            _hintPreviewDrag = null;
        }

        public void SetMode(BoardInputMode mode)
        {
            Mode = mode;
            _reader.SetEnabled(mode != BoardInputMode.Locked);

            // Defer single taps only where a double tap has a distinct meaning,
            // so a double tap fires cleanly without a Tap (hint flash) first.
            // In targeting (and other modes) taps stay instant.
            _reader.DeferTap = mode == BoardInputMode.Play
                               || (mode == BoardInputMode.HintPreview && _hintPreviewDoubleTap != null);
        }

        public void BeginHintPreview(IReadOnlyCollection<(int Row, int Col)> allowedCells, Action<int, int> onTap, Action<int, int> onDoubleTap, Action<int, int> onDrag = null)
        {
            _hintPreviewCells.Clear();
            if (allowedCells != null)
                foreach (var cell in allowedCells)
                    _hintPreviewCells.Add((cell.Row, cell.Col));
            _hintPreviewTap = onTap;
            _hintPreviewDoubleTap = onDoubleTap;
            _hintPreviewDrag = onDrag;
            SetMode(BoardInputMode.HintPreview);
        }

        public void EndHintPreview()
        {
            _hintPreviewCells.Clear();
            _hintPreviewTap = null;
            _hintPreviewDoubleTap = null;
            _hintPreviewDrag = null;
            if (Mode == BoardInputMode.HintPreview)
                SetMode(_session != null && _session.Phase == GamePhase.Playing ? BoardInputMode.Play : BoardInputMode.Locked);
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
            if (Mode == BoardInputMode.HintPreview) return;
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

                case BoardInputMode.HintPreview:
                    switch (g.Gesture)
                    {
                        case PointerGesture.DoubleTap:
                            if (_hintPreviewCells.Contains((g.Row, g.Col)))
                            {
                                _hintPreviewDoubleTap?.Invoke(g.Row, g.Col);
                                Haptic?.Play(HapticType.Success);
                            }
                            break;
                        case PointerGesture.Tap:
                            if (_hintPreviewCells.Contains((g.Row, g.Col)))
                            {
                                _hintPreviewTap?.Invoke(g.Row, g.Col);
                                Haptic?.Play(HapticType.Light);
                            }
                            break;
                        case PointerGesture.DragStart:
                        case PointerGesture.DragMove:
                            if (_hintPreviewCells.Contains((g.Row, g.Col)))
                            {
                                _hintPreviewDrag?.Invoke(g.Row, g.Col);
                                Haptic?.Play(HapticType.Soft);
                            }
                            break;
                    }
                    return;

                case BoardInputMode.Play:
                    if (_session == null) return;
                    switch (g.Gesture)
                    {
                        case PointerGesture.DoubleTap: PlayRevealHaptic(_session.Reveal(g.Row, g.Col)); break;
                        case PointerGesture.Tap: PlayHintHaptic(_session.ToggleHint(g.Row, g.Col), fromTap: true); break;
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
            if (mark == PlayerMark.Character || mark == PlayerMark.Wrong) return;

            if (!_paintModeSet)
            {
                _paintModeSet = true;
                _paintHint = mark == PlayerMark.None;
            }

            PlayHintHaptic(_session.SetHint(row, col, _paintHint), fromTap: false);
        }

        private void PlayRevealHaptic(MoveOutcome outcome)
        {
            switch (outcome)
            {
                case MoveOutcome.Revealed: Haptic?.Play(HapticType.Success); break;
                case MoveOutcome.Wrong: Haptic?.Play(HapticType.Error); break;
            }
        }

        private void PlayHintHaptic(MoveOutcome outcome, bool fromTap)
        {
            if (outcome != MoveOutcome.Hinted && outcome != MoveOutcome.Unhinted) return;
            Haptic?.Play(fromTap ? HapticType.Light : HapticType.Soft);
        }
    }
}
