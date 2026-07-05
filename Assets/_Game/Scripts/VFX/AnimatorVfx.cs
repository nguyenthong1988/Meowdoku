using UnityEngine;

namespace Cast.Game
{
    public sealed class AnimatorVfx : VfxInstance
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _stateName;

        private int _stateHash;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            _stateHash = Animator.StringToHash(_stateName);
        }

        public override void Play()
        {
            if (_animator == null) return;
            _animator.enabled = true;
            _animator.Play(_stateHash, 0, 0f);
            _animator.Update(0f);
        }

        public override void Stop()
        {
            if (_animator == null) return;
            _animator.Rebind();
            _animator.Update(0f);
            _animator.enabled = false;
        }

        public override bool IsAlive
        {
            get
            {
                if (_animator == null || !_animator.enabled) return false;
                if (_animator.IsInTransition(0)) return true;
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                return state.shortNameHash == _stateHash && state.normalizedTime < 1f;
            }
        }
    }
}
