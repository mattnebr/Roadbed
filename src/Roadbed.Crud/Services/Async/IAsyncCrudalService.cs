namespace Roadbed.Crud.Services.Async;

/// <summary>
/// Async CRUDAL ("crud-al") service. Create, Read, Update, Delete, Archive, List,
/// Exists, Upsert.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncCrudalService<TEntity, TId>
    : IAsyncCrudaService<TEntity, TId>,
      IAsyncCrudlService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}