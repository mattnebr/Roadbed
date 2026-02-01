namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncUpdateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}