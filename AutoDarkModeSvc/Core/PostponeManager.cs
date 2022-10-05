using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AutoDarkModeSvc.Core
{
    public class PostponeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<PostponeItem> PostponedQueue { get; } = new();
        private List<IAutoDarkModeModule> CallbackModules { get; } = new();

        public bool IsPostponed
        {
            get { return PostponedQueue.Count > 0; }
        }

        /// <summary>
        /// Adds a new blocking reason to the postpone queue
        /// </summary>
        /// <param name="item">the postpone item (to be identified by its reason)</param>
        /// <returns>True if element is not present in postpone queue and has been added successfully</returns>
        public bool Add(PostponeItem item)
        {
            if (PostponedQueue.Any(x => x.Reason == item.Reason))
            {
                return false;
            }
            PostponedQueue.Add(item);
            Logger.Debug($"added {item.Reason} to postpone queue: [{string.Join(", ", PostponedQueue)}]");
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
            PostponeItem item = PostponedQueue.Where(x => x.Reason == reason).FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            if (item.Expires) item.CancelExpiry();
            bool result = PostponedQueue.Remove(item);
            if (result) Logger.Debug($"removed {reason} from postpone queue: [{string.Join(", ", PostponedQueue)}]");
            if (!IsPostponed && lastElement)
            {
                Logger.Info("postpone queue cleared");
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
            Logger.Debug($"registering module {module.Name} for callback");
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
                Logger.Debug($"deregistering module {module.Name} for callback ");
                return true;
            }
            return false;
        }
    }

    public class PostponeItem
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string Reason { get; }
        private DateTime? Expiry { get; set; }
        private Task Task { get; set; }
        CancellationTokenSource CancelTokenSource { get; } = new CancellationTokenSource();
        public bool Expires
        {
            get
            {
                return Expiry != null;
            }
        }

        public PostponeItem(string reason)
        {
            Reason = reason;
        }

        public PostponeItem(string reason, DateTime expiry)
        {
            Reason = reason;
            Expiry = expiry;
            HandleExpiry();
        }

        /// <summary>
        /// Causes the Postpone item to immediately expire
        /// </summary>
        /// <returns>true if a cancellation was performed; false if there was no outstanding expiry</returns>
        public bool CancelExpiry()
        {
            if (Task != null)
            {
                CancelTokenSource.Cancel();
                Expiry = null;
                return true;
            }
            return false;
        }

        private void HandleExpiry()
        {
            if (Expiry == null) return;
            DateTime expiresUnwrapped = Expiry.Value;
            if (Expiry > DateTime.Now)
            {
                Logger.Info($"postpone item with reason {Reason} will expire at {expiresUnwrapped:MM.dd.yyyy HH:mm:ss}");
                TimeSpan delay = expiresUnwrapped - DateTime.Now;
                CancellationToken token = CancelTokenSource.Token;
                Task = Task.Delay(delay, token).ContinueWith(o =>
                {
                    if (token.IsCancellationRequested)
                    {
                        Logger.Info($"postpone item with reason {Reason} had its expiry cancelled");
                    }
                    else
                    {
                        PostponeManager pm = GlobalState.Instance().PostponeManager;
                        pm.Remove(Reason);
                        Logger.Info($"postpone item with reason {Reason} expired and was removed");
                    }
                });
            }
            else
            {
                Logger.Debug("expiry time before current time, removing myself");
                GlobalState.Instance().PostponeManager.Remove(Reason);
            }
        }
    }
}
