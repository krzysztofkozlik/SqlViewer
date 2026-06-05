using Microsoft.AspNetCore.SignalR;
using SqlViewer.Api.Services;

namespace SqlViewer.Api.Hubs;

public class SqlHub : Hub
{
    private readonly IMonitoringService _monitoring;

    public SqlHub(IMonitoringService monitoring)
    {
        _monitoring = monitoring;
    }

    public override Task OnConnectedAsync()
    {
        _monitoring.NotifyClientConnected();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _monitoring.NotifyClientDisconnected();
        return base.OnDisconnectedAsync(exception);
    }
}
