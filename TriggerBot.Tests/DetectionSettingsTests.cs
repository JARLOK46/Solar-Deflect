using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class DetectionSettingsTests
{
    [Fact]
    public void DefaultsAreSafeForFirstRun()
    {
        var settings = DetectionSettings.Default;

        Assert.Equal(30, settings.Tolerance);
        Assert.Equal(TimeSpan.Zero, settings.ClickDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(100), settings.Cooldown);
        Assert.Equal(TimeSpan.FromMilliseconds(50), settings.SpamClickInterval);
        Assert.False(settings.PositionLockEnabled);
        Assert.Null(settings.LockedPosition);
    }

    [Fact]
    public void InvalidSpamIntervalIsRejected()
    {
        var settings = DetectionSettings.Default with { SpamClickInterval = TimeSpan.FromMilliseconds(5) };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }

    [Fact]
    public void PositionLockRequiresAFixedPosition()
    {
        var settings = DetectionSettings.Default with { PositionLockEnabled = true };

        Assert.Throws<InvalidOperationException>(() => settings.Validate());
    }

    [Fact]
    public void InvalidTimingValuesAreRejected()
    {
        var settings = DetectionSettings.Default with { Cooldown = TimeSpan.Zero };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }
}
