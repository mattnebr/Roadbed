# Roadbed.Logging Reference

A self-contained library that persists structured `Microsoft.Extensions.Logging`
output to a relational database and tracks the **activities** (run instances of
jobs, pipelines, ad-hoc work) that those log rows tie back to. OpenTelemetry-
first: MEL flows through the OTel logging pipeline, and database persistence is
one exporter — adding OTLP export (Grafana / Tempo / Jaeger / etc.) later is a
configuration add, not a rewrite.

Activity rows are mutable; log entries are append-only and flushed off the
hot path by a bounded channel + background writer. The library has **no
dependency on `Roadbed.Crud`** — its bulk-insert path is internal, custom, and
stamps each row with its own originating `activity_id` rather than a uniform
activity id like the CRUDALBT "B" tier.

## Type catalog (17 types)

| Group                       | Types                                                                                                  |
| --------------------------- | ------------------------------------------------------------------------------------------------------ |
| Entities                    | `LoggingActivity`, `LoggingActivityInput`, `LoggingLogEntry`                                            |
| Enums                       | `LoggingActivityStatus`, `LoggingActivityType`, `LoggingChannelFullPolicy`                              |
| Public DTOs                 | `LoggingOptions`, `LoggingActivityBeginRequest`, `LoggingActivityUpdateRequest`                         |
| Marker interface            | `ILoggingDatabaseFactory` (extends `IDataConnectionFactory`)                                            |
| Service                     | `LoggingActivityService` (public sealed; dual ctor), `LoggingActivityScope` (IDisposable)               |
| OTel pipeline               | `RoadbedDbLogRecordExporter`, `LogWriterHostedService`, `LoggingChannel`                                |
| Wiring                      | `InstallLogging` (`IServiceCollectionInstaller`), `LoggingBuilderExtensions.AddRoadbedDbLogging`        |

## MUST

- **MUST** register a singleton `LoggingOptions` and a singleton `ILoggingDatabaseFactory` in DI **before** calling `services.InstallModulesInAppDomain(configuration)`. The installer resolves both up-front and throws if either is missing.
- **MUST** call `builder.Logging.AddRoadbedDbLogging()` on the host's `ILoggingBuilder` to wire the OpenTelemetry MEL provider, the batch processor, and the database exporter. The `IServiceCollectionInstaller` does **not** see `ILoggingBuilder`, so this step lives outside `InstallLogging`.
- **MUST** generate the activity ULID in the consuming application — Roadbed.Logging does **not** generate identifiers. Pass the same ULID you used for `IAsyncBulkInsertOperation.BulkInsertAsync` calls during the run.
- **MUST** set `LoggingOptions.Application` (and ideally `Environment`) so every row carries identifying provenance. The exporter stamps these onto every `LoggingLogEntry`.
- **MUST** set `LoggingOptions.Schema` to the MySQL database name (e.g. `"ops"`, `"platform"`) in production. The default is the empty string for SQLite-dev friendliness.
- **MUST** install the three table DDL scripts (`activity`, `activity_input`, `log_entries`) against the target schema **before** the host starts. Roadbed.Logging does not run schema migrations.
- **MUST** schedule a monthly partition-maintenance job in MySQL that (a) pre-creates next month's partition and (b) drops partitions whose range falls outside the 90-day retention window. On SQLite, run `DELETE FROM log_entries WHERE event_time_utc < datetime('now', '-90 days');` on the same cadence.
- **MUST** call `service.CompleteAsync(...)` or `service.FailAsync(...)` explicitly when a run ends. Disposing the `LoggingActivityScope` pops the ambient MEL scope and stops the diagnostic Activity, but it does **not** record a terminal status.
- **MUST** use structured log templates (`logger.LogInformation("Loaded {RowCount}", count)`) — the exporter splits the template (stored as `message_template`) from the named args (stored as `properties` JSON) for downstream aggregation. Interpolated `$"..."` strings produce a single rendered message with no template.

## MUST NOT

