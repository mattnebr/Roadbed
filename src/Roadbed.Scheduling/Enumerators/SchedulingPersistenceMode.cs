/*
 * The namespace Roadbed.Scheduling.Enumerators was removed on purpose and replaced with Roadbed.Scheduling so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

/// <summary>
/// Defines how the Quartz scheduler stores job, trigger, and execution state.
/// </summary>
public enum SchedulingPersistenceMode
{
    /// <summary>
    /// Quartz's RAMJobStore. State lives in process memory only. Restarts lose
    /// every trigger; clustering is not possible. Default mode — appropriate
    /// for short-lived hosts, integration tests, and development scenarios
    /// where job loss on restart is acceptable.
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// Quartz's AdoJobStore (JobStoreTX). State is persisted to the database
    /// supplied by the host-registered <see cref="ISchedulingDatabaseFactory"/>.
    /// Required for trigger durability across restarts and for clustering.
    /// The host application is responsible for applying Quartz's DDL to the
    /// target schema before the scheduler starts.
    /// </summary>
    Persistent = 1,
}
