using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Core
{
    public class PostponeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> PostponedQueue { get; } = new();
        private List<IAutoDarkModeModule> CallbackModules { get; } = new();

        public bool IsPostponed
        {
            get { return PostponedQueue.Count > 0; }
        }

        /// <summary>
        /// Adds a new blocking reason to the postpone queue
        /// </summary>
        /// <param name="reason">the name of the reason to be identified by</param>
        /// <returns>True if element is not present in postpone queue and has been added successfully</returns>
        public bool Add(string reason)
        {
            if (PostponedQueue.Contains(reason))
            {
                return false;
            }
            PostponedQueue.Add(reason);
            return true;
        }

        /// <summary>
        /// Removes an existing blocking reason from the postpone queue and invokes registered 
        /// callbacks if the queue is running empty
        /// </summary>
        /// <param name="reason"></param>
        /// <returns>True if removal was successful; false otherwise</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Remove(string reason)
        {
            bool lastElement = PostponedQueue.Count == 1;
            bool result = PostponedQueue.Remove(reason);
            if (!IsPostponed)
            {
                Logger.Info("no more elements postponing theme switch, running callbacks");
                CallbackModules.ForEach(m => m.Fire());
            }
            return result;
        }

        /// <summary>
        /// Registers a callback module with the postpone queueing system
        /// </summary>
        /// <param name="module"></param>
        /// <returns>true if adding the module was successful; false if the module already exists</returns>
        public bool RegisterCallbackModule(IAutoDarkModeModule module)
        {
            if (CallbackModules.Contains(module))
            {
                return false;
            }
            Logger.Debug($"Registering module for {module.Name} callback");
            CallbackModules.Add(module);
            return true;
        }

        /// <summary>
        /// Removes a callback module from the postpone queueing system
        /// </summary>
        /// <param name="module"></param>
        /// <returns>true if removing the module was successful; false if the module wasn't registered</returns>
        public bool DeregisterCallbackModule(IAutoDarkModeModule module)
        {
            if (CallbackModules.Remove(module))
            {
                Logger.Debug($"Deregistering module {module.Name} for callback ");
                return true;
            }
            return false;
        }
    }
}
