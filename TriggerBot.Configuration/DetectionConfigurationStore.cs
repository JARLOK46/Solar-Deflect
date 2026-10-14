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
        try
        {
            if (!File.Exists(FilePath)) return DetectionSettings.Default;
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<DetectionSettings>(json, JsonOptions)
                ?? DetectionSettings.Default;
            settings.Validate();
            return settings;
        }
        catch (Exception) when (File.Exists(FilePath))
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

        var temporaryPath = FilePath + ".tmp";
        var backupPath = FilePath + ".bak";
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(temporaryPath, json);

        if (File.Exists(FilePath))
            File.Replace(temporaryPath, FilePath, backupPath, ignoreMetadataErrors: true);
        else
            File.Move(temporaryPath, FilePath);
    }
}
