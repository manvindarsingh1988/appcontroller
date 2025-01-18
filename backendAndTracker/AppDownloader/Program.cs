using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AppDownloader
{
    internal class Program
    {
        private static string _url = "https://ac.saralesuvidha.com/";

        static void Main(string[] args)
        {
            try
            {
                var user = GetUser();
                WriteException(user);
                var appHelper = GetAppData(user).Result;
                WriteException(appHelper.AppVersion);
                WriteException(appHelper.InstalledAppVersion);
                var t = Task.Run(async () =>
                {
                    var parent = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
                    WriteException(parent);
                    var processes = Process.GetProcessesByName("AppController");
                    if (appHelper.AppVersion != appHelper.InstalledAppVersion)
                    {
                        WriteException(appHelper.AppVersion);
                        await GetZip(parent, appHelper.AppVersion);
                        WriteException("zipDownloaded");
                        if (processes != null && processes.Any())
                        {
                            foreach (var proc in processes)
                            {
                                proc.Kill();
                            }
                        }
                        if (Directory.Exists(parent + "\\AppController"))
                        {
                            Directory.Delete(parent + "\\AppController", true);
                        }

                        Directory.CreateDirectory(parent + "\\AppController");
                        ZipFile.ExtractToDirectory(parent + $"\\{appHelper.AppVersion}.zip", parent + "\\AppController");

                        File.Delete(parent + $"\\{appHelper.AppVersion}.zip");
                    }
                    await PostData(new UserDetail { AppVersion = appHelper.AppVersion, User = user, DownloaderVersion = appHelper.InstalledDownloaderVersion });
                    if (processes == null || !processes.Any())
                    {
                        var path = Path.Combine(parent + "\\AppController", "AppController.exe");
                        Process.Start(new ProcessStartInfo(path));
                    }
                });
                t.Wait();
            }
            catch(Exception ex)
            {
                WriteException(ex.InnerException);
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

        private static string GetUser()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
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
                        processing = false;
                    }
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                }
            }
            return helper;
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

                var response = await client.GetAsync($"/appinfo/GetZip");
                if (response.IsSuccessStatusCode)
                {
                    var rs = await response.Content.ReadAsAsync<byte[]>();
                    using (var writer = new BinaryWriter(File.OpenWrite(path + "\\" + appVersion + ".zip")))
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

        private static void WriteException(Exception ex)
        {
            var st = string.Empty;
            if (File.Exists("Appdownloader.txt"))
            {
                st = File.ReadAllText("Appdownloader.txt");
            }
            File.WriteAllText("Appdownloader.txt", st + Environment.NewLine + ex.Message);
        }

        private static void WriteException(string ex)
        {
            var st = string.Empty;
            if (File.Exists("Appdownloader.txt"))
            {
                st = File.ReadAllText("Appdownloader.txt");
            }
            File.WriteAllText("Appdownloader.txt", st + Environment.NewLine + ex);
        }
    }
    public class Helper
    {
        public int UserValidity { get; internal set; }
        public string AppVersion { get; set; }
        public string InstalledAppVersion { get; set; }
        public string DownloaderVersion { get; set; }
        public string InstalledDownloaderVersion { get; set; }
    }

    public class UserDetail
    {
        public string User { get; set; }

        public string AppVersion { get; set; }
        public string DownloaderVersion { get; set; }
    }
}
