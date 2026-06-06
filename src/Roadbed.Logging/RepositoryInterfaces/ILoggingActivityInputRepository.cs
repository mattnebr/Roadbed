namespace Roadbed.Logging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Data-access contract for the <c>activity_input</c> lineage edge table.
/// </summary>
internal interface ILoggingActivityInputRepository
{
    #region Public Methods

    /// <summary>
    /// Inserts a single lineage edge linking a consumer activity to an upstream input activity.
    /// </summary>
    /// <param name="entity">The edge to insert.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the edge has been inserted.</returns>
    /// <remarks>
    /// Duplicate <c>(activity_id, input_activity_id)</c> tuples are silently
    /// coalesced by the composite primary key; the repository swallows the
    /// resulting duplicate-key error and returns normally.
    /// </remarks>
    Task InsertAsync(
        LoggingActivityInput entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
