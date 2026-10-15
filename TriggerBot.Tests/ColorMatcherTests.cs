using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class ColorMatcherTests
{
    [Fact]
    public void MatchesWhenEveryRgbChannelIsWithinTolerance()
    {
        var target = new RgbColor(100, 150, 200);
        var sample = new RgbColor(110, 140, 205);

        Assert.True(ColorMatcher.Matches(target, sample, tolerance: 10));
    }

    [Fact]
    public void RejectsWhenOneRgbChannelExceedsTolerance()
    {
        var target = new RgbColor(100, 150, 200);
        var sample = new RgbColor(110, 161, 200);

        Assert.False(ColorMatcher.Matches(target, sample, tolerance: 10));
    }

    [Fact]
    public void RetentionMatchUsesExpandedTolerance()
    {
        var target = new RgbColor(100, 150, 200);
        var sample = new RgbColor(125, 150, 200);

        Assert.False(ColorMatcher.Matches(target, sample, tolerance: 10));
        Assert.True(ColorMatcher.MatchesRetention(target, sample, tolerance: 10, retentionExtra: 20));
    }

    [Fact]
    public void MatchesTranslucentRedBlendWhenRgbChannelsShiftTogether()
    {
        var target = new RgbColor(179, 0, 0);
        var blendedRed = new RgbColor(110, 40, 40);

        Assert.False(
            Math.Abs(target.Red - blendedRed.Red) <= 30
            && Math.Abs(target.Green - blendedRed.Green) <= 30
            && Math.Abs(target.Blue - blendedRed.Blue) <= 30);
        Assert.True(ColorMatcher.Matches(target, blendedRed, tolerance: 30));
    }

    [Fact]
    public void DoesNotTreatNeutralOrBluePixelsAsRedBlends()
    {
        var target = new RgbColor(179, 0, 0);

        Assert.False(ColorMatcher.Matches(target, new RgbColor(90, 90, 90), tolerance: 30));
        Assert.False(ColorMatcher.Matches(target, new RgbColor(55, 30, 120), tolerance: 30));
    }

    [Fact]
    public void KeepsExistingStrictMatchingForNonRedTargets()
    {
        var target = new RgbColor(100, 150, 200);
        var shifted = new RgbColor(70, 120, 170);

        Assert.False(ColorMatcher.Matches(target, shifted, tolerance: 10));
    }

    [Fact]
    public void InvalidToleranceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ColorMatcher.Matches(new RgbColor(0, 0, 0), new RgbColor(0, 0, 0), -1));
    }
}
