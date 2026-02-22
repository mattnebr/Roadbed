namespace Roadbed.Scheduling.Services;

using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Quartz job listener that bridges to ISchedulingMetrics.
/// </summary>
/// <remarks>
/// Captures job lifecycle events and delegates to configured metrics implementation.
/// Ensures metrics failures never break job execution by wrapping all metrics calls
/// in try-catch blocks that log debug diagnostics instead of propagating exceptions.
/// Uses high-precision timing with Stopwatch.GetTimestamp for accurate duration measurement.
/// Thread-safe as each job execution receives its own context instance.
/// Note: Misfire events are not captured by this listener as they occur at the trigger level,
/// not the job execution level. If misfire tracking is needed, a separate TriggerListener
/// would need to be implemented.
/// </remarks>
internal sealed class SchedulingMetricsListener : JobListenerSupport
{
    #region Private Fields

    private readonly ILogger<SchedulingMetricsListener> _logger;
    private readonly ISchedulingMetrics _metrics;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingMetricsListener"/> class.
    /// </summary>
    /// <param name="metrics">Metrics implementation to receive job lifecycle events.</param>
    /// <param name="logger">Logger instance for recording listener warnings.</param>
    /// <exception cref="ArgumentNullException">Thrown when metrics or logger is null.</exception>
    public SchedulingMetricsListener(
        ISchedulingMetrics metrics,
        ILogger<SchedulingMetricsListener> logger)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);

        this._metrics = metrics;
        this._logger = logger;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public override string Name => "SchedulingMetricsListener";

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public override Task JobToBeExecuted(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var info = this.CreateInfo(context, includeResult: false);

            // Store high-precision timestamp for duration calculation
            context.Put("metrics_start_time", Stopwatch.GetTimestamp());

            this._metrics.JobStarted(info);
        }
        catch (Exception ex)
        {
            // Never let metrics break job execution
            this._logger.LogDebug(
                ex,
                "Failed to record job start metrics for {JobName}",
                context.JobDetail.Key.Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var info = this.CreateInfo(context, includeResult: true);

            // Calculate duration using high-precision timestamps
            var startTimestamp = context.Get("metrics_start_time") as long? ?? 0;
            var duration = startTimestamp > 0
                ? Stopwatch.GetElapsedTime(startTimestamp)
                : TimeSpan.Zero;

            if (jobException is null)
            {
                this._metrics.JobCompleted(info, duration);
            }
            else
            {
                this._metrics.JobFailed(info, jobException, duration);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(
                ex,
                "Failed to record job completion metrics for {JobName}",
                context.JobDetail.Key.Name);
        }

        return Task.CompletedTask;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates JobExecutionInfo from Quartz context.
    /// </summary>
    /// <param name="context">Quartz job execution context.</param>
    /// <param name="includeResult">Whether to include context.Result in ResultMessage.</param>
    /// <returns>Job execution information populated from the context.</returns>
    private JobExecutionInfo CreateInfo(IJobExecutionContext context, bool includeResult)
    {
        return new JobExecutionInfo
        {
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            TriggerName = context.Trigger.Key.Name,
            TriggerGroup = context.Trigger.Key.Group,
            FireInstanceId = context.FireInstanceId,
            FireTimeUtc = context.FireTimeUtc,
            ScheduledFireTimeUtc = context.ScheduledFireTimeUtc,
            PreviousFireTimeUtc = context.PreviousFireTimeUtc,
            NextFireTimeUtc = context.NextFireTimeUtc,
            ResultMessage = includeResult ? context.Result?.ToString() : null,
        };
    }

    #endregion Private Methods
}