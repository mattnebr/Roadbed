namespace Roadbed.Test.Unit.Data.Sqlite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the SqliteExecutor class.
/// </summary>
[TestClass]
public class SqliteExecutorTests
{
    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #region Public Methods

    #region ExecuteAsync Tests

    /// <summary>
    /// Unit test to verify that ExecuteAsync throws ArgumentNullException when request is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        var connectionFactory = this.CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.ExecuteAsync(nullRequest!, connectionFactory);
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
    /// Unit test to verify that ExecuteAsync throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var request = new DataExecutorRequest("SELECT 1");
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.ExecuteAsync(request, nullFactory!);
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
    /// Unit test to verify that ExecuteAsync executes a CREATE TABLE command successfully.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_CreateTable_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var request = new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteAsync(request, connectionFactory);

        // Assert (Then)
        Assert.AreEqual(
            0,
            result,
            "CREATE TABLE should return 0 rows affected.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync works with parameters.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_WithParameters_ExecutesSuccessfully()
    {
        // Arrange (Given)
        int result = 0;
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table first
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            var insertRequest = new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (@Id, @Name)")
            {
                Parameters = new { Id = 1, Name = "Test" },
                RetriesEnabled = false,
            };

            // Act (When)
            result = await SqliteExecutor.ExecuteAsync(insertRequest, connectionFactory);
        }

        // Assert (Then)
        Assert.AreEqual(
            1,
            result,
            "Parameterized INSERT should return 1 row affected.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync works without logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_WithoutLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var request = new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteAsync(request, connectionFactory, logger: null);

        // Assert (Then)
        Assert.AreEqual(
            0,
            result,
            "ExecuteAsync should work without a logger.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync works with logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_WithLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();
        var request = new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteAsync(request, connectionFactory, logger);

        // Assert (Then)
        Assert.AreEqual(
            0,
            result,
            "ExecuteAsync should work with a logger.");
        Assert.IsNotEmpty(
            logger.LoggedMessages,
            "Logger should have captured log messages.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteAsync with retries disabled executes only once.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_RetriesDisabled_ExecutesOnce()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var request = new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteAsync(request, connectionFactory);

        // Assert (Then)
        Assert.AreEqual(
            0,
            result,
            "Should execute successfully without retries.");
    }

    #endregion ExecuteAsync Tests

    #region QueryAsync Tests

    /// <summary>
    /// Unit test to verify that QueryAsync throws ArgumentNullException when request is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        var connectionFactory = this.CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.QueryAsync<TestDto>(nullRequest!, connectionFactory);
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
    /// Unit test to verify that QueryAsync throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var request = new DataExecutorRequest("SELECT 1");
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.QueryAsync<TestDto>(request, nullFactory!);
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
    /// Unit test to verify that QueryAsync returns data successfully.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_ValidQuery_ReturnsData()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1'), (2, 'Test2')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QueryAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return results.");
            Assert.AreEqual(
                2,
                result.Count(),
                "Query should return 2 rows.");
        }
    }

    /// <summary>
    /// Unit test to verify that QueryAsync works with parameters.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_WithParameters_ReturnsFilteredData()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1'), (2, 'Test2')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable WHERE Id = @Id")
            {
                Parameters = new { Id = 1 },
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QueryAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return results.");
            Assert.AreEqual(
                1,
                result.Count(),
                "Query should return 1 row.");
            Assert.AreEqual(
                "Test1",
                result.First().Name,
                "Query should return the correct row.");
        }
    }

