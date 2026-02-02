namespace Roadbed.Test.Unit.Data.Sqlite;

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the SqliteConnectionFactory class.
/// </summary>
[TestClass]
public class SqliteConnectionFactoryTests
{
    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #region Public Methods

    #region Constructor Tests

    /// <summary>
    /// Unit test to verify that the constructor with connection string initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithConnectionString_InitializesProperties()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory);

        // Act (When)
        var instance = new SqliteConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully.");
        Assert.IsNotNull(
            instance.Connecion,
            "Connecion property should be initialized.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the same object that was passed to constructor.");
    }

    /// <summary>
    /// Unit test to verify that the constructor with null connection string throws ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataConnecionString? nullConnectionString = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var instance = new SqliteConnectionFactory(nullConnectionString!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when connection string is null.");
    }

    /// <summary>
    /// Unit test to verify that the constructor accepts file-based SQLite connection string.
    /// </summary>
    [TestMethod]
    public void Constructor_WithFileBased_SqliteConnectionString_InitializesCorrectly()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLite)
        {
            DatabaseSource = "test.db",
            TimeoutInSeconds = 30,
        };

        // Act (When)
        var instance = new SqliteConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with file-based SQLite connection.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the connection string object.");
        Assert.AreEqual(
            DataConnectionStringType.SQLite,
            instance.Connecion.ConnectionStringType,
            "Connection string type should be Sqlite.");
    }

    /// <summary>
    /// Unit test to verify that the constructor accepts in-memory SQLite connection string.
    /// </summary>
    [TestMethod]
    public void Constructor_WithInMemorySqliteConnectionString_InitializesCorrectly()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory);

        // Act (When)
        var instance = new SqliteConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with in-memory SQLite connection.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the connection string object.");
        Assert.AreEqual(
            DataConnectionStringType.SQLiteInMemory,
            instance.Connecion.ConnectionStringType,
            "Connection string type should be SqliteInMemory.");
    }

    /// <summary>
    /// Unit test to verify that the constructor accepts custom connection string.
    /// </summary>
    [TestMethod]
    public void Constructor_WithCustomConnectionString_InitializesCorrectly()
    {
        // Arrange (Given)
        string customConnectionString = "Data Source=custom.db;Mode=ReadWrite";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLite,
            customConnectionString);

        // Act (When)
        var instance = new SqliteConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with custom connection string.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the connection string object.");
        StringAssert.Contains(
            instance.Connecion.ConnectionString,
            "custom.db",
            "Connection string should contain custom database name.");
    }

    #endregion Constructor Tests

    #region Property Tests

    /// <summary>
    /// Unit test to verify that Connecion property returns the connection string that was set.
    /// </summary>
    [TestMethod]
    public void Connecion_Get_ReturnsConnectionString()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        var result = instance.Connecion;

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Connecion property should not be null.");
        Assert.AreSame(
            connectionString,
            result,
            "Connecion property should return the same object that was passed to constructor.");
    }

    /// <summary>
    /// Unit test to verify that Connecion property is read-only after initialization.
    /// </summary>
    [TestMethod]
    public void Connecion_Init_IsReadOnlyAfterConstruction()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        var result = instance.Connecion;

        // Assert (Then)
        Assert.AreSame(
            connectionString,
            result,
            "Connecion property should be immutable after construction.");
    }

    #endregion Property Tests

    #region CreateOpenConnection Tests (Synchronous)

    /// <summary>
    /// Unit test to verify that CreateOpenConnection creates an open connection with in-memory database.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_InMemoryDatabase_CreatesOpenConnection()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = instance.CreateOpenConnection();

        try
        {
            // Assert (Then)
            Assert.IsNotNull(
                connection,
                "CreateOpenConnection should return a connection object.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection.State,
                "Connection should be in Open state.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnection returns SqliteConnection type.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_ValidConnectionString_ReturnsSqliteConnection()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = instance.CreateOpenConnection();

        try
        {
            // Assert (Then)
            Assert.IsInstanceOfType(
                connection,
                typeof(SqliteConnection),
                "Connection should be of type SqliteConnection.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnection returns a working connection.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_ValidConnectionString_ReturnsWorkingConnection()
    {
        // Arrange (Given)
        // Use unique in-memory database name to avoid conflicts
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = instance.CreateOpenConnection();

        try
        {
            // Create a test table to verify connection works
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)";
                command.ExecuteNonQuery();
            }

            // Insert test data
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO TestTable (Id, Name) VALUES (1, 'Test')";
                int rowsAffected = command.ExecuteNonQuery();

                // Assert (Then)
                Assert.AreEqual(
                    1,
                    rowsAffected,
                    "Connection should be able to execute commands successfully.");
            }
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnection creates independent connections.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_CalledMultipleTimes_CreatesIndependentConnections()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection1 = instance.CreateOpenConnection();
        IDbConnection connection2 = instance.CreateOpenConnection();

        try
        {
            // Assert (Then)
            Assert.AreNotSame(
                connection1,
                connection2,
                "Each call should create a new independent connection.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection1.State,
                "First connection should be open.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection2.State,
                "Second connection should be open.");
        }
        finally
        {
            // Cleanup
            connection1?.Dispose();
            connection2?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnection throws SqliteException with invalid file path.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_InvalidFilePath_ThrowsSqliteException()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLite,
            "Data Source=/invalid/path/that/does/not/exist/database.db;Mode=ReadOnly");
        var instance = new SqliteConnectionFactory(connectionString);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            IDbConnection connection = instance.CreateOpenConnection();
            connection?.Dispose();
        }
        catch (SqliteException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CreateOpenConnection should throw SqliteException when file path is invalid.");
    }

    #endregion CreateOpenConnection Tests (Synchronous)

    #region CreateOpenConnectionAsync Tests (Asynchronous)

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync creates an open connection with in-memory database.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_InMemoryDatabase_CreatesOpenConnection()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        try
        {
            // Assert (Then)
            Assert.IsNotNull(
                connection,
                "CreateOpenConnectionAsync should return a connection object.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection.State,
                "Connection should be in Open state.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync returns SqliteConnection type.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_ValidConnectionString_ReturnsSqliteConnection()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        try
        {
            // Assert (Then)
            Assert.IsInstanceOfType(
                connection,
                typeof(SqliteConnection),
                "Connection should be of type SqliteConnection.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync returns a working connection.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_ValidConnectionString_ReturnsWorkingConnection()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        try
        {
            // Create a test table to verify connection works
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT)";
                command.ExecuteNonQuery();
            }

            // Insert test data
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO TestTable (Id, Name) VALUES (1, 'Test')";
                int rowsAffected = command.ExecuteNonQuery();

                // Assert (Then)
                Assert.AreEqual(
                    1,
                    rowsAffected,
                    "Connection should be able to execute commands successfully.");
            }
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync creates independent connections.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_CalledMultipleTimes_CreatesIndependentConnections()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        var instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection1 = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
        IDbConnection connection2 = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        try
        {
            // Assert (Then)
            Assert.AreNotSame(
                connection1,
                connection2,
                "Each call should create a new independent connection.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection1.State,
                "First connection should be open.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection2.State,
                "Second connection should be open.");
        }
        finally
        {
            // Cleanup
            connection1?.Dispose();
            connection2?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync throws SqliteException with invalid file path.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_InvalidFilePath_ThrowsSqliteException()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLite,
            "Data Source=/invalid/path/that/does/not/exist/database.db;Mode=ReadOnly");
        var instance = new SqliteConnectionFactory(connectionString);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            IDbConnection connection = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);
            connection?.Dispose();
        }
        catch (SqliteException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CreateOpenConnectionAsync should throw SqliteException when file path is invalid.");
    }

    #endregion CreateOpenConnectionAsync Tests (Asynchronous)

    #region Interface Implementation Tests

    /// <summary>
    /// Unit test to verify that SqliteConnectionFactory implements IDataConnectionFactory.
    /// </summary>
    [TestMethod]
    public void SqliteConnectionFactory_ImplementsIDataConnectionFactory()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);

        // Act (When)
        IDataConnectionFactory instance = new SqliteConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "SqliteConnectionFactory should implement IDataConnectionFactory.");
        Assert.IsInstanceOfType(
            instance,
            typeof(IDataConnectionFactory),
            "Instance should be assignable to IDataConnectionFactory.");
    }

    /// <summary>
    /// Unit test to verify that interface method CreateOpenConnection works correctly.
    /// </summary>
    [TestMethod]
    public void IDataConnectionFactory_CreateOpenConnection_WorksCorrectly()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        IDataConnectionFactory instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = instance.CreateOpenConnection();

        try
        {
            // Assert (Then)
            Assert.IsNotNull(
                connection,
                "CreateOpenConnection should return a connection through interface.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection.State,
                "Connection should be open when accessed through interface.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Unit test to verify that interface method CreateOpenConnectionAsync works correctly.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task IDataConnectionFactory_CreateOpenConnectionAsync_WorksCorrectly()
    {
        // Arrange (Given)
        string uniqueConnectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLiteInMemory,
            uniqueConnectionString);
        IDataConnectionFactory instance = new SqliteConnectionFactory(connectionString);

        // Act (When)
        IDbConnection connection = await instance.CreateOpenConnectionAsync(this.TestContext.CancellationToken);

        try
        {
            // Assert (Then)
            Assert.IsNotNull(
                connection,
                "CreateOpenConnectionAsync should return a connection through interface.");
            Assert.AreEqual(
                ConnectionState.Open,
                connection.State,
                "Connection should be open when accessed through interface.");
        }
        finally
        {
            // Cleanup
            connection?.Dispose();
        }
    }

    #endregion Interface Implementation Tests

    #endregion Public Methods
}