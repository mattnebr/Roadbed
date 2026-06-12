namespace Roadbed.Logging.Sqlite;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// SQLite implementation of <see cref="ILoggingDataExecutor"/>: a thin,
/// stateless adapter over <see cref="SqliteExecutor"/>.
/// </summary>
internal sealed class SqliteLoggingDataExecutor : ILoggingDataExecutor
{
    #region Public Methods

    /// <inheritdoc/>
    public Task<int> ExecuteAsync(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return SqliteExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> QueryAsync<T>(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return SqliteExecutor.QueryAsync<T>(request, factory, logger, cancellationToken);
    }

    #endregion Public Methods
}
