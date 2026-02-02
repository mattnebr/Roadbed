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
/// Contains unit tests for verifying the behavior of the DapperNullableDateTimeOffsetHandler class.
/// </summary>
[TestClass]
public class DapperNullableDateTimeOffsetHandlerTests
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
        var handler = new DapperNullableDateTimeOffsetHandler();
        object dbNullValue = DBNull.Value;

        // Act (When)
        DateTimeOffset? result = handler.Parse(dbNullValue);

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
        var handler = new DapperNullableDateTimeOffsetHandler();
        object? nullValue = null;

        // Act (When)
        DateTimeOffset? result = handler.Parse(nullValue!);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Parse should return null for null value.");
    }

    /// <summary>
    /// Unit test to verify that Parse converts SQLite TEXT to DateTimeOffset.
    /// </summary>
    [TestMethod]
    public void Parse_SqliteTextValue_ReturnsDateTimeOffset()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
        string textValue = "2024-01-15 14:30:00-06:00";

        // Act (When)
        DateTimeOffset? result = handler.Parse(textValue);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateTimeOffset value.");
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
        Assert.AreEqual(
            TimeSpan.FromHours(-6),
            result.Value.Offset,
            "Timezone offset should be parsed correctly.");
    }

    /// <summary>
    /// Unit test to verify that Parse preserves DateTimeOffset value.
    /// </summary>
    [TestMethod]
    public void Parse_DateTimeOffsetValue_PreservesValue()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
        DateTimeOffset originalValue = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));

        // Act (When)
        DateTimeOffset? result = handler.Parse(originalValue);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Parse should return a DateTimeOffset value.");
        Assert.AreEqual(
            originalValue,
            result.Value,
            "DateTimeOffset should be preserved without conversion.");
    }

    /// <summary>
    /// Unit test to verify that Parse throws exception for invalid type.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
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
    /// Unit test to verify that SetValue sets DBNull for null DateTimeOffset.
    /// </summary>
    [TestMethod]
    public void SetValue_NullDateTimeOffset_SetsDbNull()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
        DateTimeOffset? nullValue = null;
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, nullValue);

        // Assert (Then)
        Assert.AreEqual(
            DBNull.Value,
            parameter.Value,
            "Parameter value should be DBNull for null DateTimeOffset.");
    }

    /// <summary>
    /// Unit test to verify that SetValue converts DateTimeOffset to TEXT format with timezone.
    /// </summary>
    [TestMethod]
    public void SetValue_DateTimeOffset_SetsTextParameterWithTimezone()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
        DateTimeOffset? value = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, value);

        // Assert (Then)
        Assert.AreEqual(
            "2024-01-15 14:30:00-06:00",
            parameter.Value,
            "Parameter value should be formatted as TEXT with timezone offset.");
        Assert.AreEqual(
            DbType.String,
            parameter.DbType,
            "Parameter DbType should be String.");
    }

    /// <summary>
    /// Unit test to verify that SetValue preserves positive timezone offset.
    /// </summary>
    [TestMethod]
    public void SetValue_PositiveTimezoneOffset_PreservesOffset()
    {
        // Arrange (Given)
        var handler = new DapperNullableDateTimeOffsetHandler();
        DateTimeOffset? value = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(5.5));
        var parameter = new SqliteParameter();

        // Act (When)
        handler.SetValue(parameter, value);

        // Assert (Then)
        Assert.AreEqual(
            "2024-01-15 14:30:00+05:30",
            parameter.Value,
            "Parameter value should include positive timezone offset.");
    }

    #endregion SetValue Tests

    #region Integration Tests

    /// <summary>
    /// Unit test to verify that handler correctly round-trips DateTimeOffset through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripDateTimeOffset_PreservesValue()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
        DapperMapping.Configure(typeof(TestNullableDateTimeOffsetDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateTimeOffset? originalValue = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, scheduled_time TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, scheduled_time) VALUES (@Id, @ScheduledTime)",
                new { Id = 1, ScheduledTime = originalValue });

            var result = connection.QuerySingleOrDefault<TestNullableDateTimeOffsetDto>(
                "SELECT id, scheduled_time FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNotNull(
                result!.ScheduledTime,
                "ScheduledTime should not be null.");
            Assert.AreEqual(
                originalValue.Value,
                result.ScheduledTime.Value,
                "DateTimeOffset should round-trip correctly.");
            Assert.AreEqual(
                TimeSpan.FromHours(-6),
                result.ScheduledTime.Value.Offset,
                "Timezone offset should be preserved after round-trip.");
        }
    }

    /// <summary>
    /// Unit test to verify that handler correctly round-trips null DateTimeOffset through SQLite.
    /// </summary>
    [TestMethod]
    public void Integration_RoundTripNullDateTimeOffset_PreservesNull()
    {
        // Arrange (Given)
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
        DapperMapping.Configure(typeof(TestNullableDateTimeOffsetDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateTimeOffset? nullValue = null;

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, scheduled_time TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, scheduled_time) VALUES (@Id, @ScheduledTime)",
                new { Id = 1, ScheduledTime = nullValue });

            var result = connection.QuerySingleOrDefault<TestNullableDateTimeOffsetDto>(
                "SELECT id, scheduled_time FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.IsNull(
                result!.ScheduledTime,
                "ScheduledTime should be null after round-trip.");
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
        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory)
        {
            DatabaseSource = uniqueDbName,
        };
        return new SqliteConnectionFactory(connectionString);
    }

    #endregion Private Methods

    #region Test DTOs

    /// <summary>
    /// Test DTO for nullable DateTimeOffset integration tests.
    /// </summary>
    private class TestNullableDateTimeOffsetDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("scheduled_time")]
        public DateTimeOffset? ScheduledTime { get; set; }
    }

    #endregion Test DTOs
}