namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync CRUD repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Four abstract methods: Create, Read, Update, Delete. The consuming class must
/// implement all four.
/// </remarks>
public abstract class BaseSyncCrudRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncCrudRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncCrudRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract TEntity Create(TEntity entity);

    /// <inheritdoc/>
    public abstract void Delete(TId id);

    /// <inheritdoc/>
    public abstract TEntity? Read(TId id);

    /// <inheritdoc/>
    public abstract TEntity Update(TEntity entity);

    #endregion Public Methods
}