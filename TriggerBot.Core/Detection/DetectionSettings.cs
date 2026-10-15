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
    public ScreenPoint? LockedPosition { get; init; }
    public bool PositionLockEnabled { get; init; }

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
    }
}
