﻿using AppInfoController.Models;

namespace AppInfoController
{
    public class Helper
    {
        public List<AllowedAppsAndUrl> AllowedAppsAndUrls { get; set; }
        public bool KillApps { get; set; }
        public int UserValidity { get; internal set; }
    }
}
