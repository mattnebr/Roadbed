namespace Roadbed.Crud.Repositories.Async;

/// <summary>
/// Async CRUDL repository. Create, Read, Update, Delete, List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities where listing all rows is practical — hundreds to low thousands
/// of rows. Inherits from both <see cref="IAsyncCrudRepository{TEntity, TId}"/> and
/// <see cref="IAsyncListOnlyRepository{TEntity, TId}"/>.
/// </remarks>
public interface IAsyncCrudlRepository<TEntity, TId>
    : IAsyncCrudRepository<TEntity, TId>,
      IAsyncListOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}