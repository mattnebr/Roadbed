namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that drains the in-process <see cref="LoggingChannel"/>,
/// batches entries, and persists them via
/// <see cref="ILoggingLogEntryRepository.BulkInsertAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// Batches flush when either the configured row count is reached or the
/// flush interval elapses, whichever comes first. On shutdown the service
/// drains the remaining buffer before returning.
/// </para>
/// <para>
/// If the underlying repository throws, the batch is rewritten to
/// <see cref="Console.Error"/> as a recursion-safe fallback so log lines
/// are never silently lost. The writer continues to consume new entries
/// after a failure — a single bad batch must not wedge the pipeline.
/// </para>
/// </remarks>
internal sealed class LogWriterHostedService : BackgroundService
{
    #region Private Fields

    private readonly LoggingChannel _channel;
    private readonly ILoggingLogEntryRepository _repository;
    private readonly LoggingOptions _options;
    private readonly ILogger<LogWriterHostedService> _logger;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LogWriterHostedService"/> class.
    /// </summary>
    /// <param name="channel">Bounded channel feeding the writer.</param>
    /// <param name="repository">Repository handling the chunked bulk INSERT.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Diagnostic logger. Its category sits under the recursion-guard prefix to keep the writer from logging through itself.</param>
    public LogWriterHostedService(
        LoggingChannel channel,
        ILoggingLogEntryRepository repository,
        LoggingOptions options,
        ILogger<LogWriterHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this._channel = channel;
        this._repository = repository;
        this._options = options;
        this._logger = logger;
    }

    #endregion Public Constructors

    #region Internal Methods

    /// <summary>
    /// Flushes the accumulated batch to the repository.
    /// </summary>
    /// <param name="batch">The batch to flush. Cleared after the call regardless of outcome.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying bulk insert.</param>
    /// <returns>A task that completes when the flush attempt finishes.</returns>
    /// <remarks>
    /// Exposed to <c>Roadbed.Test.Unit</c> for fallback-behavior verification.
    /// </remarks>
    internal async Task FlushAsync(List<LoggingLogEntry> batch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await this._repository
                .BulkInsertAsync(batch, cancellationToken)
                .ConfigureAwait(false);

            long dropped = this._channel.ConsumeDroppedCount();
            if (dropped > 0 && this._logger.IsEnabled(LogLevel.Warning))
            {
                this._logger.LogWarning(
                    "Dropped {DroppedCount} log entries due to channel pressure",
                    dropped);
            }
        }
        catch (Exception ex)
        {
            WriteFallback(batch, ex);
        }
        finally
        {
            batch.Clear();
        }
    }

    #endregion Internal Methods

    #region Protected Methods

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<LoggingLogEntry>(this._options.BatchSize);
        var watch = Stopwatch.StartNew();

        try
        {
            while (await this._channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                // Drain whatever is immediately available without awaiting,
                // then evaluate flush conditions.
                while (this._channel.Reader.TryRead(out LoggingLogEntry? entry))
                {
                    batch.Add(entry);

                    if (batch.Count >= this._options.BatchSize)
                    {
                        await this.FlushAsync(batch, stoppingToken).ConfigureAwait(false);
                        watch.Restart();
                    }
                }

                if (batch.Count > 0 && watch.Elapsed >= this._options.FlushInterval)
                {
                    await this.FlushAsync(batch, stoppingToken).ConfigureAwait(false);
                    watch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — fall through to the final drain.
        }

        // Final drain on shutdown: pick up anything still in the channel.
        while (this._channel.Reader.TryRead(out LoggingLogEntry? entry))
        {
            batch.Add(entry);
        }

        if (batch.Count > 0)
        {
            await this.FlushAsync(batch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Writes the batch to <see cref="Console.Error"/> as a last-resort
    /// fallback when the database insert fails.
    /// </summary>
    /// <param name="batch">The batch that could not be persisted.</param>
    /// <param name="error">The exception thrown by the failed insert.</param>
    private static void WriteFallback(IReadOnlyCollection<LoggingLogEntry> batch, Exception error)
    {
        Console.Error.WriteLine(
            "Roadbed.Logging: failed to persist {0} log entr{1}; falling back to Console.Error. Reason: {2}: {3}",
            batch.Count,
            batch.Count == 1 ? "y" : "ies",
            error.GetType().FullName,
            error.Message);

        foreach (LoggingLogEntry entry in batch)
        {
            Console.Error.WriteLine(
                "[{0:O}] {1} {2}: {3}",
                entry.EventTimeUtc,
                (Microsoft.Extensions.Logging.LogLevel)entry.LogLevel,
                entry.Category,
                entry.Message);
        }
    }

    #endregion Private Methods
}
