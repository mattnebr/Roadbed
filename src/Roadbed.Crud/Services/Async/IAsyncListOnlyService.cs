namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async list-only service. Provides only the List operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncListOnlyService<TEntity, TId>
    : IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}