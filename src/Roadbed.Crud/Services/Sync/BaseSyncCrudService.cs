namespace Roadbed.Crud.Services.Sync;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Base for sync CRUD services. Four CRUD operations delegate to the repository.
/// Exists and Upsert are composed from repository primitives.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Exists"/> composes from
/// <see cref="Read"/> and returns
/// <c>true</c> when the result is not null. This assumes the repository returns
/// null for not-found entities.
/// </para>
/// <para>
/// <see cref="Upsert"/> composes from <see cref="Exists"/>,
/// <see cref="Create"/>, and
/// <see cref="Update"/>. Override when
/// the data source supports native upsert.
/// </para>
/// </remarks>
public class BaseSyncCrudService<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ISyncCrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncCrudService(
        ISyncCrudRepository<TEntity, TId> repository,
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
    protected ISyncCrudRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual TEntity Create(TEntity entity)
    {
        return this._repository.Create(entity);
    }

    /// <inheritdoc/>
    public virtual TEntity? Read(TId id)
    {
        return this._repository.Read(id);
    }

    /// <inheritdoc/>
    public virtual TEntity Update(TEntity entity)
    {
        return this._repository.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void Delete(TId id)
    {
        this._repository.Delete(id);
    }

    /// <inheritdoc/>
    public virtual bool Exists(TId id)
    {
        var entity = this._repository.Read(id);
        return entity is not null;
    }

    /// <inheritdoc/>
    public virtual TEntity Upsert(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id is not null
            && this.Exists(entity.Id!))
        {
            return this._repository.Update(entity);
        }

        return this._repository.Create(entity);
    }

    #endregion Public Methods
}