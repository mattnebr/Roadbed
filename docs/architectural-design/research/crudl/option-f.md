# Option 6: Service Layer with Repository + Service Separation

## Core Philosophy

Introduce an explicit service layer into Roadbed.Crud so that consuming projects have
a standard place for business logic, validation, and orchestration — separate from
data access. The repository remains responsible for persistence mechanics only. The
service wraps the repository and adds behavior. This option explores whether
Roadbed.Crud should define service contracts and base classes, or if that belongs
solely to consuming projects.

## Industry Context

The service-repository separation is a well-established pattern with different names
across different architectural traditions:

| Tradition                    | Term for Business Logic         | Term for Data Access     | Source                                                 |
| ---------------------------- | ------------------------------- | ------------------------ | ------------------------------------------------------ |
| Fowler (PoEAA)               | Service Layer                   | Repository / Data Mapper | Patterns of Enterprise Application Architecture (2002) |
| Domain-Driven Design (Evans) | Application Service             | Repository               | Domain-Driven Design (2003)                            |
| Clean Architecture (Martin)  | Use Case / Interactor           | Gateway                  | Clean Architecture (2017)                              |
| CQRS                         | Command Handler / Query Handler | Repository               | Greg Young, Udi Dahan                                  |
| .NET MediatR pattern         | Request Handler                 | Repository               | Jimmy Bogard                                           |
| Generic .NET convention      | Service                         | Repository               | Microsoft documentation                                |

**Key distinction**: In all of these traditions, the repository is a thin data access
layer with no business logic. Business rules, validation, authorization, orchestration,
and cross-cutting concerns like logging and caching live in the layer above. The name
varies — "service", "use case", "interactor", "handler" — but the responsibility is
the same.

Roadbed.Crud uses **"Service"** because it is the most widely understood term in the
.NET ecosystem and does not carry the domain-specific baggage of DDD or Clean
Architecture terminology.

## Architecture Diagram
```
┌─────────────────────────────────────────────────────┐
│ Application Layer (Console, Web, etc.)              │
│                                                     │
│   Depends on: IFooService (from class library)      │
│   Does NOT depend on: IFooRepository                │
└────────────────────────┬────────────────────────────┘
                         │
                         │ IFooService (public interface)
                         │
┌────────────────────────▼────────────────────────────┐
│ Class Library (implements Roadbed.Crud)              │
│                                                     │
│   public interface IFooService                      │
│       : ICrudService<Foo, string>                   │
│                                                     │
│   internal class FooService                         │
│       : BaseService<Foo, string>                    │
│       Depends on: IFooRepository                    │
│       Contains: validation, business rules, caching │
│                                                     │
│   internal interface IFooRepository                 │ ← Note: internal
│       : ICrudRepository<Foo, string>                │
│                                                     │
│   internal class FooRepository                      │
│       : BaseRepository<Foo, string>                 │
│       Contains: pure data access                    │
│                                                     │
│   public class DataInstaller : IServiceCollectionInstaller
│       Registers both service and repository in DI   │
└─────────────────────────────────────────────────────┘
```

**Key change**: The repository interface becomes internal. The application layer only
sees the service interface. The service is the public API of the class library.

## Terminology

- **Entity**: Identity-bearing objects. Same as previous options.
- **Repository**: Pure data access. No business logic, no validation, no caching.
  Now internal to the class library.
- **Service**: Business logic, validation, orchestration. The public-facing contract
  that the application layer consumes.

## Naming Conventions

