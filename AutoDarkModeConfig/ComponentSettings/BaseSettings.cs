using AutoDarkModeConfig.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.ComponentSettings
{
    public class BaseSettings<T> : ISwitchComponentSettings<T>
    {
        public int PriorityToLight { get; set; }
        public int PriorityToDark { get; set; }
        public virtual bool Enabled { get; set; }
        public T Component { get; set; }
        public BaseSettings()
        {
            Component = (T)Activator.CreateInstance(typeof(T));
        }
    }
}
