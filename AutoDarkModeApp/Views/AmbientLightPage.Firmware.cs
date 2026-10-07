using System.Globalization;
using System.IO;
using System.Security;
using AdaptiveBrightness.Core;
namespace AutoDarkModeApp.Views;
public sealed partial class AmbientLightPage
{
    private async void FlashFirmwareButton_Click(object sender, RoutedEventArgs e)
    {
        FlashFirmwareButton.IsEnabled = false;
        try { await PrepareFirmwareAsync(); }
        catch (Exception ex) { FirmwareStatusText.Text = ex.Message; }
        finally { FlashFirmwareButton.IsEnabled = true; }
    }

    private async Task PrepareFirmwareAsync()
    {
        var firmwarePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Firmware", "adaptive-brightness.uf2");
        if (!Uf2ImageValidator.TryValidateFile(firmwarePath, out var image, out var validationError))
        {
            FirmwareStatusText.Text = string.IsNullOrEmpty(validationError) ? T("FirmwareMissingStatus") : LocalizeStatus(validationError);
            return;
        }

        var bootDrive = FindRp2040BootDrive(out var detectError);
        if (bootDrive is null)
        {
            FirmwareStatusText.Text = detectError;
            return;
        }

        var root = bootDrive.RootDirectory.FullName;
        var dialog = new ContentDialog
        {
            Title = T("FirmwareConfirmTitle"),
            Content = string.Format(CultureInfo.CurrentCulture, T("FirmwareConfirmBody"), root, image!.LengthBytes),
            PrimaryButtonText = T("FirmwareConfirmButton"),
            CloseButtonText = T("CancelButton"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            var current = FindRp2040BootDrive(root, out var recheckError);
            if (current is null) throw new IOException(recheckError);
            if (!Uf2ImageValidator.TryValidateFile(firmwarePath, out _, out validationError)) throw new IOException(validationError);
            var destination = System.IO.Path.Combine(root, "adaptive-brightness.uf2");
            File.Copy(firmwarePath, destination, overwrite: true);
            FirmwareStatusText.Text = File.Exists(destination)
                ? T("FirmwareCopiedStatus")
                : T("FirmwareSubmittedStatus");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            FirmwareStatusText.Text = string.Format(CultureInfo.CurrentCulture, T("FirmwareCopyFailed"), ex.Message);
        }
    }

    private DriveInfo? FindRp2040BootDrive(out string error) => FindRp2040BootDrive(null, out error);

    private DriveInfo? FindRp2040BootDrive(string? expectedRoot, out string error)
    {
        var matches = new List<DriveInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Removable) continue;
                var root = drive.RootDirectory.FullName;
                if (expectedRoot is not null && !string.Equals(root, expectedRoot, StringComparison.OrdinalIgnoreCase)) continue;
                var infoPath = System.IO.Path.Combine(root, "INFO_UF2.TXT");
                var indexPath = System.IO.Path.Combine(root, "INDEX.HTM");
                if (!File.Exists(infoPath) || !File.Exists(indexPath)) continue;
                var info = File.ReadAllText(infoPath);
                if (Rp2040BootloaderIdentity.IsExpectedDrive(drive.VolumeLabel, isRemovable: true, info, hasIndexFile: true)) matches.Add(drive);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException) { }
        }

        if (matches.Count == 1)
        {
            error = "";
            return matches[0];
        }
        error = matches.Count == 0
            ? T("BootDriveNotFound")
            : T("BootDriveAmbiguous");
        return null;
    }

}
