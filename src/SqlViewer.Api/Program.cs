using SqlViewer.Api.Configuration;
using SqlViewer.Api.Hubs;
using SqlViewer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<SqlViewerOptions>(
    builder.Configuration.GetSection(SqlViewerOptions.Section));

builder.Services.AddSignalR();

builder.Services.AddSingleton<MonitoringService>();
builder.Services.AddSingleton<IMonitoringService>(sp => sp.GetRequiredService<MonitoringService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MonitoringService>());

// CORS is only needed when the Angular dev server runs on a different port.
// In production the SPA is served by this same host, so AllowedOrigins is empty.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });
}

var app = builder.Build();

// Serve the Angular SPA from wwwroot/.
// UseDefaultFiles maps "/" → "index.html"; UseStaticFiles serves all assets.
app.UseDefaultFiles();
app.UseStaticFiles();

if (allowedOrigins.Length > 0)
    app.UseCors();

app.UseAuthorization();
app.MapControllers();
app.MapHub<SqlHub>("/hub/sql");

// Fallback to index.html for any unmatched route — enables Angular client-side routing.
app.MapFallbackToFile("index.html");

app.Run();
