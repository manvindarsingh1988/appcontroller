
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
                
                td.Triggers.Add(tt);
                var tt1 = new SessionStateChangeTrigger(TaskSessionStateChangeType.SessionUnlock);
                td.Triggers.Add(tt1);
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var st = string.Empty;
                if (System.IO.File.Exists("StartProcess.bat"))
                {
                    st = System.IO.File.ReadAllText("StartProcess.bat");
                    st = st.Replace("$Path$", $"{path}AppController.exe");
                }
                System.IO.File.WriteAllText("StartProcess.bat", st);
                
                td.Actions.Add(new ExecAction($"\"\"{path}StartProcess.bat\"\"", null, null));
                
                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(@"AppControllerTask", td);
                var t = ts.AllTasks.FirstOrDefault(_ => _.Name == "AppControllerTask");
                var j = t.Run();
            }
        }
    }
}
