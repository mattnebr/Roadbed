namespace Roadbed.Logging;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// Extension methods that wire the OpenTelemetry logging pipeline into a
/// host's <see cref="ILoggingBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Call this after the host has registered <see cref="LoggingOptions"/> and
/// <see cref="ILoggingDatabaseFactory"/>, and ideally after
/// <c>InstallModulesInAppDomain</c> has wired the
/// <see cref="LoggingChannel"/> and repositories. The DI graph captured at
/// host build time resolves the channel from the same singleton everyone
/// else uses.
/// </para>
/// </remarks>
public static class LoggingBuilderExtensions
{
    #region Private Fields

    /// <summary>
    /// Empty configuration handed to the provider satellite installer — the
    /// Roadbed.Logging provider installers do not read configuration, and the
    /// <see cref="ILoggingBuilder"/> seam has no <see cref="IConfiguration"/>
    /// to forward.
    /// </summary>
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Wires the Roadbed.Logging OpenTelemetry exporter <em>and</em> selects
    /// the database provider in one call by naming its satellite installer.
    /// </summary>
    /// <typeparam name="TProviderInstaller">The provider satellite installer — <c>InstallLoggingMySql</c> or <c>InstallLoggingSqlite</c>. Naming the concrete type compile-pins the satellite assembly, so it loads and wires without relying on assembly auto-discovery.</typeparam>
    /// <param name="builder">The host's logging builder.</param>
    /// <param name="configureOptions">Optional callback to tweak the underlying <see cref="OpenTelemetryLoggerOptions"/>.</param>
    /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
    /// <remarks>
    /// This is the single authoritative wiring call. Register the singleton
    /// <see cref="LoggingOptions"/> and <see cref="ILoggingDatabaseFactory"/>
    /// <strong>before</strong> calling it; the provider installer runs the
    /// rest of the pipeline (executor, repositories, channel, writer) eagerly.
    /// No <c>typeof(...)</c> discard, manual <c>Assembly.Load</c>, or
    /// auto-discovery pass is required to make the provider discoverable.
    /// </remarks>
    public static ILoggingBuilder AddRoadbedDbLogging<TProviderInstaller>(
        this ILoggingBuilder builder,
        Action<OpenTelemetryLoggerOptions>? configureOptions = null)
        where TProviderInstaller : IServiceCollectionInstaller, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddRoadbedDbLogging(builder, configureOptions);
        builder.Services.InstallModule<TProviderInstaller>(EmptyConfiguration);

        return builder;
    }

    /// <summary>
    /// Registers the OpenTelemetry MEL provider and the Roadbed.Logging
    /// database exporter, without selecting a provider. Prefer the generic
    /// <see cref="AddRoadbedDbLogging{TProviderInstaller}"/> overload, which
    /// also wires the provider in the same call.
    /// </summary>
    /// <param name="builder">The host's logging builder.</param>
    /// <param name="configureOptions">Optional callback that lets the host tweak the underlying <see cref="OpenTelemetryLoggerOptions"/> (e.g. to switch scope inclusion off).</param>
    /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
    public static ILoggingBuilder AddRoadbedDbLogging(
        this ILoggingBuilder builder,
        Action<OpenTelemetryLoggerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            configureOptions?.Invoke(options);
        });

        // Use the synchronous simple processor rather than the batch
        // processor. The exporter is intentionally cheap: it maps the record
        // and hands it to the in-process logging channel, and the background
        // log-writer hosted service performs the real async batched insert off
        // the hot path. An OTel-level batch processor would only add a
        // redundant second buffer.
        //
        // More importantly, the batch processor runs the export on a
        // background drain thread where the ambient diagnostic activity is no
        // longer current. The exporter resolves the activity id column from
        // that ambient activity's roadbed activity-id tag — the same activity
        // that feeds the trace and span id columns — so the export must run in
        // the emitting execution context. The simple processor exports
        // synchronously on the thread that logged, giving the activity id
        // column the same coverage as the trace and span id columns,
        // including the caller's own log lines inside a logging activity
        // scope.
        builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging => logging.AddProcessor(sp =>
                new SimpleLogRecordExportProcessor(
                    new RoadbedDbLogRecordExporter(
                        channelAccessor: () => sp.GetRequiredService<LoggingChannel>(),
                        options: sp.GetRequiredService<LoggingOptions>()))));

        return builder;
    }

    #endregion Public Methods
}
