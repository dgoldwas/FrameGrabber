using System.Windows;
namespace FrameGrabber;
public partial class App : System.Windows.Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        Settings = AppSettings.Load();
        // The brushes must exist before the first window resolves them.
        Theme.Apply(Settings.Theme, Settings.Accent);
        base.OnStartup(e);
    }
}
