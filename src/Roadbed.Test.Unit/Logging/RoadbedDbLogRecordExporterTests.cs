namespace Roadbed.Test.Unit.Logging;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Logging;

/// <summary>
/// Unit tests for the parts of <see cref="RoadbedDbLogRecordExporter"/>
/// that can be exercised without constructing an OpenTelemetry
/// <c>LogRecord</c> (which has no public constructor in v1.15.x).
/// </summary>
/// <remarks>
/// End-to-end mapping coverage that exercises a real <c>LogRecord</c> lives
/// in <c>Roadbed.Test.Integration</c>; here we cover the deterministic
/// predicates that the exporter uses to decide whether to enqueue a record.
/// </remarks>
[TestClass]
public class RoadbedDbLogRecordExporterTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that categories under any configured recursion-guard prefix are dropped.
    /// </summary>
    [TestMethod]
    public void IsRecursionGuarded_CategoryMatchesGuardPrefix_ReturnsTrue()
    {
        // Arrange (Given)
        var options = new LoggingOptions();
        var exporter = new RoadbedDbLogRecordExporter(
            new LoggingChannel(options),
            options);

        // Act (When) + Assert (Then)
        Assert.IsTrue(exporter.IsRecursionGuarded("Roadbed.Logging.Hosted.LogWriterHostedService"));
        Assert.IsTrue(exporter.IsRecursionGuarded("Roadbed.Data.MySql.MySqlExecutor"));
        Assert.IsTrue(exporter.IsRecursionGuarded("MySqlConnector.MySqlBulkCopy"));
    }

    /// <summary>
    /// Verifies that categories outside every guard prefix flow through.
    /// </summary>
    [TestMethod]
    public void IsRecursionGuarded_UnrelatedCategory_ReturnsFalse()
    {
        // Arrange (Given)
        var options = new LoggingOptions();
        var exporter = new RoadbedDbLogRecordExporter(
            new LoggingChannel(options),
            options);

        // Act (When) + Assert (Then)
        Assert.IsFalse(exporter.IsRecursionGuarded("Pebble.Bronze.PlacesLoader"));
        Assert.IsFalse(exporter.IsRecursionGuarded("Microsoft.AspNetCore.Hosting"));
    }

    /// <summary>
    /// Verifies that null or empty categories do not trip the guard.
    /// </summary>
    [TestMethod]
    public void IsRecursionGuarded_NullOrEmptyCategory_ReturnsFalse()
    {
        // Arrange (Given)
        var options = new LoggingOptions();
        var exporter = new RoadbedDbLogRecordExporter(
            new LoggingChannel(options),
            options);

        // Act (When) + Assert (Then)
        Assert.IsFalse(exporter.IsRecursionGuarded(null));
        Assert.IsFalse(exporter.IsRecursionGuarded(string.Empty));
    }

    /// <summary>
    /// Verifies that a custom guard prefix the host appended is honored.
    /// </summary>
    [TestMethod]
    public void IsRecursionGuarded_HostAddedGuardPrefix_IsHonored()
    {
        // Arrange (Given)
        var options = new LoggingOptions();
        options.RecursionGuardCategories.Add("Pebble.Noisy");
        var exporter = new RoadbedDbLogRecordExporter(
            new LoggingChannel(options),
            options);

        // Act (When) + Assert (Then)
        Assert.IsTrue(exporter.IsRecursionGuarded("Pebble.Noisy.Subsystem"));
        Assert.IsFalse(exporter.IsRecursionGuarded("Pebble.Quiet.Subsystem"));
    }

    #endregion Public Methods
}
