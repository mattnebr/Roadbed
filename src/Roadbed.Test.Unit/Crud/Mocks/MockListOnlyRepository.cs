namespace Roadbed.Test.Unit.Crud.Mocks;

using System.Collections.Generic;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Mock repository for testing.
/// </summary>
public class MockListOnlyRepository
    : IAsyncListOnlyRepository<MockDto, int>
{
    #region Public Properties

    /// <summary>
    /// Gets a value indicating the last CancellationToken passed.
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ListAsync was called.
    /// </summary>
    public bool ListCalled { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc />
    public async Task<IList<MockDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        this.ListCalled = true;
        this.LastCancellationToken = cancellationToken;

        IList<MockDto> list = new List<MockDto>()
            {
                new MockDto { Id = 1, Name = "Test" },
            };

        return list;
    }

    #endregion Public Methods
}