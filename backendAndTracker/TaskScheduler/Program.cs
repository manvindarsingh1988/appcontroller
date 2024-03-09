
using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;
using System;
using System.Linq;

namespace TaskScheduler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();

                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Principal.UserId = WindowsIdentity.GetCurrent().Name;
                td.Principal.LogonType = TaskLogonType.InteractiveToken;

                td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StartWhenAvailable = true;
                td.Settings.Enabled = true;
                td.Settings.Hidden = false;
                td.Settings.AllowHardTerminate = false;
                td.Settings.ExecutionTimeLimit = new TimeSpan();
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.RunOnlyIfIdle = false;
                
                var tt = new LogonTrigger();
                tt.Delay = new TimeSpan(0, 0, 30);
                td.Triggers.Add(tt);
                var path = AppDomain.CurrentDomain.BaseDirectory;
                td.Actions.Add(new ExecAction($"\"\"{path}AppController.exe\"\"", null, null));
                
                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(@"Test1", td);
                var t = ts.AllTasks.FirstOrDefault(_ => _.Name == "Test1");
                var j = t.Run();
                // Remove the task we just created
                //ts.RootFolder.DeleteTask("Test1");
                //ts.StartSystemTaskSchedulerManager();
            }
        }
    }
}
