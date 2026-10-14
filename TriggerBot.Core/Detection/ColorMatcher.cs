namespace TriggerBot.Core.Detection;

public static class ColorMatcher
{
    public static bool Matches(RgbColor target, RgbColor sample, int tolerance)
    {
        ValidateTolerance(tolerance);
        return Within(target.Red, sample.Red, tolerance)
            && Within(target.Green, sample.Green, tolerance)
            && Within(target.Blue, sample.Blue, tolerance);
    }

    public static bool MatchesRetention(RgbColor target, RgbColor sample, int tolerance, int retentionExtra)
    {
        ValidateTolerance(tolerance);
        if (retentionExtra < 0)
            throw new ArgumentOutOfRangeException(nameof(retentionExtra), "Retention extra must be non-negative.");

        return Matches(target, sample, checked(tolerance + retentionExtra));
    }

    private static bool Within(byte target, byte sample, int tolerance) =>
        Math.Abs(target - sample) <= tolerance;

    private static void ValidateTolerance(int tolerance)
    {
        if (tolerance is < 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 255.");
    }
}
