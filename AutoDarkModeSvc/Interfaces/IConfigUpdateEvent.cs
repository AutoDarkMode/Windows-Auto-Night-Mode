using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Interfaces
{
    /// <summary>
    /// Interface mostly meant to handle toggle or state switches for Auto Dark Mode elements that do not encompass Components. <br/>
    /// These are modules or other unique non-modularized structures that need state changes when the config file is updated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigUpdateEvent<T>
    {
        public void OnConfigUpdate(object oldConfig, T newConfig);
    }
}
