

using UnityScreenNavigator.Runtime.Core.Page;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cast.Game
{

    public sealed class ViewGameplay : Page
    {
        [Header("Hearts / labels")]
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private HeartBar _heartBar;
        [SerializeField] private CatCounter _catCounter;

        [Header("Booster buttons")]
        [SerializeField] private UIBooster _boosterHint;
        [SerializeField] private UIBooster _boosterReveal;

        private IGameSession _session;
        private IBoosterController _boosters;

        public void Bind(IGameSession session, IBoosterController boosters)
        {
            _session = session;
            _boosters = boosters;

            _session.PhaseChanged += OnPhaseChanged;

            if (_heartBar != null) _heartBar.Bind(_session);
            if (_catCounter != null) _catCounter.Bind(_session);

            if (_boosterHint != null)
            {
                _boosterHint.Bind(_session);
                _boosterHint.Button.onClick.RemoveAllListeners();
                _boosterHint.Button.onClick.AddListener(OnBoosterHintClicked);
            }
            
            if (_boosterReveal != null)
            {
                _boosterReveal.Bind(_session);
                _boosterReveal.Button.onClick.RemoveAllListeners();
                _boosterReveal.Button.onClick.AddListener(OnBoosterRevealClicked);
            }

            RefreshLevelLabel();
        }

        private void UseBooster(BoosterType type)
        {
            _boosters?.UseAsync(type).Forget();
        }

        private void OnPhaseChanged(GamePhase phase)
        {

        }

        private void RefreshLevelLabel()
        {
            if (_levelLabel != null && _session != null)
                _levelLabel.text = $"Level {_session.Level.Id}";
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnBoosterHintClicked()
        {
           //_session.Hint();
           UseBooster(BoosterType.Hint);
        }

        private void OnBoosterRevealClicked()
        {
            //_session.Reveal();
            UseBooster(BoosterType.Reveal);
        }
    }
}
