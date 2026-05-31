using Microsoft.AspNetCore.SignalR;

namespace SqlViewer.Api.Hubs;

public class SqlHub : Hub
{
    // Server pushes to clients via IHubContext<SqlHub>.
    // No client-to-server methods needed; control goes through REST endpoints.
}
