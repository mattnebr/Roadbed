namespace Roadbed.Crud.Services.Sync;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Base for sync CRUDL services. Five CRUDL operations delegate to the repository.
/// Exists and Upsert are composed from repository primitives.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public class BaseSyncCrudlService<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudlService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ISyncCrudlRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncCrudlService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncCrudlService(
        ISyncCrudlRepository<TEntity, TId> repository,
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
    protected ISyncCrudlRepository<TEntity, TId> Repository => this._repository;

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
    public virtual IList<TEntity> List()
    {
        return this._repository.List();
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