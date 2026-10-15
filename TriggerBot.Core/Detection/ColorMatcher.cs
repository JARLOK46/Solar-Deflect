namespace TriggerBot.Core.Detection;

public static class ColorMatcher
{
    public static bool Matches(RgbColor target, RgbColor sample, int tolerance)
    {
        ValidateTolerance(tolerance);

        if (Within(target.Red, sample.Red, tolerance)
            && Within(target.Green, sample.Green, tolerance)
            && Within(target.Blue, sample.Blue, tolerance))
            return true;

        // A translucent red overlay can change all three RGB channels at once:
        // channel-by-channel matching rejects it even though its hue is still red.
        return IsRed(target) && IsRedBlend(sample, target, tolerance);
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

    private static bool IsRed(RgbColor color)
    {
        var maximumOtherChannel = Math.Max((int)color.Green, color.Blue);
        return color.Red - maximumOtherChannel >= 20;
    }

    private static bool IsRedBlend(RgbColor sample, RgbColor target, int tolerance)
    {
        var maximumOtherChannel = Math.Max((int)sample.Green, sample.Blue);
        var minimumDominance = Math.Max(8, tolerance / 3);
        var minimumRedValue = Math.Max(18, (int)Math.Round(target.Red * (0.35 - tolerance * 0.001)));

        // Blending changes brightness and lifts the other channels, but a red
        // signal remains red-dominant and cannot become a near-black neutral pixel.
        return sample.Red - maximumOtherChannel >= minimumDominance
            && sample.Red >= minimumRedValue
            && Saturation(sample) >= 0.12
            && Value(sample) >= 0.08;
    }

    private static double Saturation(RgbColor color)
    {
        var maximum = Math.Max(color.Red, Math.Max(color.Green, color.Blue));
        if (maximum == 0) return 0;
        var minimum = Math.Min(color.Red, Math.Min(color.Green, color.Blue));
        return (maximum - minimum) / (double)maximum;
    }

    private static double Value(RgbColor color) =>
        Math.Max(color.Red, Math.Max(color.Green, color.Blue)) / 255d;

    private static void ValidateTolerance(int tolerance)
    {
        if (tolerance is < 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 255.");
    }
}
