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
    public void InvalidToleranceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ColorMatcher.Matches(new RgbColor(0, 0, 0), new RgbColor(0, 0, 0), -1));
    }
}
