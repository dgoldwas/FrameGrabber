using System.Windows;
using System.Windows.Controls;

namespace FrameGrabber;

/// <summary>
/// Edit › Settings: the theme and accent, as in RadioStudio's Appearance settings, and the export folder. Every choice
/// applies and is saved as soon as it is made, so Done only closes the window.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainWindow main;

    public SettingsWindow(MainWindow main)
    {
        this.main = main;
        InitializeComponent();
        WindowFrame.Attach(this, Root, null, null, CloseButton, 52);

        foreach (var option in new[] { ThemeSystem, ThemeLight, ThemeDark })
            option.IsChecked = (string)option.Tag == Theme.Mode;

        foreach (var tint in Theme.Tints)
        {
            var pick = new RadioButton
            {
                Content = tint.Name,
                GroupName = "Tint",
                Style = (Style)FindResource("TintPill"),
                Background = Theme.Swatch(tint),
                IsChecked = tint == Theme.CurrentTint,
            };
            pick.Checked += (_, _) => Save(App.Settings.Theme, tint.Name);
            TintPicks.Children.Add(pick);
        }

        FolderText.Text = main.ExportFolder;
    }

    private void Theme_Checked(object s, RoutedEventArgs e)
    {
        if (IsLoaded) Save((string)((FrameworkElement)s).Tag, App.Settings.Accent);
    }

    private static void Save(string theme, string accent)
    {
        App.Settings.Theme = theme;
        App.Settings.Accent = accent;
        App.Settings.Save();
        Theme.Apply(theme, accent);
    }

    private void ChangeFolder_Click(object s, RoutedEventArgs e)
    {
        if (main.ChooseExportFolder()) FolderText.Text = main.ExportFolder;
    }

    private void Done_Click(object s, RoutedEventArgs e) => Close();
}
