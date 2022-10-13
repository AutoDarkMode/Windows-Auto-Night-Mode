using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers;
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
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private List<PostponeItem> PostponeQueue { get; } = new();
        private List<IAutoDarkModeModule> CallbackModules { get; } = new();
        private GlobalState state;

        public PostponeManager(GlobalState state)
        {
            this.state = state;
        }

        public bool IsSkipNextSwitch
        {
            get
            {
                if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemPauseAutoSwitch))
                    return true;
                return false;
            }
        }

        public bool IsUserDelayed
        {
            get
            {
                if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemDelayAutoSwitch))
                    return true;
                return false;
            }
        }

        public bool IsPostponed
        {
            get { return PostponeQueue.Count > 0; }
        }

        /// <summary>
        /// Adds a new blocking reason to the postpone queue
        /// </summary>
        /// <param name="item">the postpone item (to be identified by its reason)</param>
        /// <returns>True if element is not present in postpone queue and has been added successfully</returns>
        public bool Add(PostponeItem item)
        {
            if (PostponeQueue.Any(x => x.Reason == item.Reason))
            {
                return false;
            }
            PostponeQueue.Add(item);
            try
            {
                item.StartExpiry();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error(ex, $"failed adding {item.Reason} postpone item to queue: ");
            }
            Logger.Debug($"added {item} to postpone queue: [{string.Join(", ", PostponeQueue)}]");
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
            bool lastElement = PostponeQueue.Count == 1;
            PostponeItem item = PostponeQueue.Where(x => x.Reason == reason).FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            if (item.Expires) item.CancelExpiry();
            bool result = PostponeQueue.Remove(item);
            if (result) Logger.Debug($"removed {reason} from postpone queue: [{string.Join(", ", PostponeQueue)}]");
            if (!IsPostponed && lastElement)
            {
                Logger.Info("postpone queue cleared");
                CallbackModules.ForEach(m => m.Fire());
            }
            return result;
        }

        /// <summary>
        /// Empties the postpone queue by correctly calling remove on all elements
        /// </summary>
        public void ClearQueue()
        {
            List<PostponeItem> toClear = PostponeQueue.Select(i =>
            {
                if (i.Expires) i.CancelExpiry();
                return i;
            }).ToList();
            toClear.ForEach(i => Remove(i.Reason));
        }

        public PostponeItem GetSkipNextSwitchItem()
        {
            return PostponeQueue.Where(x => x.Reason == Helper.PostponeItemPauseAutoSwitch).FirstOrDefault();
        }

        public PostponeItem Get(string reason)
        {
            return PostponeQueue.Where(x => x.Reason == reason).FirstOrDefault();
        }

        /// <summary>
        /// Toggles the skip next switch feature off or on
        /// </summary>
        /// <returns>True if it was turned on; false if it was turned off</returns>
        public bool ToggleSkipNextSwitch()
        {
            if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemPauseAutoSwitch || x.Reason == Helper.PostponeItemDelayAutoSwitch))
            {
                RemoveUserClearablePostpones();
                return false;
            }
            AddSkipNextSwitch();
            return true;
        }

        /// <summary>
        /// Calculates when the nextswitch postpone should expire, dynamically depending on the governor
        /// </summary>
        /// <param name="overrideTheme">Optional: If you would like to calculate the postpone expiry for a specific active theme, pass the theme here</param>
        /// <returns>The time the next switch should expire and the skiptype as a tuple, or an empty datetime object if that information is unavailable</returns>
        public (DateTime, SkipType) GetSkipNextSwitchExpiryTime(Theme overrideTheme = Theme.Unknown)
        {
            Theme newTheme = overrideTheme == Theme.Unknown ? state.RequestedTheme : overrideTheme;

            if (builder.Config.Governor != Governor.Default) return (new(), state.NightLight.Current == Theme.Light ? SkipType.Sunset : SkipType.Sunrise);

            TimedThemeState ts = new();
            DateTime nextSwitchAdjusted;
            SkipType skipType;

            // postpone for longer if current theme is correct while performed when postpone is engaged.
            if (ts.TargetTheme == newTheme)
            {
                if (ts.TargetTheme == Theme.Light)
                {
                    nextSwitchAdjusted = ts.AdjustedSunrise;
                    skipType = SkipType.Sunset;
                }
                else
                {
                    nextSwitchAdjusted = ts.AdjustedSunset;
                    skipType = SkipType.Sunrise;
                }
            }
            else
            {
                nextSwitchAdjusted = ts.NextSwitchTime;
                if (ts.TargetTheme == Theme.Light) skipType = SkipType.Sunrise;
                else skipType = SkipType.Sunset;
            }
            if (DateTime.Compare(nextSwitchAdjusted, DateTime.Now) < 0)
            {
                nextSwitchAdjusted = new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    DateTime.Now.Day,
                    nextSwitchAdjusted.Hour,
                    nextSwitchAdjusted.Minute,
                    nextSwitchAdjusted.Second);
            }
            if (DateTime.Compare(nextSwitchAdjusted, DateTime.Now) < 0)
            {
                // Delay by one second to create overlap. This avoids that the theme switches back too early
                nextSwitchAdjusted = nextSwitchAdjusted.AddDays(1);
            }
            return (nextSwitchAdjusted, skipType);
        }

        /// <summary>
        /// Adds a postpone item that skips the next planned timed theme switch only
        /// </summary>
        public void AddSkipNextSwitch()
        {
            (DateTime nextSwitchAdjusted, SkipType skipType) = GetSkipNextSwitchExpiryTime();
            if (builder.Config.Governor == Governor.Default)
            {
                PostponeItem item = new(Helper.PostponeItemPauseAutoSwitch, nextSwitchAdjusted.AddSeconds(1), skipType);
                Add(item);
            }
            else if (builder.Config.Governor == Governor.NightLight)
            {
                PostponeItem item = new(Helper.PostponeItemPauseAutoSwitch);
                item.SkipType = skipType;
                Add(item);
            }
        }

        /// <summary>
        /// If a next switch skip is active, this method should be called 
        /// to update the switch time to compensate for user or geolocator-adjusted suntimes. <br/>
        /// Calling this method when there was no change in expiry time, or if the skip is inactive will do nothing
        /// and is safe to call
        /// </summary>

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateSkipNextSwitchExpiry()
        {
            PostponeItem item = PostponeQueue.Where(x => x.Reason == Helper.PostponeItemPauseAutoSwitch).FirstOrDefault();
            if (item != null)
            {
                (DateTime nextSwitchAdjusted, SkipType skipType) = GetSkipNextSwitchExpiryTime();
                if (builder.Config.Governor == Governor.Default)
                {
                    if (item.Expiry.HasValue)
                    {
                        nextSwitchAdjusted = nextSwitchAdjusted.AddSeconds(1);
                        if (DateTime.Compare(nextSwitchAdjusted, item.Expiry.Value) != 0)
                        {
                            item.UpdateExpiryTime(nextSwitchAdjusted, skipType);
                        }
                    }
                    else
                    {
                        item.UpdateExpiryTime(nextSwitchAdjusted.AddSeconds(1), skipType);
                    }
                }
                else if (builder.Config.Governor == Governor.NightLight && item.Expires)
                {
                    item.CancelExpiry();
                    item.SkipType = skipType;
                }
            }
        }

        public void RemoveUserClearablePostpones()
        {
            List<PostponeItem> toClear = PostponeQueue.Select(i =>
            {
                if (i.IsUserClearable)
                {
                    if (i.Expires) i.CancelExpiry();
                    return i;
                }
                return null;
            }).Where(i => i != null).ToList();
            toClear.ForEach(i => Remove(i.Reason));
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

        public PostponeQueueDto MakeDto()
        {
            List<PostponeItemDto> itemDtos = new();
            PostponeQueue.ForEach(i =>
            {
                itemDtos.Add(new(i.Reason, expiry: i.Expiry, i.Expires, i.SkipType));
            });
            return new(itemDtos);
        }
    }

  
