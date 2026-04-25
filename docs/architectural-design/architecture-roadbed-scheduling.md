# Roadbed.Scheduling Architecture

Roadbed.Scheduling provides a thin, opinionated wrapper around [Quartz.NET](https://www.quartz-scheduler.net/) for scheduling jobs in .NET hosted applications. It auto-discovers `ISchedulingJob` implementations from loaded assemblies, configures Quartz with sensible defaults, and exposes a small surface area for jobs to declare their schedule, group, priority, and misfire behavior.

The library also includes a metrics abstraction (`ISchedulingMetrics`) with a built-in logging adapter, and an opt-in `SchedulingJobOptions` POCO for gating jobs and overriding cron expressions per deployment without recompiling.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Scheduling NuGet package. When a developer asks you to add a recurring or scheduled background task to a .NET application, use this document to define the job, pick the right schedule type, and wire up DI.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Inherit from `BaseSchedulingJob<T>`** — never implement `ISchedulingJob` directly. The base class implements `Quartz.IJob` for you and manages the `Context` lifecycle.
5. **Pick one definition pattern per job:** constructor-supplied (name/description/schedule passed to `base(...)`) or property-overridden (`override string Name`, `override SchedulingSchedule Schedule`, etc.). Do not mix them on the same job.
6. **Do not register jobs manually** in DI. `InstallScheduling.ConfigureServices` discovers every concrete `ISchedulingJob` in loaded assemblies and registers it as transient.
7. **Job constructors run during DI configuration**, not at job-fire time. Constructors must only depend on services registered before `services.InstallModulesInAppDomain(configuration)` runs. For runtime-only services (e.g., `IScheduler`, `ISchedulerFactory`), inject `IServiceProvider` and resolve them in `ExecuteAsync`.
8. **`CancellationToken` is always the last parameter** with `= default` on any helper method.
9. **`ExecuteAsync` is the only method jobs implement.** The `Quartz.IJob.Execute` method is implemented explicitly by `BaseSchedulingJob<T>` — do not override it.
10. **Use `this.LogDebug()`, `this.LogInformation()`, etc.** instead of `this.Logger.LogDebug()`. The base methods check `IsEnabled()` before formatting.
11. **Set `this.Context.Result`** at the end of `ExecuteAsync` to give the metrics adapter a one-line execution summary (e.g., `"Processed 1,234 records"`).
12. **For configuration-gated jobs, use `SchedulingJobOptions`** — never read `IConfiguration` from the framework. The application owns mapping its own appSettings into the POCO and registering it as a singleton.
13. **Job `Name` is the lookup key** into `SchedulingJobOptions.Features`. Match the `const string` exactly — the framework does not normalize casing.
14. **Disabled jobs (`SchedulingJobFeature.Enabled = false`) are skipped entirely** — no `AddJob`, no `AddTrigger`, not invocable manually.
15. **`ISchedulingMetrics` implementations must be thread-safe** and must not throw. The metrics listener wraps every call in a try/catch that logs and swallows.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-scheduling.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-scheduling.md#type-catalog)
3. [Package Relationship](architecture-roadbed-scheduling.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-scheduling.md#namespace-convention)
5. [ISchedulingJob and BaseSchedulingJob\<T\>](architecture-roadbed-scheduling.md#ischedulingjob-and-baseschedulingjobt)
    - [Definition Patterns](architecture-roadbed-scheduling.md#definition-patterns)
    - [Constructor Matrix](architecture-roadbed-scheduling.md#constructor-matrix)
    - [Context During Execution](architecture-roadbed-scheduling.md#context-during-execution)
6. [SchedulingSchedule](architecture-roadbed-scheduling.md#schedulingschedule)
    - [Schedule Types](architecture-roadbed-scheduling.md#schedule-types)
    - [Configuration Properties](architecture-roadbed-scheduling.md#configuration-properties)
7. [SchedulingJobPriority and SchedulingMisfireStrategy](architecture-roadbed-scheduling.md#schedulingjobpriority-and-schedulingmisfirestrategy)
8. [Configuration-Gated Jobs (SchedulingJobOptions)](architecture-roadbed-scheduling.md#configuration-gated-jobs-schedulingjoboptions)
    - [Resolution Matrix](architecture-roadbed-scheduling.md#resolution-matrix)
    - [Lenient vs Strict Constructor Overloads](architecture-roadbed-scheduling.md#lenient-vs-strict-constructor-overloads)
    - [The Arguments Bag](architecture-roadbed-scheduling.md#the-arguments-bag)
9. [Metrics (ISchedulingMetrics)](architecture-roadbed-scheduling.md#metrics-ischedulingmetrics)
    - [JobExecutionInfo](architecture-roadbed-scheduling.md#jobexecutioninfo)
    - [LoggingMetricsAdapter](architecture-roadbed-scheduling.md#loggingmetricsadapter)
    - [Custom Adapters](architecture-roadbed-scheduling.md#custom-adapters)
10. [Module Auto-Discovery](architecture-roadbed-scheduling.md#module-auto-discovery)
11. [Built-in System Jobs](architecture-roadbed-scheduling.md#built-in-system-jobs)
12. [Implementation Walkthrough](architecture-roadbed-scheduling.md#implementation-walkthrough)
13. [Common Pitfalls](architecture-roadbed-scheduling.md#common-pitfalls)
14. [Quick Reference](architecture-roadbed-scheduling.md#quick-reference)

---

## Type Catalog

Roadbed.Scheduling contains **12 public types** organized into six groups.

### Job Contract (2 types)

| Type                   | Kind                   | Namespace            | Purpose                                                                                 |
| ---------------------- | ---------------------- | -------------------- | --------------------------------------------------------------------------------------- |
| `ISchedulingJob`       | Interface              | `Roadbed.Scheduling` | Contract for a discoverable scheduled job. Extends `Quartz.IJob`.                       |
| `BaseSchedulingJob<T>` | Abstract generic class | `Roadbed.Scheduling` | Base class for jobs. Implements `IJob.Execute`, manages `Context`, exposes `IsEnabled`. |

### Schedule Configuration (3 types)

| Type                     | Kind  | Namespace            | Purpose                                                                                 |
| ------------------------ | ----- | -------------------- | --------------------------------------------------------------------------------------- |
| `SchedulingSchedule`     | Class | `Roadbed.Scheduling` | Schedule shape: cron, interval, specific time, or manual-only.                          |
| `SchedulingScheduleType` | Enum  | `Roadbed.Scheduling` | `Cron`, `SimpleInterval`, `SpecificTimeOnce`, `SpecificTimeWithInterval`, `ManualOnly`. |
| `SchedulingJobPriority`  | Enum  | `Roadbed.Scheduling` | Priority levels mapped to Quartz integer priorities (Lowest=0 ... Highest=10).          |

### Misfire Handling (1 type)

| Type                        | Kind | Namespace            | Purpose                                                               |
| --------------------------- | ---- | -------------------- | --------------------------------------------------------------------- |
| `SchedulingMisfireStrategy` | Enum | `Roadbed.Scheduling` | Cron and simple-interval misfire instructions. Defaults to `Default`. |

### Options Pattern (2 types)

| Type                   | Kind  | Namespace            | Purpose                                                                        |
| ---------------------- | ----- | -------------------- | ------------------------------------------------------------------------------ |
| `SchedulingJobOptions` | Class | `Roadbed.Scheduling` | Application-supplied POCO; keys jobs by `Name`. Registered as singleton in DI. |
| `SchedulingJobFeature` | Class | `Roadbed.Scheduling` | One entry: `Enabled`, `CronExpression`, free-form `Arguments` dictionary.      |

### Metrics (4 types)

| Type                      | Kind                  | Namespace            | Purpose                                                                           |
| ------------------------- | --------------------- | -------------------- | --------------------------------------------------------------------------------- |
| `ISchedulingMetrics`      | Interface             | `Roadbed.Scheduling` | Contract for receiving job lifecycle events. Implementations must be thread-safe. |
| `JobExecutionInfo`        | Sealed record         | `Roadbed.Scheduling` | Immutable per-execution data passed to metrics adapters.                          |
| `LoggingMetricsAdapter`   | Sealed class          | `Roadbed.Scheduling` | Default adapter that writes lifecycle events to `ILogger`.                        |
| (`NullSchedulingMetrics`) | Internal sealed class | `Roadbed.Scheduling` | Zero-overhead default registered when no `ISchedulingMetrics` is provided.        |

### Module Auto-Discovery (1 type)

| Type                | Kind  | Namespace                       | Purpose                                                                       |
| ------------------- | ----- | ------------------------------- | ----------------------------------------------------------------------------- |
| `InstallScheduling` | Class | `Roadbed.Scheduling.Installers` | Auto-discovered installer. Configures Quartz, registers all jobs and metrics. |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Hosted application                                           │
│                                                              │
│   Defines ISchedulingJob implementations                     │
│   Optionally registers SchedulingJobOptions singleton        │
│   Optionally registers ISchedulingMetrics implementation     │
│   Calls services.InstallModulesInAppDomain(configuration)    │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Scheduling                                           │
│                                                              │
│   ISchedulingJob              BaseSchedulingJob<T>           │
│   SchedulingSchedule          SchedulingScheduleType         │
│   SchedulingJobPriority       SchedulingMisfireStrategy      │
│   SchedulingJobOptions        SchedulingJobFeature           │
│   ISchedulingMetrics          JobExecutionInfo               │
│   LoggingMetricsAdapter       (NullSchedulingMetrics)        │
│   InstallScheduling           SchedulingMetricsListener      │
└──────────┬───────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Common                                               │
│                                                              │
│   BaseClassWithLoggingFactory<T>                             │
│   IServiceCollectionInstaller                                │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ External Dependencies                                        │
│                                                              │
│   Quartz                          (3.16.x)                   │
│   Quartz.Extensions.DependencyInjection                      │
│   Quartz.Extensions.Hosting       (background service)       │
│   Microsoft.Extensions.Logging                               │
└──────────────────────────────────────────────────────────────┘
```

---

## Namespace Convention

| Namespace                       | Contains                              |
| ------------------------------- | ------------------------------------- |
| `Roadbed.Scheduling`            | All public types except the installer |
| `Roadbed.Scheduling.Installers` | `InstallScheduling`                   |

Original sub-namespaces (`Adapters`, `Dtos`, `Enumerators`, `Services`) were removed on purpose. Consuming code only needs `using Roadbed.Scheduling;` plus, in rare cases, `using Roadbed.Scheduling.Installers;`.

---

## ISchedulingJob and BaseSchedulingJob\<T\>

```csharp
namespace Roadbed.Scheduling;

using Quartz;

public interface ISchedulingJob : IJob
{
    string Description { get; }
    string Name { get; }
    SchedulingSchedule Schedule { get; }
    bool IsEnabled => true;     // default interface member

    Task ExecuteAsync(CancellationToken cancellationToken);
}
```

`ISchedulingJob` extends `Quartz.IJob`. `BaseSchedulingJob<T>` implements `IJob.Execute` explicitly so it can wrap user code with context management, then calls into the user's `ExecuteAsync`.

```csharp
public abstract class BaseSchedulingJob<T>
    : BaseClassWithLoggingFactory<T>, ISchedulingJob
{
    // Constructors — see "Constructor Matrix" below

    public virtual string Name { get; }
    public virtual string Description { get; }
    public virtual SchedulingSchedule Schedule { get; }
    public virtual bool IsEnabled { get; }

    protected IJobExecutionContext Context { get; }   // available only inside ExecuteAsync

    public abstract Task ExecuteAsync(CancellationToken cancellationToken);
}
```

### Definition Patterns

There are two ways to define `Name`/`Description`/`Schedule`:

**Pattern 1 — Constructor-supplied:** pass values to `base(...)`. Subclass does not override the properties.

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public FooJob(ILogger<FooJob> logger)
        : base(
            name: "FooJob",
            description: "Processes foos every 30 minutes",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(30)),
            logger: logger)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

**Pattern 2 — Property-overridden:** call the logger-only `base(logger)` constructor and override the three properties. Use this when a property must be computed from injected services.

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    private readonly IFooConfig _config;

    public FooJob(ILogger<FooJob> logger, IFooConfig config)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        this._config = config;
    }

    public override string Name => "FooJob";
    public override string Description => "Processes foos at a configured interval";
    public override SchedulingSchedule Schedule =>
        new SchedulingSchedule(this._config.IntervalMinutes);

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

**Do not mix the two patterns** on the same job. The base class will throw `InvalidOperationException` from a property if it was never set or overridden.

### Constructor Matrix

| Constructor                                                                                                           | Use case                                             | Resolves                                                                                           |
| --------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `(ILogger logger)`                                                                                                    | Property-override pattern.                           | Subclass must override `Name`, `Description`, `Schedule`.                                          |
| `(string name, string description, SchedulingSchedule schedule, ILogger logger)`                                      | Constructor-supplied pattern with explicit schedule. | All three properties resolve to constructor values.                                                |
| `(string name, string description, ILogger logger)`                                                                   | Manual-only job (no automatic schedule).             | Schedule resolves to `new SchedulingSchedule()` (`ScheduleType = ManualOnly`).                     |
| `(string name, string description, SchedulingSchedule defaultSchedule, SchedulingJobOptions options, ILogger logger)` | Lenient options-gated job.                           | Resolves `IsEnabled` and `Schedule` from the options entry, with the supplied default as fallback. |
| `(string name, string description, SchedulingJobOptions options, ILogger logger)`                                     | Strict options-gated job (no fallback).              | Resolves from the options entry; throws `InvalidOperationException` if entry or cron is missing.   |

### Context During Execution

Inside `ExecuteAsync`, the protected `Context` property exposes the live `Quartz.IJobExecutionContext`:

```csharp
public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    int processed = await this.ProcessAsync(cancellationToken);
    this.Context.Result = $"Processed {processed} foos";
}
```

`Context` throws `InvalidOperationException` if accessed outside `ExecuteAsync`. The base class clears it after `ExecuteAsync` returns (in a `finally` block) to make the misuse fail loudly.

`Context.Result` is read by the metrics listener and surfaced in `JobExecutionInfo.ResultMessage` (so it appears in `LoggingMetricsAdapter` and any custom `ISchedulingMetrics` implementation).

---

## SchedulingSchedule

`SchedulingSchedule` is the schedule shape passed to the constructor or returned from the override. It has four meaningful constructors plus a parameterless constructor for manual-only jobs.

### Schedule Types

| Constructor                                                          | Resulting `ScheduleType`   | Quartz trigger                                                           |
| -------------------------------------------------------------------- | -------------------------- | ------------------------------------------------------------------------ |
| `SchedulingSchedule()`                                               | `ManualOnly`               | None — job is registered as durable, must be triggered programmatically. |
| `SchedulingSchedule(string cronExpression)`                          | `Cron`                     | `WithCronSchedule(cronExpression)` in the configured `TimeZone`.         |
| `SchedulingSchedule(TimeSpan interval, TimeSpan? startDelay = null)` | `SimpleInterval`           | `WithSimpleSchedule(...)` `RepeatForever`, optional `WithRepeatCount`.   |
| `SchedulingSchedule(DateTime startAt)`                               | `SpecificTimeOnce`         | `StartAt(startAt)` with no repeat.                                       |
| `SchedulingSchedule(DateTime startAt, TimeSpan interval)`            | `SpecificTimeWithInterval` | `StartAt(startAt)` then repeat at the given interval.                    |

**Validation:**

- `cronExpression` must be non-blank.
- `interval` must be greater than `TimeSpan.Zero`.
- `startDelay`, when supplied, must be non-negative.

### Configuration Properties

| Property                 | Default                                     | Notes                                                                         |
| ------------------------ | ------------------------------------------- | ----------------------------------------------------------------------------- |
| `ScheduleType`           | Set by constructor                          | Read-only after construction.                                                 |
| `CronExpression`         | `null` unless cron constructor was used     | Read-only.                                                                    |
| `Interval`               | `null` unless interval constructor was used | Read-only.                                                                    |
| `StartDelay`             | `null` unless `SimpleInterval`              | Read-only.                                                                    |
| `StartAt`                | `null` unless `SpecificTime*`               | Read-only.                                                                    |
| `MaxExecutionCount`      | `null` (infinite)                           | Mutable. Caps `RepeatForever` schedules. Ignored for `Cron` and `ManualOnly`. |
| `Priority`               | `Normal`                                    | Mutable. Maps to Quartz trigger priority via `(int)SchedulingJobPriority`.    |
| `TimeZone`               | `TimeZoneInfo.Utc`                          | Mutable. Important for cron expressions and specific start times.             |
| `GroupName`              | `"Default"`                                 | `init` only. Becomes the Quartz `JobKey.Group` and `TriggerKey.Group`.        |
| `MisfireHandlingEnabled` | `true`                                      | Mutable.                                                                      |
| `MisfireStrategy`        | `Default`                                   | Mutable.                                                                      |

---

## SchedulingJobPriority and SchedulingMisfireStrategy

```csharp
public enum SchedulingJobPriority
{
    Lowest = 0, VeryLow = 2, Low = 4, Normal = 5, High = 7, VeryHigh = 9, Highest = 10
}

public enum SchedulingMisfireStrategy
{
    Default = 0,
    DoNothing = 1,                  // cron only
    FireAndProceed = 2,             // cron only
    IgnoreMisfires = 3,             // both
    FireNow = 4,                    // simple only
    NextWithExistingCount = 5,      // simple only
    NextWithRemainingCount = 6,     // simple only
    NowWithExistingCount = 7,       // simple only
    NowWithRemainingCount = 8       // simple only
}
```

**Misfire applicability:** `InstallScheduling` calls `ApplyCronMisfireStrategy` for cron triggers and `ApplySimpleMisfireStrategy` for simple-interval triggers. Choosing a cron-only strategy on a simple-interval schedule (or vice versa) is silently ignored.

---

## Configuration-Gated Jobs (SchedulingJobOptions)

Roadbed.Scheduling **never reads `IConfiguration` directly.** When you want to disable a job per environment or override its cron expression per security zone, the application populates a `SchedulingJobOptions` POCO from its own appSettings and registers it as a singleton.

```csharp
public sealed class SchedulingJobOptions
{
    public IReadOnlyDictionary<string, SchedulingJobFeature> Features { get; init; }
        = new Dictionary<string, SchedulingJobFeature>();
}

public sealed class SchedulingJobFeature
{
    public bool Enabled { get; init; } = true;
    public string? CronExpression { get; init; }
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }
}
```

The lookup key into `Features` is the job's `Name`. Missing entries default to enabled with the job's hardcoded schedule.

### Resolution Matrix

| Situation                                      | Lenient overload (with `defaultSchedule`) | Strict overload (no default)              |
| ---------------------------------------------- | ----------------------------------------- | ----------------------------------------- |
| Entry missing from `Features`                  | Use `defaultSchedule`, `IsEnabled = true` | Throws `InvalidOperationException`        |
| Entry with `Enabled = false`                   | `IsEnabled = false` — skipped from Quartz | `IsEnabled = false` — skipped from Quartz |
| Entry with `Enabled = true`, no cron           | Use `defaultSchedule`                     | Throws `InvalidOperationException`        |
| Entry with `Enabled = true` + `CronExpression` | Use the supplied cron                     | Use the supplied cron                     |

### Lenient vs Strict Constructor Overloads

```csharp
// Lenient — fallback to the hardcoded default if no options entry.
public FooJob(SchedulingJobOptions options, ILogger<FooJob> logger)
    : base(
        name: "FooJob",
        description: "Processes foos.",
        defaultSchedule: new SchedulingSchedule(TimeSpan.FromMinutes(30)),
        options: options,
        logger: logger)
{
}

// Strict — schedule MUST come from options. Forgetting to configure a zone fails fast.
public ZonedJob(SchedulingJobOptions options, ILogger<ZonedJob> logger)
    : base(
        name: "ZonedJob",
        description: "Zone-specific job; no universal default.",
        options: options,
        logger: logger)
{
}
```

Pick lenient when there is a sensible universal default. Pick strict when forgetting to configure a deployment should be a startup error rather than a silent fallback.

### The Arguments Bag

`SchedulingJobFeature.Arguments` is a `Dictionary<string, string>` that the framework never reads. Jobs pull whichever keys they care about:

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    private readonly string _zone;

    public FooJob(SchedulingJobOptions options, ILogger<FooJob> logger)
        : base(
            name: "FooJob",
            description: "Processes foos.",
            defaultSchedule: new SchedulingSchedule(TimeSpan.FromMinutes(30)),
            options: options,
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        var feature = options.Features.GetValueOrDefault("FooJob");
        this._zone = feature?.Arguments?.GetValueOrDefault("zone") ?? "default";
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Only string values are supported. Jobs parse to other primitives themselves.

---

## Metrics (ISchedulingMetrics)

```csharp
public interface ISchedulingMetrics
{
    void JobStarted(JobExecutionInfo info);
    void JobCompleted(JobExecutionInfo info, TimeSpan duration);
    void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration);
    void JobMisfired(JobExecutionInfo info);
}
```

### JobExecutionInfo

Immutable record passed to every metrics method:

```csharp
public sealed record JobExecutionInfo
{
    required public string FireInstanceId { get; init; }
    public DateTimeOffset? FireTimeUtc { get; init; }
    required public string JobGroup { get; init; }
    required public string JobName { get; init; }
    public DateTimeOffset? NextFireTimeUtc { get; init; }
    public DateTimeOffset? PreviousFireTimeUtc { get; init; }
    public string? ResultMessage { get; init; }
    public DateTimeOffset? ScheduledFireTimeUtc { get; init; }
    required public string TriggerGroup { get; init; }
    required public string TriggerName { get; init; }
}
```

`ResultMessage` is populated from `Context.Result` after the job completes. For `JobStarted`, it is always `null`.

### LoggingMetricsAdapter

Default adapter that writes lifecycle events to `ILogger<LoggingMetricsAdapter>`. Use when you want metrics in logs without adding any dependency.

```csharp
builder.Services.AddSingleton<ISchedulingMetrics, LoggingMetricsAdapter>();
```

If `JobExecutionInfo.ResultMessage` is non-blank, it appears in the log line:

```
Job FooJob (Default) completed in 1234.5ms - Processed 567 records
```

### Custom Adapters

Implement `ISchedulingMetrics` to forward to Datadog, Application Insights, OpenTelemetry, etc.

**Implementation requirements:**

- **Thread-safe.** Multiple jobs may complete concurrently.
- **Never throw.** The metrics listener wraps every call in a try/catch that logs at Debug and swallows. Throwing only adds noise; it does not surface the metrics failure to operators.
- **Fast.** Methods are called synchronously from the Quartz job pipeline. Queue work for async processing rather than awaiting external systems inline.

If no `ISchedulingMetrics` is registered, `InstallScheduling` registers `NullSchedulingMetrics.Instance` as a singleton — zero overhead, zero allocations.

---

## Module Auto-Discovery

```csharp
// In Program.cs / startup
builder.Services.InstallModulesInAppDomain(builder.Configuration);
```

This single call discovers `InstallScheduling` (alongside every other Roadbed module installer). `InstallScheduling.ConfigureServices` then:

1. Registers `NullSchedulingMetrics.Instance` if no `ISchedulingMetrics` was previously registered.
2. Registers the internal `SchedulingMetricsListener` as a singleton.
3. Scans loaded assemblies (excluding `System.*` and `Microsoft.*`) for non-abstract, non-interface types that implement `ISchedulingJob` and have at least one parameterized constructor. Each is registered twice: once by its concrete type (`AddTransient(jobType)`) and once as `ISchedulingJob` (for discovery).
4. Calls `services.AddQuartz(...)` to:
   - Enable in-memory job store.
   - Add `SchedulingMetricsListener` as a job listener.
   - Build a temporary `ServiceProvider`, resolve every discovered job, read its `Schedule`, and register its `JobKey` and trigger with Quartz (skipping disabled jobs).
5. Adds `services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true)`.
6. Updates `ServiceLocator` with the latest provider snapshot.

**Implication:** Job constructors run during step 4 — at DI configuration time, before the host is built. They must only depend on services registered before `InstallScheduling` runs. For runtime-only dependencies (e.g., `IScheduler`), inject `IServiceProvider` and resolve them inside `ExecuteAsync`.

---

## Built-in System Jobs

Two `ISchedulingJob` implementations ship with Roadbed.Scheduling and are auto-registered:

| Job                                 | Schedule                         | Group    | Priority | Purpose                                         |
| ----------------------------------- | -------------------------------- | -------- | -------- | ----------------------------------------------- |
| `SchedulingStartupJobsSummaryJob`   | One-time, 30 seconds after start | `System` | `Lowest` | Logs a summary of every actively scheduled job. |
| `SchedulingScheduledJobsSummaryJob` | Daily at 8:00 AM local time      | `System` | `Lowest` | Logs the same summary on a recurring basis.     |

Both jobs are useful to verify (in logs) that auto-discovery picked up everything you expected.

---

## Implementation Walkthrough

This walkthrough builds an end-to-end Foo job with metrics, options-gating, and DI wiring.

### Step 1: Pick a definition pattern and define the job

Lenient options-gated example:

```csharp
namespace MyApp.Foo;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;

public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public const string JobName = "FooJob";

    private readonly IFooService _service;

    public FooJob(
        ILogger<FooJob> logger,
        SchedulingJobOptions options,
        IFooService service)
        : base(
            name: JobName,
            description: "Processes pending foos.",
            defaultSchedule: new SchedulingSchedule(TimeSpan.FromMinutes(30))
            {
                GroupName = "MyApp",
                Priority = SchedulingJobPriority.Normal,
            },
            options: options,
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        this._service = service;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogDebug("FooJob starting");

        try
        {
            int processed = await this._service.ProcessPendingAsync(cancellationToken);

            this.Context.Result = $"Processed {processed} foos";

            this.LogInformation("FooJob completed: {Processed}", processed);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "FooJob failed");
            throw;
        }
    }
}
```

### Step 2: Populate `SchedulingJobOptions` from the application's own configuration

```csharp
namespace MyApp;

using Microsoft.Extensions.Configuration;
using Roadbed.Scheduling;

public static class FooJobOptionsBuilder
{
    public static SchedulingJobOptions Build(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new SchedulingJobOptions
        {
            Features = new Dictionary<string, SchedulingJobFeature>
            {
                [FooJob.JobName] = new SchedulingJobFeature
                {
                    Enabled = configuration.GetValue<bool>("Jobs:Foo:Enabled", true),
                    CronExpression = configuration["Jobs:Foo:Cron"],
                    Arguments = new Dictionary<string, string>
                    {
                        ["zone"] = configuration["SecurityZone"] ?? "default",
                    },
                },
            },
        };
    }
}
```

### Step 3: Wire it up in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Application owns the appSettings → POCO mapping
builder.Services.AddSingleton(FooJobOptionsBuilder.Build(builder.Configuration));

// Optional: turn on metrics-to-logs
builder.Services.AddSingleton<ISchedulingMetrics, LoggingMetricsAdapter>();

// Auto-discover and register every Roadbed module installer (including InstallScheduling)
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### Step 4: Configure `appsettings.json` per environment

```json
{
  "SecurityZone": "public",
  "Jobs": {
    "Foo": {
      "Enabled": true,
      "Cron": "0 */5 * * * ?"
    }
  }
}
```

To **disable** the job in a specific deployment, set `"Enabled": false`. To run it on a different cadence, change `"Cron"`. Restart the host for changes to take effect — gating is evaluated once at startup.

---

## Common Pitfalls

### 1. Implementing `ISchedulingJob` Directly

```csharp
// ❌ Wrong — bypasses BaseSchedulingJob's IJob.Execute → ExecuteAsync bridge
public sealed class FooJob : ISchedulingJob
{
    public string Name => "FooJob";
    // ... must also implement Quartz.IJob.Execute, manage Context, etc.
}

// ✅ Correct — inherit from BaseSchedulingJob
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public FooJob(ILogger<FooJob> logger)
        : base("FooJob", "...", new SchedulingSchedule(TimeSpan.FromMinutes(30)), logger)
    { }

    public override Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### 2. Mixing Constructor and Property Patterns

```csharp
// ❌ Wrong — constructor sets schedule, property override also returns one; the override wins,
// confusing future readers and breaking expectations.
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public FooJob(ILogger<FooJob> logger)
        : base("FooJob", "Foo", new SchedulingSchedule(TimeSpan.FromMinutes(5)), logger)
    { }

    public override SchedulingSchedule Schedule => new SchedulingSchedule(TimeSpan.FromMinutes(15));
}

// ✅ Correct — pick one pattern
```

### 3. Resolving `IScheduler` in the Constructor

```csharp
// ❌ Wrong — IScheduler isn't available until the host starts; this throws during InstallScheduling
public FooJob(IScheduler scheduler, ILogger<FooJob> logger)
    : base("FooJob", "Foo", new SchedulingSchedule(TimeSpan.FromMinutes(5)), logger)
{
    this._scheduler = scheduler;
}

// ✅ Correct — inject IServiceProvider, resolve at execution time
private readonly IServiceProvider _services;

public FooJob(IServiceProvider services, ILogger<FooJob> logger)
    : base("FooJob", "Foo", new SchedulingSchedule(TimeSpan.FromMinutes(5)), logger)
{
    ArgumentNullException.ThrowIfNull(services);
    this._services = services;
}

public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    var schedulerFactory = this._services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
    // ...
}
```

### 4. Overriding `Quartz.IJob.Execute`

```csharp
// ❌ Wrong — bypasses BaseSchedulingJob's Context lifecycle
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    Task Quartz.IJob.Execute(IJobExecutionContext context)
    {
        // ...
    }
}

// ✅ Correct — implement ExecuteAsync only
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### 5. Accessing `Context` Outside `ExecuteAsync`

```csharp
// ❌ Wrong — Context is null outside ExecuteAsync; throws InvalidOperationException
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public string Trace => this.Context.FireInstanceId;  // throws
}

// ✅ Correct — set Context.Result inside ExecuteAsync only
public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    this.Context.Result = "OK";
}
```

### 6. Reading `IConfiguration` from Inside the Job for Gating

```csharp
// ❌ Wrong — couples the job to the application's configuration shape;
// Roadbed.Scheduling's design separates these concerns.
public FooJob(IConfiguration configuration, ILogger<FooJob> logger)
    : base("FooJob", "Foo", BuildSchedule(configuration), logger)
{
    if (!configuration.GetValue<bool>("Jobs:Foo:Enabled", true))
    {
        // No way to skip from here; the job is already registered.
    }
}

// ✅ Correct — accept SchedulingJobOptions, let the application populate it from IConfiguration
public FooJob(SchedulingJobOptions options, ILogger<FooJob> logger)
    : base("FooJob", "Foo", new SchedulingSchedule(TimeSpan.FromMinutes(30)), options, logger)
{
}
```

### 7. Forgetting to Set `MessageTypeCodename` — Wrong Library

This pitfall is for `Roadbed.Messaging`. For `Roadbed.Scheduling`, the analogous mistake is forgetting to give the job a unique `Name`. Two jobs with the same `Name` and `GroupName` collide in Quartz:

```csharp
// ❌ Wrong — both jobs map to JobKey("FooJob", "Default")
public sealed class FooJob1 : BaseSchedulingJob<FooJob1>
{
    public FooJob1(ILogger<FooJob1> logger) : base("FooJob", "...", schedule, logger) { }
}
public sealed class FooJob2 : BaseSchedulingJob<FooJob2>
{
    public FooJob2(ILogger<FooJob2> logger) : base("FooJob", "...", schedule, logger) { }
}

// ✅ Correct — distinct Names (or distinct GroupNames on the schedule)
public sealed class FooJob1 : BaseSchedulingJob<FooJob1>
{
    public FooJob1(ILogger<FooJob1> logger) : base("FooJob1", "...", schedule, logger) { }
}
```

### 8. Throwing From an `ISchedulingMetrics` Implementation

```csharp
// ❌ Wrong — throws are caught and logged at Debug but otherwise silently swallowed.
// You won't see the failure unless Debug logging is enabled.
public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
{
    this._datadog.Submit(info);  // throws if datadog is unreachable
}

// ✅ Correct — handle errors inline, never propagate
public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
{
    try
    {
        this._datadog.Submit(info);
    }
    catch (Exception ex)
    {
        this._logger.LogWarning(ex, "Datadog submission failed for {JobName}", info.JobName);
    }
}
```

### 9. Using `this.Logger.LogDebug()` Instead of `this.LogDebug()`

```csharp
// ❌ Wrong — formats the message string even if Debug is disabled
this.Logger.LogDebug("FooJob processed {N} records", count);

// ✅ Correct — checks IsEnabled(Debug) before formatting
this.LogDebug("FooJob processed {N} records", count);
```

### 10. Expecting Runtime Re-evaluation of `SchedulingJobOptions`

```csharp
// ❌ Wrong — options are read at host startup. Mutating the singleton at runtime
// has no effect on running triggers.
options.Features["FooJob"] = new SchedulingJobFeature { Enabled = false };

// ✅ Correct — restart the host. Roadbed.Scheduling deliberately evaluates options once,
// before Quartz starts. This matches the design intent and keeps the framework simple.
```

---

## Quick Reference

### Using statements

```csharp
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;
```

### Minimal job (constructor pattern, fixed schedule)

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public FooJob(ILogger<FooJob> logger)
        : base(
            name: "FooJob",
            description: "Runs every 5 minutes.",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(5)),
            logger: logger)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Cron schedule with timezone

```csharp
new SchedulingSchedule("0 30 2 * * ?")
{
    TimeZone = TimeZoneInfo.Local,
    GroupName = "MyApp",
    Priority = SchedulingJobPriority.High,
}
```

### Manual-only job (programmatic trigger)

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public FooJob(ILogger<FooJob> logger)
        : base(name: "FooJob", description: "Manually triggered.", logger: logger)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// To trigger:
var schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();
await scheduler.TriggerJob(new JobKey("FooJob", "Default"));
```

### Lenient options-gated job

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public const string JobName = "FooJob";

    public FooJob(ILogger<FooJob> logger, SchedulingJobOptions options)
        : base(
            name: JobName,
            description: "Foo.",
            defaultSchedule: new SchedulingSchedule(TimeSpan.FromMinutes(30)),
            options: options,
            logger: logger)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Strict options-gated job

```csharp
public sealed class FooJob : BaseSchedulingJob<FooJob>
{
    public const string JobName = "FooJob";

    public FooJob(ILogger<FooJob> logger, SchedulingJobOptions options)
        : base(
            name: JobName,
            description: "Schedule must come from options.",
            options: options,
            logger: logger)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Populate options from appSettings

```csharp
var options = new SchedulingJobOptions
{
    Features = new Dictionary<string, SchedulingJobFeature>
    {
        [FooJob.JobName] = new SchedulingJobFeature
        {
            Enabled = configuration.GetValue<bool>("Jobs:Foo:Enabled", true),
            CronExpression = configuration["Jobs:Foo:Cron"],
        },
    },
};
services.AddSingleton(options);
```

### Set a metrics result message

```csharp
public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    int processed = await this.ProcessAsync(cancellationToken);
    this.Context.Result = $"Processed {processed} foos";
}
```

### Enable logging metrics

```csharp
builder.Services.AddSingleton<ISchedulingMetrics, LoggingMetricsAdapter>();
builder.Services.InstallModulesInAppDomain(builder.Configuration);
```

### Custom metrics adapter

```csharp
public sealed class FooMetrics : ISchedulingMetrics
{
    public void JobStarted(JobExecutionInfo info) { /* ... */ }
    public void JobCompleted(JobExecutionInfo info, TimeSpan duration) { /* ... */ }
    public void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration) { /* ... */ }
    public void JobMisfired(JobExecutionInfo info) { /* ... */ }
}

builder.Services.AddSingleton<ISchedulingMetrics, FooMetrics>();
```
