namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="ILoggingActivityService"/>.
/// </summary>
/// <remarks>
/// Snapshots the host name and process identifier at instance construction
/// so that every <c>BeginAsync</c> call records consistent provenance
/// without making a syscall on every invocation.
/// </remarks>
public sealed class LoggingActivityService
    : BaseClassWithLogging,
      ILoggingActivityService
{
    #region Private Fields

    /// <summary>
    /// Name of the diagnostic <see cref="Activity"/> opened for each run.
    /// </summary>
    private const string ActivityOperationName = "roadbed.logging.activity";

    /// <summary>
    /// Tag key under which the activity identifier is exposed on the
    /// diagnostic <see cref="Activity"/>.
    /// </summary>
    private const string ActivityIdTagKey = "roadbed.activity_id";

    /// <summary>
    /// Scope-state key under which the activity identifier is exposed to the
    /// MEL logging pipeline.
    /// </summary>
    private const string ActivityIdScopeKey = "activity_id";

    private readonly ILoggingActivityRepository _activityRepository;
    private readonly ILoggingActivityInputRepository _activityInputRepository;
    private readonly LoggingOptions _options;
    private readonly string _hostName;
    private readonly int _processId;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public LoggingActivityService(
        ILogger<LoggingActivityService> logger)
        : this(
            ServiceLocator.GetService<ILoggingActivityRepository>(),
            ServiceLocator.GetService<ILoggingActivityInputRepository>(),
            ServiceLocator.GetService<LoggingOptions>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityService"/> class.
    /// </summary>
    /// <param name="activityRepository">Repository for the <c>activity</c> table.</param>
    /// <param name="activityInputRepository">Repository for the <c>activity_input</c> lineage table.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal LoggingActivityService(
        ILoggingActivityRepository activityRepository,
        ILoggingActivityInputRepository activityInputRepository,
        LoggingOptions options,
        ILogger<LoggingActivityService> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(activityRepository);
        ArgumentNullException.ThrowIfNull(activityInputRepository);
        ArgumentNullException.ThrowIfNull(options);

        this._activityRepository = activityRepository;
        this._activityInputRepository = activityInputRepository;
        this._options = options;
        this._hostName = Environment.MachineName;
        this._processId = Environment.ProcessId;
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<LoggingActivityScope> BeginAsync(
        LoggingActivityBeginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        // Start a diagnostic Activity *before* the INSERT so that the row
        // can carry trace_id / span_id sampled from a Current that already
        // covers the run. The Activity defaults to ActivityIdFormat.W3C
        // when no parent is present, producing W3C trace/span ids
        // automatically.
        Activity? activity = new Activity(ActivityOperationName).Start();
        activity.SetTag(ActivityIdTagKey, request.Id);

        // Push the MEL scope so subsequent ILogger usage inherits the
        // activity_id without callers having to thread it through.
        IDisposable? logScope = this.Logger.BeginScope(
            new Dictionary<string, object>
            {
                [ActivityIdScopeKey] = request.Id,
            });

        var entity = new LoggingActivity
        {
            Id = request.Id,
            ParentActivityId = request.ParentActivityId,
            RootActivityId = request.RootActivityId ?? request.Id,
            TraceId = activity.TraceId.ToHexString(),
            SpanId = activity.SpanId.ToHexString(),
            ActivityKey = request.ActivityKey,
            Application = request.Application ?? this._options.Application,
            Environment = request.Environment ?? this._options.Environment,
            ActivityType = request.ActivityType ?? LoggingActivityType.Unknown.ToString().ToLowerInvariant(),
            Target = request.Target,
            Status = LoggingActivityStatus.Running,
            StartedOn = DateTime.UtcNow,
            LastHeartbeatOn = DateTime.UtcNow,
            ParametersJson = request.ParametersJson,
            SchedulerInstanceId = request.SchedulerInstanceId,
            FireInstanceId = request.FireInstanceId,
            QuartzJobName = request.QuartzJobName,
            QuartzJobGroup = request.QuartzJobGroup,
            QuartzTriggerName = request.QuartzTriggerName,
            QuartzTriggerGroup = request.QuartzTriggerGroup,
            Host = this._hostName,
            ProcessId = this._processId,
            CreatedBy = request.CreatedBy,
        };

        try
        {
            await this._activityRepository
                .InsertAsync(entity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Roll back the scope/activity we opened so the caller is not
            // left with an ambient activity_id pointing at a row that does
            // not exist.
            logScope?.Dispose();
            activity.Dispose();
            throw;
        }

        return new LoggingActivityScope(request.Id, activity, logScope);
    }

    /// <inheritdoc/>
    public Task HeartbeatAsync(
        string activityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        return this._activityRepository.RecordHeartbeatAsync(
            activityId,
            DateTime.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActivityId);

        return this._activityRepository.UpdateCurrentStateAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        if (status == LoggingActivityStatus.Failed)
        {
            throw new ArgumentException(
                $"Use {nameof(this.FailAsync)} to record an exception-driven failure.",
                nameof(status));
        }

        return this._activityRepository.CompleteAsync(
            activityId,
            status,
            DateTime.UtcNow,
            recordsImpacted,
            metricsJson,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task FailAsync(
        string activityId,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(error);

        return this._activityRepository.FailAsync(
            activityId,
            DateTime.UtcNow,
            error.Message,
            error.GetType().FullName ?? error.GetType().Name,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task AddInputAsync(
        string activityId,
        string inputActivityId,
        string? inputRole = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputActivityId);

        var entity = new LoggingActivityInput
        {
            ActivityId = activityId,
            InputActivityId = inputActivityId,
            InputRole = inputRole,
        };

        return this._activityInputRepository.InsertAsync(entity, cancellationToken);
    }

    #endregion Public Methods
}
