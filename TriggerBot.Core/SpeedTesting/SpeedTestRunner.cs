namespace TriggerBot.Core.SpeedTesting;

public sealed class SpeedTestRunner
{
    public async Task<SpeedTestStatistics> RunAsync(
        int trialCount,
        Func<int, CancellationToken, Task<TimeSpan>> measureTrial,
        CancellationToken cancellationToken)
    {
        if (trialCount is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(trialCount), "Trial count must be between 1 and 100.");
        ArgumentNullException.ThrowIfNull(measureTrial);

        var samples = new List<TimeSpan>(trialCount);
        for (var trial = 0; trial < trialCount; trial++)
        {
            var sample = await measureTrial(trial + 1, cancellationToken);
            if (sample < TimeSpan.Zero)
                throw new InvalidOperationException("A speed test returned a negative reaction time.");
            samples.Add(sample);
        }

        return SpeedTestStatistics.Calculate(samples);
    }
}
