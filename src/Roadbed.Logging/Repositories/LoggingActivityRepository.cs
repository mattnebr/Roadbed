namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
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

    /// <summary>
    /// Lowercase wire value of <see cref="LoggingActivityStatus.Running"/>,
    /// matching the <c>activity.status</c> column's stored form.
    /// </summary>
    private static readonly string RunningStatusValue =
        LoggingActivityStatus.Running.ToString().ToLowerInvariant();

    private readonly ILoggingDataExecutor _executor;
    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityRepository"/> class.
    /// </summary>
    /// <param name="executor">Provider-neutral execution port supplied by the active provider satellite.</param>
    /// <param name="factory">Database connection factory pointing at the activity schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingActivityRepository(
        ILoggingDataExecutor executor,
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingActivityRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._executor = executor;
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

        // created_on / last_modified_on are stamped explicitly with UTC
        // values supplied by the caller (DateTime.UtcNow from
        // LoggingActivityService.BeginAsync) so we never rely on the
        // session time_zone of the underlying connection. The values
        // chosen here are what LoggingActivityScope.CreatedOn returns,
        // so subsequent UPDATE WHEREs can include AND created_on = ? and
        // prune to one MySQL partition.
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
                ,created_on
                ,last_modified_on
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
                ,@CreatedOn
                ,@LastModifiedOn
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
                entity.CreatedOn,
                entity.LastModifiedOn,
            },
        };

        await this._executor
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

        string whereClause = BuildWhereClause(request.CreatedOn);

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
                ,last_modified_on      = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", request.ActivityId);
        parameters.Add("ActivityKey", request.ActivityKey);
        parameters.Add("ActivityType", request.ActivityType);
        parameters.Add("Target", request.Target);
        parameters.Add("ParametersJson", request.ParametersJson);
        parameters.Add("MetricsJson", request.MetricsJson);
        parameters.Add("RecordsImpacted", request.RecordsImpacted);
        parameters.Add("SchedulerInstanceId", request.SchedulerInstanceId);
        parameters.Add("FireInstanceId", request.FireInstanceId);
        parameters.Add("QuartzJobName", request.QuartzJobName);
        parameters.Add("QuartzJobGroup", request.QuartzJobGroup);
        parameters.Add("QuartzTriggerName", request.QuartzTriggerName);
        parameters.Add("QuartzTriggerGroup", request.QuartzTriggerGroup);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (request.CreatedOn.HasValue)
        {
            parameters.Add("CreatedOn", request.CreatedOn.Value);
        }

        var executorRequest = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(executorRequest, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RecordHeartbeatAsync(
        string activityId,
        DateTime? createdOn,
        DateTime heartbeatOn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 last_heartbeat_on = @HeartbeatOn
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("HeartbeatOn", heartbeatOn);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(
        string activityId,
        DateTime? createdOn,
        LoggingActivityStatus status,
        DateTime completedOn,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @CompletedOn
                ,records_impacted  = COALESCE(@RecordsImpacted, records_impacted)
                ,metrics           = COALESCE(@MetricsJson,     metrics)
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("Status", status.ToString().ToLowerInvariant());
        parameters.Add("CompletedOn", completedOn);
        parameters.Add("RecordsImpacted", recordsImpacted);
        parameters.Add("MetricsJson", metricsJson);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FailAsync(
        string activityId,
        DateTime? createdOn,
        DateTime completedOn,
        string error,
        string errorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errorType);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @CompletedOn
                ,error             = @Error
                ,error_type        = @ErrorType
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("Status", LoggingActivityStatus.Failed.ToString().ToLowerInvariant());
        parameters.Add("CompletedOn", completedOn);
        parameters.Add("Error", error);
        parameters.Add("ErrorType", errorType);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> FindStaleAsync(
        string application,
        string? environment,
        DateTime staleBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(application);

        string environmentClause = BuildEnvironmentClause(environment);

        // "Last sign of life" is the most recent of last_heartbeat_on,
        // started_on, created_on. COALESCE picks the first non-null in that
        // order; created_on is NOT NULL so the chain always resolves, which
        // keeps a just-begun run (recent created_on, null heartbeat) out of
        // the stale set.
        string sql = $@"
            SELECT id
            FROM {this._tableRef}
            WHERE status = @RunningStatus
              AND application = @Application{environmentClause}
              AND COALESCE(last_heartbeat_on, started_on, created_on) < @StaleBefore
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("RunningStatus", RunningStatusValue);
        parameters.Add("Application", application);
        parameters.Add("StaleBefore", staleBeforeUtc);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            parameters.Add("Environment", environment);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        IEnumerable<string> ids = await this._executor
            .QueryAsync<string>(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);

        return ids.ToList();
    }

    /// <inheritdoc/>
    public async Task ReapAsync(
        IReadOnlyList<string> activityIds,
        string application,
        string? environment,
        LoggingActivityStatus terminalStatus,
        DateTime reapedOnUtc,
        string? metricsJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activityIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(application);

        if (activityIds.Count == 0)
        {
            return;
        }

        string environmentClause = BuildEnvironmentClause(environment);

        // Set-based: one UPDATE reaps every supplied id. The WHERE re-asserts
        // status = running (so a row that finalized between find and reap is
        // left alone) and re-asserts application/environment as a defense-in-
        // depth guard against ever touching another application's row.
        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @ReapedOn
                ,metrics           = @MetricsJson
                ,last_modified_on  = @ReapedOn
            WHERE id IN @ActivityIds
              AND status = @RunningStatus
              AND application = @Application{environmentClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("Status", terminalStatus.ToString().ToLowerInvariant());
        parameters.Add("ReapedOn", reapedOnUtc);
        parameters.Add("MetricsJson", metricsJson);
        parameters.Add("ActivityIds", activityIds);
        parameters.Add("RunningStatus", RunningStatusValue);
        parameters.Add("Application", application);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            parameters.Add("Environment", environment);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Builds the WHERE clause used by every activity UPDATE.
    /// </summary>
    /// <param name="createdOn">
    /// When supplied, the clause includes
    /// <c>AND created_on = @CreatedOn</c> so MySQL prunes the UPDATE to
    /// the single monthly partition that owns the row. When <c>null</c>,
    /// the clause filters only on <c>id</c> and the UPDATE has to probe
    /// every defined partition.
    /// </param>
    /// <returns>A <c>WHERE</c> clause string ready for SQL concatenation.</returns>
    /// <remarks>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</remarks>
    internal static string BuildWhereClause(DateTime? createdOn)
    {
        return createdOn.HasValue
            ? "WHERE id = @ActivityId AND created_on = @CreatedOn"
            : "WHERE id = @ActivityId";
    }

    /// <summary>
    /// Builds the optional environment predicate appended to the reaper's
    /// find/reap statements.
    /// </summary>
    /// <param name="environment">
    /// When non-blank, the caller's configured environment. A blank value
    /// (the default when <see cref="LoggingOptions.Environment"/> is unset)
    /// suppresses environment filtering so the sweep covers every environment
    /// of the application.
    /// </param>
    /// <returns><c>" AND environment = @Environment"</c> when an environment is supplied; otherwise an empty string.</returns>
    /// <remarks>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</remarks>
    internal static string BuildEnvironmentClause(string? environment)
    {
        return string.IsNullOrWhiteSpace(environment)
            ? string.Empty
            : " AND environment = @Environment";
    }

    #endregion Internal Methods
}
