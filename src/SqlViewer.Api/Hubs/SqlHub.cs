using Microsoft.AspNetCore.SignalR;
using SqlViewer.Api.Services;

namespace SqlViewer.Api.Hubs;

public class SqlHub(IMonitoringService monitoring) : Hub
{
    public override Task OnConnectedAsync()
    {
        monitoring.NotifyClientConnected();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        monitoring.NotifyClientDisconnected();
        return base.OnDisconnectedAsync(exception);
    }
}
