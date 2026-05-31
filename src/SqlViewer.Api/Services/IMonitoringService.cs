using SqlViewer.Api.Models;

namespace SqlViewer.Api.Services;

public interface IMonitoringService
{
    SessionState State { get; }
    Task PlayAsync();
    Task PauseAsync();
    Task StopAsync();
}
