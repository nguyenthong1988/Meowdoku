using System;
using System.Collections.Generic;
using System.Threading;
using CaskFramework.Profile;

using Cast.Game;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class GameSession : IGameSession
    {
        private readonly GameSessionConfig _config;
        private IProfileService _profile;
        private readonly Stack<MoveRecord> _undo = new Stack<MoveRecord>();

        private GamePhase _phase = GamePhase.Loading;
        private int _hearts;
        private int _hintsRemaining;
        private int _moves;
        private float _startTime;

        public GameSession(GameSessionConfig config, IProfileService profile)
        {
            _config = config ?? new GameSessionConfig();
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public LevelData Level { get; private set; }
        public BoardState Board { get; private set; }
        public GamePhase Phase => _phase;
        public int Hearts => _hearts;
        public int HeartsMax => _config.HeartsMax;
        public int HintsRemaining => _hintsRemaining;
        public int Moves => _moves;
        public float ElapsedTime =>
            _phase == GamePhase.Playing
                ? UnityEngine.Time.realtimeSinceStartup - _startTime
                : 0f;

        public event Action<GamePhase> PhaseChanged;
        public event Action<CellChange> CellChanged;
        public event Action<int> HeartsChanged;
        public event Action<MoveOutcome, int, int> MoveRejected;
        public event Action<GameResult> Ended;

        public void Setup(LevelData level)
        {
            Level = level;
            Board = new BoardState(level);
            _undo.Clear();
            _hearts = _config.HeartsMax;
            _hintsRemaining = _config.HintsMax;
            _moves = 0;
            SetPhase(GamePhase.Loading);
        }

        public void Begin()
        {
            _startTime = UnityEngine.Time.realtimeSinceStartup;
            SetPhase(GamePhase.Playing);
        }

        public void CheatFinish(bool won)
        {
            Finish(won);
        }

        public MoveOutcome Reveal(int row, int col)
        {
            if (_phase != GamePhase.Playing) return MoveOutcome.NoOp;

            PlayerMark current = Board.GetMark(row, col);
            if (current == PlayerMark.Character || current == PlayerMark.Wrong) return MoveOutcome.NoOp;

            if (Board.Level.GetCell(row, col).HasCat)
            {
                ApplyMark(row, col, PlayerMark.Character, costHeart: false);
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
            if (current == PlayerMark.Character || current == PlayerMark.Wrong) return MoveOutcome.NoOp;

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
            PhaseChanged = null;
            CellChanged = null;
            HeartsChanged = null;
            MoveRejected = null;
            Ended = null;
        }

        public bool Revive()
        {
            if (_phase != GamePhase.Result || _hearts > 0) return false;
            _hearts = _config.HeartsMax;
            HeartsChanged?.Invoke(_hearts);
            SetPhase(GamePhase.Playing);
            return true;
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
            if (_hearts == 0)
            {
                AdNetworkTracker.Track(AdNetworkEvents.energy_depleted);
                Finish(won: false);
            }
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

        public bool UndoWrong()
        {
            if (_phase != GamePhase.Playing) return false;

            var temp = new Stack<MoveRecord>();
            while (_undo.Count > 0)
            {
                MoveRecord rec = _undo.Pop();
                if (rec.CostHeart)
                {
                    Board.SetMark(rec.Row, rec.Col, rec.From);
                    CellChanged?.Invoke(new CellChange(rec.Row, rec.Col, rec.To, rec.From));
                    GainHeart();

                    while (temp.Count > 0) _undo.Push(temp.Pop());
                    return true;
                }
                temp.Push(rec);
            }

            while (temp.Count > 0) _undo.Push(temp.Pop());
            return false;
        }

        public bool RandomReveal()
        {
            if (_phase != GamePhase.Playing) return false;

            var unrevealed = new List<CharacterPlacement>();
            IReadOnlyList<CharacterPlacement> solution = Level.Solution;
            for (int i = 0; i < solution.Count; i++)
            {
                CharacterPlacement p = solution[i];
                if (Board.GetMark(p.Row, p.Col) != PlayerMark.Character)
                    unrevealed.Add(p);
            }

            if (unrevealed.Count == 0) return false;

            CharacterPlacement pick = unrevealed[UnityEngine.Random.Range(0, unrevealed.Count)];
            Reveal(pick.Row, pick.Col);
            return true;
        }

        public bool ClearAllHints()
        {
            if (_phase != GamePhase.Playing) return false;

            bool cleared = false;
            int size = Board.Size;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (Board.GetMark(r, c) != PlayerMark.Hint) continue;
                    Board.SetMark(r, c, PlayerMark.None);
                    CellChanged?.Invoke(new CellChange(r, c, PlayerMark.Hint, PlayerMark.None));
                    cleared = true;
                }
            }
            return cleared;
        }

        public HintResult GetHint()
        {
            if (_phase != GamePhase.Playing) return null;
            return HintCalculator.Calculate(Board);
        }

        public bool IsBoosterUnlocked(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hint:
                    return _profile.ProgressLevel >= _config.HintUnlockLevel;
                case BoosterType.Reveal:
                    return _profile.ProgressLevel >= _config.RevealUnlockLevel;
                default:
                    return false;
            }
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
            Ended?.Invoke(new GameResult(won, _hearts, hintsUsed, elapsed, _moves));
        }
    }
}
