using AppInfoController.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

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
            using (var context = new AppControllerContext())
            {
                var appInfos = context.AllowedAppsAndUrls.ToList();
                bool killApps = context.AppSettings.First(x => x.Name == "stopApp").Value == "1";
                return new Helper { AllowedAppsAndUrls = appInfos, KillApps = killApps };
            }
        }

        [HttpGet]
        [Route("GetValidURLs")]
        public IEnumerable<string> GetValidURLs()
        {
            using (var context = new AppControllerContext())
            {
                return context.AllowedAppsAndUrls.Where(_ => _.Type == "URL").Select(_ => _.Name!).ToList();
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
    }

    public class ItemsWrapper
    {
        public List<string> Ids {  get; set; }
    }

    public class ItemWrapper
    {
        public long Id { get; set; }
    }

    public class KillAppsHelper
    {
        public bool KillApp { get; set; }
    }
}