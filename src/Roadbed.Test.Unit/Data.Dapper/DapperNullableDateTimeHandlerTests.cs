namespace Roadbed.Test.Unit.Data.Dapper;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using global::Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Contains unit tests for verifying the behavior of the DapperNullableDateTimeHandler class.
/// </summary>
[TestClass]
public class DapperNullableDateTimeHandlerTests
{
    #region Public Properties

    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext? TestContext { get; set; }

    #endregion Public Properties

    #region Public Methods

    #region Parse Tests

    /// <summary>
    /// Unit test to verify that Parse returns null for DBNull value.
    /// </summary>
    [TestMethod]
    public void Parse_DbNullValue_ReturnsNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        object dbNullValue = DBNull.Value;

        // Act (When)
        DateTime? result = handler.Parse(dbNullValue);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Parse should return null for DBNull value.");
    }

    /// <summary>
    /// Unit test to verify that Parse returns null for null value.
    /// </summary>
    [TestMethod]
    public void Parse_NullValue_ReturnsNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        object? nullValue = null;

        // Act (When)
        DateTime? result = handler.Parse(nullValue!);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Parse should return null for null value.");
    }

    /// <summary>
    /// Unit test to verify that Parse converts SQLite TEXT to UTC DateTime.
    /// </summary>
    [TestMethod]
    public void Parse_SqliteTextValue_ReturnsUtcDateTime()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        string textValue = "2024-01-15 14:30:00";

        // Act (When)
        DateTime? result = handler.Parse(textValue);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateTime value.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            result.Value.Kind,
            "Parsed DateTime should have UTC kind.");
        Assert.AreEqual(
            2024,
            result.Value.Year,
            "Year should be parsed correctly.");
        Assert.AreEqual(
            1,
            result.Value.Month,
            "Month should be parsed correctly.");
        Assert.AreEqual(
            15,
            result.Value.Day,
            "Day should be parsed correctly.");
    }

    /// <summary>
    /// Unit test to verify that Parse converts non-UTC DateTime to UTC.
    /// </summary>
    [TestMethod]
    public void Parse_NonUtcDateTime_ConvertsToUtc()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        DateTime localDateTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Local);

        // Act (When)
        DateTime? result = handler.Parse(localDateTime);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateTime value.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            result.Value.Kind,
            "DateTime should be converted to UTC kind.");
    }

    /// <summary>
    /// Unit test to verify that Parse preserves UTC DateTime.
    /// </summary>
    [TestMethod]
    public void Parse_UtcDateTime_PreservesValue()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        DateTime utcDateTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act (When)
        DateTime? result = handler.Parse(utcDateTime);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateTime value.");
        Assert.AreEqual(
            utcDateTime,
            result.Value,
            "UTC DateTime should be preserved without conversion.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            result.Value.Kind,
            "DateTime kind should remain UTC.");
    }

    /// <summary>
    /// Unit test to verify that Parse throws exception for invalid type.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        int invalidValue = 12345;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            handler.Parse(invalidValue);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Parse should throw InvalidOperationException for invalid type.");
    }

    #endregion Parse Tests

    #region SetValue Tests

    /// <summary>
    /// Unit test to verify that SetValue sets DBNull for null DateTime.
    /// </summary>
    [TestMethod]
    public void SetValue_NullDateTime_SetsDbNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        DateTime? nullDateTime = null;
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, nullDateTime);

        // Assert (Then)
        Assert.AreEqual(
            DBNull.Value,
            parameter.Value,
            "Parameter value should be DBNull for null DateTime.");
    }

    /// <summary>
    /// Unit test to verify that SetValue converts UTC DateTime to TEXT format.
    /// </summary>
    [TestMethod]
    public void SetValue_UtcDateTime_SetsTextParameter()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        DateTime? utcDateTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, utcDateTime);

        // Assert (Then)
        Assert.AreEqual(
            "2024-01-15 14:30:00",
            parameter.Value,
            "Parameter value should be formatted as TEXT.");
        Assert.AreEqual(
            DbType.String,
            parameter.DbType,
            "Parameter DbType should be String.");
    }

    /// <summary>
    /// Unit test to verify that SetValue converts non-UTC DateTime to UTC before storing.
    /// </summary>
    [TestMethod]
    public void SetValue_LocalDateTime_ConvertsToUtcBeforeStoring()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeHandler();
        DateTime? localDateTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Local);
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, localDateTime);

        // Assert (Then)
        Assert.IsNotNull(
            parameter.Value,
            "Parameter value should not be null.");
        Assert.AreEqual(
            DbType.String,
            parameter.DbType,
            "Parameter DbType should be String.");
        StringAssert.Matches(
            parameter.Value?.ToString() ?? string.Empty,
            new System.Text.RegularExpressions.Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"),
            "Parameter value should match datetime format pattern.");
    }

    #endregion SetValue Tests

    #region Integration Tests

    /// <summary>
    /// Unit test to verify that handler correctly round-trips DateTime through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripDateTime_PreservesValue()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
        DapperMapping.Configure(typeof(TestNullableDateTimeDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateTime? originalDateTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, updated_at TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, updated_at) VALUES (@Id, @UpdatedAt)",
                new { Id = 1, UpdatedAt = originalDateTime });

            var result = connection.QuerySingleOrDefault<TestNullableDateTimeDto>(
                "SELECT id, updated_at FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNotNull(
                result!.UpdatedAt,
                "UpdatedAt should not be null.");
            Assert.AreEqual(
                originalDateTime.Value,
                result.UpdatedAt.Value,
                "DateTime should round-trip correctly.");
            Assert.AreEqual(
                DateTimeKind.Utc,
                result.UpdatedAt.Value.Kind,
                "DateTime kind should be UTC after round-trip.");
        }
    }

    /// <summary>
    /// Unit test to verify that handler correctly round-trips null DateTime through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripNullDateTime_PreservesNull()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
        DapperMapping.Configure(typeof(TestNullableDateTimeDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateTime? nullDateTime = null;

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, updated_at TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, updated_at) VALUES (@Id, @UpdatedAt)",
                new { Id = 1, UpdatedAt = nullDateTime });

            var result = connection.QuerySingleOrDefault<TestNullableDateTimeDto>(
                "SELECT id, updated_at FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNull(
                result!.UpdatedAt,
                "UpdatedAt should be null after round-trip.");
        }
    }

    #endregion Integration Tests

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a connection factory with a unique in-memory database.
    /// </summary>
    /// <returns>Connection factory for testing.</returns>
    private IDataConnectionFactory CreateConnectionFactory()
    {
        string uniqueDbName = $"TestDb_{Guid.NewGuid():N}";
        var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
        {
            DatabaseSource = uniqueDbName,
        };
        return new SqliteConnectionFactory(connectionString);
    }

    #endregion Private Methods

    #region Test DTOs

    /// <summary>
    /// Test DTO for nullable DateTime integration tests.
    /// </summary>
    private class TestNullableDateTimeDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion Test DTOs
}