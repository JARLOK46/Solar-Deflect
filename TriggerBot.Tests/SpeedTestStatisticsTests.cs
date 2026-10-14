using TriggerBot.Core.SpeedTesting;

namespace TriggerBot.Tests;

public sealed class SpeedTestStatisticsTests
{
    [Fact]
    public void CalculatesBestWorstAverageAndStandardDeviation()
    {
        var samples = new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(300)
        };

        var result = SpeedTestStatistics.Calculate(samples);

        Assert.Equal(3, result.SampleCount);
        Assert.Equal(TimeSpan.FromMilliseconds(100), result.Best);
        Assert.Equal(TimeSpan.FromMilliseconds(300), result.Worst);
        Assert.Equal(TimeSpan.FromMilliseconds(200), result.Average);
        Assert.Equal(81.6496, result.StandardDeviation.TotalMilliseconds, precision: 3);
    }

    [Fact]
    public void RejectsEmptySamples()
    {
        Assert.Throws<ArgumentException>(() => SpeedTestStatistics.Calculate(Array.Empty<TimeSpan>()));
    }
}
