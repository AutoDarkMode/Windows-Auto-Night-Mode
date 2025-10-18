using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Models;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class HotkeysPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();

    public HotkeysViewModel ViewModel { get; }

    public HotkeysPage()
    {
        ViewModel = App.GetService<HotkeysViewModel>();
        InitializeComponent();
    }

    private async void EditHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not HotkeysDataObject hotkeyData || hotkeyData.Tag is null)
        {
            return;
        }

        var keyString = ViewModel.GetHotkeyValue(hotkeyData.Tag);
        var dialogContent = new ShortcutDialogContentControl();

        if (keyString is not null)
        {
            dialogContent.HotkeyCombination = keyString.Split(" + ").Select(key => new SingleHotkeyDataObject { Key = key }).ToList();
        }

        var shortcutDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "ActivationShortcut".GetLocalized() + " - " + hotkeyData.Tag,
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            SecondaryButtonText = "Reset".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };

        var result = await shortcutDialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            dialogContent.HotkeyCombination?.Clear();
            dialogContent.CapturedHotkeys = null;
        }
        else if (result != ContentDialogResult.Primary)
        {
            return;
        }

        ViewModel.UpdateHotkeyValue(hotkeyData.Tag, dialogContent.CapturedHotkeys);
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var originalContent = button.Content;
        var originalMinWidth = button.MinWidth;
        var originalMinHeight = button.MinHeight;

        button.MinWidth = button.ActualWidth;
        button.MinHeight = button.ActualHeight;
        button.Content = new ProgressRing() { Width = 18, Height = 18 };
        button.IsEnabled = false;

        try
        {
            ViewModel.ForceSaveHotkeysSettings();
            var saveTask = Task.Run(() => ViewModel.SafeSaveBuilder());
            var delayTask = Task.Delay(1000);
            await Task.WhenAll(saveTask, delayTask);

            button.Content = "SaveSuccessfully".GetLocalized();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, this.XamlRoot, "HotkeysPage");
            button.Content = "SaveFailed".GetLocalized();
        }
        finally
        {
            await Task.Delay(2000);
            button.MinWidth = originalMinWidth;
            button.MinHeight = originalMinHeight;
            button.Content = originalContent is string ? "ForceSaveSettings".GetLocalized() : originalContent;
            button.IsEnabled = true;
        }
    }
}
