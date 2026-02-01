# Option 9: Hybrid — Shared Operations + Marker Interface + Optional Service Layer

## Core Philosophy

Combine the best of Options 7 and 8 into a single cohesive architecture. Shared
operation interfaces provide reusability and consistency. Composite interfaces provide
convenience for common patterns. Marker interfaces enable assembly scanning. The
service layer is available but optional. Consuming projects choose their level of
engagement: use composites for speed, cherry-pick operations for precision, or add
custom methods alongside standard ones — all within the same system.

This option prioritizes reusable code and convention over minimalism. Unused
boilerplate in the base class is an acceptable trade-off for consistency across
consuming projects.

## Design Principles

1. **Operation interfaces are the atoms** — every CRUDL method signature is defined
   once, in one place, and reused everywhere
2. **Composites are convenience molecules** — pre-built groupings for common patterns;
   not required but recommended
3. **Marker interfaces enable tooling** — assembly scanning, generic constraints,
   and discoverability
4. **One base class serves all patterns** — virtual defaults mean consuming classes
   override only what they need
5. **Service layer is opt-in** — consuming projects add it when business logic
   warrants it; repositories work standalone when it doesn't
6. **Custom methods coexist with standard ones** — a consuming interface can inherit
   from a composite AND declare additional methods on the same interface

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>                              # Core entity contract
├── BaseEntity<TId>                           # Base entity implementation
├── IRepository                               # Non-generic marker
├── IRepository<TEntity, TId>                 # Generic marker
├── Operations/
│   ├── ICreateOperation<T, TId>              # Atomic operation interfaces
│   ├── IReadOperation<T, TId>
│   ├── IUpdateOperation<T, TId>
│   ├── IDeleteOperation<T, TId>
│   └── IListOperation<T, TId>
├── Repositories/
│   ├── ICrudRepository<T, TId>               # Full CRUDL composite
│   ├── IReadOnlyRepository<T, TId>           # Read + List
│   ├── IWriteOnlyRepository<T, TId>          # Create + Update + Delete
│   ├── IReadWriteRepository<T, TId>          # CRUD without List
│   └── ILookupRepository<T, TId>             # Read only
├── Services/
│   ├── ICrudService<T, TId>                  # Full CRUDL service composite
│   ├── IReadOnlyService<T, TId>              # Read + List service
│   └── IWriteOnlyService<T, TId>             # Create + Update + Delete service
├── BaseRepository<T, TId>                    # Single base for all repositories
├── BaseService<T, TId>                       # Single base for all services
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

### Marker Interfaces
```csharp
namespace Roadbed.Crud;

/// <summary>
/// Non-generic marker interface identifying a class as a Roadbed.Crud repository.
/// </summary>
/// <remarks>
/// Enables assembly scanning for automatic DI registration and generic constraints
/// in utility classes. Carries no methods.
/// </remarks>
public interface IRepository
{
}

/// <summary>
/// Generic marker interface identifying a repository for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IRepository<TEntity, TId> : IRepository
    where TEntity : IEntity<TId>
{
}
```

### Shared Operation Interfaces
```csharp
namespace Roadbed.Crud.Operations;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Shared between repository and service layers. Both
/// <see cref="Repositories.ICrudRepository{TEntity, TId}"/> and
/// <see cref="Services.ICrudService{TEntity, TId}"/> inherit from this interface.
/// </remarks>
public interface ICreateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
public interface IReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier.</returns>
    Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
public interface IUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
public interface IDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
public interface IListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Lists all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all entities.</returns>
    Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);
}
```

### Repository Composite Interfaces
```csharp
namespace Roadbed.Crud.Repositories;

using Roadbed.Crud.Operations;

/// <summary>
/// Full CRUDL repository contract for data access operations.
/// </summary>
public interface ICrudRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-only repository contract providing Read and List operations.
/// </summary>
public interface IReadOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Lookup repository contract providing only the Read operation.
/// </summary>
public interface ILookupRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only repository contract providing Create, Update, and Delete operations.
/// </summary>
public interface IWriteOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ICreateOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-write repository contract providing CRUD without List.
/// </summary>
public interface IReadWriteRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

**Note**: Every repository composite now also inherits from `IRepository<TEntity, TId>`,
which means the marker interface is automatically present on any consuming interface
that uses a composite. No extra inheritance step needed.

### Service Composite Interfaces
```csharp
namespace Roadbed.Crud.Services;

