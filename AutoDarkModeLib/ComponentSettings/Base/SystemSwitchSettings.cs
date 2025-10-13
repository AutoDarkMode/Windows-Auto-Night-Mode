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
namespace AutoDarkModeLib.ComponentSettings.Base;

public class SystemSwitchSettings
{
    public Mode Mode { get; set; }
    public int TaskbarSwitchDelay { get; set; } = 1200;
    public bool TaskbarColorSwitch { get; set; }
    public Theme TaskbarColorDuring { get; set; } = Theme.Light;
    public bool DWMPrevalenceSwitch { get; set; }
    public Theme DWMPrevalenceEnableTheme { get; set; } = Theme.Light;
}
