namespace Roadbed.Logging.MySql;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Roadbed.Logging.Installers;

/// <summary>
/// Auto-discovered installer that selects MySQL/MariaDB as the Roadbed.Logging
/// backing provider.
/// </summary>
/// <remarks>
/// Registers the MySQL <see cref="ILoggingDataExecutor"/> first, then delegates
/// to <see cref="LoggingModule.Register"/> for the provider-neutral wiring.
/// Registering the executor ahead of that call guarantees it is present in the
/// <c>ServiceLocator</c> snapshot the module captures. Reference exactly one
/// Roadbed.Logging provider package per host.
/// </remarks>
public sealed class InstallLoggingMySql : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<ILoggingDataExecutor, MySqlLoggingDataExecutor>();
        LoggingModule.Register(services);
    }

    #endregion Public Methods
}
