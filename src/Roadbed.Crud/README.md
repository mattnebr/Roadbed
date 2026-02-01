# Roadbed.Crud

Base classes and interfaces for implementing the Repository and Service patterns with CRUDAL operations (Create, Read, Update, Delete, Archive, List).

## Overview

Roadbed.Crud provides a structured, compiler-enforced approach to data access. It separates **repositories** (thin data access) from **services** (business logic, validation, orchestration) and gives you the exact combination of operations each entity needs — nothing more, nothing less.

For the full type catalog, interface signatures, and design rationale, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-crud.md).

## Installation

```bash
dotnet add package Roadbed.Crud
```

## Architecture

```
┌─────────────────────────────────────────────────┐
│ Application Layer                               │
│   Depends on: IFooService (public)              │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│ Class Library                                   │
│                                                 │
│   public  IFooService      → business logic     │
│   public FooService        → validation, rules  │
│   internal IFooRepository  → data access        │
│   internal FooRepository   → SQL, API calls     │
└─────────────────────────────────────────────────┘
```

The application layer only sees the **service interface**. The repository is internal to the class library.

## Composites

Pick the composite that matches your entity's needs:

|Composite|Operations|Use When|
|---|---|---|
|**ListOnly**|List|Lookup tables, reference data|
|**Crud**|Create, Read, Update, Delete|Large tables with custom queries|
|**Crudl**|Crud + List|Small-to-medium tables|
|**Cruda**|Crud + Archive|Entities with soft delete|
|**Crudal**|Crud + Archive + List|Full operation set|

Each composite is available in **Async** and **Sync** variants, and at both the **Repository** and **Service** layers. Services also include **Exists** and **Upsert**, composed automatically from repository primitives.

## Quick Start

### 1. Define Your Entity

```csharp
namespace Roadbed.Sdk.Customers;

using Newtonsoft.Json;
using Roadbed.Crud;

public sealed record Customer : BaseEntityRecord<string>
{
    [JsonProperty("id")]
    public override string? Id { get; set; }

    [JsonProperty("name")]
    required public string Name { get; set; }

    [JsonProperty("email")]
    required public string Email { get; set; }
}
```

### 2. Define the Repository Interface (internal)

```csharp
namespace Roadbed.Sdk.Customers;

using Roadbed.Crud.Repositories.Async;

internal interface ICustomerRepository
    : IAsyncCrudlRepository<Customer, string>
{
}
```

### 3. Implement the Repository

```csharp
namespace Roadbed.Sdk.Customers;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

internal sealed class CustomerRepository
    : BaseAsyncCrudlRepository<Customer, string>,
      ICustomerRepository
{
    public CustomerRepository(ILogger<CustomerRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Customer> CreateAsync(
        Customer entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // Data access logic here
        throw new NotImplementedException();
    }

    public override async Task<Customer?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        // Return null when not found
        throw new NotImplementedException();
    }

    public override async Task<Customer> UpdateAsync(
        Customer entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        throw new NotImplementedException();
    }

    public override async Task<IList<Customer>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

The base class is **abstract** — the compiler tells you exactly which methods to implement.

### 4. Define the Service Interface (public)

```csharp
namespace Roadbed.Sdk.Customers;

using Roadbed.Crud.Services.Async;

public interface ICustomerService
    : IAsyncCrudlService<Customer, string>
{
}
```

### 5. Implement the Service

```csharp
namespace Roadbed.Sdk.Customers;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

internal sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService
{
    public CustomerService(
        IAsyncCrudlRepository<Customer, string> repository,
        ILogger<CustomerService> logger)
        : base(repository, logger)
    {
    }

    // All 7 methods work with zero overrides:
    //   CreateAsync, ReadAsync, UpdateAsync, DeleteAsync, ListAsync
    //   + ExistsAsync and UpsertAsync (composed automatically)
    //
    // Override to add business logic:
    //
    // public override async Task<Customer> CreateAsync(
    //     Customer entity,
    //     CancellationToken cancellationToken = default)
    // {
    //     ArgumentNullException.ThrowIfNull(entity);
    //     ArgumentException.ThrowIfNullOrWhiteSpace(entity.Email);
    //     this.LogInformation("Creating customer: {Name}", entity.Name);
    //     return await base.CreateAsync(entity, cancellationToken);
    // }
}
```

The base class is **virtual** — everything works out of the box. Override only when you need validation, caching, or other business logic.

### 6. Register in DI

```csharp
services.AddSingleton<ICustomerRepository, CustomerRepository>();
services.AddSingleton<ICustomerService, CustomerService>();
```

## Key Concepts

**Repositories are abstract.** You must implement every data access method. The compiler enforces this.

**Services are virtual.** All operations delegate to the repository by default. Override only when adding business logic.

**Exists and Upsert are composed.** `ExistsAsync` calls `ReadAsync` and checks for null. `UpsertAsync` calls `ExistsAsync` to decide between Create and Update. Override when your data source supports native upsert.

**Read returns null for not-found.** Never throw when an entity doesn't exist. This enables the composed `ExistsAsync` to work without exception-driven control flow.

**Delete returns void.** Throw on failure instead of returning a boolean.

## Entity Base Types

|Type|Use When|
|---|---|
|`BaseEntityRecord<TId>`|DTOs, API responses, immutable data|
|`BaseEntityClass<TId>`|Domain entities with behavior, ORM compatibility|

## Requirements

- .NET 10.0+
- Microsoft.Extensions.Logging
- Newtonsoft.Json
- Roadbed.Common (BaseClassWithLogging)

## Related Packages

- **Roadbed.Common** — BaseClassWithLogging base class
- **Roadbed.Data** — Core data access abstractions
- **Roadbed.Data.Sqlite** — SQLite implementations
- **Roadbed.Data.Dapper** — Dapper configuration