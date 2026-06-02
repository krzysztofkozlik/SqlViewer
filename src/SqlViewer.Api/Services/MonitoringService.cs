using Microsoft.AspNetCore.SignalR;
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

    public async Task PlayAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_state == SessionState.Playing) return;

            if (_state == SessionState.Stopped)
            {
                _xEventSession = new XEventSession(_options, _xEventLogger);
                await _xEventSession.OpenAsync();
                await _xEventSession.CreateAsync();
                _lastPolledAt = DateTime.UtcNow;
            }
            else // Paused → Playing: skip events that arrived during the pause.
            {
                _lastPolledAt = DateTime.UtcNow;
            }

            _pollCts = new CancellationTokenSource();
            _pollTask = RunPollLoopAsync(_pollCts.Token);
            _state = SessionState.Playing;
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
            if (_state != SessionState.Playing) return;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during XEvent poll. Continuing.");
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        if (_xEventSession is null) return;

        var events = await _xEventSession.ReadNewEventsAsync(_lastPolledAt, ct);

        foreach (var ev in events)
        {
            // Advance the watermark so we never reprocess the same event.
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

    private Task BroadcastStateAsync() =>
        _hub.Clients.All.SendAsync("SessionStateChanged", _state.ToString());

    public async ValueTask DisposeAsync()
    {
        _stateLock.Dispose();
        if (_xEventSession is not null)
            await _xEventSession.DisposeAsync();
    }
}
