namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async CRUDA repository. Create, Read, Update, Delete, Archive — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities that support soft delete (archive) but where listing all rows
/// is never appropriate.
/// </remarks>
public interface IAsyncCrudaRepository<TEntity, TId>
    : IAsyncCrudRepository<TEntity, TId>,
      IAsyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}