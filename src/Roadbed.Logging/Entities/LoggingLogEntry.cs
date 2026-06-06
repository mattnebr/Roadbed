namespace Roadbed.Logging;

using System;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a row in the <c>log_entries</c> table — one structured log
/// entry emitted by the application's <c>Microsoft.Extensions.Logging</c>
/// pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Each entry independently carries its own <see cref="ActivityId"/> sampled
/// at log time, distinct from the uniform-<c>activityId</c> stamping used by
/// the Roadbed.Crud bulk insert tier.
/// </para>
/// <para>
/// The auto-increment <see cref="Id"/> is assigned by the database. The
/// composite primary key in the schema is (<see cref="Id"/>, <see cref="EventTimeUtc"/>)
/// to satisfy MySQL's partitioning requirement.
/// </para>
/// </remarks>
public sealed class LoggingLogEntry
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the database-assigned auto-increment identifier.
    /// </summary>
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the moment the log event occurred, taken from the application's clock (UTC).
    /// </summary>
    [Column("event_time_utc")]
    public DateTime EventTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the moment the row was written to the database (UTC).
    /// </summary>
    /// <remarks>
    /// Defaulted server-side via <c>CURRENT_TIMESTAMP(6)</c>; clients
    /// generally omit it.
    /// </remarks>
    [Column("recorded_on")]
    public DateTime? RecordedOn { get; set; }

    /// <summary>
    /// Gets or sets the numeric Microsoft.Extensions.Logging severity level (0=Trace .. 5=Critical).
    /// </summary>
    [Column("log_level")]
    public byte LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the logger category (usually the source type's fully qualified name).
    /// </summary>
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric portion of the Microsoft.Extensions.Logging EventId.
    /// </summary>
    [Column("event_id")]
    public int? EventId { get; set; }

    /// <summary>
    /// Gets or sets the optional name portion of the Microsoft.Extensions.Logging EventId.
    /// </summary>
    [Column("event_name")]
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the rendered (formatted) log message.
    /// </summary>
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unrendered message template (the <c>{OriginalFormat}</c> attribute), when available.
    /// </summary>
    /// <remarks>
    /// Keeping the template separate from <see cref="Message"/> lets analysts
    /// aggregate log events that share a single template across many
    /// argument values.
    /// </remarks>
    [Column("message_template")]
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Gets or sets the structured named arguments captured with the log event, serialized as JSON.
    /// </summary>
    /// <remarks>
    /// Example for <c>logger.LogInformation("Loaded {RowCount} rows in {Duration}", count, duration)</c>:
    /// <c>{"RowCount": 982000, "Duration": "00:00:14.502"}</c>.
    /// </remarks>
    [Column("properties")]
    public string? PropertiesJson { get; set; }

    /// <summary>
    /// Gets or sets the captured exception message, when an exception was attached.
    /// </summary>
    [Column("exception")]
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets the fully-qualified type name of the captured exception, when present.
    /// </summary>
    [Column("exception_type")]
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the originating activity identifier sampled at log time.
    /// </summary>
    /// <remarks>
    /// <c>NULL</c> when no activity scope was active. Each row may carry a
    /// different value within the same flushed batch.
    /// </remarks>
    [Column("activity_id")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry trace identifier active at log time.
    /// </summary>
    [Column("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry span identifier active at log time.
    /// </summary>
    [Column("span_id")]
    public string? SpanId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application that emitted the entry.
    /// </summary>
    [Column("application")]
    public string Application { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment environment label.
    /// </summary>
    [Column("environment")]
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the machine host name the entry originated from.
    /// </summary>
    [Column("host")]
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the operating-system process identifier the entry originated from.
    /// </summary>
    [Column("process_id")]
    public int? ProcessId { get; set; }

    #endregion Public Properties
}
