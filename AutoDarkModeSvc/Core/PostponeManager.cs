#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
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

        public int Count
        {
            get { return PostponeQueue.Count; }
        }

        public int CountUserClearable
        {
            get { return PostponeQueue.Count(x => x.IsUserClearable); }
        }

        /// <summary>
        /// Checks if a full theme switch skip is currently queued
        /// </summary>
        /// <returns>True if a skip is queued; false otherwise</returns>
        public bool IsSkipNextSwitch
        {
            get
            {
                if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemPauseAutoSwitch))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Checks if a timed user delay is currently queued
        /// </summary>
        /// <returns>True if a delay is queued; false otherwise</returns>
        public bool IsUserDelayed
        {
            get
            {
                if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemDelayAutoSwitch))
                    return true;
                return false;
            }
        }
        /// <summary>
        /// Checks if a grace period for theme switching is currently active
        /// </summary>
        /// <returns>True if a grace period item is queued; false otherwise</returns>
        public bool IsGracePeriod
        {
            get
            {
                if (PostponeQueue.Any(x => x.Reason == Helper.PostponeItemDelayGracePeriod))
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
            state.UpdateNotifyIcon(builder);
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
            state.UpdateNotifyIcon(builder);
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
                RemoveSkipNextSwitch();
                return false;
            }
            AddSkipNextSwitch();
            return true;
        }

        /// <summary>
        /// Calculates when the nextswitch postpone should expire when the time module is used
        /// </summary>
        /// <param name="overrideTheme">Optional: If you would like to calculate the postpone expiry for a specific active theme, pass the theme here</param>
        /// <returns>The time the next switch should expire and the skiptype as a tuple, or an empty datetime object if that information is unavailable</returns>
        public (DateTime, SkipType) GetSkipNextSwitchExpiryTime(Theme overrideTheme = Theme.Unknown)
        {
            Theme newTheme = overrideTheme == Theme.Unknown ? state.InternalTheme : overrideTheme;

            if (builder.Config.Governor != Governor.Default) return (new(), state.NightLight.Requested == Theme.Light ? SkipType.UntilSunrise : SkipType.UntilSunset);

            TimedThemeState ts = new();
            DateTime nextSwitchAdjusted;
            SkipType skipType;

            // postpone for longer if current theme is correct while performed when postpone is engaged.
            if (ts.TargetTheme == newTheme)
            {
                if (ts.TargetTheme == Theme.Light)
                {
                    nextSwitchAdjusted = ts.AdjustedSunrise;
                    skipType = SkipType.UntilSunrise;
                }
                else
                {
                    nextSwitchAdjusted = ts.AdjustedSunset;
                    skipType = SkipType.UntilSunset;
                }
            }
            else
            {
                nextSwitchAdjusted = ts.NextSwitchTime;
                if (ts.TargetTheme == Theme.Light) skipType = SkipType.UntilSunset;
                else skipType = SkipType.UntilSunrise;
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

        public void RemoveSkipNextSwitch()
        {
            PostponeItem item = PostponeQueue.Where(x => x.Reason == Helper.PostponeItemPauseAutoSwitch).FirstOrDefault();
            if (item != null)
            {
                if (item.Expires) item.CancelExpiry();
                Remove(item.Reason);
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SyncExpiryTimesWithSystemClock()
        {
            List<PostponeItem> items = PostponeQueue.Where(x =>
            {
                if (x.Reason == Helper.PostponeItemPauseAutoSwitch) return true;
                else if (x.Reason == Helper.PostponeItemDelayAutoSwitch) return true;
                return false;
            }).ToList();

            items.ForEach(i => { if (i.Expires) i.SyncExpiryWithSystemClock(); });
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

        public PostponeQueueDto MakeQueueDto()
        {
            List<PostponeItemDto> itemDtos = new();
            PostponeQueue.ForEach(i =>
            {
                itemDtos.Add(new(i.Reason, expiry: i.Expiry, i.Expires, i.SkipType, i.IsUserClearable));
            });
            return new(itemDtos);
        }

        public void GetPostonesFromDisk()
        {
            try
            {
                builder.LoadPostponeData();
                if (builder.PostponeData.InternalThemeAtExit != Theme.Unknown)
                    state.InternalTheme = builder.PostponeData.InternalThemeAtExit;
                if (builder.PostponeData.Queue.Items.Count > 0)
                {
                    Logger.Info("restoring postpone queue from disk");
                }
                List<PostponeItem> items = builder.PostponeData.Queue.Items.Where(i =>
                {
                    if (i.Expires)
                    {
                        return true;
                    }
                    else if (i.SkipType != SkipType.Unspecified)
                    {
                        // only keep previous night light postpone if the skiptype and state is reversed
                        // This way, switches will properly get delayed.
                        bool nightLightEnabled = RegistryHandler.IsNightLightEnabled();
                        // if last modified is older than 24 hours, don't keep it because then the user has spent more than one day away from their computer
                        // and the postpone is very likely not relevant anymore
                        if (builder.PostponeData.LastModified.CompareTo(DateTime.Now.AddDays(-1)) < 1)
                        {
                            return false;
                        }
                        // if night light has been toggled in the absence of the user (for example due to a system shutdown remove the postpone
                        else if (i.SkipType == SkipType.UntilSunset && !nightLightEnabled && builder.PostponeData.InternalThemeAtExit == Theme.Light)
                        {
                            return false;
                        }
                        else if (i.SkipType == SkipType.UntilSunrise && nightLightEnabled && builder.PostponeData.InternalThemeAtExit == Theme.Dark)
                        {
                            return false;
                        }
                        // all other cases should preserve the postpone
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }).Select(i => new PostponeItem(i)).ToList();
                items.ForEach(i => { if (i.Expiry > DateTime.Now.AddSeconds(5)) Add(i); });
                builder.PostponeData.Queue = new();
                builder.PostponeData.InternalThemeAtExit = Theme.Unknown;
                builder.SavePostponeData();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error loading postpone data");
            }
        }

        public void FlushPostponesToDisk()
        {
            try
            {
                if (state.PostponeManager.CountUserClearable > 0)
                {
                    builder.PostponeData.InternalThemeAtExit = state.InternalTheme;
                    PostponeQueueDto dto = state.PostponeManager.MakeQueueDto();
                    dto.Items = dto.Items.Where(i => i.IsUserClearable).ToList();
                    dto.Items = dto.Items.Where(i => i.Reason == Helper.PostponeItemDelayAutoSwitch || i.Reason == Helper.PostponeItemPauseAutoSwitch).ToList();
                    builder.PostponeData.Queue = dto;
                    Logger.Info($"postpone items preserved for next start: [{string.Join(", ", builder.PostponeData.Queue.Items.Select(i => i.Reason))}]");
                    builder.SavePostponeData();
                }
                else if (builder.PostponeData.InternalThemeAtExit != Theme.Unknown)
                {
                    builder.PostponeData.InternalThemeAtExit = Theme.Unknown;
                    builder.SavePostponeData();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not save postpone data");
            }
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", PostponeQueue)}]";
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

        public PostponeItem(PostponeItemDto dto)
        {
            //transform dto to postpone item
            Reason = dto.Reason;
            Expiry = dto.Expiry;
            SkipType = dto.SkipType;
            IsUserClearable = dto.IsUserClearable;
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
        public PostponeItem(string reason, DateTime? expiry, SkipType skipType, bool isUserClearable = true)
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

        public void SyncExpiryWithSystemClock()
        {
            if (Expiry == null) return;
            DateTime expiryUnwrapped = Expiry.Value;
            // if the expiry time is in the past we need to cancel it
            if (DateTime.Compare(expiryUnwrapped, DateTime.Now) <= 0)
            {
                CancelExpiry();
                PostponeManager pm = GlobalState.Instance().PostponeManager;
                pm.Remove(Reason);
            }
            else
            {
                UpdateExpiryTime(expiryUnwrapped);
            }
        }

        /// <summary>
        /// Starts the expiry timer.
        /// If no expiry is set, this method does nothing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void StartExpiry(bool suppressLaunchMessage = false)
        {
            if (Expiry == null) return;
            DateTime expiryUnwrapped = Expiry.Value;
            if (!suppressLaunchMessage) Logger.Info($"{Reason} will expire at {Expiry:dd.MM.yyyy HH:mm:ss}");
            if (DateTime.Compare(expiryUnwrapped, DateTime.Now) > 0)
            {
                TimeSpan delay = expiryUnwrapped - DateTime.Now;
                CancellationToken token = CancelTokenSource.Token;
                Task = Task.Delay(delay, token).ContinueWith(o =>
                {
                    if (token.IsCancellationRequested)
                    {
                        Logger.Info($"{Reason} had its expiry at {Expiry:dd.MM.yyyy HH:mm:ss} cancelled");
                    }
                    else
                    {
                        PostponeManager pm = GlobalState.Instance().PostponeManager;
                        pm.Remove(Reason);
                        Logger.Info($"{Reason} expired and was removed");
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
