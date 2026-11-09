using System.Text.Json;

namespace OsposDesktop.Core;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON. A missing or corrupt file yields defaults so the
/// app can always start and show its settings window.
/// </summary>
public sealed class SettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string FilePath { get; } = filePath;

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OSPOS Desktop",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions);
            return (settings ?? new AppSettings()).Sanitized();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Write then rename so a crash mid-save never leaves a truncated settings file.
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings.Sanitized(), JsonOptions));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
