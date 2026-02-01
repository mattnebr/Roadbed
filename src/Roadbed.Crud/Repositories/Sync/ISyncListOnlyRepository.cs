namespace Roadbed.Crud.Repositories.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync list-only repository. Provides only the List operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for dimension tables, lookup tables, and reference data loaded in bulk.
/// </remarks>
public interface ISyncListOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ISyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}