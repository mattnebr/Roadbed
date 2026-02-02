namespace Roadbed.Test.Unit.Data.Postgresql;

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Postgresql;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="PostgresqlExecutor"/> class.
/// </summary>
[TestClass]
public class PostgresqlExecutorTests
{
    #region Public Methods

    #region ExecuteAsync Tests

    /// <summary>
    /// Unit test to verify that ExecuteAsync throws when request is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteAsync(nullRequest!, factory);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "ExecuteAsync should throw ArgumentNullException when request is null.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync throws when connectionFactory is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteAsync(request, nullFactory!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "ExecuteAsync should throw ArgumentNullException when connectionFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync accepts null logger without throwing.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_NullLogger_DoesNotThrowArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool argumentNullThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteAsync(request, factory, logger: null);
        }
        catch (ArgumentNullException)
        {
            argumentNullThrown = true;
        }
        catch (Exception)
        {
            // Expected - connection will fail since there is no real database.
            // We only care that ArgumentNullException was not thrown for logger.
        }

        // Assert (Then)
        Assert.IsFalse(
            argumentNullThrown,
            "ExecuteAsync should not throw ArgumentNullException when logger is null.");
    }

    #endregion ExecuteAsync Tests

    #region QueryAsync Tests

    /// <summary>
    /// Unit test to verify that QueryAsync throws when request is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QueryAsync<string>(nullRequest!, factory);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "QueryAsync should throw ArgumentNullException when request is null.");
    }

    /// <summary>
    /// Unit test to verify that QueryAsync throws when connectionFactory is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QueryAsync<string>(request, nullFactory!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "QueryAsync should throw ArgumentNullException when connectionFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that QueryAsync accepts null logger without throwing.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NullLogger_DoesNotThrowArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool argumentNullThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QueryAsync<string>(request, factory, logger: null);
        }
        catch (ArgumentNullException)
        {
            argumentNullThrown = true;
        }
        catch (Exception)
        {
            // Expected - connection will fail since there is no real database.
        }

        // Assert (Then)
        Assert.IsFalse(
            argumentNullThrown,
            "QueryAsync should not throw ArgumentNullException when logger is null.");
    }

    #endregion QueryAsync Tests

    #region QuerySingleOrDefaultAsync Tests

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync throws when request is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QuerySingleOrDefaultAsync<string>(nullRequest!, factory);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "QuerySingleOrDefaultAsync should throw ArgumentNullException when request is null.");
    }

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync throws when connectionFactory is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QuerySingleOrDefaultAsync<string>(request, nullFactory!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "QuerySingleOrDefaultAsync should throw ArgumentNullException when connectionFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync accepts null logger without throwing.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NullLogger_DoesNotThrowArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool argumentNullThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.QuerySingleOrDefaultAsync<string>(request, factory, logger: null);
        }
        catch (ArgumentNullException)
        {
            argumentNullThrown = true;
        }
        catch (Exception)
        {
            // Expected - connection will fail since there is no real database.
        }

        // Assert (Then)
        Assert.IsFalse(
            argumentNullThrown,
            "QuerySingleOrDefaultAsync should not throw ArgumentNullException when logger is null.");
    }

    #endregion QuerySingleOrDefaultAsync Tests

    #region ExecuteScalarAsync Tests

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync throws when request is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteScalarAsync<int>(nullRequest!, factory);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "ExecuteScalarAsync should throw ArgumentNullException when request is null.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync throws when connectionFactory is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteScalarAsync<int>(request, nullFactory!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "ExecuteScalarAsync should throw ArgumentNullException when connectionFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync accepts null logger without throwing.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_NullLogger_DoesNotThrowArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest request = CreateRequest();
        IDataConnectionFactory factory = CreateConnectionFactory();
        bool argumentNullThrown = false;

        // Act (When)
        try
        {
            await PostgresqlExecutor.ExecuteScalarAsync<int>(request, factory, logger: null);
        }
        catch (ArgumentNullException)
        {
            argumentNullThrown = true;
        }
        catch (Exception)
        {
            // Expected - connection will fail since there is no real database.
        }

        // Assert (Then)
        Assert.IsFalse(
            argumentNullThrown,
            "ExecuteScalarAsync should not throw ArgumentNullException when logger is null.");
    }

    #endregion ExecuteScalarAsync Tests

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a <see cref="DataExecutorRequest"/> with minimal configuration
    /// for tests that validate input parameters before database interaction.
    /// </summary>
    /// <returns>A configured <see cref="DataExecutorRequest"/> instance.</returns>
    private static DataExecutorRequest CreateRequest()
    {
        return new DataExecutorRequest("SELECT 1");
    }

    /// <summary>
    /// Creates a <see cref="PostgresqlConnectionFactory"/> with an unreachable connection
    /// string. Used for tests that validate input parameters and never reach
    /// the database connection step.
    /// </summary>
    /// <returns>A configured <see cref="IDataConnectionFactory"/> instance.</returns>
    private static IDataConnectionFactory CreateConnectionFactory()
    {
        var connectionString = new DataConnecionString(
            DataConnectionStringType.PostgreSQL,
            "Host=localhost;Port=1;Database=testdb;Username=testuser;Password=testpass;Timeout=1");

        return new PostgresqlConnectionFactory(connectionString);
    }

    #endregion Private Methods
}