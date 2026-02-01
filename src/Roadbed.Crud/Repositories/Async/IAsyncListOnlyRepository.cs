namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async list-only repository. Provides only the List operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for dimension tables, lookup tables, and reference data loaded in bulk.
/// </remarks>
public interface IAsyncListOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}