namespace Roadbed.Scheduling;

using Roadbed.Data;

/// <summary>
/// Marker subinterface of <see cref="IDataConnectionFactory"/> identifying the
/// connection factory used by the Quartz AdoJobStore.
/// </summary>
/// <remarks>
/// <para>
/// The host application registers a concrete <see cref="ISchedulingDatabaseFactory"/>
/// pointing at the schema that backs the Quartz tables. The factory is required
/// only when <see cref="SchedulingPersistenceOptions.Mode"/> is
/// <see cref="SchedulingPersistenceMode.Persistent"/>; <see cref="SchedulingPersistenceMode.InMemory"/>
/// ignores it.
/// </para>
/// <para>
/// Using a dedicated marker (instead of resolving <see cref="IDataConnectionFactory"/>
/// directly) lets the host register a Quartz-specific connection alongside
/// the application's own data connections — typically a separate schema or
/// database (for example, <c>quartz_foo_service</c>) so backups, migrations,
/// and access control can be reasoned about independently of the application's
/// own tables.
/// </para>
/// <para>
/// Roadbed.Scheduling inspects
/// <see cref="DataConnecionString.ConnectionStringType"/> on the factory's
/// <see cref="IDataConnectionFactory.Connecion"/> to choose the appropriate
/// Quartz fluent configuration method (MySQL, PostgreSQL, SQLite, …). The
/// actual ADO.NET driver assembly is loaded reflectively by Quartz at runtime
/// and must be present in the host process — typically supplied transitively
/// by the host's reference to <c>Roadbed.Data.MySql</c>,
/// <c>Roadbed.Data.Postgresql</c>, or <c>Roadbed.Data.Sqlite</c>.
/// </para>
/// </remarks>
public interface ISchedulingDatabaseFactory
    : IDataConnectionFactory
{
}
