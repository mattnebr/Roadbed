namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Implementations should return <c>null</c> when the entity is not found rather
/// than throwing an exception. This convention enables the service-level
/// <see cref="IAsyncExistsOperation{TEntity, TId}"/> to compose from Read
/// without exception-driven control flow.
/// </remarks>
public interface IAsyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Reads an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier, or <c>null</c> if not found.</returns>
    Task<TEntity?> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}