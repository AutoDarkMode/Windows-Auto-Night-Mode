using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    abstract class AutoDarkModeModule : IAutoDarkModeModule
    {
        public string Name { get; }
        public abstract string TimerAffinity { get; }
        public abstract void Fire();
        public int Priority { get; set; }
        public bool FireOnRegistration { get; }
        public AutoDarkModeModule(string name, bool fireOnRegistration)
        {
            Name = name;
            Priority = 0;
            FireOnRegistration = fireOnRegistration;
        }

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

        public int CompareTo(int other)
        {
            return Priority.CompareTo(other);
        }

        public int CompareTo(IAutoDarkModeModule other)
        {
            if (other is null)
            {
                return 0;
            }
            return other.Priority.CompareTo(Priority);
        }

        public virtual void Cleanup()
        {

        }
    }
}
