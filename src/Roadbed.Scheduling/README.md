# Roadbed.Scheduling

This package provides job scheduling capabilities using Quartz.NET with automatic job discovery and registration.

## Quartz.NET

The `Roadbed.Scheduling` project provides integration with [Quartz.NET](https://www.quartz-scheduler.net/), a full-featured, open source job scheduling system that can be used from smallest apps to large scale enterprise systems.

### NuGet Packages

You need the Quartz.NET packages that provide dependency injection and hosting support. Include the following packages, provided by Quartz.NET, in your project:
```xml
<ItemGroup>
  <PackageReference Include="Quartz" Version="*.*.*" />
  <PackageReference Include="Quartz.Extensions.DependencyInjection" Version="*.*.*" />
  <PackageReference Include="Quartz.Extensions.Hosting" Version="*.*.*" />
</ItemGroup>
```

## Application Start-up

The `Roadbed.Common` project provides an extension method that will discover and invoke all `IServiceCollectionInstaller` implementations in the application domain. This will find the `InstallScheduling` implementation provided by `Roadbed.Scheduling`. The installer will then discover all `ISchedulingJob` implementations and register them with dependency injection and Quartz.NET.
```csharp
var builder = WebApplication.CreateBuilder(args);

// This discovers and invokes ALL IServiceCollectionInstaller implementations,
// including InstallScheduling from Roadbed.Scheduling
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### What Happens Automatically

1. ✅ `InstallModulesInAppDomain()` scans assemblies for `IServiceCollectionInstaller` implementations
2. ✅ Finds `InstallScheduling` in `Roadbed.Scheduling`
3. ✅ Creates an instance and calls `ConfigureServices()`
4. ✅ `InstallScheduling` discovers all `ISchedulingJob` implementations across all loaded assemblies
5. ✅ Registers jobs with dependency injection
6. ✅ Configures Quartz.NET with all discovered jobs
7. ✅ Sets up hosted service to run jobs in background

**Zero manual configuration required!** Your jobs are automatically discovered and scheduled.

---

## Creating Scheduled Jobs

There are two ways to create scheduled jobs: **hardcoded schedules** (via constructor) and **configuration-driven schedules** (via property overrides).

### Method 1: Hardcoded Schedule (Constructor Pattern)

Use this approach when your schedule is fixed and part of the job's definition.
```csharp
namespace MyApp.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;

/// <summary>
/// Example scheduled job with hardcoded schedule.
/// </summary>
public class FooJob : BaseSchedulingJob<FooJob>
{
    #region Private Fields

    private readonly IFooService _fooService;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="fooService">Service for foo operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or fooService is null.</exception>
    public FooJob(
        ILogger<FooJob> logger,
        IFooService fooService)
        : base(
            name: "FooJob",
            description: "Processes foo items every 30 minutes",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(30))
            {
                GroupName = "MyApp",
                Priority = SchedulingJobPriority.Normal,
            },
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(fooService);
        this._fooService = fooService;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogInformation("Starting foo processing");

        try
        {
            await this._fooService.ProcessFooItemsAsync(cancellationToken);
            this.LogInformation("Foo processing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error processing foo items");
            throw;
        }
    }

    #endregion Public Methods
}
```

### Method 2: Configuration-Driven Schedule (Property Override Pattern)

Use this approach when you want to configure schedules through `appsettings.json` without recompiling.
```csharp
namespace MyApp.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;

/// <summary>
/// Example scheduled job with configuration-driven schedule.
/// </summary>
public class FooJob : BaseSchedulingJob<FooJob>
{
    #region Private Fields

    private readonly IConfiguration _configuration;
    private readonly IFooService _fooService;
    private SchedulingSchedule? _schedule;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="fooService">Service for foo operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger, configuration, or fooService is null.</exception>
    public FooJob(
        ILogger<FooJob> logger,
        IConfiguration configuration,
        IFooService fooService)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(fooService);

        this._configuration = configuration;
        this._fooService = fooService;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public override string Description => "Processes foo items at configurable intervals";

    /// <inheritdoc/>
    public override string Name => "FooJob";

