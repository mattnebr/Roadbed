namespace Roadbed.Crud.Services.Async;

/// <summary>
/// Async CRUDL service. Create, Read, Update, Delete, List, Exists, Upsert.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncCrudlService<TEntity, TId>
    : IAsyncCrudService<TEntity, TId>,
      IAsyncListOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}