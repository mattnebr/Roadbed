namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// OpenTelemetry exporter that maps <see cref="LogRecord"/> instances into
/// <see cref="LoggingLogEntry"/> rows and queues them on the in-process
/// <see cref="LoggingChannel"/>.
/// </summary>
/// <remarks>
/// <para>
/// The exporter does not write to the database itself. The
/// <c>LogWriterHostedService</c> drains the channel, batches the entries,
/// and performs the chunked multi-row INSERT off the hot path.
/// </para>
/// <para>
/// Log records whose category matches any of the configured
/// <see cref="LoggingOptions.RecursionGuardCategories"/> prefixes are
/// silently discarded to prevent the database write path from logging
/// through itself.
/// </para>
/// </remarks>
internal sealed class RoadbedDbLogRecordExporter : BaseExporter<LogRecord>
{
    #region Private Fields

    /// <summary>
    /// Scope-state key under which <see cref="LoggingActivityService.BeginAsync"/> publishes the activity identifier.
    /// </summary>
    private const string ActivityIdScopeKey = "activity_id";

    /// <summary>
    /// Tag key under which <see cref="LoggingActivityService.BeginAsync"/> publishes the activity identifier on the diagnostic activity.
    /// </summary>
    private const string ActivityIdTagKey = "roadbed.activity_id";

    /// <summary>
    /// The structured-arg key OTel uses for the unrendered message template.
    /// </summary>
    private const string OriginalFormatKey = "{OriginalFormat}";

    private readonly LoggingChannel _channel;
    private readonly LoggingOptions _options;
    private readonly string _hostName;
    private readonly int _processId;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="RoadbedDbLogRecordExporter"/> class.
    /// </summary>
    /// <param name="channel">Bounded channel that hands records off to the background writer.</param>
    /// <param name="options">Host-supplied logging options.</param>
    public RoadbedDbLogRecordExporter(
        LoggingChannel channel,
        LoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(options);

        this._channel = channel;
        this._options = options;
        this._hostName = Environment.MachineName;
        this._processId = Environment.ProcessId;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        foreach (LogRecord record in batch)
        {
            if (this.IsRecursionGuarded(record.CategoryName))
            {
                continue;
            }

            LoggingLogEntry entry = this.MapRecord(record);
            this._channel.TryWrite(entry);
        }

        return ExportResult.Success;
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Maps a single <see cref="LogRecord"/> to a <see cref="LoggingLogEntry"/>.
    /// </summary>
    /// <param name="record">The source log record.</param>
    /// <returns>The mapped entry, ready to enqueue.</returns>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for mapping-fidelity verification.
    /// </remarks>
    internal LoggingLogEntry MapRecord(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        string? messageTemplate = null;
        Dictionary<string, object?>? namedArgs = null;

        if (record.Attributes is not null)
        {
            foreach (KeyValuePair<string, object?> attribute in record.Attributes)
            {
                if (string.Equals(attribute.Key, OriginalFormatKey, StringComparison.Ordinal))
                {
                    messageTemplate = attribute.Value?.ToString();
                    continue;
                }

                namedArgs ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                namedArgs[attribute.Key] = attribute.Value;
            }
        }

        string? activityIdFromScope = ResolveActivityIdFromScope(record);
        string? activityIdFromActivityTag = ResolveActivityIdFromCurrentActivity();
        string? activityId = activityIdFromScope ?? activityIdFromActivityTag;

        return new LoggingLogEntry
        {
            EventTimeUtc = record.Timestamp,
            LogLevel = (byte)(int)record.LogLevel,
            Category = record.CategoryName ?? string.Empty,
            EventId = record.EventId.Id == 0 ? null : record.EventId.Id,
            EventName = string.IsNullOrEmpty(record.EventId.Name) ? null : record.EventId.Name,
            Message = record.FormattedMessage ?? record.Body ?? string.Empty,
            MessageTemplate = messageTemplate,
            PropertiesJson = namedArgs is null
                ? null
                : JsonConvert.SerializeObject(namedArgs),
            Exception = record.Exception?.Message,
            ExceptionType = record.Exception?.GetType().FullName ?? record.Exception?.GetType().Name,
            ActivityId = activityId,
            TraceId = record.TraceId == default ? null : record.TraceId.ToHexString(),
            SpanId = record.SpanId == default ? null : record.SpanId.ToHexString(),
            Application = this._options.Application,
            Environment = this._options.Environment,
            Host = this._hostName,
            ProcessId = this._processId,
        };
    }

    /// <summary>
    /// Determines whether the supplied category falls under any of the
    /// configured recursion-guard prefixes.
    /// </summary>
    /// <param name="categoryName">The log record's category, possibly null.</param>
    /// <returns><c>true</c> when the category should be dropped without writing.</returns>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for recursion-guard verification.
    /// </remarks>
    internal bool IsRecursionGuarded(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            return false;
        }

        foreach (string guard in this._options.RecursionGuardCategories)
        {
            if (string.IsNullOrEmpty(guard))
            {
                continue;
            }

            if (categoryName.StartsWith(guard, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Walks the log record's scope chain to find the activity identifier
    /// published by <c>LoggingActivityService.BeginAsync</c>.
    /// </summary>
    /// <param name="record">The source log record.</param>
    /// <returns>The activity identifier found, or <c>null</c> when no scope frame published one.</returns>
    private static string? ResolveActivityIdFromScope(LogRecord record)
    {
        var holder = new ScopeProbeState();

        record.ForEachScope(
            static (scope, state) =>
            {
                if (state.Found is not null)
                {
                    return;
                }

                foreach (KeyValuePair<string, object?> item in scope)
                {
                    if (string.Equals(item.Key, ActivityIdScopeKey, StringComparison.Ordinal))
                    {
                        state.Found = item.Value?.ToString();
                        return;
                    }
                }
            },
            holder);

        return holder.Found;
    }

    /// <summary>
    /// Returns the activity identifier tagged on <see cref="Activity.Current"/>,
    /// when one is present.
    /// </summary>
    /// <returns>The activity identifier from the current diagnostic activity, or <c>null</c>.</returns>
    private static string? ResolveActivityIdFromCurrentActivity()
    {
        Activity? current = Activity.Current;

        if (current is null)
        {
            return null;
        }

        object? tagValue = current.GetTagItem(ActivityIdTagKey);
        return tagValue?.ToString();
    }

    #endregion Private Methods

    #region Private Types

    /// <summary>
    /// Mutable carrier that <see cref="LogRecord.ForEachScope{TState}"/>
    /// can write into without forcing the exporter to allocate a closure.
    /// </summary>
    private sealed class ScopeProbeState
    {
        /// <summary>
        /// Gets or sets the activity identifier discovered in a scope, when present.
        /// </summary>
        public string? Found { get; set; }
    }

    #endregion Private Types
}
