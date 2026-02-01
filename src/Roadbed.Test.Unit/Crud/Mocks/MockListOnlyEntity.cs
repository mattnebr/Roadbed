namespace Roadbed.Test.Unit.Crud.Mocks;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Mock service for testing <see cref="BaseAsyncListOnlyService{TEntity, TId}"/>.
/// </summary>
public class MockListOnlyEntity
    : BaseAsyncListOnlyService<MockDto, int>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MockListOnlyEntity"/> class.
    /// </summary>
    /// <param name="repository">Mock repository for data management.</param>
    /// <param name="logger">Mock logger for log messages.</param>
    public MockListOnlyEntity(
        IAsyncListOnlyRepository<MockDto, int> repository,
        ILogger logger)
        : base(repository, logger)
    {
    }

    #endregion Public Constructors
}