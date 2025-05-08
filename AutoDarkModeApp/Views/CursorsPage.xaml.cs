using System.Drawing;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml;
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

        DispatcherQueue.TryEnqueue(() => LoadCurosSource());
        DispatcherQueue.TryEnqueue(() => LoadCurosPreview());
    }

    private void LoadCurosSource()
    {
        try
        {
            List<string> cursors = CursorCollectionHandler.GetCursors();
            LightCurosComboBox.ItemsSource = cursors;
            DarkCurosComboBox.ItemsSource = cursors;
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "RefreshCursors");
        }
    }

    private void LoadCurosPreview()
    {
        if (ViewModel.SelectLightCusor != null)
        {
            LightImageStackPanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(ViewModel.SelectLightCusor.ToString()!);
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
                        Margin = new Thickness(2, 0, 0, 0),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    LightImageStackPanel.Children.Add(im);
                }
                catch { }
            }
        }

        if (ViewModel.SelectDarkCusor != null)
        {
            DarkImageStackPanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(ViewModel.SelectDarkCusor.ToString()!);
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
                        Margin = new Thickness(2, 0, 0, 0),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    DarkImageStackPanel.Children.Add(im);
                }
                catch { }
            }
        }
    }

    private void LightCurosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => LoadCurosPreview());
    }

    private void DarkCurosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => LoadCurosPreview());
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
