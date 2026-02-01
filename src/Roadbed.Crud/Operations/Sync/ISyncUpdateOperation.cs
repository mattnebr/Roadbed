namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Update operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncUpdateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <returns>The updated entity.</returns>
    TEntity Update(TEntity entity);

    #endregion Public Methods
}