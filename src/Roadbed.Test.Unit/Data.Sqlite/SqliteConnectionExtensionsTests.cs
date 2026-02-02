namespace Roadbed.Test.Unit.Data.Sqlite;

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the SqliteConnectionExtensions class.
/// </summary>
[TestClass]
public class SqliteConnectionExtensionsTests
{
    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #region Public Methods

    /// <summary>
    /// Unit test to verify that connection can be reopened after KeepAlive disposes it.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_AfterDispose_ConnectionCanBeReopened()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        var keepAlive = connection.KeepAlive();
        keepAlive.Dispose();

        // Act (When)
        await connection.OpenAsync();

        // Assert (Then)
        Assert.AreEqual(
            ConnectionState.Open,
            connection.State,
            "Connection should be able to be reopened after KeepAlive disposes it.");
    }

    /// <summary>
    /// Unit test to verify that database is destroyed after KeepAlive is disposed.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_AfterDispose_DatabaseIsDestroyed()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        // Create table while KeepAlive is active
        using (var keepAlive = connection.KeepAlive())
        {
            using var createConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
            using var createCommand = createConnection.CreateCommand();
            createCommand.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)";
            createCommand.ExecuteNonQuery();
        }

        // keepAlive disposed here

        // Act (When) - Try to query the table after KeepAlive is disposed
        bool tableExists = false;
        try
        {
            using var queryConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
            using var queryCommand = queryConnection.CreateCommand();
            queryCommand.CommandText = "SELECT COUNT(*) FROM TestTable";
            queryCommand.ExecuteScalar();
            tableExists = true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // SQLITE_ERROR - no such table
            tableExists = false;
        }

        // Assert (Then)
        Assert.IsFalse(
            tableExists,
            "In-memory database should be destroyed after KeepAlive is disposed.");
    }

    /// <summary>
    /// Unit test to verify that KeepAlive opens connection if it is closed.
    /// </summary>
    [TestMethod]
    public void KeepAlive_ClosedConnection_OpensConnection()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        var connection = new SqliteConnection(connectionString.ConnectionString);

        try
        {
            // Act (When)
            var keepAlive = connection.KeepAlive();

            // Assert (Then)
            Assert.AreEqual(
                ConnectionState.Open,
                connection.State,
                "KeepAlive should open the connection if it was closed.");

            // Cleanup
            keepAlive.Dispose();
        }
        finally
        {
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that disposing KeepAlive closes the connection.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_Dispose_ClosesConnection()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        var keepAlive = connection.KeepAlive();

        // Act (When)
        keepAlive.Dispose();

        // Assert (Then)
        Assert.AreEqual(
            ConnectionState.Closed,
            connection.State,
            "Connection should be closed after KeepAlive is disposed.");
    }

    /// <summary>
    /// Unit test to verify that disposing KeepAlive multiple times is safe.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_DisposeMultipleTimes_IsSafe()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        var keepAlive = connection.KeepAlive();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            keepAlive.Dispose();
            keepAlive.Dispose();
            keepAlive.Dispose();
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "Disposing KeepAlive multiple times should be safe.");
    }

    /// <summary>
    /// Unit test to verify that KeepAlive preserves in-memory database across operations.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_InMemoryDatabase_PreservesDataAcrossConnections()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table with first connection
            using (var createConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken))
            {
                using var createCommand = createConnection.CreateCommand();
                createCommand.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)";
                createCommand.ExecuteNonQuery();
            }

            // Insert data with second connection
            using (var insertConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken))
            {
                using var insertCommand = insertConnection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO TestTable (Id, Name) VALUES (1, 'Test')";
                insertCommand.ExecuteNonQuery();
            }

            // Act (When) - Query data with third connection
            using (var queryConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken))
            {
                using var queryCommand = queryConnection.CreateCommand();
                queryCommand.CommandText = "SELECT COUNT(*) FROM TestTable";
                var count = Convert.ToInt32(queryCommand.ExecuteScalar());

                // Assert (Then)
                Assert.AreEqual(
                    1,
                    count,
                    "In-memory database should preserve data across connections while KeepAlive is active.");
            }
        }
    }

    /// <summary>
    /// Unit test to verify that KeepAlive throws ArgumentNullException when connection is null.
    /// </summary>
    [TestMethod]
    public void KeepAlive_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        SqliteConnection? nullConnection = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var keepAlive = nullConnection!.KeepAlive();
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "KeepAlive should throw ArgumentNullException when connection is null.");
    }

    /// <summary>
    /// Unit test to verify that KeepAlive maintains connection in open state.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_OpenConnection_MaintainsOpenState()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        // Act (When)
        var keepAlive = connection.KeepAlive();

        // Assert (Then)
        Assert.AreEqual(
            ConnectionState.Open,
            connection.State,
            "Connection should remain open while KeepAlive is active.");

        // Cleanup
        keepAlive.Dispose();
    }

    /// <summary>
    /// Unit test to verify that KeepAlive returns an IDisposable.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_ValidConnection_ReturnsIDisposable()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        // Act (When)
        var keepAlive = connection.KeepAlive();

        // Assert (Then)
        Assert.IsNotNull(
            keepAlive,
            "KeepAlive should return a non-null IDisposable.");
        Assert.IsInstanceOfType(
            keepAlive,
            typeof(IDisposable),
            "KeepAlive should return an IDisposable object.");

        // Cleanup
        keepAlive.Dispose();
    }

    /// <summary>
    /// Unit test to verify that KeepAlive works with using declaration.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_WithUsingDeclaration_WorksCorrectly()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using var keepAlive = connection.KeepAlive();

        // Act (When) - Create table
        using var createConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using var createCommand = createConnection.CreateCommand();
        createCommand.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)";
        int result = createCommand.ExecuteNonQuery();

        // Assert (Then)
        Assert.AreEqual(
            0,
            result,
            "CREATE TABLE should execute successfully with using declaration.");
    }

    /// <summary>
    /// Unit test to verify that KeepAlive works with using statement.
    /// </summary>
    /// <returns>A unit of work representing the execution of the task.</returns>
    [TestMethod]
    public async Task KeepAlive_WithUsingStatement_WorksCorrectly()
    {
        // Arrange (Given)
        var connectionFactory = this.CreateConnectionFactory();

        // Act (When)
        using var connection = (SqliteConnection)await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            using var createConnection = await connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
            using var createCommand = createConnection.CreateCommand();
            createCommand.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY)";
            createCommand.ExecuteNonQuery();

            // Assert (Then) - Inside using block
            Assert.AreNotEqual(
                ConnectionState.Closed,
                connection.State,
                "Original connection should be closed after KeepAlive closes it.");
        }

        // Assert (Then) - After using block
        Assert.AreEqual(
            ConnectionState.Closed,
            connection.State,
            "Connection should remain closed after using block.");
    }

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
}