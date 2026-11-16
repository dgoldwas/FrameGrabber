using System.IO;
using System.Text.Json;

namespace FrameGrabber;

/// <summary>What FrameGrabber remembers between launches, kept per Windows user in %LOCALAPPDATA%\FrameGrabber\settings.json.</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string Accent { get; set; } = "Butter";
    public string? ExportFolder { get; set; }

    static string Directory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FrameGrabber");
    static string FilePath => Path.Combine(Directory, "settings.json");

    public static AppSettings Load()
    {
        AppSettings settings;
        try
        {
            settings = File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new();
        }
        catch
        {
            settings = new();
        }

        // Earlier versions kept only the export folder, in a file of its own.
        if (settings.ExportFolder is null)
        {
            var legacy = Path.Combine(Directory, "export-folder.txt");
            try
            {
                if (File.Exists(legacy)) settings.ExportFolder = File.ReadAllText(legacy).Trim();
            }
            catch { }
        }
        return settings;
    }

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
