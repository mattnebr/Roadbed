namespace Roadbed.Scheduling.Installers;

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Roadbed.Data;
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

        // Resolve persistence options + (when persistent) the database factory
        // from DI BEFORE entering the AddQuartz lambda. Roadbed.Scheduling does
        // not read configuration directly — the host is expected to have
        // registered SchedulingPersistenceOptions and (when Mode is Persistent)
        // an ISchedulingDatabaseFactory as singletons. Falling back to a
        // default SchedulingPersistenceOptions preserves the previous
        // out-of-box behavior of using Quartz's in-memory store.
        SchedulingPersistenceOptions persistence;
        ISchedulingDatabaseFactory? schedulingFactory = null;
        using (var setupProvider = services.BuildServiceProvider())
        {
            persistence = setupProvider.GetService<SchedulingPersistenceOptions>()
                          ?? new SchedulingPersistenceOptions();

            if (persistence.Mode == SchedulingPersistenceMode.Persistent)
            {
                schedulingFactory = setupProvider.GetService<ISchedulingDatabaseFactory>()
                    ?? throw new InvalidOperationException(
                        $"SchedulingPersistenceOptions.Mode is " +
                        $"'{nameof(SchedulingPersistenceMode.Persistent)}' but no " +
                        $"{nameof(ISchedulingDatabaseFactory)} is registered. " +
                        $"Register a concrete factory pointing at the Quartz schema " +
                        $"before calling InstallModulesInAppDomain, or switch Mode to " +
                        $"'{nameof(SchedulingPersistenceMode.InMemory)}'.");
            }
        }

        // Configure Quartz.NET
        services.AddQuartz(q =>
        {
            q.SchedulerName = persistence.SchedulerName;

            // Select the job store based on the resolved persistence options.
            switch (persistence.Mode)
            {
                case SchedulingPersistenceMode.InMemory:
                    q.UseInMemoryStore();
                    break;

                case SchedulingPersistenceMode.Persistent:
                    ConfigurePersistentStore(q, schedulingFactory!, persistence);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown {nameof(SchedulingPersistenceMode)} value: " +
                        $"'{persistence.Mode}'.");
            }

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

            // Skip jobs that are disabled via SchedulingJobOptions. The job is entirely
            // absent from Quartz — no job registration, no trigger, not invocable manually.
            if (!job.IsEnabled)
            {
                continue;
            }

            var schedule = job.Schedule;
            var jobKey = new JobKey(job.Name, schedule.GroupName);

            // Configure the job
            configurator.AddJob(jobType!, jobKey, jobConfig =>
            {
                jobConfig.WithDescription(job.Description);

                if (schedule.ScheduleType == SchedulingScheduleType.ManualOnly)
                {
                    jobConfig.StoreDurably();
                }
            });

            // Manual-only jobs have no trigger; they are triggered programmatically
            if (schedule.ScheduleType == SchedulingScheduleType.ManualOnly)
            {
                continue;
            }

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
    /// Configures Quartz's persistent (AdoJobStore) backend by dispatching to
    /// the appropriate provider-specific fluent method based on the connection
    /// factory's <see cref="DataConnectionStringType"/>.
    /// </summary>
    /// <param name="configurator">Quartz service collection configurator.</param>
    /// <param name="factory">Host-registered Quartz database connection factory.</param>
    /// <param name="persistence">Resolved persistence options.</param>
    /// <remarks>
    /// Roadbed.Scheduling does not reference any ADO.NET driver package
    /// (MySqlConnector, Npgsql, Microsoft.Data.Sqlite). Quartz's provider-
    /// specific extension methods (UseMySqlConnector / UsePostgres / UseSQLite)
    /// are all built into the main Quartz package and load the actual driver
    /// assembly reflectively at runtime. The driver must be present in the
    /// host process — typically supplied transitively by the host's reference
    /// to the matching Roadbed.Data.* project.
    /// </remarks>
    private static void ConfigurePersistentStore(
        IServiceCollectionQuartzConfigurator configurator,
        ISchedulingDatabaseFactory factory,
        SchedulingPersistenceOptions persistence)
    {
        var connection = factory.Connecion;
        var connectionString = connection.ConnectionString;
        var connectionType = connection.ConnectionStringType;

        configurator.UsePersistentStore(store =>
        {
            // UseProperties=true forces JobDataMap entries to be strings,
            // sidestepping the need to register a serializer (Quartz's
            // default binary serializer is deprecated; JSON serializers
            // ship in separate NuGet packages we don't want to take on
            // just for this feature).
            store.UseProperties = true;
            store.RetryInterval = TimeSpan.FromSeconds(15);

            // TablePrefix is not a CLR property on PersistentStoreOptions
            // in Quartz 3.18; it has to be set via the raw Quartz property
            // key. This is the same value Quartz's own DDL scripts use.
            store.SetProperty("quartz.jobStore.tablePrefix", persistence.TablePrefix);

            if (persistence.IsClustered)
            {
                store.UseClustering();
            }

            switch (connectionType)
            {
                case DataConnectionStringType.MySQL:
                    store.UseMySqlConnector(c => c.ConnectionString = connectionString);
                    break;

                case DataConnectionStringType.PostgreSQL:
                    store.UsePostgres(c => c.ConnectionString = connectionString);
                    break;

                case DataConnectionStringType.SQLite:
                    store.UseMicrosoftSQLite(c => c.ConnectionString = connectionString);
                    break;

                case DataConnectionStringType.SQLiteInMemory:
                    throw new InvalidOperationException(
                        $"SQLite in-memory cannot back a Quartz persistent store " +
                        $"(state would not survive the connection lifetime). " +
                        $"Use {nameof(SchedulingPersistenceMode)}." +
                        $"{nameof(SchedulingPersistenceMode.InMemory)} instead.");

                default:
                    throw new InvalidOperationException(
                        $"Quartz persistent storage is not supported for " +
                        $"{nameof(DataConnectionStringType)} '{connectionType}'.");
            }
        });
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