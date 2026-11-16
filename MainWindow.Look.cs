using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FrameGrabber;

// The parts of the window that follow the video rather than the controls: the backdrop's tint, the marks on the timeline
// and the title bar's text.
public partial class MainWindow
{
    // A handful of pixels, so the tint is broad washes of color with none of the picture's detail.
    private const int TintWidth = 8;

    /// <summary>Tints the window from <paramref name="png"/>: the frame shrunk to a few pixels and stretched across the window.</summary>
    private void TintBackdrop(byte[] png)
    {
        try
        {
            var tint = new BitmapImage();
            tint.BeginInit();
            tint.CacheOption = BitmapCacheOption.OnLoad;
            tint.DecodePixelWidth = TintWidth;
            tint.StreamSource = new MemoryStream(png);
            tint.EndInit();
            tint.Freeze();
            Backdrop.Source = tint;
        }
        catch
        {
            Backdrop.Source = null;
        }
    }

    private void ShowTitle()
    {
        if (video is not { } v)
        {
            TitleName.Text = "FrameGrabber";
            TitleMeta.Text = "";
            Title = "FrameGrabber";
            return;
        }
        TitleName.Text = v.FileName;
        TitleMeta.Text = $"{v.Width} × {v.Height} · {v.Codec} {v.BitDepth}-bit · {FormatRate(v.FrameRate)} fps · {FormatFrame(v.Duration)}";
        Title = $"{v.FileName} - FrameGrabber";
    }

    private static string FormatRate(double fps) => Math.Abs(fps - Math.Round(fps)) < 0.001 ? $"{fps:0}" : $"{fps:0.###}";

    /// <summary>Redraws the timeline's marks: a tick where each chapter starts and a diamond for each queued frame.</summary>
    private void DrawTimelineMarks()
    {
        TimelineMarks.Children.Clear();
        var hasQueue = video is not null && queue.Count > 0;
        QueueLegend.Visibility = hasQueue ? Visibility.Visible : Visibility.Collapsed;
        if (video is not { } v || v.Duration.TotalSeconds <= 0 || TimelineMarks.ActualWidth <= 0) return;

        // The thumb's centre runs from half its width in from each end, so the marks do too.
        const double inset = 9;
        var span = Math.Max(TimelineMarks.ActualWidth - inset * 2, 1);
        var middle = TimelineMarks.ActualHeight / 2;
        double X(TimeSpan t) => inset + Math.Clamp(t.TotalSeconds / v.Duration.TotalSeconds, 0, 1) * span;

        foreach (var chapter in v.Chapters.Skip(1))
        {
            var tick = new Rectangle { Width = 1, Height = 12 };
            tick.SetResourceReference(Shape.FillProperty, "FaintBrush");
            Canvas.SetLeft(tick, Math.Round(X(chapter.Start)));
            Canvas.SetTop(tick, middle - 6);
            TimelineMarks.Children.Add(tick);
        }

        foreach (var item in queue)
        {
            if (!TryResolveFrame(item.Timecode, out var frame)) continue;
            var diamond = new Rectangle { Width = 7, Height = 7, RenderTransform = new RotateTransform(45, 3.5, 3.5) };
            diamond.SetResourceReference(Shape.FillProperty, "AccentInkBrush");
            Canvas.SetLeft(diamond, X(TimeOfFrame(frame)) - 3.5);
            Canvas.SetTop(diamond, middle - 14);
            TimelineMarks.Children.Add(diamond);
        }
    }

    private string ChapterAt(TimeSpan time) =>
        video?.Chapters.LastOrDefault(c => c.Start.TotalSeconds <= time.TotalSeconds + 0.001)?.Title ?? "";
}
