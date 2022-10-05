using AutoDarkModeConfig;
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
            Logger.Debug($"added {item} to postpone queue: [{string.Join(", ", PostponedQueue)}]");
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
        /// Toggles the skip next switch feature off or on
        /// </summary>
        /// <returns>True if it was turned on; false if it was turned off</returns>
        public bool ToggleSkipNextSwitch()
        {
            if (PostponedQueue.Any(x => x.Reason == "SkipNext"))
            {
                RemoveSkipNextSwitch();
                return false;
            }
            AddSkipNextSwitch();
            return true;
        }

        public bool SkipNextSwitchActive()
        {
            if (PostponedQueue.Any(x => x.Reason == "SkipNext"))
                return true;
            return false;
        }

        /// <summary>
        /// Adds a postpone item that skips the next planned timed theme switch only
        /// </summary>
        public void AddSkipNextSwitch()
        {
            ThemeState ts = new();
            // Delay by one second to create overlap. This avoids that the theme switches back too early
            DateTime NextSwitchAdjusted = ts.NextSwitchTime;
            if (DateTime.Compare(NextSwitchAdjusted, DateTime.Now) < 0)
            {
                NextSwitchAdjusted = new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    DateTime.Now.Day,
                    ts.NextSwitchTime.Hour,
                    ts.NextSwitchTime.Minute,
                    ts.NextSwitchTime.Second);
            }
            if (DateTime.Compare(NextSwitchAdjusted, DateTime.Now) < 0)
            {
                NextSwitchAdjusted = NextSwitchAdjusted.AddDays(1);
            }

            PostponeItem item = new("SkipNext", NextSwitchAdjusted.AddSeconds(1));
            try
            {
                item.StartExpiry();
                Add(item);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error(ex, "failed adding SkipNext postpone item to queue: ");
            }
        }

        /// <summary>
        /// If a next switch skip is active, this method should be called 
        /// to update the switch time to compensate for user or geolocator-adjusted suntimes. <br/>
        /// Calling this method when there was no change in expiry time, or if the skip is inactive will do nothing
        /// and is safe to call
        /// </summary>
        public void UpdateSkipNextSwitchExpiry()
        {
            PostponeItem item = PostponedQueue.Where(x => x.Reason == "SkipNext").FirstOrDefault();
            if (item != null)
            {
                ThemeState ts = new();
                DateTime NextSwitchAdjusted = ts.NextSwitchTime;
                if (item.Expiry.HasValue)
                {
                    if (DateTime.Compare(NextSwitchAdjusted, DateTime.Now) < 0)
                    { 
                        NextSwitchAdjusted = new DateTime(
                            DateTime.Now.Year, 
                            DateTime.Now.Month, 
                            DateTime.Now.Day, 
                            ts.NextSwitchTime.Hour, 
                            ts.NextSwitchTime.Minute, 
                            ts.NextSwitchTime.Second);
                    }
                    if (DateTime.Compare(NextSwitchAdjusted, DateTime.Now) < 0)
                    {
                        NextSwitchAdjusted = NextSwitchAdjusted.AddDays(1);
                    }
                    NextSwitchAdjusted = NextSwitchAdjusted.AddSeconds(1);
                    if (DateTime.Compare(NextSwitchAdjusted, item.Expiry.Value) != 0)
                    {
                        item.UpdateExpiryTime(NextSwitchAdjusted);
                    }
                }
            }
        }

        public void RemoveSkipNextSwitch()
        {
            Remove("SkipNext");
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
        public DateTime? Expiry { get; private set; }
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

        /// <summary>
        /// Creates a new timed postpone item
        /// </summary>
        /// <param name="reason">the name of the postpone item</param>
        /// <param name="expiry">the datetime when it should expire</param>
        public PostponeItem(string reason, DateTime expiry)
        {
            Reason = reason;
            Expiry = expiry;
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

        public void UpdateExpiryTime(DateTime newExpiry)
        {
            Logger.Info($"updating expiry time for item {Reason} from {(Expiry == null ? "none" : Expiry.Value.ToString("dd.MM.yyyy HH:mm:ss"))} to {newExpiry:dd.MM.yyyy HH:mm:ss}");
            CancelExpiry();
            Expiry = newExpiry;
            StartExpiry(suppressLaunchMessage: true);
        }

        /// <summary>
        /// Starts the expiry timer
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void StartExpiry(bool suppressLaunchMessage = false)
        {
            if (Expiry == null) return;
            DateTime expiryUnwrapped = Expiry.Value;
            if (!suppressLaunchMessage) Logger.Info($"postpone item with reason {Reason} will expire at {Expiry:dd.MM.yyyy HH:mm:ss}");
            if (DateTime.Compare(expiryUnwrapped, DateTime.Now) > 0)
            {
                TimeSpan delay = expiryUnwrapped - DateTime.Now;
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
                Logger.Debug($"my ({Reason}) expiry time is before the current time, attempting to remove myself");
                bool result = GlobalState.Instance().PostponeManager.Remove(Reason);
                if (!result)
                {
                    Logger.Debug($"I ({Reason}) couldn't remove myself, i wasn't home");
                }
                throw new ArgumentOutOfRangeException("Expiry", "expiry time can't be in the past");
            }
        }

        public override string ToString()
        {
            return Reason;
        }
    }
}
