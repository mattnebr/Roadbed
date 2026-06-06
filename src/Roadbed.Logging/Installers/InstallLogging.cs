namespace Roadbed.Logging.Installers;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Roadbed.Data;

/// <summary>
/// Auto-discovered service collection installer that wires the
/// Roadbed.Logging activity service, repositories, channel, and background
/// writer into the host's DI container.
/// </summary>
/// <remarks>
/// <para>
/// The OpenTelemetry pipeline portion of the registration lives on the
/// companion <c>builder.Logging.AddRoadbedDbLogging()</c> extension method
/// because MEL needs <see cref="Microsoft.Extensions.Logging.ILoggingBuilder"/>,
/// which an <see cref="IServiceCollectionInstaller"/> does not see.
/// </para>
/// <para>
/// The installer resolves <see cref="LoggingOptions"/> and
/// <see cref="ILoggingDatabaseFactory"/> from the host registrations before
/// the rest of the pipeline runs. Both must be registered by the host
/// before <c>InstallModulesInAppDomain</c> executes; otherwise the
/// installer throws.
/// </para>
/// </remarks>
public class InstallLogging : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Resolve host registrations up-front so we can validate them and
        // wire concrete repository implementations into DI before any
        // hosted-service start signal fires.
        ILoggingDatabaseFactory factory;

        using (var setupProvider = services.BuildServiceProvider())
        {
            _ = setupProvider.GetService<LoggingOptions>()
                ?? throw new InvalidOperationException(
                    $"Roadbed.Logging requires a singleton {nameof(LoggingOptions)} to be " +
                    $"registered before InstallModulesInAppDomain runs.");

            factory = setupProvider.GetService<ILoggingDatabaseFactory>()
                ?? throw new InvalidOperationException(
                    $"Roadbed.Logging requires a singleton {nameof(ILoggingDatabaseFactory)} " +
                    $"to be registered before InstallModulesInAppDomain runs.");
        }

        ValidateProvider(factory);

        // Register the Dapper [Column] mapping for the three entities so
        // any future read paths (and the integration test suite) materialize
        // rows correctly.
        DapperMapping.Configure(
            typeof(LoggingActivity),
            typeof(LoggingActivityInput),
            typeof(LoggingLogEntry));

        // Repositories — internal sealed; one instance per process.
        services.TryAddSingleton<ILoggingActivityRepository, LoggingActivityRepository>();
        services.TryAddSingleton<ILoggingActivityInputRepository, LoggingActivityInputRepository>();
        services.TryAddSingleton<ILoggingLogEntryRepository, LoggingLogEntryRepository>();

        // Activity service — public sealed; the host injects this directly.
        services.TryAddSingleton<ILoggingActivityService, LoggingActivityService>();
        services.TryAddSingleton<LoggingActivityService>();

        // Channel + background writer.
        services.TryAddSingleton<LoggingChannel>();
        services.AddHostedService<LogWriterHostedService>();

        // Capture point-in-time snapshot in ServiceLocator so the public
        // constructor on LoggingActivityService can resolve its repositories.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Throws when the host-supplied database factory targets a provider
    /// Roadbed.Logging does not support in v1.
    /// </summary>
    /// <param name="factory">The factory the installer is about to register.</param>
    private static void ValidateProvider(ILoggingDatabaseFactory factory)
    {
        DataConnectionStringType type = factory.Connecion.ConnectionStringType;

        switch (type)
        {
            case DataConnectionStringType.MySQL:
            case DataConnectionStringType.SQLite:
            case DataConnectionStringType.SQLiteInMemory:
                return;

            default:
                throw new InvalidOperationException(
                    $"Roadbed.Logging does not support {nameof(DataConnectionStringType)} " +
                    $"'{type}'. Supported providers in v1 are MySQL and SQLite.");
        }
    }

    #endregion Private Methods
}
