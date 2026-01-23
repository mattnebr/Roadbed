namespace Roadbed.Data.Sqlite;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Provides methods for executing SQLite database operations using Dapper.
/// </summary>
public static class SqliteExecutor
{
    #region Public Methods

    /// <summary>
    /// Executes a non-query command asynchronously with optional retry logic.
    /// </summary>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<int> ExecuteAsync(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .ExecuteAsync(request.Query, request.Parameters)
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await ExecuteWithRetryAsync(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns the results asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of objects to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A collection of results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .QueryAsync<T>(request.Query, request.Parameters)
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await QueryWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a single result asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of object to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A single result or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .QuerySingleOrDefaultAsync<T>(request.Query, request.Parameters)
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await QuerySingleOrDefaultWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a scalar value asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of scalar value to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>The scalar value from the first column of the first row.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<T> ExecuteScalarAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .ExecuteScalarAsync<T>(request.Query, request.Parameters)
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await ExecuteScalarWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Executes a non-query command with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<int> ExecuteWithRetryAsync(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    int result = await dbConnection
                        .ExecuteAsync(request.Query, request.Parameters)
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation(
                                "Command succeeded on attempt {Attempt}. Rows affected: {Rows}",
                                attempt + 1,
                                result);
                        }
                    }

                    return result;
                }
            }
            catch (SqliteException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                    "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                    attempt,
                    ex.SqliteErrorCode,
                    ex.Message,
                    delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                lastException,
                "Command failed after {Attempts} attempts",
                request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute command after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<IEnumerable<T>> QueryWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .QueryAsync<T>(request.Query, request.Parameters)
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation(
                                "Query succeeded on attempt {Attempt}. Rows returned: {Count}",
                                attempt + 1,
                                result.Count());
                        }
                    }

                    return result;
                }
            }
            catch (SqliteException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                    "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                    attempt,
                    ex.SqliteErrorCode,
                    ex.Message,
                    delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
            lastException,
            "Query failed after {Attempts} attempts",
            request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a single-result query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<T?> QuerySingleOrDefaultWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .QuerySingleOrDefaultAsync<T>(request.Query, request.Parameters)
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation(
                            "Query succeeded on attempt {Attempt}. Result: {Found}",
                            attempt + 1,
                            !Equals(result, default(T)) ? "found" : "not found");
                        }
                    }

                    return result;
                }
            }
            catch (SqliteException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                    "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                    attempt,
                    ex.SqliteErrorCode,
                    ex.Message,
                    delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
            lastException,
            "Query failed after {Attempts} attempts",
            request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a scalar query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<T> ExecuteScalarWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .ExecuteScalarAsync<T>(request.Query, request.Parameters)
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation(
                                "Query succeeded on attempt {Attempt}. Scalar result: {Result}",
                                attempt + 1,
                                result);
                        }
                    }

                    return result;
                }
            }
            catch (SqliteException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                    "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                    attempt,
                    ex.SqliteErrorCode,
                    ex.Message,
                    delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
            lastException,
            "Query failed after {Attempts} attempts",
            request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute scalar query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Calculates the delay for retry based on configuration.
    /// </summary>
    private static TimeSpan CalculateDelay(DataExecutorRequest request, int attempt)
    {
        return request.DelayMultiplierEnabled
            ? TimeSpan.FromMilliseconds(request.DelayBetweenRetries.TotalMilliseconds * attempt)
            : request.DelayBetweenRetries;
    }

    /// <summary>
    /// Determines if a SQLite exception is transient and worth retrying.
    /// </summary>
    private static bool IsTransientError(SqliteException ex)
    {
        return ex.SqliteErrorCode switch
        {
            5 => true,   // SQLITE_BUSY - database is locked
            6 => true,   // SQLITE_LOCKED - table is locked
            10 => true,  // SQLITE_IOERR - disk I/O error
            13 => true,  // SQLITE_FULL - disk full
            _ => false
        };
    }

    /// <summary>
    /// Truncates a SQL query string for logging purposes.
    /// </summary>
    private static string TruncateQuery(string query, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(query))
        {
            return string.Empty;
        }

        return query.Length <= maxLength
            ? query
            : query.Substring(0, maxLength) + "...";
    }

    #endregion Private Methods
}