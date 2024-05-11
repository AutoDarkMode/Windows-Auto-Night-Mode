#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HttpClientProgress;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading;
using Windows.Media.Protection.PlayReady;
using System.Runtime.InteropServices;

namespace AutoDarkModeSvc.Handlers
{
    static class UpdateHandler
    {
        private const string defaultVersionQueryUrl = "https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version.yaml";
        private const string defaultDownloadBaseUrl = "https://github.com";
        private static readonly Version minUpdaterVersion = new("3.0");
        private static readonly Version maxUpdaterVersion = new("3.99");
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static ApiResponse UpstreamResponse { get; private set; } = new();
        public static UpdateInfo UpstreamVersion { get; private set; } = new();
        public static bool IsARMUpgrade { get; private set; }
        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private static readonly Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly NumberFormatInfo nfi = new();
        private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";

        public static bool Updating
        {
            get; [MethodImpl(MethodImplOptions.Synchronized)]
            set;
        }
        public static int Progress
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks if a new version is available
        /// </summary>
        /// <returns>ApiResponse with StatusCode.New if an update is available, <br/>
        /// StatusCode.Ok if no update is available <br/>
        /// StatusCode.Err if an error has occurred. <br/>
        /// Message carries the current version string <br/>
        /// Details carries a yaml serialized UpdateInfo object</returns>
        public static ApiResponse CheckNewVersion()
        {
            IsARMUpgrade = false;
            ApiResponse response = new();
            try
            {
                try
                {
                    builder.UpdaterData.LastCheck = DateTime.Now;
                    builder.SaveUpdaterData();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not update last update check time:");
                }
                if (builder.Config.Updater.ZipCustomUrl != null && builder.Config.Updater.HashCustomUrl != null)
                {
                    UpdateInfo info = new()
                    {
                        PathChecksum = "",
                        PathFile = "",
                        Message = "Update with custom URLs. Use at your own risk!",
                        Tag = "420.69",
                        AutoUpdateAvailable = true,
                        UpdaterVersion = "3.0",
                        ChangelogUrl = "https://github.com/AutoDarkMode/Windows-Auto-Night-Mode"
                    };
                    Logger.Info($"new custom version available");
                    response.StatusCode = StatusCode.New;
                    response.Message = $"Version: {currentVersion}";
                    response.Details = info.Serialize();
                    UpstreamResponse = response;
                    UpstreamVersion = info;
                    Logger.Warn("custom update urls are set, update at your own risk!");
                    Logger.Warn("do not proceed this except if you know what you are doing!");
                    return response;
                }

                string data = FetchVersionYaml();
                UpstreamVersion = UpdateInfo.Deserialize(data);
                Version newVersion = new(UpstreamVersion.Tag);

                if (currentVersion.CompareTo(newVersion) < 0)
                {
                    Logger.Info($"new version {newVersion} available");
                    response.StatusCode = StatusCode.New;
                    response.Message = $"Version: {currentVersion}";
                    response.Details = data;
                    UpstreamResponse = response;
                    return response;
                }
                else if (RuntimeInformation.OSArchitecture == Architecture.Arm64 
                    && RuntimeInformation.ProcessArchitecture != Architecture.Arm64
                    && currentVersion.CompareTo(newVersion) == 0 && UpstreamVersion.PathFileArm != null)
                {
                    Logger.Info($"upgrade to arm version available");
                    response.StatusCode = StatusCode.New;
                    response.Message = $"Version: {currentVersion} (ARM64)";
                    response.Details = data;
                    UpstreamResponse = response;
                    IsARMUpgrade = true;
                    return response;
                }
                else
                {
                    response.StatusCode = StatusCode.Ok;
                    response.Message = "No updates available";
                    response.Details = data;
                    UpstreamResponse = response;
                    return response;
                }
            }
            catch (System.Net.WebException ex)
            {
                Logger.Error(ex, "update check failed");
                response = new ApiResponse()
                {
                    StatusCode = StatusCode.Err,
                    Message = ex.Message,
                    Details = "WebException"
                };
                UpstreamResponse = response;
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "update check failed");
                response = new ApiResponse()
                {
                    StatusCode = StatusCode.Err,
                    Message = ex.Message
                };
                UpstreamResponse = response;
                return response;
            }
        }

