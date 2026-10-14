using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TriggerBot.Logging;

public static class StartupLogger
{
    private static readonly object Sync = new();
    private static string? _logPath;

    public static string LogPath => _logPath ?? Initialize();

    public static string Initialize()
    {
        if (_logPath is not null) return _logPath;

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TriggerBot", "logs");
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, "startup.log");
        Write("logger_initialized", new
        {
            process = Environment.ProcessPath,
            assembly = Assembly.GetEntryAssembly()?.Location,
            base_directory = AppContext.BaseDirectory,
            runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.OSArchitecture.ToString(),
            interactive = Environment.UserInteractive,
            session = Environment.GetEnvironmentVariable("SESSIONNAME")
        });
        return _logPath;
    }

    public static void Info(string eventName, string? message = null) =>
        Write(eventName, new { message });

    public static void Error(string eventName, Exception exception) =>
        Write(eventName, new
        {
            exception = exception.GetType().FullName,
            message = exception.Message,
            stack_trace = exception.ToString()
        });

    private static void Write(string eventName, object data)
    {
        try
        {
            lock (Sync)
            {
                var path = _logPath ?? throw new InvalidOperationException("Logger is not initialized.");
                var line = $"{DateTimeOffset.Now:O} | {eventName} | {System.Text.Json.JsonSerializer.Serialize(data)}{Environment.NewLine}";
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Diagnostics must never prevent the application from starting.
        }
    }
}
