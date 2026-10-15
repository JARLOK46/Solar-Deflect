using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class DetectionLoopTests
{
    [Fact]
    public async Task DoesNotReadOrClickWhenActivationKeyIsNotPressed()
    {
        var runtime = new FakeRuntime { ActivationPressed = false, Sample = new RgbColor(179, 0, 0) };
        using var cancellation = new CancellationTokenSource();
        var settings = DetectionSettings.Default with { TargetColor = runtime.Sample };
        var loop = new DetectionLoop().RunAsync(settings, runtime, cancellation.Token);

        await Task.Delay(40);
        cancellation.Cancel();
        await loop;

        Assert.Equal(0, runtime.Clicks);
        Assert.Equal(0, runtime.Reads);
    }

    [Fact]
    public async Task ClicksMatchingTargetOnlyWhenActivationKeyIsPressed()
    {
        var runtime = new FakeRuntime { ActivationPressed = true, Sample = new RgbColor(179, 0, 0) };
        using var cancellation = new CancellationTokenSource();
        var settings = DetectionSettings.Default with
        {
            TargetColor = runtime.Sample,
            ClickDelay = TimeSpan.Zero,
            Cooldown = TimeSpan.FromMilliseconds(10)
        };
        var loop = new DetectionLoop().RunAsync(settings, runtime, cancellation.Token);

        await WaitUntilAsync(() => runtime.Clicks > 0, TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await loop;

        Assert.True(runtime.Clicks >= 1);
    }

    [Fact]
    public async Task RechecksTargetAfterClickDelayBeforeClicking()
    {
        var target = new RgbColor(179, 0, 0);
        var runtime = new FakeRuntime
        {
            ActivationPressed = true,
            Sample = target,
            ReadPixelFunc = (_, readCount) => readCount == 1 ? target : new RgbColor(0, 0, 0)
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(70));
        var settings = DetectionSettings.Default with
        {
            TargetColor = target,
            ClickDelay = TimeSpan.FromMilliseconds(30),
            ClickInterval = TimeSpan.FromMilliseconds(10)
        };

        await new DetectionLoop().RunAsync(settings, runtime, cancellation.Token);

        Assert.Equal(0, runtime.Clicks);
    }

    [Fact]
    public async Task HonorsCancellationDuringClickDelay()
    {
        var runtime = new FakeRuntime { ActivationPressed = true, Sample = new RgbColor(179, 0, 0) };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        var settings = DetectionSettings.Default with
        {
            TargetColor = runtime.Sample,
            ClickDelay = TimeSpan.FromSeconds(0.2)
        };

        await new DetectionLoop().RunAsync(settings, runtime, cancellation.Token);

        Assert.Equal(0, runtime.Clicks);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed >= timeout)
                throw new TimeoutException("The expected condition was not reached in time.");
            await Task.Delay(5);
        }
    }

    private sealed class FakeRuntime : IDetectionRuntime
    {
        public bool ActivationPressed { get; set; }
        public RgbColor Sample { get; set; }
        public Func<ScreenPoint, int, RgbColor>? ReadPixelFunc { get; init; }
        public int Clicks { get; private set; }
        public int Reads { get; private set; }
        public bool IsActivationKeyPressed => ActivationPressed;
        public bool IsSpamKeyPressed => false;
        public ScreenPoint GetCursorPosition() => new(10, 20);
        public RgbColor ReadPixel(ScreenPoint point)
        {
            Reads++;
            return ReadPixelFunc?.Invoke(point, Reads) ?? Sample;
        }
        public void LeftClick() => Clicks++;
    }
}
