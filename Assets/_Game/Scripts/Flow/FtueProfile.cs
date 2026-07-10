using CaskFramework.Profile;

namespace Cast.Game
{
    public static class FtueProfile
    {
        private const int FtueCompletedFlagBit = 0;

        public static bool FtueCompleted(this IProfileService profile) =>
            profile != null && profile.IsFlagged(FtueCompletedFlagBit);

        public static void CompleteFtue(this IProfileService profile) =>
            profile?.SetFlag(FtueCompletedFlagBit);

        public static bool ShouldRunFtue(this IProfileService profile) =>
            profile != null && profile.ProgressLevel == 1 && !profile.FtueCompleted();

        public static int CurrentLevelId(this IProfileService profile) =>
            profile.ProgressLevel - 1;
    }
}
