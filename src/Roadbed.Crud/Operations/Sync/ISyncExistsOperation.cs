namespace Roadbed.Crud.Operations.Sync;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the synchronous Exists operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Exists is a service-level operation. The default service implementation
/// composes from <see cref="ISyncReadOperation{TEntity, TId}.Read"/>.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeoping to remain consistant with other operations.")]
public interface ISyncExistsOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">Identifier to check.</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    bool Exists(TId id);

    #endregion Public Methods
}