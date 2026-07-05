using UnityEngine;

namespace Cast.Game
{
    public sealed class ParticleVfx : VfxInstance
    {
        [SerializeField] private ParticleSystem _particle;

        private void Awake()
        {
            if (_particle == null)
                _particle = GetComponentInChildren<ParticleSystem>();
        }

        public override void Play()
        {
            if (_particle != null)
                _particle.Play(true);
        }

        public override void Stop()
        {
            if (_particle != null)
                _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public override bool IsAlive => _particle != null && _particle.IsAlive(true);
    }
}