    /// <inheritdoc/>
    public override SchedulingSchedule Schedule
    {
        get
        {
            if (this._schedule != null)
            {
                return this._schedule;
            }

            // Read from configuration with defaults
            var intervalMinutes = this._configuration.GetValue<int>("Jobs:FooJob:IntervalMinutes", 30);
            var startDelaySeconds = this._configuration.GetValue<int>("Jobs:FooJob:StartDelaySeconds", 0);
            var priority = this._configuration.GetValue<SchedulingJobPriority>("Jobs:FooJob:Priority", SchedulingJobPriority.Normal);

            this._schedule = new SchedulingSchedule(
                TimeSpan.FromMinutes(intervalMinutes),
                TimeSpan.FromSeconds(startDelaySeconds))
            {
                GroupName = "MyApp",
                Priority = priority
            };

            return this._schedule;
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogInformation("Starting foo processing");

        try
        {
            await this._fooService.ProcessFooItemsAsync(cancellationToken);
            this.LogInformation("Foo processing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error processing foo items");
            throw;
        }
    }

    #endregion Public Methods
}
```

### Configuration File (appsettings.json)

When using the configuration-driven approach, add settings to your `appsettings.json`:
```json
{
  "Jobs": {
    "FooJob": {
      "IntervalMinutes": 30,
      "StartDelaySeconds": 0,
      "Priority": "Normal"
    }
  }
}
```

---

## Schedule Types

`SchedulingSchedule` supports multiple schedule types:

### 1. Cron Expression
```csharp
// Run every day at 2:30 AM
new SchedulingSchedule("0 30 2 * * ?")
{
    TimeZone = TimeZoneInfo.Local
}
```

### 2. Simple Interval
```csharp
// Run every 15 minutes, starting immediately
new SchedulingSchedule(TimeSpan.FromMinutes(15))

// Run every hour, starting after 10 minutes
new SchedulingSchedule(TimeSpan.FromHours(1), TimeSpan.FromMinutes(10))
```

### 3. Specific Time (One-Time)
```csharp
// Run once at a specific date/time
new SchedulingSchedule(new DateTime(2026, 1, 20, 14, 30, 0))
```

### 4. Specific Time with Interval
```csharp
// Run every 30 minutes starting at a specific time
new SchedulingSchedule(
    new DateTime(2026, 1, 20, 14, 30, 0),
    TimeSpan.FromMinutes(30))
```

---

## Schedule Configuration Properties

All schedule types support these configuration properties:
```csharp
new SchedulingSchedule("0 0 8 * * ?")
{
    TimeZone = TimeZoneInfo.Local,                    // Default: UTC
    GroupName = "MyApp",                               // Default: "Default"
    Priority = SchedulingJobPriority.High,             // Default: Normal
    MaxExecutionCount = 100,                           // Default: null (infinite)
    MisfireHandlingEnabled = true,                     // Default: true
    MisfireStrategy = SchedulingMisfireStrategy.Default // Default: Default
}
```

### Job Priorities

Available priority levels:

- `SchedulingJobPriority.Lowest` (0)
- `SchedulingJobPriority.VeryLow` (2)
- `SchedulingJobPriority.Low` (4)
- `SchedulingJobPriority.Normal` (5) - **Default**
- `SchedulingJobPriority.High` (7)
- `SchedulingJobPriority.VeryHigh` (9)
- `SchedulingJobPriority.Highest` (10)

---

## Built-in Jobs

`Roadbed.Scheduling` includes two built-in monitoring jobs:

### 1. SchedulingStartupJobsSummaryJob

Logs a summary of all scheduled jobs **30 seconds after application startup**.

- **Group**: System
- **Priority**: Lowest
- **Runs**: Once at startup

### 2. SchedulingScheduledJobsSummaryJob

Logs a daily summary of all scheduled jobs.

- **Group**: System
- **Priority**: Lowest
- **Runs**: Daily at 8:00 AM (local time)

Both jobs help you verify that all jobs are registered and scheduled correctly.

---

## Complete Example

Here's a complete example showing both patterns in one application:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.InstallModulesInAppDomain(builder.Configuration);
var app = builder.Build();
app.Run();
```
```csharp
// HardcodedJob.cs - Simple, fixed schedule
public class DailyReportJob : BaseSchedulingJob<DailyReportJob>
{
    public DailyReportJob(ILogger<DailyReportJob> logger, IReportService service)
        : base(
            name: "DailyReport",
            description: "Generates daily reports at 6 AM",
            schedule: new SchedulingSchedule("0 0 6 * * ?"),
            logger: logger)
    {
        this._service = service;
    }

    public override async Task ExecuteAsync(CancellationToken ct)
    {
        await this._service.GenerateReportAsync(ct);
    }
}
```
```csharp
// ConfigurableJob.cs - Flexible, configuration-driven
public class DataSyncJob : BaseSchedulingJob<DataSyncJob>
{
    public DataSyncJob(ILogger<DataSyncJob> logger, IConfiguration config, ISyncService service)
        : base(logger)
    {
        this._config = config;
        this._service = service;
    }

    public override string Name => "DataSync";
    public override string Description => "Syncs data at configurable intervals";
    
    public override SchedulingSchedule Schedule
    {
        get
        {
            var minutes = this._config.GetValue<int>("Jobs:DataSync:IntervalMinutes", 60);
            return new SchedulingSchedule(TimeSpan.FromMinutes(minutes));
        }
    }

    public override async Task ExecuteAsync(CancellationToken ct)
    {
        await this._service.SyncDataAsync(ct);
    }
}
```
```json
// appsettings.json
{
  "Jobs": {
    "DataSync": {
      "IntervalMinutes": 60,
      "Priority": "High"
    }
  }
}
```

