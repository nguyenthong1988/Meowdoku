using System;

namespace Cast.Game
{
    public abstract class TutorialStep
    {
        public abstract string Message { get; }
        public abstract void Begin(TutorialManager manager, Action onComplete);
        public virtual void End(TutorialManager manager) { }
    }
}
