namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async list-only repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// One abstract method: <see cref="ListAsync"/>. The consuming class must
/// implement the data access logic. Use Visual Studio's "Implement abstract
/// class" to generate the stub.
/// </remarks>
public abstract class BaseAsyncListOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncListOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncListOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncListOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncListOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncListOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}