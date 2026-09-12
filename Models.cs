using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FrameGrabber;
public sealed record ChapterInfo(string Title, TimeSpan Start, TimeSpan End) { public override string ToString() => Title; }
public sealed record VideoInfo(string FilePath, string FileName, TimeSpan Duration, int Width, int Height, string Codec, string PixelFormat, int BitDepth, string ColorSpace, bool IsHdr, bool IsDolbyVision, string Transfer, string Primaries, double FrameRate, IReadOnlyList<ChapterInfo> Chapters);
public sealed class QueueItem : INotifyPropertyChanged
{
    private string timecode;
    public QueueItem(string timecode) => this.timecode = timecode;
    public string Timecode { get => timecode; set { if (timecode == value) return; timecode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Timecode))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}
