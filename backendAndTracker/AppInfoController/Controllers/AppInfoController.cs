using AppInfoController.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace AppInfoController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppInfoController : ControllerBase
    {
        private static object obj = new();

        [HttpGet]
        public IEnumerable<AppInfo> Get()
        {
            using (var context = new AppControllerContext())
            {
                var appInfos = context.AppInfos.ToList();
                return appInfos;
            }
        }

        [HttpGet]
        [Route("GetApplicationSettings")]
        public Helper GetApplicationSettings()
        {

            using (var context = new AppControllerContext())
            {
                var appInfos = context.AllowedAppsAndUrls.ToList();
                bool killApps = context.AppSettings.First(x => x.Name == "stopApp").Value == "1";
                var userValidity = int.Parse(context.AppSettings.First(x => x.Name == "UserValidity")?.Value ?? "10");
                var appVersion = context.AppSettings.First(x => x.Name == "AppVersion")?.Value!;
                var downloaderVersion = context.AppSettings.FirstOrDefault(x => x.Name == "DownloaderVersion")?.Value!;
                return new Helper { AllowedAppsAndUrls = appInfos, KillApps = killApps, UserValidity = userValidity, AppVersion = appVersion, DownloaderVersion = downloaderVersion };
            }
        }

        [HttpGet]
        [Route("GetConnectedUsers")]
        public IEnumerable<MyUserType> GetConnectedUsers()
        {
            return RecordingHub.MyUsers.Select(_ => _.Value);
        }

        [HttpGet]
        [Route("GetAdmins")]
        public IEnumerable<MyUserType> GetAdmins()
        {
            return RecordingHub.AdminUsers.Select(_ => _.Value);
        }
        [HttpGet]
        [Route("GetStreamingUsers")]
        public IEnumerable<String> GetStreamingUsers()
        {
            var list = new List<string>();
            foreach (var item in RecordingHub.ConnectedStreamings)
            {
                list.Add($"{item.Key.ConnectionId}/{item.Key.Id}-{item.Value.ConnectionId}/{item.Value.Id}");
            }
            return list;
        }

        [HttpGet]
        [Route("GetApplicationSettingsByUser")]
        public Helper GetApplicationSettingsByUser(string user)
        {
            using (var context = new AppControllerContext())
            {
                var userDetail = context.LastHitByUsers.FirstOrDefault(x => x.User == user);
                DateTime utcTime = DateTime.Now.ToUniversalTime(); // From current datetime I am retriving UTC time
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Now I am Getting `IST` time From `UTC`
                DateTime iSTTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
                var time = iSTTime.ToString("dd/MM/yyyy HH:mm:ss");
                if (userDetail != null)
                {
                    userDetail.Date = time;
                }
                else
                {
                    var lastHit = new LastHitByUser
                    {
                        User = user,
                        Date = time
                    };
                    context.LastHitByUsers.Add(lastHit);
                }
                context.SaveChanges();
                var appInfos = context.AllowedAppsAndUrls.Where(_ => string.IsNullOrEmpty(_.User) || _.User == user).ToList();
                bool killApps = context.AppSettings.First(x => x.Name == "stopApp").Value == "1";
                var appVersion = context.AppSettings.First(x => x.Name == "AppVersion")?.Value!;
                var downloaderVersion = context.AppSettings.First(x => x.Name == "DownloaderVersion")?.Value!;

                return new Helper { AllowedAppsAndUrls = appInfos, KillApps = killApps, AppVersion = appVersion, InstalledAppVersion = userDetail?.AppVersion!, DownloaderVersion = downloaderVersion, InstalledDownloaderVersion = userDetail?.DownloaderVersion! };
            }
        }

        [HttpGet]
        [Route("GetLastHitByUserDetails")]
        public IEnumerable<LastHitByUser> GetLastHitByUserDetails()
        {
            DateTime utcTime = DateTime.Now.ToUniversalTime(); // From current datetime I am retriving UTC time
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Now I am Getting `IST` time From `UTC`
            DateTime iSTTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
            using (var context = new AppControllerContext())
            {
                var userValidity = int.Parse(context.AppSettings.First(x => x.Name == "UserValidity")?.Value ?? "10");
                var users = context.LastHitByUsers.ToList();
                users.ForEach(_ =>
                {
                    var d = _.Date?.Split(' ');
                    var da = d[0].Split(new char[2] { '/', '-' });
                    var y = Convert.ToInt32(da.Last());
                    var m = Convert.ToInt32(da[1]);
                    var day = Convert.ToInt32(da[0]);
                    var da1 = d[1].Split(":");
                    var h = Convert.ToInt32(da1[0]);
                    var mi = Convert.ToInt32(da1[1]);
                    var s = Convert.ToInt32(da1[2]);
                    var date = new DateTime(y, m, day, h, mi, s);
                    _.Inactive = (iSTTime - date).TotalMinutes > userValidity;
                });
                return users;
            }
        }

        [HttpGet]
        [Route("GetValidURLs")]
        public ValidData GetValidURLs(string user)
        {
            using (var context = new AppControllerContext())
            {
                var validData = new ValidData();
                var userDetail = context.LastHitByUsers.FirstOrDefault(x => x.User == user);
                if (userDetail != null)
                {
                    validData.Ids = userDetail.AllowedUserId;
                }

                validData.URLs = context.AllowedAppsAndUrls
                    .Where(_ => _.Type == "URL" && (string.IsNullOrEmpty(_.User) || _.User == user))
                    .Select(_ => new ValidURL { Url = _.Name! }).ToList();
                return validData;
            }
        }

        [HttpPost]
        [Route("AddURLOrApp")]
        public void AddURLOrApp(AllowedAppsAndUrl appInfo)
        {
            using (var db = new AppControllerContext())
            {
                db.AllowedAppsAndUrls.Add(appInfo);
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Route("AddKillAppSetting")]
        public void AddKillAppSetting(KillAppsHelper killAppsHelper)
        {
            using (var db = new AppControllerContext())
            {
                db.AppSettings.First(x => x.Name == "stopApp").Value = killAppsHelper.KillApp ? "1" : "0";
                db.SaveChanges();
            }
        }


        [HttpPost]
        [Route("UpdateValidity")]
        public void UpdateValidity(UpdateUserValidity validity)
        {
            using (var db = new AppControllerContext())
            {
                db.AppSettings.First(x => x.Name == "UserValidity").Value = validity.Validity.ToString();
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Route("UpdateLatestAppVersion")]
        public void UpdateLatestAppVersion(LatestAppVersion latestAppVersion)
        {
            using (var db = new AppControllerContext())
            {
                db.AppSettings.First(x => x.Name == "AppVersion").Value = latestAppVersion.AppVersion;
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Route("UpdateLatestDownloaderVersion")]
        public void UpdateLatestDownloaderVersion(LatestAppVersion latestAppVersion)
        {
            using (var db = new AppControllerContext())
            {
                var downloaderVersion = db.AppSettings.FirstOrDefault(x => x.Name == "DownloaderVersion");
                if (downloaderVersion != null)
                {
                    downloaderVersion.Value = latestAppVersion.AppVersion;
                }
                else
                {
                    db.AppSettings.Add(new AppSetting { Name = "DownloaderVersion", Value = latestAppVersion.AppVersion });
                }
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Route("UpdateUserDetail")]
        public void UpdateUserDetail(LastHitByUser user)
        {
            using (var db = new AppControllerContext())
            {
                var userDetail = db.LastHitByUsers.FirstOrDefault(x => x.User == user.User);
                if (userDetail != null)
                {
                    userDetail.Name = user.Name;
                    userDetail.MobileNo = user.MobileNo;
                    userDetail.City = user.City;
                    userDetail.Address = user.Address;
                    userDetail.AllowedUserId = user.AllowedUserId;
                    db.SaveChanges();
                }
            }
        }

        [HttpPost]
        [Route("UpdateAppVersion")]
        public void UpdateAppVersion(LastHitByUser user)
        {
            using (var db = new AppControllerContext())
            {
                var userDetail = db.LastHitByUsers.FirstOrDefault(x => x.User == user.User);
                DateTime utcTime = DateTime.Now.ToUniversalTime(); // From current datetime I am retriving UTC time
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Now I am Getting `IST` time From `UTC`
                DateTime iSTTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
                var time = iSTTime.ToString("dd/MM/yyyy HH:mm:ss");
                if (userDetail != null)
                {
                    userDetail.Date = time;
                    userDetail.AppVersion = user.AppVersion;
                    userDetail.DownloaderVersion = user.DownloaderVersion;
                }
                else
                {
                    var lastHit = new LastHitByUser
                    {
                        User = user.User,
                        Date = time,
                        AppVersion = user.AppVersion,
                        DownloaderVersion = user.DownloaderVersion
                    };
                    db.LastHitByUsers.Add(lastHit);
                }
                db.SaveChanges();
                //var t = Task.Run(async () =>
                //{
                //    await PostData(user);
                //});
                //t.Wait();
            }
        }

        //private static async Task PostData(LastHitByUser appInfo)
        //{
        //    try
        //    {
        //        var handler = new HttpClientHandler
        //        {
        //            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        //        };
        //        var client = new HttpClient(handler);

        //        // Set the base address to simplify maintenance & requests
        //        client.BaseAddress = new Uri("https://ac.saralesuvidha.com/");

        //        // Serialize class into JSON
        //        var payload = JsonConvert.SerializeObject(appInfo);

        //        // Wrap our JSON inside a StringContent object
        //        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        //        // Post to the endpoint
        //        var response = await client.PostAsync("/appinfo/UpdateAppVersion", content);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}


        [HttpPost]
        public void Post(AppInfo appInfo)
        {
            if(appInfo.Summary != null && appInfo.Summary.Contains("being killed"))
            {
                return;
            }
            using (var db = new AppControllerContext())
            {
                var userInfo = db.LastHitByUsers.FirstOrDefault(_ => _.User == appInfo.User);
                if (userInfo != null)
                {
                    var st = new StringBuilder();
                    if (!string.IsNullOrEmpty(userInfo.Name))
                    {
                        st.Append("Name: " + userInfo.Name + " ");
                    }
                    if (!string.IsNullOrEmpty(userInfo.City))
                    {
                        st.Append("City: " + userInfo.City + " ");
                    }
                    if (!string.IsNullOrEmpty(userInfo.MobileNo))
                    {
                        st.Append("Mobile: " + userInfo.MobileNo + " ");
                    }
                    if (!string.IsNullOrEmpty(userInfo.Address))
                    {
                        st.Append("Address: " + userInfo.Address + " ");
                    }
                    if (!string.IsNullOrEmpty(st.ToString()))
                    {
                        appInfo.User = $"{appInfo.User} ({st.ToString().Trim()})";
                    }
                }
                appInfo.Id = Guid.NewGuid().ToString();
                DateTime utcTime = DateTime.Now.ToUniversalTime(); // From current datetime I am retriving UTC time
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Now I am Getting `IST` time From `UTC`
                DateTime iSTTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
                var time = iSTTime.ToString("dd/MM/yyyy HH:mm:ss");
                appInfo.Date = time;
                db.AppInfos.Add(appInfo);
                db.SaveChanges();
            }
        }

        [HttpPost]
        [Route("DeleteDetails")]
        public void DeleteDetails(ItemsWrapper itemsWrapper)
        {
            List<string> _ = new();

            try
            {
                using (var db = new AppControllerContext())
                {
                    IQueryable<AppInfo> items = db.AppInfos.Where(_ => itemsWrapper.Ids.Contains(_.Id));
                    if (items == null) return;
                    db.AppInfos.RemoveRange(items);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
            
        }

        [HttpPost]
        [Route("DeleteURLOrApp")]
        public void DeleteURLOrApp(ItemWrapper itemWrapper)
        {
            List<string> _ = new();

            try
            {
                using (var db = new AppControllerContext())
                {
                    var item = db.AllowedAppsAndUrls.First(_ => _.Id == itemWrapper.Id);
                    db.AllowedAppsAndUrls.Remove(item);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }

        [HttpPost]
        [Route("DeleteLastHitDetail")]
        public void DeleteLastHitDetail(UserWrapper item)
        {
            List<string> _ = new();

            try
            {
                using (var db = new AppControllerContext())
                {
                    var hit = db.LastHitByUsers.First(_ => _.User == item.User);
                    db.LastHitByUsers.Remove(hit);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }

        [HttpPost]
        [Route("ValidateUser")]
        public string ValidateUser(Login login)
        {
            List<string> _ = new();

            try
            {
                using (var db = new AppControllerContext())
                {
                    var user = db.AppSettings.First(x => x.Name == "UserName")?.Value;
                    var password = db.AppSettings.First(x => x.Name == "Password")?.Value;
                    if (user!.Equals(login.UserName, StringComparison.InvariantCultureIgnoreCase)
                        && password! == login.Password)
                    {
                        Guid g = Guid.NewGuid();
                        string guidString = Convert.ToBase64String(g.ToByteArray());
                        guidString = guidString.Replace("=", "");
                        guidString = guidString.Replace("+", "");
                        return guidString;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return "";
        }

        [HttpPost]
        [Route("SetLatLong")]
        public void SetLatLong(LatLongInfo latLongInfo)
        {
            try
            {
                using (var db = new AppControllerContext())
                {
                    var userInfo = db.LastHitByUsers.FirstOrDefault(_ => _.User == latLongInfo.User);

                    userInfo!.Summary = latLongInfo.Summary;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }

        [HttpGet]
        [Route("GetDownloaderZip")]
        public byte[] GetDownloaderZip()
        {
            byte[] result;
            using (MemoryStream tmpMemory = new MemoryStream(System.IO.File.ReadAllBytes("AppZip//Downloader.zip")))
            {
                result = tmpMemory.ToArray();
            };
            return result;
        }

        [HttpGet]
        [Route("GetZip")]
        public byte[] GetZip()
        {
            byte[] result;
            using (MemoryStream tmpMemory = new MemoryStream(System.IO.File.ReadAllBytes("AppZip//AppController.zip")))
            {
                result = tmpMemory.ToArray();
            };
            return result;
        }
    }

    public class Login
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ItemsWrapper
    {
        public List<string> Ids {  get; set; }
    }

    public class ItemWrapper
    {
        public long Id { get; set; }
    }

    public class UserWrapper
    {
        public string User { get; set; }
    }

    public class KillAppsHelper
    {
        public bool KillApp { get; set; }
    }

    public class UpdateUserValidity
    {
        public int Validity { get; set; }
    }

    public class LatestAppVersion
    {
        public string AppVersion { get; set; }
    }

    public class ValidURL
    {
        public string Url { get; set; }
    }

    public class ValidData
    {
        public List<ValidURL> URLs { get; set; }

        public string? Ids { get; set; }
    }

    public class LatLongInfo
    {
        public string? User { get; set; }
        public string? Summary { get; set; }
    }
}