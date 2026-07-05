using UnityEngine;

namespace Cast.Game
{
    public abstract class VfxInstance : MonoBehaviour
    {
        public abstract void Play();
        public abstract void Stop();
        public abstract bool IsAlive { get; }
    }
}
