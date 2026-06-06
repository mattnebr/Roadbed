namespace Roadbed.Logging;

using System;
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
    #region Public Methods

    /// <summary>
    /// Registers the OpenTelemetry MEL provider with batching and the
    /// Roadbed.Logging database exporter.
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

        builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging => logging.AddProcessor(sp =>
                new BatchLogRecordExportProcessor(
                    new RoadbedDbLogRecordExporter(
                        sp.GetRequiredService<LoggingChannel>(),
                        sp.GetRequiredService<LoggingOptions>()))));

        return builder;
    }

    #endregion Public Methods
}
