namespace Roadbed.Crud.Services.Sync;

/// <summary>
/// Sync CRUDL service. Create, Read, Update, Delete, List, Exists, Upsert.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncCrudlService<TEntity, TId>
    : ISyncCrudService<TEntity, TId>,
      ISyncListOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}