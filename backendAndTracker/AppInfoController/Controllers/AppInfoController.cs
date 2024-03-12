using AppInfoController.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;

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
            lock (obj)
            {
                using (var context = new AppControllerContext())
                {
                    var appInfos = context.AppInfos.ToList();
                    return appInfos;
                }
            }
        }

        [HttpGet]
        [Route("GetApplicationSettings")]
        public Helper GetApplicationSettings()
        {
            
            lock (obj)
            {
                using (var context = new AppControllerContext())
                {
                    var appInfos = context.AllowedAppsAndUrls.ToList();
                    bool killApps = context.AppSettings.First(x => x.Name == "stopApp").Value == "1";
                    return new Helper { AllowedAppsAndUrls = appInfos, KillApps = killApps };
                }
            }
        }

        [HttpGet]
        [Route("GetApplicationSettingsByUser")]
        public Helper GetApplicationSettingsByUser(string user)
        {
            lock (obj)
            {
                using (var context = new AppControllerContext())
                {
                    var userDetail = context.LastHitByUsers.FirstOrDefault(x => x.User == user);
                    DateTime utcTime = DateTime.Now.ToUniversalTime(); // From current datetime I am retriving UTC time
                    TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Now I am Getting `IST` time From `UTC`
                    DateTime iSTTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
                    var time= iSTTime.ToString("dd/MM/yyyy HH:mm:ss");
                    var userIP = user.Substring(user.LastIndexOf('(') + 1, user.LastIndexOf(')') - user.LastIndexOf('(') - 1);
                    if (userDetail != null)
                    {
                        userDetail.Date = time;
                        userDetail.IP = userIP;
                    }
                    else
                    {
                        var lastHit = new LastHitByUser
                        {
                            User = user,
                            Date = time,
                            IP = userIP,
                        };
                        context.LastHitByUsers.Add(lastHit);
                    }
                    context.SaveChanges();
                    var appInfos = context.AllowedAppsAndUrls.ToList();
                    bool killApps = context.AppSettings.First(x => x.Name == "stopApp").Value == "1";
                    return new Helper { AllowedAppsAndUrls = appInfos, KillApps = killApps };
                }
            }
        }

        [HttpGet]
        [Route("GetLastHitByUserDetails")]
        public IEnumerable<LastHitByUser> GetLastHitByUserDetails()
        {
            lock (obj)
            {
                using (var context = new AppControllerContext())
                {
                    var users = context.LastHitByUsers.ToList();
                    return users;
                }
            }
        }

        [HttpGet]
        [Route("GetValidURLs")]
        public IEnumerable<ValidURLWithIP> GetValidURLs()
        {
            lock (obj)
            {
                using (var context = new AppControllerContext())
                {
                    return context.AllowedAppsAndUrls.Where(_ => _.Type == "URL").Select(_ => new ValidURLWithIP { Url = _.Name!, IP = _.UserIP! }).ToList();
                }
            }
        }

        [HttpPost]
        [Route("AddURLOrApp")]
        public void AddURLOrApp(AllowedAppsAndUrl appInfo)
        {
            lock (obj)
            {
                using (var db = new AppControllerContext())
                {
                    db.AllowedAppsAndUrls.Add(appInfo);
                    db.SaveChanges();
                }
            }
        }

        [HttpPost]
        [Route("AddKillAppSetting")]
        public void AddKillAppSetting(KillAppsHelper killAppsHelper)
        {
            lock (obj)
            {
                using (var db = new AppControllerContext())
                {
                    db.AppSettings.First(x => x.Name == "stopApp").Value = killAppsHelper.KillApp ? "1" : "0";
                    db.SaveChanges();
                }
            }
        }

        [HttpPost]
        public void Post(AppInfo appInfo)
        {
            lock (obj)
            {
                using (var db = new AppControllerContext())
                {
                    appInfo.Id = Guid.NewGuid().ToString();
                    appInfo.User = db.LastHitByUsers.FirstOrDefault(x => x.IP == appInfo.User)?.User ?? appInfo.User;
                    db.AppInfos.Add(appInfo);
                    db.SaveChanges();
                }
            }
        }

        [HttpDelete]
        public void Delete(ItemsWrapper itemsWrapper)
        {
            lock (obj)
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
        }

        [HttpDelete]
        [Route("DeleteURLOrApp")]
        public void DeleteURLOrApp(ItemWrapper itemWrapper)
        {
            lock (obj)
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
        }

        [HttpDelete]
        [Route("DeleteLastHitDetail")]
        public void DeleteLastHitDetail(IPWrapper item)
        {
            lock (obj)
            {
                List<string> _ = new();

                try
                {
                    using (var db = new AppControllerContext())
                    {
                        var hit = db.LastHitByUsers.First(_ => _.IP == item.IP);
                        db.LastHitByUsers.Remove(hit);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }

    public class ItemsWrapper
    {
        public List<string> Ids {  get; set; }
    }

    public class ItemWrapper
    {
        public long Id { get; set; }
    }

    public class IPWrapper
    {
        public string IP { get; set; }
    }

    public class KillAppsHelper
    {
        public bool KillApp { get; set; }
    }

    public class ValidURLWithIP
    {
        public string Url { get; set; }
        public string IP { get; set; }
    }
}