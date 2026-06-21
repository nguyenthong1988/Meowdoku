using Cast.Game.Booster;
using Cast.Game.Gameplay;
using UnityScreenNavigator.Runtime.Core.Page;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game.UI
{

    public sealed class ViewGameplay : Page
    {
        [Header("Hearts / labels")]
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Transform _heartsRoot;

        [Header("Action buttons")]
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;

        [Header("Booster buttons")]
        [SerializeField] private Button _boosterHint;
        [SerializeField] private Button _boosterRevealCell;
        [SerializeField] private Button _boosterAddHeart;

        private IGameSession _session;
        private IBoosterService _boosters;

        public void Bind(IGameSession session, IBoosterService boosters)
        {
            _session = session;
            _boosters = boosters;

            _session.HeartsChanged += OnHeartsChanged;
            _session.PhaseChanged += OnPhaseChanged;

            if (_hintButton != null) _hintButton.onClick.AddListener(() => _session.Hint());
            if (_undoButton != null) _undoButton.onClick.AddListener(() => _session.Undo());
            if (_restartButton != null) _restartButton.onClick.AddListener(() => _session.Restart());

            if (_boosterHint != null) _boosterHint.onClick.AddListener(() => UseBooster(BoosterType.Hint));
            if (_boosterRevealCell != null) _boosterRevealCell.onClick.AddListener(() => UseBooster(BoosterType.RevealCell));
            if (_boosterAddHeart != null) _boosterAddHeart.onClick.AddListener(() => UseBooster(BoosterType.AddHeart));

            RefreshHearts();
            RefreshLevelLabel();
        }

        private void UseBooster(BoosterType type)
        {
            
            _boosters?.UseAsync(type).Forget();
        }

        private void OnHeartsChanged(int hearts) => RefreshHearts();

        private void OnPhaseChanged(GamePhase phase)
        {
            
        }

        private void RefreshHearts()
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
                _session.HeartsChanged -= OnHeartsChanged;
                _session.PhaseChanged -= OnPhaseChanged;
            }
        }
    }
}
