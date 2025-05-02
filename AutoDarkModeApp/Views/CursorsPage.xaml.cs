using System.Drawing;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Views;

public sealed partial class CursorsPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();

    public CursorsViewModel ViewModel { get; }

    public CursorsPage()
    {
        ViewModel = App.GetService<CursorsViewModel>();
        InitializeComponent();

        DispatcherQueue.TryEnqueue(() => LoadCursorSources());
        DispatcherQueue.TryEnqueue(() => LoadCursorPreview());
    }

    private void LoadCursorSources()
    {
        try
        {
            List<string> cursors = CursorCollectionHandler.GetCursors();
            LightCursorComboBox.ItemsSource = cursors;
            DarkCursorComboBox.ItemsSource = cursors;
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "RefreshCursors");
        }
    }

    private void LoadCursorPreview()
    {
        if (ViewModel.SelectLightCursor != null)
        {
            LightImageStackPanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(ViewModel.SelectLightCursor.ToString()!);
            foreach (string cursor in cursors)
            {
                try
                {
                    System.Drawing.Icon i = System.Drawing.Icon.ExtractAssociatedIcon(cursor)!;
                    Bitmap b = i!.ToBitmap();

                    using var memoryStream = new System.IO.MemoryStream();
                    b.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;

                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    bitmapImage.SetSource(memoryStream.AsRandomAccessStream());

                    Microsoft.UI.Xaml.Controls.Image im = new()
                    {
                        Source = bitmapImage,
                        MaxHeight = 32,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    LightImageStackPanel.Children.Add(im);
                }
                catch { }
            }
        }

        if (ViewModel.SelectDarkCursor != null)
        {
            DarkImageStackPanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(ViewModel.SelectDarkCursor.ToString()!);
            foreach (string cursor in cursors)
            {
                try
                {
                    System.Drawing.Icon i = System.Drawing.Icon.ExtractAssociatedIcon(cursor)!;
                    Bitmap b = i!.ToBitmap();

                    using var memoryStream = new System.IO.MemoryStream();
                    b.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;

                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    bitmapImage.SetSource(memoryStream.AsRandomAccessStream());

                    Microsoft.UI.Xaml.Controls.Image im = new()
                    {
                        Source = bitmapImage,
                        MaxHeight = 32,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    DarkImageStackPanel.Children.Add(im);
                }
                catch { }
            }
        }
    }

    private void LightCursorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => LoadCursorPreview());
    }

    private void DarkCursorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => LoadCursorPreview());
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
