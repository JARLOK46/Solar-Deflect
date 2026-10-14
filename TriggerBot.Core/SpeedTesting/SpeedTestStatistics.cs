namespace TriggerBot.Core.SpeedTesting;

public sealed record SpeedTestStatistics(
    int SampleCount,
    TimeSpan Best,
    TimeSpan Worst,
    TimeSpan Average,
    TimeSpan StandardDeviation)
{
    public static SpeedTestStatistics Calculate(IReadOnlyCollection<TimeSpan> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
            throw new ArgumentException("At least one reaction sample is required.", nameof(samples));
        if (samples.Any(sample => sample < TimeSpan.Zero))
            throw new ArgumentOutOfRangeException(nameof(samples), "Reaction times cannot be negative.");

        var values = samples.Select(sample => sample.TotalMilliseconds).ToArray();
        var average = values.Average();
        var variance = values.Select(value => Math.Pow(value - average, 2)).Average();

        return new SpeedTestStatistics(
            samples.Count,
            TimeSpan.FromMilliseconds(values.Min()),
            TimeSpan.FromMilliseconds(values.Max()),
            TimeSpan.FromMilliseconds(average),
            TimeSpan.FromMilliseconds(Math.Sqrt(variance)));
    }
}
