namespace Roadbed.Crud.Services.Sync;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Base for sync list-only services. The List operation delegates to the repository.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public class BaseSyncListOnlyService<TEntity, TId>
    : BaseClassWithLogging,
      ISyncListOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ISyncListOnlyRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncListOnlyService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncListOnlyService(
        ISyncListOnlyRepository<TEntity, TId> repository,
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
    protected ISyncListOnlyRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual IList<TEntity> List()
    {
        return this._repository.List();
    }

    #endregion Public Methods
}