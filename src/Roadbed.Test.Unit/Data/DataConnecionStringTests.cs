namespace Roadbed.Test.Unit.Data;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using System.Data.Common;

/// <summary>
/// Contains unit tests for verifying the behavior of the DataConnecionString class.
/// </summary>
[TestClass]
public class DataConnecionStringTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that ConnectionString can be accessed multiple times consistently.
    /// </summary>
    [TestMethod]
    public void ConnectionString_AccessMultipleTimes_ReturnsConsistentValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLite)
        {
            DatabaseSource = "test.db",
        };

        // Act (When)
        string result1 = connectionString.ConnectionString;
        string result2 = connectionString.ConnectionString;
        string result3 = connectionString.ConnectionString;

        // Assert (Then)
        Assert.AreEqual(
            result1,
            result2,
            "ConnectionString should return consistent value on multiple accesses.");
        Assert.AreEqual(
            result2,
            result3,
            "ConnectionString should return consistent value on multiple accesses.");
    }

    /// <summary>
    /// Unit test to verify that ConnectionString returns original string when provided in constructor.
    /// </summary>
    [TestMethod]
    public void ConnectionString_OriginalStringProvided_ReturnsOriginalString()
    {
        // Arrange (Given)
        string originalString = "Custom Connection String;Option=Value;";
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLite, originalString);

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.AreEqual(
            originalString,
            result,
            "ConnectionString should return the original connection string when provided.");
    }

    /// <summary>
    /// Unit test to verify that ConnectionString handles null DatabaseSource for Sqlite.
    /// </summary>
    [TestMethod]
    public void ConnectionString_SqliteWithNullDatabaseSource_GeneratesStringWithNull()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLite)
        {
            DatabaseSource = null,
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Data Source=",
            result,
            "Connection string should contain Data Source parameter.");
    }

    /// <summary>
    /// Unit test to verify that ConnectionString returns empty for Unknown type without original string.
    /// </summary>
    [TestMethod]
    public void ConnectionString_UnknownTypeWithoutOriginal_ReturnsEmptyString()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.AreEqual(
            string.Empty,
            result,
            "ConnectionString should return empty string for Unknown type without original string.");
    }

    /// <summary>
    /// Unit test to verify that ConnectionStringBuilder creates new instance each time.
    /// </summary>
    [TestMethod]
    public void ConnectionStringBuilder_AccessMultipleTimes_CreatesNewInstances()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLite,
            "Data Source=test.db;");

        // Act (When)
        DbConnectionStringBuilder builder1 = connectionString.ConnectionStringBuilder;
        DbConnectionStringBuilder builder2 = connectionString.ConnectionStringBuilder;

        // Assert (Then)
        Assert.AreNotSame(
            builder1,
            builder2,
            "ConnectionStringBuilder should create new instances each time.");
    }

    /// <summary>
    /// Unit test to verify that ConnectionStringBuilder property returns valid DbConnectionStringBuilder.
    /// </summary>
    [TestMethod]
    public void ConnectionStringBuilder_AccessProperty_ReturnsValidBuilder()
    {
        // Arrange (Given)
        string originalString = "Data Source=test.db;Version=3;";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.SQLite,
            originalString);

        // Act (When)
        DbConnectionStringBuilder builder = connectionString.ConnectionStringBuilder;

        // The DbConnectionStringBuilder class is designed to handle the standard format of a database connection string,
        // where the semicolon (;) acts purely as a key-value pair separator, not a terminator. Therefore, it will
        // automatically remove any trailing semicolons from the ConnectionString property because they indicate an empty
        // or incomplete key-value pair, which is not necessary for a valid connection.
        string actualConnectionString = builder.ConnectionString.EndsWith(';') ?
            builder.ConnectionString :
            string.Concat(builder.ConnectionString, ";");

        // Assert (Then)
        Assert.IsNotNull(
            builder,
            "ConnectionStringBuilder should not be null.");

        // The DbConnectionStringBuilder class and its derived classes (like SqlConnectionStringBuilder or
        // OdbcConnectionStringBuilder) automatically handle key casing: keywords are not case-sensitive to the builder
        // or the database provider in ADO.NET.
        Assert.AreEqual(
            originalString.ToLower(),
            actualConnectionString.ToLower(),
            "ConnectionStringBuilder should contain the connection string.");
    }

    /// <summary>
    /// Unit test to verify that different connection string types are handled correctly.
    /// </summary>
    [TestMethod]
    public void ConnectionStringType_DifferentTypes_HandledCorrectly()
    {
        // Arrange (Given)
        var sqliteConnection = new DataConnecionString(DataConnectionStringType.SQLite)
        {
            DatabaseSource = "file.db",
        };
        var inMemoryConnection = new DataConnecionString(DataConnectionStringType.SQLiteInMemory);
        var postgresqlConnection = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
        };
        var unknownConnection = new DataConnecionString(DataConnectionStringType.Unknown);

        // Act (When)
        string sqliteResult = sqliteConnection.ConnectionString;
        string inMemoryResult = inMemoryConnection.ConnectionString;
        string postgresqlResult = postgresqlConnection.ConnectionString;
        string unknownResult = unknownConnection.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Data Source=file.db",
            sqliteResult,
            "Sqlite connection string should contain database source.");
        Assert.Contains(
            "Mode=Memory",
            inMemoryResult,
            "In-memory connection string should contain memory mode.");
        Assert.Contains(
            "Host=localhost",
            postgresqlResult,
            "PostgreSQL connection string should contain host.");
        Assert.AreEqual(
            string.Empty,
            unknownResult,
            "Unknown connection string should be empty.");
    }

    /// <summary>
    /// Unit test to verify that parameterized constructor handles empty connection string.
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyConnectionString_InitializesWithEmptyString()
    {
        // Arrange (Given)
        string emptyConnectionString = string.Empty;
        DataConnectionStringType type = DataConnectionStringType.SQLite;

        // Act (When)
        var connectionString = new DataConnecionString(type, emptyConnectionString);

        // Assert (Then)
        Assert.AreEqual(
            type,
            connectionString.ConnectionStringType,
            "ConnectionStringType should match the provided value.");
        Assert.AreNotEqual(
            emptyConnectionString,
            connectionString.ConnectionString,
            "ConnectionString should not be empty when type is Sqlite.");
    }

    /// <summary>
    /// Unit test to verify that the parameterless constructor initializes properties with default values.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesWithDefaultValues()
    {
        // Arrange (Given)

        // Act (When)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);

        // Assert (Then)
        Assert.AreEqual(
            DataConnectionStringType.Unknown,
            connectionString.ConnectionStringType,
            "ConnectionStringType should be initialized to Unknown.");
        Assert.AreEqual(
            20,
            connectionString.TimeoutInSeconds,
            "TimeoutInSeconds should be initialized to 20.");
        Assert.IsNull(
            connectionString.DatabaseSource,
            "DatabaseSource should be initialized to null.");
        Assert.IsNull(
            connectionString.Password,
            "Password should be initialized to null.");
        Assert.IsNull(
            connectionString.ServerName,
            "ServerName should be initialized to null.");
        Assert.IsNull(
            connectionString.Username,
            "Username should be initialized to null.");
        Assert.AreEqual(
            string.Empty,
            connectionString.ConnectionString,
            "ConnectionString should be empty when no values are set.");
    }

    /// <summary>
    /// Unit test to verify that parameterized constructor with null connection string behaves correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_NullConnectionString_TreatsAsEmpty()
    {
        // Arrange (Given)
        string? nullConnectionString = null;
        DataConnectionStringType type = DataConnectionStringType.SQLiteInMemory;

        // Act (When)
        var connectionString = new DataConnecionString(type, nullConnectionString!);

        // Assert (Then)
        Assert.Contains(
            "Mode=Memory",
            connectionString.ConnectionString,
            "ConnectionString should generate in-memory string when null is provided.");
    }

    /// <summary>
    /// Unit test to verify that the parameterized constructor initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidConnectionStringAndType_InitializesCorrectly()
    {
        // Arrange (Given)
        string expectedConnectionString = "Data Source=test.db;Version=3;";
        DataConnectionStringType expectedType = DataConnectionStringType.SQLite;

        // Act (When)
        var connectionString = new DataConnecionString(expectedType, expectedConnectionString);

        // Assert (Then)
        Assert.AreEqual(
            expectedType,
            connectionString.ConnectionStringType,
            "ConnectionStringType should match the provided value.");
        Assert.AreEqual(
            expectedConnectionString,
            connectionString.ConnectionString,
            "ConnectionString should return the original connection string.");
    }

    /// <summary>
    /// Unit test to verify that DatabaseSource property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void DatabaseSource_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        string expectedSource = "C:\\Data\\mydb.db";

        // Act (When)
        connectionString.DatabaseSource = expectedSource;

        // Assert (Then)
        Assert.AreEqual(
            expectedSource,
            connectionString.DatabaseSource,
            "DatabaseSource should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that nullable properties can be set to null.
    /// </summary>
    [TestMethod]
    public void NullableProperties_SetNull_ReturnsNull()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown)
        {
            DatabaseSource = "test",
            Password = "pass",
            ServerName = "server",
            Username = "user",
        };

        // Act (When)
        connectionString.DatabaseSource = null;
        connectionString.Password = null;
        connectionString.ServerName = null;
        connectionString.Username = null;

        // Assert (Then)
        Assert.IsNull(connectionString.DatabaseSource, "DatabaseSource should be null.");
        Assert.IsNull(connectionString.Password, "Password should be null.");
        Assert.IsNull(connectionString.ServerName, "ServerName should be null.");
        Assert.IsNull(connectionString.Username, "Username should be null.");
    }

    /// <summary>
    /// Unit test to verify that Password property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Password_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        string expectedPassword = "SecurePassword123!";

        // Act (When)
        connectionString.Password = expectedPassword;

        // Assert (Then)
        Assert.AreEqual(
            expectedPassword,
            connectionString.Password,
            "Password should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string contains Host parameter.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlType_ContainsHost()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "db.example.com",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Host=db.example.com",
            result,
            "PostgreSQL connection string should contain Host parameter with ServerName value.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string contains Database parameter.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlType_ContainsDatabase()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "weatherdb",
            Username = "admin",
            Password = "secret",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Database=weatherdb",
            result,
            "PostgreSQL connection string should contain Database parameter with DatabaseSource value.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string contains Username parameter.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlType_ContainsUsername()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "roadbed_user",
            Password = "secret",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Username=roadbed_user",
            result,
            "PostgreSQL connection string should contain Username parameter.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string contains Password parameter.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlType_ContainsPassword()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "P@ssw0rd!",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Password=P@ssw0rd!",
            result,
            "PostgreSQL connection string should contain Password parameter.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string contains Timeout parameter.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlType_ContainsTimeout()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
            TimeoutInSeconds = 45,
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Timeout=45",
            result,
            "PostgreSQL connection string should contain Timeout parameter with TimeoutInSeconds value.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string uses default timeout when not set.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlTypeDefaultTimeout_ContainsDefaultTimeout()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Timeout=20",
            result,
            "PostgreSQL connection string should contain default Timeout of 20 seconds.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string handles null properties.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlTypeNullProperties_GeneratesStringWithNulls()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL);

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Host=",
            result,
            "PostgreSQL connection string should contain Host parameter even when ServerName is null.");
        Assert.Contains(
            "Database=",
            result,
            "PostgreSQL connection string should contain Database parameter even when DatabaseSource is null.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL type with original string returns original string.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlTypeWithOriginalString_ReturnsOriginalString()
    {
        // Arrange (Given)
        string originalString = "Host=custom.server.com;Port=5433;Database=proddb;Username=app;Password=secret;Timeout=30";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.PostgreSQL,
            originalString);

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.AreEqual(
            originalString,
            result,
            "ConnectionString should return the original string when provided, bypassing the template.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL connection string can be accessed multiple times consistently.
    /// </summary>
    [TestMethod]
    public void ConnectionString_PostgresqlTypeAccessMultipleTimes_ReturnsConsistentValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
        };

        // Act (When)
        string result1 = connectionString.ConnectionString;
        string result2 = connectionString.ConnectionString;

        // Assert (Then)
        Assert.AreEqual(
            result1,
            result2,
            "PostgreSQL ConnectionString should return consistent value on multiple accesses.");
    }

    /// <summary>
    /// Unit test to verify that PostgreSQL empty original string falls through to template.
    /// </summary>
    [TestMethod]
    public void Constructor_PostgresqlTypeEmptyConnectionString_UsesTemplate()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL, string.Empty)
        {
            ServerName = "localhost",
            DatabaseSource = "mydb",
            Username = "admin",
            Password = "secret",
        };

        // Act (When)
        string result = connectionString.ConnectionString;

        // Assert (Then)
        Assert.Contains(
            "Host=localhost",
            result,
            "PostgreSQL connection string should use template when empty original string is provided.");
    }

    /// <summary>
    /// Unit test to verify that properties can be set independently.
    /// </summary>
    [TestMethod]
    public void Properties_SetAllIndependently_ReturnCorrectValues()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        string expectedSource = "mydb.db";
        string expectedPassword = "password";
        string expectedServerName = "server1";
        string expectedUsername = "user1";
        int expectedTimeout = 45;

        // Act (When)
        connectionString.DatabaseSource = expectedSource;
        connectionString.Password = expectedPassword;
        connectionString.ServerName = expectedServerName;
        connectionString.Username = expectedUsername;
        connectionString.TimeoutInSeconds = expectedTimeout;

        // Assert (Then)
        Assert.AreEqual(expectedSource, connectionString.DatabaseSource, "DatabaseSource should match.");
        Assert.AreEqual(expectedPassword, connectionString.Password, "Password should match.");
        Assert.AreEqual(expectedServerName, connectionString.ServerName, "ServerName should match.");
        Assert.AreEqual(expectedUsername, connectionString.Username, "Username should match.");
        Assert.AreEqual(expectedTimeout, connectionString.TimeoutInSeconds, "TimeoutInSeconds should match.");
    }

    /// <summary>
    /// Unit test to verify that ServerName property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void ServerName_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        string expectedServerName = "localhost";

        // Act (When)
        connectionString.ServerName = expectedServerName;

        // Assert (Then)
        Assert.AreEqual(
            expectedServerName,
            connectionString.ServerName,
            "ServerName should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that TimeoutInSeconds can be set to negative value.
    /// </summary>
    [TestMethod]
    public void TimeoutInSeconds_SetNegativeValue_ReturnsNegativeValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        int negativeTimeout = -1;

        // Act (When)
        connectionString.TimeoutInSeconds = negativeTimeout;

        // Assert (Then)
        Assert.AreEqual(
            negativeTimeout,
            connectionString.TimeoutInSeconds,
            "TimeoutInSeconds should return negative value when set to negative value.");
    }

    /// <summary>
    /// Unit test to verify that TimeoutInSeconds property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void TimeoutInSeconds_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        int expectedTimeout = 60;

        // Act (When)
        connectionString.TimeoutInSeconds = expectedTimeout;

        // Assert (Then)
        Assert.AreEqual(
            expectedTimeout,
            connectionString.TimeoutInSeconds,
            "TimeoutInSeconds should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that TimeoutInSeconds can be set to zero.
    /// </summary>
    [TestMethod]
    public void TimeoutInSeconds_SetZero_ReturnsZero()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);

        // Act (When)
        connectionString.TimeoutInSeconds = 0;

        // Assert (Then)
        Assert.AreEqual(
            0,
            connectionString.TimeoutInSeconds,
            "TimeoutInSeconds should return zero when set to zero.");
    }

    /// <summary>
    /// Unit test to verify that Username property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Username_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var connectionString = new DataConnecionString(DataConnectionStringType.Unknown);
        string expectedUsername = "admin";

        // Act (When)
        connectionString.Username = expectedUsername;

        // Assert (Then)
        Assert.AreEqual(
            expectedUsername,
            connectionString.Username,
            "Username should return the value that was set.");
    }

    #endregion Public Methods
}