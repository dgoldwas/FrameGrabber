using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FrameGrabber;

/// <summary>
/// Colors for the light and dark themes and the accent ("tint"), with the same values as VideoTrimmer, RadioStudio and
/// WB Tunes so the apps match. <see cref="Apply"/> replaces the brushes in the application's resources; everything that
/// shows them uses DynamicResource, so open windows follow at once. The video stage stays dark in both themes.
/// </summary>
public static class Theme
{
    /// <summary>An accent: its fill in the dark theme, its fill in the light theme, a darker ink for lines and text on light surfaces, and the two halves of its swatch.</summary>
    public sealed record Tint(string Name, Color Dark, Color LightFill, Color LightInk, Color SwatchDeep, Color SwatchLight);

    public static readonly Tint[] Tints =
    [
        new("Butter", Hex(0xF5C451), Hex(0xF5C451), Hex(0x8F6400), Hex(0x9A6B00), Hex(0xF5C451)),
        new("Graphite", Hex(0xE6E6E6), Hex(0x2B2B2B), Hex(0x2B2B2B), Hex(0x161616), Hex(0xE6E6E6)),
        new("Sky", Hex(0x6CB6FF), Hex(0x3B82D9), Hex(0x2F6FC0), Hex(0x1F6FD0), Hex(0x6CB6FF)),
        new("Mint", Hex(0x7FD1A6), Hex(0x3FA676), Hex(0x2B7F57), Hex(0x1E7F52), Hex(0x7FD1A6)),
        new("Rose", Hex(0xF29BB0), Hex(0xD9577A), Hex(0xB8405F), Hex(0xC2476A), Hex(0xF29BB0)),
        new("Violet", Hex(0xB9A4F5), Hex(0x7C62D6), Hex(0x6A4FC8), Hex(0x6A4FD0), Hex(0xB9A4F5)),
        new("Coral", Hex(0xFF9B7A), Hex(0xE5673F), Hex(0xC2502C), Hex(0xBF4A26), Hex(0xFF9B7A)),
    ];

    /// <summary>System follows Windows' app mode.</summary>
    public static readonly string[] Modes = ["System", "Light", "Dark"];

    public static string Mode { get; private set; } = "Dark";
    public static Tint CurrentTint { get; private set; } = Tints[0];
    public static bool IsDark { get; private set; } = true;

    public static Color Base { get; private set; }
    public static Color WindowBorder { get; private set; }

    /// <summary>Raised after <see cref="Apply"/>, or after Windows switches app mode while the mode is System.</summary>
    public static event Action? Changed;

    static bool listening;

    public static Tint FindTint(string? name) => Tints.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Tints[0];

    public static string FindMode(string? name) => Modes.FirstOrDefault(m => string.Equals(m, name, StringComparison.OrdinalIgnoreCase)) ?? "Dark";

    public static void Apply(string? mode, string? tint)
    {
        Mode = FindMode(mode);
        CurrentTint = FindTint(tint);
        if (!listening)
        {
            listening = true;
            SystemEvents.UserPreferenceChanged += (_, e) =>
            {
                if (e.Category == UserPreferenceCategory.General && Mode == "System")
                    Application.Current?.Dispatcher.BeginInvoke(() => Apply(Mode, CurrentTint.Name));
            };
        }
        Set(Mode == "Dark" || Mode == "System" && SystemPrefersDark(), CurrentTint);
        Changed?.Invoke();
    }

