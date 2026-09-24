using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace FrameGrabber;

public sealed class MediaService
{
    private static string Run(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(MediaTools.Resolve(file)) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {file}.");
        var output = p.StandardOutput.ReadToEnd(); var error = p.StandardError.ReadToEnd(); p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException(error.Trim().Length > 0 ? error.Trim() : $"{file} failed.");
        return output;
    }

    public VideoInfo Probe(string path)
    {
        var json = Run("ffprobe", "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", "-show_chapters", path);
        using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        var stream = root.GetProperty("streams").EnumerateArray().First(s => s.TryGetProperty("codec_type", out var t) && t.GetString() == "video");
        var fmt = root.TryGetProperty("format", out var f) ? f : default;
        var duration = ParseDouble(stream, "duration") ?? ParseDouble(fmt, "duration") ?? 0;
        var pix = Get(stream, "pix_fmt"); var transfer = Get(stream, "color_transfer"); var primaries = Get(stream, "color_primaries"); var space = Get(stream, "color_space");
        var sideData = stream.TryGetProperty("side_data_list", out var sd) ? sd.ToString() : "";
        var dv = sideData.Contains("DOVI", StringComparison.OrdinalIgnoreCase) || sideData.Contains("Dolby Vision", StringComparison.OrdinalIgnoreCase);
        var hdr = dv || transfer is "smpte2084" or "arib-std-b67" || primaries is "bt2020" or "smpte431" or "smpte432";
        var depth = pix.Contains("12") ? 12 : pix.Contains("10") ? 10 : 8;
        var chapters = new List<ChapterInfo>();
        if (root.TryGetProperty("chapters", out var chapterJson)) foreach (var c in chapterJson.EnumerateArray())
        {
            var start = ParseDouble(c, "start_time") ?? 0; var end = ParseDouble(c, "end_time") ?? start;
            var title = c.TryGetProperty("tags", out var tags) ? Get(tags, "title") : "";
            chapters.Add(new ChapterInfo(string.IsNullOrWhiteSpace(title) ? $"Chapter {chapters.Count + 1}" : title, TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end)));
        }
        return new VideoInfo(path, Path.GetFileName(path), TimeSpan.FromSeconds(duration), Int(stream, "width"), Int(stream, "height"), Get(stream,"codec_name").ToUpperInvariant(), pix, depth, space, hdr, dv, transfer, primaries, ParseRate(Get(stream, "avg_frame_rate")), chapters);
    }

    public void Extract(string input, TimeSpan timestamp, string output, int? previewWidth = null, bool toneMap = false)
    {
        var vf = toneMap ? "zscale=t=linear:npl=100,format=gbrpf32le,tonemap=mobius:desat=0,zscale=p=bt709:t=bt709:m=bt709:r=tv,format=rgb24" : "format=rgb24";
        if (previewWidth is not null) vf += $",scale={previewWidth}:-2:flags=lanczos";
        Run("ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-ss", timestamp.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture), "-i", input, "-frames:v", "1", "-vf", vf, "-c:v", "png", output);
    }
    private static string Get(JsonElement e, string key) => e.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";
    private static int Int(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.TryGetInt32(out var n) ? n : 0;
    private static double? ParseDouble(JsonElement e, string key) => e.ValueKind != JsonValueKind.Undefined && e.TryGetProperty(key, out var v) && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;
    private static double ParseRate(string value) { var p = value.Split('/'); return p.Length == 2 && double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d != 0 ? n / d : 24; }
}
