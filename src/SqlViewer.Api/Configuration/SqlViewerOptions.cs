namespace SqlViewer.Api.Configuration;

public class SqlViewerOptions
{
    public const string Section = "SqlViewer";

    public string MonitoringConnectionString { get; set; } = "";
    public string MonitoredLogin { get; set; } = "";
    public string MonitoredDatabase { get; set; } = "";
    public int PollIntervalMs { get; set; } = 500;
    public string XEventSessionName { get; set; } = "SqlViewer";
    public string XEventFileDirectory { get; set; } = @"C:\temp\";
    /// <summary>
    /// Minutes of inactivity (no connected SignalR clients) before the XEvent session is stopped automatically.
    /// Set to 0 to disable.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 5;
}
