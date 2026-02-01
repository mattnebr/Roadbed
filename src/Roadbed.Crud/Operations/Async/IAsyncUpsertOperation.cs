namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Upsert operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Upsert is a service-level operation. It is not implemented at the repository
/// level. The default service implementation composes from
/// <see cref="IAsyncExistsOperation{TEntity, TId}.ExistsAsync"/>,
/// <see cref="IAsyncCreateOperation{TEntity, TId}.CreateAsync"/>, and
/// <see cref="IAsyncUpdateOperation{TEntity, TId}.UpdateAsync"/>.
/// </para>
/// <para>
/// When the entity's Id is null, Create is called. When the entity's Id is not
/// null and Exists returns true, Update is called. When the entity's Id is not
/// null and Exists returns false, Create is called.
/// </para>
/// <para>
/// Override the default implementation when the data source supports native upsert
/// (SQL Server MERGE, PostgreSQL ON CONFLICT, SQLite ON CONFLICT).
/// </para>
/// </remarks>
public interface IAsyncUpsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Creates or updates an entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to create or update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created or updated entity.</returns>
    Task<TEntity> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}