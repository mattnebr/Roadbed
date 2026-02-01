namespace Roadbed.Crud.Operations.Async;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Delete is a hard removal — the entity is physically removed from the data source.
/// For soft removal, see <see cref="IAsyncArchiveOperation{TEntity, TId}"/>.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeoping to remain consistant with other operations.")]
public interface IAsyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}