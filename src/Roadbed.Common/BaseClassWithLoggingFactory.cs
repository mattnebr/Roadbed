/*
 * The namespace Roadbed.Common was removed on purpose and replaced with Roadbed
 * so that no additional using statements are required.
 */

namespace Roadbed;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Base class with logging and logger factory support.
/// </summary>
/// <typeparam name="TCategoryName">Type inheriting from the Base.</typeparam>
/// <remarks>
/// <para>
/// Extends <see cref="BaseClassWithLogging"/> with <see cref="ILoggerFactory"/>
/// support and a typed <see cref="ILogger{TCategoryName}"/> property. Use this
/// class when you need to create additional loggers from the factory or when you
/// need the typed logger for direct access.
/// </para>
/// <para>
/// For classes that only need the logging convenience methods (such as Roadbed.Crud
/// repository and service base classes), inherit from
/// <see cref="BaseClassWithLogging"/> directly.
/// </para>
/// </remarks>
public abstract class BaseClassWithLoggingFactory<TCategoryName>
    : BaseClassWithLogging
{
    #region Private Fields

    /// <summary>
    /// Container for the public property LoggerFactory.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseClassWithLoggingFactory{TCategoryName}"/> class.
    /// </summary>
    protected BaseClassWithLoggingFactory()
        : base()
    {
        this._loggerFactory = NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseClassWithLoggingFactory{TCategoryName}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseClassWithLoggingFactory(ILogger logger)
        : base(logger)
    {
        this._loggerFactory = NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseClassWithLoggingFactory{TCategoryName}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Represents a type used to configure the logging
    /// system and create instances of ILogger from the registered
    /// ILoggerProviders.</param>
    protected BaseClassWithLoggingFactory(ILoggerFactory loggerFactory)
        : base((loggerFactory ?? NullLoggerFactory.Instance)
            .CreateLogger<TCategoryName>())
    {
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <summary>
    /// Gets the type used to configure the logging system and create instances of
    /// ILogger from the registered ILoggerProviders.
    /// </summary>
    public ILoggerFactory LoggerFactory => this._loggerFactory;

    #endregion Public Properties
}