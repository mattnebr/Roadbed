namespace Roadbed.Logging;

using System;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a row in the <c>activity</c> table — one run instance of a
/// job, pipeline, or ad-hoc unit of work.
/// </summary>
/// <remarks>
/// <para>
/// The entity is mutable: a single row is inserted as <see cref="LoggingActivityStatus.Running"/>
/// at the start of the run, patched as the run progresses (heartbeats,
/// curated current-state updates), and finalized with a terminal status
/// when the run completes.
/// </para>
/// <para>
/// Identifier values are <strong>caller-supplied ULIDs</strong> — Roadbed.Logging
/// does not generate them. The opaque string carried in <see cref="Id"/> is
/// expected to be lexically chronological so that range scans on the
/// <c>activity</c> table align with insertion order without an auxiliary
/// timestamp index.
/// </para>
/// </remarks>
public sealed class LoggingActivity
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the caller-supplied ULID identifying this activity row.
    /// </summary>
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional parent activity that started this one.
    /// </summary>
    /// <remarks>
    /// Soft reference. Used to express run hierarchies; not foreign-key
    /// enforced.
    /// </remarks>
    [Column("parent_activity_id")]
    public string? ParentActivityId { get; set; }

    /// <summary>
    /// Gets or sets the optional root activity anchoring the run this row belongs to.
    /// </summary>
    /// <remarks>
    /// For a root activity, <see cref="RootActivityId"/> equals <see cref="Id"/>.
    /// </remarks>
    [Column("root_activity_id")]
    public string? RootActivityId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry trace identifier (32 hexadecimal characters) snapshotted at begin time.
    /// </summary>
    [Column("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry span identifier (16 hexadecimal characters) snapshotted at begin time.
    /// </summary>
    [Column("span_id")]
    public string? SpanId { get; set; }

    /// <summary>
    /// Gets or sets the logical activity definition slug used to group multiple runs of the same job.
    /// </summary>
    /// <remarks>
    /// For Quartz jobs this is typically the job's logical name. For non-Quartz
    /// activities it is a stable string the application owns (e.g.
    /// <c>"silver.places.full-refresh"</c>).
    /// </remarks>
    [Column("activity_key")]
    public string? ActivityKey { get; set; }

    /// <summary>
    /// Gets or sets the name of the application running this activity.
    /// </summary>
    [Column("application")]
    public string Application { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment environment label (e.g. <c>dev</c>, <c>prod</c>).
    /// </summary>
    [Column("environment")]
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the activity category, persisted as a lowercase string.
    /// </summary>
    /// <remarks>
    /// See <see cref="LoggingActivityType"/> for well-known values. Free-form
    /// strings are accepted because the column is a VARCHAR rather than an
    /// ENUM.
    /// </remarks>
    [Column("activity_type")]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource the activity acted on (e.g. <c>schema.table</c>, dataset name, URI).
    /// </summary>
    [Column("target")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the activity.
    /// </summary>
    [Column("status")]
    public LoggingActivityStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the moment the run began (UTC).
    /// </summary>
    [Column("started_on")]
    public DateTime? StartedOn { get; set; }

    /// <summary>
    /// Gets or sets the moment the run finished (UTC).
    /// </summary>
    [Column("completed_on")]
    public DateTime? CompletedOn { get; set; }

    /// <summary>
    /// Gets or sets the moment of the most recent heartbeat (UTC).
    /// </summary>
    /// <remarks>
    /// A stale value combined with <see cref="LoggingActivityStatus.Running"/>
    /// indicates a crashed or zombie process.
    /// </remarks>
    [Column("last_heartbeat_on")]
    public DateTime? LastHeartbeatOn { get; set; }

    /// <summary>
    /// Gets or sets the headline count of records produced or affected.
    /// </summary>
    /// <remarks>
    /// Typically the sum of values returned from
    /// <c>IAsyncBulkInsertOperation.BulkInsertAsync</c> calls executed during
    /// the run. Finer-grained metrics belong in <see cref="MetricsJson"/>.
    /// </remarks>
    [Column("records_impacted")]
    public long? RecordsImpacted { get; set; }

    /// <summary>
    /// Gets or sets the input parameters of the run, serialized as JSON.
    /// </summary>
    [Column("parameters")]
    public string? ParametersJson { get; set; }

    /// <summary>
    /// Gets or sets the per-run output metrics, serialized as JSON.
    /// </summary>
    [Column("metrics")]
    public string? MetricsJson { get; set; }

    /// <summary>
    /// Gets or sets the rendered exception message captured on failure.
    /// </summary>
    [Column("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the fully-qualified type name of the failure exception.
    /// </summary>
    [Column("error_type")]
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the Quartz scheduler instance identifier that owned this fire, when applicable.
    /// </summary>
    [Column("scheduler_instance_id")]
    public string? SchedulerInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the Quartz fire instance identifier, when applicable.
    /// </summary>
    /// <remarks>
    /// Snapshotted here because Quartz's <c>QRTZ_FIRED_TRIGGERS</c> table is
    /// transient and is cleared once the trigger completes.
    /// </remarks>
    [Column("fire_instance_id")]
    public string? FireInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the Quartz job name, when applicable.
    /// </summary>
    [Column("quartz_job_name")]
    public string? QuartzJobName { get; set; }

    /// <summary>
    /// Gets or sets the Quartz job group, when applicable.
    /// </summary>
    [Column("quartz_job_group")]
    public string? QuartzJobGroup { get; set; }

    /// <summary>
    /// Gets or sets the Quartz trigger name, when applicable.
    /// </summary>
    [Column("quartz_trigger_name")]
    public string? QuartzTriggerName { get; set; }

    /// <summary>
    /// Gets or sets the Quartz trigger group, when applicable.
    /// </summary>
    [Column("quartz_trigger_group")]
    public string? QuartzTriggerGroup { get; set; }

    /// <summary>
    /// Gets or sets the machine host name the activity ran on.
    /// </summary>
    [Column("host")]
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the operating-system process identifier that ran the activity.
    /// </summary>
    [Column("process_id")]
    public int? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the optional user identifier responsible for triggering the activity.
    /// </summary>
    [Column("created_by")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the moment this row was originally inserted (UTC).
    /// </summary>
    /// <remarks>
    /// Defaulted server-side via <c>CURRENT_TIMESTAMP(6)</c>.
    /// </remarks>
    [Column("created_on")]
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the moment this row was last modified (UTC).
    /// </summary>
    /// <remarks>
    /// Maintained server-side via <c>ON UPDATE CURRENT_TIMESTAMP(6)</c> in MySQL;
    /// patched explicitly in SQLite.
    /// </remarks>
    [Column("last_modified_on")]
    public DateTime LastModifiedOn { get; set; }

    #endregion Public Properties
}
