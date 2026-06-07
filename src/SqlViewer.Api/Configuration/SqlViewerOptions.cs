namespace SqlViewer.Api.Configuration;

public class SqlViewerOptions
{
    public const string Section = "SqlViewer";

    /// <summary>Connection string for the account used to create and read XEvent sessions. Requires ALTER ANY EVENT SESSION and VIEW SERVER STATE.</summary>
    public string MonitoringConnectionString { get; set; } = "";

    /// <summary>SQL Server login name whose queries are captured (server_principal_name filter on the XEvent session).</summary>
    public string MonitoredLogin { get; set; } = "";

    /// <summary>Database whose queries are captured (database_name filter on the XEvent session).</summary>
    public string MonitoredDatabase { get; set; } = "";

    /// <summary>How often the backend polls the XEvent file for new events, in milliseconds.</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>Name of the XEvent session created on the server. Also used as the base name of the .xel output file.</summary>
    public string XEventSessionName { get; set; } = "SqlViewer";

    /// <summary>Directory where XEvent .xel files are written. Must be accessible by the SQL Server service account.</summary>
    public string XEventFileDirectory { get; set; } = @"C:\temp\";

    /// <summary>Maximum size in MB of each XEvent rollover file.</summary>
    public int XEventMaxFileSizeMb { get; set; } = 50;

    /// <summary>Maximum number of XEvent rollover files to keep.</summary>
    public int XEventMaxRolloverFiles { get; set; } = 5;

    /// <summary>How often (in seconds) the XEvent session flushes buffered events to the file.</summary>
    public int XEventDispatchLatencySeconds { get; set; } = 1;

    /// <summary>
    /// Minutes of inactivity (no connected SignalR clients) before the XEvent session is stopped automatically.
    /// Set to 0 to disable.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 5;
}
