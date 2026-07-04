using System;

namespace Cast.Game
{
    public abstract class TutorialStep
    {
        public abstract string Message { get; }
        public abstract void Begin(TutorialStepContext context, Action onComplete);
        public virtual void End(TutorialStepContext context) { }
    }
}
