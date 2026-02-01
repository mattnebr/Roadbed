namespace Roadbed.Crud.Services.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync CRUDA service. Create, Read, Update, Delete, Archive, Exists, Upsert — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncCrudaService<TEntity, TId>
    : ISyncCrudService<TEntity, TId>,
      ISyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}