using System.Collections.Generic;
using Cast.Game.Gameplay;
using UnityEngine;

namespace Cast.Game
{
    public sealed class HeartBar : MonoBehaviour
    {
        [SerializeField] private HeartIcon _heartPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private List<HeartIcon> _hearts = new List<HeartIcon>();

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
            if (_heartPrefab == null || _container == null) return;

            for (int i = _hearts.Count; i < max; i++)
            {
                var icon = Instantiate(_heartPrefab, _container);
                _hearts.Add(icon);
            }

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
