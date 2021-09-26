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

namespace AutoDarkModeSvc.Handlers
{
    static class UpdateHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ApiResponse upstreamVersion = new();

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
                UpdateInfo info = UpdateInfo.Deserialize(data);
                Version newVersion = new(info.Tag);

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion.CompareTo(newVersion) < 0)
                {
                    Logger.Info($"new version {newVersion} available");
                    response.StatusCode = StatusCode.New;
                    response.Message = newVersion.ToString();
                    response.Details = data;
                    upstreamVersion = response;
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

        private static string GetUpdateUrl(string tag, string fileName)
        {
            //return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{tag}/{fileName}";
            return "https://cloud.walzen.org/s/s5Jdw3C2FzjXMzq/download/AdmUpdateTest.zip";
        }

        private static string GetUpdateHashUrl(string tag, string fileName)
        {
            //return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{tag}/{fileName}";
            return "https://cloud.walzen.org/s/gcRt7yteSpTRkcK/download/zip_hash.sha256";
        }


        public static ApiResponse CanUpdate()
        {

            if (Extensions.CanAutoUpdate())
            {
                if (!Directory.Exists(Extensions.ExecutionDirUpdater))
                {
                    Logger.Error("updater missing, please re-install AutoDarkMode to re-instate update functionality");
                    return new ApiResponse
                    {
                        StatusCode = StatusCode.Err,
                        Message = "updater broken"
                    };
                }
                return upstreamVersion;
            }
            else
            {
                return new ApiResponse
                {
                    StatusCode = StatusCode.UnsupportedOperation,
                    Message = "installed for all users, cannot auto update"
                };
            }
        }

        /// <summary>
        /// Prepares the update process by upgrading the updater
        /// </summary>
        /// <returns>A bool tuple where the first item holds the value whether the update has been successfully prepared. <br></br>
        /// The second item is to determine whether a notification should be displayed</returns>
        public static (bool, bool) PrepareUpdate()
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
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "other auto dark mode components still running, skipping update until next run");
                    return (false, false);
                }

                Logger.Info("downloading new version");
                UpdateInfo info = UpdateInfo.Deserialize(upstreamVersion.Details);
                using WebClient webClient = new();
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                byte[] buffer = webClient.DownloadData(GetUpdateHashUrl(info.Tag, "zip_hash.sha256"));
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
                webClient.DownloadFile(GetUpdateUrl(info.Tag, info.FileName), downloadPath);

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
            (bool, bool) result = PrepareUpdate();
            bool success = result.Item1;
            bool notify = result.Item2;
            
            if (!success)
            {
                return;
            }

            Logger.Info("update preparation complete. shutting down");

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
