using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace AutoDarkModeSvc.Modules
{
    class UpdaterModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool firstRun = true;
        private bool checkFailed;
        public UpdaterModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            try
            {
                builder.LoadUpdaterData();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not load last update time:");
            }
        }
        public override string TimerAffinity => TimerName.IO;

        public override void Fire()
        {
            _ = Task.Run(() =>
            {
                Check();
            });
            //Updater();
        }

        /// <summary>
        /// Checks for updates and applies them if enabled and available
        /// </summary>
        private void Check()
        {
            try
            {
                TimeSpan PollingCooldownTimeSpan = TimeSpan.FromDays(builder.Config.Updater.DaysBetweenUpdateCheck);
                DateTime nextUpdate = builder.UpdaterData.LastCheck.Add(PollingCooldownTimeSpan);
                if (DateTime.Now >= nextUpdate || (firstRun && builder.Config.Updater.CheckOnStart) || checkFailed)
                {
                    //Logger.Debug("performing update check");
                    firstRun = false;
                    checkFailed = false;
                    _ = UpdateHandler.CheckNewVersion();
                    ApiResponse versionCheck = UpdateHandler.UpstreamResponse;

                    // check if a new version is available upstream
                    if (versionCheck.StatusCode == StatusCode.New)
                    {
                        ApiResponse canUseUpdater = UpdateHandler.CanUseUpdater();
                        // will pass through the update message if auto updater can be used
                        if (canUseUpdater.StatusCode == StatusCode.New)
                        {
                            // if mode is not silent, or auto install is disabled, show the notification to prompt the user
                            if (!builder.Config.Updater.Silent || !builder.Config.Updater.AutoInstall)
                            {
                                ToastHandler.InvokeUpdateToast();
                            }
                            if (builder.Config.Updater.AutoInstall)
                            {
                                Task.Run(() => UpdateHandler.Update()).Wait();
                            }
                        }
                        // display notification without update options if unavailable
                        else if (canUseUpdater.StatusCode == StatusCode.UnsupportedOperation || canUseUpdater.StatusCode == StatusCode.Disabled)
                        {
                            ToastHandler.InvokeUpdateToast(canUseUpdater: false);
                        }
                    }
                    else if (versionCheck.StatusCode == StatusCode.Err && versionCheck.Details != null && versionCheck.Details.Equals("WebException"))
                    {
                        Logger.Warn("rescheduling update check on next timer tick");
                        checkFailed = true;
                    }
                }
                else
                {
                    Logger.Debug($"Next update check scheduled: {nextUpdate}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while running update checker:");
            }
        }
    }
}
