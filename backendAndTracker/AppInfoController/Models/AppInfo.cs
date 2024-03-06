using System;
using System.Collections.Generic;

namespace AppInfoController.Models;

public partial class AppInfo
{
    public string Id { get; set; } = null!;

    public string? Date { get; set; }

    public string? User { get; set; }

    public string? AppName { get; set; }

    public string? Summary { get; set; }
}
