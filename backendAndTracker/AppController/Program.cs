using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace AppController
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        private static List<int> _processIds = new List<int>();
        //private static Dictionary<long, List<Tuple<string, string>>> _browserProcessIds = new Dictionary<long, List<Tuple<string, string>>>();
        private static string _url = "https://manvindarsingh.bsite.net";
        private static DateTime _updatedOn = DateTime.Now;
        private static string _userInner = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private static List<AppInfo> _failed = new List<AppInfo>();

        static void Main()
        {
            var user = GetUser();
            var appHelper = GetAppData(user).Result;

            try
            {
                while (true)
                {
                    UpdateAppSettings(ref appHelper, ref user);

                    ProcessFailed();

                    CheckAndRemoveHoldProcesses(appHelper);

                    NotifyAndKillOpenedProcesses(appHelper, user);

                    //NotifyOpenedBrowserTabs(user);

                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                var st = string.Empty;
                if (System.IO.File.Exists("test.txt"))
                {
                    st = System.IO.File.ReadAllText("test.txt");
                }
                System.IO.File.WriteAllText("test.txt", st + Environment.NewLine + ex.Message);
            }
        }

        private static void ProcessFailed()
        {
            if(_failed.Any())
            {
                var processing = _failed.ToList();
                foreach (var item in processing)
                {
                    var t = Task.Run(async () =>
                    {
                        var result = await PostDataInner(item);
                        if(result)
                        {
                            _failed.Remove(item);
                        }
                    });
                    t.Wait();
                }
            }
        }

        private static void NotifyAndKillOpenedProcesses(Helper appHelper, string user)
        {
            var apps = appHelper.AllowedAppsAndUrls
                .Where(_ => _.Type == "App" && (string.IsNullOrEmpty(_.User) || (!string.IsNullOrEmpty(_.User) && user.Contains(_.User))))
                .Select(_ => _.Name);

            var processes = Process.GetProcesses().Where(_ => _.MainWindowHandle != IntPtr.Zero && !apps.Contains(_.ProcessName));
            foreach (Process p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    if (appHelper.KillApps)
                    {
                        p.Kill();
                    }

                    var t = Task.Run(async () =>
                    {
                        if (!_processIds.Contains(p.Id))
                        {
                            await PostData(p.ProcessName, user, p.MainWindowTitle);
                        }
                    });
                    t.Wait();
                    if (!appHelper.KillApps && !_processIds.Contains(p.Id))
                    {
                        _processIds.Add(p.Id);
                    }
                }
            }
        }

        //private static void NotifyOpenedBrowserTabs(string user)
        //{
        //    List<Process> procsChrome = Process.GetProcessesByName("chrome").ToList();
        //    procsChrome.AddRange(Process.GetProcessesByName("msedge"));
        //    foreach (Process proc in procsChrome)
        //    {
        //        // the chrome process must have a window 
        //        if (proc.MainWindowHandle == IntPtr.Zero)
        //        {
        //            continue;
        //        }
        //        AutomationElement root = AutomationElement.FromHandle(proc.MainWindowHandle);
        //        Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
        //        var tabs = root.FindAll(TreeScope.Descendants, condition);
        //        foreach (AutomationElement tabitem in tabs)
        //        {
        //            if (_browserProcessIds.ContainsKey(proc.Id))
        //            {
        //                if (!string.IsNullOrEmpty(tabitem.Current.Name))
        //                {
        //                    var matchedItem = _browserProcessIds[proc.Id].FirstOrDefault(_ => _.Item1 == tabitem.Current.AutomationId && _.Item2 == tabitem.Current.Name);
        //                    if (matchedItem == null)
        //                    {
        //                        var t = Task.Run(async () =>
        //                        {
        //                            await PostData(proc.ProcessName, user, tabitem.Current.Name);
        //                            _browserProcessIds[proc.Id].Add(new Tuple<string, string>(tabitem.Current.AutomationId, tabitem.Current.Name));
        //                        });
        //                        t.Wait();
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var t = Task.Run(async () =>
        //                {
        //                    if (!string.IsNullOrEmpty(tabitem.Current.Name))
        //                    {
        //                        await PostData(proc.ProcessName, user, tabitem.Current.Name);
        //                        _browserProcessIds.Add(proc.Id, new List<Tuple<string, string>> { new Tuple<string, string>(tabitem.Current.AutomationId, tabitem.Current.Name) });
        //                    }
        //                });
        //                t.Wait();
        //            }
        //        }
        //    }
        //}

        private static void UpdateAppSettings(ref Helper appHelper, ref string user)
        {
            if ((DateTime.Now - _updatedOn).TotalMinutes > 2)
            {
                user = GetUser();
                appHelper = GetAppData(user).Result;
                _updatedOn = DateTime.Now;
            }
        }

        private static string GetUser()
        {
            return _userInner;
        }

        private static void CheckAndRemoveHoldProcesses(Helper appHelper)
        {
            if (_processIds.Any() && appHelper.KillApps)
            {
                foreach (var processId in _processIds)
                {
                    var p = Process.GetProcessById(processId);
                    p?.Kill();
                }
                _processIds.Clear();
            }
        }

        private static async Task PostData(string appName, string user, string summary)
        {
            var appInfo = new AppInfo()
            {
                Id = string.Empty,
                Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                AppName = appName,
                Summary = summary,
                User = user,
            };
            await PostDataInner(appInfo);
        }

        private static async Task<bool> PostDataInner(AppInfo appInfo)
        {
            try
            {
                if((DateTime.Now - _updatedOn).TotalSeconds < 15)
                {
                    throw new Exception();
                }
                
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                var client = new HttpClient(handler);

                // Set the base address to simplify maintenance & requests
                client.BaseAddress = new Uri(_url);

                // Serialize class into JSON
                var payload = JsonConvert.SerializeObject(appInfo);

                // Wrap our JSON inside a StringContent object
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Post to the endpoint
                var response = await client.PostAsync("/appinfo", content);
                return true;
            }
            catch
            {
                if(!_failed.Contains(appInfo))
                {
                    _failed.Add(appInfo);
                }
                return false;
            }
        }

        private static async Task<Helper> GetAppData(string user)
        {
            var processing = true;
            Helper helper = null;
            while (processing)
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    };
                    var client = new HttpClient(handler);

                    // Set the base address to simplify maintenance & requests
                    client.BaseAddress = new Uri(_url);

                    // Post to the endpoint

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //GET Method
                    
                    var response = await client.GetAsync($"/appinfo/GetApplicationSettingsByUser?user={user}");
                    if (response.IsSuccessStatusCode)
                    {
                        helper = await response.Content.ReadAsAsync<Helper>();
                        helper.AllowedAppsAndUrls.Add(new AllowedAppsAndUrl { Name = "AppController", Type = "App" });
                        processing = false;
                    }
                }
                catch
                {
                }
            }
            return helper;
        }
    }

    public class Helper
    {
        public List<AllowedAppsAndUrl> AllowedAppsAndUrls { get; set; }
        public bool KillApps { get; set; }
    }

    public partial class AllowedAppsAndUrl
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string User { get; set; }
    }

    public class AppInfo
    {
        public string Id { get; set; }

        public string Date { get; set; }

        public string User { get; set; }

        public string AppName { get; set; }

        public string Summary { get; set; }
    }
}