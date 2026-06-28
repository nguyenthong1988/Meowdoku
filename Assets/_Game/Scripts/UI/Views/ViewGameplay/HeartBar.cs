using System.Collections.Generic;

using UnityEngine;

namespace Cast.Game
{
    public sealed class HeartBar : MonoBehaviour
    {
        [SerializeField] private List<HeartIcon> _hearts;

        private IGameSession _session;

        public void Bind(IGameSession session)
        {
            Unbind();
            _session = session;
            if (_session == null) return;

            Build(_session.HeartsMax);
            _session.HeartsChanged += OnHeartsChanged;
            Refresh(_session.Hearts);
        }

        private void Build(int max)
        {
            for (int i = 0; i < _hearts.Count; i++)
                if (_hearts[i] != null)
                    _hearts[i].gameObject.SetActive(i < max);
        }

        private void OnHeartsChanged(int hearts) => Refresh(hearts);

        private void Refresh(int hearts)
        {
            for (int i = 0; i < _hearts.Count; i++)
                if (_hearts[i] != null)
                    _hearts[i].SetFilled(i < hearts);
        }

        private void Unbind()
        {
            if (_session != null)
                _session.HeartsChanged -= OnHeartsChanged;
        }

        private void OnDestroy() => Unbind();
    }
}
