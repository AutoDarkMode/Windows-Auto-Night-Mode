using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeApp.Handlers
{
    static class TaskSchdHandler
    {
        static readonly string folder = "ADM_" + Environment.UserName;
        static readonly string author = "Armin Osaj";
        static readonly string program = "Windows Auto Dark Mode";
        static readonly string description = "Task of the program Windows Auto Dark Mode.";
        static readonly string appupdater = "ADM AppUpdater";
        public static void CreateAppUpdaterTask()
        {
            using TaskService taskService = new();
            TaskDefinition tdUpdate = taskService.NewTask();

            tdUpdate.RegistrationInfo.Description = "Checks the GitHub-Server for new app version in the background. " + description;
            tdUpdate.RegistrationInfo.Author = author;
            tdUpdate.RegistrationInfo.Source = program;
            tdUpdate.Settings.DisallowStartIfOnBatteries = false;
            tdUpdate.Settings.ExecutionTimeLimit = TimeSpan.FromMinutes(15);
            tdUpdate.Settings.StartWhenAvailable = true;

            tdUpdate.Triggers.Add(new MonthlyTrigger { StartBoundary = DateTime.Today.AddMonths(1) + TimeSpan.FromHours(12) });
            tdUpdate.Actions.Add(new ExecAction(System.Reflection.Assembly.GetExecutingAssembly().Location, "/update"));

            taskService.GetFolder(folder).RegisterTaskDefinition(appupdater, tdUpdate);
            Console.WriteLine("created task for app updates");
        }

        public static void RemoveAppUpdaterTask()
        {
            using TaskService taskService = new();
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
}
