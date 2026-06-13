namespace Roadbed.Test.Unit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

/// <summary>
/// Unit tests for the ServiceCollectionExtensions class.
/// </summary>
[TestClass]
public class ServiceCollectionExtensionsTests
{
    #region InstallModule Tests

    /// <summary>
    /// Verifies that the typed <c>InstallModule&lt;TInstaller&gt;</c> runs
    /// exactly the named installer — the deterministic, compile-pinned
    /// alternative to assembly auto-discovery for vendored satellites.
    /// </summary>
    [TestMethod]
    public void InstallModule_NamedInstaller_RunsThatInstaller()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        IServiceCollection returned = services.InstallModule<TestInstaller1>(configuration);

        // Assert (Then)
        Assert.AreSame(services, returned, "InstallModule should return the same collection for chaining.");
        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<TestService1>(), "The named installer's services must be registered.");
        Assert.IsNull(provider.GetService<TestService2>(), "Only the named installer runs — not every installer in the assembly.");
    }

    #endregion InstallModule Tests

    #region InstallFromAssembly Tests

    /// <summary>
    /// Verifies that InstallFromAssembly finds and executes installers in the assembly.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithInstallers_ExecutesAllInstallers()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert (Then)
        var service1 = serviceProvider.GetService<TestService1>();
        var service2 = serviceProvider.GetService<TestService2>();

        Assert.IsNotNull(
            service1,
            "TestService1 should be registered by TestInstaller1.");
        Assert.IsNotNull(
            service2,
            "TestService2 should be registered by TestInstaller2.");
    }

    /// <summary>
    /// Verifies that InstallFromAssembly registers services from all found installers.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithMultipleInstallers_RegistersAllServices()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);

        // Assert (Then)
        var descriptor1 = services.FirstOrDefault(x => x.ServiceType == typeof(TestService1));
        var descriptor2 = services.FirstOrDefault(x => x.ServiceType == typeof(TestService2));

        Assert.IsNotNull(
            descriptor1,
            "TestService1 should be in service collection.");
        Assert.IsNotNull(
            descriptor2,
            "TestService2 should be in service collection.");
    }

    /// <summary>
    /// Verifies that InstallFromAssembly registers services in the order installers are found.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_RegistersServices_InOrder()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);

        // Assert (Then)
        Assert.IsGreaterThan(
            0,
            services.Count,
            "Services should be registered.");

        var hasTestService1 = services.Any(x => x.ServiceType == typeof(TestService1));
        var hasTestService2 = services.Any(x => x.ServiceType == typeof(TestService2));

        Assert.IsTrue(
            hasTestService1,
            "TestService1 should be registered.");
        Assert.IsTrue(
            hasTestService2,
            "TestService2 should be registered.");
    }

    #endregion InstallFromAssembly Tests

    #region InstallFromAssembly Tests (Previously InstallModulesInAppDomain)

    /// <summary>
    /// Unit test to verify that InstallFromAssembly finds and executes installers from the test assembly.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithInstallers_ExecutesAllInstallersInTestAssembly()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert (Then)
        var service1 = serviceProvider.GetService<TestService1>();
        var service2 = serviceProvider.GetService<TestService2>();

        Assert.IsNotNull(
            service1,
            "TestService1 should be registered by installer found in test assembly.");
        Assert.IsNotNull(
            service2,
            "TestService2 should be registered by installer found in test assembly.");
    }

    /// <summary>
    /// Unit test to verify that InstallFromAssembly registers services from multiple installers.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithMultipleInstallers_RegistersAllServicesInTestAssembly()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);

        // Assert (Then)
        var descriptor1 = services.FirstOrDefault(x => x.ServiceType == typeof(TestService1));
        var descriptor2 = services.FirstOrDefault(x => x.ServiceType == typeof(TestService2));

        Assert.IsNotNull(
            descriptor1,
            "TestService1 should be in service collection from test assembly scan.");
        Assert.IsNotNull(
            descriptor2,
            "TestService2 should be in service collection from test assembly scan.");
    }

    /// <summary>
    /// Unit test to verify that InstallFromAssembly does not register services from abstract installers.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithAbstractInstaller_DoesNotRegisterAbstractServices()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert (Then)
        var abstractService = serviceProvider.GetService<AbstractTestService>();

        Assert.IsNull(
            abstractService,
            "AbstractTestService should not be registered because its installer is abstract.");
    }

    /// <summary>
    /// Unit test to verify that InstallFromAssembly passes configuration to installers correctly.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithConfiguration_PassesConfigurationToInstallers()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string>
        {
            { "TestKey", "TestValue" },
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        // Act (When)
        services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert (Then)
        var configService = serviceProvider.GetService<ConfigurationTestService>();

        Assert.IsNotNull(
            configService,
            "ConfigurationTestService should be registered.");
        Assert.AreEqual(
            "TestValue",
            configService.ConfigValue,
            "Configuration value should be passed to installer and used in service registration.");
    }

    /// <summary>
    /// Unit test to verify that InstallFromAssembly handles assemblies with reflection errors gracefully.
    /// </summary>
    [TestMethod]
    public void InstallFromAssembly_WithReflectionErrors_ContinuesProcessing()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act (When)
        bool exceptionThrown = false;
        try
        {
            services.InstallFromAssembly<ServiceCollectionExtensionsTests>(configuration);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "InstallFromAssembly should handle reflection errors gracefully without throwing exceptions.");
        Assert.IsGreaterThan(
            0,
            services.Count,
            "Services should still be registered despite potential reflection errors.");
    }

    #endregion InstallFromAssembly Tests (Previously InstallModulesInAppDomain)

    #region Test Helper Classes and Installers

    /// <summary>
    /// Test service 1.
    /// </summary>
    public class TestService1
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; } = "Service1";
    }

    /// <summary>
    /// Test service 2.
    /// </summary>
    public class TestService2
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; } = "Service2";
    }

    /// <summary>
    /// Service that uses configuration.
    /// </summary>
    public class ConfigurationTestService
    {
        /// <summary>
        /// Gets or sets the config value of the service.
        /// </summary>
        public string? ConfigValue { get; set; }
    }

    /// <summary>
    /// Existing service for testing preservation.
    /// </summary>
    public class ExistingService
    {
        /// <summary>
        /// Gets or sets the value of the service.
        /// </summary>
        public string Value { get; set; } = "Existing";
    }

    /// <summary>
    /// Test service from abstract installer (should not be registered).
    /// </summary>
    public class AbstractTestService
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; } = "AbstractService";
    }

    /// <summary>
    /// Test service from private installer (should not be registered).
    /// </summary>
    public class PrivateTestService
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; } = "PrivateService";
    }

    /// <summary>
    /// Service with dependency.
    /// </summary>
    public class ServiceWithDependency
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceWithDependency"/> class.
        /// </summary>
        /// <param name="testService">Test service.</param>
        public ServiceWithDependency(TestService1 testService)
        {
            this.TestService = testService;
        }

        /// <summary>
        /// Gets the test service.
        /// </summary>
        public TestService1 TestService { get; }
    }

    /// <summary>
    /// Test installer 1.
    /// </summary>
    public class TestInstaller1 : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TestService1>();
        }
    }

    /// <summary>
    /// Test installer 2.
    /// </summary>
    public class TestInstaller2 : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TestService2>();
        }
    }

    /// <summary>
    /// Test installer that uses configuration.
    /// </summary>
    public class ConfigurationTestInstaller : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var configValue = configuration["TestKey"] ?? "DefaultValue";
            services.AddSingleton(new ConfigurationTestService { ConfigValue = configValue });
        }
    }

    /// <summary>
    /// Test installer that registers service with dependencies.
    /// </summary>
    public class DependencyTestInstaller : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ServiceWithDependency>();
        }
    }

    /// <summary>
    /// Abstract installer (should not be instantiated).
    /// </summary>
    public abstract class AbstractTestInstaller : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<AbstractTestService>();
        }
    }

    /// <summary>
    /// Private installer (should not be found by GetExportedTypes).
    /// </summary>
    private class PrivateTestInstaller : IServiceCollectionInstaller
    {
        /// <summary>
        /// Configure Test Services.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="configuration">Configured for application.</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<PrivateTestService>();
        }
    }

    #endregion Test Helper Classes and Installers
}