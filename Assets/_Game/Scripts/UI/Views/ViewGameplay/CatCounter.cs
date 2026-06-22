using Cast.Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{
    public sealed class CatCounter : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _fillBar;

        private IGameSession _session;
        private int _total;

        public int Found => _session?.Board != null ? _session.Board.CatsRevealed : 0;
        public int Total => _total;

        public void Bind(IGameSession session)
        {
            Unbind();
            _session = session;
            if (_session == null) return;

            _total = _session.Level != null ? _session.Level.Solution.Count : 0;
            _session.CellChanged += OnCellChanged;
            Refresh();
        }

        private void OnCellChanged(CellChange change)
        {
            if (change.From == PlayerMark.Cat || change.To == PlayerMark.Cat)
                Refresh();
        }

        private void Refresh()
        {
            int found = Found;
            if (_label != null) _label.text = $"{found}/{_total}";
            if (_fillBar != null) _fillBar.fillAmount = _total > 0 ? (float)found / _total : 0f;
        }

        private void Unbind()
        {
            if (_session != null)
                _session.CellChanged -= OnCellChanged;
        }

        private void OnDestroy() => Unbind();
    }
}
