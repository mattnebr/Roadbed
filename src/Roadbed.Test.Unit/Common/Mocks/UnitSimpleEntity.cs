namespace Roadbed.Test.Unit.Crud.Mocks;

using Microsoft.Extensions.Logging;
using Roadbed;

/// <summary>
/// Simple entity for testing purposes.
/// </summary>
/// <typeparam name="TCategoryName">Type inheriting from the Base.</typeparam>
/// <remarks>
/// This class is used to test the <see cref="BaseClassWithLoggingFactory{TCategoryName}"/> class.
/// </remarks>
public class UnitSimpleEntity<TCategoryName> :
    BaseClassWithLoggingFactory<TCategoryName>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitSimpleEntity{TCategoryName}"/> class.
    /// </summary>
    public UnitSimpleEntity()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitSimpleEntity{TCategoryName}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public UnitSimpleEntity(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitSimpleEntity{TCategoryName}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    public UnitSimpleEntity(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    #endregion Protected Constructors
}