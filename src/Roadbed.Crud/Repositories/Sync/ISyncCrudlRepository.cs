namespace Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Sync CRUDL repository. Create, Read, Update, Delete, List.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for entities where listing all rows is practical — hundreds to low thousands
/// of rows. Inherits from both <see cref="ISyncCrudRepository{TEntity, TId}"/> and
/// <see cref="ISyncListOnlyRepository{TEntity, TId}"/>.
/// </remarks>
public interface ISyncCrudlRepository<TEntity, TId>
    : ISyncCrudRepository<TEntity, TId>,
      ISyncListOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}