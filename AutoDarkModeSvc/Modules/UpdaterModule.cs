using AutoDarkModeConfig;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class UpdaterModule : AutoDarkModeModule
    {
        AdmConfigBuilder builder;
        public UpdaterModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            builder = AdmConfigBuilder.Instance();
        }

        public override string TimerAffinity => TimerName.IO;

        public override void Fire()
        {
            throw new NotImplementedException();
        }
    }
}
