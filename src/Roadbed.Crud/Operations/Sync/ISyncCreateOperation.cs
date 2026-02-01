namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncCreateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    TEntity Create(TEntity entity);

    #endregion Public Methods
}