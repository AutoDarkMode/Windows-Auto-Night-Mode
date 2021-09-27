using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Interfaces
{
    public interface IConfigUpdateEvent<T>
    {
        public void OnConfigUpdate(object oldConfig, T newConfig);
    }
}
