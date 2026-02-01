namespace Roadbed.Crud.Operations.Sync;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the synchronous Delete operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Delete is a hard removal. For soft removal, see
/// <see cref="ISyncArchiveOperation{TEntity, TId}"/>.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeoping to remain consistant with other operations.")]
public interface ISyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    void Delete(TId id);

    #endregion Public Methods
}