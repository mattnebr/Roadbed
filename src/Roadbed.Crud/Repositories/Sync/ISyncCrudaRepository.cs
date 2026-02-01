namespace Roadbed.Crud.Repositories.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync CRUDA repository. Create, Read, Update, Delete, Archive — no List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities that support soft delete (archive) but where listing all rows
/// is never appropriate.
/// </remarks>
public interface ISyncCrudaRepository<TEntity, TId>
    : ISyncCrudRepository<TEntity, TId>,
      ISyncArchiveOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}