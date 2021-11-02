using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class ScriptSwitchSettings
    {
        public int TimeoutMillis { get; set; } = 10000;
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
        public List<string> ArgsLight { get; set; }
        public List<string> ArgsDark { get; set; }
        public override bool Equals(object obj)
        {
            if (obj is Script other)
            {
                if (other.Name != this.Name) return false;
                if (other.Command != this.Command) return false;
                if (!other.ArgsDark.SequenceEqual(this.ArgsDark)) return false;
                if (!other.ArgsLight.SequenceEqual(this.ArgsLight)) return false;
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
