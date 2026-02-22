/*
 * The namespace Roadbed.Scheduling.Adapters was removed on purpose
 * and replaced with Roadbed.Scheduling so that no additional
 * using statements are required.
 */
namespace Roadbed.Scheduling;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs job execution metrics using standard ILogger.
/// </summary>
/// <remarks>
/// Simple metrics adapter that writes to application logs using structured logging.
/// Useful for basic monitoring without external dependencies.
/// This adapter conditionally includes ResultMessage in log output when present,
/// allowing jobs to provide execution summaries that appear in logs.
/// Thread-safe as it delegates to ILogger which is thread-safe.
/// </remarks>
public sealed class LoggingMetricsAdapter
    : BaseClassWithLoggingFactory<LoggingMetricsAdapter>, ISchedulingMetrics
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMetricsAdapter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for writing metrics.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public LoggingMetricsAdapter(ILogger<LoggingMetricsAdapter> logger)
        : base(logger)
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
    {
        if (!string.IsNullOrWhiteSpace(info.ResultMessage))
        {
            this.LogDebug(
                "Job {JobName} ({JobGroup}) completed in {DurationMs}ms - {ResultMessage}",
                info.JobName,
                info.JobGroup,
                duration.TotalMilliseconds,
                info.ResultMessage);
        }
        else
        {
            this.LogDebug(
                "Job {JobName} ({JobGroup}) completed in {DurationMs}ms",
                info.JobName,
                info.JobGroup,
                duration.TotalMilliseconds);
        }
    }

    /// <inheritdoc/>
    public void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration)
    {
        if (!string.IsNullOrWhiteSpace(info.ResultMessage))
        {
            this.LogWarning(
                exception,
                "Job {JobName} ({JobGroup}) failed after {DurationMs}ms - {ResultMessage}",
                info.JobName,
                info.JobGroup,
                duration.TotalMilliseconds,
                info.ResultMessage);
        }
        else
        {
            this.LogWarning(
                exception,
                "Job {JobName} ({JobGroup}) failed after {DurationMs}ms",
                info.JobName,
                info.JobGroup,
                duration.TotalMilliseconds);
        }
    }

    /// <inheritdoc/>
    public void JobMisfired(JobExecutionInfo info)
    {
        this.LogWarning(
            "Job {JobName} ({JobGroup}) misfired - trigger: {TriggerName}",
            info.JobName,
            info.JobGroup,
            info.TriggerName);
    }

    /// <inheritdoc/>
    public void JobStarted(JobExecutionInfo info)
    {
        this.LogDebug(
            "Job {JobName} ({JobGroup}) started - FireInstanceId: {FireInstanceId}",
            info.JobName,
            info.JobGroup,
            info.FireInstanceId);
    }

    #endregion Public Methods
}