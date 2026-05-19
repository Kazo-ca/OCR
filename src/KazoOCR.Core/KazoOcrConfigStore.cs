using System.Text.Json;

namespace KazoOCR.Core;

/// <summary>
/// Persists and loads <see cref="KazoOcrConfig"/> from disk.
/// </summary>
public interface IKazoOcrConfigStore
{
    /// <summary>Loads the configuration, returning defaults if the file does not exist.</summary>
    KazoOcrConfig Load();

    /// <summary>Saves the configuration to disk.</summary>
    void Save(KazoOcrConfig config);
}

/// <summary>
/// Default implementation that stores config in %APPDATA%\KazoOCR\config.json.
/// </summary>
public sealed class KazoOcrConfigStore : IKazoOcrConfigStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KazoOCR");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc />
    public KazoOcrConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new KazoOcrConfig();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<KazoOcrConfig>(json) ?? new KazoOcrConfig();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupted or unreadable config — start fresh
            return new KazoOcrConfig();
        }
    }

    /// <inheritdoc />
    public void Save(KazoOcrConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
