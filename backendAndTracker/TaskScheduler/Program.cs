
using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;
using System;
using System.Linq;
using System.IO;
using System.Net;

namespace TaskScheduler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string _userInner = WindowsIdentity.GetCurrent().Name.Replace(@"\","\\\\");
            string hostName = Dns.GetHostName();
            var ip = Dns.GetHostByName(hostName).AddressList[0].ToString();
            if (File.Exists("extension/js/background.js"))
            {
                var js = System.IO.File.ReadAllText("extension/js/background.js");
                js = js.Replace("$user$", _userInner + " (" + ip + ")");
                File.WriteAllText("extension/js/background.js", js);
            }
            
            using (TaskService ts = new TaskService())
            {
                var task = ts.AllTasks.FirstOrDefault(_ => _.Name == "AppControllerTask");
                if(task != null)
                {
                    ts.RootFolder.DeleteTask("AppControllerTask");
                }

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
                    File.WriteAllText("StartProcess.bat", st);
                }
                
                
                td.Actions.Add(new ExecAction($"\"\"{path}StartProcess.bat\"\"", null, null));
                
                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(@"AppControllerTask", td);
                var t = ts.AllTasks.FirstOrDefault(_ => _.Name == "AppControllerTask");
                var j = t.Run();
            }
        }
    }
}
