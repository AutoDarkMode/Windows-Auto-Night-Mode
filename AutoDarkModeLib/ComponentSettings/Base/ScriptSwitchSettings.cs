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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeLib.ComponentSettings.Base
{
    public class ScriptSwitchSettings
    {
        public List<Script> Scripts { get; set; }
        public ScriptSwitchSettings()
        {
            Scripts = new();
            Scripts.Add(new()
            {
                Name = "cmd example",
                Command = "cmd",
                WorkingDirectory = AdmConfigBuilder.ConfigDir,
                ArgsLight = new() { "/c", "echo I am a light command" },
                ArgsDark = new() { "/c", "echo I am a dark command" },
            });
        }
        public override bool Equals(object obj)
        {
            if (obj is ScriptSwitchSettings other)
            {
                return Scripts.SequenceEqual(other.Scripts);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Script
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string WorkingDirectory { get; set; }
        public List<string> ArgsLight { get; set; } = new();
        public List<string> ArgsDark { get; set; } = new();
        public List<SwitchSource> AllowedSources { get; set; } = new() { SwitchSource.Any };
        public int? TimeoutMillis { get; set; }
        public override bool Equals(object obj)
        {
            if (obj is Script other)
            {
                if (other.Name != Name) return false;
                if (other.Command != Command) return false;
                if (!other.ArgsDark.SequenceEqual(ArgsDark)) return false;
                if (!other.ArgsLight.SequenceEqual(ArgsLight)) return false;
                if (other.WorkingDirectory != WorkingDirectory) return false;
                if (!other.AllowedSources.SequenceEqual(AllowedSources)) return false;
                //don't think a timeoutmillis comparison is necessary for equality, may be subject to change
                return true;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Command, ArgsLight, ArgsDark);
        }
    }
}