    static bool SystemPrefersDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch
        {
            return true;
        }
    }

    static void Set(bool dark, Tint tint)
    {
        IsDark = dark;
        var r = Application.Current.Resources;
        var accent = dark ? tint.Dark : tint.LightFill;
        var ink = dark ? tint.Dark : tint.LightInk;
        SetBrush("AccentBrush", accent);
        SetBrush("AccentInkBrush", ink);
        SetBrush("OnAccentBrush", ReadableOn(accent));
        SetBrush("SelectionBrush", Color.FromArgb(90, ink.R, ink.G, ink.B));
        // The video stage, and anything drawn on it, is dark in both themes.
        SetBrush("StageBrush", dark ? Color.FromRgb(7, 8, 11) : Color.FromRgb(22, 24, 29));

        if (dark)
        {
            Base = Color.FromRgb(13, 15, 19);
            WindowBorder = Color.FromRgb(40, 44, 52);
            SetBrush("TextBrush", Color.FromRgb(242, 244, 248));
            SetBrush("MutedBrush", Color.FromRgb(166, 174, 189));
            SetBrush("FaintBrush", Color.FromRgb(140, 149, 166));
            SetBrush("GoodBrush", Color.FromRgb(61, 220, 151));
            SetBrush("CautionBrush", Color.FromRgb(255, 138, 138));
            SetBrush("PanelBrush", Color.FromArgb(14, 255, 255, 255));
            SetBrush("PanelBorderBrush", Color.FromArgb(26, 255, 255, 255));
            Edge(Color.FromArgb(44, 255, 255, 255), Color.FromArgb(22, 255, 255, 255));
            SetBrush("ControlBrush", Color.FromArgb(16, 255, 255, 255));
            SetBrush("ControlHoverBrush", Color.FromArgb(30, 255, 255, 255));
            SetBrush("ControlPressedBrush", Color.FromArgb(56, 0, 0, 0));
            SetBrush("ControlBorderBrush", Color.FromArgb(26, 255, 255, 255));
            SetBrush("SelectedBrush", Color.FromArgb(36, 255, 255, 255));
            SetBrush("HairlineBrush", Color.FromArgb(20, 255, 255, 255));
            SetBrush("OutlineBrush", Color.FromArgb(40, 255, 255, 255));
            SetBrush("OutlineStrongBrush", Color.FromArgb(110, 255, 255, 255));
            SetBrush("WellBrush", Color.FromRgb(18, 20, 25));
            SetBrush("WellBorderBrush", Color.FromRgb(44, 48, 56));
            SetBrush("MenuBrush", Color.FromRgb(27, 31, 39));
            SetBrush("MenuBorderBrush", Color.FromRgb(54, 59, 70));
            r["BackdropOpacity"] = 0.22;
        }
        else
        {
            Base = Color.FromRgb(240, 240, 243);
            WindowBorder = Color.FromRgb(200, 200, 206);
            SetBrush("TextBrush", Color.FromRgb(27, 27, 30));
            SetBrush("MutedBrush", Color.FromRgb(85, 85, 92));
            SetBrush("FaintBrush", Color.FromRgb(118, 118, 126));
            SetBrush("GoodBrush", Color.FromRgb(46, 125, 79));
            SetBrush("CautionBrush", Color.FromRgb(179, 38, 30));
            SetBrush("PanelBrush", Color.FromArgb(158, 255, 255, 255));
            SetBrush("PanelBorderBrush", Color.FromArgb(24, 0, 0, 0));
            Edge(Color.FromArgb(255, 255, 255, 255), Color.FromArgb(24, 0, 0, 0));
            SetBrush("ControlBrush", Color.FromArgb(13, 0, 0, 0));
            SetBrush("ControlHoverBrush", Color.FromArgb(23, 0, 0, 0));
            SetBrush("ControlPressedBrush", Color.FromArgb(36, 0, 0, 0));
            SetBrush("ControlBorderBrush", Color.FromArgb(31, 0, 0, 0));
            SetBrush("SelectedBrush", Colors.White);
            SetBrush("HairlineBrush", Color.FromArgb(22, 0, 0, 0));
            SetBrush("OutlineBrush", Color.FromArgb(40, 0, 0, 0));
            SetBrush("OutlineStrongBrush", Color.FromArgb(100, 0, 0, 0));
            SetBrush("WellBrush", Color.FromRgb(232, 232, 236));
            SetBrush("WellBorderBrush", Color.FromRgb(206, 206, 213));
            SetBrush("MenuBrush", Color.FromRgb(252, 252, 253));
            SetBrush("MenuBorderBrush", Color.FromRgb(214, 214, 219));
            r["BackdropOpacity"] = 0.16;
        }
        SetBrush("BaseBrush", Base);
        SetBrush("WindowBorderBrush", WindowBorder);
    }

    static void SetBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Application.Current.Resources[key] = brush;
    }

    /// <summary>A panel's border: lit along the top edge, plain below it, so panels read as panes of glass.</summary>
    static void Edge(Color top, Color rest)
    {
        var brush = new LinearGradientBrush([new GradientStop(top, 0), new GradientStop(rest, 0.12), new GradientStop(rest, 1)], 90);
        brush.Freeze();
        Application.Current.Resources["PanelEdgeBrush"] = brush;
    }

    /// <summary>The two-tone swatch shown for <paramref name="tint"/> in Settings, as in RadioStudio.</summary>
    public static Brush Swatch(Tint tint)
    {
        var brush = new LinearGradientBrush([new GradientStop(tint.SwatchDeep, 0.5), new GradientStop(tint.SwatchLight, 0.5)], new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        return brush;
    }

    /// <summary>Near-black or white, whichever reads better on <paramref name="fill"/>.</summary>
    static Color ReadableOn(Color fill)
    {
        var dark = Color.FromRgb(20, 22, 26);
        return Contrast(dark, fill) >= Contrast(Colors.White, fill) ? dark : Colors.White;
    }

    static double Contrast(Color a, Color b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    static double Luminance(Color c)
    {
        static double Channel(byte value)
        {
            var v = value / 255d;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    static Color Hex(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
}
