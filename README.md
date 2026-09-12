# FrameGrabber

FrameGrabber is a focused Windows desktop utility for finding and exporting exact still frames from video. Drop in a video, scrub to the moment you need, confirm the frame in the preview, and export a native-resolution PNG.

The interface is designed for editors, creators, archivists, and anyone who needs a clean reference image from a video without opening a full NLE.

## Features

- Drag and drop video files directly onto the app.
- Browse for files when drag and drop is not convenient.
- Supports common FFmpeg-readable containers, including MKV, MP4, MOV, WebM, AVI, M4V, and transport streams.
- Scrub through the video with a timeline.
- Step forward or backward one source frame at a time.
- Jump between embedded video chapters when chapter metadata is available.
- Enter an exact frame timecode in `HH:MM:SS.frames` format, or enter seconds such as `93.5`.
- Automatically render a preview for the selected moment.
- Export a PNG at the source video's native resolution.
- Use a stable, recreation-friendly filename such as `scene-00-01-23.11.png`.
- Detect and display source resolution, codec, pixel format, color depth, color space, transfer, and primaries.
- Detect HDR10/PQ, HLG, wide-gamut BT.2020, and Dolby Vision metadata when FFprobe exposes it.
- Tone-map HDR and Dolby Vision frames to SDR BT.709 for both the preview and exported PNG.
- Choose a default export folder. The choice persists between launches.
- Build a queue of frames, edit queued timecodes, preview queue items, remove or reorder items, and batch-export them.
- Save queues as JSON and import them later to restore the source video and exact frame list.
- Modern dark WPF interface with a deliberately small, distraction-free workflow.
- Custom multi-resolution Windows application icon based on a film frame and capture aperture.

## Requirements

- Windows 10 or later.
- .NET 8 Desktop Runtime, unless using the framework-dependent build through `dotnet run`.
- FFmpeg and FFprobe available on the system `PATH`.

FrameGrabber uses the `ffmpeg` and `ffprobe` command-line tools rather than embedding a media library. This keeps the application small and lets FFmpeg provide broad codec and container support.

## Installing FFmpeg

Install an FFmpeg Windows build that includes both `ffmpeg.exe` and `ffprobe.exe`, then add its `bin` directory to your system or user `PATH`.

Verify the installation from PowerShell:

```powershell
ffmpeg -version
ffprobe -version
```

If either command is not recognized, restart FrameGrabber after updating `PATH`.

On startup, FrameGrabber checks for both tools. If either is missing, it offers to install the FFmpeg Essentials package automatically with `winget`. The installation requires Windows Package Manager to be available and may require restarting FrameGrabber before the refreshed `PATH` is visible.

## Running from source

Clone the repository, open a PowerShell window in the project folder, and run:

```powershell
dotnet restore
dotnet run
```

To create a Release build:

```powershell
dotnet publish -c Release --no-restore
```

The published executable is written to:

```text
bin\Release\net8.0-windows\publish\FrameGrabber.exe
```

## Using FrameGrabber

1. Launch FrameGrabber.
2. Drop a video onto the drop zone, or select **Browse files**.
3. Review the metadata in the **Video Details** panel.
4. Move the timeline, or enter a timecode and press **Enter**.
5. Check the preview image. HDR and Dolby Vision sources are labeled and shown through the SDR tone-map.
6. Choose an export location with **Choose** if needed.
7. Select **Export PNG**.

### Building a frame queue

Select **Add current frame** whenever the preview is on a frame you want to keep. Each queue row can be edited directly, previewed, moved up or down, or removed. **Export all PNGs** batch-renders the queue to the selected export folder. **Save queue** writes a portable JSON file containing the source path and timecodes; **Import** restores it for later re-export.

The frame and chapter buttons use the video's reported frame rate and chapter metadata. If a file has no chapters, the chapter controls remain inactive.

Exports are saved as:

```text
original-filename-HH-MM-SS.frames.png
```

Colons are replaced with hyphens so the filename is valid on Windows and remains easy to map back to the original timecode.

## HDR and Dolby Vision handling

FrameGrabber reads stream metadata through FFprobe. When a source is identified as HDR or Dolby Vision, the frame is converted through an FFmpeg linear-light tone-map and output in SDR BT.709 RGB PNG format. This means the exported PNG is suitable for normal SDR applications and displays.

HDR metadata detection depends on what is present in the source stream. A file with missing or stripped metadata may be reported as SDR even if its origin was HDR.

Regular SDR sources are exported without tone-mapping. A 10-bit SDR source remains treated as SDR when its transfer and primaries indicate SDR.

## Project structure

```text
FrameGrabber.csproj   WPF/.NET project definition
App.xaml              Shared colors and control styles
MainWindow.xaml       Main application layout
MainWindow.xaml.cs    UI events and application workflow
MediaService.cs       FFprobe metadata and FFmpeg frame extraction
Models.cs             Video metadata model
```

## Versioning and releases

FrameGrabber uses date-based release versions in the format `vYYYY.MM.DD`. If another release is made on the same date, use a variant suffix such as `v2026.09.12.1`, then `v2026.09.12.2`.

## Version log

### v2026.09.12.1

- Added frame-forward and frame-backward navigation.
- Added one-second and five-second seek controls.
- Added chapter navigation and automatic chapter-selector synchronization.
- Added editable frame queues with preview, remove, reorder, import, export, clear, and batch PNG export actions.
- Added direct timeline click seeking.
- Added a custom Windows application icon and refined dark ComboBox styling.
- Added a persistent default export folder.

### v2026.09.12

- Initial FrameGrabber release.
- Added drag-and-drop video loading, FFmpeg metadata inspection, frame preview, HDR/Dolby Vision SDR tone-mapping, native-resolution PNG export, and timecode-based filenames.

## License

No license has been selected yet. Until a license file is added, all rights are reserved by the repository owner.
