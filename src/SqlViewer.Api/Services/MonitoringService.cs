using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SqlViewer.Api.Configuration;
using SqlViewer.Api.Hubs;
using SqlViewer.Api.Models;
using SqlViewer.Api.Parsing;

namespace SqlViewer.Api.Services;

public sealed class MonitoringService : IMonitoringService, IHostedService, IAsyncDisposable
{
    private readonly SqlViewerOptions _options;
    private readonly IHubContext<SqlHub> _hub;
    private readonly ILogger<MonitoringService> _logger;
    private readonly ILogger<XEventSession> _xEventLogger;

    private SessionState _state = SessionState.Stopped;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private XEventSession? _xEventSession;
    private CancellationTokenSource? _pollCts;
    private Task _pollTask = Task.CompletedTask;
    private DateTime _lastPolledAt = DateTime.UtcNow;

    private int _connectedClients = 0;
    private CancellationTokenSource? _idleCts;
    private readonly Lock _idleLock = new();

    public SessionState State => _state;

    public MonitoringService(
        IOptions<SqlViewerOptions> options,
        IHubContext<SqlHub> hub,
        ILogger<MonitoringService> logger,
        ILogger<XEventSession> xEventLogger)
    {
        _options = options.Value;
        _hub = hub;
        _logger = logger;
        _xEventLogger = xEventLogger;
    }

    // IHostedService — nothing to do on startup; control comes via REST.
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Gracefully tear down when the host shuts down.
        await StopInternalAsync(CancellationToken.None);
    }

    public async Task StartAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_state == SessionState.Listening) return;

            if (_state == SessionState.Stopped)
            {
                _xEventSession = new XEventSession(_options, _xEventLogger);
                await _xEventSession.OpenAsync();
                await _xEventSession.CreateAsync();
                _lastPolledAt = DateTime.UtcNow;
            }
            else // Paused → Listening: skip events that arrived during the pause.
            {
                _lastPolledAt = DateTime.UtcNow;
            }

            _pollCts = new CancellationTokenSource();
            _pollTask = RunPollLoopAsync(_pollCts.Token);
            _state = SessionState.Listening;
        }
        finally
        {
            _stateLock.Release();
        }

        await BroadcastStateAsync();
    }

    public async Task PauseAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_state != SessionState.Listening) return;

            await CancelPollLoopAsync();
            _state = SessionState.Paused;
        }
        finally
        {
            _stateLock.Release();
        }

        await BroadcastStateAsync();
    }

    public async Task StopAsync()
    {
        await StopInternalAsync(CancellationToken.None);
        await BroadcastStateAsync();
    }

    private async Task StopInternalAsync(CancellationToken ct)
    {
        await _stateLock.WaitAsync(ct);
        try
        {
            if (_state == SessionState.Stopped) return;

            await CancelPollLoopAsync();

            if (_xEventSession is not null)
            {
                await _xEventSession.DropAsync();
                await _xEventSession.DisposeAsync();
                _xEventSession = null;
            }

            _state = SessionState.Stopped;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private static readonly int[] ReconnectDelaysSeconds = [2, 4, 8, 16, 30];

    private async Task RunPollLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.PollIntervalMs));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
                await PollOnceAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "SQL connection lost.");
                await ReconnectLoopAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during XEvent poll. Continuing.");
            }
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        // Write _state without the lock — safe because CancelPollLoopAsync awaits this
        // task before acquiring _stateLock, so no concurrent writer is possible here.
        _state = SessionState.Reconnecting;
        await BroadcastStateAsync();

        for (int attempt = 0; !ct.IsCancellationRequested; attempt++)
        {
            var delaySec = ReconnectDelaysSeconds[Math.Min(attempt, ReconnectDelaysSeconds.Length - 1)];
            _logger.LogInformation("Reconnect attempt {Attempt} in {Delay}s...", attempt + 1, delaySec);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);

                if (_xEventSession is not null)
                    await _xEventSession.ReconnectAsync(ct);

                _state = SessionState.Listening;
                await BroadcastStateAsync();
                _logger.LogInformation("Reconnected to SQL Server successfully.");
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnect attempt {Attempt} failed.", attempt + 1);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        if (_xEventSession is null) return;

        var events = await _xEventSession.ReadNewEventsAsync(_lastPolledAt, ct);

        foreach (var ev in events)
        {
            if (ev.TimestampUtc > _lastPolledAt)
                _lastPolledAt = ev.TimestampUtc;

            var parsed = SqlStatementParser.TryParse(ev.Statement);
            if (parsed is null) continue;

            var commandEvent = new SqlCommandEvent(
                TraceId: parsed.TraceId,
                SpanId: parsed.SpanId,
                Url: parsed.Url,
                MethodName: parsed.MethodName,
                CommandType: parsed.CommandType,
                FirstTable: parsed.FirstTable,
                DurationUs: ev.DurationUs,
                RowCount: ev.RowCount,
                SqlText: ev.Statement,
                CapturedAt: new DateTimeOffset(ev.TimestampUtc, TimeSpan.Zero)
            );

            await _hub.Clients.All.SendAsync("ReceiveCommand", commandEvent, ct);
        }
    }

    private async Task CancelPollLoopAsync()
    {
        if (_pollCts is not null)
        {
            await _pollCts.CancelAsync();
            await _pollTask;
            _pollCts.Dispose();
            _pollCts = null;
        }
    }

    public void NotifyClientConnected()
    {
        lock (_idleLock)
        {
            _connectedClients++;
            if (_connectedClients == 1)
                CancelIdleTimer();
        }
    }

    public void NotifyClientDisconnected()
    {
        lock (_idleLock)
        {
            if (_connectedClients > 0) _connectedClients--;
            if (_connectedClients == 0)
                ScheduleIdleStop();
        }
    }

    private void CancelIdleTimer()
    {
        _idleCts?.Cancel();
        _idleCts?.Dispose();
        _idleCts = null;
    }

    private void ScheduleIdleStop()
    {
        if (_options.IdleTimeoutMinutes <= 0) return;
        CancelIdleTimer();
        _idleCts = new CancellationTokenSource();
        _ = RunIdleTimerAsync(_idleCts.Token);
    }

    private async Task RunIdleTimerAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(_options.IdleTimeoutMinutes), ct);
            _logger.LogInformation(
                "No active connections for {Minutes} min. Stopping XEvent session automatically.",
                _options.IdleTimeoutMinutes);
            await StopInternalAsync(CancellationToken.None);
            await BroadcastStateAsync();
        }
        catch (OperationCanceledException) { }
    }

    private Task BroadcastStateAsync() =>
        _hub.Clients.All.SendAsync("SessionStateChanged", _state.ToString());

    public async ValueTask DisposeAsync()
    {
        _stateLock.Dispose();
        if (_xEventSession is not null)
            await _xEventSession.DisposeAsync();
    }
}
