# Plan: Roadbed.Logging

Status: proposed — ready for implementation.
Audience: the Roadbed coding agent.

A new `Roadbed.Logging` library that gives every consuming application (a
portfolio of console apps + web apps, spread across separate git repos) one
shared, turnkey way to:

1. Persist `Microsoft.Extensions.Logging` (MEL) output as **structured** rows in
   a database, and
2. Record **activities** (run instances of jobs, pipelines, ad-hoc work) that
   both data-lineage rows and log lines tie back to — replacing per-app
   bespoke "ingestion_batch"-style tables.

It is **OpenTelemetry-first**: logging flows through the OTel logging pipeline
and a custom batching **exporter**, so DB persistence is one exporter and OTLP
export (Grafana/Tempo/Jaeger/etc.) is a later config add, not a rewrite.

`Roadbed.Logging` is **self-contained** — it does **not** depend on
`Roadbed.Crud`. It persists via `Roadbed.Data` directly, following the Roadbed
repository-service pattern (see `docs/pattern-repository-service.md`), and the
high-volume log write is an internal custom bulk insert (see §4).

---

## 1. Why this exists (context)

These decisions were settled before this plan and must be honored, not
re-litigated:

- **One central, shared model.** Multiple console apps in different repos run
  Quartz.NET jobs and need a common place to record run activity + logs. Build
  it once in Roadbed; reuse everywhere.
- **`activity` replaces `ingestion_batch`.** The old model put `source_id` on
  the batch (a Bronze-only idea) and forced Silver runs to invent a synthetic
  "source". The generic `activity` record fixes that and adds first-class
  hierarchy + lineage.
- **`activity_id` is an opaque `string` (ULID in practice), supplied by the
  caller.** Roadbed.Logging does **not** generate ids. The consuming app mints
  the activity ULID — the same id it passes to
  `IAsyncBulkInsertOperation.BulkInsertAsync` for its Bronze/Silver loads — and
  passes it to `BeginAsync`. Log lines pick it up automatically from the ambient
  `System.Diagnostics.Activity.Current` / logging scope. The app owns ULID
  generation (and any ULID NuGet dependency); Roadbed.Logging just receives the
  string.
- **OpenTelemetry from day one.** Activities map to OTel spans; `trace_id` /
  `span_id` are captured from `Activity.Current`.
- **Soft references between telemetry tables.** No enforced FKs among
  `activity` / `activity_input` / `log_entries` — they are indexed and joined,
  but each table is pruned/partitioned on its own retention schedule.
- **90-day log retention via partition drop.** `log_entries` is RANGE-
  partitioned by month from day one; a monthly maintenance job drops partitions
  older than 90 days.

### Operation taxonomy note

Roadbed's CRUD operation abbreviation is **CRUDALBT** — Create, Read, Update,
Delete, **Archive**, List, **BulkInsert**, **Truncate**. The just-shipped "B"
(`IAsyncBulkInsertOperation.BulkInsertAsync(string activityId, IList<TEntity>, ct)`)
**tags every row with one uniform `activityId`** — correct for Bronze/Silver
loads, but **not** how `log_entries` is written (see §4).

### Naming (locked)

