using System;
using System.Threading;
using Cast.Game.Data;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Gameplay
{

    public interface IGameSession
    {
        LevelData Level { get; }
        BoardState Board { get; }
        GamePhase Phase { get; }
        int Hearts { get; }
        int HeartsMax { get; }
        int HintsRemaining { get; }

        event Action<GamePhase> PhaseChanged;
        event Action<CellChange> CellChanged;
        event Action<int> HeartsChanged;
        event Action<MoveOutcome, int, int> MoveRejected; 

        void Setup(LevelData level);

        void Begin();

        UniTask<GameResult> PlayToEndAsync(CancellationToken ct = default);

        MoveOutcome PlaceCat(int row, int col);
        MoveOutcome RemoveCat(int row, int col);
        MoveOutcome ToggleMark(int row, int col);
        bool Undo();
        bool Hint();
        void Restart();

        bool AddHeart();

        void Dispose();
    }
}
