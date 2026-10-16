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
        Assert.Equal("F6", settings.ColorHotkey);
        Assert.Equal("R", settings.SpamHotkey);
        Assert.Equal("XButton1", settings.CameraMacroHotkey);
        Assert.Equal(1.0, settings.CameraMacroSensitivity);
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
    public void UnsupportedHotkeyIsRejected()
    {
        var settings = DetectionSettings.Default with { CameraMacroHotkey = "Mouse7" };

        Assert.Throws<ArgumentException>(() => settings.Validate());
    }

    [Fact]
    public void NonPositiveMacroSensitivityIsRejected()
    {
        var settings = DetectionSettings.Default with { CameraMacroSensitivity = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }

    [Fact]
    public void InvalidTimingValuesAreRejected()
    {
        var settings = DetectionSettings.Default with { Cooldown = TimeSpan.Zero };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }
}
