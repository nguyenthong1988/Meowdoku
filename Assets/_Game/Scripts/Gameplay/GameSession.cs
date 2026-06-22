using System;
using System.Collections.Generic;
using System.Threading;
using Cast.Game.Data;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Gameplay
{

    public sealed class GameSession : IGameSession
    {
        private readonly IHintProvider _hints;
        private readonly GameSessionConfig _config;
        private readonly Stack<MoveRecord> _undo = new Stack<MoveRecord>();

        private UniTaskCompletionSource<GameResult> _endSource;
        private GamePhase _phase = GamePhase.Loading;
        private int _hearts;
        private int _hintsRemaining;
        private int _moves;
        private float _startTime;

        public GameSession(IHintProvider hints, GameSessionConfig config)
        {
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _config = config ?? new GameSessionConfig();
        }

        public LevelData Level { get; private set; }
        public BoardState Board { get; private set; }
        public GamePhase Phase => _phase;
        public int Hearts => _hearts;
        public int HeartsMax => _config.HeartsMax;
        public int HintsRemaining => _hintsRemaining;

        public event Action<GamePhase> PhaseChanged;
        public event Action<CellChange> CellChanged;
        public event Action<int> HeartsChanged;
        public event Action<MoveOutcome, int, int> MoveRejected;

        public void Setup(LevelData level)
        {
            Level = level;
            Board = new BoardState(level);
            _undo.Clear();
            _hearts = _config.HeartsMax;
            _hintsRemaining = _config.HintsMax;
            _moves = 0;
            _endSource = new UniTaskCompletionSource<GameResult>();
            SetPhase(GamePhase.Loading);
        }

        public void Begin()
        {
            _startTime = UnityEngine.Time.realtimeSinceStartup;
            SetPhase(GamePhase.Playing);
        }

        public UniTask<GameResult> PlayToEndAsync(CancellationToken ct = default)
        {
            return _endSource.Task;
        }

        public MoveOutcome Reveal(int row, int col)
        {
            if (_phase != GamePhase.Playing) return MoveOutcome.NoOp;

            PlayerMark current = Board.GetMark(row, col);
            if (current == PlayerMark.Cat || current == PlayerMark.Wrong) return MoveOutcome.NoOp;

            if (Board.Level.GetCell(row, col).HasCat)
            {
                ApplyMark(row, col, PlayerMark.Cat, costHeart: false);
                if (Board.IsSolved()) Finish(won: true);
                return MoveOutcome.Revealed;
            }

            ApplyMark(row, col, PlayerMark.Wrong, costHeart: true);
            MoveRejected?.Invoke(MoveOutcome.Wrong, row, col);
            LoseHeart();
            return MoveOutcome.Wrong;
        }

        public MoveOutcome ToggleHint(int row, int col)
        {
            if (_phase != GamePhase.Playing) return MoveOutcome.NoOp;
            return SetHint(row, col, Board.GetMark(row, col) != PlayerMark.Hint);
        }

        public MoveOutcome SetHint(int row, int col, bool on)
        {
            if (_phase != GamePhase.Playing) return MoveOutcome.NoOp;

            PlayerMark current = Board.GetMark(row, col);
            if (current == PlayerMark.Cat || current == PlayerMark.Wrong) return MoveOutcome.NoOp;

            if (on)
            {
                if (current == PlayerMark.Hint) return MoveOutcome.NoOp;
                ApplyMark(row, col, PlayerMark.Hint, costHeart: false);
                return MoveOutcome.Hinted;
            }

            if (current == PlayerMark.None) return MoveOutcome.NoOp;
            ApplyMark(row, col, PlayerMark.None, costHeart: false);
            return MoveOutcome.Unhinted;
        }

        public bool Undo()
        {
            if (_phase != GamePhase.Playing || _undo.Count == 0) return false;

            MoveRecord rec = _undo.Pop();
            Board.SetMark(rec.Row, rec.Col, rec.From);
            CellChanged?.Invoke(new CellChange(rec.Row, rec.Col, rec.To, rec.From));
            if (rec.CostHeart) GainHeart();
            return true;
        }

        public bool Hint()
        {
            if (_phase != GamePhase.Playing || _hintsRemaining <= 0) return false;
            if (!_hints.TryGetHint(Board, out int row, out int col)) return false;

            _hintsRemaining--;
            ApplyMark(row, col, PlayerMark.Cat, costHeart: false);
            if (Board.IsSolved()) Finish(won: true);
            return true;
        }

        public void Restart()
        {
            if (Board == null || _phase != GamePhase.Playing) return;

            int size = Board.Size;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    PlayerMark from = Board.GetMark(r, c);
                    if (from == PlayerMark.None) continue;
                    Board.SetMark(r, c, PlayerMark.None);
                    CellChanged?.Invoke(new CellChange(r, c, from, PlayerMark.None));
                }
            }
            _undo.Clear();
        }

        public void Dispose()
        {
            _endSource?.TrySetCanceled();
            PhaseChanged = null;
            CellChanged = null;
            HeartsChanged = null;
            MoveRejected = null;
        }

        private void ApplyMark(int row, int col, PlayerMark mark, bool costHeart)
        {
            PlayerMark from = Board.GetMark(row, col);
            Board.SetMark(row, col, mark);
            _undo.Push(new MoveRecord(row, col, from, mark, costHeart));
            _moves++;
            CellChanged?.Invoke(new CellChange(row, col, from, mark));
        }

        private void LoseHeart()
        {
            _hearts = Math.Max(0, _hearts - 1);
            HeartsChanged?.Invoke(_hearts);
            if (_hearts == 0) Finish(won: false);
        }

        private void GainHeart()
        {
            if (_hearts >= _config.HeartsMax) return;
            _hearts++;
            HeartsChanged?.Invoke(_hearts);
        }

        public bool AddHeart()
        {
            if (_phase != GamePhase.Playing || _hearts >= _config.HeartsMax) return false;
            GainHeart();
            return true;
        }

        private void SetPhase(GamePhase phase)
        {
            if (_phase == phase) return;
            _phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void Finish(bool won)
        {
            SetPhase(GamePhase.Result);
            float elapsed = UnityEngine.Time.realtimeSinceStartup - _startTime;
            int hintsUsed = _config.HintsMax - _hintsRemaining;
            _endSource?.TrySetResult(new GameResult(won, _hearts, hintsUsed, elapsed, _moves));
        }
    }
}
