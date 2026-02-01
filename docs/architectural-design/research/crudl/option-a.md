# Option 1: Minimal Async-First (Baseline)

## Core Philosophy

The simplest possible architecture. Async-only interfaces with thin base abstract
classes. No service layer. No filtering or pagination. This serves as the baseline
to compare all other options against. The premise is that Roadbed.Crud should define
the absolute minimum contract and push all decisions to consuming libraries.

## Terminology

- **Entity**: Renamed from "Data Transfer Object (DTO)". Rationale: every object has
  an `Id`, which is an identity concept. DTOs are typically dumb data carriers with no
  identity. Since Roadbed.Crud enforces `Id`, the objects are entities by definition.
  The term "entity" is also agnostic — it doesn't imply database, API, or file origin.
- **Repository**: The data access abstraction. Kept as-is.
- **No service layer**: Consuming projects decide if they need one.

## Naming Conventions

| Current Name | Proposed Name | Rationale |
|---|---|---|
| `IDataTransferObject<T>` | `IEntity<T>` | Identity-bearing objects are entities |
| `IRepositoryOperationRead<T, TId>` | `IReadOperation<T, TId>` | Shorter, removes redundant "Repository" prefix since context is obvious |
| `IRepositoryOperationCreate<T, TId>` | `ICreateOperation<T, TId>` | Same |
| `IRepositoryOperationUpdate<T, TId>` | `IUpdateOperation<T, TId>` | Same |
| `IEntityOperationDelete<T, TId>` | `IDeleteOperation<T, TId>` | Consistent prefix pattern |
| `IRepositoryOperationList<T, TId>` | `IListOperation<T, TId>` | Same |
| `IBaseRepositoryWithCrud<T, TId>` | `ICrudRepository<T, TId>` | Concise composite name |
| `BaseDataTransferObject<T>` | `BaseEntity<T>` | Matches interface rename |

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>                          # Core identity interface
├── BaseEntity<TId>                       # Base implementation
├── Operations/
│   ├── ICreateOperation<T, TId>          # Individual operation interfaces
│   ├── IReadOperation<T, TId>
│   ├── IUpdateOperation<T, TId>
│   ├── IDeleteOperation<T, TId>
│   └── IListOperation<T, TId>
├── ICrudRepository<T, TId>               # Full CRUD+L composite
├── BaseRepository<T, TId>                # Abstract base with logging
```

## Interface Definitions

### Core Entity Interface
```csharp
namespace Roadbed.Crud;

/// <summary>
/// Defines an entity with an identifier.
/// </summary>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Gets the identifier of the entity.
    /// </summary>
    TId? Id { get; }
}
```

**Note**: The `Errors` property from `IDataTransferObject<T>` is removed. Error
handling is a cross-cutting concern that belongs to consuming libraries (e.g.,
`ApiResponse` in Roadbed.NET). This keeps the entity interface focused on identity.

### Individual Operation Interfaces
```csharp
namespace Roadbed.Crud.Operations;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ICreateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
public interface IReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier.</returns>
    Task<TEntity> ReadAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
public interface IUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
public interface IDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
public interface IListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Lists all entities.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all entities.</returns>
    Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
```

### Composite Interface
```csharp
namespace Roadbed.Crud;

using Roadbed.Crud.Operations;

/// <summary>
/// Composite contract for Create, Read, Update, Delete, and List operations.
/// </summary>
public interface ICrudRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

## Base Abstract Class
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Operations;

/// <summary>
/// Base abstract repository with logging support for all CRUDL operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseRepository<TEntity, TId>
    : BaseClassWithLogging<BaseRepository<TEntity, TId>>,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    protected BaseRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
```

## Sync/Async Strategy

**This option does NOT address sync/async duality.** All interfaces and base classes
are async-only. Consuming projects that need synchronous access would need to:

- Call `.Result` or `.GetAwaiter().GetResult()` (not recommended)
- Create their own sync wrappers

This is a known limitation of this option and is intentional — it keeps the library
surface area minimal.

## Consuming Project Example

### Entity Definition
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

### Repository Interface (in consuming class library)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public interface IFooRepository : ICrudRepository<Foo, string>
{
}
```

### Internal Repository Implementation
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class FooRepository
    : BaseRepository<Foo, string>,
      IFooRepository
{
    internal FooRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    public override async Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Foo: {Id}", entity.Id);
        // Implementation specific to data source
        throw new NotImplementedException();
    }

    public override async Task<Foo> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Foo: {Id}", id);
        // Implementation specific to data source
        throw new NotImplementedException();
    }

    public override async Task<Foo> UpdateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Foo: {Id}", entity.Id);
        // Implementation specific to data source
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Deleting Foo: {Id}", id);
        // Implementation specific to data source
        throw new NotImplementedException();
    }

    public override async Task<IList<Foo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Foo entities");
        // Implementation specific to data source
        throw new NotImplementedException();
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
        services.AddSingleton<IFooRepository, FooRepository>();
    }
}
```

## Pros

- Absolute minimum surface area — easy to understand
- No decisions forced on consuming projects
- Clean rename from DTO to Entity grounds the identity concept
- Errors removed from base entity — keeps it focused
- Familiar repository pattern — low learning curve
- Composes well with any data source (API, file, database)

## Cons

- **No sync support** — consuming projects must work around this or use async everywhere
- **No filtering or pagination** — `ListAsync` returns everything; no query capability
- **No service layer** — no guidance on where business logic lives
- **No transaction support** — no coordination across repositories
- **No validation hooks** — consuming projects must roll their own
- **Single base class** — `BaseRepository` implements all 5 operations; no way to
  inherit for a subset (e.g., read-only repository)
- **Errors property removed** — consuming projects that relied on it need adjustment
- **BaseClassWithLogging generic parameter** — `BaseRepository<TEntity, TId>` becomes
  the logger category, which produces log entries like
  `BaseRepository<Foo, String>` instead of `FooRepository`

