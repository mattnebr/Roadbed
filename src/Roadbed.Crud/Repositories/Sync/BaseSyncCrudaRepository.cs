namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync CRUDA repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Five abstract methods: Create, Read, Update, Delete, Archive.
/// </remarks>
public abstract class BaseSyncCrudaRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudaRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudaRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncCrudaRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudaRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncCrudaRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract TEntity Archive(TId id);

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