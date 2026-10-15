namespace TriggerBot.Core.Detection;

public sealed class DetectionLoop
{
    private static readonly TimeSpan RetentionPollInterval = TimeSpan.FromMilliseconds(2);
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
                    await Task.Delay(settings.ClickInterval, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var point = ResolvePoint(settings, runtime);
                var sample = runtime.ReadPixel(point);
                if (!ColorMatcher.Matches(target, sample, settings.Tolerance))
                {
                    await Task.Delay(settings.ClickInterval, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (settings.ClickDelay > TimeSpan.Zero)
                    await Task.Delay(settings.ClickDelay, cancellationToken).ConfigureAwait(false);

                // Re-check the key, position and color after the optional delay so stale
                // samples cannot trigger a click after the user has moved or released.
                if (!runtime.IsActivationKeyPressed)
                    continue;

                var clickPoint = ResolvePoint(settings, runtime);
                var clickSample = runtime.ReadPixel(clickPoint);
                if (!ColorMatcher.Matches(target, clickSample, settings.Tolerance))
                    continue;

                runtime.LeftClick();
                ClickDetected?.Invoke(this, EventArgs.Empty);
                await WaitForTargetReleaseAsync(settings, runtime, clickPoint, target, cancellationToken).ConfigureAwait(false);
                await Task.Delay(settings.Cooldown, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is the normal shutdown path for the worker.
        }
    }

    private static ScreenPoint ResolvePoint(DetectionSettings settings, IDetectionRuntime runtime) =>
        settings.PositionLockEnabled ? settings.LockedPosition!.Value : runtime.GetCursorPosition();

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

                await Task.Delay(RetentionPollInterval, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The target remained visible for the complete retention window; cooldown still applies.
        }
    }
}
