using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppInfoController.Models;

public partial class AppControllerContext : DbContext
{
    public AppControllerContext()
    {
    }

    public AppControllerContext(DbContextOptions<AppControllerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AllowedAppsAndUrl> AllowedAppsAndUrls { get; set; }

    public virtual DbSet<AppInfo> AppInfos { get; set; }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=.\\Database\\AppController.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllowedAppsAndUrl>(entity =>
        {
            entity.ToTable("AllowedAppsAndURLs");
        });

        modelBuilder.Entity<AppInfo>(entity =>
        {
            entity.ToTable("AppInfo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