public class PostponeItem
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string Reason { get; }
        public bool IsUserClearable { get; }
        public DateTime? Expiry { get; private set; }
        private Task Task { get; set; }
        CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();
        public SkipType SkipType { get; set; } = SkipType.Unspecified;
        public bool Expires
        {
            get
            {
                return Expiry != null && Task != null && !Task.IsCompleted;
            }
        }

        public PostponeItem(string reason, bool isUserClearable = true)
        {
            Reason = reason;
            IsUserClearable = isUserClearable;
        }

        /// <summary>
        /// Creates a new timed postpone item
        /// </summary>
        /// <param name="reason">the name of the postpone item</param>
        /// <param name="expiry">the datetime when it should expire</param>
        public PostponeItem(string reason, DateTime expiry, SkipType skipType, bool isUserClearable = true)
        {
            Reason = reason;
            Expiry = expiry;
            SkipType = skipType;
            IsUserClearable = isUserClearable;
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
                Task.Wait(1000);
                Expiry = null;
                Task = null;
                return true;
            }
            return false;
        }

        public void UpdateExpiryTime(DateTime newExpiry, SkipType skipType = SkipType.Unspecified)
        {
            Logger.Info($"updating expiry time for item {Reason} from {(Expiry == null ? "none" : Expiry.Value.ToString("dd.MM.yyyy HH:mm:ss"))} to {newExpiry:dd.MM.yyyy HH:mm:ss}");
            CancelExpiry();
            Expiry = newExpiry;
            SkipType = skipType;
            CancelTokenSource = new();
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
                        Logger.Info($"postpone item with reason {Reason} had its expiry at {Expiry:dd.MM.yyyy HH:mm:ss} cancelled");
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
                    Logger.Debug($"i ({Reason}) couldn't remove myself, i wasn't home");
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
