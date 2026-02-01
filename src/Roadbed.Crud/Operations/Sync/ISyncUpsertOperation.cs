namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Upsert operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Upsert is a service-level operation. The default service implementation
/// composes from Exists, Create, and Update.
/// </remarks>
public interface ISyncUpsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Creates or updates an entity.
    /// </summary>
    /// <param name="entity">Entity to create or update.</param>
    /// <returns>The created or updated entity.</returns>
    TEntity Upsert(TEntity entity);

    #endregion Public Methods
}