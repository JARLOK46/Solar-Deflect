using System.IO;
using TriggerBot.Configuration;
using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class DetectionConfigurationStoreTests
{
    [Fact]
    public void SavesAndLoadsDetectionSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), "TriggerBotTests", Guid.NewGuid().ToString("N"), "config.json");
        var settings = DetectionSettings.Default with
        {
            TargetColor = new RgbColor(12, 34, 56),
            Tolerance = 18,
            LockedPosition = new ScreenPoint(123, 456),
            PositionLockEnabled = true
        };

        try
        {
            var store = new DetectionConfigurationStore(path);
            store.Save(settings);
            var loaded = store.LoadOrDefault();

            Assert.Equal(settings, loaded);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CorruptConfigurationFallsBackToDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "TriggerBotTests", Guid.NewGuid().ToString("N"), "config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ definitely-not-json }");

        try
        {
            var loaded = new DetectionConfigurationStore(path).LoadOrDefault();

            Assert.Equal(DetectionSettings.Default, loaded);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
