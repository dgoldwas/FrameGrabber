using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FrameGrabber;

/// <summary>
/// The menu bar's commands. Each carries its own shortcut, so the menu shows it and the window answers it; the arrow keys,
/// Page Up and Page Down stay in <see cref="MainWindow.Window_PreviewKeyDown"/>, since text boxes and lists need them too.
/// </summary>
public static class FrameCommands
{
    public static readonly RoutedUICommand New = Make("New", new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl+N"));
    public static readonly RoutedUICommand CloseProject = Make("Close project", new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl+W"));
    public static readonly RoutedUICommand OpenVideo = Make("Open video", new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl+O"));
    public static readonly RoutedUICommand OpenQueue = Make("Open queue", new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+O"));
    public static readonly RoutedUICommand SaveQueue = Make("Save queue", new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl+S"));
    public static readonly RoutedUICommand SaveQueueAs = Make("Save queue as", new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+S"));
    public static readonly RoutedUICommand ExportFrame = Make("Export this frame", new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl+E"));
    public static readonly RoutedUICommand ExportAll = Make("Export all queued", new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+E"));
    public static readonly RoutedUICommand ChooseFolder = Make("Choose export folder");
    public static readonly RoutedUICommand Exit = Make("Exit", new KeyGesture(Key.F4, ModifierKeys.Alt, "Alt+F4"));

    public static readonly RoutedUICommand Undo = Make("Undo", new KeyGesture(Key.Z, ModifierKeys.Control, "Ctrl+Z"));
    public static readonly RoutedUICommand Redo = Make("Redo", new KeyGesture(Key.Y, ModifierKeys.Control, "Ctrl+Y"), new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+Z"));
    public static readonly RoutedUICommand AddFrame = Make("Add this frame to the queue", new KeyGesture(Key.Enter, ModifierKeys.Control, "Ctrl+Enter"));
    public static readonly RoutedUICommand RemoveFrame = Make("Remove from the queue", new KeyGesture(Key.Delete, ModifierKeys.None, "Del"));
    public static readonly RoutedUICommand ClearQueue = Make("Clear the queue");
    public static readonly RoutedUICommand CopyFrame = Make("Copy frame", new KeyGesture(Key.C, ModifierKeys.Control, "Ctrl+C"));
    public static readonly RoutedUICommand CopyTimecode = Make("Copy timecode", new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+C"));
    public static readonly RoutedUICommand Settings = Make("Settings", new KeyGesture(Key.OemComma, ModifierKeys.Control, "Ctrl+,"));

    public static readonly RoutedUICommand GoToTimecode = Make("Go to timecode", new KeyGesture(Key.G, ModifierKeys.Control, "Ctrl+G"));
    public static readonly RoutedUICommand PreviousFrame = Make("Previous frame");
    public static readonly RoutedUICommand NextFrame = Make("Next frame");
    public static readonly RoutedUICommand Back1 = Make("Back 1 second");
    public static readonly RoutedUICommand Forward1 = Make("Forward 1 second");
    public static readonly RoutedUICommand Back5 = Make("Back 5 seconds");
    public static readonly RoutedUICommand Forward5 = Make("Forward 5 seconds");
    public static readonly RoutedUICommand PreviousChapter = Make("Previous chapter");
    public static readonly RoutedUICommand NextChapter = Make("Next chapter");

    public static readonly RoutedUICommand Shortcuts = Make("Keyboard shortcuts", new KeyGesture(Key.F1, ModifierKeys.None, "F1"));

    static RoutedUICommand Make(string text, params KeyGesture[] gestures) =>
        new(text, text.Replace(" ", ""), typeof(FrameCommands), new InputGestureCollection(gestures));
}

public partial class MainWindow
{
    /// <summary>The queue as it was before a change, so the change can be undone. Items are kept, so their thumbnails come back too.</summary>
    private sealed record QueueSnapshot(string Label, (QueueItem Item, string Timecode)[] Items);

    private readonly Stack<QueueSnapshot> undoHistory = new();
    private readonly Stack<QueueSnapshot> redoHistory = new();
    private QueueItem? editingItem;
    private string editingFrom = "";
    private string? queuePath;
    // The queue's timecodes as last saved or opened, to tell whether closing it would lose anything.
    private string[] savedQueue = [];
    // Raised by New, so a video still loading for the old project doesn't land in the new one.
    private int workspaceGeneration;
    private int thumbnailGeneration;

    private bool QueueUnsaved => queue.Count > 0 && !queue.Select(q => q.Timecode).SequenceEqual(savedQueue);

    private void MarkQueueSaved() => savedQueue = queue.Select(q => q.Timecode).ToArray();

    /// <summary>File › New and File › Close project: back to the window as it opens, with no video and an empty queue.</summary>
    private void ResetWorkspace()
    {
        if (!ConfirmDiscard()) return;
        workspaceGeneration++;
        previewGeneration++;
        thumbnailGeneration++;
        video = null;
        lastPreview = null;
        queue.Clear();
        undoHistory.Clear();
        redoHistory.Clear();
        queuePath = null;
        savedQueue = [];

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewMeta.Visibility = Visibility.Collapsed;
        PreviewHdrTag.Visibility = Visibility.Collapsed;
        DropHint.Visibility = Visibility.Visible;
        DropOutline.Visibility = Visibility.Visible;
        Backdrop.Source = null;
        FileNameText.Text = "No video loaded";
        FilePathText.Text = "";
        VideoChips.Visibility = Visibility.Collapsed;
        HdrBadge.Visibility = Visibility.Collapsed;
        ColorText.Text = "";
        suppress = true;
        Timeline.Value = 0;
        Timeline.Maximum = 1;
        suppress = false;
        CurrentTimeText.Text = DurationText.Text = TimeInput.Text = "00:00:00.00";
        ChapterPicker.ItemsSource = null;
        SetChapterControlsVisibility(false);
        ChapterNameText.Text = "";
        TransportPanel.IsEnabled = false;
        ShowTitle();
        UpdateQueueChrome();
        StatusText.Text = "Drop a video anywhere, or choose File › Open video.";
    }

    /// <summary>Offers to save a queue with unsaved changes before it is closed. False means stay: Cancel, or the save didn't happen.</summary>
    private bool ConfirmDiscard()
    {
        if (!QueueUnsaved) return true;
        var answer = System.Windows.MessageBox.Show(this, "Save the frame queue before closing it?", "FrameGrabber", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel) return false;
        if (answer == MessageBoxResult.No) return true;
        SaveQueue(saveAs: false);
        return !QueueUnsaved;
    }

    /// <summary>Renders a small thumbnail, in the background, for each queued frame that has none, such as those read from a saved queue.</summary>
    private async Task RefreshThumbnails()
    {
        if (video is not { } current) return;
        var generation = ++thumbnailGeneration;
        foreach (var item in queue.Where(q => q.Thumbnail is null).ToList())
        {
            if (!TryResolveFrame(item.Timecode, out var frame)) continue;
            var tmp = Path.Combine(Path.GetTempPath(), $"framegrabber-thumb-{Guid.NewGuid():N}.png");
            try
            {
                await Task.Run(() => media.Extract(current.FilePath, TimeOfFrame(frame), tmp, 160, current.IsHdr));
                var bytes = await File.ReadAllBytesAsync(tmp);
                if (generation != thumbnailGeneration || video != current) return;
                item.Thumbnail = DecodePng(bytes);
                item.Chapter = ChapterAt(TimeOfFrame(frame));
            }
            catch
            {
                // A frame FFmpeg can't render keeps the empty thumbnail; Show still reports the problem.
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }

    private static BitmapImage DecodePng(byte[] bytes)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(bytes);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void WireCommands()
    {
        bool loaded() => video is not null;
        bool hasChapters() => video is { Chapters.Count: > 0 };

        Bind(FrameCommands.New, ResetWorkspace, () => loaded() || queue.Count > 0);
        Bind(FrameCommands.CloseProject, ResetWorkspace, () => loaded() || queue.Count > 0);
        Bind(FrameCommands.OpenVideo, () => Browse_Click(this, new RoutedEventArgs()));
        Bind(FrameCommands.OpenQueue, () => ImportQueue_Click(this, new RoutedEventArgs()));
        Bind(FrameCommands.SaveQueue, () => SaveQueue(saveAs: false), () => queue.Count > 0);
        Bind(FrameCommands.SaveQueueAs, () => SaveQueue(saveAs: true), () => queue.Count > 0);
        Bind(FrameCommands.ExportFrame, () => Export_Click(this, new RoutedEventArgs()), loaded);
        Bind(FrameCommands.ExportAll, () => ExportAll_Click(this, new RoutedEventArgs()), () => loaded() && queue.Count > 0);
        Bind(FrameCommands.ChooseFolder, () => ChooseExportFolder());
        Bind(FrameCommands.Exit, Close);

        Bind(FrameCommands.Undo, () => StepHistory(redo: false), () => undoHistory.Count > 0);
        Bind(FrameCommands.Redo, () => StepHistory(redo: true), () => redoHistory.Count > 0);
        Bind(FrameCommands.AddFrame, () => AddQueue_Click(this, new RoutedEventArgs()), loaded);
        Bind(FrameCommands.RemoveFrame, () => { if (QueueList.SelectedItem is QueueItem q) RemoveQueued(q); }, () => QueueList.SelectedItem is QueueItem);
        Bind(FrameCommands.ClearQueue, () => ClearQueue_Click(this, new RoutedEventArgs()), () => queue.Count > 0);
        Bind(FrameCommands.CopyFrame, async () => await CopyFrame(), loaded);
        Bind(FrameCommands.CopyTimecode, CopyTimecode, loaded);
        Bind(FrameCommands.Settings, ShowSettings);

        Bind(FrameCommands.GoToTimecode, () => { TimeInput.Focus(); TimeInput.SelectAll(); }, loaded);
        Bind(FrameCommands.PreviousFrame, async () => await StepFrame(-1), loaded);
        Bind(FrameCommands.NextFrame, async () => await StepFrame(1), loaded);
        Bind(FrameCommands.Back1, async () => await GoTo(TimeSpan.FromSeconds(Timeline.Value - 1)), loaded);
        Bind(FrameCommands.Forward1, async () => await GoTo(TimeSpan.FromSeconds(Timeline.Value + 1)), loaded);
        Bind(FrameCommands.Back5, async () => await GoTo(TimeSpan.FromSeconds(Timeline.Value - 5)), loaded);
        Bind(FrameCommands.Forward5, async () => await GoTo(TimeSpan.FromSeconds(Timeline.Value + 5)), loaded);
        Bind(FrameCommands.PreviousChapter, () => ChapterBack_Click(this, new RoutedEventArgs()), hasChapters);
        Bind(FrameCommands.NextChapter, () => ChapterForward_Click(this, new RoutedEventArgs()), hasChapters);

        Bind(FrameCommands.Shortcuts, () => new ShortcutsWindow { Owner = this }.ShowDialog());
    }

    private void Bind(RoutedCommand command, Action run, Func<bool>? canRun = null) =>
        CommandBindings.Add(new CommandBinding(command,
            (_, e) => { e.Handled = true; run(); },
            (_, e) => { e.Handled = true; e.CanExecute = canRun?.Invoke() ?? true; }));

    // Edit names what Undo and Redo would change.
    private void EditMenu_SubmenuOpened(object s, RoutedEventArgs e)
    {
        UndoMenuItem.Header = undoHistory.TryPeek(out var undo) ? $"_Undo {undo.Label}" : "_Undo";
        RedoMenuItem.Header = redoHistory.TryPeek(out var redo) ? $"_Redo {redo.Label}" : "_Redo";
    }

    /// <summary>Call before changing the queue.</summary>
    private void RecordQueue(string label)
    {
        undoHistory.Push(Snapshot(label));
        redoHistory.Clear();
    }

    private QueueSnapshot Snapshot(string label) => new(label, queue.Select(q => (q, q.Timecode)).ToArray());

    private void StepHistory(bool redo)
    {
        var from = redo ? redoHistory : undoHistory;
        if (!from.TryPop(out var step)) return;
        (redo ? undoHistory : redoHistory).Push(Snapshot(step.Label));
        queue.Clear();
        foreach (var (item, timecode) in step.Items)
        {
            item.Timecode = timecode;
            queue.Add(item);
        }
        StatusText.Text = $"{(redo ? "Redid" : "Undid")} {step.Label}";
    }

    private void RemoveQueued(QueueItem item)
    {
        var index = queue.IndexOf(item);
        if (index < 0) return;
        RecordQueue("remove frame");
        queue.RemoveAt(index);
        if (queue.Count > 0) QueueList.SelectedIndex = Math.Min(index, queue.Count - 1);
    }

    // A timecode typed into a queue row is one change for undo, recorded when the row's box loses focus.
    private void QueueTimecode_GotFocus(object s, KeyboardFocusChangedEventArgs e)
    {
        if (((FrameworkElement)s).DataContext is not QueueItem item) return;
        editingItem = item;
        editingFrom = item.Timecode;
        QueueList.SelectedItem = item;
    }

    private void QueueTimecode_LostFocus(object s, KeyboardFocusChangedEventArgs e)
    {
        if (editingItem is not { } item || item.Timecode == editingFrom) return;
        undoHistory.Push(new QueueSnapshot("edit timecode", queue.Select(q => (q, q == item ? editingFrom : q.Timecode)).ToArray()));
        redoHistory.Clear();
        editingItem = null;
    }

    private async void QueueTimecode_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || ((FrameworkElement)s).DataContext is not QueueItem item) return;
        e.Handled = true;
        await ShowQueued(item);
    }

    private void SaveQueue(bool saveAs)
    {
        if (saveAs || queuePath is null)
        {
            var title = video is null ? "video" : Path.GetFileNameWithoutExtension(video.FileName);
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "FrameGrabber queue|*.json", FileName = queuePath is null ? $"{title}-framegrab.json" : Path.GetFileName(queuePath) };
            if (dialog.ShowDialog(this) != true) return;
            queuePath = dialog.FileName;
        }
        try
        {
            File.WriteAllText(queuePath, System.Text.Json.JsonSerializer.Serialize(new { Video = video?.FilePath, Timecodes = queue.Select(x => x.Timecode) }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            MarkQueueSaved();
            StatusText.Text = $"Saved the queue to {Path.GetFileName(queuePath)}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Couldn't save the queue", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Copies the frame at the playhead, at full resolution, as an image other apps can paste.</summary>
    private async Task CopyFrame()
    {
        if (video is not { } current) return;
        var frame = CurrentFrame();
        var tmp = Path.Combine(Path.GetTempPath(), $"framegrabber-copy-{Guid.NewGuid():N}.png");
        try
        {
            StatusText.Text = "Copying frame…";
            await Task.Run(() => media.Extract(current.FilePath, TimeOfFrame(frame), tmp, null, current.IsHdr));
            var bytes = await File.ReadAllBytesAsync(tmp);
            var image = DecodePng(bytes);
            var data = new System.Windows.DataObject();
            data.SetImage(image);
            // Apps that understand PNG keep its exact pixels; the rest take the bitmap.
            data.SetData("PNG", new MemoryStream(bytes));
            System.Windows.Clipboard.SetDataObject(data, true);
            StatusText.Text = $"Copied {FormatFrameIndex(frame)} ({current.Width} × {current.Height})";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Couldn't copy the frame: " + ex.Message;
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    private void CopyTimecode()
    {
        var timecode = FormatFrameIndex(CurrentFrame());
        try
        {
            System.Windows.Clipboard.SetText(timecode);
            StatusText.Text = $"Copied {timecode}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Couldn't copy the timecode: " + ex.Message;
        }
    }

    private void ShowSettings() => new SettingsWindow(this) { Owner = this }.ShowDialog();

    private void About_Click(object s, RoutedEventArgs e) =>
        System.Windows.MessageBox.Show(this, $"FrameGrabber {UpdateService.CurrentVersion}\n\nPrecision stills from motion.\nDeveloped by Warm Butter Design.", "About FrameGrabber", MessageBoxButton.OK, MessageBoxImage.Information);

    /// <summary>Asks for a new export folder and remembers it. Returns whether it changed.</summary>
    internal bool ChooseExportFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose where PNGs are saved", InitialDirectory = folder };
        if (dialog.ShowDialog(this) != true) return false;
        folder = dialog.FolderName;
        FolderText.Text = folder;
        App.Settings.ExportFolder = folder;
        App.Settings.Save();
        return true;
    }

    internal string ExportFolder => folder;
}
