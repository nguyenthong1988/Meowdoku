using System.Collections.Generic;
using System.Threading;
using CaskFramework.Audio;
using CaskFramework.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{

    public sealed class ScatterRevealAnimation : IBoardRevealAnimation
    {
        private readonly BoardRevealConfig _config;

        public ScatterRevealAnimation(BoardRevealConfig config)
        {
            _config = config ?? new BoardRevealConfig();
        }

        public void Prepare(IReadOnlyList<CellView> cells)
        {
            if (cells == null || cells.Count == 0) return;
            foreach (CellView cell in cells)
            {
                Vector3 offset = (Vector3)(Random.insideUnitCircle * _config.ScatterRadius);
                cell.SetHidden(offset, _config.StartScale);
            }
        }

        public async UniTask PlayAsync(IReadOnlyList<CellView> cells, BoardLayout layout, CancellationToken ct = default)
        {
            if (cells == null || cells.Count == 0) return;

            IReadOnlyList<CellView> ordered = Order(cells);

            const float sfxInterval = 0.06f;
            GameRuntime.TryGet(out IAudioManager audio);

            float maxEnd = 0f;
            for (int i = 0; i < ordered.Count; i++)
            {
                CellView cell = ordered[i];
                float delay = i * _config.StaggerStep;
                Vector3 target = layout.CellToWorld(cell.Row, cell.Col);
                cell.AnimateIn(target, _config.CellDuration, delay);
                maxEnd = Mathf.Max(maxEnd, delay + _config.CellDuration);
            }

            if (audio != null && ordered.Count > 0)
            {
                float elapsed = 0f;
                float nextSfxTime = 0f;
                while (elapsed < maxEnd)
                {
                    if (ct.IsCancellationRequested) return;
                    if (elapsed >= nextSfxTime)
                    {
                        audio.PlaySfx(AudioNames.SFX_SPREAD_TILE);
                        nextSfxTime = elapsed + sfxInterval;
                    }
                    await UniTask.NextFrame(ct);
                    elapsed += Time.deltaTime;
                }
            }
            else if (maxEnd > 0f)
            {
                await UniTask.Delay(Mathf.CeilToInt(maxEnd * 1000f), cancellationToken: ct);
            }
        }

        private IReadOnlyList<CellView> Order(IReadOnlyList<CellView> cells)
        {
            if (_config.Order != RevealOrder.Random) return cells;

            var copy = new List<CellView>(cells);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
