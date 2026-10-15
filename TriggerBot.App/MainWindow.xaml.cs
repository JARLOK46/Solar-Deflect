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
    private Task? _detectionTask;
    private CancellationTokenSource? _speedTestCancellation;
    private CancellationTokenSource? _spamCancellation;
    private Task? _spamTask;
    private bool _awaitingColorSelection;
    private bool _awaitingPositionSelection;
    // Slider ValueChanged can fire while InitializeComponent is still constructing
    // the XAML tree, before all named controls have been assigned.
    private bool _initializingSettings = true;

    public MainWindow()
    {
        InitializeComponent();
        _initializingSettings = false;
        DataContext = _viewModel;
        RefreshCaptureDisplay();
        InitializeSettingsDisplay();
        UpdateDashboardSettings();
        UpdateTriggerButton();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Navigation_Click(DashboardNav, new RoutedEventArgs());
        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;
        _hotkeys.Pressed += Hotkeys_Pressed;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Status))
        {
            StatusText.Text = _viewModel.Status;
            AlertText.Text = _viewModel.Status;
            var isError = _viewModel.Status.Contains("error", StringComparison.OrdinalIgnoreCase)
                || _viewModel.Status.Contains("fall", StringComparison.OrdinalIgnoreCase);
            AlertBanner.Background = (Brush)FindResource(isError ? "DangerBrush" : "AccentDimBrush");
            AlertText.Foreground = (Brush)FindResource(isError ? "TextPrimaryBrush" : "AccentBrush");
        }
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _hotkeys.Attach(this);
            _viewModel.SetStatus("Listo · F5 activar · F6 color · F7 posición");
        }
        catch (HotkeyRegistrationException exception)
        {
            _viewModel.SetStatus(exception.Message);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _detectionCancellation?.Cancel();
        _speedTestCancellation?.Cancel();
        _spamCancellation?.Cancel();
        _hotkeys.Pressed -= Hotkeys_Pressed;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
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
            case TriggerHotkey.Spam:
                StartSpamClicks();
                break;
        }
    }

    private void InitializeSettingsDisplay()
    {
        _initializingSettings = true;
        ToleranceSlider.Value = _viewModel.Tolerance;
        ClickIntervalSlider.Value = _viewModel.ClickInterval.TotalMilliseconds;
        ClickDelaySlider.Value = _viewModel.ClickDelay.TotalSeconds;
        CooldownSlider.Value = _viewModel.Cooldown.TotalSeconds;
        SpamIntervalSlider.Value = _viewModel.SpamClickInterval.TotalMilliseconds;
        ToleranceValue.Text = $"{_viewModel.Tolerance} / 100";
        ClickIntervalValue.Text = $"{_viewModel.ClickInterval.TotalMilliseconds:0} ms";
        ClickDelayValue.Text = $"{_viewModel.ClickDelay.TotalSeconds:0.00} s";
        CooldownValue.Text = $"{_viewModel.Cooldown.TotalSeconds:0.00} s";
        SpamIntervalValue.Text = $"{_viewModel.SpamClickInterval.TotalMilliseconds:0} ms";
        PositionModeValue.Text = _viewModel.PositionLockEnabled ? "Usar punto fijo" : "Sigue el cursor";
        FixedPositionValue.Text = _viewModel.Settings.LockedPosition is { } fixedPoint
            ? $"Punto fijo: ({fixedPoint.X}, {fixedPoint.Y})"
            : "Punto fijo: no definido";
        PositionLockCheckBox.IsChecked = _viewModel.PositionLockEnabled;
        SpamEnabledCheckBox.IsChecked = _viewModel.SpamEnabled;
        _initializingSettings = false;
    }

    private void Settings_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Do not read any other slider during XAML construction: the event can be
        // raised before later named controls (or the view model) are ready.
        if (_initializingSettings
            || _viewModel is null
            || ToleranceSlider is null
            || ClickIntervalSlider is null
            || ClickDelaySlider is null
            || CooldownSlider is null)
            return;

        var settings = _viewModel.Settings with
        {
            Tolerance = (int)Math.Round(ToleranceSlider.Value),
            ClickInterval = TimeSpan.FromMilliseconds(Math.Round(ClickIntervalSlider.Value)),
            ClickDelay = TimeSpan.FromSeconds(ClickDelaySlider.Value),
            Cooldown = TimeSpan.FromSeconds(CooldownSlider.Value),
            SpamClickInterval = TimeSpan.FromMilliseconds(Math.Round(SpamIntervalSlider.Value))
        };

        try
        {
            _viewModel.UpdateSettings(settings);
            ToleranceValue.Text = $"{settings.Tolerance} / 100";
            ClickIntervalValue.Text = $"{settings.ClickInterval.TotalMilliseconds:0} ms";
            ClickDelayValue.Text = $"{settings.ClickDelay.TotalSeconds:0.00} s";
            CooldownValue.Text = $"{settings.Cooldown.TotalSeconds:0.00} s";
            SpamIntervalValue.Text = $"{settings.SpamClickInterval.TotalMilliseconds:0} ms";
            UpdateDashboardSettings();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            _viewModel.SetStatus(exception.Message);
            InitializeSettingsDisplay();
        }
    }

    private void SpamEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings || SpamEnabledCheckBox.IsChecked is not bool enabled) return;

        try
        {
            _viewModel.UpdateSettings(_viewModel.Settings with { SpamEnabled = enabled });
            _viewModel.SetStatus(enabled ? "Spam habilitado · mantén R para usarlo" : "Spam deshabilitado");
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
            PositionStatus.Text = enabled ? "Punto fijo activo" : "Modo dinámico";
            UpdateDashboardSettings();
            InitializeSettingsDisplay();
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
            _viewModel.SetStatus("Detén el detector antes de iniciar una prueba de velocidad.");
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
        Task? detectionTask = null;

        try
        {
            detectionTask = Task.Run(
                () => _detectionLoop.RunAsync(settings, _detectionRuntime, cancellation.Token));

            var statistics = await new SpeedTestRunner().RunAsync(
                10,
                (trial, token) => testWindow.MeasureTrialAsync(trial, _detectionLoop, token),
                cancellation.Token);

            BestValue.Text = FormatMilliseconds(statistics.Best);
            AverageValue.Text = FormatMilliseconds(statistics.Average);
            DeviationValue.Text = FormatMilliseconds(statistics.StandardDeviation);
            _viewModel.SetStatus("Prueba completada · 10 mediciones registradas");
        }
        catch (TimeoutException exception)
        {
            _viewModel.SetStatus($"La prueba no se completó: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            _viewModel.SetStatus("Prueba cancelada");
        }
        finally
        {
            cancellation.Cancel();
            if (detectionTask is not null)
            {
                try
                {
                    await detectionTask;
                }
                catch (OperationCanceledException)
                {
                    // Cancellation is expected when the test window closes or the app exits.
                }
            }
            testWindow.Close();
            _speedTestCancellation = null;
            cancellation.Dispose();
        }
    }

    private void UpdateDashboardSettings()
    {
        DashboardToleranceValue.Text = _viewModel.Tolerance.ToString();
        DashboardClickDelayValue.Text = $"{_viewModel.ClickDelay.TotalSeconds:0.00} s";
        DashboardCooldownValue.Text = $"{_viewModel.Cooldown.TotalSeconds:0.00} s";
        PositionModeValue.Text = _viewModel.PositionLockEnabled ? "Usar punto fijo" : "Sigue el cursor";
        FixedPositionValue.Text = _viewModel.Settings.LockedPosition is { } point
            ? $"Punto fijo: ({point.X}, {point.Y})"
            : "Punto fijo: no definido";
    }

    private static string FormatMilliseconds(TimeSpan value) => $"{value.TotalMilliseconds:0.0} ms";

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string section) return;

        _viewModel.ActiveSection = section;
        SectionTitle.Text = section switch
        {
            "Settings" => "Ajustes",
            "Speed Test" => "Prueba de velocidad",
            "Help" => "Ayuda y guía rápida",
            _ => "Panel de control"
        };
        SectionSubtitle.Text = section switch
        {
            "Settings" => "Personaliza la sensibilidad y los tiempos de respuesta.",
            "Speed Test" => "Comprueba el tiempo entre la señal y la respuesta.",
            "Help" => "Todo lo necesario para empezar de forma segura.",
            _ => "Configura el objetivo y activa la detección cuando estés listo."
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
            ? new SolidColorBrush(Color.FromRgb(22, 59, 56))
            : Brushes.Transparent;
        if (button.Content is StackPanel panel && panel.Children.Count > 1)
        {
            if (panel.Children[0] is TextBlock icon) icon.Foreground = active ? activeBrush : inactiveBrush;
            if (panel.Children[1] is TextBlock label) label.Foreground = active ? Brushes.White : (Brush)new SolidColorBrush(Color.FromRgb(167, 182, 198));
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

        UpdateTriggerButton();
    }

    private void UpdateTriggerButton()
    {
        TriggerButton.Content = _viewModel.IsArmed ? "DETENER DETECTOR" : "INICIAR DETECTOR";
        TriggerButton.Style = (Style)FindResource(_viewModel.IsArmed ? "DangerButton" : "PrimaryButton");
        DetectorStatusText.Text = _viewModel.DetectorState switch
        {
            DetectorState.Armed or DetectorState.Running => "Detector activo",
            DetectorState.Faulted => "Error del detector",
            _ => "En espera"
        };
        DetectorStatusDot.Fill = (Brush)FindResource(_viewModel.DetectorState == DetectorState.Faulted
            ? "DangerBrush"
            : _viewModel.IsArmed ? "AccentBrush" : "TextMutedBrush");
    }

    private void StartDetection()
    {
        var previousTask = _detectionTask;
        _detectionCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _detectionCancellation = cancellation;
        _detectionTask = RunDetectionAsync(previousTask, cancellation);
    }

    private void StopDetection() => _detectionCancellation?.Cancel();

    private void StartSpamClicks()
    {
        if (!_viewModel.SpamEnabled)
        {
            _viewModel.SetStatus("Spam deshabilitado · actívalo desde Ajustes");
            return;
        }
        if (_spamTask is { IsCompleted: false }) return;

        _spamCancellation?.Dispose();
        _spamCancellation = new CancellationTokenSource();
        var cancellation = _spamCancellation;
        _viewModel.SetStatus($"Spam activo · mantén R · {_viewModel.SpamClickInterval.TotalMilliseconds:0} ms");
        _spamTask = Task.Run(() => RunSpamClicksAsync(cancellation));
    }

    private async Task RunSpamClicksAsync(CancellationTokenSource cancellation)
    {
        try
        {
            var interval = _viewModel.SpamClickInterval;
            while (!cancellation.IsCancellationRequested && _detectionRuntime.IsSpamKeyPressed)
            {
                _detectionRuntime.LeftClick();
                await Task.Delay(interval, cancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => _viewModel.SetStatus("Spam detenido · pulsa R para repetir"));
            if (ReferenceEquals(_spamCancellation, cancellation))
                _spamCancellation = null;
            cancellation.Dispose();
        }
    }

    private async Task RunDetectionAsync(Task? previousTask, CancellationTokenSource cancellation)
    {
        try
        {
            if (previousTask is not null)
            {
                try
                {
                    await previousTask;
                }
                catch (OperationCanceledException)
                {
                    // A previous worker may have been cancelled before it started.
                }
            }

            if (!cancellation.IsCancellationRequested)
            {
                var settings = _viewModel.Settings;
                await Task.Run(() => _detectionLoop.RunAsync(settings, _detectionRuntime, cancellation.Token));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _viewModel.MarkFault(exception);
                UpdateTriggerButton();
            });
            cancellation.Cancel();
        }
        finally
        {
            if (ReferenceEquals(_detectionCancellation, cancellation))
                _detectionCancellation = null;
            cancellation.Dispose();
        }
    }

    private void SelectColor_Click(object sender, RoutedEventArgs e)
    {
        _awaitingColorSelection = true;
        _awaitingPositionSelection = false;
        _viewModel.SetStatus("Selección de color preparada · sitúa el cursor y pulsa F6");
        ColorStatus.Text = "Esperando F6 para capturar";
        ColorStatus.Foreground = (Brush)FindResource("WarningBrush");
    }

    private void CaptureTargetColor()
    {
        try
        {
            var point = _detectionRuntime.GetCursorPosition();
            var color = _detectionRuntime.ReadPixel(point);
            _viewModel.SetTargetColor(color);
            // When fixed-position mode is enabled, F6 must capture the color and
            // its coordinate as one pair. Otherwise the detector may keep reading
            // an older point while the user is looking at the newly captured color.
            if (_viewModel.PositionLockEnabled)
                _viewModel.SetLockedPosition(point);
            _awaitingColorSelection = false;
            TargetColorValue.Text = _viewModel.TargetColorLabel;
            TargetColorSwatch.Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
            ColorStatus.Text = $"Capturado en ({point.X}, {point.Y})";
            ColorStatus.Foreground = (Brush)FindResource("AccentBrush");
        }
        catch (Exception exception)
        {
            _viewModel.SetStatus($"Error al capturar color: {exception.Message}");
            ColorStatus.Text = "Error al capturar";
            ColorStatus.Foreground = (Brush)FindResource("DangerBrush");
        }
    }

    private void FixPosition_Click(object sender, RoutedEventArgs e)
    {
        _awaitingPositionSelection = true;
        _awaitingColorSelection = false;
        _viewModel.SetStatus("Selección de posición preparada · sitúa el cursor y pulsa F7");
        PositionStatus.Text = "Esperando F7 para fijar";
    }

    private void RefreshCaptureDisplay()
    {
        TargetColorValue.Text = _viewModel.TargetColorLabel;
        ColorStatus.Text = _viewModel.TargetColorStatus;
        TargetColorSwatch.Background = _viewModel.TargetColor is { } color
            ? new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue))
            : new SolidColorBrush(Color.FromRgb(39, 52, 66));
        if (_viewModel.Settings.LockedPosition is { } point)
        {
            PositionValue.Text = $"({point.X}, {point.Y})";
            PositionStatus.Text = _viewModel.PositionLockEnabled ? "Punto fijo activo" : "Punto fijo guardado · modo dinámico";
        }
        UpdateDashboardSettings();
    }

    private void CaptureFixedPosition()
    {
        try
        {
            var point = _detectionRuntime.GetCursorPosition();
            _viewModel.SetLockedPosition(point);
            _awaitingPositionSelection = false;
            PositionValue.Text = $"({point.X}, {point.Y})";
            PositionStatus.Text = "Punto fijo guardado";
            InitializeSettingsDisplay();
            UpdateDashboardSettings();
        }
        catch (Exception exception)
        {
            _viewModel.SetStatus($"Error al capturar posición: {exception.Message}");
            PositionStatus.Text = "Error al capturar";
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
