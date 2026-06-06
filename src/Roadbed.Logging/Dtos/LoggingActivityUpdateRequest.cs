namespace Roadbed.Logging;

/// <summary>
/// Request carrying the mid-run state changes a caller wants to patch onto
/// an existing <see cref="LoggingActivity"/> row.
/// </summary>
/// <remarks>
/// <para>
/// Every property except <see cref="ActivityId"/> is nullable. Only
/// non-<c>null</c> properties are patched onto the existing row via a
/// <c>COALESCE</c>-style UPDATE — properties left at <c>null</c> preserve
/// the value already stored in the database.
/// </para>
/// <para>
/// This deliberately excludes the lifecycle status, started/completed
/// timestamps, and the heartbeat timestamp, which have dedicated methods
/// on <c>ILoggingActivityService</c>. Use this request type for "current
/// state" columns that may not be known at begin time or that evolve
/// through the run.
/// </para>
/// </remarks>
public sealed class LoggingActivityUpdateRequest
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the identifier of the activity row to patch.
    /// </summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a new <c>activity_key</c> value.
    /// </summary>
    public string? ActivityKey { get; set; }

    /// <summary>
    /// Gets or sets a new <c>activity_type</c> value.
    /// </summary>
    public string? ActivityType { get; set; }

    /// <summary>
    /// Gets or sets a new <c>target</c> value.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets a refreshed <c>parameters</c> JSON value.
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// Gets or sets a refreshed <c>metrics</c> JSON value.
    /// </summary>
    public string? MetricsJson { get; set; }

    /// <summary>
    /// Gets or sets a new <c>records_impacted</c> value.
    /// </summary>
    public long? RecordsImpacted { get; set; }

    /// <summary>
    /// Gets or sets the Quartz scheduler instance identifier.
    /// </summary>
    public string? SchedulerInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the Quartz fire instance identifier.
    /// </summary>
    public string? FireInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the Quartz job name.
    /// </summary>
    public string? QuartzJobName { get; set; }

    /// <summary>
    /// Gets or sets the Quartz job group.
    /// </summary>
    public string? QuartzJobGroup { get; set; }

    /// <summary>
    /// Gets or sets the Quartz trigger name.
    /// </summary>
    public string? QuartzTriggerName { get; set; }

    /// <summary>
    /// Gets or sets the Quartz trigger group.
    /// </summary>
    public string? QuartzTriggerGroup { get; set; }

    #endregion Public Properties
}
