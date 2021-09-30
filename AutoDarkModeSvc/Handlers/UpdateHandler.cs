using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AutoDarkModeSvc.Handlers
{
    static class UpdateHandler
    {
        private const string defaultVersionQueryUrl = "https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version.yaml";
        private const string defaultDownloadBaseUrl = "https://github.com";
        private static readonly Version minUpdaterVersion = new("2.0");
        private static readonly Version maxUpdaterVersion = new("2.99");
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static ApiResponse UpstreamResponse { get; private set; } = new();
        public static UpdateInfo UpstreamVersion { get; private set; } = new();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private static readonly Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly NumberFormatInfo nfi = new NumberFormatInfo();
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
            ApiResponse response = new();
            try
            {
                if (builder.Config.Updater.ZipCustomUrl != null && builder.Config.Updater.HashCustomUrl != null)
                {
                    UpdateInfo info = new()
                    {
                        PathChecksum = "",
                        PathFile = "",
                        Message = "Update with custom URLs. Use at your own risk!",
                        Tag = "420.69",
                        AutoUpdateAvailable = true
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

                using RedirectWebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                webClient.Headers.Add("Cache-Control", "no-cache");
                string data = webClient.DownloadString(GetUpdateUrl());
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
                else
                {
                    response.StatusCode = StatusCode.Ok;
                    response.Message = "No updates available";
                    UpstreamResponse = response;
                    return response;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "update check failed");
                return new ApiResponse()
                {
                    StatusCode = StatusCode.Err,
                    Message = ex.Message
                };
            }
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
            if (!Extensions.InstallModeUsers())
            {
                Logger.Warn("installed in for all users mode, auto updates are disabled");
                return new ApiResponse
                {
                    StatusCode = StatusCode.UnsupportedOperation,
                    Message = "installed for all users, auto updates are disabled"
                };
            }

            if (UpstreamResponse.StatusCode == StatusCode.New)
            {
                Version newVersion = new(UpstreamVersion.Tag);
                if (newVersion.Major != currentVersion.Major && newVersion.Major != 420)
                {
                    return new ApiResponse
                    {
                        StatusCode = StatusCode.Disabled,
                        Message = "major version upgrade pending, manual update required"
                    };
                }
                try
                {
                    Version updaterVersion = new Version(UpstreamVersion.UpdaterVersion);
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
            bool notifyShell = result.Item2;
            bool notifyApp = result.Item3;

            if (!success)
            {
                ToastHandler.InvokeFailedUpdateToast();
                Updating = false;
                return;
            }
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not clear progress toast:");
            }

            Logger.Info("updater patch complete");

            Updating = false;
            if (notifyShell || notifyApp)
            {
                List<string> arguments = new();
                arguments.Add("--notify");
                arguments.Add(notifyShell.ToString());
                arguments.Add(notifyApp.ToString());
                Process.Start(Extensions.ExecutionPathUpdater, arguments);
            }
            else
            {
                Process.Start(Extensions.ExecutionPathUpdater);
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
            bool shellRestart = false;
            bool appRestart = false;
            Progress = 0;

            string baseZipUrl = GetBaseUrl();
            string baseUrlHash = GetBaseUrl();
            bool useCustomUrls = false;
            if (builder.Config.Updater.ZipCustomUrl != null && builder.Config.Updater.HashCustomUrl != null)
            {
                baseZipUrl = builder.Config.Updater.ZipCustomUrl;
                baseUrlHash = builder.Config.Updater.HashCustomUrl;
                useCustomUrls = true;
            }
            string downloadPath = Path.Combine(Extensions.UpdateDataDir, "Update.zip");
            try
            {
                // show toast if UI components were open to inform the user that the program is being updated
                if (!builder.Config.Updater.Silent || overrideSilent)
                {
                    ToastHandler.InvokeUpdateInProgressToast();
                }

                //download zip file file
                Logger.Info("downloading new version");
                using WebClient webClient = new();
                webClient.Proxy = null;
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                webClient.Headers.Add("Cache-Control", "no-cache");
                byte[] buffer = webClient.DownloadData(UpstreamVersion.GetUpdateHashUrl(baseUrlHash, useCustomUrls));
                string expectedHash = Encoding.ASCII.GetString(buffer);

                if (!Directory.Exists(Extensions.UpdateDataDir))
                {
                    Directory.CreateDirectory(Extensions.UpdateDataDir);
                }
                else
                {
                    Logger.Warn("found unclean update dir state, cleaning up first");
                    Directory.Delete(Extensions.UpdateDataDir, true);
                    Directory.CreateDirectory(Extensions.UpdateDataDir);
                }


                DownloadProgressChangedEventHandler callback = new DownloadProgressChangedEventHandler(DownloadProgress);
                webClient.DownloadProgressChanged += callback;
                Task.Run(async () => await webClient.DownloadFileTaskAsync(new Uri(UpstreamVersion.GetUpdateUrl(baseZipUrl, useCustomUrls)), downloadPath)).Wait();
                webClient.DownloadProgressChanged -= callback;


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
                Logger.Error(ex, "updating failed:");
                return (false, false, false);
            }

            string unpackDirectory = Path.Combine(Extensions.UpdateDataDir, "unpacked");
            try
            {
                // unzip download data if hash is valid and update the updater
                Directory.CreateDirectory(unpackDirectory);
                ZipFile.ExtractToDirectory(downloadPath, unpackDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "updating failed while extracting update data:");
                return (false, false, false);
            }

            // kill auto dark mode app and shell if they were running to avoid file move/delete issues
            try
            {
                Process[] pShell = Process.GetProcessesByName("AutoDarkModeShell");
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");

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
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "other auto dark mode components still running, skipping update");
                return (false, false, false);
            }

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

        private static bool UpdateUpdater(string unpackDirectory)
        {
            string tempDir = Path.Combine(Extensions.ExecutionDir, "Temp");
            if (Directory.Exists(tempDir))
            {
                Logger.Warn($"Temp directory {tempDir} already exists, cleaning");
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
                if (Directory.Exists(Extensions.ExecutionDirUpdater))
                {
                    Directory.Move(Extensions.ExecutionDirUpdater, tempDir);
                }
                else
                {
                    Logger.Warn("huh, the updater is missing. trying to restore updater");
                }
                string newUpdaterPath = Path.Combine(unpackDirectory, Extensions.UpdaterDirName);
                Directory.Move(newUpdaterPath, Extensions.ExecutionDirUpdater);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not move updater files directory, rolling back");
                Directory.Move(tempDir, Extensions.ExecutionDirUpdater);
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

        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > Progress)
            {
                if (e.ProgressPercentage % 10 == 0)
                {
                    string mbReceived = (e.BytesReceived / 1000000).ToString();
                    string mbTotal = (e.TotalBytesToReceive / 1000000).ToString();
                    nfi.NumberDecimalSeparator = ".";
                    string progressString = (e.ProgressPercentage / 100d).ToString(nfi);
                    Progress = e.ProgressPercentage;
                    Logger.Info($"downloaded {mbReceived} of {mbTotal} MB. {e.ProgressPercentage} % complete");
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

    class RedirectWebClient : WebClient
    {
        Uri _responseUri;

        public Uri ResponseUri
        {
            get { return _responseUri; }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            _responseUri = response.ResponseUri;
            return response;
        }
    }
}
