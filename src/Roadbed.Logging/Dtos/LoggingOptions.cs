namespace Roadbed.Logging;

using System;
using System.Collections.Generic;

/// <summary>
/// Host-supplied options that govern how Roadbed.Logging persists activity
/// rows and structured log entries.
/// </summary>
/// <remarks>
/// <para>
/// The consuming application registers a populated instance as a singleton
/// in the DI container before <c>InstallModulesInAppDomain</c> runs.
/// Roadbed.Logging does not read <c>IConfiguration</c> directly.
/// </para>
/// <para>
/// Defaults are chosen to be conservative: the write path is non-blocking
/// (drop-oldest), batches flush every five seconds or every thousand rows,
/// and the channel's bounded capacity caps memory pressure during a database
/// outage.
/// </para>
/// </remarks>
public sealed class LoggingOptions
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the schema (database) name under which the
    /// <c>activity</c>, <c>activity_input</c>, and <c>log_entries</c> tables
    /// live.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to render the <c>{Schema}</c> placeholder in DDL install scripts
    /// and to qualify table names in every statement Roadbed.Logging emits.
    /// </para>
    /// <para>
    /// Defaults to the empty string, which means the tables are referenced
    /// unqualified. Set this to the MySQL database name (e.g. <c>"ops"</c>
    /// or <c>"platform"</c>) in production. Leave empty for SQLite unless
    /// the host has <c>ATTACH</c>ed the database under a different alias.
    /// </para>
    /// </remarks>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the application emitting the logs.
    /// </summary>
    /// <remarks>
    /// Stamped onto every <see cref="LoggingActivity"/> and
    /// <see cref="LoggingLogEntry"/> when not explicitly overridden by a
    /// <see cref="LoggingActivityBeginRequest"/>.
    /// </remarks>
    public string Application { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment environment label stamped onto rows.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of rows the background writer
    /// accumulates before flushing.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the longest the background writer waits between flushes
    /// when the batch size threshold has not been reached.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the bounded capacity of the in-process log channel.
    /// </summary>
    /// <remarks>
    /// Caps memory growth during a database outage. Once full, the channel
    /// applies the configured <see cref="ChannelFullPolicy"/>.
    /// </remarks>
    public int ChannelCapacity { get; set; } = 50000;

    /// <summary>
    /// Gets or sets the behavior of the in-process log channel when it
    /// reaches its bounded capacity.
    /// </summary>
    public LoggingChannelFullPolicy ChannelFullPolicy { get; set; } = LoggingChannelFullPolicy.DropOldest;

    /// <summary>
    /// Gets or sets the maximum number of placeholders Roadbed.Logging is
    /// willing to put into a single multi-row INSERT statement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MySQL's MySqlConnector driver caps parameters at 65,535; SQLite's
    /// default is 32,766. Set conservatively to leave headroom for
    /// non-row parameters and to fit comfortably under both providers'
    /// ceilings.
    /// </para>
    /// </remarks>
    public int MaxPlaceholdersPerStatement { get; set; } = 30000;

    /// <summary>
    /// Gets the list of logger category name prefixes whose log records the
    /// exporter discards before they enter the channel.
    /// </summary>
    /// <remarks>
    /// Prevents the database write path from logging through itself. The
    /// default list covers Roadbed.Logging, the data-access libraries, and
    /// the MySql connector. Hosts may add their own prefixes (for instance
    /// to silence a noisy third-party driver).
    /// </remarks>
    public IList<string> RecursionGuardCategories { get; } = new List<string>
    {
        "Roadbed.Logging",
        "Roadbed.Data",
        "Roadbed.Data.MySql",
        "Roadbed.Data.Sqlite",
        "MySqlConnector",
    };

    #endregion Public Properties
}
