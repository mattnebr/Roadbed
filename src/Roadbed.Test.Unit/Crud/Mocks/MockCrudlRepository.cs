namespace Roadbed.Test.Unit.Crud.Mocks;

using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Mock repository for testing.
/// </summary>
public class MockCrudlRepository
    : IAsyncCrudlRepository<MockDto, int>
{
    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether CreateAsync was called.
    /// </summary>
    public bool CreateCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether DeleteAsync was called.
    /// </summary>
    public bool DeleteCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating the last CancellationToken passed.
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ListAsync was called.
    /// </summary>
    public bool ListCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ReadAsync was called.
    /// </summary>
    public bool ReadCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether UpdateAsync was called.
    /// </summary>
    public bool UpdateCalled { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc />
    public Task<MockDto> CreateAsync(
        MockDto entity,
        CancellationToken cancellationToken = default)
    {
        this.CreateCalled = true;
        this.LastCancellationToken = cancellationToken;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<MockDto?> ReadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        this.ReadCalled = true;
        this.LastCancellationToken = cancellationToken;

        MockDto? mock = id > 0
            ? new MockDto { Id = id, Name = "Test" }
            : null;

        return Task.FromResult(mock);
    }

    /// <inheritdoc />
    public Task<MockDto> UpdateAsync(
        MockDto entity,
        CancellationToken cancellationToken = default)
    {
        this.UpdateCalled = true;
        this.LastCancellationToken = cancellationToken;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        this.DeleteCalled = true;
        this.LastCancellationToken = cancellationToken;

        if (id <= 0)
        {
            throw new InvalidOperationException("Entity not found.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IList<MockDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.ListCalled = true;
        this.LastCancellationToken = cancellationToken;

        IList<MockDto> list = new List<MockDto>
        {
            new MockDto { Id = 1, Name = "Test" },
        };

        return Task.FromResult(list);
    }

    #endregion Public Methods
}