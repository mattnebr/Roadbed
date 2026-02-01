namespace Roadbed.Crud.Services.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync CRUD service. Create, Read, Update, Delete, Exists, Upsert — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Includes <see cref="ISyncExistsOperation{TEntity, TId}"/> and
/// <see cref="ISyncUpsertOperation{TEntity, TId}"/> which are composed from
/// repository primitives in the base service class.
/// </remarks>
public interface ISyncCrudService<TEntity, TId>
    : ISyncCreateOperation<TEntity, TId>,
      ISyncReadOperation<TEntity, TId>,
      ISyncUpdateOperation<TEntity, TId>,
      ISyncDeleteOperation<TEntity, TId>,
      ISyncExistsOperation<TEntity, TId>,
      ISyncUpsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}