        /// <summary>
        /// Checks whether a downgrade is available to apply <br/>
        /// Updates the UpstreamResponse object <br/>
        /// <returns>
        /// Returns ApiResponse with StatusCode.Downgrade if a downgrade is available, <br/>
        /// StatusCode.Ok if no downgrade is available
        /// </returns>
        /// </summary>
        public static ApiResponse CheckDowngrade()
        {
            if (UpstreamResponse.StatusCode == StatusCode.Ok)
            {
                Version newVersion = new(UpstreamVersion.Tag);
                if (currentVersion.CompareTo(newVersion) > 0)
                {
                    UpstreamResponse.StatusCode = StatusCode.Downgrade;
                    return UpstreamResponse;
                }
            }
            return new()
            {
                StatusCode = StatusCode.No,
                Message = "no downgrade available"
            };
        }

        private static string FetchVersionYaml()
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                MaxAge = TimeSpan.FromSeconds(1),
                MaxStale = false,
                NoStore = true,
            };
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            Task<string> downloadString = client.GetStringAsync(GetUpdateUrl());
            downloadString.Wait();
            return downloadString.Result;
        }

        /// <summary>
        /// Checks if auto update is allowed <br/>
        /// If the service has been installed in all users mode, this is always disabled <br/>
        /// If aute auto install functionality has been disabled in the config file, also disallow
        /// </summary>
        /// <returns>An ApiResponse with UnsupportedOperation if auto install is unavailable. <br/>
        /// If auto install is available, the latest update check response will be returned instead</returns>
        public static ApiResponse CanUseUpdater()
        {
            if (!Helper.InstallModeUsers())
            {
                Logger.Warn("installed in for all users mode, auto updates are disabled");
                return new ApiResponse
                {
                    StatusCode = StatusCode.UnsupportedOperation,
                    Message = "installed for all users, auto updates are disabled"
                };
            }

            if (UpstreamResponse.StatusCode == StatusCode.New || UpstreamResponse.StatusCode == StatusCode.Downgrade)
            {
                try
                {
                    Version updaterVersion = new(UpstreamVersion.UpdaterVersion);
                    if (updaterVersion.CompareTo(minUpdaterVersion) < 0 || updaterVersion.CompareTo(maxUpdaterVersion) > 0)
                    {
                        return new ApiResponse
                        {
                            StatusCode = StatusCode.Disabled,
                            Message = "incompatible updater detected, manual update required"
                        };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "error parsing updater version strings:");
                    return new ApiResponse
                    {
                        StatusCode = StatusCode.Disabled,
                        Message = "updater version could not be determined"
                    };
                }
            }
            return UpstreamResponse;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool Downgrade(bool overrideSilent = false)
        {
            if (UpstreamResponse.StatusCode != StatusCode.Downgrade)
            {
                Logger.Info("updater called, but no cached downgrade upstream version available");
                return false;
            }

            if (!UpstreamVersion.AutoUpdateAvailable)
            {
                Logger.Info("auto update blocked by upstream, please update manually");
                return false;
            }

            Updating = true;
            bool success = GetPatchData(overrideSilent, out _, true);
            if (!success)
            {
                ToastHandler.RemoveUpdaterToast();
                ToastHandler.InvokeFailedUpdateToast();
                Updating = false;
                return false;
            }
            EndBlockingProcesses(out bool shellRestart, out bool appRestart);

            /*
            string futureUpdaterDir = Path.Combine(Helper.ExecutionDir, "UpdaterFuture");
            string futureUpdaterExecutablePath = Path.Combine(futureUpdaterDir, Helper.UpdaterExecutableName);
            try
            {
                if (Directory.Exists(futureUpdaterDir)) Directory.Delete(futureUpdaterDir, true);
                Directory.Move(Helper.ExecutionDirUpdater, futureUpdaterDir);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't rename updater directory for downgarde:");
                ToastHandler.RemoveUpdaterToast();
                ToastHandler.InvokeFailedUpdateToast();
                Updating = false;
                return false;
            }
            */

            Logger.Info("downgrade preparation complete");

            if (shellRestart || appRestart)
            {
                ProcessStartInfo startInfo = new();
                startInfo.ArgumentList.Add("--notify");
                startInfo.ArgumentList.Add(shellRestart.ToString());
                startInfo.ArgumentList.Add(appRestart.ToString());
                startInfo.FileName = Helper.ExecutionPathUpdater;
                startInfo.WorkingDirectory = Helper.ExecutionDirUpdater;
                Process.Start(startInfo);
            }
            else
            {
                Process.Start(Helper.ExecutionPathUpdater);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Update(bool overrideSilent = false)
        {
            if (UpstreamResponse.StatusCode != StatusCode.New)
            {
                Logger.Info("updater called, but no newer cached upstream version available");
                return;
            }

            if (!UpstreamVersion.AutoUpdateAvailable)
            {
                Logger.Info("auto update blocked by upstream, please update manually");
                return;
            }
            Updating = true;
            (bool, bool, bool) result = PrepareUpdate(overrideSilent);
            bool success = result.Item1;
            bool shellRestart = result.Item2;
            bool appRestart = result.Item3;

            if (!success)
            {
                ToastHandler.RemoveUpdaterToast();
                ToastHandler.InvokeFailedUpdateToast();
                Updating = false;
                return;
            }
            try
            {
                ToastHandler.RemoveUpdaterToast();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not clear progress toast:");
            }

            Logger.Info("updater patch complete");

            Updating = false;

            if (shellRestart || appRestart)
            {
                ProcessStartInfo startInfo = new();
                startInfo.ArgumentList.Add("--notify");
                startInfo.ArgumentList.Add(shellRestart.ToString());
                startInfo.ArgumentList.Add(appRestart.ToString());
                startInfo.FileName = Helper.ExecutionPathUpdater;
                startInfo.WorkingDirectory = Helper.ExecutionDirUpdater;
                Process.Start(startInfo);
            }
            else
            {
                ProcessStartInfo startInfo = new();
                startInfo.FileName = Helper.ExecutionPathUpdater;
                startInfo.WorkingDirectory = Helper.ExecutionDirUpdater;
                Process.Start(startInfo);
            }
        }

        /// <summary>
        /// Prepares the update process by upgrading the updater
        /// </summary>
        /// <returns>A bool tuple where the first item holds the value whether the update has been successfully prepared. <br></br>
        /// The second item is to determine whether the shell needs to be restarted <br/>
        /// The third item is to determine whether the app needs to be restarted</returns>
        private static (bool, bool, bool) PrepareUpdate(bool overrideSilent)
        {
            bool success = GetPatchData(overrideSilent, out string unpackDirectory, false);
            if (!success)
            {
                return (false, false, false);
            }
            EndBlockingProcesses(out bool shellRestart, out bool appRestart);
            try
            {
                if (UpdateUpdater(unpackDirectory))
                {
                    return (true, shellRestart, appRestart);
                }
                else
                {
                    Logger.Error("updating failed, rollback successful");
                    return (false, false, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "updater failed, rollback failed, the updater is now missing and needs to be restored on next update attempt:");
                return (false, false, false);
            }
        }

        private static bool GetPatchData(bool overrideSilent, out string unpackDirectory, bool downgrade)
        {
            Progress = 0;
            unpackDirectory = Path.Combine(Helper.UpdateDataDir, "unpacked");
            string baseZipUrl = GetBaseUrl();
            string baseUrlHash = GetBaseUrl();
            bool useCustomUrls = false;
            if (builder.Config.Updater.ZipCustomUrl != null && builder.Config.Updater.HashCustomUrl != null)
            {
                baseZipUrl = builder.Config.Updater.ZipCustomUrl;
                baseUrlHash = builder.Config.Updater.HashCustomUrl;
                useCustomUrls = true;
            }
            string downloadPath = Path.Combine(Helper.UpdateDataDir, "update.zip");
            try
            {
                // show toast if UI components were open to inform the user that the program is being updated
                if (!builder.Config.Updater.Silent || overrideSilent)
                {
                    ToastHandler.InvokeUpdateInProgressToast(UpstreamVersion.Tag, downgrade);
                }

                //download zip file file
                Logger.Info("downloading update data");
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                try
                {
                    client.GetStringAsync(UpstreamVersion.ChangelogUrl).Wait();
                }
                catch (Exception)
                {
                    Logger.Warn("changelog page not found");
                }
                Task<byte[]> hashDownloadTask = client.GetByteArrayAsync(UpstreamVersion.GetUpdateHashUrl(baseUrlHash, useCustomUrls));
                hashDownloadTask.Wait();
                byte[] buffer = hashDownloadTask.Result;

                string expectedHash = Encoding.ASCII.GetString(buffer);

                if (!Directory.Exists(Helper.UpdateDataDir))
                {
                    Directory.CreateDirectory(Helper.UpdateDataDir);
                }
                else
                {
                    Logger.Warn("found unclean update dir state, cleaning up first");
                    Directory.Delete(Helper.UpdateDataDir, true);
                    Directory.CreateDirectory(Helper.UpdateDataDir);
                }

                var progress = new Progress<(float, long, long)>();
                progress.ProgressChanged += DownloadProgress;

                using (var file = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Task zipDownloadTask = client.DownloadDataAsync(UpstreamVersion.GetUpdateUrl(baseZipUrl, useCustomUrls), file, progress);
                    zipDownloadTask.Wait();
                }

                // calculate hash of downloaded file, abort if hash mismatches
                using SHA256 sha256 = SHA256.Create();
                using FileStream fileStream = File.OpenRead(downloadPath);
                byte[] downloadHash = sha256.ComputeHash(fileStream);
                StringBuilder downloadHashStringBuilder = new();
                for (int i = 0; i < downloadHash.Length; i++)
                {
                    downloadHashStringBuilder.Append(downloadHash[i].ToString("x2"));
                }
                string downloadHashString = downloadHashStringBuilder.ToString();

                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                if (comparer.Compare(expectedHash, downloadHashString) != 0)
                {
                    throw new ArgumentException($"hash mismatch, expected: {expectedHash}, got: {downloadHashString}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "downloading patch failed:");
                return false;
            }

            try
            {
                // unzip download data if hash is valid
                Directory.CreateDirectory(unpackDirectory);
                ZipFile.ExtractToDirectory(downloadPath, unpackDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while extracting patch:");
                return false;
            }

            Logger.Info("patch preparation complete");
            return true;
        }

        public static void EndBlockingProcesses(out bool shellRestart, out bool appRestart)
        {
            shellRestart = false;
            appRestart = false;
            Process[] pShell = Array.Empty<Process>();
            Process[] pApp = Array.Empty<Process>();

            // kill auto dark mode app and shell if they were running to avoid file move/delete issues
            var currentSessionID = Process.GetCurrentProcess().SessionId;
            try
            {
                pShell = Process.GetProcessesByName("AutoDarkModeShell").Where(p => p.SessionId == currentSessionID).ToArray();
                pApp = Process.GetProcessesByName("AutoDarkModeApp").Where(p => p.SessionId == currentSessionID).ToArray();

                if (pShell.Length != 0)
                {
                    pShell[0].Kill();
                    shellRestart = true;
                }
                if (pApp.Length != 0)
                {
                    pApp[0].Kill();
                    appRestart = true;
                }

                bool shellExited = false;
                bool appExited = false;

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Thread.Sleep(500);
                        Process[] pShellConfirm = Process.GetProcessesByName("AutoDarkModeShell").Where(p => p.SessionId == currentSessionID).ToArray();
                        Process[] pAppConfirm = Process.GetProcessesByName("AutoDarkModeApp").Where(p => p.SessionId == currentSessionID).ToArray();
                        if (pShellConfirm.Length == 0 && pAppConfirm.Length == 0)
                        {
                            shellExited = true;
                            appExited = true;
                            if (shellRestart || appRestart) Logger.Debug("other auto dark mode components have been stopped");
                            break;
                        }
                        if (pShellConfirm.Length == 0)
                        {
                            appExited = true;
                        }
                        if (pAppConfirm.Length == 0)
                        {
                            shellExited = true;
                        }
                        foreach (Process p in pShellConfirm)
                        {
                            p.Dispose();
                        }
                        foreach (Process p in pAppConfirm)
                        {
                            p.Dispose();
                        }
                        Logger.Debug($"end blocking processes attempt: {i+1}/5");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "could not verify if other auto dark mode components have been stopped:");
                    }
                    if (!shellExited || !appExited)
                    {
                        Logger.Warn("other auto dark mode components still running after grace period");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "other auto dark mode components still running: ");
            }
            finally
            {
                foreach (Process p in pShell)
                {
                    p.Dispose();
                }
                foreach (Process p in pApp)
                {
                    p.Dispose();
                }
            }
        }

        private static bool UpdateUpdater(string unpackDirectory)
        {
            string tempDir = Path.Combine(Helper.ExecutionDir, "Temp");
            if (Directory.Exists(tempDir))
            {
                Logger.Warn($"emp directory {tempDir} already exists, cleaning");
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not clean up updater temp directory, aborting update");
                    return false;
                }
            }
            try
            {
                Logger.Info("applying updater patch");
                if (Directory.Exists(Helper.ExecutionDirUpdater))
                {
                    Directory.Move(Helper.ExecutionDirUpdater, tempDir);
                }
                else
                {
                    Logger.Warn("huh, the updater is missing. trying to restore updater");
                }
                string newUpdaterPath = Path.Combine(unpackDirectory, Helper.UpdaterDirName);
                Directory.Move(newUpdaterPath, Helper.ExecutionDirUpdater);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not move updater files directory, rolling back");
                Directory.Move(tempDir, Helper.ExecutionDirUpdater);
                return false;
            }

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not delete old updater files, manual investigation required");
            }

            Logger.Info("updater patched successfully");
            return true;
        }

        private static void DownloadProgress(object sender, (float, long, long) progress)
        {
            int percent = (int)progress.Item1;
            long totalBytes = progress.Item2;
            long receivedBytes = progress.Item3;

            if (percent > Progress)
            {
                if (percent % 10 == 0)
                {
                    string mbReceived = (receivedBytes / 1000000).ToString();
                    string mbTotal = (totalBytes / 1000000).ToString();
                    nfi.NumberDecimalSeparator = ".";
                    Progress = percent;
                    string progressString = (Progress / 100d).ToString(nfi);
                    Logger.Info($"downloaded {mbReceived} of {mbTotal} MB. {Progress} % complete");
                    try
                    {
                        ToastHandler.UpdateProgressToast(progressString, $"{mbReceived} / {mbTotal} MB");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "toast updater died, please tell the devs to add the toast updater to the ActionQueue:");
                    }
                }
            }
        }

        private static string GetUpdateUrl()
        {
            List<string> blacklistUrls = new()
            {
            };
            string current = builder.Config.Updater.VersionQueryUrl;
            if (current == null)
            {
                return defaultVersionQueryUrl;
            }
            bool blacklisted = blacklistUrls.Contains(current);
            if (blacklisted)
            {
                Logger.Warn($"outdated version query url provided, using recomended url {defaultDownloadBaseUrl}");
                try
                {
                    builder.Config.Updater.VersionQueryUrl = null;
                    builder.Save();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "error saving config file while updating version query url:");
                }
                return defaultVersionQueryUrl;
            }
            else
            {
                return current;
            }
        }

        private static string GetBaseUrl()
        {
            List<string> blacklistUrls = new()
            {
            };
            string current = builder.Config.Updater.DownloadBaseUrl;
            if (current == null)
            {
                return defaultDownloadBaseUrl;
            }
            bool blacklisted = blacklistUrls.Contains(current);
            if (blacklisted)
            {
                Logger.Warn($"outdated update base url provided, using recomended url {defaultDownloadBaseUrl}");
                try
                {
                    builder.Config.Updater.DownloadBaseUrl = null;
                    builder.Save();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "error saving config file while updating base url:");
                }
                return defaultDownloadBaseUrl;
            }
            else
            {
                return current;
            }
        }
    }
}
