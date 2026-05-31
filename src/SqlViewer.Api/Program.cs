using SqlViewer.Api.Configuration;
using SqlViewer.Api.Hubs;
using SqlViewer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<SqlViewerOptions>(
    builder.Configuration.GetSection(SqlViewerOptions.Section));

builder.Services.AddSignalR();

// MonitoringService is both the IMonitoringService used by controllers and the IHostedService.
// Register as singleton so the same instance is shared across both registrations.
builder.Services.AddSingleton<MonitoringService>();
builder.Services.AddSingleton<IMonitoringService>(sp => sp.GetRequiredService<MonitoringService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MonitoringService>());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? [];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // required for SignalR WebSocket
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<SqlHub>("/hub/sql");

app.Run();
