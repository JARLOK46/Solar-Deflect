using System.Windows;
using System.Windows.Threading;
using TriggerBot.Logging;

namespace TriggerBot.App;

public partial class App : Application
{
    public App()
    {
        StartupLogger.Initialize();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        StartupLogger.Info("app_constructor_completed");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        StartupLogger.Info("startup_begin", $"args={string.Join(" ", e.Args)}");

        try
        {
            base.OnStartup(e);
            StartupLogger.Info("startup_base_completed");

            var window = new MainWindow
            {
                Visibility = Visibility.Visible,
                ShowActivated = true,
                WindowState = WindowState.Normal
            };

            MainWindow = window;
            StartupLogger.Info("main_window_created", $"title={window.Title}, visibility={window.Visibility}, state={window.WindowState}");
            window.Show();
            StartupLogger.Info("main_window_shown", $"handle={new System.Windows.Interop.WindowInteropHelper(window).Handle}");
            window.Activate();
            StartupLogger.Info("main_window_activated");
        }
        catch (Exception exception)
        {
            StartupLogger.Error("startup_failed", exception);
            ShowFatalError(exception);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StartupLogger.Error("dispatcher_unhandled_exception", e.Exception);
        e.Handled = true;
        ShowFatalError(e.Exception);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            StartupLogger.Error("unhandled_exception", exception);
        else
            StartupLogger.Info("unhandled_exception", e.ExceptionObject?.ToString());
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupLogger.Error("unobserved_task_exception", e.Exception);
        e.SetObserved();
    }

    private static void ShowFatalError(Exception exception)
    {
        try
        {
            MessageBox.Show(
                $"Solar Deflect no pudo iniciar.\n\nDetalle: {exception.Message}\n\nRegistro:\n{StartupLogger.LogPath}",
                "Solar Deflect - Error de inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // If WPF cannot display a dialog, the startup log remains available.
        }
    }
}
