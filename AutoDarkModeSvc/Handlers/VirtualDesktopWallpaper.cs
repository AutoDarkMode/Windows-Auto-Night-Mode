// COM declarations adapted from MScholtes/VirtualDesktop (MIT).
// See ThirdParty/VirtualDesktop-LICENSE.txt and docs/virtual-desktop-wallpaper.md.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoDarkModeSvc.Handlers;

/// <summary>Synchronizes Explorer's per-virtual-desktop wallpapers without switching desktops.</summary>
internal static class VirtualDesktopWallpaper
{
    internal sealed record DesktopWallpaper(Guid Id, string Path);

    // This internal COM ABI is build-specific. Never guess vtable layouts on a new build.
    internal static bool IsSupportedBuild(int build) => AutoDarkModeLib.WallpaperSynchronizationPolicy.IsSupportedBuild(build);

    internal static IReadOnlyList<DesktopWallpaper> ReadWallpapers() => InSta(() => WithManager(Read));

    internal static int Synchronize(string wallpaper)
    {
        if (string.IsNullOrWhiteSpace(wallpaper) || !File.Exists(wallpaper))
            throw new FileNotFoundException("The global wallpaper must exist before synchronizing desktops.", wallpaper);
        var path = Path.GetFullPath(wallpaper);
        return InSta(() => WithManager(manager =>
        {
            var before = Read(manager);
            if (AllMatch(before, path)) return before.Count;
            IntPtr hstring = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(WindowsCreateString(path, path.Length, out hstring));
                manager.UpdateWallpaperPathForAllDesktops(hstring);
            }
            finally { if (hstring != IntPtr.Zero) WindowsDeleteString(hstring); }
            var after = Read(manager);
            if (!AllMatch(after, path))
                throw new InvalidOperationException("Explorer did not synchronize the wallpaper for every virtual desktop.");
            return after.Count;
        }));
    }

    internal static bool AllMatch(IReadOnlyList<DesktopWallpaper> desktops, string path)
    {
        if (desktops.Count == 0) return false;
        foreach (var desktop in desktops)
            if (!string.Equals(desktop.Path, path, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private static IReadOnlyList<DesktopWallpaper> Read(IVirtualDesktopManagerInternal manager)
    {
        manager.GetDesktops(out var desktops);
        try
        {
            desktops.GetCount(out var count);
            var result = new List<DesktopWallpaper>(count);
            for (var i = 0; i < count; i++)
            {
                var iid = typeof(IVirtualDesktop).GUID;
                desktops.GetAt(i, ref iid, out var value);
                try
                {
                    var desktop = (IVirtualDesktop)value;
                    IntPtr text = desktop.GetWallpaperPath();
                    try
                    {
                        var buffer = WindowsGetStringRawBuffer(text, out var length);
                        result.Add(new(desktop.GetId(), Marshal.PtrToStringUni(buffer, (int)length) ?? ""));
                    }
                    finally { if (text != IntPtr.Zero) WindowsDeleteString(text); }
                }
                finally { Marshal.ReleaseComObject(value); }
            }
            return result;
        }
        finally { Marshal.ReleaseComObject(desktops); }
    }

    private static T WithManager<T>(Func<IVirtualDesktopManagerInternal, T> action)
    {
        if (!IsSupportedBuild(Environment.OSVersion.Version.Build))
            throw new PlatformNotSupportedException("The virtual desktop wallpaper ABI is not verified on this Windows build.");
        object shell = null;
        object manager = null;
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2F03A33-21F5-47FA-B4BB-156362A2F239")));
            var service = new Guid("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
            var iid = typeof(IVirtualDesktopManagerInternal).GUID;
            manager = ((IServiceProvider)shell).QueryService(ref service, ref iid);
            return action((IVirtualDesktopManagerInternal)manager);
        }
        finally
        {
            if (manager != null) Marshal.ReleaseComObject(manager);
            if (shell != null) Marshal.ReleaseComObject(shell);
        }
    }

    private static T InSta<T>(Func<T> action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) return action();
        T result = default;
        Exception error = null;
        var thread = new Thread(() => { try { result = action(); } catch (Exception ex) { error = ex; } })
        { IsBackground = true, Name = "VirtualDesktopWallpaper" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
        return result;
    }

    [DllImport("combase.dll", CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string value, int length, out IntPtr hstring);
    [DllImport("combase.dll")] private static extern int WindowsDeleteString(IntPtr hstring);
    [DllImport("combase.dll")] private static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out uint length);

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.IUnknown)] object QueryService(ref Guid service, ref Guid iid);
    }

    [ComImport, Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IObjectArray
    {
        void GetCount(out int count);
        void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object value);
    }

    [ComImport, Guid("3F07F4BE-B107-441A-AF0F-39D82529072C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktop
    {
        bool IsViewVisible(IntPtr view);
        Guid GetId();
        IntPtr GetName();
        IntPtr GetWallpaperPath();
        bool IsRemote();
    }

    // Windows 11 24H2 layout, including SwitchDesktopAndMoveForegroundView at slot 7.
    [ComImport, Guid("53F5CA0B-158F-4124-900C-057158060B27"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManagerInternal
    {
        int GetCount();
        void MoveViewToDesktop(IntPtr view, IVirtualDesktop desktop);
        bool CanViewMoveDesktops(IntPtr view);
        IVirtualDesktop GetCurrentDesktop();
        void GetDesktops(out IObjectArray desktops);
        [PreserveSig] int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);
        void SwitchDesktop(IVirtualDesktop desktop);
        void SwitchDesktopAndMoveForegroundView(IVirtualDesktop desktop);
        IVirtualDesktop CreateDesktop();
        void MoveDesktop(IVirtualDesktop desktop, int index);
        void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
        IVirtualDesktop FindDesktop(ref Guid id);
        void GetDesktopSwitchIncludeExcludeViews(IVirtualDesktop desktop, out IObjectArray include, out IObjectArray exclude);
        void SetDesktopName(IVirtualDesktop desktop, IntPtr name);
        void SetDesktopWallpaper(IVirtualDesktop desktop, IntPtr path);
        void UpdateWallpaperPathForAllDesktops(IntPtr path);
    }
}
