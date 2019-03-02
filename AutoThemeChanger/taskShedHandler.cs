using System;
using Microsoft.Win32.TaskScheduler;

namespace AutoThemeChanger
{
    public class TaskShedHandler
    {
        readonly string dark = "Auto-Night Mode Dark";
        readonly string light = "Auto-Night Mode Light";
        readonly string folder = "Auto-Night Mode";
        readonly string updater = "Auto-Night Mode Updater";
        readonly string author = "Armin Osaj";
        readonly string program = "Windows Auto Night-Mode";
        readonly string descrpition = "Task of the program Windows Auto-Night Mode.";

        public void CreateTask(int startTime, int startTimeMinutes, int endTime, int endTimeMinutes)
        {
            using (TaskService taskService = new TaskService())
            {
                try
                {
                    taskService.RootFolder.CreateFolder(folder);
                }catch{}

                //create task for DARK
                TaskDefinition tdDark = taskService.NewTask();

                tdDark.RegistrationInfo.Description = "Automatically switches to the Windows dark theme. " + descrpition;
                tdDark.RegistrationInfo.Author = author;
                tdDark.RegistrationInfo.Source = program;
                tdDark.Settings.DisallowStartIfOnBatteries = false;
                tdDark.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(10);
                tdDark.Settings.StartWhenAvailable = true;

                tdDark.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(startTime).AddMinutes(startTimeMinutes) });
                tdDark.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));

                taskService.GetFolder(folder).RegisterTaskDefinition(dark, tdDark);
                Console.WriteLine("created task for dark theme");

                //create task for LIGHT
                TaskDefinition tdLight = taskService.NewTask();

                tdLight.RegistrationInfo.Description = "Automatically switches to the Windows light theme. " + descrpition;
                tdLight.RegistrationInfo.Author = author;
                tdLight.RegistrationInfo.Source = program;
                tdLight.Settings.DisallowStartIfOnBatteries = false;
                tdLight.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(10);
                tdLight.Settings.StartWhenAvailable = true;

                tdLight.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(endTime).AddMinutes(endTimeMinutes) });
                tdLight.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/switch"));

                taskService.GetFolder(folder).RegisterTaskDefinition(light, tdLight);
                Console.WriteLine("created task for light theme");
            }
        }

        public void CreateLocationTask()
        {
            using(TaskService taskService = new TaskService())
            {
                TaskDefinition tdLocation = taskService.NewTask();

                tdLocation.RegistrationInfo.Description = "Updates the activation time of dark & light theme based on user location. " + descrpition;
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

        public void RemoveTask()
        {
            using (TaskService taskService = new TaskService())
            {
                TaskFolder taskFolder = taskService.GetFolder(folder);
                try
                {
                    taskFolder.DeleteTask(light);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(dark);
                }
                catch
                {

                }
                try
                {
                    taskFolder.DeleteTask(updater);
                }
                catch
                {

                }
                try
                {
                    taskService.RootFolder.DeleteFolder(folder);
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
                try
                {
                    TaskFolder taskFolder = taskService.GetFolder(folder);
                    taskFolder.DeleteTask(updater);
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
