namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Writes rows to the <c>activity_input</c> lineage edge table.
/// </summary>
internal sealed class LoggingActivityInputRepository
    : BaseClassWithLogging,
      ILoggingActivityInputRepository
{
    #region Private Fields

    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityInputRepository"/> class.
    /// </summary>
    /// <param name="factory">Database connection factory pointing at the activity schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingActivityInputRepository(
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingActivityInputRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._factory = factory;
        this._tableRef = string.IsNullOrWhiteSpace(options.Schema)
            ? "activity_input"
            : $"{options.Schema}.activity_input";
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task InsertAsync(
        LoggingActivityInput entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.ActivityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.InputActivityId);

        // INSERT ... ON DUPLICATE KEY UPDATE on MySQL; INSERT OR IGNORE on SQLite.
        // Either way the duplicate-PK case is treated as a successful no-op.
        string sql = this._factory.Connecion.ConnectionStringType switch
        {
            DataConnectionStringType.MySQL => $@"
                INSERT INTO {this._tableRef}
                (
                     activity_id
                    ,input_activity_id
                    ,input_role
                )
                VALUES
                (
                     @ActivityId
                    ,@InputActivityId
                    ,@InputRole
                )
                ON DUPLICATE KEY UPDATE input_role = COALESCE(VALUES(input_role), input_role)
                ;",

            _ => $@"
                INSERT OR IGNORE INTO {this._tableRef}
                (
                     activity_id
                    ,input_activity_id
                    ,input_role
                )
                VALUES
                (
                     @ActivityId
                    ,@InputActivityId
                    ,@InputRole
                )
                ;",
        };

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                entity.ActivityId,
                entity.InputActivityId,
                entity.InputRole,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public Methods
}