| Component | Naming Pattern | Example |
|---|---|---|
| Entity | `IEntity<TId>` | `Foo : IEntity<string>` |
| Repository operation | `I{Operation}Operation<T, TId>` | `IReadOperation<Foo, string>` |
| Repository composite | `ICrud{Suffix}<T, TId>` | `ICrudRepository<Foo, string>` |
| Repository base class | `BaseRepository<T, TId>` | `BaseRepository<Foo, string>` |
| Service operation | `I{Operation}ServiceOperation<T, TId>` | `IReadServiceOperation<Foo, string>` |
| Service composite | `ICrud{Suffix}<T, TId>` | `ICrudService<Foo, string>` |
| Service base class | `BaseService<T, TId>` | `BaseService<Foo, string>` |

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>
├── BaseEntity<TId>
├── Operations/
│   ├── ICreateOperation<T, TId>
│   ├── IReadOperation<T, TId>
│   ├── IUpdateOperation<T, TId>
│   ├── IDeleteOperation<T, TId>
│   └── IListOperation<T, TId>
├── Composites/
│   ├── ICrudRepository<T, TId>
│   ├── IReadOnlyRepository<T, TId>
│   ├── IWriteOnlyRepository<T, TId>
│   ├── IReadWriteRepository<T, TId>
│   └── ILookupRepository<T, TId>
├── Services/
│   ├── ICreateServiceOperation<T, TId>
│   ├── IReadServiceOperation<T, TId>
│   ├── IUpdateServiceOperation<T, TId>
│   ├── IDeleteServiceOperation<T, TId>
│   ├── IListServiceOperation<T, TId>
│   ├── ICrudService<T, TId>
│   ├── IReadOnlyService<T, TId>
│   └── IWriteOnlyService<T, TId>
├── BaseRepository<T, TId>
├── BaseService<T, TId>
```

## Interface Definitions

### Repository Layer (Same as Previous Options)

Operation interfaces and composites are identical to Options 4/5. Not repeated here.

### Service Operation Interfaces
```csharp
namespace Roadbed.Crud.Services;

/// <summary>
/// Defines the asynchronous Create operation at the service layer.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Service operations mirror repository operations but represent the business
/// logic boundary. Implementations may include validation, authorization,
/// caching, event publishing, or other cross-cutting concerns before delegating
/// to the repository.
/// </remarks>
public interface ICreateServiceOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Creates a new entity with business rule validation.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Read operation at the service layer.
/// </summary>
public interface IReadServiceOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier.</returns>
    Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Update operation at the service layer.
/// </summary>
public interface IUpdateServiceOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Updates an existing entity with business rule validation.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Delete operation at the service layer.
/// </summary>
public interface IDeleteServiceOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous List operation at the service layer.
/// </summary>
public interface IListServiceOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Lists all entities.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all entities.</returns>
    Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);
}
```

### Service Composite Interfaces
```csharp
namespace Roadbed.Crud.Services;

/// <summary>
/// Full CRUDL service contract.
/// </summary>
public interface ICrudService<TEntity, TId>
    : ICreateServiceOperation<TEntity, TId>,
      IReadServiceOperation<TEntity, TId>,
      IUpdateServiceOperation<TEntity, TId>,
      IDeleteServiceOperation<TEntity, TId>,
      IListServiceOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-only service contract providing Read and List operations.
/// </summary>
public interface IReadOnlyService<TEntity, TId>
    : IReadServiceOperation<TEntity, TId>,
      IListServiceOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only service contract providing Create, Update, and Delete operations.
/// </summary>
public interface IWriteOnlyService<TEntity, TId>
    : ICreateServiceOperation<TEntity, TId>,
      IUpdateServiceOperation<TEntity, TId>,
      IDeleteServiceOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

## Base Classes

### BaseRepository (Same as Option 5)

Uses non-generic `BaseClassWithLogging`, virtual method defaults. Identical to
Option 5's `BaseRepository<TEntity, TId>`. Not repeated here.

### BaseService
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Composites;
using Roadbed.Crud.Services;

