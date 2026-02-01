namespace Roadbed.Crud.Services.Sync;

/// <summary>
/// Sync CRUDAL ("crud-al") service. Create, Read, Update, Delete, Archive, List,
/// Exists, Upsert.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncCrudalService<TEntity, TId>
    : ISyncCrudaService<TEntity, TId>,
      ISyncCrudlService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}