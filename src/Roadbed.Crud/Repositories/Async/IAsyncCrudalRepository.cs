namespace Roadbed.Crud.Repositories.Async;

/// <summary>
/// Async CRUDAL ("crud-al") repository. Create, Read, Update, Delete, Archive, List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// The full composite. Use for entities that support all operations including
/// soft delete and listing. Inherits from both
/// <see cref="IAsyncCrudaRepository{TEntity, TId}"/> and
/// <see cref="IAsyncCrudlRepository{TEntity, TId}"/>.
/// </remarks>
public interface IAsyncCrudalRepository<TEntity, TId>
    : IAsyncCrudaRepository<TEntity, TId>,
      IAsyncCrudlRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}