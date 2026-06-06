namespace Roadbed.Logging;

using Roadbed.Data;

/// <summary>
/// Marker interface identifying the <see cref="IDataConnectionFactory"/> that
/// Roadbed.Logging uses to reach its persistent backing store.
/// </summary>
/// <remarks>
/// <para>
/// Consuming applications register a concrete implementation as a singleton
/// in the DI container before <c>InstallModulesInAppDomain</c> runs. The
/// installer resolves the registration and forwards the connection factory
/// to every Roadbed.Logging repository.
/// </para>
/// <para>
/// Roadbed.Logging supports two connection string types in v1: MySQL/MariaDB
/// and SQLite. Any other type returned by <see cref="DataConnecionString.ConnectionStringType"/>
/// will cause the installer to throw.
/// </para>
/// </remarks>
public interface ILoggingDatabaseFactory : IDataConnectionFactory
{
}
