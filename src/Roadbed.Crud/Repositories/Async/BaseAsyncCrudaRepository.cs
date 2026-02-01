namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async CRUDA repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Five abstract methods: Create, Read, Update, Delete, Archive.
/// </remarks>
public abstract class BaseAsyncCrudaRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudaRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncCrudaRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncCrudaRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncCrudaRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncCrudaRepository(ILogger logger)
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
    public abstract Task<TEntity?> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}