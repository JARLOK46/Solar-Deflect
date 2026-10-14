using System.IO;
using TriggerBot.Logging;

namespace TriggerBot.Tests;

public sealed class StartupLoggerTests
{
    [Fact]
    public void InitializeCreatesLogFileInLocalApplicationData()
    {
        var path = StartupLogger.Initialize();

        Assert.True(Path.IsPathFullyQualified(path));
        Assert.Equal("startup.log", Path.GetFileName(path));
        Assert.True(File.Exists(path));
        Assert.Contains(Path.Combine("TriggerBot", "logs"), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InfoWritesStructuredStartupEvent()
    {
        var path = StartupLogger.Initialize();
        var marker = $"test_marker_{Guid.NewGuid():N}";

        StartupLogger.Info("test_event", marker);

        var log = File.ReadAllText(path);
        Assert.Contains("test_event", log);
        Assert.Contains(marker, log);
    }
}
