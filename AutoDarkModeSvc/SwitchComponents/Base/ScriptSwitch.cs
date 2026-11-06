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
using System;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class ScriptSwitch : BaseComponent<ScriptSwitchSettings>
{
    public override int PriorityToDark => 30;
    public override int PriorityToLight => 30;
    private Theme currentComponentTheme = Theme.Unknown;
    public override bool ThemeHandlerCompatibility { get; } = true;
    private Task switchTask;

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e) => currentComponentTheme != e.Theme;

    protected async override void HandleSwitch(SwitchEventArgs e)
    {
        string oldTheme = Enum.GetName(currentComponentTheme);
        switchTask = Task.Run(() =>
        {
            switch (e.Theme)
            {
                case Theme.Light:
                {
                    currentComponentTheme = Theme.Light;
                    Settings.Component.Scripts.ForEach(s =>
                    {
                        if (s.AllowedSources.Contains(SwitchSource.Any) || s.AllowedSources.Contains(e.Source))
                            ScriptHandler.Launch(s.Name, s.Command, s.ArgsLight, s.TimeoutMillis, s.WorkingDirectory);
                    });
                    break;
                }

                default:
                {
                    currentComponentTheme = Theme.Dark;
                    Settings.Component.Scripts.ForEach(s =>
                    {
                        if (s.AllowedSources.Contains(SwitchSource.Any) || s.AllowedSources.Contains(e.Source))
                            ScriptHandler.Launch(s.Name, s.Command, s.ArgsDark, s.TimeoutMillis, s.WorkingDirectory);
                    });
                    break;
                }
            }
        });
        await switchTask;
        Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(currentComponentTheme)}");
    }

    protected override void EnableHook()
    {
        currentComponentTheme = Theme.Unknown;
    }

    protected override void DisableHook()
    {
        currentComponentTheme = Theme.Unknown;
    }

    protected override void UpdateSettingsState()
    {
        if (!Settings.Component.Equals(SettingsBefore.Component))
        {
            currentComponentTheme = Theme.Unknown;
        }
    }

    /*
    protected override async void Callback()
    {
        if (switchTask != null)
        {
            await switchTask;
        }
        Logger.Debug("test callback wait");
    }
    */
}
