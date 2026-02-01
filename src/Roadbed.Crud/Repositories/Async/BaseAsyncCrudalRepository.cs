namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async CRUDAL ("crud-al") repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Six abstract methods: Create, Read, Update, Delete, Archive, List. This is
/// the full complement of repository operations.
/// </remarks>
public abstract class BaseAsyncCrudalRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudalRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncCrudalRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncCrudalRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncCrudalRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncCrudalRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<TEntity> ArchiveAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity?> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}