namespace Cast.Game
{

    public readonly struct BoosterResult
    {
        public readonly BoosterType Type;
        public readonly bool Applied;
        public readonly bool Cancelled;
        public readonly string Reason;

        private BoosterResult(BoosterType type, bool applied, bool cancelled, string reason)
        {
            Type = type;
            Applied = applied;
            Cancelled = cancelled;
            Reason = reason;
        }

        public static BoosterResult Ok(BoosterType t) => new BoosterResult(t, true, false, null);
        public static BoosterResult Cancel(BoosterType t) => new BoosterResult(t, false, true, "cancelled");
        public static BoosterResult Rejected(BoosterType t, string reason) => new BoosterResult(t, false, false, reason);
    }
}
