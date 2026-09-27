using System.Globalization;
using AdaptiveBrightness.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;

namespace AutoDarkModeApp.Views;

public sealed partial class AmbientLightPage
{
    private const double PlotLeft = 56;
    private const double PlotTop = 16;
    private const double PlotRight = 20;
    private const double PlotBottom = 42;
    private const double HandleGapPixels = 18;

    private void DrawCurve()
    {
        if (CurveCanvas is null || CurveCanvas.ActualWidth <= PlotLeft + PlotRight || CurveCanvas.ActualHeight <= PlotTop + PlotBottom)
            return;

        CurveCanvas.Children.Clear();
        var axisMaximum = CurveEditorMath.AxisMaximumLux(_draftCurve);
        var plotWidth = CurveCanvas.ActualWidth - PlotLeft - PlotRight;
        var plotHeight = CurveCanvas.ActualHeight - PlotTop - PlotBottom;
        var xAxis = PlotTop + plotHeight;
        var gridBrush = Brush("DividerStrokeColorDefaultBrush", Color.FromArgb(88, 128, 128, 128));
        var textBrush = Brush("TextFillColorSecondaryBrush", Color.FromArgb(190, 120, 120, 120));
        var curveBrush = Brush("AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 212));

        foreach (var percent in new[] { 0, 25, 50, 75, 100 })
        {
            var y = PlotTop + plotHeight * (100 - percent) / 100d;
            AddCurveLine(PlotLeft, y, PlotLeft + plotWidth, y, gridBrush, percent is 0 or 100 ? 1.25 : 0.8);
            AddCurveLabel($"{percent}%", 3, y - 9, 46, textBrush, TextAlignment.Right);
        }

        AddCurveLine(PlotLeft, PlotTop, PlotLeft, xAxis, gridBrush, 1.25);
        AddCurveLine(PlotLeft, xAxis, PlotLeft + plotWidth, xAxis, gridBrush, 1.25);
        var lastLabelRight = double.NegativeInfinity;
        foreach (var tick in CurveEditorMath.AxisTicks)
        {
            var x = PlotLeft + plotWidth * CurveEditorMath.LuxToNormalizedX(tick, axisMaximum);
            var major = tick is 0 or 1 or 10 or 100 or 1000 or 10000;
            AddCurveLine(x, PlotTop, x, xAxis, gridBrush, major ? 0.9 : 0.3);
            AddCurveLine(x, xAxis, x, xAxis + (major ? 6 : 3), textBrush, 0.8);
            var labelCandidate = major || tick is 2 or 5 or 20 or 50 or 200 or 500 or 2000 or 5000;
            var nextMajor = tick < 1 ? 1 : Math.Min(10000, Math.Pow(10, Math.Floor(Math.Log10(tick)) + 1));
            var nextMajorX = PlotLeft + plotWidth * CurveEditorMath.LuxToNormalizedX(nextMajor, axisMaximum);
            if (major || (labelCandidate && x - 26 > lastLabelRight + 4 && nextMajorX - x > 58))
            {
                AddCurveLabel(FormatAxisValue(tick), x - 28, xAxis + 8, 56, textBrush, TextAlignment.Center);
                lastLabelRight = x + 28;
            }
        }
        AddCurveLabel(T("CurveYAxisLabel"), PlotLeft, 2, 72, textBrush, TextAlignment.Left);
        AddCurveLabel(T("CurveXAxisLabel"), PlotLeft, xAxis + 25, plotWidth, textBrush, TextAlignment.Center);

        for (var i = 1; i < _draftCurve.Count; i++)
        {
            var from = CurveToCanvasPoint(_draftCurve[i - 1], axisMaximum, plotWidth, plotHeight);
            var to = CurveToCanvasPoint(_draftCurve[i], axisMaximum, plotWidth, plotHeight);
            AddCurveLine(from.X, from.Y, to.X, to.Y, curveBrush, 3);
        }
        for (var i = 0; i < _draftCurve.Count; i++)
        {
            var point = CurveToCanvasPoint(_draftCurve[i], axisMaximum, plotWidth, plotHeight);
            var diameter = i == _selectedPointIndex ? 19 : 14;
            var handle = new Ellipse
            {
                Width = diameter, Height = diameter, Fill = curveBrush,
                Stroke = new SolidColorBrush(Colors.White), StrokeThickness = 2
            };
            Canvas.SetLeft(handle, point.X - diameter / 2);
            Canvas.SetTop(handle, point.Y - diameter / 2);
            CurveCanvas.Children.Add(handle);
        }
        if (_lastLux is double lux)
        {
            var current = CurveToCanvasPoint(new CurvePoint(lux, BrightnessCurve.Interpolate(lux, _draftCurve)), axisMaximum, plotWidth, plotHeight);
            AddCurveLine(current.X, PlotTop, current.X, xAxis, textBrush, 1);
            var marker = new Ellipse { Width = 10, Height = 10, Fill = textBrush };
            Canvas.SetLeft(marker, current.X - 5);
            Canvas.SetTop(marker, current.Y - 5);
            CurveCanvas.Children.Add(marker);
        }
        CurvePointCountText.Text = string.Format(CultureInfo.CurrentCulture, _uiLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "{0} points" : "{0} 个控制点", _draftCurve.Count);
        UpdateLiveLuxPreview();
    }

    private Point CurveToCanvasPoint(CurvePoint point, double axisMaximum, double plotWidth, double plotHeight) =>
        new(PlotLeft + plotWidth * CurveEditorMath.LuxToNormalizedX(point.Lux, axisMaximum),
            PlotTop + plotHeight * (100 - point.Brightness) / 100d);

    private void AddCurveLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness) =>
        CurveCanvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness });

    private void AddCurveLabel(string text, double x, double y, double width, Brush foreground, TextAlignment alignment)
    {
        var label = new TextBlock { Text = text, Width = Math.Max(0, width), FontSize = 11, Foreground = foreground, TextAlignment = alignment };
        Canvas.SetLeft(label, Math.Max(0, x));
        Canvas.SetTop(label, Math.Max(0, y));
        CurveCanvas.Children.Add(label);
    }

    private Brush Brush(string key, Color fallback) =>
        Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush ? brush : new SolidColorBrush(fallback);

    private static string FormatAxisValue(double value) => value.ToString("#,0.##", CultureInfo.InvariantCulture);

    private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawCurve();

    private void CurveCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint(CurveCanvas).Position;
        var axisMaximum = CurveEditorMath.AxisMaximumLux(_draftCurve);
        var plotWidth = CurveCanvas.ActualWidth - PlotLeft - PlotRight;
        var plotHeight = CurveCanvas.ActualHeight - PlotTop - PlotBottom;
        var nearest = -1;
        var nearestDistance = double.MaxValue;
        for (var i = 0; i < _draftCurve.Count; i++)
        {
            var point = CurveToCanvasPoint(_draftCurve[i], axisMaximum, plotWidth, plotHeight);
            var dx = position.X - point.X;
            var dy = position.Y - point.Y;
            var distance = dx * dx + dy * dy;
            if (distance < nearestDistance) { nearest = i; nearestDistance = distance; }
        }
        if (nearest < 0 || nearestDistance > 26 * 26) return;
        SelectCurvePoint(nearest);
        _draggingPoint = true;
        _dragPointerId = e.Pointer.PointerId;
        CurveCanvas.CapturePointer(e.Pointer);
        CurveCanvas.Focus(FocusState.Pointer);
        e.Handled = true;
    }

    private void CurveCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_draggingPoint || e.Pointer.PointerId != _dragPointerId) return;
        UpdateCurvePointFromPosition(e.GetCurrentPoint(CurveCanvas).Position);
        e.Handled = true;
    }

    private void CurveCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_draggingPoint && e.Pointer.PointerId == _dragPointerId)
        {
            _draggingPoint = false;
            CurveCanvas.ReleasePointerCapture(e.Pointer);
            FlushCurveChanges();
            e.Handled = true;
        }
    }

    private void CurveCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_draggingPoint && e.Pointer.PointerId == _dragPointerId)
        {
            _draggingPoint = false;
            CurveCanvas.ReleasePointerCapture(e.Pointer);
            FlushCurveChanges();
        }
    }

    private void UpdateCurvePointFromPosition(Point position)
    {
        var plotWidth = CurveCanvas.ActualWidth - PlotLeft - PlotRight;
        var plotHeight = CurveCanvas.ActualHeight - PlotTop - PlotBottom;
        var axisMaximum = CurveEditorMath.AxisMaximumLux(_draftCurve);
        var normalizedX = (position.X - PlotLeft) / plotWidth;
        normalizedX = CurveEditorMath.ConstrainNormalizedX(_draftCurve, _selectedPointIndex, normalizedX, axisMaximum, HandleGapPixels / plotWidth);
        var lux = CurveEditorMath.NormalizedXToLux(normalizedX, axisMaximum);
        var brightness = Math.Clamp(100 * (1 - (position.Y - PlotTop) / plotHeight), 0, 100);
        _draftCurve[_selectedPointIndex] = new CurvePoint(lux, brightness);
        SyncSelectedPointFields();
        MarkCurveDraftChanged();
    }

    private void CurveCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_draftCurve.Count == 0) return;
        if (e.Key == VirtualKey.Home) { SelectCurvePoint(0); e.Handled = true; return; }
        if (e.Key == VirtualKey.End) { SelectCurvePoint(_draftCurve.Count - 1); e.Handled = true; return; }
        if (e.Key == VirtualKey.Enter) { SelectCurvePoint((_selectedPointIndex + 1) % _draftCurve.Count); e.Handled = true; return; }
        var point = _draftCurve[_selectedPointIndex];
        var axisMaximum = CurveEditorMath.AxisMaximumLux(_draftCurve);
        var updated = point;
        switch (e.Key)
        {
            case VirtualKey.Left:
            case VirtualKey.Right:
            {
                var currentX = CurveEditorMath.LuxToNormalizedX(point.Lux, axisMaximum);
                var step = e.Key == VirtualKey.Right ? 0.008 : -0.008;
                var width = Math.Max(1, CurveCanvas.ActualWidth - PlotLeft - PlotRight);
                var normalized = CurveEditorMath.ConstrainNormalizedX(_draftCurve, _selectedPointIndex, currentX + step, axisMaximum, HandleGapPixels / width);
                updated = point with { Lux = CurveEditorMath.NormalizedXToLux(normalized, axisMaximum) };
                break;
            }
            case VirtualKey.Up: updated = point with { Brightness = Math.Min(100, point.Brightness + 1) }; break;
            case VirtualKey.Down: updated = point with { Brightness = Math.Max(0, point.Brightness - 1) }; break;
            default: return;
        }
        _draftCurve[_selectedPointIndex] = updated;
        SyncSelectedPointFields();
        MarkCurveDraftChanged();
        e.Handled = true;
    }

    private void SelectCurvePoint(int index)
    {
        if (_draftCurve.Count == 0) return;
        _selectedPointIndex = Math.Clamp(index, 0, _draftCurve.Count - 1);
        SyncSelectedPointFields();
        DrawCurve();
    }

    private void SyncSelectedPointFields()
    {
        if (_draftCurve.Count == 0 || SelectedLuxTextBox is null) return;
        var point = _draftCurve[Math.Clamp(_selectedPointIndex, 0, _draftCurve.Count - 1)];
        _syncingPointEditors = true;
        try
        {
            SelectedLuxTextBox.Text = FormatNumber(point.Lux);
            SelectedBrightnessTextBox.Text = FormatNumber(point.Brightness);
        }
        finally { _syncingPointEditors = false; }
    }

    private void SelectedPointEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_initializingUi || _syncingPointEditors) return;
        if (TryCommitSelectedEditor(out var error)) MarkCurveDraftChanged();
        else { _autoSaveTimer.Stop(); SaveStatusText.Text = error; }
    }

    private void SelectedPointEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_initializingUi || _syncingPointEditors) return;
        if (TryCommitSelectedEditor(out var error)) { MarkCurveDraftChanged(); FlushCurveChanges(); }
        else SaveStatusText.Text = error;
    }

    private bool TryCommitSelectedEditor(out string error)
    {
        error = "";
        if (!TryParseNumber(SelectedLuxTextBox.Text, out var lux) || lux < 0 || lux > CurveEditorMath.MaximumLux)
        {
            error = _uiLanguage.StartsWith("zh") ? "环境光必须在 0 到 10,000 lux 之间。" : "Ambient light must be between 0 and 10,000 lux.";
            return false;
        }
        if (!TryParseNumber(SelectedBrightnessTextBox.Text, out var brightness) || brightness is < 0 or > 100)
        {
            error = T("InvalidBrightness");
            return false;
        }
        if (!CurveEditorMath.HasStrictLuxOrder(_draftCurve, _selectedPointIndex, lux))
        {
            error = T("CurveOrderError");
            return false;
        }
        _draftCurve[_selectedPointIndex] = new CurvePoint(lux, brightness);
        DrawCurve();
        return true;
    }

    private void MarkCurveDraftChanged()
    {
        DrawCurve();
        QueueCurveChanges();
    }

    private void UpdateLiveLuxPreview()
    {
        if (CurvePreviewText is null || _draftCurve.Count == 0) return;
        var lux = _lastLux ?? _draftCurve[Math.Clamp(_selectedPointIndex, 0, _draftCurve.Count - 1)].Lux;
        var target = BrightnessCurve.Interpolate(lux, _draftCurve);
        CurvePreviewText.Text = _uiLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? $"Curve target: {lux:F1} lux → {target:F1}%"
            : $"曲线目标：{lux:F1} lux → {target:F1}%";
    }

    private void AddPointButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCommitSelectedEditor(out var error)) { SaveStatusText.Text = error; return; }
        if (!TryParseNumber(NewLuxTextBox.Text, out var lux) || lux < 0 || !TryParseNumber(NewBrightnessTextBox.Text, out var brightness) || brightness is < 0 or > 100)
        {
            SaveStatusText.Text = T("InvalidNewPoint");
            return;
        }
        var updated = _draftCurve.Append(new CurvePoint(lux, brightness)).OrderBy(point => point.Lux).ToList();
        if (!AppSettingsValidation.TryValidate(_settings with { Curve = updated }, out error)) { SaveStatusText.Text = LocalizeStatus(error); return; }
        _selectedPointIndex = updated.FindIndex(point => point.Lux.Equals(lux));
        _draftCurve = updated;
        NewLuxTextBox.Text = "";
        NewBrightnessTextBox.Text = "";
        SyncSelectedPointFields();
        MarkCurveDraftChanged();
    }

    private void RemovePointButton_Click(object sender, RoutedEventArgs e)
    {
        if (_draftCurve.Count <= 2) { SaveStatusText.Text = T("CurveMinimumPoints"); return; }
        _draftCurve.RemoveAt(_selectedPointIndex);
        _selectedPointIndex = Math.Min(_selectedPointIndex, _draftCurve.Count - 1);
        SyncSelectedPointFields();
        MarkCurveDraftChanged();
    }

    private void RebuildPresetSelector()
    {
        var initializing = _initializingUi;
        _initializingUi = true;
        CurvePresetComboBox.Items.Clear();
        foreach (var preset in _draftPresets.OrderBy(preset => preset.Name, StringComparer.CurrentCultureIgnoreCase))
            CurvePresetComboBox.Items.Add(new ComboBoxItem { Content = preset.Name, Tag = preset.Name });
        CurvePresetComboBox.SelectedItem = CurvePresetComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
            string.Equals(item.Tag as string, _selectedPresetName, StringComparison.OrdinalIgnoreCase));
        DeletePresetButton.IsEnabled = _draftPresets.Count > 1;
        _initializingUi = initializing;
    }

    private void CurvePresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingUi || CurvePresetComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string name) return;
        if (!FlushCurveChanges()) return;
        var preset = _draftPresets.FirstOrDefault(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        if (preset is null) return;
        _selectedPresetName = preset.Name;
        _draftCurve = preset.Points.OrderBy(point => point.Lux).ToList();
        _selectedPointIndex = Math.Clamp(_selectedPointIndex, 0, _draftCurve.Count - 1);
        SyncSelectedPointFields();
        DrawCurve();
        DeletePresetButton.IsEnabled = _draftPresets.Count > 1;
        QueueCurveChanges();
        FlushCurveChanges();
    }

    private async void SavePresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCommitSelectedEditor(out var error)) { SaveStatusText.Text = error; return; }
        QueueCurveChanges();
        if (!FlushCurveChanges()) return;
        var nameBox = new TextBox { PlaceholderText = T("PresetNamePlaceholder") };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock { Text = T("PresetNamePrompt"), TextWrapping = TextWrapping.Wrap });
        content.Children.Add(nameBox);
        var dialog = new ContentDialog
        {
            Title = T("PresetSaveTitle"), Content = content, PrimaryButtonText = T("SavePreset"), CloseButtonText = T("CancelButton"),
            DefaultButton = ContentDialogButton.Primary, XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var name = nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64) { SaveStatusText.Text = T("PresetNameInvalid"); return; }
        if (string.Equals(name, _settings.ActiveCurvePreset, StringComparison.OrdinalIgnoreCase))
        {
            SaveStatusText.Text = T("PresetSavedStatus");
            return;
        }
        var index = _draftPresets.FindIndex(preset => string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));
        var preset = new NamedBrightnessCurve(name, _draftCurve.ToList());
        if (index >= 0) _draftPresets[index] = preset;
        else _draftPresets.Add(preset);
        _selectedPresetName = name;
        RebuildPresetSelector();
        QueueCurveChanges();
        if (FlushCurveChanges()) SaveStatusText.Text = T("PresetSavedStatus");
    }

    private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_draftPresets.Count <= 1) { SaveStatusText.Text = T("LastPresetDeleteError"); return; }
        var updatedPresets = _draftPresets.Where(preset => !string.Equals(preset.Name, _selectedPresetName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (updatedPresets.Count == _draftPresets.Count) return;
        _draftPresets = updatedPresets;
        _selectedPresetName = updatedPresets[0].Name;
        _draftCurve = updatedPresets[0].Points.ToList();
        _selectedPointIndex = 0;
        RebuildPresetSelector();
        SyncSelectedPointFields();
        DrawCurve();
        QueueCurveChanges();
        if (FlushCurveChanges()) SaveStatusText.Text = T("PresetDeletedStatus");
    }
}
