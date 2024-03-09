using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.UseAuthorization();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

const string cacheMaxAge = "604800";
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // using Microsoft.AspNetCore.Http;
        ctx.Context.Response.Headers.Append(
             "Cache-Control", $"public, max-age={cacheMaxAge}");
    }
});
app.UseEndpoints(config =>
{
    config.MapControllers();
    config.MapFallbackToController("Index", "Fallback");
});
app.Run();
