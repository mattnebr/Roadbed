namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Writes batches of <see cref="LoggingLogEntry"/> rows to the
/// <c>log_entries</c> table using chunked multi-row INSERT statements.
/// </summary>
/// <remarks>
/// This is the internal custom bulk insert called out in the
/// Roadbed.Logging plan §4. It deliberately does NOT take a
/// uniform-<c>activityId</c> parameter: each <see cref="LoggingLogEntry"/>
/// supplies its own <see cref="LoggingLogEntry.ActivityId"/> column value,
/// because log batches mix entries that originated under different
/// activity scopes.
/// </remarks>
internal sealed class LoggingLogEntryRepository
    : BaseClassWithLogging,
      ILoggingLogEntryRepository
{
    #region Private Fields

    /// <summary>
    /// Number of placeholders per inserted row. Must match the column list in <see cref="ColumnList"/>.
    /// </summary>
    private const int PlaceholdersPerRow = 16;

    /// <summary>
    /// Comma-separated column list shared by every chunk INSERT.
    /// </summary>
    private const string ColumnList =
        "event_time_utc" +
        ",log_level" +
        ",category" +
        ",event_id" +
        ",event_name" +
        ",message" +
        ",message_template" +
        ",properties" +
        ",exception" +
        ",exception_type" +
        ",activity_id" +
        ",trace_id" +
        ",span_id" +
        ",application" +
        ",environment" +
        ",host" +
        ",process_id";

    private readonly ILoggingDataExecutor _executor;
    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;
    private readonly int _maxRowsPerChunk;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingLogEntryRepository"/> class.
    /// </summary>
    /// <param name="executor">Provider-neutral execution port supplied by the active provider satellite.</param>
    /// <param name="factory">Database connection factory pointing at the log_entries schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingLogEntryRepository(
        ILoggingDataExecutor executor,
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingLogEntryRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._executor = executor;
        this._factory = factory;
        this._tableRef = string.IsNullOrWhiteSpace(options.Schema)
            ? "log_entries"
            : $"{options.Schema}.log_entries";
        this._maxRowsPerChunk = Math.Max(1, options.MaxPlaceholdersPerStatement / PlaceholdersPerRow);
    }

    #endregion Public Constructors

    #region Internal Properties

    /// <summary>
    /// Gets the maximum number of rows per chunk derived from the configured placeholder ceiling.
    /// </summary>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for chunking-boundary verification.
    /// </remarks>
    internal int MaxRowsPerChunk => this._maxRowsPerChunk;

    #endregion Internal Properties

    #region Public Methods

    /// <inheritdoc/>
    public async Task<int> BulkInsertAsync(
        IReadOnlyList<LoggingLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return 0;
        }

        int totalInserted = 0;

        for (int offset = 0; offset < entries.Count; offset += this._maxRowsPerChunk)
        {
            int chunkSize = Math.Min(this._maxRowsPerChunk, entries.Count - offset);
            int rowsInserted = await this.InsertChunkAsync(
                entries,
                offset,
                chunkSize,
                cancellationToken).ConfigureAwait(false);

            totalInserted += rowsInserted;
        }

        return totalInserted;
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Builds the Dapper <see cref="DynamicParameters"/> bag for the supplied chunk.
    /// </summary>
    /// <param name="entries">The full batch of entries.</param>
    /// <param name="offset">Index of the first entry in this chunk.</param>
    /// <param name="chunkSize">Number of entries in this chunk.</param>
    /// <returns>Populated <see cref="DynamicParameters"/> matching the placeholders in <see cref="BuildInsertSql(int)"/>.</returns>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for parameter-mapping verification.
    /// </remarks>
    internal static DynamicParameters BuildParameters(
        IReadOnlyList<LoggingLogEntry> entries,
        int offset,
        int chunkSize)
    {
        var parameters = new DynamicParameters();

        for (int i = 0; i < chunkSize; i++)
        {
            var entry = entries[offset + i];
            string n = i.ToString(CultureInfo.InvariantCulture);

            parameters.Add("p" + n + "_event_time_utc", entry.EventTimeUtc);
            parameters.Add("p" + n + "_log_level", entry.LogLevel);
            parameters.Add("p" + n + "_category", entry.Category);
            parameters.Add("p" + n + "_event_id", entry.EventId);
            parameters.Add("p" + n + "_event_name", entry.EventName);
            parameters.Add("p" + n + "_message", entry.Message);
            parameters.Add("p" + n + "_message_template", entry.MessageTemplate);
            parameters.Add("p" + n + "_properties", entry.PropertiesJson);
            parameters.Add("p" + n + "_exception", entry.Exception);
            parameters.Add("p" + n + "_exception_type", entry.ExceptionType);
            parameters.Add("p" + n + "_activity_id", entry.ActivityId);
            parameters.Add("p" + n + "_trace_id", entry.TraceId);
            parameters.Add("p" + n + "_span_id", entry.SpanId);
            parameters.Add("p" + n + "_application", entry.Application);
            parameters.Add("p" + n + "_environment", entry.Environment);
            parameters.Add("p" + n + "_host", entry.Host);
            parameters.Add("p" + n + "_process_id", entry.ProcessId);
        }

        return parameters;
    }

    /// <summary>
    /// Builds the multi-row INSERT SQL for the supplied chunk size.
    /// </summary>
    /// <param name="chunkSize">Number of rows in the chunk.</param>
    /// <returns>The rendered SQL with positional placeholders <c>@p{n}_{column}</c>.</returns>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.
    /// </remarks>
    internal string BuildInsertSql(int chunkSize)
    {
        var builder = new StringBuilder();
        builder.Append("INSERT INTO ").Append(this._tableRef).Append(" (").Append(ColumnList).Append(") VALUES ");

        for (int i = 0; i < chunkSize; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            string n = i.ToString(CultureInfo.InvariantCulture);

            builder
                .Append('(')
                .Append("@p").Append(n).Append("_event_time_utc")
                .Append(",@p").Append(n).Append("_log_level")
                .Append(",@p").Append(n).Append("_category")
                .Append(",@p").Append(n).Append("_event_id")
                .Append(",@p").Append(n).Append("_event_name")
                .Append(",@p").Append(n).Append("_message")
                .Append(",@p").Append(n).Append("_message_template")
                .Append(",@p").Append(n).Append("_properties")
                .Append(",@p").Append(n).Append("_exception")
                .Append(",@p").Append(n).Append("_exception_type")
                .Append(",@p").Append(n).Append("_activity_id")
                .Append(",@p").Append(n).Append("_trace_id")
                .Append(",@p").Append(n).Append("_span_id")
                .Append(",@p").Append(n).Append("_application")
                .Append(",@p").Append(n).Append("_environment")
                .Append(",@p").Append(n).Append("_host")
                .Append(",@p").Append(n).Append("_process_id")
                .Append(')');
        }

        builder.Append(';');

        return builder.ToString();
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Inserts a single chunk of entries via one multi-row INSERT.
    /// </summary>
    /// <param name="entries">The full batch of entries.</param>
    /// <param name="offset">Index of the first entry in this chunk.</param>
    /// <param name="chunkSize">Number of entries in this chunk.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The number of rows the database reported as inserted.</returns>
    private async Task<int> InsertChunkAsync(
        IReadOnlyList<LoggingLogEntry> entries,
        int offset,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        string sql = this.BuildInsertSql(chunkSize);
        DynamicParameters parameters = BuildParameters(entries, offset, chunkSize);

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        return await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Private Methods
}
