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
        private static List<long> _processIds = new List<long>();
        private static Dictionary<long, List<Tuple<string, string>>> _browserProcessIds = new Dictionary<long, List<Tuple<string, string>>>();
        private static string url = "https://manvindarsingh.bsite.net";
        static void Main()
        {
            var appHelper = GetAppData().Result;
            var updatedOn = DateTime.Now;
            var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string myIP = GetIP();
            user = user + " (" + myIP + ")";
            try
            {
                while (true)
                {
                    if ((DateTime.Now - updatedOn).TotalMinutes > 2)
                    {
                        appHelper = GetAppData().Result;
                        updatedOn = DateTime.Now;
                        myIP = GetIP();
                        user = user + " (" + myIP + ")";
                    }
                    var list = new List<string>();
                    var apps = appHelper.AllowedAppsAndUrls.Where(_ => _.Type == "App").Select(_ => _.Name);
                    var processes = Process.GetProcesses().Where(_ => !apps.Contains(_.ProcessName));

                    foreach (Process p in processes)
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle))
                        {
                            list.Add(p.MainWindowTitle);
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
                    List<Process> procsChrome = Process.GetProcessesByName("chrome").ToList();
                    procsChrome.AddRange(Process.GetProcessesByName("msedge"));
                    foreach (Process proc in procsChrome)
                    {
                        // the chrome process must have a window 
                        if (proc.MainWindowHandle == IntPtr.Zero)
                        {
                            continue;
                        }
                        AutomationElement root = AutomationElement.FromHandle(proc.MainWindowHandle);
                        Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
                        var tabs = root.FindAll(TreeScope.Descendants, condition);
                        foreach (AutomationElement tabitem in tabs)
                        {
                            if (_browserProcessIds.ContainsKey(proc.Id))
                            {
                                if (!string.IsNullOrEmpty(tabitem.Current.Name))
                                {
                                    var matchedItem = _browserProcessIds[proc.Id].FirstOrDefault(_ => _.Item1 == tabitem.Current.AutomationId && _.Item2 == tabitem.Current.Name);
                                    if (matchedItem == null)
                                    {
                                        var t = Task.Run(async () =>
                                        {
                                            await PostData(proc.ProcessName, user, tabitem.Current.Name);
                                            _browserProcessIds[proc.Id].Add(new Tuple<string, string>(tabitem.Current.AutomationId, tabitem.Current.Name));
                                        });
                                        t.Wait();
                                    }
                                }
                            }
                            else
                            {
                                var t = Task.Run(async () =>
                                {
                                    if (!string.IsNullOrEmpty(tabitem.Current.Name))
                                    {
                                        await PostData(proc.ProcessName, user, tabitem.Current.Name);
                                        _browserProcessIds.Add(proc.Id, new List<Tuple<string, string>> { new Tuple<string, string>(tabitem.Current.AutomationId, tabitem.Current.Name) });
                                    }
                                });
                                t.Wait();
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                var st = string.Empty;
                if (File.Exists("test.txt"))
                {
                    st = File.ReadAllText("test.txt");
                }
                File.WriteAllText("test.txt", st + Environment.NewLine + ex.Message);
            }
        }

        private static string GetIP()
        {
            string hostName = Dns.GetHostName();
            return Dns.GetHostByName(hostName).AddressList[0].ToString();
        }

        private static async Task PostData(string appName, string user, string summary)
        {
            var processing = true;
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
                    client.BaseAddress = new Uri(url);

                    // Create an object
                    var appInfo = new AppInfo()
                    {
                        Id = string.Empty,
                        Date = DateTime.Now,
                        AppName = appName,
                        Summary = summary,
                        User = user,
                    };

                    // Serialize class into JSON
                    var payload = JsonConvert.SerializeObject(appInfo);

                    // Wrap our JSON inside a StringContent object
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    // Post to the endpoint
                    var response = await client.PostAsync("/appinfo", content);
                    processing = false;
                }
                catch
                {
                }
            }

        }

        private static async Task<Helper> GetAppData()
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
                    client.BaseAddress = new Uri(url);

                    // Post to the endpoint

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //GET Method
                    var response = await client.GetAsync("/appinfo/GetApplicationSettings");
                    if (response.IsSuccessStatusCode)
                    {
                        helper = await response.Content.ReadAsAsync<Helper>();
                        processing = false;
                        return helper;
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
    }

    public class AppInfo
    {
        public string Id { get; set; }

        public DateTime Date { get; set; }

        public string User { get; set; }

        public string AppName { get; set; }

        public string Summary { get; set; }
    }
}