namespace Roadbed.Crud.Operations.Sync;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the synchronous Read operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Implementations should return <c>null</c> when the entity is not found rather
/// than throwing an exception.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
public interface ISyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Reads an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <returns>The entity matching the identifier, or <c>null</c> if not found.</returns>
    TEntity? Read(TId id);

    #endregion Public Methods
}