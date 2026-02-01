namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Lists all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all entities.</returns>
    Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}