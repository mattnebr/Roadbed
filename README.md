<h1 align="center" style="border:0;margin:0;"> <img src="docs/assets/icon-intersection-100.png" alt="Roadbed" width="100" /> <br /> Roadbed <br /> </h1>

---

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

- [Roadbed.Common](/src/Roadbed.Common/README.md) - Shared types and utilities
- [Roadbed.Crud](/src/Roadbed.Crud/README.md) - Repository/Entity patterns
- [Roadbed.Data](/src/Roadbed.Data/README.md) - Data access abstractions
- [Roadbed.Data.Dapper](/src/Roadbed.Data.Dapper/README.md) - Dapper configuration
- [Roadbed.Data.Sqlite](/src/Roadbed.Data.Sqlite/README.md) - SQLite data access
- [Roadbed.IO](/src/Roadbed.IO/README.md) - File I/O and CSV operations
- [Roadbed.Messaging](/src/Roadbed.Messaging/README.md) - Message envelopes
- [Roadbed.Net](/src/Roadbed.Net/README.md) - HTTP client wrapper
- [Roadbed.Scheduling](/src/Roadbed.Scheduling/README.md) - Job scheduling
- [Roadbed.Sdk.NationalWeatherService](/src/Roadbed.Sdk.NationalWeatherService/README.md) - NWS API client

## Quick Start

### 1. Install Core Package

```bash
dotnet add package Roadbed.Common
```

### 2. Define an Entity

```csharp
using Roadbed.Crud;

public sealed record FooRecord : BaseEntityRecord<long>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
```

### 3. Implement a Repository

Repository interfaces and implementations are `internal`. The repository handles data access only.

```csharp
using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;

internal interface IFooRepository
    : IAsyncCrudlRepository<FooRecord, long>
{
}

internal sealed class FooRepository
    : BaseAsyncCrudlRepository<FooRecord, long>,
      IFooRepository
{
    public FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    {
    }

    public override async Task<FooRecord> CreateAsync(
        FooRecord entity,
        CancellationToken cancellationToken = default)
    {
        // Data access implementation
        throw new NotImplementedException();
    }

    public override async Task<FooRecord?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // Data access implementation
        throw new NotImplementedException();
    }

    public override async Task<FooRecord> UpdateAsync(
        FooRecord entity,
        CancellationToken cancellationToken = default)
    {
        // Data access implementation
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // Data access implementation
        throw new NotImplementedException();
    }

    public override async Task<IList<FooRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        // Data access implementation
        throw new NotImplementedException();
    }
}
```

### 4. Implement a Service

Service classes are `public sealed` with dual constructors: a public constructor for consuming applications (resolves the repository via `ServiceLocator`) and an internal constructor for unit tests (accepts the repository directly via `InternalsVisibleTo`).

```csharp
using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

public sealed class FooService
    : BaseAsyncCrudlService<FooRecord, long>
{
    /// <summary>
    /// Public constructor for consuming applications.
    /// </summary>
    public FooService(ILogger<FooService> logger)
        : base(ServiceLocator.GetService<IFooRepository>(), logger)
    {
    }

    /// <summary>
    /// Internal constructor for unit tests.
    /// </summary>
    internal FooService(
        IFooRepository repository,
        ILogger<FooService> logger)
        : base(repository, logger)
    {
    }

    /// <summary>
    /// Custom business logic beyond standard CRUDL operations.
    /// </summary>
    public async Task<FooRecord> CloneFooAsync(
        long sourceId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        FooRecord? source = await this.ReadAsync(sourceId, cancellationToken);
        ArgumentNullException.ThrowIfNull(source);

        var clone = new FooRecord
        {
            Name = newName,
            Description = source.Description,
        };

        return await this.CreateAsync(clone, cancellationToken);
    }
}
```

### 5. Create a Module Installer

The installer registers only the internal repository (for `ServiceLocator` resolution). The `public sealed` service class is consumed directly by the application — it is not registered in DI.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;

public sealed class InstallFooModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register repository for ServiceLocator resolution
        services.AddScoped<IFooRepository, FooRepository>();

        // Capture ServiceLocator snapshot for NuGet self-containment
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 6. Application Startup

```csharp
using Roadbed;

var builder = WebApplication.CreateBuilder(args);

// Auto-discovers and registers all IServiceCollectionInstaller implementations
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 7. Make HTTP Calls with NetHttpClient

```csharp
using Microsoft.Extensions.Logging;
using Roadbed.Net;

