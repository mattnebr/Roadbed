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
/// Contains unit tests for verifying the behavior of the DapperDateTimeOffsetHandler class.
/// </summary>
[TestClass]
public class DapperDateTimeOffsetHandlerTests
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
    /// Unit test to verify that Parse converts SQLite TEXT to DateTimeOffset.
    /// </summary>
    [TestMethod]
    public void Parse_SqliteTextValue_ReturnsDateTimeOffset()
    {
        // Arrange (Given)
        var handler = new DapperDateTimeOffsetHandler();
        string textValue = "2024-01-15 14:30:00-06:00";

        // Act (When)
        DateTimeOffset result = handler.Parse(textValue);

        // Assert (Then)
        Assert.AreEqual(
            2024,
            result.Year,
            "Year should be parsed correctly.");
        Assert.AreEqual(
            1,
            result.Month,
            "Month should be parsed correctly.");
        Assert.AreEqual(
            15,
            result.Day,
            "Day should be parsed correctly.");
        Assert.AreEqual(
            14,
            result.Hour,
            "Hour should be parsed correctly.");
        Assert.AreEqual(
            30,
            result.Minute,
            "Minute should be parsed correctly.");
        Assert.AreEqual(
            TimeSpan.FromHours(-6),
            result.Offset,
            "Timezone offset should be parsed correctly.");
    }

    /// <summary>
    /// Unit test to verify that Parse preserves DateTimeOffset value.
    /// </summary>
    [TestMethod]
    public void Parse_DateTimeOffsetValue_PreservesValue()
    {
        // Arrange (Given)
        var handler = new DapperDateTimeOffsetHandler();
        DateTimeOffset originalValue = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));

        // Act (When)
        DateTimeOffset result = handler.Parse(originalValue);

        // Assert (Then)
        Assert.AreEqual(
            originalValue,
            result,
            "DateTimeOffset should be preserved without conversion.");
    }

    /// <summary>
    /// Unit test to verify that Parse throws exception for invalid type.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var handler = new DapperDateTimeOffsetHandler();
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
    /// Unit test to verify that SetValue converts DateTimeOffset to TEXT format with timezone.
    /// </summary>
    [TestMethod]
    public void SetValue_DateTimeOffset_SetsTextParameterWithTimezone()
    {
        // Arrange (Given)
        var handler = new DapperDateTimeOffsetHandler();
        DateTimeOffset value = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));
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
        var handler = new DapperDateTimeOffsetHandler();
        DateTimeOffset value = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(5.5));
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
        SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
        DapperMapping.Configure(typeof(TestDateTimeOffsetDto));
        var connectionFactory = this.CreateConnectionFactory();
        DateTimeOffset originalValue = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-6));

        using var connection = (SqliteConnection)connectionFactory.CreateOpenConnectionAsync(this.TestContext!.CancellationToken).Result;
        using (var keepAlive = connection.KeepAlive())
        {
            connection.Execute("CREATE TABLE test_table (id INTEGER PRIMARY KEY, event_time TEXT)");

            // Act (When)
            connection.Execute(
                "INSERT INTO test_table (id, event_time) VALUES (@Id, @EventTime)",
                new { Id = 1, EventTime = originalValue });

            var result = connection.QuerySingleOrDefault<TestDateTimeOffsetDto>(
                "SELECT id, event_time FROM test_table WHERE id = 1");

            // Assert (Then)
            Assert.IsNotNull(
                result,
                "Query should return a result.");
            Assert.AreEqual(
                originalValue,
                result!.EventTime,
                "DateTimeOffset should round-trip correctly.");
            Assert.AreEqual(
                TimeSpan.FromHours(-6),
                result.EventTime.Offset,
                "Timezone offset should be preserved after round-trip.");
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
    /// Test DTO for DateTimeOffset integration tests.
    /// </summary>
    private class TestDateTimeOffsetDto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("event_time")]
        public DateTimeOffset EventTime { get; set; }
    }

    #endregion Test DTOs
}