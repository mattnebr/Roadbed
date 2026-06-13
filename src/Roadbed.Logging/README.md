# Roadbed.Logging

A self-contained library that persists `Microsoft.Extensions.Logging` (MEL) output to a relational database and tracks the **activities** (run instances of jobs, pipelines, ad-hoc work) that those log rows tie back to.

## OpenTelemetry-first

Logging flows through the OpenTelemetry MEL provider, and database persistence is a single batching exporter. This means adding OTLP trace/metric/log export later (Grafana, Tempo, Jaeger, etc.) is a configuration add, not a rewrite.

```
Application code
   │
   ▼  Microsoft.Extensions.Logging
OpenTelemetryLoggerProvider
   │
   ▼
BatchLogRecordProcessor
   │
   ▼
RoadbedDbLogRecordExporter   ── activity_id / trace_id / span_id captured per row
   │
   ▼
LoggingChannel               ── bounded; drop-oldest on overflow by default
   │
   ▼
LogWriterHostedService       ── batches, flushes, console fallback on DB error
   │
   ▼
log_entries                  ── MySQL partitioned monthly, or SQLite plain table
```

## Activity tracking

`LoggingActivityService` writes mutable rows to the `activity` table — one per run. The caller mints a ULID, passes it to `BeginAsync`, heartbeats during long-running steps, patches mid-run state via `UpdateAsync`, and records a terminal status with `CompleteAsync` or `FailAsync`. Lineage edges (Silver-consumes-Bronze) go into `activity_input` via `AddInputAsync`.

Roadbed.Logging **does not generate identifiers**. The consuming app owns ULID generation and any ULID NuGet dependency.

### Reaping crash-orphaned runs

A process force-killed mid-run cannot write its own terminal status, leaving the row stuck in `running`. `ReapStaleActivitiesAsync(staleAfter, reason, ct)` lets a later, living process clean up — it transitions this application's stale `running` rows (last sign of life, `COALESCE(last_heartbeat_on, started_on, created_on)`, older than `staleAfter`) to `Canceled`, records the reason in `metrics`, and returns the reaped ids. It is **strictly scoped to `LoggingOptions.Application`** (and `Environment` when set) and never touches another application's rows. `FindStaleActivitiesAsync` is the read-only dry run. The library does not schedule the sweep — the host decides when to call it (startup, scheduled job).

## What it does not do

- Does **not** depend on `Roadbed.Crud`. The log-write path is an internal custom bulk insert that stamps each row with its own `activity_id` — distinct from the CRUDALBT "B" tier which stamps every row with one uniform identifier.
- Does **not** read `IConfiguration` directly. The host registers a singleton `LoggingOptions` POCO and a singleton `ILoggingDatabaseFactory`, and `InstallLogging` resolves both from DI.
- Does **not** run schema migrations. The DDL ships in [Assets/Tables/](Assets/Tables/) as install scripts; the host installs them before startup.

## Quick start

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new LoggingOptions
{
    Schema = "ops",                                     // empty for SQLite dev
    Application = "Foo",
    Environment = builder.Environment.EnvironmentName,
});

builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

builder.Logging.AddRoadbedDbLogging();                  // OTel + exporter

builder.Services.InstallModulesInAppDomain(builder.Configuration);

using var host = builder.Build();
await host.RunAsync();
```

```csharp
// Inside a Quartz job (or any unit of work)
string activityId = Ulid.NewUlid().ToString();

using LoggingActivityScope scope = await this._activities.BeginAsync(
    new LoggingActivityBeginRequest
    {
        Id = activityId,
        ActivityType = "ingestion",
        Target = "ops.foo",
        ActivityKey = "Foo.Ingestion.FullRefresh",
    },
    cancellationToken);

try
{
    int rows = await this._loader.LoadAsync(activityId, cancellationToken);

    await this._activities.CompleteAsync(
        activityId,
        LoggingActivityStatus.Succeeded,
        recordsImpacted: rows,
        cancellationToken: cancellationToken);
}
catch (Exception ex)
{
    await this._activities.FailAsync(activityId, ex, CancellationToken.None);
    throw;
}
```

## Supported providers

This core package is **provider-neutral** — it depends only on `Roadbed.Data` (+ `.Dapper`) and carries no database client. Reference exactly one provider satellite alongside it; the satellite brings the client and the `ILoggingDataExecutor` adapter:

- **`Roadbed.Logging.MySql`** — MySQL / MariaDB (brings MySqlConnector). Primary target; `log_entries` is RANGE-partitioned monthly for 90-day retention via `DROP PARTITION`.
- **`Roadbed.Logging.Sqlite`** — SQLite for local/dev (brings Microsoft.Data.Sqlite). No native partitioning; retention is a scheduled `DELETE` against `event_time_utc`.

Wire logging — and select the provider — with a single typed call that names the satellite installer:

```csharp
builder.Logging.AddRoadbedDbLogging<InstallLoggingMySql>();   // or <InstallLoggingSqlite>
```

Naming the type compile-pins the satellite assembly, so it loads and wires deterministically even when vendored via `HintPath` — no `typeof` discard, manual `Assembly.Load`, or auto-discovery pass is required. A MySQL-only host never pulls in the SQLite native binaries, and vice-versa. There is no Postgres provider package; an unmatched connection type has no executor and fails at first query.

## See also

- [Roadbed.Logging Architecture](/docs/architectural-design/architecture-roadbed-logging.md) — long-form design doc.
- DDL install scripts: [Assets/Tables/activity/](Assets/Tables/activity/), [Assets/Tables/activity_input/](Assets/Tables/activity_input/), [Assets/Tables/log_entries/](Assets/Tables/log_entries/).
