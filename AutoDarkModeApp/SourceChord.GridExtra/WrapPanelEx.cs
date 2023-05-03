using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SourceChord.GridExtra
{
    public static class WrapPanelEx
    {

        public static Size GetOriginalSize(DependencyObject obj)
        {
            return (Size)obj.GetValue(OriginalSizeProperty);
        }

        private static void SetOriginalSize(DependencyObject obj, Size value)
        {
            obj.SetValue(OriginalSizeProperty, value);
        }

        // Using a DependencyProperty as the backing store for OriginalSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OriginalSizeProperty =
            DependencyProperty.RegisterAttached("OriginalSize", typeof(Size), typeof(WrapPanelEx), new PropertyMetadata(Size.Empty));


        public static bool GetAdaptiveLayout(DependencyObject obj)
        {
            return (bool)obj.GetValue(AdaptiveLayoutProperty);
        }

        public static void SetAdaptiveLayout(DependencyObject obj, bool value)
        {
            obj.SetValue(AdaptiveLayoutProperty, value);
        }

        // Using a DependencyProperty as the backing store for AdaptiveLayout.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AdaptiveLayoutProperty =
            DependencyProperty.RegisterAttached("AdaptiveLayout", typeof(bool), typeof(WrapPanelEx), new PropertyMetadata(false, OnAdaptiveLayoutChanged));

        private static void OnAdaptiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as WrapPanel;
            var isEnabled = (bool)e.NewValue;
            if (panel == null) { return; }

            if (isEnabled)
            {
                SetOriginalSize(panel, new Size(panel.ItemWidth, panel.ItemHeight));
                var layoutUpdateCallback = CreateLayoutUpdateHandler(panel);
                panel.LayoutUpdated += layoutUpdateCallback;
                SetLayoutUpdatedCallback(panel, layoutUpdateCallback);
            }
            else
            {
                var originalSize = GetOriginalSize(panel);

                panel.ItemWidth = originalSize.Width;
                panel.ItemHeight = originalSize.Height;

                panel.ClearValue(OriginalSizeProperty);
                // イベントの解除
                var callback = GetLayoutUpdatedCallback(panel);
                panel.LayoutUpdated -= callback;
            }
        }

        private static EventHandler CreateLayoutUpdateHandler(WrapPanel panel)
        {
            var layoutUpdateCallback = new EventHandler((sender, args) =>
            {
                if (panel == null) return;
                var orientaion = panel.Orientation;
                var originalSize = GetOriginalSize(panel);

                if (orientaion == Orientation.Horizontal)
                {
                    if (double.IsNaN(originalSize.Width)) return;

                    var count = Math.Floor(panel.ActualWidth / originalSize.Width);
                    var size = panel.ActualWidth / count;

                    panel.ItemWidth = size;
                }
                else
                {
                    if (double.IsNaN(originalSize.Width)) return;

                    var count = Math.Floor(panel.ActualHeight / originalSize.Height);
                    var size = panel.ActualHeight / count;

                    panel.ItemHeight = size;
                }
            });

            return layoutUpdateCallback;
        }

        public static EventHandler GetLayoutUpdatedCallback(DependencyObject obj)
        {
            return (EventHandler)obj.GetValue(LayoutUpdatedCallbackProperty);
        }
        private static void SetLayoutUpdatedCallback(DependencyObject obj, EventHandler value)
        {
            obj.SetValue(LayoutUpdatedCallbackProperty, value);
        }
        // Using a DependencyProperty as the backing store for LayoutUpdatedCallback.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutUpdatedCallbackProperty =
            DependencyProperty.RegisterAttached("LayoutUpdatedCallback", typeof(EventHandler), typeof(WrapPanelEx), new PropertyMetadata(null));

    }
}