using Roadbed.Crud.Operations;

/// <summary>
/// Full CRUDL service contract for business logic operations.
/// </summary>
public interface ICrudService<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-only service contract.
/// </summary>
public interface IReadOnlyService<TEntity, TId>
    : IReadOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only service contract.
/// </summary>
public interface IWriteOnlyService<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

## Base Classes

### BaseRepository
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories;

/// <summary>
/// Base repository with logging and virtual CRUDL method defaults.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Implements <see cref="ICrudRepository{TEntity, TId}"/> so that it satisfies
/// any composite interface (read-only, write-only, etc.) since all composites
/// are subsets of the full CRUDL set. Consuming classes override only the methods
/// required by their repository interface.
/// </para>
/// <para>
/// Inherits from <see cref="BaseClassWithLogging"/> (non-generic) for logging
/// convenience methods. The consuming class injects <c>ILogger&lt;T&gt;</c> to
/// ensure the correct logger category name.
/// </para>
/// </remarks>
public class BaseRepository<TEntity, TId>
    : BaseClassWithLogging,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseRepository{TEntity, TId}"/> class with no logging.
    /// </summary>
    protected BaseRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;TYourRepository&gt;</c> from the
    /// concrete class to ensure the correct logger category name.
    /// </param>
    protected BaseRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public virtual Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion Public Methods
}
```

### BaseService
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories;
using Roadbed.Crud.Services;

/// <summary>
/// Base service with logging and virtual CRUDL defaults that delegate
/// to an injected repository.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public class BaseService<TEntity, TId>
    : BaseClassWithLogging,
      ICrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

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
        this._repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
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
    public virtual async Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.CreateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ReadAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        await this._repository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ListAsync(cancellationToken);
    }

    #endregion Public Methods
}
```

## Three Ways to Consume (Choose Your Level)

### Level 1: Composite Interface (Fastest Path)

Use a pre-built composite. Best for standard CRUDL patterns.
```csharp
// Pick a composite that matches your needs
public interface IFooRepository : ICrudRepository<Foo, string> { }
public interface IBarRepository : IReadOnlyRepository<Bar, int> { }
```

### Level 2: Cherry-Pick Operations (Precise Control)

Compose from individual operation interfaces. Best for non-standard combinations.
```csharp
// Append-only: Create + Read + List (no composite matches this)
public interface IBazRepository
    : IRepository<Baz, Guid>,
      ICreateOperation<Baz, Guid>,
      IReadOperation<Baz, Guid>,
      IListOperation<Baz, Guid>
{
}
```

**Note**: When cherry-picking, include `IRepository<TEntity, TId>` manually for
the marker interface. Composites include it automatically.

### Level 3: Composite + Custom Methods (Extend Standard)

Start with a composite and add custom methods. Best for entities that need
both standard CRUDL and domain-specific queries.
```csharp
// Full CRUDL plus custom date range query
public interface IQuxRepository : ICrudRepository<Qux, long>
{
    /// <summary>
    /// Lists Qux entities created within the specified date range.
    /// </summary>
    Task<IList<Qux>> ListByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists Qux entities matching the specified status.
    /// </summary>
    Task<IList<Qux>> ListByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);
}
```

## Consuming Project Examples

### Example 1: Full CRUDL with Service
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

public interface IFooService : ICrudService<Foo, string>
{
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories;

internal interface IFooRepository : ICrudRepository<Foo, string>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

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
using Roadbed.Crud.Repositories;

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

    public override async Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);
        this.LogInformation("Creating Foo: {Name}", entity.Name);
        return await base.CreateAsync(entity, cancellationToken);
    }
}
```

### Example 2: Read-Only Repository, No Service
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories;

public interface IBarRepository : IReadOnlyRepository<Bar, int>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class BarRepository
    : BaseRepository<Bar, int>,
      IBarRepository
{
    internal BarRepository(ILogger<BarRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Bar> ReadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Bar: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<IList<Bar>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Bar entities");
        throw new NotImplementedException();
    }
}
```

