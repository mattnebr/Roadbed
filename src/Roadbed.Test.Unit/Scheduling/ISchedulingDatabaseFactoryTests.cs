namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Scheduling;

/// <summary>
/// Contains unit tests for verifying the contract of the
/// <see cref="ISchedulingDatabaseFactory"/> marker interface.
/// </summary>
[TestClass]
public class ISchedulingDatabaseFactoryTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that ISchedulingDatabaseFactory inherits from
    /// IDataConnectionFactory. The marker exists solely so DI can distinguish
    /// the Quartz database connection from the application's other
    /// IDataConnectionFactory registrations.
    /// </summary>
    [TestMethod]
    public void ISchedulingDatabaseFactory_TypeCheck_ExtendsIDataConnectionFactory()
    {
        // Arrange (Given)

        // Act (When)
        bool isAssignable = typeof(IDataConnectionFactory)
            .IsAssignableFrom(typeof(ISchedulingDatabaseFactory));

        // Assert (Then)
        const string message =
            "ISchedulingDatabaseFactory should extend IDataConnectionFactory " +
            "so a concrete factory can flow through Roadbed.Data executors.";
        Assert.IsTrue(isAssignable, message);
    }

    /// <summary>
    /// Unit test to verify that ISchedulingDatabaseFactory is an interface.
    /// </summary>
    [TestMethod]
    public void ISchedulingDatabaseFactory_TypeCheck_IsInterface()
    {
        // Arrange (Given)

        // Act (When)
        bool isInterface = typeof(ISchedulingDatabaseFactory).IsInterface;

        // Assert (Then)
        Assert.IsTrue(
            isInterface,
            "ISchedulingDatabaseFactory should be declared as an interface.");
    }

    /// <summary>
    /// Unit test to verify that ISchedulingDatabaseFactory declares no
    /// members of its own — it is purely a DI marker.
    /// </summary>
    [TestMethod]
    public void ISchedulingDatabaseFactory_DeclaredMembers_AreEmpty()
    {
        // Arrange (Given)

        // Act (When)
        int declaredMemberCount = typeof(ISchedulingDatabaseFactory)
            .GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly)
            .Length;

        // Assert (Then)
        const string message =
            "ISchedulingDatabaseFactory should be a pure marker — no members " +
            "of its own; all surface comes through the inherited " +
            "IDataConnectionFactory contract.";
        Assert.AreEqual(0, declaredMemberCount, message);
    }

    #endregion Public Methods
}
