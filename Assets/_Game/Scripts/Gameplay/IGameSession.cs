using System;
using System.Collections.Generic;
using System.Threading;
using Cast.Game;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public interface IGameSession
    {
        LevelData Level { get; }
        BoardState Board { get; }
        GamePhase Phase { get; }
        int Hearts { get; }
        int HeartsMax { get; }
        int HintsRemaining { get; }
        float ElapsedTime { get; }

        event Action<GamePhase> PhaseChanged;
        event Action<CellChange> CellChanged;
        event Action<int> HeartsChanged;
        event Action<MoveOutcome, int, int> MoveRejected;
        event Action<GameResult> Ended;

        void Setup(LevelData level);

        void Begin();

        void CheatFinish(bool won);

        MoveOutcome Reveal(int row, int col);
        MoveOutcome ToggleHint(int row, int col);
        MoveOutcome SetHint(int row, int col, bool on);
        bool Undo();
        void Restart();

        bool AddHeart();
        bool Revive();
        bool UndoWrong();
        bool RandomReveal();
        bool ClearAllHints();
        HintResult GetHint();
        bool IsBoosterUnlocked(BoosterType type);
        void Dispose();
    }
}
