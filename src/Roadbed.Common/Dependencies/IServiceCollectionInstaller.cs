/*
 * The namespace Roadbed.Common.Dependencies was removed on purpose and replaced with Roadbed so that no additional using statements are required.
 */

namespace Roadbed;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface defining a service collection installer.
/// </summary>
public interface IServiceCollectionInstaller
{
    #region Public Methods

    /// <summary>
    /// Configures services in the service collection.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    #endregion Public Methods
}