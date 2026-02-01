namespace Roadbed.Test.Unit.Crud.Mocks;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Mock service for testing <see cref="BaseAsyncCrudService{TEntity, TId}"/>.
/// </summary>
public class MockCrudEntity
    : BaseAsyncCrudService<MockDto, int>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MockCrudEntity"/> class.
    /// </summary>
    /// <param name="repository">Mock repository for data management.</param>
    /// <param name="logger">Mock logger for log messages.</param>
    public MockCrudEntity(
        IAsyncCrudRepository<MockDto, int> repository,
        ILogger logger)
        : base(repository, logger)
    {
    }

    #endregion Public Constructors
}