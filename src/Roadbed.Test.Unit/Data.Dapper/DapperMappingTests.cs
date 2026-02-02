namespace Roadbed.Test.Unit.Data.Dapper;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using global::Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the DapperMapping class.
/// </summary>
[TestClass]
public class DapperMappingTests
{
    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #region Public Methods

    #region Configure Tests

    /// <summary>
    /// Unit test to verify that Configure registers a type successfully.
    /// </summary>
    [TestMethod]
    public void Configure_SingleType_RegistersSuccessfully()
    {
        // Arrange (Given)
        Type testType = typeof(TestDtoWithColumns);

        // Act (When)
        DapperMapping.Configure(testType);

        // Assert (Then)
        // Verify by attempting to use the configured type with Dapper
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, first_name TEXT)");
            connection.Execute("INSERT INTO test_table (id, first_name) VALUES (1, 'Test')");

            var result = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                "SELECT id, first_name FROM test_table WHERE id = 1");

            Assert.IsNotNull(
                result,
                "Configure should successfully register type for Dapper mapping.");
        }
    }

    /// <summary>
    /// Unit test to verify that Configure can register multiple types.
    /// </summary>
    [TestMethod]
    public void Configure_MultipleTypes_RegistersAllSuccessfully()
    {
        // Arrange (Given)
        Type[] testTypes = new[] { typeof(TestDtoWithColumns), typeof(AnotherTestDto) };

        // Act (When)
        DapperMapping.Configure(testTypes);

        // Assert (Then)
        // Verify both types work with Dapper
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            // Test first type
            connection.Execute("CREATE TABLE test_table1 (id INTEGER PRIMARY KEY, first_name TEXT)");
            connection.Execute("INSERT INTO test_table1 (id, first_name) VALUES (1, 'Test1')");
            var result1 = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                "SELECT id, first_name FROM test_table1 WHERE id = 1");

            // Test second type
            connection.Execute("CREATE TABLE test_table2 (id INTEGER PRIMARY KEY, description TEXT)");
            connection.Execute("INSERT INTO test_table2 (id, description) VALUES (1, 'Test2')");
            var result2 = connection.QuerySingleOrDefault<AnotherTestDto>(
                "SELECT id, description FROM test_table2 WHERE id = 1");

            Assert.IsNotNull(
                result1,
                "First type should be registered successfully.");
            Assert.IsNotNull(
                result2,
                "Second type should be registered successfully.");
        }
    }

    /// <summary>
    /// Unit test to verify that Configure can be called multiple times with same type.
    /// </summary>
    [TestMethod]
    public void Configure_SameTypeTwice_DoesNotThrowException()
    {
        // Arrange (Given)
        Type testType = typeof(TestDtoWithColumns);

        // Act (When)
        DapperMapping.Configure(testType);
        DapperMapping.Configure(testType);

        // Assert (Then)
        // Verify type still works correctly after duplicate configuration
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, first_name TEXT)");
            connection.Execute("INSERT INTO test_table (id, first_name) VALUES (1, 'Test')");

            var result = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                "SELECT id, first_name FROM test_table WHERE id = 1");

            Assert.IsNotNull(
                result,
                "Duplicate configuration should not break Dapper mapping.");
        }
    }

    /// <summary>
    /// Unit test to verify that Configure accepts IEnumerable of types.
    /// </summary>
    [TestMethod]
    public void Configure_IEnumerableOfTypes_RegistersSuccessfully()
    {
        // Arrange (Given)
        var types = new[] { typeof(TestDtoWithColumns), typeof(AnotherTestDto) };

        // Act (When)
        DapperMapping.Configure(types);

        // Assert (Then)
        // Verify first type works with Dapper
        var connectionFactory = this.CreateConnectionFactory();
        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, first_name TEXT)");
            connection.Execute("INSERT INTO test_table (id, first_name) VALUES (1, 'Test')");

            var result = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                "SELECT id, first_name FROM test_table WHERE id = 1");

            Assert.IsNotNull(
                result,
                "IEnumerable overload should register types successfully.");
        }
    }

    #endregion Configure Tests

    #region Integration Tests with Dapper

    /// <summary>
    /// Unit test to verify that Dapper correctly maps snake_case columns to PascalCase properties using Column attributes.
    /// </summary>
    [TestMethod]
    public void DapperMapping_SnakeCaseColumns_MapsToPascalCaseProperties()
    {
        // Arrange (Given)
        DapperMapping.Configure(typeof(TestDtoWithColumns));
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table with snake_case columns
            connection.Execute(@"
                CREATE TABLE test_table (
                    id INTEGER PRIMARY KEY,
                    first_name TEXT,
                    last_name TEXT,
                    email_address TEXT
                )");

            // Insert test data
            connection.Execute(@"
                INSERT INTO test_table (id, first_name, last_name, email_address)
                VALUES (1, 'John', 'Doe', 'john.doe@example.com')");

            // Act (When)
            var result = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                "SELECT id, first_name, last_name, email_address FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                1,
                result.Id,
                "Id should be mapped correctly.");
            Assert.AreEqual(
                "John",
                result.FirstName,
                "FirstName should be mapped from first_name column.");
            Assert.AreEqual(
                "Doe",
                result.LastName,
                "LastName should be mapped from last_name column.");
            Assert.AreEqual(
                "john.doe@example.com",
                result.EmailAddress,
                "EmailAddress should be mapped from email_address column.");
        }
    }

    /// <summary>
    /// Unit test to verify that Dapper falls back to property name matching when Column attribute is missing.
    /// </summary>
    [TestMethod]
    public void DapperMapping_MissingColumnAttribute_FallsBackToPropertyNameMatching()
    {
        // Arrange (Given)
        DapperMapping.Configure(typeof(TestDtoWithMixedAttributes));
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            connection.Execute(@"
                CREATE TABLE test_table (
                    id INTEGER PRIMARY KEY,
                    Name TEXT
                )");

            // Insert test data
            connection.Execute(@"
                INSERT INTO test_table (id, Name)
                VALUES (1, 'Test Name')");

            // Act (When)
            var result = connection.QuerySingleOrDefault<TestDtoWithMixedAttributes>(
                "SELECT id, Name FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                1,
                result.Id,
                "Id should be mapped with Column attribute.");
            Assert.AreEqual(
                "Test Name",
                result.Name,
                "Name should be mapped by property name fallback.");
        }
    }

    /// <summary>
    /// Unit test to verify that Dapper throws exception when column cannot be mapped.
    /// </summary>
    [TestMethod]
    public void DapperMapping_UnmappableColumn_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        DapperMapping.Configure(typeof(TestDtoWithColumns));
        var connectionFactory = this.CreateConnectionFactory();
        InvalidOperationException? caughtException = null;

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table
            connection.Execute(@"
                CREATE TABLE test_table (
                    id INTEGER PRIMARY KEY,
                    nonexistent_column TEXT
                )");

            // Insert test data
            connection.Execute(@"
                INSERT INTO test_table (id, nonexistent_column)
                VALUES (1, 'Test')");

            // Act (When)
            try
            {
                var result = connection.QuerySingleOrDefault<TestDtoWithColumns>(
                    "SELECT id, nonexistent_column FROM test_table WHERE id = 1");
            }
            catch (InvalidOperationException ex)
            {
                caughtException = ex;
            }
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Query should throw InvalidOperationException for unmappable column.");
        StringAssert.Contains(
            caughtException.Message,
            "nonexistent_column",
            "Exception message should mention the problematic column name.");
    }

    /// <summary>
    /// Unit test to verify that Dapper mapping is case-insensitive for property name fallback.
    /// </summary>
    [TestMethod]
    public void DapperMapping_CaseInsensitiveFallback_MapsCorrectly()
    {
        // Arrange (Given)
        DapperMapping.Configure(typeof(TestDtoWithMixedAttributes));
        var connectionFactory = this.CreateConnectionFactory();

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            // Create table with lowercase column name
            connection.Execute(@"
                CREATE TABLE test_table (
                    id INTEGER PRIMARY KEY,
                    name TEXT
                )");

            // Insert test data
            connection.Execute(@"
                INSERT INTO test_table (id, name)
                VALUES (1, 'Test Name')");

            // Act (When)
            var result = connection.QuerySingleOrDefault<TestDtoWithMixedAttributes>(
                "SELECT id, name FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                "Test Name",
                result.Name,
                "Name should be mapped case-insensitively (name -> Name).");
        }
    }

    #endregion Integration Tests with Dapper

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

    #region Test DTOs

    /// <summary>
    /// Test DTO with Column attributes for all properties.
    /// </summary>
    private class TestDtoWithColumns
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("email_address")]
        public string? EmailAddress { get; set; }
    }

    /// <summary>
    /// Another test DTO for testing multiple type registration.
    /// </summary>
    private class AnotherTestDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Test DTO with mixed attributes (some with Column, some without).
    /// </summary>
    private class TestDtoWithMixedAttributes
    {
        [Column("id")]
        public int Id { get; set; }

        // No Column attribute - should fall back to property name matching
        public string? Name { get; set; }
    }

    #endregion Test DTOs
}