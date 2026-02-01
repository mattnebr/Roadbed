namespace Roadbed.Test.Unit.Crud;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Test.Unit.Crud.Mocks;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="Roadbed.Crud.Services.Async.BaseAsyncListOnlyService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseEntityWithListOnlyTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor with ILogger initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_InitializesSuccessfully()
    {
        // Arrange (Given)
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();

        // Act (When)
        var entity = new MockListOnlyEntity(repository, logger);

        // Assert (Then)
        Assert.IsNotNull(
            entity,
            "Entity should be initialized when valid repository and logger are provided.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentNullException when
    /// repository is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        IAsyncListOnlyRepository<MockDto, int>? nullRepository = null;
        var logger = new MockLogger();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var entity = new MockListOnlyEntity(nullRepository!, logger);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync returns a list of entities successfully.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ListAsync_ValidCall_ReturnsListSuccessfully()
    {
        // Arrange (Given)
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();
        var entity = new MockListOnlyEntity(repository, logger);

        // Act (When)
        var result = await entity.ListAsync();

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "ListAsync should return a non-null list.");
        Assert.IsTrue(
            repository.ListCalled,
            "Repository ListAsync should be called when service ListAsync is invoked.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync passes the CancellationToken to
    /// the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ListAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();
        var entity = new MockListOnlyEntity(repository, logger);
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.ListAsync(cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    #endregion Public Methods
}