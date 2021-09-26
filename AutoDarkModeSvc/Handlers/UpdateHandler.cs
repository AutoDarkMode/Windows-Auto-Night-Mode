using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace AutoDarkModeSvc.Handlers
{
    static class UpdateHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static ApiResponse UpstreamResponse { get; private set; } = new();
        public static UpdateInfo UpstreamVersion { get; private set; } = new();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        /// <summary>
        /// Checks if a new version is available
        /// </summary>
        /// <returns>version string with download url</returns>
        public static string CheckNewVersion()
        {
            ApiResponse response = new();
            try
            {
                string updateUrl = "https://raw.githubusercontent.com/Armin2208/Windows-Auto-Night-Mode/master/version.yaml";
                using RedirectWebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                string data = webClient.DownloadString(updateUrl);
                UpstreamVersion = UpdateInfo.Deserialize(data);
                Version newVersion = new(UpstreamVersion.Tag);

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion.CompareTo(newVersion) < 0)
                {
                    Logger.Info($"new version {newVersion} available");
                    response.StatusCode = StatusCode.New;
                    response.Message = currentVersion.ToString();
                    response.Details = data;
                    UpstreamResponse = response;
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

        /// <summary>
        /// Checks if auto update is allowed <br/>
        /// If the service has been installed in all users mode, this is always disabled <br/>
        /// If aute auto install functionality has been disabled in the config file, also disallow
        /// </summary>
        /// <returns>An ApiResponse with UnsupportedOperation if auto install is unavailable. <br/>
        /// If auto install is available, the latest update check response will be returned instead</returns>
        public static ApiResponse CanAutoInstall()
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
            return UpstreamResponse;
        }

        public static void Update()
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

            (bool, bool, bool) result = PrepareUpdate();
            bool success = result.Item1;
            bool notifyShell = result.Item2;
            bool notifyApp = result.Item3;

            if (!success)
            {
                return;
            }

            Logger.Info("update preparation complete");

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
        private static (bool, bool, bool) PrepareUpdate()
        {
            bool shellRestart = false;
            bool appRestart = false;
            string baseUrl = UpdateInfo.baseUrl;
            string baseUrlHash = UpdateInfo.baseUrl;
            if (builder.Config.Updater.UpdateZipCustomUrl.Length != 0)
            {
                baseUrl = builder.Config.Updater.UpdateZipCustomUrl;
            }
            if (builder.Config.Updater.UpdateHashCustomUrl.Length != 0)
            {
                baseUrlHash = builder.Config.Updater.UpdateHashCustomUrl;
            }
            string downloadPath = Path.Combine(Extensions.UpdateDataDir, "Update.zip");
            try
            {
                Process[] pShell = Process.GetProcessesByName("AutoDarkModeShell");
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");

                // kill auto dark mode app and shell if they were running to avoid file move/delete issues
                try
                {
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

                    // show toast if UI components were open to inform the user that the program is being updated
                    if (shellRestart || appRestart)
                    {
                        Windows.Data.Xml.Dom.XmlDocument xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
                        Windows.Data.Xml.Dom.XmlNodeList text = xml.GetElementsByTagName("text");

                        _ = text[0].AppendChild(xml.CreateTextNode("Auto Dark Mode is updating"));
                        _ = text[1].AppendChild(xml.CreateTextNode("Please wait until the update is complete"));
                        var toast = new ToastNotification(xml);
                        ToastNotificationManager.CreateToastNotifier("AutoDarkModeSvc").Show(toast);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "other auto dark mode components still running, skipping update");
                    return (false, false, false);
                }


                //download zip file file
                Logger.Info("downloading new version");
                using WebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                byte[] buffer = webClient.DownloadData(UpstreamVersion.GetUpdateHashUrl(baseUrlHash));
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
                webClient.DownloadFile(UpstreamVersion.GetUpdateUrl(baseUrl), downloadPath);

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


        public static void NotifyFailedUpdate()
        {
            Windows.Data.Xml.Dom.XmlDocument xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
            Windows.Data.Xml.Dom.XmlNodeList text = xml.GetElementsByTagName("text");

            _ = text[0].AppendChild(xml.CreateTextNode("Auto Dark Mode Update failed"));
            _ = text[1].AppendChild(xml.CreateTextNode("An error occurred while updating."));
            _ = text[2].AppendChild(xml.CreateTextNode("Please see service.log and updater.log for more infos"));
            var toast = new ToastNotification(xml);
            ToastNotificationManager.CreateToastNotifier("AutoDarkModeSvc").Show(toast);
        }

        public static void NotifyUpdateAvailable()
        {
            Windows.Data.Xml.Dom.XmlDocument xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
            Windows.Data.Xml.Dom.XmlNodeList text = xml.GetElementsByTagName("text");
            _ = text[0].AppendChild(xml.CreateTextNode("Auto Dark Mode Update"));
            _ = text[1].AppendChild(xml.CreateTextNode($"Version {UpstreamVersion.Tag} is available"));
            _ = text[2].AppendChild(xml.CreateTextNode($"You have Version {Assembly.GetExecutingAssembly().GetName().Version}"));
            var toast = new ToastNotification(xml);
            ToastNotificationManager.CreateToastNotifier("AutoDarkModeSvc").Show(toast);
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
