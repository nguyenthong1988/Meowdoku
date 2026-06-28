using System;
using System.Threading;

using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public interface IBooster
    {
        BoosterType Type { get; }
        bool RequiresTarget { get; }

        bool CanUse(IGameSession session);

        UniTask<BoosterResult> UseAsync(BoosterController ctx, CancellationToken ct);
        //bool IsBusy { get; }

        // UniTask<BoosterResult> UseAsync(BoosterType type, CancellationToken ct = default);

        // event Action<BoosterType> BoosterStarted;
        // event Action<BoosterResult> BoosterFinished;
    }
}
