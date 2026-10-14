using TriggerBot.Core.SpeedTesting;

namespace TriggerBot.Tests;

public sealed class SpeedTestRunnerTests
{
    [Fact]
    public async Task RunsEachTrialAndAggregatesTheSamples()
    {
        var calls = new List<int>();
        var runner = new SpeedTestRunner();

        var result = await runner.RunAsync(
            3,
            (trial, _) =>
            {
                calls.Add(trial);
                return Task.FromResult(TimeSpan.FromMilliseconds(trial * 100));
            },
            CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, calls);
        Assert.Equal(3, result.SampleCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), result.Average);
    }

    [Fact]
    public async Task RejectsInvalidTrialCount()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new SpeedTestRunner().RunAsync(0, (_, _) => Task.FromResult(TimeSpan.Zero), CancellationToken.None));
    }
}