| Table | Entity (C# class) | Notes |
|---|---|---|
| `activity` | `LoggingActivity` | Run/lineage record. Entity is **not** named `Activity` to avoid colliding with `System.Diagnostics.Activity`. |
| `activity_input` | `LoggingActivityInput` | Lineage DAG edges. |
| `log_entries` | `LoggingLogEntry` | Structured log rows. |

The `activity` / `activity_id` *nomenclature* (table + columns) is kept
everywhere; only the C# class names carry the `Logging` prefix. The library
namespace is `Roadbed.Logging`.

---

## 2. Architecture overview

```
Application code → ILogger (MEL)
        │
        ├─(logs)→ OpenTelemetryLoggerProvider
        │             → BatchLogRecordProcessor              (batching, provided by OTel)
        │             → RoadbedDbLogRecordExporter           (NEW: maps LogRecord → LoggingLogEntry,
        │                                                      reading per-row activity_id/trace_id/span_id
        │                                                      from Activity.Current / scope)
        │             → bounded Channel<LoggingLogEntry>     (non-blocking handoff)
        │             → LogWriterHostedService               (drains, batches, INTERNAL custom bulk insert,
        │                                                      flushes on shutdown, falls back to console on DB error)
        │             → log_entries  (each row carries its own originating activity_id — no uniform stamp)
        │
        └─(activities)→ ILoggingActivityService              (NEW)
                          → insert 'running' (caller-supplied ULID), heartbeat, complete/fail
                          → link inputs (activity_input)
                          → activity   (mutable; custom insert + update via Roadbed.Data)
```

Two distinct write paths, deliberately:

- **Activities** are low-volume and **mutable** (insert `running` → heartbeat →
  `succeeded`/`failed`). Custom single-row insert + update via `Roadbed.Data`.
- **Log entries** are high-volume and **append-only**. Buffered through a
  channel and **bulk-inserted in batches** off the hot path by an internal
  custom writer.

---

## 3. Deliverables

### 3.1 Entities

- `LoggingActivity` (id = the caller-supplied ULID `string`).
- `LoggingActivityInput` (composite key; see DDL).
- `LoggingLogEntry` (id = `long`).

Mirror the existing Roadbed entity conventions (regions, XML docs, nullability).
These do **not** need to implement `Roadbed.Crud.IEntity<TId>` since the library
does not use the Crud base classes; use whatever lightweight base the Roadbed
repository-service pattern expects without pulling in `Roadbed.Crud`.

### 3.2 Canonical schema (shipped as install scripts)

Ship the DDL as embedded install scripts (one per table, following the existing
Roadbed repository-pattern install-script convention). The **schema name is
caller-configurable** (apps install into their own `ops`/`platform` schema);
the scripts must not hard-code a schema. Target **MySQL / MariaDB** for the
central store. SQLite may be supported for local/dev (see Open Decisions), but
note SQLite has no native partitioning — retention there is `DELETE`-based.

> All `CHAR(26)` ULID and `CHAR(32)`/`CHAR(16)` trace/span columns use
> `CHARACTER SET ascii COLLATE ascii_bin` so lexical order == chronological
> order and the identifier compares exactly. This is load-bearing for ULID
> sortability.

```sql
-- activity  (entity LoggingActivity) -----------------------------------------
CREATE TABLE activity (
     id                    CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL  -- caller-supplied ULID
    ,parent_activity_id    CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL       -- tree: sub-activity within a run (soft)
    ,root_activity_id      CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL       -- run anchor; == id for a root (soft)
    ,trace_id              CHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL       -- OTel trace id (W3C, 32 hex)
    ,span_id               CHAR(16) CHARACTER SET ascii COLLATE ascii_bin NULL       -- OTel span id (16 hex)
    ,activity_key          VARCHAR(100)  NULL                                         -- logical definition slug; groups runs; non-Quartz too
    ,application           VARCHAR(100)  NOT NULL
    ,environment           VARCHAR(20)   NULL
    ,activity_type         VARCHAR(50)   NOT NULL                                     -- ingestion|transformation|promotion|maintenance|manual...
    ,target                VARCHAR(255)  NULL                                         -- what it acted on (schema.table / dataset)
    ,status                ENUM('pending','running','succeeded','failed','canceled','skipped') NOT NULL DEFAULT 'pending'
    ,started_on            DATETIME(6)   NULL
    ,completed_on          DATETIME(6)   NULL
    ,last_heartbeat_on     DATETIME(6)   NULL                                         -- stale + running == crashed/zombie
    ,records_impacted      BIGINT UNSIGNED NULL                                       -- headline outcome (sum of BulkInsert returns)
    ,parameters            JSON          NULL                                         -- inputs/config (vintage, fiscal year, crosswalk type...)
    ,metrics               JSON          NULL                                         -- detailed outputs (per-table counts, bytes, durations...)
    ,error                 TEXT          NULL
    ,error_type            VARCHAR(255)  NULL
    -- Quartz correlation (snapshotted at fire time; QRTZ_FIRED_TRIGGERS is transient):
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

-- activity_input  (entity LoggingActivityInput) ------------------------------
-- Lineage DAG: "this activity consumed the output of those upstream activities."
CREATE TABLE activity_input (
     activity_id        CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL  -- consumer
    ,input_activity_id  CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NOT NULL  -- upstream input it consumed
    ,input_role         VARCHAR(50) NULL                                         -- 'places','cousubs','hud-centroid'...
    ,created_on         DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
    ,PRIMARY KEY (activity_id, input_activity_id)
    ,KEY idx_activity_input_reverse (input_activity_id)                          -- reverse impact analysis
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- log_entries  (entity LoggingLogEntry) --------------------------------------
-- PK includes event_time_utc because MySQL requires the partition key in every
-- unique key. Partitioned monthly from day one for 90-day retention.
-- activity_id is the ORIGINATING activity, captured per row at log time.
CREATE TABLE log_entries (
     id               BIGINT UNSIGNED NOT NULL AUTO_INCREMENT
    ,event_time_utc   DATETIME(6)   NOT NULL                                      -- when it occurred (app clock, UTC)
    ,recorded_on      DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6)         -- when the row was written
    ,log_level        TINYINT UNSIGNED NOT NULL                                   -- MEL LogLevel: 0=Trace .. 5=Critical
    ,category         VARCHAR(255)  NOT NULL                                      -- logger category (usually class FQN)
    ,event_id         INT           NULL                                          -- MEL EventId.Id
    ,event_name       VARCHAR(255)  NULL                                          -- MEL EventId.Name
    ,message          TEXT          NOT NULL                                      -- rendered message
    ,message_template TEXT          NULL                                          -- unrendered template (for aggregation)
    ,properties       JSON          NULL                                          -- structured args {"RowCount":982000,...}
    ,exception        TEXT          NULL
    ,exception_type   VARCHAR(255)  NULL
    ,activity_id      CHAR(26) CHARACTER SET ascii COLLATE ascii_bin NULL         -- ORIGINATING activity (soft ref) — per row
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

Ship a **partition-maintenance routine** (a SQL proc or documented script) that
(a) pre-creates next month's partition and (b) drops partitions whose range is
older than 90 days. Apps schedule it via their own Quartz job.

### 3.3 The OTel logging exporter + batch pipeline

- `RoadbedDbLogRecordExporter : BaseExporter<LogRecord>` — maps each `LogRecord`
  to a `LoggingLogEntry`, capturing: level (numeric), category, `EventId`,
  rendered `FormattedMessage`, the **message template** (`{OriginalFormat}`) and
  **named args** (→ `properties` JSON) from `LogRecord.Attributes`, exception +
  type, and `TraceId`/`SpanId` (OTel populates these from `Activity.Current`).
  Resolve the **originating** `activity_id` from the ambient activity / scope
  (see 3.4) and write it **per row**.
- Use OTel's `BatchLogRecordProcessor` for batching, OR a bounded
  `System.Threading.Channels.Channel<LoggingLogEntry>` + a
  `LogWriterHostedService : BackgroundService` that drains, batches (by count or
  time window), performs the **internal custom bulk insert** (see §4),
  **flushes on `StopAsync`**, and on DB failure **falls back to a non-DB sink**
  (console/file) so logs are never silently lost and a DB outage never wedges
  the app.
- **Recursion safety is structural:** the writer's own diagnostics must not flow
  back through the DB exporter (use a separate internal/console logger). The
  channel hand-off already decouples produce from consume; also filter the
  library's own categories and the data-access categories as a backstop.
- Honor MEL/OTel level filtering via configuration — **do not** hard-code a
  Warning+ threshold in code.
- Provide a one-call registration extension, e.g.
  `builder.Logging.AddRoadbedDbLogging(options => …)` (and/or
  `services.AddRoadbedLogging(...)`), that wires the OTel provider, processor,
  exporter, channel, and hosted writer.

### 3.4 The activity API

`ILoggingActivityService` / `LoggingActivityService` providing:

- `BeginAsync(string activityId, …)` → inserts `activity` as `running` using the
  **caller-supplied** ULID (capturing application/environment/host/process_id,
  `activity_key`, `activity_type`, Quartz fields when present, and
  `trace_id`/`span_id` from `Activity.Current`), and pushes the id onto the
  ambient scope so subsequent log lines pick it up. Returns a disposable scope.
- `HeartbeatAsync(activityId)` → updates `last_heartbeat_on`.
- `CompleteAsync(activityId, status, recordsImpacted, metrics?)` /
  `FailAsync(activityId, error)` → terminal update.
- `AddInputAsync(activityId, inputActivityId, role?)` → inserts an
  `activity_input` edge.

`LoggingActivity` is **mutable** → its repository uses custom insert + update
via `Roadbed.Data` (the repository-service pattern, **not** the Crud base
classes). `LoggingActivityInput` uses a simple custom insert.

> Roadbed.Logging does **not** generate ids. `BeginAsync` requires the caller to
> pass the ULID it already minted. Logs emitted outside any activity simply land
> with `activity_id = NULL`.

---

## 4. The log write path: internal custom bulk insert (no Roadbed.Crud)

`log_entries` is written by an **internal custom bulk insert inside the
Roadbed.Logging provider** — it deliberately does **not** use the Roadbed.Crud
"B" operation, and Roadbed.Logging does **not** reference `Roadbed.Crud`.

Why: the CRUDALBT "B" operation,
`BulkInsertAsync(string activityId, IList<TEntity>, ct)`, **stamps one uniform
`activityId` onto every row** (correct for Bronze/Silver, where the load's
activity *is* each row's lineage). A batch of log rows is different — each row
already carries its **own originating** `activity_id` (captured at log time), and
those differ across the batch. There is no single uniform `activityId` to pass,
and the act of flushing logs is not itself an activity worth recording (it would
add one noisy `activity` row per process with no consumer — `application` /
`host` / `process_id` already identify the process, and writer health like a
dropped-row count is better emitted as its own Warning log).

So the writer follows ~95% of the bulk-insert *pattern* — chunked multi-row
`INSERT` via `Roadbed.Data`, sized under the provider's placeholder ceiling,
returning the inserted count — but with **no `activityId` parameter**; each
`LoggingLogEntry` supplies its own `activity_id` column value. Reuse any
chunking/SQL-builder helpers `Roadbed.Data` exposes; do not take a dependency on
`Roadbed.Crud` just for this.

### Dependencies

- `Roadbed.Data` (+ `Roadbed.Data.MySql`, optionally `Roadbed.Data.Sqlite`) — execution.
- `Roadbed.Common` — shared base utilities.
- `Microsoft.Extensions.Logging.Abstractions`.
- `OpenTelemetry` + `OpenTelemetry.Extensions.Hosting`.
- **Not** `Roadbed.Crud`. **No** ULID/GUID package (the library generates no ids).

---

## 5. Key design decisions (locked)

1. OTel-first: MEL → OTel provider → batch processor → custom DB exporter.
2. `activity` replaces `ingestion_batch`; caller-supplied ULID `id`; no `job`
   table (don't duplicate Quartz `QRTZ_JOB_DETAILS` — use `activity_key` +
   snapshotted `quartz_*` columns, because `QRTZ_FIRED_TRIGGERS` is transient).
3. Soft references (indexed, not FK-enforced) among the three tables.
4. `log_entries` partitioned monthly from day one; 90-day retention via
   `DROP PARTITION`.
5. `trace_id` + `span_id` nullable on both `activity` and `log_entries`,
   captured from `Activity.Current`.
6. `activity` is mutable (`last_modified_on ON UPDATE`, `last_heartbeat_on`);
   no `_audit` mirror (it is itself the history record).
7. ULID columns are `ascii_bin`; the activity timestamp columns follow the
   `_on` house convention; the log event instant keeps the explicit `_utc`.
8. The log write path is non-blocking, batched, flush-on-shutdown, with a
   non-DB fallback; recursion safety is structural; it is an **internal custom
   bulk insert** (no Roadbed.Crud, no `activityId` param).
9. `records_impacted` is a first-class column (sum of `BulkInsertAsync`
   returns on the consuming app's data loads); finer metrics live in `metrics`
   JSON.
10. Roadbed.Logging **generates no ids** and **does not depend on Roadbed.Crud**;
    callers supply ULIDs (the app owns id generation and any ULID NuGet).

---

## 6. Open decisions for the agent

1. **DB targets:** MySQL/MariaDB only, or also SQLite (no native partitioning →
   `DELETE`-based retention for SQLite)?
2. **Batch tuning defaults:** batch size, flush interval, channel bound, and the
   drop policy when the channel is full (drop-oldest vs. block briefly).
3. **Schema placement:** confirm the install scripts stay schema-name-agnostic
   so each app installs into its own `ops`/`platform` schema.
4. **Chunking helper reuse:** whether `Roadbed.Data` already exposes a multi-row
   INSERT / chunking helper the writer can reuse, or whether the writer carries
   its own small chunker (still without referencing `Roadbed.Crud`).

(Resolved earlier: the log writer is internal/custom with no `activityId`
param; Roadbed.Logging takes no Roadbed.Crud dependency and generates no ids.)

---

## 7. Out of scope

- No app-specific tables or domain logic.
- No replacement of Quartz's own `QRTZ_*` store.
- No metrics/traces *export* beyond logs in v1 (OTLP trace export can be added
  later as another exporter — the schema and id capture already allow it).
- Migrating existing apps off `ingestion_batch` is the consuming app's task
  (Pebble), not this library's.

---

## 8. Acceptance criteria

- `Roadbed.Logging` builds under the solution's analyzer / warnings-as-errors
  gate with no new analyzer suppressions beyond existing conventions, and with
  **no reference to `Roadbed.Crud`**.
- One-call registration wires the full pipeline; a sample console app emits a
  log that lands in `log_entries` with populated `message_template`,
  `properties`, `activity_id` (the originating activity), `trace_id`, and
  `span_id`.
- An activity lifecycle (begin with a caller-supplied ULID → heartbeat →
  complete) round-trips, and an `activity_input` edge can be recorded and
  queried (forward + reverse).
- Log writes are batched and off the hot path via the internal custom writer; a
  forced DB outage falls back to the console sink without throwing into caller
  code; shutdown flushes the buffer.
- Unit tests mirror the existing Roadbed.Test.Unit coverage style for the
  exporter mapping, the internal bulk writer, and the activity service.

---

## 9. Skill update

When the library lands, update the Roadbed skill (`skills/code-roadbed-csharp`,
or add a focused logging section / companion skill) to document: how to register
Roadbed.Logging, the activity lifecycle API (caller supplies the ULID), the
`LoggingActivity` / `LoggingActivityInput` / `LoggingLogEntry` entities, and the
fact that `log_entries` is written by an internal custom bulk insert (per-row
`activity_id`, no Roadbed.Crud dependency) — distinct from the CRUDALBT "B"
operation used for uniform-`activityId` Bronze/Silver loads. Include the
retention/partition maintenance routine.
