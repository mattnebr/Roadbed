<h1 align="center">
  <img src="docs/assets/icon-intersection-100.png" alt="Roadbed" width="100" />
  <br />
  Roadbed
  <br />

</h1>
<hr>

A comprehensive framework for building resilient, data-driven C# applications with standardized patterns for data access, HTTP communication, messaging, and API integration.

## Overview

Roadbed provides a cohesive set of libraries that work together to accelerate development of maintainable .NET applications. Built on modern .NET patterns including dependency injection, structured logging, and async/await, Roadbed eliminates boilerplate code while enforcing best practices.

## Key Features

- **Auto-Discovery Module Registration** - Automatic dependency injection setup across assemblies
- **Repository & Entity Patterns** - Structured CRUD operations with business logic separation
- **HTTP Client Wrapper** - Resilient API calls with automatic retry logic and compression
- **Standardized Messaging** - Consistent message envelopes for event-driven architectures
- **Data Access Abstractions** - Database-agnostic interfaces with SQLite implementation
- **Base Classes with Logging** - Performance-optimized logging built into base classes
- **Job Scheduling** - Automatic job discovery and scheduling with Quartz.NET
- **File I/O Utilities** - Type-safe CSV operations with custom mappers
- **SDK Development Tools** - Patterns for building type-safe API client libraries

## Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                        Your Application                      │
└────────────────────┬────────────────────────────────────────┘
                     │
     ┌───────────────┼───────────────┬─────────────────┬──────────────┐
     │               │               │                 │              │
┌────▼─────┐  ┌─────▼──────┐  ┌────▼─────┐   ┌──────▼──────┐  ┌────▼────────┐
│ Roadbed  │  │  Roadbed   │  │ Roadbed  │   │   Roadbed   │  │   Roadbed   │
│  .Crud   │  │ .Messaging │  │  .Net    │   │    .Sdk.*   │  │ .Scheduling │
└────┬─────┘  └────────────┘  └──────────┘   └─────────────┘  └─────────────┘
     │
┌────▼──────────┐
│ Roadbed.Data  │  ← Abstractions
└────┬──────────┘
     │
┌────▼─────────────┐  ┌────────────┐
│ Roadbed.Data     │  │  Roadbed   │
│   .Sqlite        │  │    .IO     │
└──────────────────┘  └────────────┘
     │
┌────▼──────────┐
│   Roadbed     │
│   .Common     │
└───────────────┘
```

## Package Ecosystem

For detailed documentation on each package:

- [Roadbed.Common](./Roadbed.Common/README.md) - Shared types and utilities
- [Roadbed.Crud](./Roadbed.Crud/README.md) - Repository/Entity patterns
- [Roadbed.Data](./Roadbed.Data/README.md) - Data access abstractions
- [Roadbed.Data.Dapper](./Roadbed.Data.Dapper/README.md) - Dapper configuration
- [Roadbed.Data.Sqlite](./Roadbed.Data.Sqlite/README.md) - SQLite data access
- [Roadbed.IO](./Roadbed.IO/README.md) - File I/O and CSV operations
- [Roadbed.Messaging](./Roadbed.Messaging/README.md) - Message envelopes
- [Roadbed.Net](./Roadbed.Net/README.md) - HTTP client wrapper
- [Roadbed.Scheduling](./Roadbed.Scheduling/README.md) - Job scheduling
- [Roadbed.Sdk.NationalWeatherService](./Roadbed.Sdk.NationalWeatherService/README.md) - NWS API client

## Quick Start

### 1. Install Core Package
```bash
dotnet add package Roadbed.Common
```

### 2. Create Module Installer
```csharp
using Roadbed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public class InstallMyModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register your services
        services.AddScoped<IFooRepository, FooRepository>();
        services.AddScoped<FooEntity>();
        
        // Capture ServiceLocator snapshot
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 3. Application Startup
```csharp
using Roadbed;

var builder = WebApplication.CreateBuilder(args);

// Auto-discovers and registers all IServiceCollectionInstaller implementations
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 4. Use in Your Services
```csharp
using Roadbed;

public class FooService : BaseClassWithLogging<FooService>
{
    private readonly FooEntity _fooEntity;

    public FooService(FooEntity fooEntity, ILoggerFactory factory)
        : base(factory)
    {
        _fooEntity = fooEntity;
    }

    public async Task ProcessAsync()
    {
        this.LogInformation("Starting process");
        
        var items = await _fooEntity.ListAsync();
        this.LogDebug("Found {Count} items", items.Count);
        
        // Business logic
    }
}
```


## Design Principles

### Auto-Discovery Over Manual Registration

Roadbed uses `IServiceCollectionInstaller` to automatically discover and register services across assemblies, eliminating manual wiring in `Program.cs`.

### Base Classes with Built-In Logging

`BaseClassWithLogging<T>` provides performance-optimized logging methods that check log levels before formatting messages, preventing unnecessary allocations.

### Repository Pattern with Entity Wrapper

Repositories handle data access, while Entities add business logic. This separation keeps concerns clear and code testable.

### Standardized Response Patterns

`NetHttpResponse<T>` and messaging envelopes provide consistent success/failure patterns across HTTP and messaging boundaries.

### ServiceLocator for NuGet Self-Containment

While generally an anti-pattern, ServiceLocator enables NuGet packages to operate self-contained without requiring consumers to manually register internal dependencies.

## Complete Example: Weather Service
```csharp
using Roadbed;
using Roadbed.Sdk.NationalWeatherService;
using Roadbed.Messaging;
using Roadbed.Common;

public class WeatherService : BaseClassWithLogging<WeatherService>
{
    private readonly MessagingMessageRequest<CommonKeyValuePair<string, string>> _messagingRequest;

    public WeatherService(ILoggerFactory factory)
        : base(factory)
    {
        var publisher = new MessagingPublisher(
            new CommonBusinessKey("weather-service", "WeatherService"),
            "v1.0");
        
        _messagingRequest = new MessagingMessageRequest<CommonKeyValuePair<string, string>>(
            publisher,
            "nws.forecast");
    }

    public async Task<string> GetForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        this.LogInformation("Getting forecast for {Lat}, {Lon}", latitude, longitude);

        // Convert lat/long to grid coordinates
        var forecastRequest = await NwsForecastRequest.FromLocation(
            latitude,
            longitude,
            _messagingRequest,
            cancellationToken);

        this.LogDebug(
            "Grid coordinates: Office={Office}, X={X}, Y={Y}",
            forecastRequest.OfficeId,
            forecastRequest.GridCoordinateX,
            forecastRequest.GridCoordinateY);

        // Get daily forecast
        var daily = await NwsForecastDaily.FromForecastRequest(
            forecastRequest,
            _messagingRequest,
            cancellationToken);

        if (daily.Periods == null || daily.Periods.Count == 0)
        {
            this.LogWarning("No forecast periods available");
            return "No forecast data available";
        }

        // Build summary
        var today = daily.Periods[0];
        return $"{today.Name}: {today.DescriptionShort}, {today.TemperatureInFahrenheit}°F";
    }
}
```

## Requirements

- .NET 10.0+
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration
