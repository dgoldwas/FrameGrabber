using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FrameGrabber;

internal static class MediaTools
{
    private const string ResourcePrefix = "FrameGrabber.Tools.";
    private static readonly Assembly Assembly = typeof(MediaTools).Assembly;

    public static bool AreBundled => Assembly.GetManifestResourceInfo(ResourcePrefix + "ffmpeg.exe") is not null
        && Assembly.GetManifestResourceInfo(ResourcePrefix + "ffprobe.exe") is not null;

    public static string Resolve(string name)
    {
        var fileName = name + ".exe";
        if (!AreBundled)
        {
            var adjacent = Path.Combine(AppContext.BaseDirectory, fileName);
            return File.Exists(adjacent) ? adjacent : name;
        }

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FrameGrabber", "tools", Assembly.ManifestModule.ModuleVersionId.ToString("N"));
        var destination = Path.Combine(directory, fileName);
        if (File.Exists(destination)) return destination;

        Directory.CreateDirectory(directory);
        using var resource = Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Bundled {fileName} could not be read.");
        var temporary = Path.Combine(directory, $"{fileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                resource.CopyTo(output);
            try { File.Move(temporary, destination); }
            catch (IOException) when (File.Exists(destination)) { }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return destination;
    }

    public static bool IsAvailable(string name)
    {
        if (AreBundled) return File.Exists(Resolve(name));
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, name + ".exe"))) return true;
        try
        {
            using var process = Process.Start(new ProcessStartInfo("where.exe", name)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(3000);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }
}
