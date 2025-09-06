using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        if (e.Theme == Theme.Dark)
        {
            if (Settings.Component.TaskbarColorDuring == Theme.Dark && !currentTaskbarColorActive)
            {
                return true;
            }
            else if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Light && currentTaskbarColorActive)
            {
                return true;
            }
        }
        else if (e.Theme == Theme.Light)
        {
            if (Settings.Component.TaskbarColorDuring == Theme.Light && !currentTaskbarColorActive)
            {
                return true;
            }
            else if (Settings.Component.TaskbarColorDuring == Theme.Dark && currentTaskbarColorActive)
            {
                return true;
            }
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        if (e.Theme == Theme.Dark)
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
        else if (e.Theme == Theme.Light)
        {
            if (Settings.Component.TaskbarColorDuring == Theme.Light)
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
    }

    protected override void DisableHook()
    {
        if (RegistryHandler.SystemUsesLightTheme() && currentTaskbarColorActive)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
            DwmRefreshHandler.Enqueue(DwmRefreshSource.TaskbarColorSwitchComponent);
        }
    }
}
