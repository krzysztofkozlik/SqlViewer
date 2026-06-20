using SqlViewer.Api.Models;

namespace SqlViewer.Api.Services;

public interface IMonitoringService
{
    SessionState State { get; }
    string MonitoredDatabase { get; }
    string MonitoredLogin { get; }
    Task StartAsync();
    Task PauseAsync();
    Task StopAsync();
    void NotifyClientConnected();
    void NotifyClientDisconnected();
}
