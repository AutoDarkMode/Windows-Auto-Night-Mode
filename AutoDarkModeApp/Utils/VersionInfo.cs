﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoDarkModeApp.Utils.Handlers;
using AdmExtensions = AutoDarkModeLib.Helper;

namespace AutoDarkModeApp.Utils;

internal class VersionInfo
{
    public string Commit { get; }
    public string Svc { get; }
    public string Updater { get; }
    public string Shell { get; }
    public string NetCore { get; }
    public string WindowsVersion { get; }
    public string Arch { get; }

    public VersionInfo()
    {
        //var currentDirectory = AdmExtensions.ExecutionDir;

        Commit = AdmExtensions.CommitHash();
        Svc = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(AdmExtensions.ExecutionPathService)?.FileVersion);
        Updater = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(AdmExtensions.ExecutionPathUpdater)?.FileVersion);
        Shell = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(AdmExtensions.ExecutionPathShell)?.FileVersion);
        NetCore = ValueOrNotFound(Environment.Version.ToString);
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
}