/// <summary>
/// Base service with logging and virtual CRUDL method defaults that delegate
/// to an injected repository.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// The service layer sits between the application and the repository. Default
/// implementations delegate directly to the repository with no additional logic.
/// Override individual methods to add validation, authorization, caching,
/// logging enrichment, or other business rules.
/// </para>
/// <para>
/// The repository is injected as <see cref="ICrudRepository{TEntity, TId}"/>
/// which is the full composite. This allows the service to delegate any operation
/// regardless of which service composite the consuming project exposes.
/// </para>
/// </remarks>
public class BaseService<TEntity, TId>
    : BaseClassWithLogging,
      ICrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    /// <summary>
    /// Container for the protected property Repository.
    /// </summary>
    private readonly ICrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseService{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="repository">Repository for data access operations.</param>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;TYourService&gt;</c> from the
    /// concrete class to ensure the correct logger category name.
    /// </param>
    protected BaseService(
        ICrudRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the repository for data access operations.
    /// </summary>
    protected ICrudRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to the repository's CreateAsync.
    /// Override to add validation or business rules before creation.
    /// </remarks>
    public virtual async Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.CreateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to the repository's ReadAsync.
    /// Override to add caching, authorization, or enrichment.
    /// </remarks>
    public virtual async Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ReadAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to the repository's UpdateAsync.
    /// Override to add validation or business rules before update.
    /// </remarks>
    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to the repository's DeleteAsync.
    /// Override to add authorization or soft-delete logic.
    /// </remarks>
    public virtual async Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        await this._repository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to the repository's ListAsync.
    /// Override to add filtering, sorting, or result enrichment.
    /// </remarks>
    public virtual async Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ListAsync(cancellationToken);
    }

    #endregion Public Methods
}
```

## Consuming Project Examples

### Example 1: Simple Pass-Through (No Business Logic Yet)

When a consuming project has no business logic to add, the service delegates entirely
to the repository. The service still exists to establish the pattern — business logic
can be added later without changing the application layer.
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public sealed record Foo : IEntity<string>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

/// <summary>
/// Public service interface — this is what the application layer sees.
/// </summary>
public interface IFooService : ICrudService<Foo, string>
{
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;

/// <summary>
/// Internal repository interface — hidden from the application layer.
/// </summary>
internal interface IFooRepository : ICrudRepository<Foo, string>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

/// <summary>
/// Pure data access. No business logic.
/// </summary>
internal sealed class FooRepository
    : BaseRepository<Foo, string>,
      IFooRepository
{
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Foo: {Id}", entity.Id);
        // Pure data access — API call, DB query, file write, etc.
        throw new NotImplementedException();
    }

    public override async Task<Foo> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Foo: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<Foo> UpdateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Foo: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Deleting Foo: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<IList<Foo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Foo entities");
        throw new NotImplementedException();
    }
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Composites;

/// <summary>
/// Simple pass-through service. No business logic overrides needed yet.
/// All methods delegate to the repository via BaseService defaults.
/// </summary>
internal sealed class FooService
    : BaseService<Foo, string>,
      IFooService
{
    internal FooService(
        ICrudRepository<Foo, string> repository,
        ILogger<FooService> logger)
        : base(repository, logger)
    {
    }

    // No overrides needed — BaseService defaults delegate to the repository.
    // Business logic can be added later by overriding specific methods.
}
```

### Example 2: Service with Validation and Logging
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

public interface IBarService : ICrudService<Bar, int>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Composites;

/// <summary>
/// Bar service with validation on create and update.
/// </summary>
internal sealed class BarService
    : BaseService<Bar, int>,
      IBarService
{
    internal BarService(
        ICrudRepository<Bar, int> repository,
        ILogger<BarService> logger)
        : base(repository, logger)
    {
    }

    public override async Task<Bar> CreateAsync(
        Bar entity,
        CancellationToken cancellationToken = default)
    {
        // Business rule: Name is required
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);

        this.LogInformation("Creating Bar with Name: {Name}", entity.Name);

        // Delegate to repository for actual persistence
        return await base.CreateAsync(entity, cancellationToken);
    }

    public override async Task<Bar> UpdateAsync(
        Bar entity,
        CancellationToken cancellationToken = default)
    {
        // Business rule: Name is required
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);

        // Business rule: entity must exist before updating
        var existing = await this.Repository.ReadAsync(entity.Id!.Value, cancellationToken);

        this.LogInformation(
            "Updating Bar {Id}: Name '{OldName}' -> '{NewName}'",
            entity.Id,
            existing.Name,
            entity.Name);

        return await base.UpdateAsync(entity, cancellationToken);
    }

    public override async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        // Business rule: verify entity exists before deleting
        var existing = await this.Repository.ReadAsync(id, cancellationToken);

        this.LogInformation("Deleting Bar {Id}: {Name}", id, existing.Name);

        await base.DeleteAsync(id, cancellationToken);
    }

    // ReadAsync and ListAsync use the default pass-through from BaseService.
}
```

### Example 3: Read-Only Service (No Repository Mutations Exposed)
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

/// <summary>
/// Baz is reference data — read and list only at the service layer.
/// </summary>
public interface IBazService : IReadOnlyService<Baz, Guid>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Composites;

/// <summary>
/// Read-only service. The BaseService defaults for Create, Update, Delete
/// exist but are unreachable because IBazService only exposes Read and List.
/// </summary>
internal sealed class BazService
    : BaseService<Baz, Guid>,
      IBazService
{
    internal BazService(
        ICrudRepository<Baz, Guid> repository,
        ILogger<BazService> logger)
        : base(repository, logger)
    {
    }

    // ReadAsync and ListAsync are inherited from BaseService and delegate
    // to the repository. No overrides needed.
}
```

