using System.Security.Principal;
using System;
using System.Linq;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace TaskScheduler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var chromeKeys = new string[4] { "SOFTWARE", "Policies", "Google", "Chrome" };
            var edgeKeys = new string[4] { "SOFTWARE", "Policies", "Microsoft", "Edge" };
            AddKey(chromeKeys, "IncognitoModeAvailability");

            using (TaskService ts = new TaskService())
            {
                var task = ts.AllTasks.FirstOrDefault(_ => _.Name == "AppControllerTask");
                if (task != null)
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

                td.Actions.Add(new ExecAction($"\"\"{path}AppDownloader.exe\"\"", null, null));

                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(@"AppControllerTask", td);
                var t = ts.AllTasks.FirstOrDefault(_ => _.Name == "AppControllerTask");
                var j = t.Run();
            }
            AddKey("SOFTWARE\\Policies\\Google\\Chrome", "IncognitoModeAvailability");
            AddKey("SOFTWARE\\Policies\\Microsoft\\Edge", "InPrivateModeAvailability");
            AddKey(chromeKeys, "IncognitoModeAvailability");
            AddKey(edgeKeys, "InPrivateModeAvailability");
        }

        private static void AddKey(string path, string key)
        {
            var obj = Registry.LocalMachine.OpenSubKey(path, true);
            if(obj != null)
            {
                var keys = obj.GetValueNames();
                if (!keys.Contains(key))
                {
                    obj.SetValue(key, 1);
                }
            }
        }

        private static void AddKey(string[] keys, string keyName)
        {
            RegistryKey keyInner;
            RegistryKey oldKeyInner = null;
            var st = string.Empty;
            foreach(var key in keys)
            {
                st = string.IsNullOrEmpty(st) ? key : st + "\\" + key;
                keyInner = Registry.LocalMachine.OpenSubKey(st, true);
                if (keyInner != null)
                {
                    oldKeyInner = keyInner;
                }
                else
                {
                    oldKeyInner = oldKeyInner.CreateSubKey(key, true);
                }
            }
            var allKeys = oldKeyInner.GetValueNames();
            if (!allKeys.Contains(keyName))
            {
                oldKeyInner.SetValue(keyName, 1);
            }
        }
    }
}
