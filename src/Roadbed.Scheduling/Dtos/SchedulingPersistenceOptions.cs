/*
 * The namespace Roadbed.Scheduling.Dtos was removed on purpose and replaced with Roadbed.Scheduling so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

/// <summary>
/// Options POCO for selecting the Quartz job-store backend and naming the
/// scheduler instance.
/// </summary>
/// <remarks>
/// <para>
/// This type is populated by the hosting application and registered as a
/// singleton in DI. Roadbed.Scheduling resolves it via DI in
/// <c>InstallScheduling</c>; if no instance is registered, the installer
/// falls back to a default <see cref="SchedulingPersistenceOptions"/> (which
/// yields <see cref="SchedulingPersistenceMode.InMemory"/>, preserving the
/// previous out-of-box behavior).
/// </para>
/// <para>
/// Roadbed.Scheduling never reads configuration directly — the application
/// owns the mapping between its own configuration shape (appsettings.json,
/// environment variables, secret store, …) and this POCO.
/// </para>
/// <para>
/// When <see cref="Mode"/> is <see cref="SchedulingPersistenceMode.Persistent"/>,
/// the host must also register an <see cref="ISchedulingDatabaseFactory"/>
/// singleton pointing at the Quartz schema.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In the host application's Program.cs or installer:
/// services.AddSingleton(new SchedulingPersistenceOptions
/// {
///     SchedulerName = "Foo-Prod",
///     Mode = SchedulingPersistenceMode.Persistent,
///     TablePrefix = "QRTZ_",
///     IsClustered = false,
/// });
///
/// services.AddSingleton&lt;ISchedulingDatabaseFactory&gt;(_ =&gt;
///     new FooSchedulingDatabaseFactory(new DataConnecionString(DataConnectionStringType.MySQL)
///     {
///         ServerName    = configuration["Foo:Scheduler:Mysql:Server"]!,
///         DatabaseSource = configuration["Foo:Scheduler:Mysql:Database"]!,
///         Username      = configuration["Foo:Scheduler:Mysql:Username"]!,
///         Password      = configuration["Foo:Scheduler:Mysql:Password"]!,
///     }));
/// </code>
/// </example>
public sealed class SchedulingPersistenceOptions
{
    #region Public Properties

    /// <summary>
    /// Gets the Quartz scheduler instance name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Maps to Quartz's <c>quartz.scheduler.instanceName</c> and to the
    /// <c>SCHED_NAME</c> column in every AdoJobStore table. Distinct values
    /// isolate schedulers that share a schema — two Windows services pointed
    /// at the same MySQL schema with different <c>SchedulerName</c> values
    /// will not see each other's jobs.
    /// </para>
    /// <para>
    /// Conversely, multiple host nodes that share the same
    /// <c>SchedulerName</c> AND have <see cref="IsClustered"/> set to
    /// <see langword="true"/> form a Quartz cluster: only one node fires
    /// each trigger, and surviving nodes recover in-flight jobs from a
    /// failed node after the misfire threshold.
    /// </para>
    /// <para>
    /// Defaults to <c>"RoadbedScheduler"</c>. Required to be non-blank in
    /// practice; the host should override with a value that identifies the
    /// scheduler within its operational context.
    /// </para>
    /// </remarks>
    public string SchedulerName { get; init; } = "RoadbedScheduler";

    /// <summary>
    /// Gets the persistence mode that selects the Quartz job-store backend.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SchedulingPersistenceMode.InMemory"/>. Switch
    /// to <see cref="SchedulingPersistenceMode.Persistent"/> when you need
    /// trigger durability across restarts or want to cluster two host nodes.
    /// </remarks>
    public SchedulingPersistenceMode Mode { get; init; }
        = SchedulingPersistenceMode.InMemory;

    /// <summary>
    /// Gets the table prefix for Quartz AdoJobStore tables.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>"QRTZ_"</c>, matching Quartz's bundled DDL scripts.
    /// Override only if the host has embedded a Quartz schema version in
    /// the prefix to make in-place migrations easier (for example,
    /// <c>"QRTZ_V3_18_"</c>). Ignored when <see cref="Mode"/> is
    /// <see cref="SchedulingPersistenceMode.InMemory"/>.
    /// </remarks>
    public string TablePrefix { get; init; } = "QRTZ_";

    /// <summary>
    /// Gets a value indicating whether this host participates in a Quartz
    /// cluster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to <see langword="true"/>, Quartz enables its clustering
    /// protocol against the persistent store: multiple host nodes that
    /// share the same <see cref="SchedulerName"/> coordinate execution
    /// through the database's locking tables.
    /// </para>
    /// <para>
    /// Ignored when <see cref="Mode"/> is
    /// <see cref="SchedulingPersistenceMode.InMemory"/> (clustering
    /// requires a persistent store).
    /// </para>
    /// <para>
    /// Defaults to <see langword="false"/>.
    /// </para>
    /// </remarks>
    public bool IsClustered { get; init; }

    #endregion Public Properties
}
