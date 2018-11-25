using System;
using Microsoft.Win32.TaskScheduler;

namespace AutoThemeChanger
{
    public class TaskShedHandler
    {
        public void CreateTask(int startTime, int endTime)
        {
            using (TaskService taskService = new TaskService())
            {
                //create task for DARK
                TaskDefinition tdDark = taskService.NewTask();

                tdDark.RegistrationInfo.Description = "Automatically switches to the Windows dark theme. Task of the program Windows Auto-Night Mode.";
                tdDark.RegistrationInfo.Author = "Armin Osaj";
                tdDark.RegistrationInfo.Source = "Windows Auto Night-Mode";
                tdDark.Settings.DisallowStartIfOnBatteries = false;
                tdDark.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(10);
                tdDark.Settings.StartWhenAvailable = true;

                tdDark.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(startTime) });
                tdDark.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/dark"));

                taskService.RootFolder.RegisterTaskDefinition(@"Auto-Night Mode Dark", tdDark);
                Console.WriteLine("created task for dark theme");

                //create task for LIGHT
                TaskDefinition tdLight = taskService.NewTask();

                tdLight.RegistrationInfo.Description = "Automatically switches to the Windows light theme. Task of the program Windows Auto-Night Mode.";
                tdLight.RegistrationInfo.Author = "Armin Osaj";
                tdLight.RegistrationInfo.Source = "Windows Auto Night-Mode";
                tdLight.Settings.DisallowStartIfOnBatteries = false;
                tdLight.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(10);
                tdLight.Settings.StartWhenAvailable = true;

                tdLight.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddDays(0).AddHours(endTime) });
                tdLight.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/light"));

                taskService.RootFolder.RegisterTaskDefinition(@"Auto-Night Mode Light", tdLight);
                Console.WriteLine("created task for light theme");
            }
        }

        public void RemoveTask()
        {
            using (TaskService taskService = new TaskService())
            {
                try
                {
                    TaskFolder taskFolder = taskService.RootFolder;
                    taskFolder.DeleteTask("Auto-Night Mode Light");
                    taskFolder.DeleteTask("Auto-Night Mode Dark");
                }
                catch (Exception)
                {

                }
            }
        }

        public string CheckExistingClass()
        {
            using (TaskService taskService = new TaskService())
            {
                try
                {
                    var task1 = taskService.FindTask("Auto-Night Mode Dark").ToString();
                    var task2 = taskService.FindTask("Auto-Night Mode Light").ToString();
                    return task1 + task2;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int GetRunTime(string theme)
        {
            using (TaskService taskService = new TaskService())
            {
                if(theme == "dark")
                {
                    return GetRunHour(taskService.FindTask("Auto-Night Mode Dark"));
                }else{
                    return GetRunHour(taskService.FindTask("Auto-Night Mode Light"));
                }
            }
        }

        private int GetRunHour(Task task)
        {
                DateTime time = task.NextRunTime;
                return time.Hour;
        }
    }
}
