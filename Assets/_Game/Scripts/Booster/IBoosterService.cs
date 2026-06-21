using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public interface IBoosterService
    {
        bool IsBusy { get; }

        UniTask<BoosterResult> UseAsync(BoosterType type, CancellationToken ct = default);

        event Action<BoosterType> BoosterStarted;
        event Action<BoosterResult> BoosterFinished;
    }
}
