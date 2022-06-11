using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Core
{
    public class PostponeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> PostponedQueue { get; } = new();
        private List<IAutoDarkModeModule> CallbackModules { get; } = new();

        public bool _IsPostponed;
        public bool IsPostponed
        {
            get { return PostponedQueue.Count > 0; }
        }

        public bool Add(string reason)
        {
            if (PostponedQueue.Contains(reason))
            {
                return false;
            }
            PostponedQueue.Add(reason);
            return true;
        }

        public bool Remove(string reason)
        {
            bool empty = PostponedQueue.Count > 0;
            bool result = PostponedQueue.Remove(reason);
            if (!empty && !IsPostponed)
            {
                Logger.Info("no more elements postponing theme switch, run callbacks");
                CallbackModules.ForEach(m => m.Fire());
            }
            return result;
        }
    }
}
