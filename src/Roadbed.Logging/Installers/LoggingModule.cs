namespace Roadbed.Logging.Installers;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Roadbed.Data;

/// <summary>
/// Provider-neutral wiring for the Roadbed.Logging activity service,
/// repositories, channel, and background writer.
/// </summary>
/// <remarks>
/// <para>
/// This type is <strong>not</strong> an auto-discovered
/// <c>IServiceCollectionInstaller</c>. The core <c>Roadbed.Logging</c>
/// assembly cannot decide which database client to load, so a provider
/// satellite owns the auto-discovered installer: it registers an
/// <see cref="ILoggingDataExecutor"/> first, then calls
/// <see cref="Register"/>. Keeping the executor registration ahead of this
/// call guarantees it is present in the <c>ServiceLocator</c> snapshot this
/// method captures.
/// </para>
/// <para>
/// The OpenTelemetry pipeline portion of the registration lives on the
/// companion <c>builder.Logging.AddRoadbedDbLogging()</c> extension method
/// because MEL needs <see cref="Microsoft.Extensions.Logging.ILoggingBuilder"/>,
/// which an installer does not see.
/// </para>
/// </remarks>
public static class LoggingModule
{
    #region Public Methods

    /// <summary>
    /// Wires the provider-neutral Roadbed.Logging services into the supplied
    /// collection. Call this from a provider satellite's installer
    /// <em>after</em> registering an <see cref="ILoggingDataExecutor"/>.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="ILoggingDataExecutor"/> has been registered,
    /// when the required <see cref="LoggingOptions"/> /
    /// <see cref="ILoggingDatabaseFactory"/> host registrations are missing.
    /// </exception>
    public static void Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The provider satellite must register its executor before calling
        // Register; absence means the satellite installer is misordered or a
        // consumer wired Register directly without a provider package.
        if (!services.Any(d => d.ServiceType == typeof(ILoggingDataExecutor)))
        {
            throw new InvalidOperationException(
                $"Roadbed.Logging requires an {nameof(ILoggingDataExecutor)} to be registered " +
                "before wiring. Reference exactly one provider package " +
                "(Roadbed.Logging.MySql or Roadbed.Logging.Sqlite); its installer registers the executor.");
        }

        // Resolve host registrations up-front so we can validate them and
        // wire concrete repository implementations into DI before any
        // hosted-service start signal fires.
        LoggingOptions options;

        using (var setupProvider = services.BuildServiceProvider())
        {
            options = setupProvider.GetService<LoggingOptions>()
                ?? throw new InvalidOperationException(
                    $"Roadbed.Logging requires a singleton {nameof(LoggingOptions)} to be " +
                    $"registered before InstallModulesInAppDomain runs.");

            _ = setupProvider.GetService<ILoggingDatabaseFactory>()
                ?? throw new InvalidOperationException(
                    $"Roadbed.Logging requires a singleton {nameof(ILoggingDatabaseFactory)} " +
                    $"to be registered before InstallModulesInAppDomain runs.");
        }

        // Register the Dapper [Column] mapping for the three entities so
        // any future read paths (and the integration test suite) materialize
        // rows correctly.
        DapperMapping.Configure(
            typeof(LoggingActivity),
            typeof(LoggingActivityInput),
            typeof(LoggingLogEntry));

        // Repositories — internal sealed; one instance per process. Each
        // depends on the satellite-supplied ILoggingDataExecutor.
        services.TryAddSingleton<ILoggingActivityRepository, LoggingActivityRepository>();
        services.TryAddSingleton<ILoggingActivityInputRepository, LoggingActivityInputRepository>();
        services.TryAddSingleton<ILoggingLogEntryRepository, LoggingLogEntryRepository>();

        // Activity service — public sealed; the host injects this directly.
        services.TryAddSingleton<ILoggingActivityService, LoggingActivityService>();
        services.TryAddSingleton<LoggingActivityService>();

        // LoggingChannel — process-wide shared instance. Constructed eagerly
        // from LoggingOptions and registered as a concrete singleton so
        // every IServiceProvider built from this IServiceCollection
        // (the host's container and every ServiceLocator snapshot) resolves
        // the SAME object. Producers (OTel DB exporter realized in any
        // container's MEL pipeline) and the single consumer
        // (LogWriterHostedService running in the host) therefore meet
        // around one channel, even though each container builds its own
        // OTel provider and exporter.
        if (!services.Any(d => d.ServiceType == typeof(LoggingChannel)))
        {
            services.AddSingleton<LoggingChannel>(new LoggingChannel(options));
        }

        services.AddHostedService<LogWriterHostedService>();

        // Capture point-in-time snapshot in ServiceLocator so the public
        // constructor on LoggingActivityService can resolve its repositories.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods
}
