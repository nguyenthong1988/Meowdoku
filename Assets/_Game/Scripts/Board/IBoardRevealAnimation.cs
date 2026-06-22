using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Board
{

    public interface IBoardRevealAnimation
    {
        void Prepare(IReadOnlyList<CellView> cells);
        UniTask PlayAsync(IReadOnlyList<CellView> cells, BoardLayout layout, CancellationToken ct = default);
    }
}
