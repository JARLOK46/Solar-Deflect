using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using TriggerBot.Core.Detection;

namespace TriggerBot.App;

public partial class SpeedTestWindow : Window
{
    private static readonly RgbColor SignalColor = new(179, 0, 0);
    private readonly Random _random = new();

    public SpeedTestWindow()
    {
        InitializeComponent();
    }

    public ScreenPoint DetectionPoint => new(
        (int)Math.Round(Left + Width / 2),
        (int)Math.Round(Top + Height / 2));

    public async Task<TimeSpan> MeasureTrialAsync(int trial, DetectionLoop loop, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loop);
        SetTrial(trial);
        SetSignalActive(false);

        await Task.Delay(_random.Next(1000, 3001), cancellationToken);

        var clickDetected = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClickDetected(object? sender, EventArgs args) => clickDetected.TrySetResult(null);
        loop.ClickDetected += OnClickDetected;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            SetSignalActive(true);
            var completed = await Task.WhenAny(
                clickDetected.Task,
                Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            if (completed != clickDetected.Task)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("No detector click was received during the speed test trial.");
            }

            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        finally
        {
            loop.ClickDetected -= OnClickDetected;
            SetSignalActive(false);
        }
    }

    private void SetTrial(int trial)
    {
        Dispatcher.Invoke(() => TrialLabel.Text = $"Trial {trial} / 10");
    }

    private void SetSignalActive(bool active)
    {
        Dispatcher.Invoke(() =>
        {
            SignalSurface.Background = active
                ? new SolidColorBrush(Color.FromRgb(SignalColor.Red, SignalColor.Green, SignalColor.Blue))
                : new SolidColorBrush(Color.FromRgb(5, 7, 10));
            SignalLabel.Text = active ? "SIGNAL" : "WAITING";
            SignalLabel.Foreground = active
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Color.FromRgb(113, 130, 150));
        });
    }
}
