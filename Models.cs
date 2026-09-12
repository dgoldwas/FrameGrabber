namespace FrameGrabber;
public sealed record VideoInfo(string FilePath, string FileName, TimeSpan Duration, int Width, int Height, string Codec, string PixelFormat, int BitDepth, string ColorSpace, bool IsHdr, bool IsDolbyVision, string Transfer, string Primaries);
