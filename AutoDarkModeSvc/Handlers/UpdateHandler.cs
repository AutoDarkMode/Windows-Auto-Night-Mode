using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Windows.UI.Notifications;

namespace AutoDarkModeSvc.Handlers
{
    static class UpdateHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ApiResponse upstreamResponse = new();
        private static UpdateInfo upstreamVersion = new();


        /// <summary>
        /// Checks if a new version is available
        /// </summary>
        /// <returns>version string with download url</returns>
        public static string CheckNewVersion()
        {
            ApiResponse response = new ApiResponse();
            try
            {
                string updateUrl = "https://raw.githubusercontent.com/Armin2208/Windows-Auto-Night-Mode/master/version.yaml";
                using WebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                string data = webClient.DownloadString(updateUrl);
                upstreamVersion = UpdateInfo.Deserialize(data);
                Version newVersion = new(upstreamVersion.Tag);

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion.CompareTo(newVersion) < 0)
                {
                    Logger.Info($"new version {newVersion} available");
                    response.StatusCode = StatusCode.New;
                    response.Message = currentVersion.ToString();
                    response.Details = data;
                    upstreamResponse = response;
                    return response.ToString();
                }
                else
                {
                    return StatusCode.Ok;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "update check failed");
            }
            return StatusCode.Err;
        }

        public static ApiResponse CanAutoUpdate()
        {
            if (Extensions.InstallModeUsers())
            {
                if (!upstreamVersion.AutoUpdateAvailable)
                {
                    Logger.Info("auto update blocked by upstream, please update manually");
                    return new ApiResponse
                    {
                        StatusCode = StatusCode.No,
                        Message = "auto update blocked by upstream"
                    };
                }

                if (!Directory.Exists(Extensions.ExecutionDirUpdater))
                {
                    Logger.Error("updater missing, please re-install AutoDarkMode to re-instate update functionality");
                    return new ApiResponse
                    {
                        StatusCode = StatusCode.Err,
                        Message = "updater broken"
                    };
                }
                return upstreamResponse;
            }
            else
            {
                Logger.Warn("installed in for all users mode, auto updates are disabled");
                return new ApiResponse
                {
                    StatusCode = StatusCode.UnsupportedOperation,
                    Message = "installed for all users, auto updates are disabled"
                };
            }
        }

        /// <summary>
        /// Prepares the update process by upgrading the updater
        /// </summary>
        /// <returns>A bool tuple where the first item holds the value whether the update has been successfully prepared. <br></br>
        /// The second item is to determine whether a notification should be displayed</returns>
        private static (bool, bool) PrepareUpdate()
        {
            bool notifyAboutUpdate = false;
            try
            {
                Process[] pShell = Process.GetProcessesByName("AutoDarkModeShell");
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");
                try
                {
                    if (pShell.Length != 0)
                    {
                        pShell[0].Kill();
                        notifyAboutUpdate = true;
                    }
                    if (pApp.Length != 0)
                    {
                        pApp[0].Kill();
                        notifyAboutUpdate = true;
                    }

                    if (notifyAboutUpdate)
                    {
                        var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
                        var text = xml.GetElementsByTagName("text");
                        text[0].AppendChild(xml.CreateTextNode("Auto Dark Mode is updating"));
                        text[1].AppendChild(xml.CreateTextNode("Please wait until the update is complete"));
                        var toast = new ToastNotification(xml);
                        ToastNotificationManager.CreateToastNotifier("AutoDarkModeSvc").Show(toast);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "other auto dark mode components still running, skipping update until next run");
                    return (false, false);
                }

                Logger.Info("downloading new version");
                using WebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                byte[] buffer = webClient.DownloadData(upstreamVersion.GetUpdateHashUrl());
                string expectedHash = Encoding.ASCII.GetString(buffer);

                //download file
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
                string downloadPath = Path.Combine(Extensions.UpdateDataDir, "Update.zip");
                webClient.DownloadFile(upstreamVersion.GetUpdateUrl(), downloadPath);

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
                    throw new ArgumentException($"updating failed, hash mismatch, expected: {expectedHash}, got: {downloadHashString}");
                }

                // unzip download data if hash is valid and update the "updater"
                string unpackDirectory = Path.Combine(Extensions.UpdateDataDir, "unpacked");
                Directory.CreateDirectory(unpackDirectory);
                ZipFile.ExtractToDirectory(downloadPath, unpackDirectory, true);
                UpdateUpdater(unpackDirectory);
                return (true, notifyAboutUpdate);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "updating failed");
                return (false, false);
            }
        }

        public static void Update()
        {
            if (upstreamResponse.StatusCode != StatusCode.New)
            {
                Logger.Info("updater called, but no newer cached upstream version available");
                return;
            }

            (bool, bool) result = PrepareUpdate();
            bool success = result.Item1;
            bool notify = result.Item2;
            
            if (!success)
            {
                return;
            }

            Logger.Info("update preparation complete");

            if (notify)
            {
                Process.Start(Extensions.ExecutionPathUpdater, "--notify");
            }
            else
            {
                Process.Start(Extensions.ExecutionPathUpdater);
            }
        }

        private static void UpdateUpdater(string unpackDirectory)
        {
            string tempDir = Path.Combine(Extensions.ExecutionDir, "Temp");
            if (Directory.Exists(tempDir))
            {
                throw new IOException($"Temp directory {tempDir} already exists");
            }
            try
            {
                Logger.Info("applying updater patch");
                Directory.Move(Extensions.ExecutionDirUpdater, tempDir);
                string newUpdaterPath = Path.Combine(unpackDirectory, Extensions.UpdaterDirName);
                Directory.Move(newUpdaterPath, Extensions.ExecutionDirUpdater);
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not move updater files directory");
                Directory.Move(tempDir, Extensions.ExecutionDirUpdater);
            }
        }
    }
}