- **MUST NOT** reference `Roadbed.Crud` from a project that already takes `Roadbed.Logging`. The library is deliberately a peer, not a consumer, of the CRUD pattern.
- **MUST NOT** point `ILoggingDatabaseFactory` at any `DataConnectionStringType` other than `MySQL`, `SQLite`, or `SQLiteInMemory`. The installer throws on every other value.
- **MUST NOT** rely on `LoggingActivityScope` to auto-finalize the activity row. Skipped, Canceled, and Succeeded outcomes are all distinct terminal states; the dispose path has no way to choose between them.
- **MUST NOT** invent your own batching or retry layer around `LoggingActivityService` or the log-entry path — the background writer already batches, falls back to `Console.Error` on database error, and flushes on `StopAsync`.
- **MUST NOT** log from a category that overlaps `LoggingOptions.RecursionGuardCategories` and expect the entry to be persisted. Categories under `Roadbed.Logging`, `Roadbed.Data`, `Roadbed.Data.MySql`, `Roadbed.Data.Sqlite`, and `MySqlConnector` are dropped to prevent the database write path from logging through itself.
- **MUST NOT** pass `LoggingActivityStatus.Failed` to `CompleteAsync`. Use `FailAsync(activityId, exception)` instead — it records the exception message and type as well as the terminal status.
- **MUST NOT** read `IConfiguration` from inside Roadbed.Logging-aware code expecting the library to honor it. The library only sees `LoggingOptions` and `ILoggingDatabaseFactory` from DI.

## Code patterns

### Host wire-up

```csharp
// Program.cs (host)
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new LoggingOptions
{
    Schema = "ops",
    Application = "Foo",
    Environment = builder.Environment.EnvironmentName,
    BatchSize = 1000,
    FlushInterval = TimeSpan.FromSeconds(5),
});

builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

builder.Logging.AddRoadbedDbLogging();   // OTel + batch processor + exporter

builder.Services.InstallModulesInAppDomain(builder.Configuration);  // InstallLogging runs here

using var host = builder.Build();
await host.RunAsync();
```

### Marker-interface factory implementation

```csharp
namespace Foo.App;

using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Roadbed.Data;
using Roadbed.Logging;

internal sealed class FooLoggingDatabaseFactory : ILoggingDatabaseFactory
{
    public FooLoggingDatabaseFactory(IConnectionStringProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.Connecion = new DataConnecionString(
            DataConnectionStringType.MySQL,
            provider.Resolve("FooLogging"));
    }

    public DataConnecionString Connecion { get; }

    public IDbConnection CreateOpenConnection() { /* open + return */ throw new NotImplementedException(); }

    public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        /* open + return */ throw new NotImplementedException();
    }
}
```

### A Quartz job that opens an activity

```csharp
namespace Foo.App.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Roadbed.Logging;
using Roadbed.Scheduling;

public sealed class FooIngestionJob : BaseSchedulingJob<FooIngestionJob>
{
    private readonly LoggingActivityService _activities;
    private readonly IFooLoader _loader;

    public FooIngestionJob(
        ILogger<FooIngestionJob> logger,
        LoggingActivityService activities,
        IFooLoader loader)
        : base(
            name: "FooIngestion",
            description: "Loads the Foo dataset every 15 minutes.",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(15)),
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(loader);

        this._activities = activities;
        this._loader = loader;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string activityId = Ulid.NewUlid().ToString();        // app owns the ULID dep

        using LoggingActivityScope scope = await this._activities.BeginAsync(
            new LoggingActivityBeginRequest
            {
                Id = activityId,
                ActivityType = LoggingActivityType.Ingestion.ToString().ToLowerInvariant(),
                Target = "ops.foo",
                ActivityKey = "Foo.Ingestion.FullRefresh",
                FireInstanceId = this.Context.FireInstanceId,
                QuartzJobName = this.Context.JobDetail.Key.Name,
                QuartzJobGroup = this.Context.JobDetail.Key.Group,
                QuartzTriggerName = this.Context.Trigger.Key.Name,
                QuartzTriggerGroup = this.Context.Trigger.Key.Group,
                SchedulerInstanceId = this.Context.Scheduler.SchedulerInstanceId,
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

            this.Context.Result = $"Loaded {rows:N0} foo rows";
        }
        catch (Exception ex)
        {
            await this._activities.FailAsync(activityId, ex, CancellationToken.None);
            throw;
        }
    }
}
```

### Heartbeating from a long-running step

```csharp
while (await reader.ReadBatchAsync(cancellationToken) is { } batch)
{
    await this._sink.WriteAsync(batch, cancellationToken);
    await this._activities.HeartbeatAsync(activityId, cancellationToken);
}
```

### Patching current state mid-run

