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
namespace AutoDarkModeSvc.Interfaces;

/// <summary>
/// Interface mostly meant to handle toggle or state switches for Auto Dark Mode elements that do not encompass Components. <br/>
/// These are modules or other unique non-modularized structures that need state changes when the config file is updated.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IConfigUpdateEvent<T>
{
    public void OnConfigUpdate(object oldConfig, T newConfig);
}
