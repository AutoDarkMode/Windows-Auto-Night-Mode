using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    abstract class AutoDarkModeModule : IAutoDarkModeModule
    {
        public string Name { get; protected set; }
        public abstract string TimerAffinity { get; }
        public abstract void Poll();
        public bool Equals(IAutoDarkModeModule other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            if (other.Name == Name)
            {
                return true;
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as IAutoDarkModeModule);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
