﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private static string _url = "https://ac.saralesuvidha.com/";
        private static DateTime _updatedOn = DateTime.Now;
        private static string _userInner = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private static Dictionary<Guid, string> _failed = new Dictionary<Guid, string>();
        private static HttpListener _listener;
        private static object _lock = new object();
        private static GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
        private static DateTime? watcherStoppedOn = null;
        static void Main()
        {
            watcher.StatusChanged += Watcher_StatusChanged;
            // Start the watcher.  
            watcher.Start();
            var user = GetUser();
            var connection = new HubConnectionBuilder().WithUrl($"{_url}recordinghub").WithAutomaticReconnect().Build();
            ConnectSignalRServer(user, connection);
            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            var parentPath = Directory.GetParent(path).FullName;

            RecreateAppSettingFile(path, parentPath);

            DisablePrivateMode(path);

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:60024/");
            _listener.Start();
            var result = _listener.BeginGetContext(new AsyncCallback(Program.ProcessRequest), null);

            //result.AsyncWaitHandle.WaitOne();
            var appHelper = GetAppData(user).Result;

            try
            {
                while (true)
                {
                    if (watcherStoppedOn != null && watcherStoppedOn.GetValueOrDefault().Date < DateTime.Now.Date)
                    {
                        var appSettings = GetAppSettings();
                        if (appSettings.HitOn == null || appSettings.HitOn?.Date < DateTime.Now.Date)
                        {
                            watcher.Start();
                        }
                    }

                    UpdateAppSettings(ref appHelper, ref user);

                    ProcessFailed();

                    CheckAndRemoveHoldProcesses(appHelper, user);

                    NotifyAndKillOpenedProcesses(appHelper, user);

                    if (appHelper.DownloaderVersion != appHelper.InstalledDownloaderVersion)
                    {
                        if (File.Exists(parentPath + "\\AppDownloader.exe"))
                        {
                            File.Delete(parentPath + "\\AppDownloader.exe");
                        }
                        if (File.Exists(parentPath + "\\AppDownloader.pdb"))
                        {
                            File.Delete(parentPath + "\\AppDownloader.pdb");
                        }
                        var t = Task.Run(async () =>
                        {
                            await GetZip(parentPath, appHelper.DownloaderVersion);
                            ZipFile.ExtractToDirectory(parentPath + $"\\Downloader_{appHelper.DownloaderVersion}.zip", parentPath);
                            File.Delete(parentPath + $"\\Downloader_{appHelper.DownloaderVersion}.zip");
                            await PostData(new UserDetail { AppVersion = appHelper.InstalledAppVersion, User = user, DownloaderVersion = appHelper.DownloaderVersion });
                        });
                        t.Wait();
                        
                    }
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
                WriteException(new Exception("1"));
            }
        }

        private static async Task PostData(UserDetail appInfo)
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
                var response = await client.PostAsync("/appinfo/UpdateAppVersion", content);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        private async static Task GetZip(string path, string appVersion)
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                //GET Method

                var response = await client.GetAsync($"/appinfo/GetDownloaderZip");
                if (response.IsSuccessStatusCode)
                {
                    var rs = await response.Content.ReadAsAsync<byte[]>();
                    using (var writer = new BinaryWriter(File.OpenWrite(path + $"\\Downloader_{appVersion}.zip")))
                    {
                        writer.Write(rs);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        private static void ConnectSignalRServer(string user, HubConnection connection)
        {
            try
            {
                connection.Reconnected += (msg) => connection.InvokeAsync("RegisterUser", user);
                var helper = new WasapiCaptureHelper();
                connection.StartAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                    }
                    else
                    {
                        try
                        {
                            connection.InvokeAsync("RegisterUser", user);
                            WriteException(new Exception($"RegisterUser-{user}"));
                            if (connection.State == HubConnectionState.Connected)
                            {
                                connection.Closed += (ex) => Connection_Closed(ex, connection);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteException(ex);
                            WriteException(new Exception("2"));
                        }
                    }
                }).Wait();
                connection.On<string>("StartRecording", (message) =>
                {
                    try
                    {
                        if (!helper.isEnable)
                        {
                            helper.connectionURL = message;
                            helper.HandleRecording();
                            WriteException(new Exception($"StartRecording-{user}"));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteException(ex);
                        WriteException(new Exception("3"));
                    }
                });
                connection.On<string>("StopRecording", (message) =>
                {
                    try
                    {
                        helper.isEnable = false;
                        WriteException(new Exception($"StopRecording-{user}"));
                    }
                    catch (Exception ex)
                    {
                        WriteException(ex);
                        WriteException(new Exception("4"));
                    }
                });
            }
            catch (Exception ex)
            {
                WriteException(ex);
                WriteException(new Exception("5"));
            }
        }

        private static async Task Connection_Closed(Exception arg, HubConnection connection)
        {
            ConnectSignalRServer(_userInner, connection);
            await Task.CompletedTask;
        }

        private static void RecreateAppSettingFile(string path, string parentPath)
        {
            if (File.Exists(parentPath + "\\App.json"))
            {
                var appSettings = GetAppSettings();

                var data1 = File.ReadAllText(parentPath + "\\App.json");
                var appSettings1 = JsonConvert.DeserializeObject<AppSettings>(data1);

                appSettings.EnableExn = appSettings1.EnableExn;
                appSettings.PrivateModeDisable = appSettings1.PrivateModeDisable;
                appSettings.LastModified = appSettings1.LastModified;
                appSettings.HitOn = appSettings1.HitOn;
                WriteAppSettings(appSettings);
                File.Delete(parentPath + "\\App.json");
            }
        }

        private static void DisablePrivateMode(string path)
        {
            var chromeKeys = new string[4] { "SOFTWARE", "Policies", "Google", "Chrome" };
            var edgeKeys = new string[4] { "SOFTWARE", "Policies", "Microsoft", "Edge" };
            var appSettings = GetAppSettings();
            if (!appSettings.PrivateModeDisable)
            {
                AddKey(chromeKeys, "IncognitoModeAvailability");
                AddKey(edgeKeys, "InPrivateModeAvailability");
                
                appSettings.PrivateModeDisable = true;
                WriteAppSettings(appSettings);
            }
            AddKey(chromeKeys, "BrowserGuestModeEnabled", 0);
            AddKey(chromeKeys, "BrowserAddPersonEnabled", 0);
        }

        private static void AddKey(string[] keys, string keyName, int keyValue = 1)
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
                oldKeyInner.SetValue(keyName, keyValue);
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
                        var appSettings = GetAppSettings();
                        if (appSettings.LastModified == null)
                        {
                            appSettings.LastModified = modifiedDate;
                            WriteAppSettings(appSettings);
                        }
                        else if (appSettings.LastModified < modifiedDate)
                        {
                            appSettings.LastModified = modifiedDate;
                            WriteAppSettings(appSettings);
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
                WriteException(new Exception("6"));
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
                                var result = await PostDataInner(item.Value, "/appinfo", item.Key);
                                if (result)
                                {
                                    _failed.Remove(item.Key);
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
                WriteException(new Exception("7"));
            }
        }

        private static void NotifyAndKillOpenedProcesses(Helper appHelper, string user)
        {
            try
            {
                var apps = appHelper.AllowedAppsAndUrls
                .Where(_ => _.Type == "App" && (string.IsNullOrEmpty(_.User) || (!string.IsNullOrEmpty(_.User) && user.Contains(_.User))))
                .Select(_ => _.Name);

                var processes = Process.GetProcesses().Where(_ => _.MainWindowHandle != IntPtr.Zero && IsApplicableProcess(_, apps));
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
                        WriteException(new Exception("8"));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
                WriteException(new Exception("9"));
            }
        }

        private static bool IsApplicableProcess(Process process, IEnumerable<string> apps)
        {
            if (process.ProcessName == "ApplicationFrameHost")
            {
                try
                {
                    var process1 = GetRealProcess(process);
                    return !apps.Contains(process1.ProcessName);
                }
                catch(Exception ex)
                {
                    return false;
                }

            }
            else
            {
                return !apps.Contains(process.ProcessName);
            }
        }

        private static Process _realProcess;
        private static Process GetRealProcess(Process foregroundProcess)
        {
            WinAPIFunctions.EnumChildWindows(foregroundProcess.MainWindowHandle, ChildWindowCallback, IntPtr.Zero);
            return _realProcess;
        }

        private static bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
        {
            var process = Process.GetProcessById(WinAPIFunctions.GetWindowProcessId(hwnd));
            if (process.ProcessName != "ApplicationFrameHost")
            {
                _realProcess = process;
            }
            return true;
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
                WriteException(new Exception("10"));
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
                            WriteException(new Exception("11"));
                        }
                    }
                    _processIds.Clear();
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
                WriteException(new Exception("12"));
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
            var payload = JsonConvert.SerializeObject(appInfo);
            await PostDataInner(payload, "/appinfo", Guid.NewGuid());
        }

        private static async Task<bool> PostDataInner(string payload, string endpoint, Guid id)
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

                // Wrap our JSON inside a StringContent object
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Post to the endpoint
                var response = await client.PostAsync(endpoint, content);
                return true;
            }
            catch(Exception ex)
            {
                WriteException(ex);
                WriteException(new Exception("13"));
                if (!_failed.ContainsKey(id))
                {
                    _failed.Add(id, payload);
                }
                return false;
            }
        }

        private static async Task<Helper> GetAppData(string user, string summary = "")
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
                    WriteException(new Exception("14"));
                }
            }
            return helper;
        }

        private static void Watcher_StatusChanged(object sen, GeoPositionStatusChangedEventArgs e) // Find GeoLocation of Device  
        {
            try
            {
                if (e.Status == GeoPositionStatus.Ready)
                {
                    if (!watcher.Position.Location.IsUnknown)
                    {
                        var summary = string.Empty;
                        var appSettings = GetAppSettings();
                        if (appSettings.HitOn == null || appSettings.HitOn?.Date < DateTime.Now.Date)
                        {
                            var latitude = watcher.Position.Location.Latitude.ToString();
                            var longitute = watcher.Position.Location.Longitude.ToString();
                            summary = $"Latitude: {latitude}, Longitude: {longitute}";
                            appSettings.HitOn = DateTime.Now.Date;
                            WriteAppSettings(appSettings);
                            watcher.Stop();
                            watcherStoppedOn = appSettings.HitOn;
                            var latLongInfo = new LatLongInfo()
                            {
                                User = GetUser(),
                                Summary = summary
                            };
                            var payload = JsonConvert.SerializeObject(latLongInfo);
                            var ta = Task.Run(async () =>
                                {
                                    var response = await PostDataInner(payload, "/appinfo/SetLatLong", Guid.NewGuid());
                                });
                            ta.Wait();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static AppSettings GetAppSettings()
        {
            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            var data = File.ReadAllText(path + "\\App.json");
            return JsonConvert.DeserializeObject<AppSettings>(data);
        }

        private static void WriteAppSettings(AppSettings appSettings)
        {
            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            File.WriteAllText(path + "\\App.json", JsonConvert.SerializeObject(appSettings));
        }
    }

    public class WinAPIFunctions
    {
        //Used to get Handle for Foreground Window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetForegroundWindow();

        //Used to get ID of any Window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        public static int GetWindowProcessId(IntPtr hwnd)
        {
            int pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return pid;
        }

        public static IntPtr GetforegroundWindow()
        {
            return GetForegroundWindow();
        }
    }

    public class Helper
    {
        public List<AllowedAppsAndUrl> AllowedAppsAndUrls { get; set; }
        public bool KillApps { get; set; }
        public int UserValidity { get; internal set; }
        public string AppVersion { get; set; }
        public string InstalledAppVersion { get; set; }
        public string DownloaderVersion { get; set; }
        public string InstalledDownloaderVersion { get; set; }
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
        public DateTime? HitOn { get; set;}

        public bool PrivateModeDisable { get; set; }
    }

    public class ExtensionUpdate
    {
        public bool IsModified { get; set; }
    }

    public class UserDetail
    {
        public string User { get; set; }
        public string AppVersion { get; set; }
        public string DownloaderVersion { get; set; }
    }

    public class LatLongInfo
    {
        public string User { get; set; }
        public string Summary { get; set; }
    }
}