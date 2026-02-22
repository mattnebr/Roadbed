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
/// One-time scheduled job that logs information about all actively scheduled jobs at startup.
/// </summary>
public class SchedulingStartupJobsSummaryJob
    : BaseSchedulingJob<SchedulingStartupJobsSummaryJob>
{
    #region Private Fields

    private readonly IServiceProvider _serviceProvider;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingStartupJobsSummaryJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="serviceProvider">Service provider to resolve the scheduler factory at execution time.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or serviceProvider is null.</exception>
    public SchedulingStartupJobsSummaryJob(
        ILogger<SchedulingStartupJobsSummaryJob> logger,
        IServiceProvider serviceProvider)
        : base(
            name: "ScheduledJobsStartupSummary",
            description: "Logs a one-time summary of all actively scheduled jobs 30 seconds after startup",
            schedule: new SchedulingSchedule(DateTime.UtcNow.AddSeconds(30))
            {
                TimeZone = TimeZoneInfo.Utc,
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
        this.LogDebug("Starting startup scheduled jobs summary");

        var schedulerFactory = this._serviceProvider.GetRequiredService<ISchedulerFactory>();
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        StringBuilder summary = new StringBuilder();
        int totalJobs = await SchedulingJobSummaryService.CreateJobScheduleSummaryAsync(
            summary,
            scheduler,
            cancellationToken);

        this.LogDebug("{JobsSummary}", summary.ToString());
        this.LogDebug("Completed startup scheduled jobs summary. Total jobs: {TotalJobs}", totalJobs);
    }

    #endregion Public Methods
}
