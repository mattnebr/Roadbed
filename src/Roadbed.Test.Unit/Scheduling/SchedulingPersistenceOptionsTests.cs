namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="SchedulingPersistenceOptions"/> POCO and the
/// <see cref="SchedulingPersistenceMode"/> enum.
/// </summary>
[TestClass]
public class SchedulingPersistenceOptionsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that SchedulerName defaults to a sensible value.
    /// </summary>
    [TestMethod]
    public void SchedulerName_DefaultConstructor_DefaultsToRoadbedScheduler()
    {
        // Arrange (Given)

        // Act (When)
        var options = new SchedulingPersistenceOptions();

        // Assert (Then)
        Assert.AreEqual(
            "RoadbedScheduler",
            options.SchedulerName,
            "SchedulerName should default to 'RoadbedScheduler'.");
    }

    /// <summary>
    /// Unit test to verify that Mode defaults to InMemory.
    /// </summary>
    [TestMethod]
    public void Mode_DefaultConstructor_DefaultsToInMemory()
    {
        // Arrange (Given)

        // Act (When)
        var options = new SchedulingPersistenceOptions();

        // Assert (Then)
        Assert.AreEqual(
            SchedulingPersistenceMode.InMemory,
            options.Mode,
            "Mode should default to InMemory to preserve out-of-box behavior.");
    }

    /// <summary>
    /// Unit test to verify that TablePrefix defaults to Quartz's bundled DDL prefix.
    /// </summary>
    [TestMethod]
    public void TablePrefix_DefaultConstructor_DefaultsToQrtzUnderscore()
    {
        // Arrange (Given)

        // Act (When)
        var options = new SchedulingPersistenceOptions();

        // Assert (Then)
        Assert.AreEqual(
            "QRTZ_",
            options.TablePrefix,
            "TablePrefix should default to 'QRTZ_' to match Quartz's bundled DDL.");
    }

    /// <summary>
    /// Unit test to verify that IsClustered defaults to false.
    /// </summary>
    [TestMethod]
    public void IsClustered_DefaultConstructor_DefaultsToFalse()
    {
        // Arrange (Given)

        // Act (When)
        var options = new SchedulingPersistenceOptions();

        // Assert (Then)
        Assert.IsFalse(
            options.IsClustered,
            "IsClustered should default to false (single-node).");
    }

    /// <summary>
    /// Unit test to verify that every property can be set through the init accessor.
    /// </summary>
    [TestMethod]
    public void AllProperties_InitializedExplicitly_ReturnsSuppliedValues()
    {
        // Arrange (Given)
        const string expectedName = "Foo-Prod";
        const SchedulingPersistenceMode expectedMode = SchedulingPersistenceMode.Persistent;
        const string expectedPrefix = "QRTZ_V3_18_";
        const bool expectedClustered = true;

        // Act (When)
        var options = new SchedulingPersistenceOptions
        {
            SchedulerName = expectedName,
            Mode = expectedMode,
            TablePrefix = expectedPrefix,
            IsClustered = expectedClustered,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            options.SchedulerName,
            "SchedulerName should reflect the initialized value.");
        Assert.AreEqual(
            expectedMode,
            options.Mode,
            "Mode should reflect the initialized value.");
        Assert.AreEqual(
            expectedPrefix,
            options.TablePrefix,
            "TablePrefix should reflect the initialized value.");
        Assert.AreEqual(
            expectedClustered,
            options.IsClustered,
            "IsClustered should reflect the initialized value.");
    }

    /// <summary>
    /// Unit test to verify the enum exposes exactly InMemory (0) and Persistent (1).
    /// </summary>
    [TestMethod]
    public void SchedulingPersistenceMode_EnumValues_AreInMemoryAndPersistent()
    {
        // Arrange (Given)

        // Act (When)
        var inMemoryValue = (int)SchedulingPersistenceMode.InMemory;
        var persistentValue = (int)SchedulingPersistenceMode.Persistent;

        // Assert (Then)
        Assert.AreEqual(
            0,
            inMemoryValue,
            "InMemory should have ordinal value 0 (default).");
        Assert.AreEqual(
            1,
            persistentValue,
            "Persistent should have ordinal value 1.");
    }

    #endregion Public Methods
}
