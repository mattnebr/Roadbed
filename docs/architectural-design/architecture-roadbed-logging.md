# Roadbed.Logging Architecture

Roadbed.Logging is a self-contained library that persists `Microsoft.Extensions.Logging` (MEL) output to a relational database as structured rows, and tracks the **activities** — run instances of jobs, pipelines, and ad-hoc work — that those log rows tie back to. It is OpenTelemetry-first: logging flows through the OTel logging pipeline and a custom batching exporter, so DB persistence is one exporter and OTLP export (Grafana / Tempo / Jaeger / etc.) is a configuration add, not a rewrite.

Roadbed.Logging is deliberately a **peer** of Roadbed.Crud, not a consumer of it. The high-volume log write is an internal custom bulk insert that stamps each row with its own originating `activity_id` — distinct from the CRUDALBT "B" tier's uniform-`activityId` stamping appropriate to Bronze/Silver loads.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Logging NuGet package. When a developer asks you to add structured log persistence, run/activity tracking, or lineage capture to a .NET application, use this document to wire DI, open activities, and shape the schema.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **The caller mints the ULID.** Roadbed.Logging does not generate identifiers. Use the same ULID library you use to stamp `IAsyncBulkInsertOperation.BulkInsertAsync` calls during the run.
5. **Register `LoggingOptions` and `ILoggingDatabaseFactory` as singletons** in DI **before** `services.InstallModulesInAppDomain(configuration)` runs. The installer resolves both up-front and throws if either is missing.
6. **Call `builder.Logging.AddRoadbedDbLogging()`** on the host's `ILoggingBuilder` to wire the OpenTelemetry MEL provider, the batch processor, and the database exporter. The `IServiceCollectionInstaller` does not see `ILoggingBuilder`, so this step lives outside `InstallLogging`.
7. **Disposing `LoggingActivityScope` does not record a terminal status.** Always call `CompleteAsync(...)` or `FailAsync(...)` explicitly — the dispose path has no way to choose between Succeeded, Canceled, and Skipped.
8. **`CancellationToken` is always the last parameter** with `= default` on async methods.
9. **Use structured log templates** (`logger.LogInformation("Loaded {RowCount}", count)`). The exporter splits the template (stored as `message_template`) from the named args (stored as `properties` JSON) for downstream aggregation. Interpolated `$"..."` strings produce a single rendered message with no template.
10. **Roadbed.Logging never reads `IConfiguration` directly.** The host populates `LoggingOptions` from whatever source it likes; the library only sees the POCO.
11. **Roadbed.Logging never references `Roadbed.Crud`.** Do not add the dependency.
12. **MySQL/MariaDB and SQLite are the only supported `DataConnectionStringType` values in v1.** Postgres, SQL Server, etc. cause the installer to throw.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-logging.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-logging.md#type-catalog)
3. [Package Relationship](architecture-roadbed-logging.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-logging.md#namespace-convention)
5. [Two Write Paths](architecture-roadbed-logging.md#two-write-paths)
6. [Entities and Schema](architecture-roadbed-logging.md#entities-and-schema)
    - [activity](architecture-roadbed-logging.md#activity)
    - [activity_input](architecture-roadbed-logging.md#activity_input)
    - [log_entries](architecture-roadbed-logging.md#log_entries)
7. [LoggingActivityService](architecture-roadbed-logging.md#loggingactivityservice)
    - [BeginAsync](architecture-roadbed-logging.md#beginasync)
    - [HeartbeatAsync](architecture-roadbed-logging.md#heartbeatasync)
    - [UpdateAsync](architecture-roadbed-logging.md#updateasync)
    - [CompleteAsync and FailAsync](architecture-roadbed-logging.md#completeasync-and-failasync)
    - [AddInputAsync](architecture-roadbed-logging.md#addinputasync)
    - [LoggingActivityScope](architecture-roadbed-logging.md#loggingactivityscope)
8. [The Log Write Pipeline](architecture-roadbed-logging.md#the-log-write-pipeline)
    - [RoadbedDbLogRecordExporter](architecture-roadbed-logging.md#roadbeddblogrecordexporter)
    - [LoggingChannel](architecture-roadbed-logging.md#loggingchannel)
    - [LogWriterHostedService](architecture-roadbed-logging.md#logwriterhostedservice)
    - [Recursion Safety](architecture-roadbed-logging.md#recursion-safety)
9. [Module Auto-Discovery and Wiring](architecture-roadbed-logging.md#module-auto-discovery-and-wiring)
10. [Schema Installation](architecture-roadbed-logging.md#schema-installation)
11. [Implementation Walkthrough](architecture-roadbed-logging.md#implementation-walkthrough)
12. [Common Pitfalls](architecture-roadbed-logging.md#common-pitfalls)
13. [Quick Reference](architecture-roadbed-logging.md#quick-reference)

---

## Type Catalog

Roadbed.Logging contains **17 public types** organized into seven groups.

### Entities (3 types)

| Type                   | Kind         | Purpose                                                                  |
| ---------------------- | ------------ | ------------------------------------------------------------------------ |
| `LoggingActivity`      | Sealed class | One row in `activity`. Mutable — patched as the run progresses.          |
| `LoggingActivityInput` | Sealed class | One edge in `activity_input` (the lineage DAG).                          |
| `LoggingLogEntry`      | Sealed class | One row in `log_entries`. Append-only; populated by the exporter.        |

### Enumerators (3 types)

| Type                          | Kind | Purpose                                                                          |
| ----------------------------- | ---- | -------------------------------------------------------------------------------- |
| `LoggingActivityStatus`       | Enum | `Pending`, `Running`, `Succeeded`, `Failed`, `Canceled`, `Skipped`.              |
| `LoggingActivityType`         | Enum | `Unknown`, `Ingestion`, `Transformation`, `Promotion`, `Maintenance`, `Manual`, `Custom`. |
| `LoggingChannelFullPolicy`    | Enum | `DropOldest` (default) or `BlockBriefly` for backpressure-on-overflow.            |

### Public DTOs (3 types)

| Type                            | Kind         | Purpose                                                                       |
| ------------------------------- | ------------ | ----------------------------------------------------------------------------- |
| `LoggingOptions`                | Sealed class | Host-supplied POCO: schema, application, batch tuning, recursion guard list.  |
| `LoggingActivityBeginRequest`   | Sealed class | Initial values for a new activity row. Carries the caller-supplied ULID.      |
| `LoggingActivityUpdateRequest`  | Sealed class | Patch payload for non-null current-state columns (target, metrics, Quartz). |

### Marker Interface (1 type)

| Type                       | Kind      | Purpose                                                                       |
| -------------------------- | --------- | ----------------------------------------------------------------------------- |
| `ILoggingDatabaseFactory`  | Interface | Extends `IDataConnectionFactory`. Marker so DI can locate the logging schema. |

### Service Surface (2 types)

| Type                       | Kind                | Purpose                                                                       |
| -------------------------- | ------------------- | ----------------------------------------------------------------------------- |
| `LoggingActivityService`   | Public sealed class | Activity lifecycle API. Dual ctor (public takes `ILogger<T>`; internal takes deps for tests). |
| `LoggingActivityScope`     | Public sealed class | `IDisposable` bundling the diagnostic `Activity` + MEL scope frame.           |

### OTel Pipeline (3 types — `internal`)

| Type                              | Kind         | Purpose                                                                          |
| --------------------------------- | ------------ | -------------------------------------------------------------------------------- |
| `RoadbedDbLogRecordExporter`      | Sealed class | `BaseExporter<LogRecord>`. Maps records, enqueues onto the channel.              |
| `LoggingChannel`                  | Sealed class | Bounded `Channel<LoggingLogEntry>` shared by exporter (producer) and writer (consumer). |
| `LogWriterHostedService`          | Sealed class | `BackgroundService`. Drains, batches, flushes, falls back to `Console.Error`.    |

### Wiring (2 types)

| Type                            | Kind                 | Purpose                                                                              |
| ------------------------------- | -------------------- | ------------------------------------------------------------------------------------ |
| `InstallLogging`                | Class                | `IServiceCollectionInstaller`. Wires repositories, service, channel, hosted writer.  |
| `LoggingBuilderExtensions`      | Public static class  | Exposes `builder.Logging.AddRoadbedDbLogging()` for OTel wire-up.                     |

The repository contracts (`ILoggingActivityRepository`, `ILoggingActivityInputRepository`, `ILoggingLogEntryRepository`) and implementations are `internal` to the library; they are not part of the public surface and are split into `RepositoryInterfaces/` and `Repositories/` folders.

---

## Package Relationship

```
                       ┌───────────────────────┐
                       │  Consuming Application │
                       └───────────┬───────────┘
                                   │
                  ┌────────────────┴────────────────┐
                  │                                  │
                  ▼                                  ▼
       ┌─────────────────────┐           ┌────────────────────────┐
       │   Roadbed.Logging   │           │     Roadbed.Scheduling │
       │  (this package)     │           │   (optional — emits    │
       └─────────┬───────────┘           │    BeginAsync calls)   │
                 │                       └────────────────────────┘
   ┌─────────────┼────────────────┐
   │             │                │
   ▼             ▼                ▼
┌──────┐   ┌──────────┐   ┌────────────┐
│Common│   │Data      │   │Data.MySql / │
│      │   │+ Dapper  │   │ Data.Sqlite │
└──────┘   └──────────┘   └────────────┘
              │
              ▼
        OpenTelemetry
        + OTel.Hosting
```

Roadbed.Logging depends on `Roadbed.Common`, `Roadbed.Data`, `Roadbed.Data.Dapper`, `Roadbed.Data.MySql`, and `Roadbed.Data.Sqlite`. It does **not** depend on `Roadbed.Crud`. Roadbed.Scheduling does not depend on Roadbed.Logging; consuming apps that want their Quartz jobs to open activities call `LoggingActivityService.BeginAsync` from their job's `ExecuteAsync` body.

---

## Namespace Convention

All public types live in the top-level `Roadbed.Logging` namespace. The installer lives in `Roadbed.Logging.Installers`. Repository contracts and implementations are `internal` and also live in `Roadbed.Logging`.

The entity class is named `LoggingActivity` rather than `Activity` to avoid colliding with `System.Diagnostics.Activity`, which the library uses heavily. The table is still called `activity` and its primary key column is still `id` — the C# rename is for code clarity only.

---

## Two Write Paths

```
Application code → ILogger (MEL)
        │
        ├─(logs)→ OpenTelemetryLoggerProvider
        │             → BatchLogRecordProcessor              (OTel)
        │             → RoadbedDbLogRecordExporter           (maps LogRecord → LoggingLogEntry)
        │             → LoggingChannel<LoggingLogEntry>      (bounded, non-blocking)
        │             → LogWriterHostedService               (drains, batches, custom bulk insert)
        │             → log_entries                          (per-row activity_id)
        │
        └─(activities)→ LoggingActivityService               (public surface)
                          → ILoggingActivityRepository       (insert + patch + complete)
                          → ILoggingActivityInputRepository  (insert lineage edges)
                          → activity, activity_input         (mutable; small volume)
```

Two distinct paths, deliberately:

- **Activities** are low-volume and **mutable** (insert `running` → heartbeat → curated patches → terminal status). Custom single-row INSERT + UPDATE via `Roadbed.Data`.
- **Log entries** are high-volume and **append-only**. Buffered through a channel and **bulk-inserted in batches** off the hot path by an internal custom writer.

---

## Entities and Schema

The DDL is shipped as install scripts under [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/). Each table has both an `install_mysql.txt` and an `install_sqlite.txt`. Both files use the literal token `{SchemaPrefix}` for schema qualification — substitute either the empty string or a database name followed by a dot (`ops.`) before executing.

### activity

One row per run. Mutable.

- **`id`** — `CHAR(26) ascii_bin` (MySQL) / `TEXT COLLATE BINARY` (SQLite). Caller-supplied ULID. `ascii_bin` collation guarantees lexical order matches chronological order.
- **`parent_activity_id`**, **`root_activity_id`** — soft references for run hierarchy. Roots have `root_activity_id == id`.
- **`trace_id`**, **`span_id`** — captured from `Activity.Current` at `BeginAsync` time. W3C 32-hex and 16-hex respectively.
- **`activity_key`** — logical definition slug (e.g. `"Foo.Ingestion.FullRefresh"`). Groups multiple runs of the same job.
- **`application`**, **`environment`**, **`host`**, **`process_id`** — provenance.
- **`activity_type`** — `ingestion` / `transformation` / `promotion` / `maintenance` / `manual` / custom.
- **`target`** — what the run acted on (`schema.table`, dataset name).
- **`status`** — `pending` / `running` / `succeeded` / `failed` / `canceled` / `skipped`.
- **`started_on`**, **`completed_on`**, **`last_heartbeat_on`** — UTC `DATETIME(6)`. A stale heartbeat combined with `status = 'running'` indicates a crashed or zombie process.
- **`records_impacted`** — headline count. Typically the sum of `BulkInsertAsync` returns during the run.
- **`parameters`**, **`metrics`** — `JSON` (MySQL) / `TEXT` (SQLite); structured input config and structured output metrics respectively.
- **`error`**, **`error_type`** — populated by `FailAsync(exception)`.
- **Quartz block** — `scheduler_instance_id`, `fire_instance_id`, `quartz_job_name`, `quartz_job_group`, `quartz_trigger_name`, `quartz_trigger_group`. Snapshotted at `BeginAsync` time (or patched later via `UpdateAsync`) because `QRTZ_FIRED_TRIGGERS` is transient and is cleared once the trigger completes.
- **`created_on`**, **`last_modified_on`** — server-defaulted. MySQL maintains `last_modified_on` via `ON UPDATE CURRENT_TIMESTAMP(6)`; SQLite has no equivalent, so the repository sets it explicitly.

Indexes cover the `activity_key`, parent/root, trace, `application + started_on`, `activity_type + started_on`, `status`, and `fire_instance_id` lookups that analysts and dashboards run.

### activity_input

The lineage DAG: "this activity consumed the output of those upstream activities."

- Composite primary key (`activity_id`, `input_activity_id`).
- `input_role` — optional free-form label (`"places"`, `"cousubs"`, `"hud-centroid"`).
- One reverse index on `input_activity_id` for impact analysis (which downstream activities consumed *this* upstream output?).
- Duplicate edges are silently coalesced — the repository emits `INSERT ... ON DUPLICATE KEY UPDATE` (MySQL) or `INSERT OR IGNORE` (SQLite).

### log_entries

The high-volume append-only log store.

- **Composite primary key** `(id, event_time_utc)`. MySQL requires the partition key in every unique key.
- **MySQL: RANGE-partitioned monthly** on `TO_DAYS(event_time_utc)`. Ship a partition-maintenance routine (a stored proc or a Roadbed.Scheduling job) that pre-creates next month's partition and drops partitions older than the 90-day retention window.
- **SQLite: no partitioning.** Retention is a scheduled `DELETE FROM log_entries WHERE event_time_utc < datetime('now', '-90 days')` followed by `VACUUM`.
- **`activity_id`** is **per row**, sampled at log time — not a uniform stamp for the whole batch. This is the core distinction from the CRUDALBT "B" tier.
- **`message_template`** is the unrendered template (the `{OriginalFormat}` attribute on the `LogRecord`); **`properties`** is JSON of the named args. Keeping these separate lets analysts aggregate log events by template across many argument values.

---

## LoggingActivityService

`LoggingActivityService` is the only public service surface in the library. It uses the standard Roadbed dual-constructor pattern — the public constructor takes only `ILogger<LoggingActivityService>` and resolves repositories via `ServiceLocator`; the `internal` constructor takes the repositories directly for unit tests via `InternalsVisibleTo`.

### BeginAsync

```csharp
Task<LoggingActivityScope> BeginAsync(
    LoggingActivityBeginRequest request,
    CancellationToken cancellationToken = default);
```

`BeginAsync` does five things in order:

1. **Starts a `System.Diagnostics.Activity`** named `"roadbed.logging.activity"`. The default `ActivityIdFormat` is W3C, so the new Activity gets a 32-hex `TraceId` and a 16-hex `SpanId` automatically. The Activity is tagged with `roadbed.activity_id = {requestId}` so consumers can read it out of `Activity.Current` if they prefer that to the MEL scope.
2. **Pushes a MEL `BeginScope`** carrying `("activity_id", requestId)`. Subsequent `ILogger.Log*` calls inside the scope inherit this key, and the OTel pipeline propagates it into the `LogRecord` scope chain.
3. **Constructs a `LoggingActivity` entity** with `Status = Running`, `StartedOn = LastHeartbeatOn = UtcNow`, `Host` and `ProcessId` snapshotted from `Environment.MachineName` / `Environment.ProcessId`, and `TraceId` / `SpanId` lifted from the freshly-started Activity. Defaults `RootActivityId` to `request.Id` when the caller omits it.
4. **Calls `ILoggingActivityRepository.InsertAsync`** to persist the row.
5. **Returns a `LoggingActivityScope`** wrapping the Activity and the MEL scope handle.

If the repository INSERT throws, the service disposes the MEL scope and stops the Activity before propagating, so the caller is never left with an ambient `activity_id` pointing at a row that does not exist.

### HeartbeatAsync

Stamps `UtcNow` into `last_heartbeat_on` via a one-row UPDATE. Long-running steps should call this every few seconds — every iteration of a batch loop is a reasonable cadence — so that a stale heartbeat with `status = 'running'` becomes a reliable signal of a crashed process.

### UpdateAsync

```csharp
Task UpdateAsync(
    LoggingActivityUpdateRequest request,
    CancellationToken cancellationToken = default);
```

Patches only the **non-null** properties on the request onto the existing row via a `COALESCE`-driven UPDATE. Properties left at `null` preserve their existing values.

This is the right surface for "current state" mid-run updates — fields that may not be known at Begin time or that evolve through the run:

- `Target` — sometimes discovered after the run starts.
- `Parameters`, `Metrics` — JSON blobs that grow as work progresses.
- `RecordsImpacted` — running total.
- The Quartz block — for callers that didn't have it at Begin time, or for jobs that re-key partway through.

`UpdateAsync` deliberately excludes `Status`, `StartedOn`, `CompletedOn`, and `LastHeartbeatOn`, which have dedicated methods.

### CompleteAsync and FailAsync

```csharp
Task CompleteAsync(
    string activityId,
    LoggingActivityStatus status,
    long? recordsImpacted = null,
    string? metricsJson = null,
    CancellationToken cancellationToken = default);

Task FailAsync(
    string activityId,
    Exception error,
    CancellationToken cancellationToken = default);
```

`CompleteAsync` is the terminal call for non-exception outcomes: `Succeeded`, `Canceled`, or `Skipped`. It explicitly **rejects** `Status.Failed` — that path is `FailAsync`, which records the exception message and the fully-qualified type name as well as the status.

Both methods stamp `completed_on = UtcNow`.

### AddInputAsync

```csharp
Task AddInputAsync(
    string activityId,
    string inputActivityId,
    string? inputRole = null,
    CancellationToken cancellationToken = default);
```

Inserts a single edge into `activity_input`. Call it once per upstream input the run consumed — a Silver run that joins three Bronze loads emits three `AddInputAsync` calls. Duplicate edges are silently coalesced.

### LoggingActivityScope

`LoggingActivityScope` is `IDisposable` and bundles three things: the caller-supplied `ActivityId`, the started `System.Diagnostics.Activity` (so consumers can read its `TraceId`/`SpanId`), and the MEL scope handle.

Dispose pops the MEL scope first, then stops the Activity. It is intentionally **not** an async-disposable and does **not** auto-call `CompleteAsync` or `FailAsync`. The terminal status is information the dispose path cannot recover — Succeeded, Canceled, and Skipped are all distinct outcomes.

```csharp
using LoggingActivityScope scope = await activities.BeginAsync(request, cancellationToken);
try
{
    await DoWorkAsync(cancellationToken);
    await activities.CompleteAsync(scope.ActivityId, LoggingActivityStatus.Succeeded, ct);
}
catch (Exception ex)
{
    await activities.FailAsync(scope.ActivityId, ex, CancellationToken.None);
    throw;
}
```

---

## The Log Write Pipeline

```
ILogger.LogInformation(...)
   │
   ▼
OpenTelemetryLoggerProvider             (configured via builder.Logging.AddRoadbedDbLogging)
   │
   ▼
BatchLogRecordProcessor                 (OTel default; default 1024-record buffer)
   │
   ▼
RoadbedDbLogRecordExporter.Export(batch)
   │  ├── recursion guard check (drops Roadbed.Logging/Data/MySqlConnector categories)
   │  ├── MapRecord: pulls activity_id from scope, then Activity.Current tag
   │  ├── pulls message_template from {OriginalFormat}, properties from named args
   │  └── enqueue via LoggingChannel.TryWrite
   │
   ▼
LoggingChannel  (BoundedChannelOptions{ FullMode = DropOldest | Wait })
   │
   ▼
LogWriterHostedService.ExecuteAsync     (BackgroundService)
   │  ├── drain channel into a buffer
   │  ├── when buffer >= BatchSize OR FlushInterval elapsed: FlushAsync
   │  ├── on shutdown: drain remaining + final FlushAsync(CancellationToken.None)
   │  └── on insert failure: WriteFallback(batch) → Console.Error
   │
   ▼
ILoggingLogEntryRepository.BulkInsertAsync
   │  └── chunked multi-row INSERT, sized under MaxPlaceholdersPerStatement
   │
   ▼
log_entries
```

### RoadbedDbLogRecordExporter

The exporter is an OTel `BaseExporter<LogRecord>`. Its `Export(in Batch<LogRecord>)` walks each record:

1. **Recursion guard.** If the record's `CategoryName` starts with any prefix in `LoggingOptions.RecursionGuardCategories`, the record is silently dropped. Defaults cover `Roadbed.Logging`, `Roadbed.Data`, `Roadbed.Data.MySql`, `Roadbed.Data.Sqlite`, and `MySqlConnector`. Hosts can append their own prefixes (for instance to silence a noisy third-party driver).
2. **Map.** Pulls `Timestamp` → `event_time_utc`, `LogLevel` → numeric, `CategoryName`, `EventId.Id` and `EventId.Name`, `FormattedMessage` → `message`, `Exception.Message` and `Exception.GetType().FullName`, `TraceId` and `SpanId` (already hex-encoded by OTel). From `LogRecord.Attributes`: the `{OriginalFormat}` key becomes `message_template`; the rest become `properties` JSON via `Newtonsoft.Json`.
3. **Resolve `activity_id`.** Walks the scope chain via `LogRecord.ForEachScope` looking for the `"activity_id"` key. Falls back to `Activity.Current?.GetTagItem("roadbed.activity_id")` when no scope frame carried one. Both mechanisms are wired by `BeginAsync` so either path resolves.
4. **Enqueue.** `LoggingChannel.TryWrite(entry)`. On the default `DropOldest` policy this always succeeds; on `BlockBriefly` it returns `false` on contention and increments the dropped counter.

The exporter returns `ExportResult.Success` regardless of channel acceptance — the writer is responsible for surfacing back-pressure.

### LoggingChannel

A thin wrapper around `Channel<LoggingLogEntry>`. Constructed from `LoggingOptions.ChannelCapacity` (default 50,000) and `LoggingOptions.ChannelFullPolicy` (default `DropOldest`).

- **`DropOldest`** — `BoundedChannelFullMode.DropOldest`. Producers never block; the channel silently discards the oldest queued entry to make room. Drops are not directly observable through `TryWrite`.
- **`BlockBriefly`** — `BoundedChannelFullMode.Wait`. Producers calling `TryWrite` get `false` on contention, and the wrapper increments an atomic dropped counter. The writer reads and resets the counter on every successful flush and surfaces it as a periodic Warning log.

`SingleReader = true` because only the hosted writer drains.

### LogWriterHostedService

A `BackgroundService` whose `ExecuteAsync` loops on `LoggingChannel.Reader.WaitToReadAsync`. Each iteration:

1. Drains everything immediately available with `TryRead`.
2. Adds each entry to a buffer.
3. **Flushes** when the buffer reaches `LoggingOptions.BatchSize` (default 1000) OR when `LoggingOptions.FlushInterval` (default 5 seconds) elapses since the last flush — whichever comes first.

`FlushAsync` calls `ILoggingLogEntryRepository.BulkInsertAsync`. On success, it reads and zeroes the dropped-counter and surfaces it as a Warning log. On exception, it calls a private `WriteFallback` that writes the batch's pretty-printed contents to `Console.Error` so the lines are never silently lost. The buffer is always cleared in `finally` — a single bad batch must not wedge the writer loop.

`StopAsync` (inherited from `BackgroundService`) cancels `ExecuteAsync`. The `catch (OperationCanceledException)` block falls through to a final drain that pulls the remaining channel content into the buffer and emits one last `FlushAsync(CancellationToken.None)` so the shutdown does not lose work.

### Recursion Safety

Structurally, the channel hand-off decouples produce from consume — `Export` runs on the OTel batch processor thread; `FlushAsync` runs on the hosted-service thread. The recursion-guard filter on the exporter is a backstop against any framework-level diagnostic that would otherwise feed log lines back through the database write path. Internal writer diagnostics (the "Dropped N entries" warning, the bulk-insert retry messages) emit through the writer's `ILogger<LogWriterHostedService>`, whose category matches the guard. Catastrophic-fallback messages (DB errors, dropped batches) go to `Console.Error` directly.

---

## Module Auto-Discovery and Wiring

```csharp
public class InstallLogging : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Resolve host registrations up-front; throw with actionable messages if missing.
        ILoggingDatabaseFactory factory;
        using (var setupProvider = services.BuildServiceProvider())
        {
            _ = setupProvider.GetService<LoggingOptions>()
                ?? throw new InvalidOperationException(...);
            factory = setupProvider.GetService<ILoggingDatabaseFactory>()
                ?? throw new InvalidOperationException(...);
        }

        // 2. Validate the connection-string type is one we support.
        ValidateProvider(factory);

        // 3. Configure Dapper [Column] mapping for the three entities.
        DapperMapping.Configure(
            typeof(LoggingActivity),
            typeof(LoggingActivityInput),
            typeof(LoggingLogEntry));

        // 4. Register repositories, service, channel, and hosted writer as singletons.
        services.TryAddSingleton<ILoggingActivityRepository, LoggingActivityRepository>();
        services.TryAddSingleton<ILoggingActivityInputRepository, LoggingActivityInputRepository>();
        services.TryAddSingleton<ILoggingLogEntryRepository, LoggingLogEntryRepository>();
        services.TryAddSingleton<ILoggingActivityService, LoggingActivityService>();
        services.TryAddSingleton<LoggingActivityService>();
        services.TryAddSingleton<LoggingChannel>();
        services.AddHostedService<LogWriterHostedService>();

        // 5. Snapshot ServiceLocator so LoggingActivityService's public ctor can resolve.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

The companion `LoggingBuilderExtensions.AddRoadbedDbLogging` lives outside `InstallLogging` because the MEL configuration extension method needs `ILoggingBuilder`, which an `IServiceCollectionInstaller` does not see. It registers OTel's logger provider with `IncludeScopes = true`, `IncludeFormattedMessage = true`, `ParseStateValues = true`, then wires a `BatchLogRecordExportProcessor` whose factory pulls `LoggingChannel` and `LoggingOptions` from the eventual `IServiceProvider`.

Host wiring order:

```csharp
builder.Services.AddSingleton(new LoggingOptions { ... });
builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

builder.Logging.AddRoadbedDbLogging();                        // OTel + exporter
builder.Services.InstallModulesInAppDomain(builder.Configuration);  // InstallLogging runs

using var host = builder.Build();
await host.RunAsync();
```

---

## Schema Installation

Roadbed.Logging does **not** run migrations. The DDL ships as install scripts under [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/). Copies are also embedded in [the skill's reference](../../skills/code-roadbed-csharp/references/reference-roadbed-logging.md#ddl-install-scripts) so an AI assistant can paste them straight into a host setup script.

Before executing, substitute the literal token `{SchemaPrefix}` with either the empty string (unqualified tables) or a database name followed by a dot (`ops.`). The default `LoggingOptions.Schema` is the empty string, which makes `Schema = "ops"` an explicit opt-in for MySQL production and keeps SQLite-dev frictionless.

Retention:

- **MySQL** — schedule a monthly job that (a) pre-creates next month's partition and (b) drops partitions whose range is older than the retention window. Roadbed.Logging does not ship the partition routine; build it as a stored proc or a Roadbed.Scheduling job. The 90-day default is per the plan; the partition layout makes that a `DROP PARTITION` instead of a table scan.
- **SQLite** — `DELETE FROM {schema}log_entries WHERE event_time_utc < datetime('now', '-90 days');` on the same cadence. Follow with `VACUUM` (or set `PRAGMA auto_vacuum = INCREMENTAL` plus periodic `incremental_vacuum`) to reclaim disk.

---

## Implementation Walkthrough

A consuming Quartz job that opens an activity, heartbeats, records lineage, and finalizes:

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
        string activityId = Ulid.NewUlid().ToString();   // app owns the ULID dep

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
            int rows = 0;
            await foreach (var batch in this._loader.StreamAsync(activityId, cancellationToken))
            {
                rows += await this._loader.WriteAsync(batch, cancellationToken);
                await this._activities.HeartbeatAsync(activityId, cancellationToken);
            }

            // Record the Bronze inputs this Silver run consumed.
            foreach (var inputId in this._loader.ConsumedActivityIds)
            {
                await this._activities.AddInputAsync(activityId, inputId, "bronze", cancellationToken);
            }

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

Inside the loader, every `this._logger.LogInformation(...)` call inherits the ambient `activity_id`, `trace_id`, and `span_id` and lands in `log_entries` with those columns populated.

---

## Common Pitfalls

**Disposing the scope without calling `CompleteAsync`.** The row stays in `running` forever. Always finalize explicitly with `CompleteAsync` or `FailAsync`.

**Passing `LoggingActivityStatus.Failed` to `CompleteAsync`.** Throws `ArgumentException`. Use `FailAsync(activityId, exception)` — it also records the message and type.

**Setting `Schema = "ops"` against SQLite without `ATTACH`.** The SQL becomes `INSERT INTO ops.activity ...`, which SQLite rejects. Either leave `Schema` empty for SQLite or ATTACH the file under that alias.

**Generating ULIDs inside Roadbed.Logging.** The library never generates identifiers — the caller mints the ULID and passes it on `BeginAsync`, and the same identifier is what `IAsyncBulkInsertOperation.BulkInsertAsync` should receive for the run's Bronze/Silver writes. Mints inside the library would orphan the lineage.

**Adding `Roadbed.Crud` as a dependency.** The "B" tier and the log writer look superficially similar but stamp `activity_id` differently. Roadbed.Crud's `BulkInsertAsync(string activityId, IList<TEntity>, ct)` stamps every row with one uniform identifier — correct for Bronze/Silver loads where the load's activity *is* each row's lineage. The log writer stamps each row with its **own originating** `activity_id` because a batch of log rows mixes entries from different scopes.

**Logging from a category that overlaps `RecursionGuardCategories`.** The exporter silently drops those records to prevent the database write path from logging through itself. Either accept the drop or use a different category for genuine operator-visible diagnostics.

**Treating logs as a substitute for the activity row.** Logs are per-event narrative; the activity row is the run record. Always `BeginAsync` at the start of a meaningful unit of work — heartbeats, lineage, and terminal status all hang off the activity row.

**Expecting `LoggingActivityScope.Dispose` to be async.** It is not. The dispose path stops the diagnostic Activity and pops the MEL scope synchronously; persistence is the caller's job via the explicit terminal methods.

---

## Quick Reference

| Need                                                            | Use                                                                                                |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Insert a run record                                             | `await service.BeginAsync(request, ct)`                                                            |
| Stamp `last_heartbeat_on`                                       | `await service.HeartbeatAsync(activityId, ct)`                                                     |
| Patch current-state columns (target, metrics, Quartz, …)        | `await service.UpdateAsync(updateRequest, ct)`                                                     |
| Finish successfully                                             | `await service.CompleteAsync(activityId, LoggingActivityStatus.Succeeded, recordsImpacted: n, ct)` |
| Finish on exception                                             | `await service.FailAsync(activityId, exception, ct)`                                               |
| Mark canceled / skipped                                         | `await service.CompleteAsync(activityId, LoggingActivityStatus.Canceled, ct)`                      |
| Record a lineage edge                                           | `await service.AddInputAsync(consumerId, inputId, inputRole, ct)`                                  |
| Wire MEL → OTel → DB                                            | `builder.Logging.AddRoadbedDbLogging()`                                                            |
| Switch to back-pressure on overflow                             | `new LoggingOptions { ChannelFullPolicy = LoggingChannelFullPolicy.BlockBriefly }`                  |
| Quiet a noisy third-party category                              | `options.RecursionGuardCategories.Add("ThirdParty.Noisy")`                                          |
| Read the schema scripts                                         | [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/)                     |
