using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace AutoDarkModeApp;

/// <summary>A hidden anchor window for a native desktop MenuFlyout, independent of the settings window.</summary>
internal sealed class TrayMenuWindow : Window
{
    private readonly Grid anchor = new() { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
    private readonly CancellationTokenSource lifetime = new();
    private readonly int ownerPid;
    private MenuFlyout? menu;
    private TaskCompletionSource<int>? selection;
    private int selectedId = -1;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer ownerTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer focusTimer;

    public TrayMenuWindow(int ownerPid)
    {
        this.ownerPid = ownerPid;
        Title = TrayMenuProtocol.WindowTitle;
        Content = anchor;
        AppWindow.IsShownInSwitchers = false;
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.SetPresenter(presenter);
        MakeAnchorTransparent();
        AppWindow.Resize(new SizeInt32(1, 1));
        AppWindow.Hide();
        focusTimer = DispatcherQueue.CreateTimer();
        focusTimer.Interval = TimeSpan.FromMilliseconds(100);
        focusTimer.Tick += (_, _) => DismissIfInactive();
        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                DispatcherQueue.TryEnqueue(DismissIfInactive);
        };
        Closed += (_, _) => { focusTimer.Stop(); lifetime.Cancel(); selection?.TrySetResult(-1); };
        ownerTimer = DispatcherQueue.CreateTimer();
        ownerTimer.Interval = TimeSpan.FromSeconds(2);
        ownerTimer.Tick += (_, _) =>
        {
            try { using var process = Process.GetProcessById(ownerPid); if (!process.HasExited) return; }
            catch (ArgumentException) { }
            ownerTimer.Stop();
            Close();
            Application.Current.Exit();
        };
        ownerTimer.Start();
        _ = ListenAsync();
    }

    private void MakeAnchorTransparent()
    {
        // A transparent XAML brush does not make the native HWND transparent.
        // Windows can also enforce a minimum size despite Resize(1, 1). Hide the
        // entire anchor surface; the unconstrained flyout owns a separate HWND.
        const int extendedStyle = -20;
        const int layered = 0x00080000;
        const uint alpha = 0x00000002;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        int style = GetWindowLong(hwnd, extendedStyle);
        Marshal.SetLastPInvokeError(0);
        if (SetWindowLong(hwnd, extendedStyle, style | layered) == 0 && Marshal.GetLastPInvokeError() != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        if (!SetLayeredWindowAttributes(hwnd, 0, 0, alpha))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte opacity, uint flags);

    private void DismissIfInactive()
    {
        if (menu?.IsOpen != true) return;
        var foreground = GetForegroundWindow();
        // An unconstrained MenuFlyout has its own window. Focus moving to that
        // window is internal, but clicking the desktop or another app dismisses it.
        GetWindowThreadProcessId(foreground, out var processId);
        if (foreground == IntPtr.Zero || processId != (uint)Environment.ProcessId)
            menu.Hide();
    }

    private void KeepMenuOnTop()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var threadId = GetWindowThreadProcessId(hwnd, out _);
        // This dedicated process contains only the anchor and its native flyout.
        // Promote the popup too: setting the anchor presenter alone is insufficient.
        EnumThreadWindows(threadId, (window, _) =>
        {
            if (IsWindowVisible(window))
                SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
            return true;
        }, IntPtr.Zero);
    }

    private delegate bool WindowCallback(IntPtr hwnd, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    private async Task ListenAsync()
    {
        while (!lifetime.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(TrayMenuProtocol.PipeName(ownerPid), PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(lifetime.Token);
                using var reader = new StreamReader(pipe, leaveOpen: true);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
                var json = await reader.ReadLineAsync(lifetime.Token);
                var model = JsonSerializer.Deserialize<TrayMenuModel>(json ?? "");
                if (model == null) continue;
                var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!DispatcherQueue.TryEnqueue(() => ShowMenu(model, completion))) break;
                var id = await completion.Task.WaitAsync(lifetime.Token);
                await writer.WriteLineAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Debug.WriteLine($"Tray menu: {ex}"); }
        }
    }

    private void ShowMenu(TrayMenuModel model, TaskCompletionSource<int> completion)
    {
        try
        {
            menu?.Hide();
            selection = completion;
            selectedId = -1;
            anchor.RequestedTheme = model.Dark ? ElementTheme.Dark : ElementTheme.Light;
            AppWindow.Move(new PointInt32(model.X, model.Y));
            menu = new MenuFlyout { ShouldConstrainToRootBounds = false };
            foreach (var entry in model.Items)
            {
                if (entry.Separator) { menu.Items.Add(new MenuFlyoutSeparator()); continue; }
                MenuFlyoutItemBase item;
                if (entry.Checkable)
                {
                    var toggle = new ToggleMenuFlyoutItem { Text = entry.Text, IsChecked = entry.Checked, IsEnabled = entry.Enabled };
                    toggle.Click += (_, _) => selectedId = entry.Id;
                    item = toggle;
                }
                else
                {
                    var action = new MenuFlyoutItem { Text = entry.Text, IsEnabled = entry.Enabled };
                    if (!string.IsNullOrWhiteSpace(entry.Glyph)) action.Icon = new FontIcon { Glyph = entry.Glyph };
                    action.Click += (_, _) => selectedId = entry.Id;
                    item = action;
                }
                menu.Items.Add(item);
            }
            menu.Closed += (_, _) =>
            {
                focusTimer.Stop();
                AppWindow.Hide();
                completion.TrySetResult(selectedId);
            };
            menu.Opened += (_, _) =>
            {
                KeepMenuOnTop();
                focusTimer.Start();
            };
            Activate();
            SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try { menu.ShowAt(anchor, new FlyoutShowOptions { Position = new Windows.Foundation.Point(0, 0), Placement = FlyoutPlacementMode.TopEdgeAlignedRight, ShowMode = FlyoutShowMode.Standard }); }
                catch (Exception ex) { AppWindow.Hide(); completion.TrySetException(ex); }
            });
        }
        catch (Exception ex) { AppWindow.Hide(); completion.TrySetException(ex); }
    }
}
