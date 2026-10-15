using System.Text.Json;
using System.Text.Json.Serialization;
using TriggerBot.Core.Detection;

namespace TriggerBot.Configuration;

public sealed class DetectionConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DetectionConfigurationStore(string? path = null)
    {
        FilePath = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TriggerBot", "config.json");
    }

    public string FilePath { get; }

    public DetectionSettings LoadOrDefault()
    {
        if (!File.Exists(FilePath)) return DetectionSettings.Default;

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<DetectionSettings>(json, JsonOptions)
                ?? DetectionSettings.Default;
            settings.Validate();
            return settings;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or ArgumentException
            or InvalidOperationException)
        {
            return DetectionSettings.Default;
        }
    }

    public void Save(DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Configuration path has no directory.");
        Directory.CreateDirectory(directory);

        // A unique sibling temp file avoids collisions between concurrent saves and
        // ensures the final rename stays on the same volume.
        var temporaryPath = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