### Example 3: Cherry-Picked Operations (Append-Only)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Operations;

public interface IBazRepository
    : IRepository<Baz, Guid>,
      ICreateOperation<Baz, Guid>,
      IReadOperation<Baz, Guid>,
      IListOperation<Baz, Guid>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class BazRepository
    : BaseRepository<Baz, Guid>,
      IBazRepository
{
    internal BazRepository(ILogger<BazRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Baz> CreateAsync(
        Baz entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Baz: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task<Baz> ReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Baz: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<IList<Baz>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Baz entities");
        throw new NotImplementedException();
    }
}
```

### Example 4: Composite + Custom Methods + Service
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

public interface IQuxService : ICrudService<Qux, long>
{
    Task DeactivateAsync(long id, CancellationToken cancellationToken = default);
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories;

internal interface IQuxRepository : ICrudRepository<Qux, long>
{
    Task<IList<Qux>> ListByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        long id,
        string status,
        CancellationToken cancellationToken = default);
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class QuxRepository
    : BaseRepository<Qux, long>,
      IQuxRepository
{
    internal QuxRepository(ILogger<QuxRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Qux> CreateAsync(
        Qux entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Qux: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task<Qux> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Qux: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<Qux> UpdateAsync(
        Qux entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IList<Qux>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<Qux>> ListByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing Qux by status: {Status}", status);
        throw new NotImplementedException();
    }

    public async Task UpdateStatusAsync(
        long id,
        string status,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Qux {Id} status to: {Status}", id, status);
        throw new NotImplementedException();
    }
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Repositories;

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
        this._quxRepository = repository;
    }

    public async Task DeactivateAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var existing = await this.Repository.ReadAsync(id, cancellationToken);
        this.LogInformation("Deactivating Qux {Id}: {Name}", id, existing.Name);
        await this._quxRepository.UpdateStatusAsync(
            id, "Inactive", cancellationToken);
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
        // Foo: service + repository
        services.AddSingleton<ICrudRepository<Foo, string>, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Bar: repository only, no service
        services.AddSingleton<IBarRepository, BarRepository>();

        // Baz: cherry-picked operations, no service
        services.AddSingleton<IBazRepository, BazRepository>();

        // Qux: custom methods + service
        services.AddSingleton<IQuxRepository, QuxRepository>();
        services.AddSingleton<ICrudRepository<Qux, long>>(sp =>
            sp.GetRequiredService<IQuxRepository>());
        services.AddSingleton<IQuxService, QuxService>();
    }
}
```

## Interface and Class Count Summary

| Component | Count |
|---|---|
| Core entity interface | 1 |
| Marker interfaces | 2 |
| Shared operation interfaces | 5 |
| Repository composite interfaces | 5 |
| Service composite interfaces | 3 |
| Base repository class | 1 |
| Base service class | 1 |
| Non-generic BaseClassWithLogging | 1 (in Roadbed.Common) |
| **Total Roadbed.Crud types** | **18** |

## Consuming Interface Decision Tree
```
Do you need standard CRUDL operations?
├── Yes, all five → Use ICrudRepository<T, TId>
├── Yes, a common subset
│   ├── Read + List → Use IReadOnlyRepository<T, TId>
│   ├── Read only → Use ILookupRepository<T, TId>
│   ├── Create + Update + Delete → Use IWriteOnlyRepository<T, TId>
│   └── CRUD without List → Use IReadWriteRepository<T, TId>
├── Yes, an uncommon subset → Cherry-pick: IRepository<T, TId> + individual operations
└── No, only custom methods → Use IRepository<T, TId> and declare methods directly

Do you need custom methods alongside standard CRUDL?
├── Yes → Inherit from composite + declare custom methods on your interface
└── No → Just use the composite

Do you need a service layer?
├── Yes → Create IFooService : ICrudService<T, TId> (or subset)
│         Create FooService : BaseService<T, TId>
│         Make the repository interface internal
└── No → Make the repository interface public
         Application layer depends on it directly
```

## Comparison Across All Options

| Aspect | Opt 1 | Opt 5 | Opt 7 | Opt 8 | **Opt 9** |
|---|---|---|---|---|---|
| Roadbed.Crud types | 8 | 13 | 16 | 4 | **18** |
| Operation interfaces | 5 | 5 | 5 | 0 | **5** |
| Composite interfaces | 1 | 5 | 8 | 0 | **8** |
| Marker interfaces | 0 | 0 | 0 | 2 | **2** |
| Base classes | 1 | 1 | 2 | 1 | **2** |
| Service layer | No | No | Optional | No | **Optional** |
| Custom methods | Separate interface | Separate interface | Same interface | Same interface | **Same interface** |
| Assembly scanning | No | No | No | Yes | **Yes** |
| Polymorphism | Yes | Yes | Yes | No | **Yes** |
| Consuming levels | 1 | 1 | 2 | 1 | **3** |

## Pros

- **Three levels of engagement** — composites for speed, cherry-pick for precision,
  marker + custom for freedom. Consuming projects choose what fits
- **Shared operation interfaces** — method signatures defined once, shared between
  repository and service layers. No duplication
- **Marker interfaces included** — `IRepository` enables assembly scanning for DI;
  composites include it automatically; cherry-pick requires manual inclusion
- **Custom methods are first-class** — declared alongside CRUDL on the same consuming
  interface with no friction (Level 3)
- **Service layer is truly optional** — Examples 2 and 3 show repository-only usage;
  Examples 1 and 4 show service + repository. Same Roadbed.Crud library serves both
- **Interface polymorphism preserved** — code can depend on `IReadOperation<Foo, string>`
  to accept either a repository or a service
- **Composites include markers automatically** — consuming projects that use
  `ICrudRepository` get `IRepository` for free; no extra inheritance needed
- **Consistent logging** — non-generic `BaseClassWithLogging` with correct category
  names via `ILogger<T>` injection
- **Single base class per layer** — one `BaseRepository`, one `BaseService`. Virtual
  defaults eliminate the need for `BaseReadOnlyRepository`, etc.

## Cons

- **18 types in Roadbed.Crud** — the most of any option. Some teams may feel this is
  too many types for a CRUDL abstraction library
- **Three consumption levels add cognitive load** — new developers must understand
  when to use composites vs cherry-pick vs marker-only. The decision tree helps but
  adds to onboarding
- **Virtual defaults still lack compile-time safety** — same concern as Option 5;
  forgetting to override a method results in `NotImplementedException` at runtime
- **BaseService requires ICrudRepository** — same constructor issue as Options 6-7
- **DI forwarding for custom repositories** — when a service needs a typed repository
  (Example 4), double-registration is needed. This is an inherent complexity of
  having both a typed and composite registration
- **Marker interface is redundant for composite users** — developers using
  `ICrudRepository` never interact with `IRepository` directly; it exists silently
  in the inheritance chain
- **Service composites fewer than repository composites** — 3 service composites vs
  5 repository composites. If a consuming project needs `IReadWriteService` or
  `ILookupService`, they must cherry-pick operations themselves

## Open Questions This Option Raises

1. **Are 18 types justified?** The library serves 11 NuGet packages across multiple
   solutions. More types means more reusable patterns, but also more to maintain.
   Is the surface area proportional to the value?
2. **Should the three consumption levels be documented in a Roadbed.Crud README?**
   The decision tree above could be part of the NuGet package documentation to help
   consuming developers choose the right level.
3. **Should service composites mirror repository composites exactly?** Adding
   `IReadWriteService`, `ILookupService` would bring service composites to 5 (matching
   repository composites) but adds 2 more types. Is the symmetry worth it?
4. **Should `BaseService` accept a narrower repository interface?** An overload that
   takes `IReadOnlyRepository` for read-only services would eliminate the mismatch
   but adds constructor complexity.
5. **Is the marker interface pulling its weight?** If assembly scanning is rarely
   used, the marker interfaces add 2 types for minimal benefit. Would an
   `[Repository]` attribute be more idiomatic?