namespace Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Sync CRUDAL ("crud-al") repository. Create, Read, Update, Delete, Archive, List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// The full composite. Use for entities that support all operations including
/// soft delete and listing. Inherits from both
/// <see cref="ISyncCrudaRepository{TEntity, TId}"/> and
/// <see cref="ISyncCrudlRepository{TEntity, TId}"/>.
/// </remarks>
public interface ISyncCrudalRepository<TEntity, TId>
    : ISyncCrudaRepository<TEntity, TId>,
      ISyncCrudlRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}