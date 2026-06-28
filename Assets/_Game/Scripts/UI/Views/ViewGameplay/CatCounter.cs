
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{
    public sealed class CatCounter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _counterText;

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
            _counterText.text = $"<color=#436E35>{found}</color>/<color=#695458>{Total}</color>";
        }

        private void Unbind()
        {
            if (_session != null)
                _session.CellChanged -= OnCellChanged;
        }

        private void OnDestroy() => Unbind();
    }
}
