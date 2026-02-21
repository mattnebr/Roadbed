namespace Roadbed.Scheduling.Installers;

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Roadbed.Scheduling.Services;

/// <summary>
/// Configures services for the scheduling framework.
/// </summary>
/// <remarks>
/// This installer automatically discovers all ISchedulingJob implementations,
/// registers them in the DI container, configures Quartz.NET, and sets up
/// the metrics listener for job execution tracking.
/// </remarks>
public class InstallScheduling : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register default no-op metrics if none provided by consumer
        if (!services.Any(d => d.ServiceType == typeof(ISchedulingMetrics)))
        {
            services.AddSingleton<ISchedulingMetrics>(NullSchedulingMetrics.Instance);
        }

        // Register metrics listener (internal, uses ISchedulingMetrics from DI)
        services.AddSingleton<SchedulingMetricsListener>();

        // Discover and register all ISchedulingJob implementations
        RegisterSchedulingJobs(services);

        // Configure Quartz.NET
        services.AddQuartz(q =>
        {
            // Use in-memory job store
            q.UseInMemoryStore();

            // Add metrics listener
            q.AddJobListener<SchedulingMetricsListener>();

            // Configure all discovered jobs
            ConfigureQuartzJobs(q, services);
        });

        // Add Quartz hosted service to run in the background
        services.AddQuartzHostedService(options =>
        {
            // Wait for jobs to complete on shutdown
            options.WaitForJobsToComplete = true;
        });

        // Capture point-in-time snapshot in ServiceLocator
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Applies cron misfire strategy to schedule builder.
    /// </summary>
    /// <param name="builder">Cron schedule builder.</param>
    /// <param name="strategy">Misfire strategy to apply.</param>
    private static void ApplyCronMisfireStrategy(
        CronScheduleBuilder builder,
        SchedulingMisfireStrategy strategy)
    {
        switch (strategy)
        {
            case SchedulingMisfireStrategy.DoNothing:
                builder.WithMisfireHandlingInstructionDoNothing();
                break;

            case SchedulingMisfireStrategy.FireAndProceed:
                builder.WithMisfireHandlingInstructionFireAndProceed();
                break;

            case SchedulingMisfireStrategy.IgnoreMisfires:
                builder.WithMisfireHandlingInstructionIgnoreMisfires();
                break;
        }
    }

    /// <summary>
    /// Applies simple misfire strategy to schedule builder.
    /// </summary>
    /// <param name="builder">Simple schedule builder.</param>
    /// <param name="strategy">Misfire strategy to apply.</param>
    private static void ApplySimpleMisfireStrategy(
        SimpleScheduleBuilder builder,
        SchedulingMisfireStrategy strategy)
    {
        switch (strategy)
        {
            case SchedulingMisfireStrategy.FireNow:
                builder.WithMisfireHandlingInstructionFireNow();
                break;

            case SchedulingMisfireStrategy.IgnoreMisfires:
                builder.WithMisfireHandlingInstructionIgnoreMisfires();
                break;

            case SchedulingMisfireStrategy.NextWithExistingCount:
                builder.WithMisfireHandlingInstructionNextWithExistingCount();
                break;

            case SchedulingMisfireStrategy.NextWithRemainingCount:
                builder.WithMisfireHandlingInstructionNextWithRemainingCount();
                break;

            case SchedulingMisfireStrategy.NowWithExistingCount:
                builder.WithMisfireHandlingInstructionNowWithExistingCount();
                break;

            case SchedulingMisfireStrategy.NowWithRemainingCount:
                builder.WithMisfireHandlingInstructionNowWithRemainingCount();
                break;
        }
    }

    /// <summary>
    /// Configures Quartz jobs from discovered ISchedulingJob implementations.
    /// </summary>
    /// <param name="configurator">Quartz service collection configurator.</param>
    /// <param name="services">Service collection for resolving job instances.</param>
    private static void ConfigureQuartzJobs(
        IServiceCollectionQuartzConfigurator configurator,
        IServiceCollection services)
    {
        // Get all registered ISchedulingJob types
        var jobTypes = services
            .Where(d => d.ServiceType == typeof(ISchedulingJob))
            .Select(d => d.ImplementationType)
            .Where(t => t != null)
            .Distinct()
            .ToList();

        // Build a single temporary provider for reading job schedule metadata
        using var tempProvider = services.BuildServiceProvider();

        foreach (var jobType in jobTypes)
        {
            ISchedulingJob job;

            try
            {
                job = (ISchedulingJob)tempProvider.GetRequiredService(jobType!);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve scheduling job '{jobType!.Name}' during Quartz configuration. " +
                    $"Job constructors must only depend on services registered before AddQuartz runs. " +
                    $"For runtime-only dependencies (e.g., IScheduler, ISchedulerFactory), " +
                    $"inject IServiceProvider and resolve them in ExecuteAsync instead.",
                    ex);
            }

            var schedule = job.Schedule;
            var jobKey = new JobKey(job.Name, schedule.GroupName);

            // Configure the job
            configurator.AddJob(jobType!, jobKey, jobConfig =>
            {
                jobConfig.WithDescription(job.Description);
            });

            // Create trigger based on schedule type
            configurator.AddTrigger(trigger =>
            {
                trigger
                    .ForJob(jobKey)
                    .WithIdentity($"{job.Name}-trigger", schedule.GroupName)
                    .WithPriority((int)schedule.Priority);

                // Configure schedule based on type
                switch (schedule.ScheduleType)
                {
                    case SchedulingScheduleType.Cron:
                        trigger.WithCronSchedule(schedule.CronExpression!, builder =>
                        {
                            builder.InTimeZone(schedule.TimeZone);

                            if (schedule.MisfireHandlingEnabled)
                            {
                                ApplyCronMisfireStrategy(builder, schedule.MisfireStrategy);
                            }
                        });
                        break;

                    case SchedulingScheduleType.SimpleInterval:
                        trigger
                            .StartAt(DateTimeOffset.UtcNow.Add(schedule.StartDelay!.Value))
                            .WithSimpleSchedule(builder =>
                            {
                                builder.WithInterval(schedule.Interval!.Value).RepeatForever();

                                if (schedule.MaxExecutionCount.HasValue)
                                {
                                    builder.WithRepeatCount(schedule.MaxExecutionCount.Value - 1);
                                }

                                if (schedule.MisfireHandlingEnabled)
                                {
                                    ApplySimpleMisfireStrategy(builder, schedule.MisfireStrategy);
                                }
                            });
                        break;

                    case SchedulingScheduleType.SpecificTimeOnce:
                        trigger.StartAt(schedule.StartAt!.Value);
                        break;

                    case SchedulingScheduleType.SpecificTimeWithInterval:
                        trigger
                            .StartAt(schedule.StartAt!.Value)
                            .WithSimpleSchedule(builder =>
                            {
                                builder.WithInterval(schedule.Interval!.Value).RepeatForever();

                                if (schedule.MaxExecutionCount.HasValue)
                                {
                                    builder.WithRepeatCount(schedule.MaxExecutionCount.Value - 1);
                                }

                                if (schedule.MisfireHandlingEnabled)
                                {
                                    ApplySimpleMisfireStrategy(builder, schedule.MisfireStrategy);
                                }
                            });
                        break;
                }
            });
        }
    }

    /// <summary>
    /// Discovers and registers all ISchedulingJob implementations.
    /// </summary>
    /// <param name="services">Service collection.</param>
    private static void RegisterSchedulingJobs(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.FullName!.StartsWith("System."))
            .Where(a => !a.FullName!.StartsWith("Microsoft."));

        var jobTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ISchedulingJob).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length > 0));

        foreach (var jobType in jobTypes)
        {
            // Double registration pattern:
            // 1. As concrete type (for DI injection)
            services.AddTransient(jobType);

            // 2. As ISchedulingJob (for discovery)
            services.AddTransient(typeof(ISchedulingJob), jobType);
        }
    }

    #endregion Private Methods
}