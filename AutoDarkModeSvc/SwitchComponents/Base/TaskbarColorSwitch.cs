using System;
using System.Threading;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using NLog;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class TaskbarColorSwitch : BaseComponent<SystemSwitchSettings>
{
    public override bool ThemeHandlerCompatibility => true;
    public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
    public override bool Enabled => Settings.Component.TaskbarColorSwitch;
    public override int DwmRefreshDelay => 1000;
    public bool useCallbackForDark = Environment.OSVersion.Version.Build < (int)WindowsBuilds.Win11_RC;
    private readonly bool lightTaskbarAccentPermitted = (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_24H2) && false; // always false

    private bool currentTaskbarColorActive;

    public TaskbarColorSwitch() : base() { }

    protected override void EnableHook()
    {
        try
        {
            currentTaskbarColorActive = RegistryHandler.IsTaskbarColor();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "couldn't retrieve DWM prevalence state: ");
        }
    }

    private bool CanApplyLightAccentColor()
    {
        return lightTaskbarAccentPermitted || (Settings.Component.Mode == Mode.DarkOnly);
    }

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        // allow the application of the accent color during light theme if
        // we permit it for the specific windows build or
        // mode is set to dark only

        if (e.Theme == Theme.Dark)
        {
            switch (Settings.Component.TaskbarColorDuring)
            {
                case Theme.Dark when !currentTaskbarColorActive:
                    return true;
                case Theme.Light when currentTaskbarColorActive:
                    return true;
            }
        }
        else if (e.Theme == Theme.Light)
        {
            // change to !currentTaskbarColorActive here in the future
            switch (Settings.Component.TaskbarColorDuring)
            {
                case Theme.Dark when currentTaskbarColorActive:
                    return true;
                case Theme.Light when currentTaskbarColorActive != CanApplyLightAccentColor():
                    return true;
            }
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        switch (e.Theme)
        {
            case Theme.Dark when !useCallbackForDark:
                SwitchDark();
                break;
            case Theme.Light:
                if (Settings.Component.TaskbarColorDuring == Theme.Light && CanApplyLightAccentColor())
                {
                    RegistryHandler.SetTaskbarColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else
                {
                    RegistryHandler.SetTaskbarColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
                break;
        }
    }

    protected override void Callback(SwitchEventArgs e)
    {
        if (e.Theme == Theme.Dark && useCallbackForDark)
        {
            Thread.Sleep(Settings.Component.TaskbarSwitchDelay);
            SwitchDark();
        }
    }

    protected void SwitchDark()
    {
        if (Settings.Component.TaskbarColorDuring == Theme.Dark)
        {
            RegistryHandler.SetTaskbarColorPrevalence(1);
            currentTaskbarColorActive = true;
        }
        else
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
            currentTaskbarColorActive = false;
        }
    }

    protected override void DisableHook()
    {
        if (RegistryHandler.SystemUsesLightTheme() && currentTaskbarColorActive)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
            DwmRefreshHandler.Enqueue(new(DwmRefreshSource.TaskbarColorSwitchComponent, DwmRefreshDelay));
        }
        else
        {
            // default this for now
            RegistryHandler.SetTaskbarColorPrevalence(0);
            DwmRefreshHandler.Enqueue(new(DwmRefreshSource.TaskbarColorSwitchComponent, DwmRefreshDelay));
        }

        if (Settings.Component.Mode == Mode.DarkOnly && Settings.Component.TaskbarColorDuring == Theme.Dark)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
            DwmRefreshHandler.Enqueue(new(DwmRefreshSource.TaskbarColorSwitchComponent, DwmRefreshDelay));
        }
    }
}
