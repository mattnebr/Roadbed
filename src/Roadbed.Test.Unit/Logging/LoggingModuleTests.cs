namespace Roadbed.Test.Unit.Logging;

using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Data;
using Roadbed.Logging;
using Roadbed.Logging.Installers;
using Roadbed.Logging.Sqlite;

/// <summary>
/// Unit tests for <see cref="LoggingModule"/> (the provider-neutral wiring) and
/// the provider-satellite installer composition, focused on the
/// executor-presence contract and the channel-sharing guarantee that makes the
/// host writer and any <c>ServiceLocator</c>-resolved producer meet around one
/// <see cref="LoggingChannel"/>.
/// </summary>
[TestClass]
public class LoggingModuleTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that <see cref="LoggingModule.Register"/> throws when no
    /// provider satellite has registered an <see cref="ILoggingDataExecutor"/>
    /// — the loud failure that points a consumer at the missing provider
    /// package.
    /// </summary>
    [TestMethod]
    public void Register_NoExecutorRegistered_Throws()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(DataConnectionStringType.MySQL));

        // Act (When) + Assert (Then)
        Assert.ThrowsExactly<System.InvalidOperationException>(() => LoggingModule.Register(services));
    }

    /// <summary>
    /// Verifies the SQLite satellite installer registers an executor and then
    /// wires the full pipeline so the activity service resolves end-to-end.
    /// </summary>
    [TestMethod]
    public void InstallLoggingSqlite_WiresExecutorAndPipeline()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(DataConnectionStringType.SQLite));

        // Act (When)
        new InstallLoggingSqlite().ConfigureServices(services, new ConfigurationBuilder().Build());

        // Assert (Then) — resolve a repository (pure constructor injection) to
        // prove the satellite-supplied executor flows into the data path,
        // without touching the global ServiceLocator the public
        // LoggingActivityService constructor uses (that static is shared across
        // the parallel test run).
        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<ILoggingDataExecutor>(), "Satellite must register the execution port.");
        Assert.IsNotNull(provider.GetService<ILoggingActivityRepository>(), "Repositories must resolve with the satellite-supplied executor injected.");
        Assert.IsNotNull(provider.GetRequiredService<LoggingChannel>());
    }

    /// <summary>
    /// Verifies that two distinct <see cref="System.IServiceProvider"/>
    /// instances built from the same <see cref="IServiceCollection"/>
    /// resolve the SAME <see cref="LoggingChannel"/> object — the
    /// guarantee that lets host-resolved and
    /// <c>ServiceLocator</c>-resolved producers enqueue into one queue.
    /// </summary>
    [TestMethod]
    public void Register_LoggingChannel_IsSharedAcrossContainers()
    {
        // Arrange (Given)
        var services = BuildWiredServices(DataConnectionStringType.MySQL);

        // Act (When)
        LoggingModule.Register(services);

        using var providerA = services.BuildServiceProvider();
        using var providerB = services.BuildServiceProvider();

        var channelA = providerA.GetRequiredService<LoggingChannel>();
        var channelB = providerB.GetRequiredService<LoggingChannel>();

        // Assert (Then)
        Assert.AreSame(
            channelA,
            channelB,
            "LoggingChannel must be a singleton instance shared across every IServiceProvider built from the same IServiceCollection.");
    }

    /// <summary>
    /// Verifies that calling <see cref="LoggingModule.Register"/>
    /// more than once does not register multiple
    /// <see cref="LoggingChannel"/> instances. Auto-discovery in a
    /// multi-host or test scenario can fire the installer twice; the
    /// channel descriptor must remain a single shared instance.
    /// </summary>
    [TestMethod]
    public void Register_RunTwice_LoggingChannelStaysShared()
    {
        // Arrange (Given)
        var services = BuildWiredServices(DataConnectionStringType.SQLite);

        // Act (When) - run the wiring twice over the same services
        LoggingModule.Register(services);
        LoggingModule.Register(services);

        using var provider = services.BuildServiceProvider();
        var channel = provider.GetRequiredService<LoggingChannel>();

        // Assert (Then) - exactly one descriptor; resolving it produces a single instance.
        int channelDescriptorCount = services.Count(d => d.ServiceType == typeof(LoggingChannel));

        Assert.AreEqual(
            1,
            channelDescriptorCount,
            "Double-running the wiring must not register a second LoggingChannel descriptor.");
        Assert.IsNotNull(channel);
    }

    /// <summary>
    /// Verifies that <see cref="LoggingChannel"/> resolved before any
    /// <c>ServiceLocator</c> snapshot is taken still matches the instance
    /// resolved after — proving the descriptor pins one object regardless
    /// of when each container is built.
    /// </summary>
    [TestMethod]
    public void Register_ChannelResolvedBeforeAndAfter_AreSameInstance()
    {
        // Arrange (Given)
        var services = BuildWiredServices(DataConnectionStringType.MySQL);
        LoggingModule.Register(services);

        // Act (When) - resolve before, then build a new provider, then resolve again.
        using var earlyProvider = services.BuildServiceProvider();
        var earlyChannel = earlyProvider.GetRequiredService<LoggingChannel>();

        using var lateProvider = services.BuildServiceProvider();
        var lateChannel = lateProvider.GetRequiredService<LoggingChannel>();

        // Assert (Then)
        Assert.AreSame(earlyChannel, lateChannel);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Builds a service collection with the host registrations plus a stub
    /// executor, ready for <see cref="LoggingModule.Register"/>.
    /// </summary>
    /// <param name="type">The connection-string type the factory advertises.</param>
    /// <returns>A configured <see cref="IServiceCollection"/>.</returns>
    private static IServiceCollection BuildWiredServices(DataConnectionStringType type)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(type));
        services.AddSingleton(Mock.Of<ILoggingDataExecutor>());
        return services;
    }

    /// <summary>
    /// Builds a mock <see cref="ILoggingDatabaseFactory"/> whose connection
    /// reports the supplied type. Tests that exercise installer wiring
    /// only need the factory to advertise a supported provider; no live
    /// connection is ever opened.
    /// </summary>
    /// <param name="type">The connection-string type to advertise.</param>
    /// <returns>A configured <see cref="ILoggingDatabaseFactory"/> mock.</returns>
    private static ILoggingDatabaseFactory BuildFactory(DataConnectionStringType type)
    {
        var connection = new DataConnecionString(type, "Server=stub");
        var mock = new Mock<ILoggingDatabaseFactory>();
        mock.SetupGet(f => f.Connecion).Returns(connection);
        mock.Setup(f => f.CreateOpenConnection()).Returns(Mock.Of<IDbConnection>());
        mock.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<IDbConnection>()));
        return mock.Object;
    }

    #endregion Private Methods
}
