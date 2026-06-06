namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;
using Roadbed.Data.MySql;
using Roadbed.Data.Sqlite;

/// <summary>
/// Internal helper that dispatches <see cref="DataExecutorRequest"/>
/// invocations to <see cref="MySqlExecutor"/> or <see cref="SqliteExecutor"/>
/// based on the supplied factory's connection-string type.
/// </summary>
/// <remarks>
/// Centralizing the switch here keeps every repository free of the
/// provider-pick boilerplate and gives a single seam to extend if Postgres
/// support is added later.
/// </remarks>
internal static class LoggingSqlDispatcher
{
    #region Public Methods

    /// <summary>
    /// Executes a non-query command against MySQL or SQLite.
    /// </summary>
    /// <param name="request">The request carrying SQL and parameters.</param>
    /// <param name="factory">The Roadbed.Logging database factory.</param>
    /// <param name="logger">Logger used for retry diagnostics.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The number of rows affected.</returns>
    public static Task<int> ExecuteAsync(
        DataExecutorRequest request,
        ILoggingDatabaseFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return factory.Connecion.ConnectionStringType switch
        {
            DataConnectionStringType.MySQL =>
                MySqlExecutor.ExecuteAsync(request, factory, logger, cancellationToken),

            DataConnectionStringType.SQLite =>
                SqliteExecutor.ExecuteAsync(request, factory, logger, cancellationToken),

            DataConnectionStringType.SQLiteInMemory =>
                SqliteExecutor.ExecuteAsync(request, factory, logger, cancellationToken),

            var other => throw UnsupportedProvider(other),
        };
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Builds the exception thrown when the connection-string type is not one Roadbed.Logging supports.
    /// </summary>
    /// <param name="type">The unsupported connection-string type.</param>
    /// <returns>An <see cref="InvalidOperationException"/> describing the mismatch.</returns>
    private static InvalidOperationException UnsupportedProvider(DataConnectionStringType type)
    {
        return new InvalidOperationException(
            $"Roadbed.Logging does not support {nameof(DataConnectionStringType)} " +
            $"'{type}'. Supported providers in v1 are MySQL and SQLite.");
    }

    #endregion Private Methods
}
