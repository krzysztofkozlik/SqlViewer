# SQL Viewer

A real-time SQL monitoring tool for local development. Connects to a SQL Server instance and displays SQL commands as they execute, grouped by the HTTP request or background job that triggered them.

---

## Core Monitoring

- Captures `rpc_completed` and `sql_batch_completed` XEvents filtered by a configured SQL login and database
- Reads events from a file-based XEvent target (`asynchronous_file_target`) polled every 500 ms
- Groups SQL commands by span ID (HTTP request) or operation ID
- Extracts command type (SELECT / INSERT / UPDATE / DELETE / MERGE), first table name, duration, and row count from each captured statement
- XEvent session is automatically cleaned up on application stop and on idle timeout (configurable; triggers when no browser tab is connected)

## Real-time Frontend

- Angular 21 single-page app served directly by the .NET backend — no separate web server required
- Live updates via SignalR with automatic reconnect; toolbar shows **Connected / Reconnecting / Disconnected** state
- Session control: **Start**, **Pause** (keeps session open), and **Stop** buttons

## Request List

- Requests grouped by span / operation ID, sorted newest-first
- Each row shows: URL or assembly name, query count, total SQL time, and timestamp
- Expandable to show individual SQL command rows with command type, table, duration, row count, and method name
- Lazy panel rendering — SQL is only formatted and highlighted when a row is first expanded

## SQL Display

- Formatted T-SQL via **sql-formatter** for clean indentation
- Syntax highlighting via **highlight.js** with VS Code token colours
- Toggle between **Highlighted** and **Raw SQL** views
- One-click **Copy to clipboard** (copies the active view's text)

## Visual Indicators

| Icon | Meaning |
|------|---------|
| ⏱ | Request's total SQL time exceeds the slow-request threshold |
| ⚠ | Request contains at least one long-running individual query |
| 🚫 | Request contains an empty query (EF determined no results were possible) |

All three icons appear on both the request row header and the individual SQL command row.

## Filtering

- **URL / assembly filter** — live text search with 300 ms debounce; clears instantly
- **Long-running queries** — show only requests containing a slow individual query
- **Slow requests** — show only requests whose total SQL time exceeds the threshold
- **Empty queries** — show only requests containing EF no-result short-circuit queries
- **Clear all filters** button

## Settings
Persisted in `localStorage`

- **Dark / Light mode** — VS Code Dark+ and Light+ inspired colour palettes
- **Long-running query threshold** (default 400 ms)
- **Slow request threshold** (default 800 ms)
- **Max displayed requests** — oldest requests are dropped when the limit is reached (default 100)

## Configuration Reference

All settings live under the `"SqlViewer"` key in `appsettings.json`.

---

### Connection & Target

| Setting | Default | Description |
|---|---|---|
| `MonitoringConnectionString` | _(required)_ | Connection string for the account used to create and query XEvent sessions. The account needs **`ALTER ANY EVENT SESSION`** and **`VIEW SERVER STATE`** at the server level. |
| `MonitoredLogin` | _(required)_ | SQL Server login name whose queries are captured. Maps to the `server_principal_name` filter on the XEvent session. |
| `MonitoredDatabase` | _(required)_ | Database whose queries are captured. Maps to the `database_name` filter on the XEvent session. |

---

### XEvent Session

| Setting | Default | Description |
|---|---|---|
| `XEventSessionName` | `"SqlViewer"` | Name of the XEvent session created on the server. Also used as the base name of the `.xel` output file. |
| `XEventFileDirectory` | `"C:\temp\"` | Directory where XEvent `.xel` files are written. Must be accessible by the **SQL Server service account** (not the monitoring account). |
| `XEventMaxFileSizeMb` | `50` | Maximum size in MB of each XEvent rollover file. |
| `XEventMaxRolloverFiles` | `5` | Maximum number of rollover files to keep. Oldest files are deleted automatically when the limit is reached. |
| `XEventDispatchLatencySeconds` | `1` | How often (in seconds) the XEvent session flushes buffered events to disk. Lower values reduce latency; higher values reduce I/O. |

---

### Polling

| Setting | Default | Description |
|---|---|---|
| `PollIntervalMs` | `500` | How often the backend checks the XEvent file for new events, in milliseconds. |

---

### Idle Timeout

| Setting | Default | Description |
|---|---|---|
| `IdleTimeoutMinutes` | `5` | Minutes without any connected browser before the XEvent session is stopped automatically. Set to `0` to disable. |

---

### Minimal example

```json
{
  "SqlViewer": {
    "MonitoringConnectionString": "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;",
    "MonitoredLogin": "my_app_login",
    "MonitoredDatabase": "MyDatabase"
  }
}
```

## Packaging

A `build-release.ps1` script builds the Angular frontend, embeds it as static files inside the .NET publish output, and produces a single `SqlViewer.Api.exe` + `appsettings.json` — no runtime installation required beyond .NET 9 (or self-contained with `-SelfContained`).
