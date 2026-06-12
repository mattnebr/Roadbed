namespace Roadbed.Test.Unit.Logging;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Data;
using Roadbed.Logging;

/// <summary>
/// Unit tests for <see cref="LoggingLogEntryRepository"/>.
/// </summary>
[TestClass]
public class LoggingLogEntryRepositoryTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the SQL builder honors the configured placeholder ceiling
    /// when computing the maximum rows per chunk.
    /// </summary>
    [TestMethod]
    public void MaxRowsPerChunk_DerivedFromPlaceholderCeiling()
    {
        // Arrange (Given) - 320 placeholders / 16 columns = 20 rows per chunk.
        var options = new LoggingOptions
        {
            Schema = "ops",
            MaxPlaceholdersPerStatement = 320,
        };
        var factory = BuildFactory(DataConnectionStringType.MySQL);

        // Act (When)
        var repository = new LoggingLogEntryRepository(
            Mock.Of<ILoggingDataExecutor>(),
            factory,
            options,
            NullLogger<LoggingLogEntryRepository>.Instance);

        // Assert (Then)
        Assert.AreEqual(20, repository.MaxRowsPerChunk);
    }

    /// <summary>
    /// Verifies that a single-row chunk produces well-formed SQL with the
    /// expected placeholder names.
    /// </summary>
    [TestMethod]
    public void BuildInsertSql_SingleRow_RendersExpectedSql()
    {
        // Arrange (Given)
        var options = new LoggingOptions { Schema = "ops" };
        var factory = BuildFactory(DataConnectionStringType.MySQL);
        var repository = new LoggingLogEntryRepository(
            Mock.Of<ILoggingDataExecutor>(),
            factory,
            options,
            NullLogger<LoggingLogEntryRepository>.Instance);

        // Act (When)
        string sql = repository.BuildInsertSql(1);

        // Assert (Then)
        StringAssert.Contains(sql, "INSERT INTO ops.log_entries", "Schema-qualified table reference expected.");
        StringAssert.Contains(sql, "@p0_event_time_utc");
        StringAssert.Contains(sql, "@p0_log_level");
        StringAssert.Contains(sql, "@p0_activity_id");
        StringAssert.Contains(sql, "@p0_process_id");
        Assert.IsTrue(sql.TrimEnd().EndsWith(';'), "SQL should be terminated with a semicolon.");
    }

    /// <summary>
    /// Verifies that the SQL builder emits exactly N rows of placeholders for an N-row chunk.
    /// </summary>
    [TestMethod]
    public void BuildInsertSql_MultiRowChunk_EmitsOneTupleSequencePerRow()
    {
        // Arrange (Given)
        var options = new LoggingOptions { Schema = string.Empty };
        var factory = BuildFactory(DataConnectionStringType.SQLite);
        var repository = new LoggingLogEntryRepository(
            Mock.Of<ILoggingDataExecutor>(),
            factory,
            options,
            NullLogger<LoggingLogEntryRepository>.Instance);

        // Act (When)
        string sql = repository.BuildInsertSql(3);

        // Assert (Then)
        StringAssert.Contains(sql, "INSERT INTO log_entries", "Unqualified reference when schema is empty.");
        StringAssert.Contains(sql, "@p0_event_time_utc");
        StringAssert.Contains(sql, "@p1_event_time_utc");
        StringAssert.Contains(sql, "@p2_event_time_utc");

        // The chunk size should produce 3 closing parens for VALUES tuples
        // (additionally the column list has its own outer parens, so we
        // expect at least 4 closing parens — the column list and 3 tuples).
        int closeParenCount = sql.Count(c => c == ')');
        Assert.IsTrue(closeParenCount >= 4, $"Expected at least 4 close parens, found {closeParenCount}.");
    }

    /// <summary>
    /// Verifies that <see cref="LoggingLogEntryRepository.BuildParameters"/>
    /// publishes one Dapper parameter per row per column.
    /// </summary>
    [TestMethod]
    public void BuildParameters_TwoRowChunk_ExposesAllPerRowParameterNames()
    {
        // Arrange (Given)
        var entries = new List<LoggingLogEntry>
        {
            new ()
            {
                EventTimeUtc = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc),
                LogLevel = 2,
                Category = "Foo.Bar",
                Message = "first",
                Application = "test",
                ActivityId = "01ACTIVITYIDONEAAAAAAAAAAAA",
            },
            new ()
            {
                EventTimeUtc = new DateTime(2026, 6, 6, 12, 0, 1, DateTimeKind.Utc),
                LogLevel = 4,
                Category = "Foo.Bar",
                Message = "second",
                Application = "test",
                ActivityId = "01ACTIVITYIDTWOBBBBBBBBBBBB",
            },
        };

        // Act (When)
        DynamicParameters parameters = LoggingLogEntryRepository.BuildParameters(entries, 0, 2);
        var names = parameters.ParameterNames.ToList();

        // Assert (Then)
        Assert.IsTrue(names.Contains("p0_event_time_utc"));
        Assert.IsTrue(names.Contains("p0_activity_id"));
        Assert.IsTrue(names.Contains("p1_event_time_utc"));
        Assert.IsTrue(names.Contains("p1_activity_id"));
        Assert.IsTrue(
            names.Count >= 32,
            $"Expected at least 32 placeholders (16 columns x 2 rows), found {names.Count}.");
    }

    /// <summary>
    /// Verifies that BulkInsertAsync on an empty list returns 0 without touching the database.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task BulkInsertAsync_EmptyList_ReturnsZero()
    {
        // Arrange (Given)
        var options = new LoggingOptions();
        var factory = BuildFactory(DataConnectionStringType.MySQL);
        var repository = new LoggingLogEntryRepository(
            Mock.Of<ILoggingDataExecutor>(),
            factory,
            options,
            NullLogger<LoggingLogEntryRepository>.Instance);

        // Act (When)
        int inserted = await repository.BulkInsertAsync(Array.Empty<LoggingLogEntry>());

        // Assert (Then)
        Assert.AreEqual(0, inserted);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Builds a mock <see cref="ILoggingDatabaseFactory"/> whose connection
    /// reports the supplied type. Repository tests that only exercise the
    /// SQL-building seams never open a real connection.
    /// </summary>
    /// <param name="type">The connection-string type to advertise.</param>
    /// <returns>A configured <see cref="ILoggingDatabaseFactory"/> mock.</returns>
    private static ILoggingDatabaseFactory BuildFactory(DataConnectionStringType type)
    {
        var connection = new DataConnecionString(type, "Server=stub");
        var mock = new Mock<ILoggingDatabaseFactory>();
        mock.SetupGet(f => f.Connecion).Returns(connection);
        mock.Setup(f => f.CreateOpenConnection()).Returns(Mock.Of<IDbConnection>());
        return mock.Object;
    }

    #endregion Private Methods
}
