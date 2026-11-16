using System.Windows;

namespace FrameGrabber;

/// <summary>Help › Keyboard shortcuts (F1).</summary>
public partial class ShortcutsWindow : Window
{
    public sealed record Shortcut(string Action, string Keys);
    public sealed record Section(string Title, Shortcut[] Keys);

    public ShortcutsWindow()
    {
        InitializeComponent();
        WindowFrame.Attach(this, Root, null, null, CloseButton, 52);
        Sections.ItemsSource = new Section[]
        {
            new("MOVING AROUND",
            [
                new("Previous or next frame", "Left / Right"),
                new("Back or forward 1 second", "Shift+Left / Right"),
                new("Back or forward 5 seconds", "Ctrl+Left / Right"),
                new("Previous or next chapter", "PgUp / PgDn"),
                new("Go to a timecode", "Ctrl+G"),
            ]),
            new("FRAMES AND THE QUEUE",
            [
                new("Export this frame as a PNG", "Ctrl+E"),
                new("Add this frame to the queue", "Ctrl+Enter"),
                new("Remove the selected queued frame", "Del"),
                new("Export every queued frame", "Ctrl+Shift+E"),
                new("Copy this frame", "Ctrl+C"),
                new("Copy this timecode", "Ctrl+Shift+C"),
                new("Undo or redo a queue change", "Ctrl+Z / Ctrl+Y"),
            ]),
            new("FILES AND SETTINGS",
            [
                new("New, or close the project", "Ctrl+N / Ctrl+W"),
                new("Open a video", "Ctrl+O"),
                new("Open or save a queue", "Ctrl+Shift+O / Ctrl+S"),
                new("Settings", "Ctrl+,"),
            ]),
        };
    }

    private void Close_Click(object s, RoutedEventArgs e) => Close();
}
