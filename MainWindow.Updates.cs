using System.Windows;
using System.Windows.Threading;

namespace FrameGrabber;

// Help offers a manual check and changes to "Update to …" when a release is available, with a dot on Help to point at it.
public partial class MainWindow
{
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    private readonly CancellationTokenSource updateLifetime = new();
    private AvailableUpdate? availableUpdate;
    private string? pendingUpdateSetup;
    private string? pendingUpdateVersion;
    private bool updateBusy;

    private void StartUpdateChecks()
    {
        ShowVersion();
        Closed += (_, _) =>
        {
            updateTimer.Stop();
            updateLifetime.Cancel();
            LaunchPendingUpdate();
        };
        if (UpdateService.TakeLastResult() is { } result)
        {
            if (result.StartsWith("failed", StringComparison.OrdinalIgnoreCase))
                StatusText.Text = $"The last update didn't install ({result["failed: ".Length..]}). Check for updates to try again.";
            else if (result.StartsWith("installed", StringComparison.OrdinalIgnoreCase))
                StatusText.Text = $"Updated to FrameGrabber {UpdateService.CurrentVersion}.";
        }

        updateTimer.Tick += async (_, _) => await CheckForUpdateAsync(manual: false);
        updateTimer.Start();
        // Let the window open first; the check is a single small request.
        _ = Task.Delay(TimeSpan.FromSeconds(5), updateLifetime.Token).ContinueWith(
            _ => Dispatcher.InvokeAsync(async () => await CheckForUpdateAsync(manual: false)),
            TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void ShowVersion()
    {
        AboutMenuItem.InputGestureText = UpdateService.CurrentVersion;
        if (availableUpdate is { } found)
        {
            UpdateMenuItem.Header = $"_Update to {found.Version}…";
            UpdateMenuItem.ToolTip = $"FrameGrabber {found.Version} is available (you have {UpdateService.CurrentVersion}).";
            HelpMenu.Tag = "dot";
        }
        else
        {
            UpdateMenuItem.Header = "Check for _updates";
            UpdateMenuItem.ToolTip = null;
            HelpMenu.Tag = null;
        }
    }

    private async void UpdateMenuItem_Click(object s, RoutedEventArgs e)
    {
        if (updateBusy) return;
        if (availableUpdate is null) await CheckForUpdateAsync(manual: true);
        else await InstallUpdateAsync();
    }

    private async Task CheckForUpdateAsync(bool manual)
    {
        if (updateBusy || pendingUpdateSetup != null) return;
        updateBusy = true;
        if (manual) StatusText.Text = "Checking for updates…";
        try
        {
            availableUpdate = await UpdateService.CheckAsync(updateLifetime.Token);
            ShowVersion();
            if (manual && availableUpdate is null)
                StatusText.Text = $"FrameGrabber is up to date (version {UpdateService.CurrentVersion}).";
        }
        catch (OperationCanceledException) when (updateLifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // Offline or GitHub unavailable: automatic checks try again later.
            if (manual) StatusText.Text = "Couldn't check for updates: " + ex.Message;
            return;
        }
        finally
        {
            updateBusy = false;
        }

        if (manual && availableUpdate is not null) await InstallUpdateAsync();
    }

    private async Task InstallUpdateAsync()
    {
        if (availableUpdate is not { } update || updateBusy) return;
        var size = update.Size > 0 ? $" ({update.Size / 1048576.0:N0} MB)" : "";
        var answer = System.Windows.MessageBox.Show(this,
            $"FrameGrabber {update.Version} is available. You have {UpdateService.CurrentVersion}.\n\nDownload it{size} and install it now? FrameGrabber will close, Windows will ask for permission to install, and FrameGrabber will open again when the update is done. Your frame queue is not kept.",
            "Update FrameGrabber", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        if (answer != MessageBoxResult.OK) return;

        updateBusy = true;
        UpdateMenuItem.IsEnabled = false;
        try
        {
            var progress = new Progress<double>(p => StatusText.Text = $"Downloading FrameGrabber {update.Version}… {p:P0}");
            pendingUpdateSetup = await UpdateService.DownloadAsync(update, progress, updateLifetime.Token);
            pendingUpdateVersion = update.Version;
            StatusText.Text = "Installing…";
            // Closed hands the installer to the helper once the window is gone.
            Close();
        }
        catch (OperationCanceledException) when (updateLifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            pendingUpdateSetup = null;
            UpdateMenuItem.IsEnabled = true;
            ShowVersion();
            StatusText.Text = "The update couldn't be downloaded: " + ex.Message;
        }
        finally
        {
            updateBusy = false;
        }
    }

    /// <summary>Called as the window closes: hands a downloaded update to the installer helper.</summary>
    private void LaunchPendingUpdate()
    {
        if (pendingUpdateSetup == null) return;
        try
        {
            UpdateService.LaunchInstallerAfterExit(pendingUpdateSetup, pendingUpdateVersion ?? "");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"The update couldn't be started: {ex.Message}\n\nRun {pendingUpdateSetup} to install it.", "FrameGrabber", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
