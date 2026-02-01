namespace Roadbed.Test.Unit.Crud;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Test.Unit.Crud.Mocks;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="Roadbed.Crud.Services.Async.BaseAsyncCrudlService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseEntityWithCrudlTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor with ILogger initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_InitializesSuccessfully()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();

        // Act (When)
        var entity = new MockCrudlEntity(repository, logger);

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
        IAsyncCrudlRepository<MockDto, int>? nullRepository = null;
        var logger = new MockLogger();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var entity = new MockCrudlEntity(nullRepository!, logger);
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
    /// Unit test to verify that CreateAsync returns the created entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task CreateAsync_ValidEntity_ReturnsCreatedEntity()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Test" };

        // Act (When)
        var result = await entity.CreateAsync(dto);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "CreateAsync should return the created entity.");
        Assert.AreEqual(
            100,
            result.Id,
            "Created entity should have the expected Id.");
        Assert.IsTrue(
            repository.CreateCalled,
            "Repository CreateAsync should be called.");
    }

    /// <summary>
    /// Unit test to verify that CreateAsync passes the CancellationToken to
    /// the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task CreateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Test" };
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.CreateAsync(dto, cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync returns an entity for a valid ID.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_ValidId_ReturnsEntity()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int id = 100;

        // Act (When)
        var result = await entity.ReadAsync(id);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "ReadAsync should return an entity for a valid ID.");
        Assert.IsTrue(
            repository.ReadCalled,
            "Repository ReadAsync should be called.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync returns null for an invalid ID.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_InvalidId_ReturnsNull()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int invalidId = 0;

        // Act (When)
        var result = await entity.ReadAsync(invalidId);

        // Assert (Then)
        Assert.IsNull(
            result,
            "ReadAsync should return null when the entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync passes the CancellationToken to
    /// the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int id = 100;
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.ReadAsync(id, cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync returns the updated entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_ValidEntity_ReturnsUpdatedEntity()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Updated" };

        // Act (When)
        var result = await entity.UpdateAsync(dto);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "UpdateAsync should return the updated entity.");
        Assert.AreEqual(
            "Updated",
            result.Name,
            "Updated entity should have the expected Name.");
        Assert.IsTrue(
            repository.UpdateCalled,
            "Repository UpdateAsync should be called.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync passes the CancellationToken to
    /// the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Updated" };
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.UpdateAsync(dto, cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync deletes successfully with a valid ID.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_ValidId_DeletesSuccessfully()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int id = 100;

        // Act (When)
        await entity.DeleteAsync(id);

        // Assert (Then)
        Assert.IsTrue(
            repository.DeleteCalled,
            "Repository DeleteAsync should be called.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync throws when the entity is not found.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_InvalidId_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int invalidId = 0;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await entity.DeleteAsync(invalidId);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "DeleteAsync should throw InvalidOperationException when entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync passes the CancellationToken to
    /// the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int id = 100;
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.DeleteAsync(id, cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync returns a list of entities.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ListAsync_ValidCall_ReturnsListSuccessfully()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);

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
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var cts = new CancellationTokenSource();

        // Act (When)
        await entity.ListAsync(cts.Token);

        // Assert (Then)
        Assert.AreEqual(
            cts.Token,
            repository.LastCancellationToken,
            "CancellationToken should be passed through to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ExistsAsync returns true for an existing entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ExistsAsync_ValidId_ReturnsTrue()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int id = 100;

        // Act (When)
        var result = await entity.ExistsAsync(id);

        // Assert (Then)
        Assert.IsTrue(
            result,
            "ExistsAsync should return true when the entity exists.");
    }

    /// <summary>
    /// Unit test to verify that ExistsAsync returns false for a non-existing entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ExistsAsync_InvalidId_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        int invalidId = 0;

        // Act (When)
        var result = await entity.ExistsAsync(invalidId);

        // Assert (Then)
        Assert.IsFalse(
            result,
            "ExistsAsync should return false when the entity does not exist.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync calls CreateAsync for a new entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_NewEntity_CallsCreate()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 0, Name = "New" };

        // Act (When)
        var result = await entity.UpsertAsync(dto);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "UpsertAsync should return the created entity.");
        Assert.IsTrue(
            repository.CreateCalled,
            "Repository CreateAsync should be called for a new entity.");
        Assert.IsFalse(
            repository.UpdateCalled,
            "Repository UpdateAsync should not be called for a new entity.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync calls UpdateAsync for an existing entity.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Existing" };

        // Act (When)
        var result = await entity.UpsertAsync(dto);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "UpsertAsync should return the updated entity.");
        Assert.IsTrue(
            repository.UpdateCalled,
            "Repository UpdateAsync should be called for an existing entity.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync throws ArgumentNullException when
    /// entity is null.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var repository = new MockCrudlRepository();
        var logger = new MockLogger();
        var entity = new MockCrudlEntity(repository, logger);
        MockDto? nullDto = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await entity.UpsertAsync(nullDto!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "UpsertAsync should throw ArgumentNullException when entity is null.");
    }

    #endregion Public Methods
}