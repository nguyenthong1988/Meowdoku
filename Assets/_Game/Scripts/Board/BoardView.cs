using System.Collections.Generic;
using System.Threading;
using CaskFramework.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{

    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _cellRoot;
        [SerializeField] private string _cellAddress = "CellView";
        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _cellSpacing = 0.1f;
        [SerializeField] private SpriteRenderer _cellBackground;

        [SerializeField] private float _padding = 0.5f;
        [SerializeField] private BoardRevealConfig _revealConfig = new BoardRevealConfig();
        [SerializeField] private GameObject _overlayObject;

        private CellViewPool _pool;
        private IBoardRevealAnimation _reveal;
        private IGameSession _session;

        private readonly List<CellView> _cells = new List<CellView>();
        private CellView[,] _grid;

        public BoardLayout Layout { get; private set; }

        public Camera Camera => _camera;

        public CellView GetCell(int row, int col)
        {
            if (_grid == null) return null;
            if (row < 0 || row >= _grid.GetLength(0) || col < 0 || col >= _grid.GetLength(1)) return null;
            return _grid[row, col];
        }

        public void Configure(IAssetManager assets)
        {
            _pool = new CellViewPool(assets, _cellAddress);
            _reveal = new ScatterRevealAnimation(_revealConfig);
        }

        public UniTask PreloadAsync() =>
            _pool != null ? _pool.PreloadAsync(CellParent.gameObject) : UniTask.CompletedTask;

        public async UniTask BuildAsync(LevelData level)
        {
            Clear();
            int size = level.Size;
            Layout = BoardLayout.Fit(size, _camera, _padding);
            _grid = new CellView[size, size];

            if (!_pool.IsReady)
                await _pool.PreloadAsync(CellParent.gameObject);

            List<CellView> cells = _pool.Take(size * size, _cellRoot);
            if (cells == null) return;

            if (_cellBackground != null)
            {
                float boardSpan = Layout.Size * Layout.CellSize;
                _cellBackground.size = new Vector2(boardSpan + 0.75f * _padding, boardSpan + 0.75f * _padding);
                float cx = Layout.Origin.x + (Layout.Size - 1) * 0.5f * Layout.CellSize;
                float cy = Layout.Origin.y - (Layout.Size - 1) * 0.5f * Layout.CellSize;
                _cellBackground.transform.position = new Vector3(cx, cy, _cellBackground.transform.position.z);
            }

            int i = 0;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (i >= cells.Count) break;
                    CellView cell = cells[i++];
                    CellData data = level.GetCell(r, c);

                    cell.transform.position = Layout.CellToWorld(r, c);
                    cell.SetCell(r, c, data.ColorIndex, Layout.CellSize);
                    _cells.Add(cell);
                    _grid[r, c] = cell;
                }
            }
        }

        private Transform CellParent => _cellRoot != null ? _cellRoot : transform;

        public void SetVisible(bool visible) => CellParent.gameObject.SetActive(visible);

        public void SetOverlay(bool enabled) { if (_overlayObject != null) _overlayObject.SetActive(enabled); }

        public void SetHintCellsSortingLayer(string layerName)
        {
            foreach (CellView cell in _cells)
                if (cell.CurrentMark == PlayerMark.Hint)
                    cell.SetSortingLayer(layerName);
        }

        public void ClearBoard()
        {
            UnbindRendering();
            Clear();
            SetVisible(false);
        }

        public void PrepareReveal() => _reveal?.Prepare(_cells);

        public UniTask PlayRevealAsync(CancellationToken ct = default) =>
            _reveal != null ? _reveal.PlayAsync(_cells, Layout, ct) : UniTask.CompletedTask;

        public void BindRendering(IGameSession session)
        {
            _session = session;
            _session.CellChanged += OnCellChanged;
            _session.MoveRejected += OnMoveRejected;
        }

        public void UnbindRendering()
        {
            if (_session != null)
            {
                _session.CellChanged -= OnCellChanged;
                _session.MoveRejected -= OnMoveRejected;
            }
            _session = null;
        }

        private void OnCellChanged(CellChange change)
        {
            CellView cell = GetCell(change.Row, change.Col);
            if (cell == null) return;
            cell.SetMark(change.To);
            if (change.To == PlayerMark.Character) cell.PlayPlace();
        }

        private void OnMoveRejected(MoveOutcome outcome, int row, int col)
        {
            GetCell(row, col)?.PlayShake();
        }

        public void Clear()
        {
            _pool?.ReturnAll();
            _cells.Clear();
            _grid = null;
        }

        private void OnDestroy() => UnbindRendering();
    }
}
