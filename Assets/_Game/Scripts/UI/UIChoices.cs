namespace Cast.Game
{
    public enum HomeChoice : byte
    {
        Play = 0,
        Settings = 1,
    }

    public enum PauseChoice : byte
    {
        Resume = 0,
        Restart = 1,
        Settings = 2,
        Home = 3,
    }

    public enum WinChoice : byte
    {
        Next = 0,
        Replay = 1,
        Home = 2,
    }

    public enum LoseChoice : byte
    {
        Retry = 0,
        Home = 1,
    }
}