### Example 4: Service with Custom Operations
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

/// <summary>
/// Qux service with a custom operation beyond standard CRUDL.
/// </summary>
public interface IQuxService : ICrudService<Qux, long>
{
    /// <summary>
    /// Archives all Qux entities older than the specified date.
    /// </summary>
    /// <param name="olderThan">Cutoff date for archiving.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Number of entities archived.</returns>
    Task<int> ArchiveAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;

/// <summary>
/// Qux repository with a custom query for the archive operation.
/// </summary>
internal interface IQuxRepository : ICrudRepository<Qux, long>
{
    /// <summary>
    /// Returns all Qux entities with a CreatedAt before the specified date.
    /// </summary>
    Task<IList<Qux>> ListOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Composites;

internal sealed class QuxService
    : BaseService<Qux, long>,
      IQuxService
{
    private readonly IQuxRepository _quxRepository;

    internal QuxService(
        IQuxRepository repository,
        ILogger<QuxService> logger)
        : base(repository, logger)
    {
        // Keep a typed reference for custom repository methods
        this._quxRepository = repository;
    }

    public async Task<int> ArchiveAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        this.LogInformation("Archiving Qux entities older than {Date}", olderThan);

        var entitiesToArchive = await this._quxRepository
            .ListOlderThanAsync(olderThan, cancellationToken);

        int count = 0;
        foreach (var entity in entitiesToArchive)
        {
            await this.Repository.DeleteAsync(entity.Id!.Value, cancellationToken);
            count++;
        }

        this.LogInformation("Archived {Count} Qux entities", count);
        return count;
    }
}
```

### DI Registration
```csharp
namespace MyApp.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class DataInstaller : IServiceCollectionInstaller
{
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Foo: simple pass-through
        services.AddSingleton<IFooRepository, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Bar: service with validation
        services.AddSingleton<IBarRepository, BarRepository>();
        services.AddSingleton<IBarService, BarService>();

        // Baz: read-only
        services.AddSingleton<IBazRepository, BazRepository>();
        services.AddSingleton<IBazService, BazService>();

