namespace Roadbed.Scheduling.Jobs;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Roadbed.Scheduling.Services;

/// <summary>
/// Scheduled job that logs information about all actively scheduled jobs.
/// </summary>
public class SchedulingScheduledJobsSummaryJob
    : BaseSchedulingJob<SchedulingScheduledJobsSummaryJob>
{
    #region Private Fields

    private readonly IServiceProvider _serviceProvider;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingScheduledJobsSummaryJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="serviceProvider">Service provider to resolve the scheduler factory at execution time.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or serviceProvider is null.</exception>
    public SchedulingScheduledJobsSummaryJob(
        ILogger<SchedulingScheduledJobsSummaryJob> logger,
        IServiceProvider serviceProvider)
        : base(
            name: "ScheduledJobsSummary",
            description: "Logs a daily summary of all actively scheduled jobs in the system",
            schedule: new SchedulingSchedule("0 0 8 * * ?") // Daily at 8:00 AM
            {
                TimeZone = TimeZoneInfo.Local,
                GroupName = "System",
                Priority = SchedulingJobPriority.Lowest,
            },
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this._serviceProvider = serviceProvider;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogDebug("Starting scheduled jobs summary");

        var schedulerFactory = this._serviceProvider.GetRequiredService<ISchedulerFactory>();
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        StringBuilder summary = new StringBuilder();

        int totalJobs = await SchedulingJobSummaryService.CreateJobScheduleSummaryAsync(
            summary,
            scheduler,
            cancellationToken);

        this.LogInformation("{JobsSummary}", summary.ToString());
        this.LogDebug("Completed scheduled jobs summary. Total jobs: {TotalJobs}", totalJobs);
    }

    #endregion Public Methods
}
