using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helpers.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON. Settings live under the
/// user's roaming app data; models and other large files do not belong here.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly object _sync = new();

    public SettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultPath();
    }

    public string FilePath { get; }

    public AppSettings Current { get; private set; } = new();

    public static string DefaultFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), HelpersInfo.DataFolderName);

    public static string DefaultPath() => Path.Combine(DefaultFolder(), "settings.json");

    public static string DefaultModelsFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), HelpersInfo.DataFolderName, "models");

    /// <summary>Reads the file. A missing or unreadable file gives defaults, never an exception.</summary>
    public AppSettings Load()
    {
        lock (_sync)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch (JsonException)
            {
                Current = new AppSettings();
            }
            catch (IOException)
            {
                Current = new AppSettings();
            }

            return Current;
        }
    }

    /// <summary>Writes the current settings. Writes to a temp file first so a crash can't leave half a file.</summary>
    public void Save()
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Current, JsonOptions));
            File.Move(temp, FilePath, overwrite: true);
        }
    }

    /// <summary>Applies a change and saves in one step.</summary>
    public void Update(Action<AppSettings> change)
    {
        lock (_sync)
        {
            change(Current);
            Save();
        }
    }
}
