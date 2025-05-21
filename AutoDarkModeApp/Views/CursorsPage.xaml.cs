using System.Drawing;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class CursorsPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();

    public CursorsViewModel ViewModel { get; }

    public CursorsPage()
    {
        ViewModel = App.GetService<CursorsViewModel>();
        InitializeComponent();

        DispatcherQueue.TryEnqueue(LoadCursorsSource);
        DispatcherQueue.TryEnqueue(LoadCursorsPreview);
    }

    private void LoadCursorsSource()
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

    private void LoadCursorsPreview()
    {
        if (ViewModel.SelectLightCursor != null)
        {
            LightVariableSizedWrapGrid.Children.Clear();
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
                        Margin = new Thickness(2, 0, 0, 0),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    LightVariableSizedWrapGrid.Children.Add(im);
                }
                catch { }
            }
        }

        if (ViewModel.SelectDarkCursor != null)
        {
            DarkVariableSizedWrapGrid.Children.Clear();
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
                        Margin = new Thickness(2, 0, 0, 0),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    };

                    DarkVariableSizedWrapGrid.Children.Add(im);
                }
                catch { }
            }
        }
    }

    private void LightCursorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(LoadCursorsPreview);
    }

    private void DarkCursorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(LoadCursorsPreview);
    }
}