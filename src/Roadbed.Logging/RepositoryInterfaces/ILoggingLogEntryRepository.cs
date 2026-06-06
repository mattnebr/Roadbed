namespace Roadbed.Logging;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Data-access contract for the high-volume <c>log_entries</c> table.
/// </summary>
/// <remarks>
/// The single exposed operation is a chunked multi-row INSERT. Each entry in
/// the supplied list carries its own originating <c>activity_id</c>; there
/// is no uniform-<c>activityId</c> stamping like the Roadbed.Crud bulk
/// insert tier offers.
/// </remarks>
internal interface ILoggingLogEntryRepository
{
    #region Public Methods

    /// <summary>
    /// Inserts a batch of log entries using chunked multi-row INSERT statements.
    /// </summary>
    /// <param name="entries">The entries to insert. May be empty; in that case the call returns zero without touching the database.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The total number of rows inserted across every chunk.</returns>
    Task<int> BulkInsertAsync(
        IReadOnlyList<LoggingLogEntry> entries,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
