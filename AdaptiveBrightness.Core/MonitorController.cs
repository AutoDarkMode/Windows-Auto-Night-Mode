using System.Runtime.InteropServices;

namespace AdaptiveBrightness.Core;

public sealed class MonitorController : IDisposable
{
    private const byte BrightnessVcpCode = 0x10;
    private static readonly TimeSpan MonitorRefreshInterval = TimeSpan.FromSeconds(5);
    private readonly List<PhysicalMonitor> _handles = [];
    private DateTime _lastRefreshUtc = DateTime.MinValue;
    public double? LastTarget { get; set; }

    public double? GetCurrentBrightnessPercent()
    {
        if (DateTime.UtcNow - _lastRefreshUtc >= MonitorRefreshInterval) Refresh();
        var values = new List<double>();
        foreach (var monitor in _handles)
            if (Native.GetVCPFeatureAndVCPFeatureReply(monitor.Handle, BrightnessVcpCode, out _, out var current, out var maximum) && maximum > 0)
                values.Add(current * 100d / maximum);
        return values.Count == 0 ? null : values.Average();
    }

    public string SetBrightness(double percent)
    {
        if (DateTime.UtcNow - _lastRefreshUtc >= MonitorRefreshInterval) Refresh();
        var successes = 0;
        var readFailures = 0;
        var writeFailures = 0;
        foreach (var monitor in _handles)
        {
            // Always read the real VCP maximum; different displays expose different ranges.
            if (!Native.GetVCPFeatureAndVCPFeatureReply(monitor.Handle, BrightnessVcpCode, out _, out var current, out var maximum) || maximum == 0) { readFailures++; continue; }
            var target = (uint)Math.Round(Math.Clamp(percent, 0, 100) / 100d * maximum);
            if (target == current) successes++;
            else if (Native.SetVCPFeature(monitor.Handle, BrightnessVcpCode, target)) successes++;
            else writeFailures++;
        }
        if (successes > 0) LastTarget = percent;
        if (successes > 0) return $"{successes} 台显示器已到目标亮度";
        if (_handles.Count == 0) return "未检测到外接显示器";
        return $"DDC/CI 失败（读取 {readFailures}，写入 {writeFailures}）";
    }

    private void Refresh()
    {
        DisposeHandles();
        Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hmonitor, _, _, _) =>
        {
            if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hmonitor, out var count) || count == 0) return true;
            var monitors = new Native.PhysicalMonitor[count];
            if (!Native.GetPhysicalMonitorsFromHMONITOR(hmonitor, count, monitors)) return true;
            _handles.AddRange(monitors.Select(m => new PhysicalMonitor(m.hPhysicalMonitor)));
            return true;
        }, IntPtr.Zero);
        _lastRefreshUtc = DateTime.UtcNow;
    }

    public void Dispose() => DisposeHandles();
    private void DisposeHandles()
    {
        foreach (var item in _handles) Native.DestroyPhysicalMonitor(item.Handle);
        _handles.Clear();
    }

    private sealed record PhysicalMonitor(IntPtr Handle);

    private static class Native
    {
        internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr data);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct PhysicalMonitor { public IntPtr hPhysicalMonitor; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szPhysicalMonitorDescription; }

        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
        [DllImport("dxva2.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint numberOfPhysicalMonitors);
        [DllImport("dxva2.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint numberOfPhysicalMonitors, [Out] PhysicalMonitor[] physicalMonitorArray);
        [DllImport("dxva2.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte vcpCode, out uint vcpType, out uint currentValue, out uint maximumValue);
        [DllImport("dxva2.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetVCPFeature(IntPtr hMonitor, byte vcpCode, uint newValue);
        [DllImport("dxva2.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);
    }
}
