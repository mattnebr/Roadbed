/*
 * The namespace Roadbed.Common.Extensions was removed on purpose and replaced with Roadbed so that no additional using statements are required.
 */

namespace Roadbed;

using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for common Service Collection operations.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region Private Fields

    /// <summary>
    /// Type of interface used to install modules.
    /// </summary>
    private static readonly Type InterfaceType = typeof(IServiceCollectionInstaller);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Installs services from the specified assembly that implement <see cref="IServiceCollectionInstaller"/>.
    /// </summary>
    /// <typeparam name="T">Assembly to scan for implemenations of <see cref="IServiceCollectionInstaller"/>.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <remarks>
    /// This method comes from the Installer.Microsoft.ServiceCollection NuGet package. You can find the original
    /// source code at: https://github.com/thisisnabi/Installer.Microsoft.ServiceCollection.
    /// </remarks>
    public static void InstallFromAssembly<T>(this IServiceCollection services, IConfiguration configuration)
    {
        InvokeInstallers(typeof(T).Assembly, services, configuration);
    }

    /// <summary>
    /// Runs a single, explicitly named <see cref="IServiceCollectionInstaller"/>.
    /// </summary>
    /// <typeparam name="TInstaller">The installer to run. Naming the concrete type is a compile-time dependency, so the C# compiler keeps the reference in the entry assembly's manifest and the runtime loads the assembly — there is nothing for it to elide.</typeparam>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">Application configuration passed to the installer.</param>
    /// <param name="logger">Optional bootstrap logger; when supplied, one line is written as the installer runs.</param>
    /// <returns>The same <paramref name="services"/> for fluent chaining.</returns>
    /// <remarks>
    /// This is the deterministic, vendored-DLL-safe alternative to
    /// <see cref="InstallModulesInAppDomain"/> for selecting a provider
    /// satellite (e.g. a logging or scheduling backend). Auto-discovery
    /// depends on the satellite assembly already being loaded, which is not
    /// guaranteed when it is referenced only via <c>HintPath</c> and used by
    /// no source-level type. Naming the installer here removes that ambiguity
    /// and self-documents the provider choice at the call site.
    /// </remarks>
    public static IServiceCollection InstallModule<TInstaller>(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger? logger = null)
        where TInstaller : IServiceCollectionInstaller, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        logger?.LogInformation("Roadbed installer selected: {InstallerType}", typeof(TInstaller).FullName);
        new TInstaller().ConfigureServices(services, configuration);
        logger?.LogInformation("Roadbed installer configured services: {InstallerType}", typeof(TInstaller).FullName);

        return services;
    }

    /// <summary>
    /// Installs services from the specified assembly that implement <see cref="IServiceCollectionInstaller"/>.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="logger">Optional bootstrap logger; when supplied, each discovered and invoked installer is logged so a satellite that fails to load (and is therefore silently skipped) is visible at startup.</param>
    /// <remarks>
    /// This method comes from the Installer.Microsoft.ServiceCollection NuGet package. You can find the original
    /// source code at: https://github.com/thisisnabi/Installer.Microsoft.ServiceCollection.
    /// <para>
    /// Auto-discovery only finds installers in assemblies that are already
    /// loaded or reachable through the reference graph. A provider satellite
    /// referenced only via <c>HintPath</c> with no source-level use may be
    /// absent from both, and is then silently skipped. Prefer
    /// <see cref="InstallModule{TInstaller}"/> (or the typed
    /// <c>AddRoadbedDbLogging&lt;TInstaller&gt;()</c>) to select such a
    /// satellite deterministically.
    /// </para>
    /// </remarks>
    public static void InstallModulesInAppDomain(this IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<Assembly>();

        // Start with all currently loaded assemblies in the AppDomain
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.FullName) &&
                        !a.FullName.StartsWith("System.") &&
                        !a.FullName.StartsWith("Microsoft."));

        foreach (var assembly in loadedAssemblies)
        {
            queue.Enqueue(assembly);
        }

        // Also include the entry assembly if it exists
        var rootAssembly = Assembly.GetEntryAssembly();
        if (rootAssembly != null)
        {
            queue.Enqueue(rootAssembly);
        }

        // Loop through Assemblies until empty
        while (queue.Any())
        {
            // Grab one from queue
            Assembly assembly = queue.Dequeue();

            if ((assembly == null) || string.IsNullOrEmpty(assembly.FullName))
            {
                continue;
            }

            // Skip if already visited
            if (visited.Contains(assembly.FullName))
            {
                continue;
            }

            // Mark as visited
            visited.Add(assembly.FullName);

            // Process this assembly for installers
            InvokeInstallers(assembly, services, configuration, logger);

            // Get referenced assemblies
            try
            {
                AssemblyName[] references = assembly.GetReferencedAssemblies();

                foreach (var reference in references)
                {
                    // Skip Microsoft/System assemblies
                    if (reference.FullName.StartsWith("System.") ||
                        reference.FullName.StartsWith("Microsoft."))
                    {
                        continue;
                    }

                    // Skip if already visited
                    if (visited.Contains(reference.FullName))
                    {
                        continue;
                    }

                    Assembly loadedAssembly = Assembly.Load(reference);
                    queue.Enqueue(loadedAssembly);
                }
            }
            catch (Exception)
            {
                // Failed to get referenced assemblies, continue with next assembly
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Locates instances of a specific Type.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    /// <returns>List of classes that have implemented an interface.</returns>
    private static IList<IServiceCollectionInstaller> GetImplementations(Assembly assembly)
    {
        // Get public classes that implement from IServiceCollectionInstaller
        var installerInstances = assembly.GetExportedTypes()
                                        .Where(x => InterfaceType.IsAssignableFrom(x) &&
                                                    x is { IsAbstract: false, IsInterface: false })
                                        .Select(Activator.CreateInstance)
                                        .Cast<IServiceCollectionInstaller>()
                                        .ToList();

        return installerInstances;
    }

    /// <summary>
    /// Invokes instances of IServiceCollectionInstaller.
    /// </summary>
    /// <param name="assembly">Assembly to scan for instances of IServiceCollectionInstaller.</param>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    private static void InvokeInstallers(Assembly assembly, IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
    {
        // Locate instances of IServiceCollectionInstaller
        var implementations = GetImplementations(assembly);

        foreach (var implemenation in implementations)
        {
            logger?.LogInformation("Roadbed installer discovered: {InstallerType}", implemenation.GetType().FullName);
            implemenation.ConfigureServices(services, configuration);
            logger?.LogInformation("Roadbed installer configured services: {InstallerType}", implemenation.GetType().FullName);
        }
    }

    #endregion Private Methods
}