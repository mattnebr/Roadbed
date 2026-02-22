/*
 * The namespace Roadbed.Common was removed on purpose and replaced with Roadbed
 * so that no additional using statements are required.
 */

namespace Roadbed;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Base class providing logging convenience methods.
/// </summary>
/// <remarks>
/// <para>
/// Provides level-checked convenience methods (<see cref="LogDebug(string)"/>,
/// <see cref="LogInformation(string)"/>, etc.) that check
/// <see cref="ILogger.IsEnabled(LogLevel)"/> before formatting messages. This
/// prevents unnecessary string allocation and parameter boxing when the log level
/// is disabled.
/// </para>
/// <para>
/// Subclasses should use the convenience methods (e.g., <c>this.LogDebug(...)</c>)
/// instead of accessing the logger directly.
/// </para>
/// <para>
/// For subclasses that need <see cref="ILoggerFactory"/> or a typed
/// <see cref="ILogger{TCategoryName}"/>, use
/// <see cref="BaseClassWithLoggingFactory{TCategoryName}"/> instead.
/// </para>
/// </remarks>
public abstract class BaseClassWithLogging
{
    #region Private Fields

    /// <summary>
    /// Logger instance used by all convenience methods.
    /// </summary>
    private readonly ILogger _logger;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClassWithLogging"/> class
    /// with a <see cref="NullLogger"/> instance.
    /// </summary>
    protected BaseClassWithLogging()
    {
        this._logger = NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClassWithLogging"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// When <paramref name="logger"/> is <c>null</c>, a <see cref="NullLogger"/>
    /// instance is used. The <see cref="ILogger"/> may be a typed
    /// <see cref="ILogger{TCategoryName}"/> — the category information is preserved
    /// in the log output.
    /// </remarks>
    protected BaseClassWithLogging(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the logger instance for passing to external libraries.
    /// </summary>
    /// <remarks>
    /// Prefer the convenience methods (e.g., <c>this.LogDebug(...)</c>) for direct
    /// logging. Use this property only when an <see cref="ILogger"/> must be passed
    /// to another component (e.g., <c>SqliteExecutor</c>).
    /// </remarks>
    protected ILogger Logger => this._logger;

    #endregion Protected Properties

    #region Public Methods

    /// <summary>
    /// Appends a single key/value pair to the logging scope.
    /// </summary>
    /// <param name="key">Key for the dictionary entry.</param>
    /// <param name="value">Value for the dictionary entry.</param>
    /// <returns>Logger with the scope attached.</returns>
    /// <remarks>
    /// For more context, see https://andrewlock.net/creating-an-extension-method-for-attaching-key-value-pairs-to-scope-state-using-asp-net-core/.
    /// <code>
    /// using (this.BeginScope("transactionId", transaction.Id))
    /// {
    ///     this.LogInformation("Successful transaction");
    /// }
    /// </code>
    /// </remarks>
    public IDisposable? BeginScope(string key, object value)
    {
        return this._logger.BeginScope(key, value);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Critical"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogCritical(string message)
    {
        this._logger.LogWithCheck(LogLevel.Critical, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Critical"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogCritical(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Critical, message, param);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Critical"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    public void LogCritical(Exception exception, string message)
    {
        this._logger.LogCritical(exception, message);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Critical"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogCritical(Exception exception, string message, params object[] param)
    {
        this._logger.LogCritical(exception, message, param);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogDebug(string message)
    {
        this._logger.LogWithCheck(LogLevel.Debug, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogDebug(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Debug, message, param);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    public void LogDebug(Exception exception, string message)
    {
        this._logger.LogDebug(exception, message);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogDebug(Exception exception, string message, params object[] param)
    {
        this._logger.LogDebug(exception, message, param);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogError(string message)
    {
        this._logger.LogWithCheck(LogLevel.Error, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogError(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Error, message, param);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    public void LogError(Exception exception, string message)
    {
        this._logger.LogError(exception, message);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogError(Exception exception, string message, params object[] param)
    {
        this._logger.LogError(exception, message, param);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Information"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogInformation(string message)
    {
        this._logger.LogWithCheck(LogLevel.Information, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Information"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogInformation(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Information, message, param);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Trace"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogTrace(string message)
    {
        this._logger.LogWithCheck(LogLevel.Trace, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Trace"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogTrace(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Trace, message, param);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogWarning(string message)
    {
        this._logger.LogWithCheck(LogLevel.Warning, message);
    }

    /// <summary>
    /// Logs a message with a severity level of <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogWarning(string message, params object[] param)
    {
        this._logger.LogWithCheck(LogLevel.Warning, message, param);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    public void LogWarning(Exception exception, string message)
    {
        this._logger.LogWarning(exception, message);
    }

    /// <summary>
    /// Logs an exception with a severity level of <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="param">Message parameters.</param>
    public void LogWarning(Exception exception, string message, params object[] param)
    {
        this._logger.LogWarning(exception, message, param);
    }

    #endregion Public Methods
}