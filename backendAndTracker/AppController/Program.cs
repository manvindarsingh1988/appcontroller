﻿using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
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
        private static string _url = "https://www.appcontroller.in/";
        private static DateTime _updatedOn = DateTime.Now;
        private static string _userInner = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private static List<AppInfo> _failed = new List<AppInfo>();
        private static HttpListener _listener;
        private static object _lock = new object();

        static void Main()
        {
            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            var parentPath = Directory.GetParent(path).FullName;

            RecreateAppSettingFile(path, parentPath);

            DisablePrivateMode(path);

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:60024/");
            _listener.Start();
            var result = _listener.BeginGetContext(new AsyncCallback(Program.ProcessRequest), null);

            //result.AsyncWaitHandle.WaitOne();

            var user = GetUser();
            var appHelper = GetAppData(user).Result;

            try
            {
                while (true)
                {
                    UpdateAppSettings(ref appHelper, ref user);

                    ProcessFailed();

                    CheckAndRemoveHoldProcesses(appHelper, user);

                    NotifyAndKillOpenedProcesses(appHelper, user);

                    //NotifyOpenedBrowserTabs(user);
                    if (appHelper.AppVersion != appHelper.InstalledAppVersion)
                    {
                        File.Copy(path + "\\App.json", parentPath + "\\App.json");
                        Process.Start(new ProcessStartInfo(parentPath + "\\AppDownloader.exe"));
                    }

                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        private static void RecreateAppSettingFile(string path, string parentPath)
        {
            if (File.Exists(parentPath + "\\App.json"))
            {
                var data = File.ReadAllText(path + "\\App.json");
                var appSettings = JsonConvert.DeserializeObject<AppSettings>(data);

                var data1 = File.ReadAllText(parentPath + "\\App.json");
                var appSettings1 = JsonConvert.DeserializeObject<AppSettings>(data1);

                appSettings.EnableExn = appSettings1.EnableExn;
                appSettings.PrivateModeDisable = appSettings1.PrivateModeDisable;
                appSettings.LastModified = appSettings1.LastModified;
                File.WriteAllText(path + "\\App.json", JsonConvert.SerializeObject(appSettings));
                File.Delete(parentPath + "\\App.json");
            }
        }

        private static void DisablePrivateMode(string path)
        {
            var data = File.ReadAllText(path + "\\App.json");

            var appSettings = JsonConvert.DeserializeObject<AppSettings>(data);
            if (!appSettings.PrivateModeDisable)
            {
                var chromeKeys = new string[4] { "SOFTWARE", "Policies", "Google", "Chrome" };
                var edgeKeys = new string[4] { "SOFTWARE", "Policies", "Microsoft", "Edge" };
                AddKey(chromeKeys, "IncognitoModeAvailability");
                AddKey(edgeKeys, "InPrivateModeAvailability");
                appSettings.PrivateModeDisable = true;
                File.WriteAllText(path + "\\App.json", JsonConvert.SerializeObject(appSettings));
            }
        }

        private static void AddKey(string[] keys, string keyName)
        {
            RegistryKey keyInner;
            RegistryKey oldKeyInner = null;
            var st = string.Empty;
            foreach (var key in keys)
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

        static void ProcessRequest(IAsyncResult result)
        {
            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Max-Age", "1728000");
            response.Headers.Add("Access-Control-Allow-Origin", "*");

            string postData;
            EventDetail eventDetail = new EventDetail();
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                postData = reader.ReadToEnd();
                eventDetail = JsonConvert.DeserializeObject<EventDetail>(postData);
                //use your favourite json parser here
            }
            
            var app = GetData(eventDetail);
            byte[] buffer = Encoding.UTF8.GetBytes(app);
            
            response.ContentLength64 = buffer.Length;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ProtocolVersion = new Version("1.1");
            Stream output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);

            output.Close();
            response.Close();
            _listener.BeginGetContext(new AsyncCallback(ProcessRequest), null);
        }

        private static string GetData(EventDetail eventDetail)
        {
            try
            {
                lock (_lock)
                {
                    var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
                    if (eventDetail.EventName == "GetExtensionInability")
                    {
                        return File.ReadAllText(path + "\\App.json");
                    }

                    if (eventDetail.EventName == "GetExtensionModified")
                    {
                        var res = new ExtensionUpdate() { IsModified = false };
                        var directory = new DirectoryInfo(path + "\\Extension\\js");
                        var modifiedDate = directory.GetFiles().Max(file => file.LastWriteTime);
                        var data = File.ReadAllText(path + "\\App.json");

                        var appSettings = JsonConvert.DeserializeObject<AppSettings>(data);
                        if (appSettings.LastModified == null)
                        {
                            appSettings.LastModified = modifiedDate;
                            File.WriteAllText(path + "\\App.json", JsonConvert.SerializeObject(appSettings));
                        }
                        else if (appSettings.LastModified < modifiedDate)
                        {
                            appSettings.LastModified = modifiedDate;
                            File.WriteAllText(path + "\\App.json", JsonConvert.SerializeObject(appSettings));
                            res.IsModified = true;
                        }
                        return JsonConvert.SerializeObject(res);
                    }

                    if (eventDetail.EventName == "GetUser")
                    {
                        return JsonConvert.SerializeObject(new UserDetail { User = _userInner });
                    }
                }
            }
            catch (Exception ex) 
            {
                WriteException(ex);
            }
            return string.Empty;
        }

        private static void WriteException(Exception ex)
        {
            var st = string.Empty;
            if (System.IO.File.Exists("test.txt"))
            {
                st = System.IO.File.ReadAllText("test.txt");
            }
            System.IO.File.WriteAllText("test.txt", st + Environment.NewLine + ex.Message);
        }

        private static void ProcessFailed()
        {
            try
            {
                if (_failed.Any())
                {
                    var processing = _failed.ToList();
                    foreach (var item in processing)
                    {
                        try
                        {
                            var t = Task.Run(async () =>
                            {
                                var result = await PostDataInner(item);
                                if (result)
                                {
                                    _failed.Remove(item);
                                }
                            });
                            t.Wait();
                        }
                        catch (Exception ex)
                        {
                            WriteException(ex);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                WriteException(ex);
            }
        }

        private static void NotifyAndKillOpenedProcesses(Helper appHelper, string user)
        {
            try
            {
                var apps = appHelper.AllowedAppsAndUrls
                .Where(_ => _.Type == "App" && (string.IsNullOrEmpty(_.User) || (!string.IsNullOrEmpty(_.User) && user.Contains(_.User))))
                .Select(_ => _.Name);

                var processes = Process.GetProcesses().Where(_ => _.MainWindowHandle != IntPtr.Zero && !apps.Contains(_.ProcessName));
                foreach (Process p in processes)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle))
                        {
                            if (appHelper.KillApps)
                            {
                                var ta = Task.Run(async () =>
                                {
                                    if (!_processIds.Contains(p.Id))
                                    {
                                        await PostData(p.ProcessName, user, p.MainWindowTitle + " being killed.");
                                    }
                                });
                                ta.Wait();
                                p?.Kill();
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
                    catch (Exception ex)
                    {
                        WriteException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
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
            try
            {
                if ((DateTime.Now - _updatedOn).TotalMinutes > 2)
                {
                    user = GetUser();
                    appHelper = GetAppData(user).Result;
                    _updatedOn = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        private static string GetUser()
        {
            _userInner = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            return _userInner;
        }

        private static void CheckAndRemoveHoldProcesses(Helper appHelper, string user)
        {
            try
            {
                if (_processIds.Any() && appHelper.KillApps)
                {
                    var apps = appHelper.AllowedAppsAndUrls
                    .Where(_ => _.Type == "App" && (string.IsNullOrEmpty(_.User) || (!string.IsNullOrEmpty(_.User) && user.Contains(_.User))))
                    .Select(_ => _.Name);
                    foreach (var processId in _processIds)
                    {
                        try
                        {
                            var p = Process.GetProcessById(processId);
                            if (!apps.Contains(p.ProcessName))
                            {
                                var ta = Task.Run(async () =>
                                {
                                    if (!_processIds.Contains(p.Id))
                                    {
                                        await PostData(p.ProcessName, user, p.MainWindowTitle + " being killed.");
                                    }
                                });
                                ta.Wait();
                                p?.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteException(ex);
                        }
                    }
                    _processIds.Clear();
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
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
            catch(Exception ex)
            {
                WriteException(ex);
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
                        helper.AllowedAppsAndUrls.Add(new AllowedAppsAndUrl { Name = "AppDownloader", Type = "App" });
                        processing = false;
                    }
                }
                catch( Exception ex)
                {
                    WriteException(ex);
                }
            }
            return helper;
        }
    }

    public class Helper
    {
        public List<AllowedAppsAndUrl> AllowedAppsAndUrls { get; set; }
        public bool KillApps { get; set; }
        public int UserValidity { get; internal set; }
        public string AppVersion { get; set; }
        public string InstalledAppVersion { get; set; }
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

    public class EventDetail
    {
        public string EventName { get; set; }
    }

    public class AppSettings
    {
        public int EnableExn { get; set; }
        public DateTime? LastModified { get; set;}

        public bool PrivateModeDisable { get; set; }
    }

    public class ExtensionUpdate
    {
        public bool IsModified { get; set; }
    }

    public class UserDetail
    {
        public string User { get; set; }
    }
}