namespace Roadbed.Logging;

/// <summary>
/// Request carrying the values the caller knows at the start of a run, used
/// to insert the first <see cref="LoggingActivity"/> row.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Id"/> property is the caller-supplied ULID; Roadbed.Logging
/// does not generate identifiers. Every other property is optional and may
/// be patched later via <see cref="LoggingActivityUpdateRequest"/> as the
/// run discovers more about itself.
/// </para>
/// <para>
/// The Quartz block is populated when the caller is a Quartz job that wants
/// the fire instance correlated into the lineage table. Roadbed.Logging
/// has no direct Quartz dependency; the job's <c>ExecuteAsync</c>
/// (typically inside a <c>BaseSchedulingJob</c> subclass) is responsible
/// for copying the values out of the Quartz context.
/// </para>
/// </remarks>
public sealed class LoggingActivityBeginRequest
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the caller-supplied ULID identifying the new activity row.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional parent activity that started this one.
    /// </summary>
    public string? ParentActivityId { get; set; }

    /// <summary>
    /// Gets or sets the optional root activity anchoring the run this activity belongs to.
    /// </summary>
    /// <remarks>
    /// When omitted on a root call, the service defaults this to <see cref="Id"/>.
    /// </remarks>
    public string? RootActivityId { get; set; }

    /// <summary>
    /// Gets or sets the logical activity definition slug used to group runs of the same job.
    /// </summary>
    public string? ActivityKey { get; set; }

    /// <summary>
    /// Gets or sets the application name override.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> the value falls back to <see cref="LoggingOptions.Application"/>.
    /// </remarks>
    public string? Application { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment override.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> the value falls back to <see cref="LoggingOptions.Environment"/>.
    /// </remarks>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the activity category, persisted as a lowercase string.
    /// </summary>
    /// <remarks>
    /// May be a member of <see cref="LoggingActivityType"/> stringified or a
    /// free-form value. Required; defaults to <c>"unknown"</c> on the entity
    /// if the caller passes <c>null</c>.
    /// </remarks>
    public string? ActivityType { get; set; }

    /// <summary>
    /// Gets or sets the resource the activity is acting on.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the input parameters for the run, serialized as JSON.
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// Gets or sets the optional user identifier responsible for triggering the activity.
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the Quartz scheduler instance identifier that owned this fire.
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
