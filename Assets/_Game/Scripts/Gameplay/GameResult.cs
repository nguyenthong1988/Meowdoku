namespace Cast.Game
{

    public readonly struct GameResult
    {
        public readonly bool Won;
        public readonly int HeartsLeft;
        public readonly int HintsUsed;
        public readonly float Elapsed;
        public readonly int Moves;

        public GameResult(bool won, int heartsLeft, int hintsUsed, float elapsed, int moves)
        {
            Won = won;
            HeartsLeft = heartsLeft;
            HintsUsed = hintsUsed;
            Elapsed = elapsed;
            Moves = moves;
        }
    }
}
