namespace Cast.Game.Gameplay
{

    public enum MoveOutcome : byte
    {
        
        NoOp = 0,

        Placed = 1,

        RemovedCat = 2,

        Marked = 3,

        Unmarked = 4,

        RejectedIllegal = 5,

        Wrong = 6,
    }
}
