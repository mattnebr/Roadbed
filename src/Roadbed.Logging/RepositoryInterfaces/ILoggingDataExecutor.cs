namespace Roadbed.Logging;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Provider-neutral execution port the Roadbed.Logging repositories call
/// instead of binding directly to a concrete database executor.
/// </summary>
/// <remarks>
/// <para>
/// This is the single seam that keeps the core <c>Roadbed.Logging</c>
/// assembly free of any database-client dependency. A provider satellite
/// (<c>Roadbed.Logging.MySql</c> or <c>Roadbed.Logging.Sqlite</c>) supplies
/// the implementation — a thin adapter over the matching
/// <c>Roadbed.Data.*</c> executor — and registers it before
/// <see cref="Installers.LoggingModule.Register"/> wires the rest of the
/// pipeline.
/// </para>
/// <para>
/// Implementations are stateless and thread-safe: the
/// <see cref="ILoggingDatabaseFactory"/> and <see cref="ILogger"/> are passed
/// per call so the caller's repository category drives retry diagnostics.
/// </para>
/// </remarks>
internal interface ILoggingDataExecutor
{
    /// <summary>
    /// Executes a non-query command and returns the rows affected.
    /// </summary>
    /// <param name="request">The request carrying SQL and parameters.</param>
    /// <param name="factory">The host-supplied logging database factory.</param>
    /// <param name="logger">Logger used for retry diagnostics; carries the calling repository's category.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteAsync(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a query and materializes the rows as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The row projection type (e.g. <see cref="string"/> for an id column).</typeparam>
    /// <param name="request">The request carrying SQL and parameters.</param>
    /// <param name="factory">The host-supplied logging database factory.</param>
    /// <param name="logger">Logger used for retry diagnostics; carries the calling repository's category.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The materialized rows.</returns>
    Task<IEnumerable<T>> QueryAsync<T>(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken);
}
