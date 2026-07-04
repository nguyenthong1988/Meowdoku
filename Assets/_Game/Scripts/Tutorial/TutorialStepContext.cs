namespace Cast.Game
{
    public sealed class TutorialStepContext
    {
        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardInput Input { get; }
        public TutorialController View { get; }

        public TutorialStepContext(IGameSession session, BoardView board, IBoardInput input, TutorialController view)
        {
            Session = session;
            Board = board;
            Input = input;
            View = view;
        }
    }
}
