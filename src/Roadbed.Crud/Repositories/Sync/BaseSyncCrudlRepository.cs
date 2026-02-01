namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync CRUDL repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Five abstract methods: Create, Read, Update, Delete, List.
/// </remarks>
public abstract class BaseSyncCrudlRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudlRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudlRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncCrudlRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudlRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncCrudlRepository(ILogger logger)
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
    public abstract IList<TEntity> List();

    /// <inheritdoc/>
    public abstract TEntity? Read(TId id);

    /// <inheritdoc/>
    public abstract TEntity Update(TEntity entity);

    #endregion Public Methods
}