using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class DetectionLoopTests
{
    [Fact]
    public async Task DoesNotClickWhenActivationKeyIsNotPressed()
    {
        var runtime = new FakeRuntime { ActivationPressed = false, Sample = new RgbColor(179, 0, 0) };
        using var cancellation = new CancellationTokenSource();
        var settings = DetectionSettings.Default with { TargetColor = runtime.Sample };
        var loop = new DetectionLoop().RunAsync(settings, runtime, cancellation.Token);

        await Task.Delay(30);
        cancellation.Cancel();
        await loop;

        Assert.Equal(0, runtime.Clicks);
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

        await Task.Delay(80);
        cancellation.Cancel();
        await loop;

        Assert.True(runtime.Clicks >= 1);
    }

    private sealed class FakeRuntime : IDetectionRuntime
    {
        public bool ActivationPressed { get; set; }
        public RgbColor Sample { get; set; }
        public int Clicks { get; private set; }
        public bool IsActivationKeyPressed => ActivationPressed;
        public ScreenPoint GetCursorPosition() => new(10, 20);
        public RgbColor ReadPixel(ScreenPoint point) => Sample;
        public void LeftClick() => Clicks++;
    }
}
