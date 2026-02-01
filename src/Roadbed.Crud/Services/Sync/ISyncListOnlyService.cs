namespace Roadbed.Crud.Services.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync list-only service. Provides only the List operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncListOnlyService<TEntity, TId>
    : ISyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}