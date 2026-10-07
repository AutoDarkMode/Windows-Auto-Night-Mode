using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
namespace AutoDarkModeApp.Views;
public sealed partial class AmbientLightPage
{
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentLuxSliderPercentage))
        {
            UpdateIndicatorPosition();
        }
    }

    private void IndicatorCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateIndicatorPosition();
    }

    private void UpdateIndicatorPosition()
    {
        if (IndicatorCanvas == null || CurrentLuxIndicator == null) return;

        double width = IndicatorCanvas.ActualWidth;
        if (width <= 0) return;

        // CurrentLuxSliderPercentage is 0-1000.
        double normalized = Math.Clamp(ViewModel.CurrentLuxSliderPercentage / 1000.0, 0, 1);

        // Calculate target X. Center of icon should be at percentage.
        // Icon is 12px (FontSize 12). If ActualWidth is 0, assume 12.
        double iconWidth = CurrentLuxIndicator.ActualWidth > 0 ? CurrentLuxIndicator.ActualWidth : 12;
        double targetX = normalized * width - (iconWidth / 2);

        // Animate
        var anim = new DoubleAnimation
        {
            To = targetX,
            Duration = new TimeSpan(0, 0, 0, 0, 300), // 300ms smooth
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(anim);
        Storyboard.SetTarget(anim, CurrentLuxIndicator);
        Storyboard.SetTargetProperty(anim, "(Canvas.Left)");

        storyboard.Begin();
    }

    private void AmbientLightRangeSelector_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Hide the tooltip elements in the RangeSelector control.
            if (sender is FrameworkElement element)
            {
                // Try immediately first
                HideTooltipElements(element);

                // Also schedule delayed attempts to catch lazy-loaded templates
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    try { HideTooltipElements(element); } catch { }
                });
            }
        }
        catch { }

        // Also attach to PointerEntered to ensure we catch any dynamic tooltip generation
        if (sender is UIElement uiElement)
        {
            uiElement.PointerEntered += (s, args) =>
            {
                try
                {
                    if (s is FrameworkElement fe) HideTooltipElements(fe);
                }
                catch { }
            };
        }
    }

    /// <summary>
    /// Recursively searches the visual tree to find and hide tooltip elements.
    /// The RangeSelector control has built-in tooltips that show raw slider values (0-1000).
    /// </summary>
    private static void HideTooltipElements(DependencyObject parent)
    {
        if (parent == null) return;
        try
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe)
                {
                    // Method 1: Check name for "ToolTip"
                    string name = fe.Name ?? string.Empty;
                    bool isToolTip = name.Contains("ToolTip", StringComparison.OrdinalIgnoreCase);

                    // Method 2: Check if it's the specific thumb parts (MinThumb/MaxThumb) and disable their ToolTipService
                    if (name.Contains("Thumb", StringComparison.OrdinalIgnoreCase))
                    {
                        ToolTipService.SetToolTip(fe, null);
                    }

                    if (isToolTip)
                    {
                        fe.Opacity = 0;
                        fe.IsHitTestVisible = false;
                        // fe.Visibility = Visibility.Collapsed; // Collapsed causes artifacts
                    }
                }

                HideTooltipElements(child);
            }
        }
        catch { }
    }

    private void AmbientLightRangeSelector_ThumbDragStarted(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragStartedEventArgs e)
    {
        PageScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
        PageScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
    }

    private void AmbientLightRangeSelector_ThumbDragCompleted(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragCompletedEventArgs e)
    {
        PageScrollViewer.HorizontalScrollMode = ScrollMode.Auto;
        PageScrollViewer.VerticalScrollMode = ScrollMode.Auto;
    }
}
