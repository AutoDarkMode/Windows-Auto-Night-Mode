using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class ColorFilterSwitch : BaseComponent<object>
    {
        private bool currentColorFilterActive;
        public ColorFilterSwitch() : base() { }
        public override bool ThemeHandlerCompatibility => true;
        public override void EnableHook()
        {
            try
            {
                RegistryHandler.ColorFilterSetup();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed to initialize color filter");
            }

            try
            {
                currentColorFilterActive = RegistryHandler.IsColorFilterActive();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize color filter state");
            }

            base.EnableHook();
        }
        public override void DisableHook()
        {
            if (!Settings.Enabled && currentColorFilterActive)
            {
                RegistryHandler.ColorFilterKeySender(false);
                currentColorFilterActive = false;
            }
            base.DisableHook();
        }
        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (!currentColorFilterActive && newTheme == Theme.Dark)
            {
                return true;
            }
            else if (currentColorFilterActive && newTheme == Theme.Light)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            bool oldTheme = currentColorFilterActive;
            try
            {
                RegistryHandler.ColorFilterSetup();
                if (newTheme == Theme.Dark)
                {
                    RegistryHandler.ColorFilterKeySender(true);
                    currentColorFilterActive = true;
                }
                else
                {
                    RegistryHandler.ColorFilterKeySender(false);
                    currentColorFilterActive = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not enable color filter:");
            }
            Logger.Info($"update info - previous: {oldTheme}, now: {currentColorFilterActive}, enabled: {Settings.Enabled}");

        }
    }
}
