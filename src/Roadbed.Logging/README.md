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

- **MySQL / MariaDB** — primary target; `log_entries` is RANGE-partitioned monthly for 90-day retention via `DROP PARTITION`.
- **SQLite** — supported for local/dev. No native partitioning; retention is a scheduled `DELETE` against `event_time_utc`.

Other `DataConnectionStringType` values cause `InstallLogging` to throw.

## See also

- [Roadbed.Logging Architecture](/docs/architectural-design/architecture-roadbed-logging.md) — long-form design doc.
- DDL install scripts: [Assets/Tables/activity/](Assets/Tables/activity/), [Assets/Tables/activity_input/](Assets/Tables/activity_input/), [Assets/Tables/log_entries/](Assets/Tables/log_entries/).
