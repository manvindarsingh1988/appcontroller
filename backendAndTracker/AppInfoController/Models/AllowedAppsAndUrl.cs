using System;
using System.Collections.Generic;

namespace AppInfoController.Models;

public partial class AllowedAppsAndUrl
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? UserIP { get; set; }
}
