using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using TriggerBot.App.ViewModels;
using TriggerBot.Core.Detection;
using TriggerBot.Windows.Input;

namespace TriggerBot.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private readonly DetectionLoop _detectionLoop = new();
    private readonly WindowsDetectionRuntime _detectionRuntime = new();
    private CancellationTokenSource? _detectionCancellation;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
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
                SelectColor_Click(this, new RoutedEventArgs());
                break;
            case TriggerHotkey.FixPosition:
                FixPosition_Click(this, new RoutedEventArgs());
                break;
        }
    }

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
        _viewModel.SetStatus("Color selection armed · press F6 over the target pixel");
        ColorStatus.Text = "Waiting for F6";
        ColorStatus.Foreground = (Brush)FindResource("WarningBrush");
    }

    private void FixPosition_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetStatus("Position captured · (959, 571)");
        PositionValue.Text = "(959, 571)";
        PositionStatus.Text = "Fixed position ready";
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