        // Qux: custom operations (register as typed for custom methods)
        services.AddSingleton<IQuxRepository, QuxRepository>();
        services.AddSingleton<IQuxService, QuxService>();
    }
}
```

## The BaseService Constructor Problem

### The Issue

`BaseService` requires `ICrudRepository<TEntity, TId>` (the full composite). But what
if the consuming project's repository only implements `IReadOnlyRepository`? The DI
container cannot resolve `ICrudRepository` from a class that only implements
`IReadOnlyRepository`.

### Solutions

**Solution A — Always register repository as ICrudRepository** (chosen in this option):

The repository implementation always inherits from `BaseRepository`, which implements
`ICrudRepository`. Even if the consuming interface is `IReadOnlyRepository`, the
concrete class IS-A `ICrudRepository` because `BaseRepository` implements it. The
`DataInstaller` registers the concrete class against `ICrudRepository`:
```csharp
// BazRepository inherits from BaseRepository which implements ICrudRepository.
// Even though IBazRepository only extends IReadOnlyRepository, the concrete
// class satisfies ICrudRepository because of BaseRepository.
services.AddSingleton<ICrudRepository<Baz, Guid>, BazRepository>();
services.AddSingleton<IBazService, BazService>();
```

The unreachable Create/Update/Delete methods on `BaseRepository` throw
`NotImplementedException` if somehow called — but the read-only service interface
prevents that from happening.

**Solution B — Accept the narrower interface in BaseService** (alternative):

Overload `BaseService` constructors to accept narrower repository interfaces. This
is more type-safe but requires multiple constructors or a factory pattern. Not explored
further in this option but noted as an alternative.

## Interface and Class Count Summary

| Component                        | Count                 |
| -------------------------------- | --------------------- |
| Repository operation interfaces  | 5                     |
| Repository composite interfaces  | 5                     |
| Service operation interfaces     | 5                     |
| Service composite interfaces     | 3                     |
| Base repository class            | 1                     |
| Base service class               | 1                     |
| Core entity interface            | 1                     |
| Non-generic BaseClassWithLogging | 1 (in Roadbed.Common) |
| **Total Roadbed.Crud types**     | **21**                |

## Layer Responsibility Summary

| Concern                                | Repository | Service           |
| -------------------------------------- | ---------- | ----------------- |
| Data access (SQL, HTTP, file I/O)      | ✅          | ❌                 |
| Business rule validation               | ❌          | ✅                 |
| Input validation (null checks, ranges) | ❌          | ✅                 |
| Authorization                          | ❌          | ✅                 |
| Caching                                | ❌          | ✅                 |
| Logging (data operations)              | ✅          | ❌                 |
| Logging (business operations)          | ❌          | ✅                 |
| Event publishing                       | ❌          | ✅                 |
| Orchestration (multi-step)             | ❌          | ✅                 |
| Custom query methods                   | ✅          | Delegates to repo |

## Pros

- **Clear separation of concerns** — business logic cannot leak into the repository;
  data access cannot leak into the service
- **Repository becomes internal** — the application layer only depends on the service
  interface, reducing coupling and surface area
- **Base service provides pass-through defaults** — for entities with no business
  logic, the service class can be empty; all CRUDL operations delegate to the
  repository automatically
- **Incremental business logic** — start with a pass-through service and add
  validation, caching, or authorization later by overriding individual methods
- **Custom operations compose naturally** — the service interface can define custom
  methods (like `ArchiveAsync`) while inheriting standard CRUDL from the composite
- **Two logger categories** — `FooService` and `FooRepository` appear separately
  in log output, making it easy to filter business logic logs vs data access logs
- **Industry-standard pattern** — the service-repository separation is widely
  documented and understood across the .NET ecosystem

## Cons

- **Double the types** — every entity now requires a repository interface, repository
  class, service interface, and service class (4 types per entity, plus the entity
  itself). For projects with many entities, this is a significant amount of code
- **Double the DI registrations** — every entity requires two registrations (service
  and repository) in the `IServiceCollectionInstaller`
- **Pass-through services feel like boilerplate** — when a service has no business
  logic, the empty class feels wasteful. Developers may question why the service
  layer exists at all until they need it
- **BaseService requires ICrudRepository** — the constructor demands the full
  composite even for read-only services. This works because `BaseRepository`
  implements `ICrudRepository`, but it feels mismatched
- **Consuming project complexity** — the consuming class library must now manage
  two layers (repository + service), two interfaces per entity, and two registrations.
  This is more complex than Options 1-5
- **No enforcement that repositories stay internal** — it's a convention, not a
  compiler constraint. A developer could make `IFooRepository` public by accident
- **Service operation interfaces mirror repository operations exactly** — the method
  signatures (`CreateAsync`, `ReadAsync`, etc.) are identical. This feels duplicative.
  Some architects would argue a single set of operation interfaces should be shared
- **21 types in Roadbed.Crud** — significantly more than Option 5's 13 types

## Open Questions This Option Raises

1. **Should service and repository operation interfaces be the same?** The method
   signatures are identical (`CreateAsync`, `ReadAsync`, etc.). Should there be one
   set of operation interfaces shared by both layers, or do separate interfaces
   add meaningful clarity?
2. **Is the service layer always necessary?** For simple data sources (e.g., a CSV
   file in Roadbed.IO), the service layer adds overhead with no benefit. Should
   Roadbed.Crud make the service layer optional by continuing to support direct
   repository usage from previous options?
3. **Should `BaseService` be abstract?** Like `BaseRepository`, it has no abstract
   members. The `abstract` keyword would prevent direct instantiation, which is
   desirable.
4. **How should the consuming project handle custom repository methods?** Example 4
   shows the service keeping a typed `_quxRepository` reference alongside the base
   class's `Repository` property. Is this the right pattern, or should there be a
   generic mechanism for this?
5. **Should Roadbed.Crud provide a source generator or template?** Given the 4-types-
   per-entity boilerplate, would a Roslyn source generator or dotnet template reduce
   the cost of the service layer?


