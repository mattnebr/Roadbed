namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async CRUD service. Create, Read, Update, Delete, Exists, Upsert — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Includes <see cref="IAsyncExistsOperation{TEntity, TId}"/> and
/// <see cref="IAsyncUpsertOperation{TEntity, TId}"/> which are composed from
/// repository primitives in the base service class.
/// </remarks>
public interface IAsyncCrudService<TEntity, TId>
    : IAsyncCreateOperation<TEntity, TId>,
      IAsyncReadOperation<TEntity, TId>,
      IAsyncUpdateOperation<TEntity, TId>,
      IAsyncDeleteOperation<TEntity, TId>,
      IAsyncExistsOperation<TEntity, TId>,
      IAsyncUpsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}