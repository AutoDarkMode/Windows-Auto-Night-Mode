using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoDarkModeApp.Handlers;

using AdmExtensions = AutoDarkModeLib.Helper;

namespace AutoDarkModeApp.Utils;

public class VersionInfo
{
    public VersionInfo()
    {
        var currentDirectory = AdmExtensions.ExecutionDir;

        Commit = AdmExtensions.CommitHash();
        Svc = ValueOrNotFound(() =>
            FileVersionInfo.GetVersionInfo(currentDirectory + @"\AutoDarkModeSvc.exe")?.FileVersion);
        Updater = ValueOrNotFound(() =>
            FileVersionInfo.GetVersionInfo(AdmExtensions.ExecutionPathUpdater)?.FileVersion);
        Shell = ValueOrNotFound(() =>
            FileVersionInfo.GetVersionInfo(currentDirectory + @"\AutoDarkModeShell.exe")?.FileVersion);
        NetCore = ValueOrNotFound(() => Environment.Version.ToString());
        WindowsVersion = ValueOrNotFound(() => $"{Environment.OSVersion.Version.Build}.{RegistryHandler.GetUbr()}");
        Arch = RuntimeInformation.ProcessArchitecture.ToString();

        static string ValueOrNotFound(Func<string?> value)
        {
            try
            {
                return value() ?? "not found";
            }
            catch
            {
                return "not found";
            }
        }
    }

    public string Commit { get; }
    public string Svc { get; }
    public string Updater { get; }
    public string Shell { get; }
    public string NetCore { get; }
    public string WindowsVersion { get; }
    public string Arch { get; }
}
