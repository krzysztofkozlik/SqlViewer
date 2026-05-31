using Microsoft.Data.SqlClient;
using SqlViewer.Api.Configuration;

namespace SqlViewer.Api.Services;

public sealed class XEventSession : IAsyncDisposable
{
    private readonly SqlViewerOptions _options;
    private readonly ILogger<XEventSession> _logger;
    private SqlConnection? _connection;

    public record RawEvent(DateTime TimestampUtc, string Statement, long DurationUs, long RowCount);

    public XEventSession(SqlViewerOptions options, ILogger<XEventSession> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task OpenAsync(CancellationToken ct = default)
    {
        _connection = new SqlConnection(_options.MonitoringConnectionString);
        await _connection.OpenAsync(ct);
    }

    /// <summary>
    /// Drops any existing session with the configured name, then creates and starts a new one.
    /// </summary>
    public async Task CreateAsync(CancellationToken ct = default)
    {
        if (_connection is null)
            throw new InvalidOperationException("Call OpenAsync before CreateAsync.");

        var sessionName = _options.XEventSessionName;

        // Sanitise: session name comes from config, not user input, but guard against
        // accidental bracket-breaking characters to keep the SQL valid.
        if (sessionName.Contains(']') || sessionName.Contains('\''))
            throw new ArgumentException("XEventSessionName must not contain ] or ' characters.");

        var dropSql = $"""
            IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'{sessionName}')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.dm_xe_sessions WHERE name = N'{sessionName}')
                    ALTER EVENT SESSION [{sessionName}] ON SERVER STATE = STOP;
                DROP EVENT SESSION [{sessionName}] ON SERVER;
            END;
            """;

        var createSql = $"""
            CREATE EVENT SESSION [{sessionName}] ON SERVER
            ADD EVENT sqlserver.sql_statement_completed(
                WHERE (
                    sqlserver.database_name = N'{_options.MonitoredDatabase}'
                    AND sqlserver.server_principal_name = N'{_options.MonitoredLogin}'
                )
            )
            ADD TARGET package0.ring_buffer(SET max_memory = 51200)
            WITH (MAX_DISPATCH_LATENCY = 1 SECONDS);

            ALTER EVENT SESSION [{sessionName}] ON SERVER STATE = START;
            """;

        await ExecuteNonQueryAsync(dropSql, ct);
        await ExecuteNonQueryAsync(createSql, ct);

        _logger.LogInformation(
            "XEvent session '{SessionName}' created for login '{Login}' on database '{Database}'.",
            sessionName, _options.MonitoredLogin, _options.MonitoredDatabase);
    }

    /// <summary>
    /// Reads events from the ring buffer that occurred after <paramref name="since"/> (UTC).
    /// Returns them ordered by timestamp ascending.
    /// </summary>
    public async Task<IReadOnlyList<RawEvent>> ReadNewEventsAsync(DateTime since, CancellationToken ct = default)
    {
        if (_connection is null)
            return [];

        var sessionName = _options.XEventSessionName;

        const string sql = """
            SELECT
                event_data.value('(event/@timestamp)[1]',                                  'datetime2(7)') AS [Timestamp],
                event_data.value('(event/data[@name="statement"]/value)[1]',               'nvarchar(max)') AS [Statement],
                event_data.value('(event/data[@name="duration"]/value)[1]',                'bigint')        AS [DurationUs],
                event_data.value('(event/data[@name="row_count"]/value)[1]',               'bigint')        AS [RowCount]
            FROM (
                SELECT CAST(target_data AS XML) AS ring_data
                FROM sys.dm_xe_session_targets  t
                INNER JOIN sys.dm_xe_sessions   s ON t.event_session_address = s.address
                WHERE s.name = @SessionName AND t.target_name = 'ring_buffer'
            ) AS data
            CROSS APPLY ring_data.nodes('RingBufferTarget/event') AS events(event_data)
            WHERE event_data.value('(event/@timestamp)[1]', 'datetime2(7)') > @Since
            ORDER BY [Timestamp];
            """;

        await using var cmd = new SqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@SessionName", sessionName);
        cmd.Parameters.AddWithValue("@Since", since);

        var results = new List<RawEvent>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var timestamp = reader.GetDateTime(0);
            var statement = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var durationUs = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);
            var rowCount = reader.IsDBNull(3) ? 0L : reader.GetInt64(3);

            if (!string.IsNullOrWhiteSpace(statement))
                results.Add(new RawEvent(timestamp, statement, durationUs, rowCount));
        }

        return results;
    }

    /// <summary>
    /// Stops and drops the XEvent session. Safe to call even if the session no longer exists.
    /// </summary>
    public async Task DropAsync(CancellationToken ct = default)
    {
        if (_connection is null) return;

        var sessionName = _options.XEventSessionName;
        var sql = $"""
            IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'{sessionName}')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.dm_xe_sessions WHERE name = N'{sessionName}')
                    ALTER EVENT SESSION [{sessionName}] ON SERVER STATE = STOP;
                DROP EVENT SESSION [{sessionName}] ON SERVER;
            END;
            """;

        await ExecuteNonQueryAsync(sql, ct);
        _logger.LogInformation("XEvent session '{SessionName}' dropped.", sessionName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, _connection);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
