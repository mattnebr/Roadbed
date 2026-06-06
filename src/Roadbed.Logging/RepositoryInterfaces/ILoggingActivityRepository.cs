namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Data-access contract for the <c>activity</c> table.
/// </summary>
/// <remarks>
/// Each method maps to a single SQL statement against the activity row
/// addressed by its identifier. There is intentionally no read or list
/// surface here — Roadbed.Logging only writes activity rows; analysts read
/// them directly out of the database.
/// </remarks>
internal interface ILoggingActivityRepository
{
    #region Public Methods

    /// <summary>
    /// Inserts the supplied activity row in the <c>running</c> state.
    /// </summary>
    /// <param name="entity">The activity row to insert.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been inserted.</returns>
    Task InsertAsync(
        LoggingActivity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches the supplied non-<c>null</c> fields onto an existing activity row.
    /// </summary>
    /// <param name="request">The current-state update request.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been patched.</returns>
    /// <remarks>
    /// Properties left at <c>null</c> on <paramref name="request"/> preserve
    /// their existing values via a <c>COALESCE</c>-driven UPDATE.
    /// </remarks>
    Task UpdateCurrentStateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the supplied timestamp into the <c>last_heartbeat_on</c> column.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="heartbeatOn">UTC moment of the heartbeat.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the heartbeat has been recorded.</returns>
    Task RecordHeartbeatAsync(
        string activityId,
        DateTime heartbeatOn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as terminal with the supplied status.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="status">Terminal status to record.</param>
    /// <param name="completedOn">UTC moment the activity completed.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected.</param>
    /// <param name="metricsJson">Optional updated metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        DateTime completedOn,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as <see cref="LoggingActivityStatus.Failed"/>
    /// and records the captured exception.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="completedOn">UTC moment the failure was recorded.</param>
    /// <param name="error">Rendered exception message.</param>
    /// <param name="errorType">Fully-qualified type name of the captured exception.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task FailAsync(
        string activityId,
        DateTime completedOn,
        string error,
        string errorType,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
