namespace Roadbed.Crud.Repositories.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync CRUD repository. Create, Read, Update, Delete — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities where listing all rows is never appropriate. Custom filtered
/// queries can be added via Level 3 consumption (composite + custom methods).
/// </remarks>
public interface ISyncCrudRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ISyncCreateOperation<TEntity, TId>,
      ISyncReadOperation<TEntity, TId>,
      ISyncUpdateOperation<TEntity, TId>,
      ISyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}