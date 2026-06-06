namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Writes and patches rows in the <c>activity</c> table.
/// </summary>
internal sealed class LoggingActivityRepository
    : BaseClassWithLogging,
      ILoggingActivityRepository
{
    #region Private Fields

    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityRepository"/> class.
    /// </summary>
    /// <param name="factory">Database connection factory pointing at the activity schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingActivityRepository(
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingActivityRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._factory = factory;
        this._tableRef = string.IsNullOrWhiteSpace(options.Schema)
            ? "activity"
            : $"{options.Schema}.activity";
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task InsertAsync(
        LoggingActivity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);

        string sql = $@"
            INSERT INTO {this._tableRef}
            (
                 id
                ,parent_activity_id
                ,root_activity_id
                ,trace_id
                ,span_id
                ,activity_key
                ,application
                ,environment
                ,activity_type
                ,target
                ,status
                ,started_on
                ,last_heartbeat_on
                ,parameters
                ,scheduler_instance_id
                ,fire_instance_id
                ,quartz_job_name
                ,quartz_job_group
                ,quartz_trigger_name
                ,quartz_trigger_group
                ,host
                ,process_id
                ,created_by
            )
            VALUES
            (
                 @Id
                ,@ParentActivityId
                ,@RootActivityId
                ,@TraceId
                ,@SpanId
                ,@ActivityKey
                ,@Application
                ,@Environment
                ,@ActivityType
                ,@Target
                ,@Status
                ,@StartedOn
                ,@LastHeartbeatOn
                ,@ParametersJson
                ,@SchedulerInstanceId
                ,@FireInstanceId
                ,@QuartzJobName
                ,@QuartzJobGroup
                ,@QuartzTriggerName
                ,@QuartzTriggerGroup
                ,@Host
                ,@ProcessId
                ,@CreatedBy
            )
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                entity.Id,
                entity.ParentActivityId,
                entity.RootActivityId,
                entity.TraceId,
                entity.SpanId,
                entity.ActivityKey,
                entity.Application,
                entity.Environment,
                entity.ActivityType,
                entity.Target,
                Status = entity.Status.ToString().ToLowerInvariant(),
                entity.StartedOn,
                entity.LastHeartbeatOn,
                entity.ParametersJson,
                entity.SchedulerInstanceId,
                entity.FireInstanceId,
                entity.QuartzJobName,
                entity.QuartzJobGroup,
                entity.QuartzTriggerName,
                entity.QuartzTriggerGroup,
                entity.Host,
                entity.ProcessId,
                entity.CreatedBy,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateCurrentStateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActivityId);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 activity_key          = COALESCE(@ActivityKey,          activity_key)
                ,activity_type         = COALESCE(@ActivityType,         activity_type)
                ,target                = COALESCE(@Target,               target)
                ,parameters            = COALESCE(@ParametersJson,       parameters)
                ,metrics               = COALESCE(@MetricsJson,          metrics)
                ,records_impacted      = COALESCE(@RecordsImpacted,      records_impacted)
                ,scheduler_instance_id = COALESCE(@SchedulerInstanceId,  scheduler_instance_id)
                ,fire_instance_id      = COALESCE(@FireInstanceId,       fire_instance_id)
                ,quartz_job_name       = COALESCE(@QuartzJobName,        quartz_job_name)
                ,quartz_job_group      = COALESCE(@QuartzJobGroup,       quartz_job_group)
                ,quartz_trigger_name   = COALESCE(@QuartzTriggerName,    quartz_trigger_name)
                ,quartz_trigger_group  = COALESCE(@QuartzTriggerGroup,   quartz_trigger_group)
            WHERE
                id = @ActivityId
            ;";

        var executorRequest = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                request.ActivityId,
                request.ActivityKey,
                request.ActivityType,
                request.Target,
                request.ParametersJson,
                request.MetricsJson,
                request.RecordsImpacted,
                request.SchedulerInstanceId,
                request.FireInstanceId,
                request.QuartzJobName,
                request.QuartzJobGroup,
                request.QuartzTriggerName,
                request.QuartzTriggerGroup,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(executorRequest, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RecordHeartbeatAsync(
        string activityId,
        DateTime heartbeatOn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                last_heartbeat_on = @HeartbeatOn
            WHERE
                id = @ActivityId
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                ActivityId = activityId,
                HeartbeatOn = heartbeatOn,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        DateTime completedOn,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @CompletedOn
                ,records_impacted  = COALESCE(@RecordsImpacted, records_impacted)
                ,metrics           = COALESCE(@MetricsJson,     metrics)
            WHERE
                id = @ActivityId
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                ActivityId = activityId,
                Status = status.ToString().ToLowerInvariant(),
                CompletedOn = completedOn,
                RecordsImpacted = recordsImpacted,
                MetricsJson = metricsJson,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FailAsync(
        string activityId,
        DateTime completedOn,
        string error,
        string errorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errorType);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status        = @Status
                ,completed_on  = @CompletedOn
                ,error         = @Error
                ,error_type    = @ErrorType
            WHERE
                id = @ActivityId
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                ActivityId = activityId,
                Status = LoggingActivityStatus.Failed.ToString().ToLowerInvariant(),
                CompletedOn = completedOn,
                Error = error,
                ErrorType = errorType,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public Methods
}
