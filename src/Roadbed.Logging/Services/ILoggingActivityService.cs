namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Public surface for managing the lifecycle of a row in the <c>activity</c>
/// table.
/// </summary>
/// <remarks>
/// Implementations open a diagnostic <c>Activity</c> and an MEL logger scope
/// for every <see cref="BeginAsync"/> call, so subsequent <c>ILogger</c>
/// usage automatically inherits the activity's <c>activity_id</c>,
/// <c>trace_id</c>, and <c>span_id</c> on every emitted log row.
/// </remarks>
internal interface ILoggingActivityService
{
    #region Public Methods

    /// <summary>
    /// Inserts a new <c>activity</c> row in the <see cref="LoggingActivityStatus.Running"/>
    /// state and opens an ambient scope that subsequent log lines inherit.
    /// </summary>
    /// <param name="request">Initial values for the new activity row. The caller supplies the ULID identifier.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A disposable handle whose lifetime defines the ambient scope. Dispose to pop the scope; call <see cref="CompleteAsync"/> or <see cref="FailAsync"/> to mark the row terminal.</returns>
    Task<LoggingActivityScope> BeginAsync(
        LoggingActivityBeginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps <c>UtcNow</c> into the <c>last_heartbeat_on</c> column of an
    /// existing activity row.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the heartbeat has been recorded.</returns>
    Task HeartbeatAsync(
        string activityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches the supplied non-<c>null</c> "current state" fields onto an
    /// existing activity row.
    /// </summary>
    /// <param name="request">The patch request. Null properties preserve their existing values.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the patch has been applied.</returns>
    Task UpdateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row terminal with the supplied non-<see cref="LoggingActivityStatus.Failed"/> status.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to finalize.</param>
    /// <param name="status">Terminal status to record. Use <see cref="FailAsync"/> for exception-driven failures.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected during the run.</param>
    /// <param name="metricsJson">Optional metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as <see cref="LoggingActivityStatus.Failed"/>
    /// and records the captured exception.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to finalize.</param>
    /// <param name="error">The exception that ended the activity.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task FailAsync(
        string activityId,
        Exception error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a lineage edge into <c>activity_input</c> linking the
    /// supplied activity to one of its upstream inputs.
    /// </summary>
    /// <param name="activityId">The consuming activity's identifier.</param>
    /// <param name="inputActivityId">The upstream input activity's identifier.</param>
    /// <param name="inputRole">Optional free-form role describing the consumed input (e.g. <c>"places"</c>, <c>"cousubs"</c>).</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the edge has been inserted.</returns>
    Task AddInputAsync(
        string activityId,
        string inputActivityId,
        string? inputRole = null,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
