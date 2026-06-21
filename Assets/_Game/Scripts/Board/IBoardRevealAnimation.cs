using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Board
{

    public interface IBoardRevealAnimation
    {
        UniTask PlayAsync(IReadOnlyList<CellView> cells, BoardLayout layout, CancellationToken ct = default);
    }
}
