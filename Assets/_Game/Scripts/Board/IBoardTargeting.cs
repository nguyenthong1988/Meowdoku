using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Board
{

    public interface IBoardTargeting
    {
        UniTask<(bool ok, int row, int col)> PickCellAsync(CancellationToken ct = default);
    }
}
