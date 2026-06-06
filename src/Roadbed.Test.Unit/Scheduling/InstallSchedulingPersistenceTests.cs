namespace Roadbed.Test.Unit.Scheduling;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using Roadbed.Scheduling;
using Roadbed.Scheduling.Installers;

/// <summary>
/// Contains unit tests for verifying the persistence-dispatch behavior added
/// to <see cref="InstallScheduling"/>: resolution of
/// <see cref="SchedulingPersistenceOptions"/> from DI, the required-factory
/// check when <see cref="SchedulingPersistenceMode.Persistent"/> is selected,
/// and the dispatcher's rejection of unsupported connection-string types.
/// </summary>
/// <remarks>
/// <para>
/// These tests deliberately do NOT exercise the InMemory happy path end-to-end
/// because <see cref="InstallScheduling.ConfigureServices"/> performs assembly-
/// wide job discovery in <c>RegisterSchedulingJobs</c>. In a unit-test process
/// that's loaded test-only job classes from sibling test fixtures, the
/// downstream <c>ConfigureQuartzJobs</c> call would attempt to resolve those
/// jobs and fail for reasons unrelated to the persistence dispatch this fixture
/// is testing.
/// </para>
/// <para>
/// Each throw case below is structured so the exception fires <b>before</b>
/// reaching job discovery, which keeps the tests focused on dispatch behavior.
/// </para>
/// </remarks>
[TestClass]
public class InstallSchedulingPersistenceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that selecting Persistent mode without registering
    /// an ISchedulingDatabaseFactory throws InvalidOperationException with a
    /// message that names the missing service.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_PersistentModeMissingFactory_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(new SchedulingPersistenceOptions
        {
            Mode = SchedulingPersistenceMode.Persistent,
        });

        // ISchedulingDatabaseFactory deliberately NOT registered.
        var installer = new InstallScheduling();
        bool exceptionThrown = false;
        string actualMessage = string.Empty;

        // Act (When)
        try
        {
            installer.ConfigureServices(services, configuration);
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            actualMessage = ex.Message;
        }

        // Assert (Then)
        const string throwMessage =
            "ConfigureServices should throw InvalidOperationException when " +
            "Persistent mode is selected but no ISchedulingDatabaseFactory is registered.";
        const string containsMessage =
            "The exception message should name the missing service so the host " +
            "operator knows what to register.";
        Assert.IsTrue(exceptionThrown, throwMessage);
        StringAssert.Contains(actualMessage, nameof(ISchedulingDatabaseFactory), containsMessage);
    }

    /// <summary>
    /// Unit test to verify that selecting Persistent mode with a SQLite
    /// in-memory connection factory throws InvalidOperationException —
    /// SQLite in-memory cannot back a Quartz persistent store because the
    /// data would not survive the connection lifetime.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_PersistentModeWithSqliteInMemory_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(new SchedulingPersistenceOptions
        {
            Mode = SchedulingPersistenceMode.Persistent,
        });
        services.AddSingleton<ISchedulingDatabaseFactory>(
            new StubSchedulingDatabaseFactory(DataConnectionStringType.SQLiteInMemory));

        var installer = new InstallScheduling();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            installer.ConfigureServices(services, configuration);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        const string message =
            "ConfigureServices should throw InvalidOperationException when the " +
            "Quartz database factory points at SQLite in-memory storage.";
        Assert.IsTrue(exceptionThrown, message);
    }

    /// <summary>
    /// Unit test to verify that selecting Persistent mode with a connection
    /// type Roadbed.Scheduling does not know about throws
    /// InvalidOperationException via the dispatcher's default switch arm.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_PersistentModeWithUnknownConnectionType_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(new SchedulingPersistenceOptions
        {
            Mode = SchedulingPersistenceMode.Persistent,
        });
        services.AddSingleton<ISchedulingDatabaseFactory>(
            new StubSchedulingDatabaseFactory(DataConnectionStringType.Unknown));

        var installer = new InstallScheduling();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            installer.ConfigureServices(services, configuration);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        const string message =
            "ConfigureServices should throw InvalidOperationException when the " +
            "Quartz database factory's ConnectionStringType is not one the " +
            "dispatcher can map to a Quartz fluent configuration method.";
        Assert.IsTrue(exceptionThrown, message);
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Test double for <see cref="ISchedulingDatabaseFactory"/> that returns a
    /// <see cref="DataConnecionString"/> built with a chosen
    /// <see cref="DataConnectionStringType"/>. Used only to drive the
    /// dispatcher's switch arm; no actual connection is opened.
    /// </summary>
    private sealed class StubSchedulingDatabaseFactory : ISchedulingDatabaseFactory
    {
        private readonly DataConnecionString _connection;

        public StubSchedulingDatabaseFactory(DataConnectionStringType type)
        {
            // Supply a literal connection string so DataConnecionString does
            // not try to compose one from null property values.
            this._connection = new DataConnecionString(type, "stub-connection-string");
        }

        public DataConnecionString Connecion => this._connection;

        public IDbConnection CreateOpenConnection()
            => throw new NotSupportedException(
                "Stub factory: opening a real connection is not supported.");

        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException(
                "Stub factory: opening a real connection is not supported.");
    }

    #endregion Private Classes
}
