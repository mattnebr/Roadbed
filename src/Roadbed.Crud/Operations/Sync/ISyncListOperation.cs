namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous List operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Lists all entities.
    /// </summary>
    /// <returns>Collection of all entities.</returns>
    IList<TEntity> List();

    #endregion Public Methods
}