    /// <summary>
    /// Unit test to verify that QueryAsync returns empty collection when no rows match.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_NoMatchingRows_ReturnsEmptyCollection()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table without data
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QueryAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a collection.");
            Assert.AreEqual(
                0,
                result.Count(),
                "Query should return empty collection.");
        }
    }

    /// <summary>
    /// Unit test to verify that QueryAsync works with logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QueryAsync_WithLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QueryAsync<TestDto>(queryRequest, connectionFactory, logger);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should execute successfully.");
            Assert.IsNotEmpty(
                logger.LoggedMessages,
                "Logger should have captured log messages.");
        }
    }

    #endregion QueryAsync Tests

    #region QuerySingleOrDefaultAsync Tests

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync throws ArgumentNullException when request is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        var connectionFactory = this.CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(nullRequest!, connectionFactory);
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
    /// Unit test to verify that QuerySingleOrDefaultAsync throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var request = new DataExecutorRequest("SELECT 1");
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(request, nullFactory!);
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
    /// Unit test to verify that QuerySingleOrDefaultAsync returns a single result.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_SingleRow_ReturnsResult()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable WHERE Id = 1")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                1,
                result.Id,
                "Query should return the correct row.");
            Assert.AreEqual(
                "Test1",
                result.Name,
                "Query should return the correct row.");
        }
    }

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync returns default when no rows found.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_NoRows_ReturnsDefault()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table without data
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable WHERE Id = 999")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNull(
                result,
                "Query should return null when no rows are found.");
        }
    }

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync works with parameters.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_WithParameters_ReturnsResult()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1'), (2, 'Test2')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable WHERE Name = @Name")
            {
                Parameters = new { Name = "Test2" },
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(queryRequest, connectionFactory);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                2,
                result.Id,
                "Query should return the correct row.");
        }
    }

    /// <summary>
    /// Unit test to verify that QuerySingleOrDefaultAsync works with logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task QuerySingleOrDefaultAsync_WithLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var queryRequest = new DataExecutorRequest("SELECT Id, Name FROM TestTable WHERE Id = 1")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            var result = await SqliteExecutor.QuerySingleOrDefaultAsync<TestDto>(queryRequest, connectionFactory, logger);

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should execute successfully.");
            Assert.IsNotEmpty(
                logger.LoggedMessages,
                "Logger should have captured log messages.");
        }
    }

    #endregion QuerySingleOrDefaultAsync Tests

    #region ExecuteScalarAsync Tests

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync throws ArgumentNullException when request is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataExecutorRequest? nullRequest = null;
        var connectionFactory = this.CreateConnectionFactory();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.ExecuteScalarAsync<long>(nullRequest!, connectionFactory);
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
    /// Unit test to verify that ExecuteScalarAsync throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var request = new DataExecutorRequest("SELECT 1");
        IDataConnectionFactory? nullFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await SqliteExecutor.ExecuteScalarAsync<long>(request, nullFactory!);
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
    /// Unit test to verify that ExecuteScalarAsync returns last inserted row ID.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_InsertWithLastInsertRowId_ReturnsNewId()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var insertRequest = new DataExecutorRequest(
                "INSERT INTO TestTable (Name) VALUES (@Name); SELECT last_insert_rowid();")
            {
                Parameters = new { Name = "Test1" },
                RetriesEnabled = false,
            };

            // Act (When)
            long newId = await SqliteExecutor.ExecuteScalarAsync<long>(insertRequest, connectionFactory);

            // Assert (Then)
            Assert.AreEqual(
                1L,
                newId,
                "ExecuteScalarAsync should return the ID of the newly inserted row.");
        }
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync returns count from aggregate query.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_CountQuery_ReturnsCount()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test1'), (2, 'Test2'), (3, 'Test3')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var countRequest = new DataExecutorRequest("SELECT COUNT(*) FROM TestTable")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            long count = await SqliteExecutor.ExecuteScalarAsync<long>(countRequest, connectionFactory);

            // Assert (Then)
            Assert.AreEqual(
                3L,
                count,
                "ExecuteScalarAsync should return the count of rows in the table.");
        }
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync works with parameters.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_WithParameters_ReturnsScalarValue()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT, Age INTEGER)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name, Age) VALUES (1, 'Test1', 25), (2, 'Test2', 30)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var scalarRequest = new DataExecutorRequest("SELECT Age FROM TestTable WHERE Id = @Id")
            {
                Parameters = new { Id = 2 },
                RetriesEnabled = false,
            };

            // Act (When)
            int age = await SqliteExecutor.ExecuteScalarAsync<int>(scalarRequest, connectionFactory);

            // Assert (Then)
            Assert.AreEqual(
                30,
                age,
                "ExecuteScalarAsync should return the scalar value from the specified row.");
        }
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync works without logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_WithoutLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var request = new DataExecutorRequest("SELECT 42")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteScalarAsync<int>(request, connectionFactory, logger: null);

        // Assert (Then)
        Assert.AreEqual(
            42,
            result,
            "ExecuteScalarAsync should work without a logger.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync works with logger.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_WithLogger_ExecutesSuccessfully()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();
        var request = new DataExecutorRequest("SELECT 42")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteScalarAsync<int>(request, connectionFactory, logger);

        // Assert (Then)
        Assert.AreEqual(
            42,
            result,
            "ExecuteScalarAsync should work with a logger.");
        Assert.IsNotEmpty(
            logger.LoggedMessages,
            "Logger should have captured log messages.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync with retries disabled executes only once.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_RetriesDisabled_ExecutesOnce()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        var request = new DataExecutorRequest("SELECT 100")
        {
            RetriesEnabled = false,
        };

        // Act (When)
        int result = await SqliteExecutor.ExecuteScalarAsync<int>(request, connectionFactory);

        // Assert (Then)
        Assert.AreEqual(
            100,
            result,
            "Should execute successfully without retries.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync returns correct type for string result.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_StringResult_ReturnsCorrectType()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create and populate table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'TestName')")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var scalarRequest = new DataExecutorRequest("SELECT Name FROM TestTable WHERE Id = 1")
            {
                RetriesEnabled = false,
            };

            // Act (When)
            string? result = await SqliteExecutor.ExecuteScalarAsync<string>(scalarRequest, connectionFactory);

            // Assert (Then)
            Assert.AreEqual(
                "TestName",
                result,
                "ExecuteScalarAsync should return string scalar value correctly.");
        }
    }

    #endregion ExecuteScalarAsync Tests

    #region Retry Logic Tests

    /// <summary>
    /// Unit test to verify that ExecuteAsync retries on transient errors.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_TransientError_RetriesAndSucceeds()
    {
        // Arrange (Given)
        int result = 0;
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            await SqliteExecutor.ExecuteAsync(
            new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)")
            {
                RetriesEnabled = false,
            },
            connectionFactory);

            var request = new DataExecutorRequest("INSERT INTO TestTable (Id, Name) VALUES (1, 'Test')")
            {
                RetriesEnabled = true,
                MaxRetries = 3,
                DelayBetweenRetries = TimeSpan.FromMilliseconds(10),
                DelayMultiplierEnabled = false,
            };

            // Act (When)
            result = await SqliteExecutor.ExecuteAsync(request, connectionFactory, logger);
        }

        // Assert (Then)
        Assert.AreEqual(
            1,
            result,
            "Command should eventually succeed with retries.");
    }

    /// <summary>
    /// Unit test to verify that retry delay multiplier works correctly.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteAsync_WithDelayMultiplier_CalculatesDelayCorrectly()
    {
        // Arrange (Given)
        int result = 0;
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var request = new DataExecutorRequest("INSERT INTO TestTable (Id) VALUES (1)")
            {
                RetriesEnabled = true,
                MaxRetries = 2,
                DelayBetweenRetries = TimeSpan.FromMilliseconds(50),
                DelayMultiplierEnabled = true,
            };

            var startTime = DateTime.UtcNow;

            // Act (When)
            result = await SqliteExecutor.ExecuteAsync(request, connectionFactory);

            var elapsed = DateTime.UtcNow - startTime;
        }

        // Assert (Then)
        Assert.AreEqual(
            1,
            result,
            "Command should succeed.");
    }

    /// <summary>
    /// Unit test to verify that ExecuteScalarAsync retries on transient errors.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ExecuteScalarAsync_TransientError_RetriesAndSucceeds()
    {
        // Arrange (Given)
        long result = 0;
        var connectionFactory = this.CreateConnectionFactory();
        var logger = new TestLogger();

        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest("CREATE TABLE TestTable (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)")
                {
                    RetriesEnabled = false,
                },
                connectionFactory);

            var request = new DataExecutorRequest(
                "INSERT INTO TestTable (Name) VALUES (@Name); SELECT last_insert_rowid();")
            {
                Parameters = new { Name = "Test" },
                RetriesEnabled = true,
                MaxRetries = 3,
                DelayBetweenRetries = TimeSpan.FromMilliseconds(10),
                DelayMultiplierEnabled = false,
            };

            // Act (When)
            result = await SqliteExecutor.ExecuteScalarAsync<long>(request, connectionFactory, logger);
        }

        // Assert (Then)
        Assert.AreEqual(
            1L,
            result,
            "Scalar command should eventually succeed with retries.");
    }

    #endregion Retry Logic Tests

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a connection factory with a unique in-memory database.
    /// </summary>
    /// <returns>Connection factory for testing.</returns>
    private IDataConnectionFactory CreateConnectionFactory()
    {
        string uniqueDbName = $"TestDb_{Guid.NewGuid():N}";
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory)
        {
            DatabaseSource = uniqueDbName,
        };
        return new SqliteConnectionFactory(connectionString);
    }

    #endregion Private Methods

    #region Private Classes

    /// <summary>
    /// Test DTO for query results.
    /// </summary>
    private class TestDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    /// <summary>
    /// Test logger implementation for capturing log messages.
    /// </summary>
    private class TestLogger : ILogger
    {
        public List<string> LoggedMessages { get; } = new List<string>();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.LoggedMessages.Add(formatter(state, exception));
        }
    }

    #endregion Private Classes
}