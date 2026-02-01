namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Archive operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Archive is a soft delete — the entity is marked as inactive or archived in the
/// data source rather than being physically removed. The repository implementation
/// determines the archival mechanism (status column, archived_at timestamp, etc.).
/// For hard removal, see <see cref="IAsyncDeleteOperation{TEntity, TId}"/>.
/// </remarks>
public interface IAsyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Archives an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity to archive.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The archived entity with its updated archival state.</returns>
    Task<TEntity> ArchiveAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}