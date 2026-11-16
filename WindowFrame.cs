using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Path = System.Windows.Shapes.Path;

namespace FrameGrabber;

/// <summary>
/// Merges the title bar into the window's own top bar, as VideoTrimmer does. WPF's <see cref="WindowChrome"/> keeps the
/// native frame, so resizing, snapping, the shadow and the system menu stay Windows' own. On top of that this:
/// answers hit tests over the maximize button as HTMAXBUTTON so Windows 11 shows snap layouts there; pads the content
/// while maximized, when the window hangs past the screen by its frame; and colors the border to match the theme.
/// </summary>
public static class WindowFrame
{
    const int WM_NCHITTEST = 0x0084;
    const int WM_NCMOUSEMOVE = 0x00A0;
    const int WM_NCLBUTTONDOWN = 0x00A1;
    const int WM_NCLBUTTONUP = 0x00A2;
    const int WM_NCLBUTTONDBLCLK = 0x00A3;
    const int WM_NCMOUSELEAVE = 0x02A2;
    const int HTMAXBUTTON = 9;

    public static void Attach(Window window, FrameworkElement root, Button? minimize, Button? maximize, Button close, double captionHeight)
    {
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = captionHeight,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
        });

        if (minimize is not null) minimize.Click += (_, _) => window.WindowState = WindowState.Minimized;
        if (maximize is not null) maximize.Click += (_, _) => ToggleMaximized(window);
        close.Click += (_, _) => window.Close();

        void Pad()
        {
            root.Margin = window.WindowState == WindowState.Maximized ? MaximizedInset(window) : new Thickness(0);
            if (maximize?.Content is Path glyph)
                glyph.Data = Geometry.Parse(window.WindowState == WindowState.Maximized ? "M2.5,0.5 H9.5 V7.5 M0.5,2.5 H7.5 V9.5 H0.5 Z" : "M0.5,0.5 H9.5 V9.5 H0.5 Z");
        }

        window.StateChanged += (_, _) => Pad();
        window.SourceInitialized += (_, _) =>
        {
            Pad();
            Recolor(window);
            if (maximize is not null) HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(SnapHook(window, maximize));
        };
        Action recolor = () => Recolor(window);
        Theme.Changed += recolor;
        window.Closed += (_, _) => Theme.Changed -= recolor;
    }

    public static void ToggleMaximized(Window window) =>
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    static HwndSourceHook SnapHook(Window window, Button maximize)
    {
        var pressed = false;
        return (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            switch (msg)
            {
                case WM_NCHITTEST when maximize.IsVisible && Over(maximize, lParam):
                    handled = true;
                    return HTMAXBUTTON;
                case WM_NCMOUSEMOVE:
                    if (wParam != HTMAXBUTTON) pressed = false;
                    maximize.Tag = wParam == HTMAXBUTTON ? pressed ? "pressed" : "hover" : null;
                    break;
                case WM_NCMOUSELEAVE:
                    pressed = false;
                    maximize.Tag = null;
                    break;
                case WM_NCLBUTTONDOWN or WM_NCLBUTTONDBLCLK when wParam == HTMAXBUTTON:
                    // Handled here, or Windows would draw and track its own old-style button.
                    pressed = true;
                    maximize.Tag = "pressed";
                    handled = true;
                    break;
                case WM_NCLBUTTONUP when wParam == HTMAXBUTTON:
                    if (pressed) ToggleMaximized(window);
                    pressed = false;
                    maximize.Tag = null;
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        };
    }

    static bool Over(FrameworkElement element, IntPtr lParam)
    {
        var x = (short)((long)lParam & 0xFFFF);
        var y = (short)(((long)lParam >> 16) & 0xFFFF);
        try
        {
            var point = element.PointFromScreen(new Point(x, y));
            return point.X >= 0 && point.Y >= 0 && point.X < element.ActualWidth && point.Y < element.ActualHeight;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>The frame a maximized window hangs past the screen by, in device-independent pixels.</summary>
    static Thickness MaximizedInset(Window window)
    {
        var scale = VisualTreeHelper.GetDpi(window).DpiScaleX;
        var frame = (GetSystemMetrics(32) + GetSystemMetrics(92)) / scale; // SM_CXSIZEFRAME + SM_CXPADDEDBORDER
        return new Thickness(frame);
    }

    static void Recolor(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        try
        {
            var dark = Theme.IsDark ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
            var round = 2;
            DwmSetWindowAttribute(handle, 33, ref round, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE: round
            var border = ColorRef(Theme.WindowBorder);
            DwmSetWindowAttribute(handle, 34, ref border, sizeof(int)); // DWMWA_BORDER_COLOR
            var caption = ColorRef(Theme.Base);
            DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
        }
        catch (DllNotFoundException)
        {
            // Without DWM there is no frame to color.
        }
    }

    static int ColorRef(Color c) => c.R | c.G << 8 | c.B << 16;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int index);
}
