namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync list-only repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// One abstract method: <see cref="List"/>. The consuming class must implement
/// the data access logic. Use Visual Studio's "Implement abstract class" to
/// generate the stub.
/// </remarks>
public abstract class BaseSyncListOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncListOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncListOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncListOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncListOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncListOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract IList<TEntity> List();

    #endregion Public Methods
}