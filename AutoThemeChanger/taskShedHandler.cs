using System;
using Microsoft.Win32.TaskScheduler;

namespace AutoThemeChanger
{
    public class TaskShedHandler
    {
        readonly string dark = "Auto Dark Mode DARK";
        readonly string light = "Auto Dark Mode LIGHT";
        readonly string hibernation = "Auto Dark Mode HIBERNATION";
        readonly string updater = "Auto Dark Mode UPDATER";
        readonly string appupdater = "Auto Dark Mode APPUPDATER";
        readonly string connected = "Auto Dark Mode CONNECTED STANDBY";
        readonly string folder = "Auto Dark Mode";
        readonly string author = "Armin Osaj";
        readonly string program = "Windows Auto Dark Mode";
        readonly string description = "Task of the program Windows Auto Dark Mode.";

        public void CreateTask(int startTime, int startTimeMinutes, int endTime, int endTimeMinutes)
        {
            using (TaskService taskService = new TaskService())
            {
                taskService.RootFolder.CreateFolder(folder, null, false);

                //create task for DARK
                TaskDefinition tdDark = taskService.NewTask();

                tdDark.RegistrationInfo.Description = "Automatically switches to the Windows dark theme. " + description;
                tdDark.RegistrationInfo.Author = author;
                tdDark.RegistrationInfo.Source = program;
                tdDark.Settings.DisallowStartIfOnBatteries = false;
                tdDark.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(5);
                tdDark.Settings.StartWhenAvailable = true;

                tdDark.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(startTime).AddMinutes(startTimeMinutes) });
                tdDark.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));

                taskService.GetFolder(folder).RegisterTaskDefinition(dark, tdDark);
                Console.WriteLine("created task for dark theme");

                //create task for LIGHT
                TaskDefinition tdLight = taskService.NewTask();

                tdLight.RegistrationInfo.Description = "Automatically switches to the Windows light theme. " + description;
                tdLight.RegistrationInfo.Author = author;
                tdLight.RegistrationInfo.Source = program;
                tdLight.Settings.DisallowStartIfOnBatteries = false;
                tdLight.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(5);
                tdLight.Settings.StartWhenAvailable = true;

                tdLight.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(endTime).AddMinutes(endTimeMinutes) });
                tdLight.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));

                taskService.GetFolder(folder).RegisterTaskDefinition(light, tdLight);
                Console.WriteLine("created task for light theme");

                //create EventLog task
                TaskDefinition tdHibernation = taskService.NewTask();

                tdHibernation.RegistrationInfo.Description = "Improves reliability of the theme switch. " + description;
                tdHibernation.RegistrationInfo.Author = author;
                tdHibernation.RegistrationInfo.Source = program;
                tdHibernation.Settings.DisallowStartIfOnBatteries = false;
                tdHibernation.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(5);
                tdHibernation.Settings.StartWhenAvailable = true;

                EventTrigger eventTrigger = tdHibernation.Triggers.Add(new EventTrigger());
                eventTrigger.Subscription = @"<QueryList><Query Id='0' Path='System'><Select Path='System'>*[System[Provider[@Name='Microsoft-Windows-Power-Troubleshooter'] and (Level=4 or Level=0) and (EventID=1)]]</Select></Query></QueryList>";
                tdHibernation.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));
                taskService.GetFolder(folder).RegisterTaskDefinition(hibernation, tdHibernation);
                Console.WriteLine("created task for hibernation");
            }
        }

        public void CreateLocationTask()
        {
            using(TaskService taskService = new TaskService())
            {
                TaskDefinition tdLocation = taskService.NewTask();

                tdLocation.RegistrationInfo.Description = "Updates the sunset and sunrise dates with the user location. " + description;
                tdLocation.RegistrationInfo.Author = author;
                tdLocation.RegistrationInfo.Source = program;
                tdLocation.Settings.DisallowStartIfOnBatteries = false;
                tdLocation.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(10);
                tdLocation.Settings.StartWhenAvailable = true;

                tdLocation.Triggers.Add(new WeeklyTrigger { StartBoundary = DateTime.Today.AddDays(7) });
                tdLocation.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/location"));

                taskService.GetFolder(folder).RegisterTaskDefinition(updater, tdLocation);
                Console.WriteLine("created task for location time updates");
            }
        }

        public void CreateAppUpdaterTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskDefinition tdUpdate = taskService.NewTask();

                tdUpdate.RegistrationInfo.Description = "Checks the GitHub-Server for new app version in the background. " + description;
                tdUpdate.RegistrationInfo.Author = author;
                tdUpdate.RegistrationInfo.Source = program;
                tdUpdate.Settings.DisallowStartIfOnBatteries = false;
                tdUpdate.Settings.StartWhenAvailable = true;

                tdUpdate.Triggers.Add(new MonthlyTrigger { StartBoundary = DateTime.Today.AddMonths(1) });
                tdUpdate.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/update"));

                taskService.GetFolder(folder).RegisterTaskDefinition(appupdater, tdUpdate);
                Console.WriteLine("created task for app updates");
            }
        }

        public void CreateConnectedStandbyTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskDefinition tdConnected = taskService.NewTask();

                tdConnected.RegistrationInfo.Description = "Improves reliability of the theme switch for devices that support connected standby. " + description;
                tdConnected.RegistrationInfo.Author = author;
                tdConnected.RegistrationInfo.Source = program;
                tdConnected.Settings.DisallowStartIfOnBatteries = false;
                tdConnected.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(5);
                tdConnected.Settings.StartWhenAvailable = true;

                EventTrigger eventTrigger = tdConnected.Triggers.Add(new EventTrigger());
                eventTrigger.Subscription = @"<QueryList><Query Id='0' Path='System'><Select Path='System'>*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and (Level=4 or Level=0) and (EventID=507)]]</Select></Query></QueryList>";
                tdConnected.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));
                taskService.GetFolder(folder).RegisterTaskDefinition(connected, tdConnected);
                Console.WriteLine("created task for connected standby");
            }
        }

        public void RemoveTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskFolder taskFolder = taskService.GetFolder(folder);
                try
                {
                    taskFolder.DeleteTask(light, false);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(dark, false);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(hibernation, false);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(updater, false);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(appupdater, false);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(connected, false);
                }
                catch
                {

                }
                try
                {
                    taskService.RootFolder.DeleteFolder(folder, false);
                }
                catch
                {

                }
            }
        }

        public void RemoveLocationTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskFolder taskFolder = taskService.GetFolder(folder);
                try
                {
                    taskFolder.DeleteTask(updater, false);
                }
                catch
                {

                }
            }
        }

        public void RemoveAppUpdaterTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskFolder taskFolder = taskService.GetFolder(folder);
                try
                {
                    taskFolder.DeleteTask(appupdater, false);
                }
                catch
                {

                }
            }
        }

        public void RemoveConnectedStandbyTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskFolder taskFolder = taskService.GetFolder(folder);
                try
                {
                    taskFolder.DeleteTask(connected, false);
                }
                catch
                {

                }
            }
        }

        public int CheckExistingClass()
        {
            using (TaskService taskService = new TaskService())
            {
                try
                {
                    var task3 = taskService.FindTask(updater).ToString();
                    return 2;
                }
                catch
                {
                    
                }
                try
                {
                    var task1 = taskService.FindTask(dark).ToString();
                    var task2 = taskService.FindTask(light).ToString();
                    return 1;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int[] GetRunTime(string theme)
        {
            using (TaskService taskService = new TaskService())
            {
                int[] runTime = new int[2];

                if(theme == "dark")
                {
                    runTime[0] = GetRunHour(taskService.FindTask(dark));
                    runTime[1] = GetRunMinute(taskService.FindTask(dark));
                }
                else{
                    runTime[0] = GetRunHour(taskService.FindTask(light));
                    runTime[1] = GetRunMinute(taskService.FindTask(light));
                }
                return runTime;
            }
        }

        private int GetRunHour(Task task)
        {
            DateTime time = task.NextRunTime;
            return time.Hour;
        }

        private int GetRunMinute(Task task)
        {
            DateTime time = task.NextRunTime;
            return time.Minute;
        }
    }
}