public class BarApiClient : BaseClassWithLogging
{
    private readonly INetHttpClient _httpClient;

    public BarApiClient(
        INetHttpClient httpClient,
        ILogger<BarApiClient> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this._httpClient = httpClient;
    }

    public async Task<BarResponse?> GetBarAsync(
        string barId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barId);

        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri($"https://api.example.com/bars/{barId}"),
            Method = HttpMethod.Get,
            Authentication = new NetHttpAuthentication
            {
                AuthenticationType = NetHttpAuthenticationType.Bearer,
                Value = "my-api-token",
            },
        };

        NetHttpResponse<BarResponse> response =
            await this._httpClient.MakeHttpRequestAsync<BarResponse>(
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            this.LogWarning(
                "Failed to get Bar {BarId}: {StatusCode}",
                barId,
                response.HttpStatusCode);

            return null;
        }

        return response.Data;
    }
}
```

## Design Principles

### Auto-Discovery Over Manual Registration

Roadbed uses `IServiceCollectionInstaller` to automatically discover and register services across assemblies, eliminating manual wiring in `Program.cs`.

### Base Classes with Built-In Logging

`BaseClassWithLogging` provides performance-optimized logging methods that check log levels before formatting messages, preventing unnecessary string allocations.

```csharp
// Good: No string formatting occurs when Debug logging is disabled
this.LogDebug("Processing {Count} items with {Size} bytes", items.Count, totalSize);
```

### Repository Pattern with Service Delegation

Repositories handle data access and are `internal`. Services are `public sealed` and delegate to repositories, composing higher-level operations like `ExistsAsync` and `UpsertAsync` from repository primitives. This separation keeps concerns clear and code testable.

### Dual Constructor Pattern

Service classes expose a public constructor (resolves dependencies via `ServiceLocator`) and an internal constructor (accepts dependencies directly for unit testing via `InternalsVisibleTo`). This enables NuGet packages to be self-contained while remaining fully testable.

### Standardized Response Patterns

`NetHttpResponse<T>` provides consistent success/failure patterns across HTTP boundaries with built-in retry and backoff support via `NetHttpRequest`.

### ServiceLocator for NuGet Self-Containment

While generally an anti-pattern, `ServiceLocator` enables NuGet packages to operate self-contained without requiring consumers to manually register internal dependencies.

## Complete Example: Foo Service with External API

```csharp
using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud;
using Roadbed.Crud.Services.Async;
using Roadbed.Net;

/// <summary>
/// Service for managing Foo entities with external API enrichment.
/// Inherits CRUDL operations and composes ExistsAsync/UpsertAsync
/// from repository primitives.
/// </summary>
public sealed class FooService
    : BaseAsyncCrudlService<FooRecord, long>
{
    private readonly INetHttpClient _httpClient;

    /// <summary>
    /// Public constructor for consuming applications.
    /// </summary>
    public FooService(
        INetHttpClient httpClient,
        ILogger<FooService> logger)
        : base(ServiceLocator.GetService<IFooRepository>(), logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this._httpClient = httpClient;
    }

    /// <summary>
    /// Internal constructor for unit tests.
    /// </summary>
    internal FooService(
        IFooRepository repository,
        INetHttpClient httpClient,
        ILogger<FooService> logger)
        : base(repository, logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this._httpClient = httpClient;
    }

    /// <summary>
    /// Creates a Foo entity enriched with data from an external API.
    /// </summary>
    public async Task<FooRecord> CreateEnrichedFooAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        this.LogInformation("Creating enriched Foo: {Name}", name);

        // Fetch enrichment data from external API
        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri($"https://api.example.com/enrich/{name}"),
            Method = HttpMethod.Get,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = 2,
                DelayMultiplierInSeconds = 3,
            },
        };

        NetHttpResponse<FooEnrichmentDto> response =
            await this._httpClient.MakeHttpRequestAsync<FooEnrichmentDto>(
                request,
                cancellationToken);

        // Build entity with enrichment data (or defaults on failure)
        var foo = new FooRecord
        {
            Name = name,
            Description = response.IsSuccessStatusCode
                ? response.Data?.Description
                : "No enrichment data available",
        };

        // UpsertAsync checks ExistsAsync, then calls CreateAsync or UpdateAsync
        FooRecord result = await this.UpsertAsync(foo, cancellationToken);

        this.LogInformation("Foo created with Id: {Id}", result.Id);

        return result;
    }
}
```

## Requirements

- .NET 10.0+
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration