using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FrameGrabber;
public sealed record ChapterInfo(string Title, TimeSpan Start, TimeSpan End) { public override string ToString() => Title; }
public sealed record VideoInfo(string FilePath, string FileName, TimeSpan Duration, int Width, int Height, string Codec, string PixelFormat, int BitDepth, string ColorSpace, bool IsHdr, bool IsDolbyVision, string Transfer, string Primaries, double FrameRate, IReadOnlyList<ChapterInfo> Chapters);
public sealed class QueueItem : INotifyPropertyChanged
{
    private string timecode;
    private string chapter = "";
    private System.Windows.Media.ImageSource? thumbnail;
    public QueueItem(string timecode) => this.timecode = timecode;
    public string Timecode { get => timecode; set => Set(ref timecode, value); }
    /// <summary>The chapter the frame was in when it was last shown, or empty.</summary>
    public string Chapter { get => chapter; set => Set(ref chapter, value); }
    /// <summary>The preview of the frame when it was last shown; none for frames read from a saved queue until they are shown.</summary>
    public System.Windows.Media.ImageSource? Thumbnail { get => thumbnail; set => Set(ref thumbnail, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string name = "") { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
}
