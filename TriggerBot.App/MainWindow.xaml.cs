using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using TriggerBot.App.ViewModels;
using TriggerBot.Core.Detection;
using TriggerBot.Core.SpeedTesting;
using TriggerBot.Windows.Input;

namespace TriggerBot.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private readonly DetectionLoop _detectionLoop = new();
    private readonly WindowsDetectionRuntime _detectionRuntime = new();
    private CancellationTokenSource? _detectionCancellation;
    private CancellationTokenSource? _speedTestCancellation;
    private bool _awaitingColorSelection;
    private bool _awaitingPositionSelection;
    private bool _initializingSettings;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        RefreshCaptureDisplay();
        InitializeSettingsDisplay();
        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;
        _hotkeys.Pressed += Hotkeys_Pressed;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _hotkeys.Attach(this);
            _viewModel.SetStatus("Ready · F5 toggle · F6 color · F7 position");
        }
        catch (HotkeyRegistrationException exception)
        {
            _viewModel.SetStatus(exception.Message);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _detectionCancellation?.Cancel();
        _detectionCancellation?.Dispose();
        _speedTestCancellation?.Cancel();
        _speedTestCancellation?.Dispose();
        _hotkeys.Pressed -= Hotkeys_Pressed;
        _hotkeys.Dispose();
    }

    private void Hotkeys_Pressed(object? sender, TriggerHotkey hotkey)
    {
        switch (hotkey)
        {
            case TriggerHotkey.Toggle:
                ToggleTrigger_Click(this, new RoutedEventArgs());
                break;
            case TriggerHotkey.SelectColor:
                if (_awaitingColorSelection) CaptureTargetColor();
                else SelectColor_Click(this, new RoutedEventArgs());
                break;
            case TriggerHotkey.FixPosition:
                if (_awaitingPositionSelection) CaptureFixedPosition();
                else FixPosition_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void InitializeSettingsDisplay()
    {
        _initializingSettings = true;
        ToleranceSlider.Value = _viewModel.Tolerance;
        ClickDelaySlider.Value = _viewModel.ClickDelay.TotalSeconds;
        CooldownSlider.Value = _viewModel.Cooldown.TotalSeconds;
        PositionLockCheckBox.IsChecked = _viewModel.PositionLockEnabled;
        _initializingSettings = false;
    }

    private void Settings_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializingSettings) return;

        var settings = _viewModel.Settings with
        {
            Tolerance = (int)Math.Round(ToleranceSlider.Value),
            ClickDelay = TimeSpan.FromSeconds(ClickDelaySlider.Value),
            Cooldown = TimeSpan.FromSeconds(CooldownSlider.Value)
        };

        try
        {
            _viewModel.UpdateSettings(settings);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            _viewModel.SetStatus(exception.Message);
            InitializeSettingsDisplay();
        }
    }

    private void PositionLock_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings || PositionLockCheckBox.IsChecked is not bool enabled) return;

        try
        {
            _viewModel.UpdateSettings(_viewModel.Settings with { PositionLockEnabled = enabled });
            PositionStatus.Text = enabled ? "Locked position active" : "Follows cursor";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            _viewModel.SetStatus(exception.Message);
            InitializeSettingsDisplay();
        }
    }

    private async void RunSpeedTest_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsArmed)
        {
            _viewModel.SetStatus("Stop the detector before running a speed test.");
            return;
        }
        if (_speedTestCancellation is not null) return;

        var testWindow = new SpeedTestWindow();
        testWindow.Show();
        var point = testWindow.DetectionPoint;
        var settings = _viewModel.Settings with
        {
            TargetColor = new RgbColor(179, 0, 0),
            LockedPosition = point,
            PositionLockEnabled = true
        };
        var cancellation = new CancellationTokenSource();
        _speedTestCancellation = cancellation;
        var detectionTask = _detectionLoop.RunAsync(settings, _detectionRuntime, cancellation.Token);

        try
        {
            var statistics = await new SpeedTestRunner().RunAsync(
                10,
                (trial, token) => testWindow.MeasureTrialAsync(trial, _detectionLoop, token),
                cancellation.Token);

            BestValue.Text = FormatMilliseconds(statistics.Best);
            AverageValue.Text = FormatMilliseconds(statistics.Average);
            DeviationValue.Text = FormatMilliseconds(statistics.StandardDeviation);
            _viewModel.SetStatus("Speed test complete · 10 trials recorded");
        }
        catch (TimeoutException exception)
        {
            _viewModel.SetStatus(exception.Message);
        }
        catch (OperationCanceledException)
        {
            _viewModel.SetStatus("Speed test cancelled");
        }
        finally
        {
            cancellation.Cancel();
            await detectionTask;
            testWindow.Close();
            _speedTestCancellation = null;
            cancellation.Dispose();
        }
    }

    private static string FormatMilliseconds(TimeSpan value) => $"{value.TotalMilliseconds:0.0} ms";

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string section) return;

        _viewModel.ActiveSection = section;
        SectionTitle.Text = section;
        SectionSubtitle.Text = section switch
        {
            "Settings" => "Tune response and appearance without losing focus.",
            "Speed Test" => "Measure the complete path from signal to response.",
            "Help" => "Quick guidance for a predictable setup.",
            _ => "Configure the signal, then arm the detector when you are ready."
        };

        DashboardView.Visibility = section == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = section == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        SpeedTestView.Visibility = section == "Speed Test" ? Visibility.Visible : Visibility.Collapsed;
        HelpView.Visibility = section == "Help" ? Visibility.Visible : Visibility.Collapsed;

        var activeBrush = (Brush)FindResource("AccentBrush");
        var inactiveBrush = (Brush)FindResource("TextMutedBrush");
        SetNavigationState(DashboardNav, section == "Dashboard", activeBrush, inactiveBrush);
        SetNavigationState(SettingsNav, section == "Settings", activeBrush, inactiveBrush);
        SetNavigationState(SpeedNav, section == "Speed Test", activeBrush, inactiveBrush);
        SetNavigationState(HelpNav, section == "Help", activeBrush, inactiveBrush);
    }

    private static void SetNavigationState(Button button, bool active, Brush activeBrush, Brush inactiveBrush)
    {
        button.Background = active
            ? new SolidColorBrush(Color.FromRgb(25, 60, 59))
            : Brushes.Transparent;
        if (button.Content is StackPanel panel && panel.Children.Count > 1)
        {
            if (panel.Children[0] is TextBlock icon) icon.Foreground = active ? activeBrush : inactiveBrush;
            if (panel.Children[1] is TextBlock label) label.Foreground = active ? Brushes.White : (Brush)new SolidColorBrush(Color.FromRgb(168, 180, 193));
        }
    }

    private void ToggleTrigger_Click(object sender, RoutedEventArgs e)
    {
        var wasArmed = _viewModel.IsArmed;
        _viewModel.ToggleArmed();

        if (!wasArmed && _viewModel.IsArmed)
            StartDetection();
        else if (wasArmed && !_viewModel.IsArmed)
            StopDetection();

        TriggerButton.Content = _viewModel.IsArmed ? "STOP TRIGGERBOT" : "START TRIGGERBOT";
        TriggerButton.Background = _viewModel.IsArmed
            ? new SolidColorBrush(Color.FromRgb(104, 62, 62))
            : (Brush)FindResource("AccentBrush");
    }

    private void StartDetection()
    {
        _detectionCancellation?.Cancel();
        _detectionCancellation?.Dispose();
        _detectionCancellation = new CancellationTokenSource();
        _ = RunDetectionAsync(_detectionCancellation.Token);
    }

    private void StopDetection() => _detectionCancellation?.Cancel();

    private async Task RunDetectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _detectionLoop.RunAsync(_viewModel.Settings, _detectionRuntime, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _viewModel.MarkFault(exception);
            _detectionCancellation?.Cancel();
        }
    }

    private void SelectColor_Click(object sender, RoutedEventArgs e)
    {
        _awaitingColorSelection = true;
        _awaitingPositionSelection = false;
        _viewModel.SetStatus("Color selection armed · move the cursor and press F6");
        ColorStatus.Text = "Waiting for F6";
        ColorStatus.Foreground = (Brush)FindResource("WarningBrush");
    }

    private void CaptureTargetColor()
    {
        try
        {
            var point = _detectionRuntime.GetCursorPosition();
            var color = _detectionRuntime.ReadPixel(point);
            _viewModel.SetTargetColor(color);
            _awaitingColorSelection = false;
            TargetColorValue.Text = _viewModel.TargetColorLabel;
            ColorStatus.Text = $"Captured at ({point.X}, {point.Y})";
            ColorStatus.Foreground = (Brush)FindResource("AccentBrush");
        }
        catch (Exception exception)
        {
            _viewModel.SetStatus($"Color capture failed: {exception.Message}");
            ColorStatus.Text = "Capture failed";
            ColorStatus.Foreground = (Brush)FindResource("DangerBrush");
        }
    }

    private void FixPosition_Click(object sender, RoutedEventArgs e)
    {
        _awaitingPositionSelection = true;
        _awaitingColorSelection = false;
        _viewModel.SetStatus("Position selection armed · move the cursor and press F7");
        PositionStatus.Text = "Waiting for F7";
    }

    private void RefreshCaptureDisplay()
    {
        TargetColorValue.Text = _viewModel.TargetColorLabel;
        ColorStatus.Text = _viewModel.TargetColorStatus;
        if (_viewModel.Settings.LockedPosition is { } point)
        {
            PositionValue.Text = $"({point.X}, {point.Y})";
            PositionStatus.Text = "Fixed position ready";
        }
    }

    private void CaptureFixedPosition()
    {
        try
        {
            var point = _detectionRuntime.GetCursorPosition();
            _viewModel.SetLockedPosition(point);
            _awaitingPositionSelection = false;
            PositionValue.Text = $"({point.X}, {point.Y})";
            PositionStatus.Text = "Fixed position ready";
        }
        catch (Exception exception)
        {
            _viewModel.SetStatus($"Position capture failed: {exception.Message}");
            PositionStatus.Text = "Capture failed";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
}
