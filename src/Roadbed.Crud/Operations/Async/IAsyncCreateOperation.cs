namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncCreateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}