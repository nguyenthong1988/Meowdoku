using System.Threading;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public interface IBooster
    {
        BoosterType Type { get; }
        bool RequiresTarget { get; }

        bool CanUse(IGameSession session);

        UniTask<BoosterResult> UseAsync(BoosterContext ctx, CancellationToken ct);
    }
}
