namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async CRUD repository. Create, Read, Update, Delete — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities where listing all rows is never appropriate. Custom filtered
/// queries can be added via Level 3 consumption (composite + custom methods).
/// </remarks>
public interface IAsyncCrudRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IAsyncCreateOperation<TEntity, TId>,
      IAsyncReadOperation<TEntity, TId>,
      IAsyncUpdateOperation<TEntity, TId>,
      IAsyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}