```csharp
await this._activities.UpdateAsync(
    new LoggingActivityUpdateRequest
    {
        ActivityId = activityId,
        Target = $"ops.{table}",                // discovered after Begin
        ParametersJson = JsonConvert.SerializeObject(currentParameters),
        RecordsImpacted = runningTotal,
    },
    cancellationToken);
```

### Recording lineage edges

```csharp
// "this silver-run consumed those two bronze loads"
await this._activities.AddInputAsync(silverActivityId, bronzePlacesActivityId, inputRole: "places", cancellationToken);
await this._activities.AddInputAsync(silverActivityId, bronzeCousubsActivityId, inputRole: "cousubs", cancellationToken);
```

## Common pitfalls

❌ Disposing the scope without calling `CompleteAsync`:
```csharp
using var scope = await activities.BeginAsync(request, ct);
await DoWorkAsync(ct);
// scope disposes → row stays in 'running' forever
```
✅ Always finalize explicitly:
```csharp
using var scope = await activities.BeginAsync(request, ct);
try { await DoWorkAsync(ct); await activities.CompleteAsync(scope.ActivityId, LoggingActivityStatus.Succeeded, cancellationToken: ct); }
catch (Exception ex) { await activities.FailAsync(scope.ActivityId, ex, CancellationToken.None); throw; }
```

❌ Logging from inside the database write path:
```csharp
// Repository inside Roadbed.Logging itself
this._logger.LogInformation("Inserted {N} entries", count);
// → category is "Roadbed.Logging.Repositories.LoggingLogEntryRepository"
// → recursion-guarded → silently dropped
```
✅ Either accept the drop (the guard exists for a reason) or pick a category outside the guard list for genuine operator-visible diagnostics.

❌ Treating logs as a substitute for the activity row:
```csharp
this._logger.LogInformation("Starting Foo run with batch={BatchId}", batchId);
// no activity row → no run record, no heartbeat, no terminal status
```
✅ Open an activity at run start; logs become the per-event narrative attached to it.

❌ Setting `Schema = "ops"` against SQLite without ATTACH:
```csharp
new LoggingOptions { Schema = "ops" }
// SQL becomes "INSERT INTO ops.activity ..." which SQLite rejects.
```
✅ Either leave `Schema` empty for SQLite or ATTACH the file under that alias.

❌ Passing `LoggingActivityStatus.Failed` to `CompleteAsync`:
```csharp
await activities.CompleteAsync(id, LoggingActivityStatus.Failed);  // throws ArgumentException
```
✅ Use `FailAsync(id, exception)` — it records the message and type as well.

## Quick reference

| Need                                                       | Use                                                                                                |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Insert a run record                                        | `await service.BeginAsync(request, ct)`                                                            |
| Stamp last_heartbeat_on                                    | `await service.HeartbeatAsync(activityId, ct)`                                                     |
| Patch current-state columns (target, metrics, Quartz, ...) | `await service.UpdateAsync(updateRequest, ct)`                                                     |
| Finish successfully                                        | `await service.CompleteAsync(activityId, LoggingActivityStatus.Succeeded, recordsImpacted: n, ct)` |
| Finish on exception                                        | `await service.FailAsync(activityId, exception, ct)`                                               |
| Mark canceled / skipped                                    | `await service.CompleteAsync(activityId, LoggingActivityStatus.Canceled, ct)`                      |
| Record a lineage edge                                      | `await service.AddInputAsync(consumerId, inputId, inputRole, ct)`                                  |
| Wire MEL → OTel → DB                                       | `builder.Logging.AddRoadbedDbLogging()`                                                            |
| Override drop policy                                       | `new LoggingOptions { ChannelFullPolicy = LoggingChannelFullPolicy.BlockBriefly }`                  |

## DDL install scripts

These are copies of the source-of-truth files in
`src/Roadbed.Logging/Assets/Tables/<table>/install_<provider>.txt`. Before
executing, substitute the literal token `{SchemaPrefix}` with either the
empty string (unqualified) or a database name followed by a dot (`ops.`).

### MySQL / MariaDB

#### activity

