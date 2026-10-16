namespace TriggerBot.Core.Detection;

public sealed record DetectionSettings
{
    public static DetectionSettings Default { get; } = new();

    public RgbColor? TargetColor { get; init; }
    public int Tolerance { get; init; } = 30;
    public TimeSpan ClickInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan ClickDelay { get; init; } = TimeSpan.Zero;
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan SpamClickInterval { get; init; } = TimeSpan.FromMilliseconds(50);
    public bool SpamEnabled { get; init; } = true;
    public ScreenPoint? LockedPosition { get; init; }
    public bool PositionLockEnabled { get; init; }
    public string ColorHotkey { get; init; } = "F6";
    public string SpamHotkey { get; init; } = "R";
    public string CameraMacroHotkey { get; init; } = "XButton1";
    public double CameraMacroSensitivity { get; init; } = 1.0;

    public void Validate()
    {
        if (Tolerance is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Tolerance), "Tolerance must be between 0 and 100.");
        if (ClickInterval < TimeSpan.FromMilliseconds(10) || ClickInterval > TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(ClickInterval), "Click interval must be between 0.01 and 1 second.");
        if (ClickDelay < TimeSpan.Zero || ClickDelay > TimeSpan.FromMilliseconds(500))
            throw new ArgumentOutOfRangeException(nameof(ClickDelay), "Click delay must be between 0 and 0.5 seconds.");
        if (Cooldown < TimeSpan.FromMilliseconds(10) || Cooldown > TimeSpan.FromMilliseconds(500))
            throw new ArgumentOutOfRangeException(nameof(Cooldown), "Cooldown must be between 0.01 and 0.5 seconds.");
        if (SpamClickInterval < TimeSpan.FromMilliseconds(10) || SpamClickInterval > TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(SpamClickInterval), "Spam click interval must be between 0.01 and 1 second.");
        if (PositionLockEnabled && LockedPosition is null)
            throw new InvalidOperationException("Position lock requires a fixed position.");
        ValidateHotkey(ColorHotkey, nameof(ColorHotkey));
        ValidateHotkey(SpamHotkey, nameof(SpamHotkey));
        ValidateHotkey(CameraMacroHotkey, nameof(CameraMacroHotkey));
        if (double.IsNaN(CameraMacroSensitivity) || double.IsInfinity(CameraMacroSensitivity)
            || CameraMacroSensitivity <= 0)
            throw new ArgumentOutOfRangeException(nameof(CameraMacroSensitivity), "Camera macro sensitivity must be greater than zero.");
    }

    private static void ValidateHotkey(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Hotkey cannot be empty.", parameterName);
        if (!HotkeyVirtualKey.TryParse(value, out _))
            throw new ArgumentException($"Unsupported hotkey '{value}'.", parameterName);
    }
}
