namespace Roadbed.Logging.MySql;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;
using Roadbed.Data.MySql;

/// <summary>
/// MySQL/MariaDB implementation of <see cref="ILoggingDataExecutor"/>: a thin,
/// stateless adapter over <see cref="MySqlExecutor"/>.
/// </summary>
internal sealed class MySqlLoggingDataExecutor : ILoggingDataExecutor
{
    #region Public Methods

    /// <inheritdoc/>
    public Task<int> ExecuteAsync(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return MySqlExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> QueryAsync<T>(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return MySqlExecutor.QueryAsync<T>(request, factory, logger, cancellationToken);
    }

    #endregion Public Methods
}
