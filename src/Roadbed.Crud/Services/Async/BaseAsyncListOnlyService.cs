namespace Roadbed.Crud.Services.Async;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Base for async list-only services. The List operation delegates to the repository.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public class BaseAsyncListOnlyService<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncListOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly IAsyncListOnlyRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncListOnlyService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncListOnlyService(
        IAsyncListOnlyRepository<TEntity, TId> repository,
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
    protected IAsyncListOnlyRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual async Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ListAsync(cancellationToken);
    }

    #endregion Public Methods
}