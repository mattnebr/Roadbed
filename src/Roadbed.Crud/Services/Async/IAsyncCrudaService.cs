namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async CRUDA service. Create, Read, Update, Delete, Archive, Exists, Upsert — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncCrudaService<TEntity, TId>
    : IAsyncCrudService<TEntity, TId>,
      IAsyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}