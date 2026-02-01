namespace Roadbed.Crud.Operations.Sync;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the synchronous Archive operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Archive is a soft delete. The repository implementation determines the archival
/// mechanism. For hard removal, see <see cref="ISyncDeleteOperation{TEntity, TId}"/>.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
public interface ISyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Archives an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity to archive.</param>
    /// <returns>The archived entity with its updated archival state.</returns>
    TEntity Archive(TId id);

    #endregion Public Methods
}