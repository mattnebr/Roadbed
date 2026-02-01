namespace Roadbed.Crud.Operations.Async;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the asynchronous Exists operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Exists is a service-level operation. It is not implemented at the repository
/// level. The default service implementation composes from
/// <see cref="IAsyncReadOperation{TEntity, TId}.ReadAsync"/> and returns
/// <c>true</c> when the entity is not null.
/// </para>
/// <para>
/// This operation is included in service composites that contain CRUD operations
/// (CrudService, CrudlService, CrudaService, CrudalService).
/// </para>
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeoping to remain consistant with other operations.")]
public interface IAsyncExistsOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">Identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}