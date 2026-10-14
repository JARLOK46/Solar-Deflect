namespace TriggerBot.Core.Detection;

public sealed class DetectionLoop
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(2);
    private static readonly TimeSpan RetentionWait = TimeSpan.FromMilliseconds(450);
    private const int RetentionExtraTolerance = 30;

    public event EventHandler? ClickDetected;

    public async Task RunAsync(DetectionSettings settings, IDetectionRuntime runtime, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runtime);
        settings.Validate();

        var target = settings.TargetColor
            ?? throw new InvalidOperationException("A target color is required before detection can begin.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!runtime.IsActivationKeyPressed)
                {
                    await Task.Delay(PollInterval, cancellationToken);
                    continue;
                }

                var point = settings.PositionLockEnabled
                    ? settings.LockedPosition!.Value
                    : runtime.GetCursorPosition();
                var sample = runtime.ReadPixel(point);

                if (!ColorMatcher.Matches(target, sample, settings.Tolerance))
                {
                    await Task.Delay(PollInterval, cancellationToken);
                    continue;
                }

                if (settings.ClickDelay > TimeSpan.Zero)
                    await Task.Delay(settings.ClickDelay, cancellationToken);

                if (!runtime.IsActivationKeyPressed)
                    continue;

                runtime.LeftClick();
                ClickDetected?.Invoke(this, EventArgs.Empty);
                await WaitForTargetReleaseAsync(settings, runtime, point, target, cancellationToken);
                await Task.Delay(settings.Cooldown, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is the normal shutdown path for the worker.
        }
    }

    private static async Task WaitForTargetReleaseAsync(
        DetectionSettings settings,
        IDetectionRuntime runtime,
        ScreenPoint point,
        RgbColor target,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RetentionWait);

        try
        {
            while (!timeout.IsCancellationRequested)
            {
                var sample = runtime.ReadPixel(point);
                if (!ColorMatcher.MatchesRetention(target, sample, settings.Tolerance, RetentionExtraTolerance))
                    return;

                await Task.Delay(PollInterval, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The target remained visible for the complete retention window; cooldown still applies.
        }
    }
}
