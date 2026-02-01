namespace Roadbed.Crud.Services.Async;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Base for async CRUD services. Four CRUD operations delegate to the repository.
/// Exists and Upsert are composed from repository primitives.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ExistsAsync"/> composes from
/// <see cref="ReadAsync"/> and returns
/// <c>true</c> when the result is not null. This assumes the repository returns
/// null for not-found entities.
/// </para>
/// <para>
/// <see cref="UpsertAsync"/> composes from <see cref="ExistsAsync"/>,
/// <see cref="CreateAsync"/>, and
/// <see cref="UpdateAsync"/>. Override when
/// the data source supports native upsert.
/// </para>
/// </remarks>
public class BaseAsyncCrudService<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly IAsyncCrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncCrudService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncCrudService(
        IAsyncCrudRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this._repository = repository;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the repository for data access operations.
    /// </summary>
    protected IAsyncCrudRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual async Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.CreateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ReadAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        await this._repository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await this._repository.ReadAsync(id, cancellationToken);
        return entity is not null;
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id is not null
            && await this.ExistsAsync(entity.Id!, cancellationToken))
        {
            return await this._repository.UpdateAsync(entity, cancellationToken);
        }

        return await this._repository.CreateAsync(entity, cancellationToken);
    }

    #endregion Public Methods
}