```sql
CREATE TABLE {SchemaPrefix}activity (
     id                    CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
    ,parent_activity_id    CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,root_activity_id      CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,trace_id              CHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,span_id               CHAR(16) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,activity_key          VARCHAR(100)  NULL
    ,application           VARCHAR(100)  NOT NULL
    ,environment           VARCHAR(20)   NULL
    ,activity_type         VARCHAR(50)   NOT NULL
    ,target                VARCHAR(255)  NULL
    ,status                ENUM('pending','running','succeeded','failed','canceled','skipped') NOT NULL DEFAULT 'pending'
    ,started_on            DATETIME(6)   NULL
    ,completed_on          DATETIME(6)   NULL
    ,last_heartbeat_on     DATETIME(6)   NULL
    ,records_impacted      BIGINT UNSIGNED NULL
    ,parameters            JSON          NULL
    ,metrics               JSON          NULL
    ,error                 TEXT          NULL
    ,error_type            VARCHAR(255)  NULL
    ,scheduler_instance_id VARCHAR(200)  NULL
    ,fire_instance_id      VARCHAR(95)   NULL
    ,quartz_job_name       VARCHAR(200)  NULL
    ,quartz_job_group      VARCHAR(200)  NULL
    ,quartz_trigger_name   VARCHAR(200)  NULL
    ,quartz_trigger_group  VARCHAR(200)  NULL
    ,host                  VARCHAR(255)  NULL
    ,process_id            INT           NULL
    ,created_by            BIGINT UNSIGNED NULL
    ,created_on            DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
    ,last_modified_on      DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
    ,PRIMARY KEY (id)
    ,KEY idx_activity_key (activity_key)
    ,KEY idx_activity_parent (parent_activity_id)
    ,KEY idx_activity_root (root_activity_id)
    ,KEY idx_activity_trace (trace_id)
    ,KEY idx_activity_app_started (application, started_on)
    ,KEY idx_activity_type_started (activity_type, started_on)
    ,KEY idx_activity_status (status)
    ,KEY idx_activity_fire (fire_instance_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### activity_input

```sql
CREATE TABLE {SchemaPrefix}activity_input (
     activity_id        CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
    ,input_activity_id  CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
    ,input_role         VARCHAR(50) NULL
    ,created_on         DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
    ,PRIMARY KEY (activity_id, input_activity_id)
    ,KEY idx_activity_input_reverse (input_activity_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### log_entries

```sql
CREATE TABLE {SchemaPrefix}log_entries (
     id               BIGINT UNSIGNED NOT NULL AUTO_INCREMENT
    ,event_time_utc   DATETIME(6)   NOT NULL
    ,recorded_on      DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
    ,log_level        TINYINT UNSIGNED NOT NULL
    ,category         VARCHAR(255)  NOT NULL
    ,event_id         INT           NULL
    ,event_name       VARCHAR(255)  NULL
    ,message          TEXT          NOT NULL
    ,message_template TEXT          NULL
    ,properties       JSON          NULL
    ,exception        TEXT          NULL
    ,exception_type   VARCHAR(255)  NULL
    ,activity_id      CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,trace_id         CHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,span_id          CHAR(16) CHARACTER SET ascii COLLATE ascii_bin NULL
    ,application      VARCHAR(100)  NOT NULL
    ,environment      VARCHAR(20)   NULL
    ,host             VARCHAR(255)  NULL
    ,process_id       INT           NULL
    ,PRIMARY KEY (id, event_time_utc)
    ,KEY idx_log_activity (activity_id)
    ,KEY idx_log_trace (trace_id)
    ,KEY idx_log_app_time (application, event_time_utc)
    ,KEY idx_log_level_time (log_level, event_time_utc)
    ,KEY idx_log_time (event_time_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
PARTITION BY RANGE (TO_DAYS(event_time_utc)) (
     PARTITION p_min VALUES LESS THAN (TO_DAYS('2026-01-01'))
    ,PARTITION pmax  VALUES LESS THAN MAXVALUE
);
```

### SQLite

#### activity

```sql
CREATE TABLE {SchemaPrefix}activity (
     id                    TEXT     NOT NULL COLLATE BINARY
    ,parent_activity_id    TEXT     NULL     COLLATE BINARY
    ,root_activity_id      TEXT     NULL     COLLATE BINARY
    ,trace_id              TEXT     NULL     COLLATE BINARY
    ,span_id               TEXT     NULL     COLLATE BINARY
    ,activity_key          TEXT     NULL
    ,application           TEXT     NOT NULL
    ,environment           TEXT     NULL
    ,activity_type         TEXT     NOT NULL
    ,target                TEXT     NULL
    ,status                TEXT     NOT NULL DEFAULT 'pending'
                           CHECK (status IN ('pending','running','succeeded','failed','canceled','skipped'))
    ,started_on            DATETIME NULL
    ,completed_on          DATETIME NULL
    ,last_heartbeat_on     DATETIME NULL
    ,records_impacted      INTEGER  NULL
    ,parameters            TEXT     NULL
    ,metrics               TEXT     NULL
    ,error                 TEXT     NULL
    ,error_type            TEXT     NULL
    ,scheduler_instance_id TEXT     NULL
    ,fire_instance_id      TEXT     NULL
    ,quartz_job_name       TEXT     NULL
    ,quartz_job_group      TEXT     NULL
    ,quartz_trigger_name   TEXT     NULL
    ,quartz_trigger_group  TEXT     NULL
    ,host                  TEXT     NULL
    ,process_id            INTEGER  NULL
    ,created_by            INTEGER  NULL
    ,created_on            DATETIME NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    ,last_modified_on      DATETIME NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    ,PRIMARY KEY (id)
);

CREATE INDEX idx_activity_key             ON {SchemaPrefix}activity (activity_key);
CREATE INDEX idx_activity_parent          ON {SchemaPrefix}activity (parent_activity_id);
CREATE INDEX idx_activity_root            ON {SchemaPrefix}activity (root_activity_id);
CREATE INDEX idx_activity_trace           ON {SchemaPrefix}activity (trace_id);
CREATE INDEX idx_activity_app_started     ON {SchemaPrefix}activity (application, started_on);
CREATE INDEX idx_activity_type_started    ON {SchemaPrefix}activity (activity_type, started_on);
CREATE INDEX idx_activity_status          ON {SchemaPrefix}activity (status);
CREATE INDEX idx_activity_fire            ON {SchemaPrefix}activity (fire_instance_id);
```

#### activity_input

```sql
CREATE TABLE {SchemaPrefix}activity_input (
     activity_id        TEXT     NOT NULL COLLATE BINARY
    ,input_activity_id  TEXT     NOT NULL COLLATE BINARY
    ,input_role         TEXT     NULL
    ,created_on         DATETIME NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    ,PRIMARY KEY (activity_id, input_activity_id)
);

CREATE INDEX idx_activity_input_reverse ON {SchemaPrefix}activity_input (input_activity_id);
```

#### log_entries

```sql
CREATE TABLE {SchemaPrefix}log_entries (
     id               INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT
    ,event_time_utc   DATETIME NOT NULL
    ,recorded_on      DATETIME NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    ,log_level        INTEGER  NOT NULL
    ,category         TEXT     NOT NULL
    ,event_id         INTEGER  NULL
    ,event_name       TEXT     NULL
    ,message          TEXT     NOT NULL
    ,message_template TEXT     NULL
    ,properties       TEXT     NULL
    ,exception        TEXT     NULL
    ,exception_type   TEXT     NULL
    ,activity_id      TEXT     NULL COLLATE BINARY
    ,trace_id         TEXT     NULL COLLATE BINARY
    ,span_id          TEXT     NULL COLLATE BINARY
    ,application      TEXT     NOT NULL
    ,environment      TEXT     NULL
    ,host             TEXT     NULL
    ,process_id       INTEGER  NULL
);

CREATE INDEX idx_log_activity   ON {SchemaPrefix}log_entries (activity_id);
CREATE INDEX idx_log_trace      ON {SchemaPrefix}log_entries (trace_id);
CREATE INDEX idx_log_app_time   ON {SchemaPrefix}log_entries (application, event_time_utc);
CREATE INDEX idx_log_level_time ON {SchemaPrefix}log_entries (log_level, event_time_utc);
CREATE INDEX idx_log_time       ON {SchemaPrefix}log_entries (event_time_utc);
```

### Retention

- **MySQL** — schedule a monthly job that pre-creates next month's partition and drops every partition whose range is older than the retention window. Roadbed.Logging does not ship the partition routine; build it as a stored proc or a Roadbed.Scheduling job.
- **SQLite** — schedule the same cadence to run:
  ```sql
  DELETE FROM {SchemaPrefix}log_entries
  WHERE event_time_utc < datetime('now', '-90 days');
  ```
  Follow with `VACUUM` (or set `PRAGMA auto_vacuum = INCREMENTAL` plus periodic `incremental_vacuum`) to reclaim disk.
