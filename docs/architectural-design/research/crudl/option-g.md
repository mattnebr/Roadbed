# Option 7: Shared Operation Interfaces Across Layers

## Core Philosophy

Address the biggest criticism of Option 6: the service and repository operation
interfaces have identical method signatures, making the service-specific interfaces
feel like pure duplication. This option uses a single set of operation interfaces
that both the repository and service layers implement. The distinction between
service and repository is expressed through composite interface naming and the
consuming project's architecture — not through duplicate interface hierarchies.

## The Duplication Problem (Option 6)
```csharp
// Option 6: Two interfaces with identical signatures
namespace Roadbed.Crud.Operations;
public interface ICreateOperation<TEntity, in TId>
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
}

namespace Roadbed.Crud.Services;
public interface ICreateServiceOperation<TEntity, in TId>
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
}
// These are structurally identical. The only difference is the namespace and name.
```

## The Solution (This Option)

One set of operation interfaces. Both repository and service composites inherit from
the same operations. The layer distinction is in the composite name, not the operation
interface.
```
                    ICreateOperation<T, TId>
                   /                        \
                  /                          \
 ICrudRepository<T, TId>           ICrudService<T, TId>
         |                                 |
  BaseRepository<T, TId>           BaseService<T, TId>
         |                                 |
  FooRepository                      FooService
```

Both `ICrudRepository` and `ICrudService` include `ICreateOperation`. The consuming
project's `FooService` and `FooRepository` both implement `CreateAsync` from the
same interface. The method signature is defined once.

## Terminology

- **Entity**: Identity-bearing objects. Same as previous options.
- **Repository**: Pure data access. Internal to the class library.
- **Service**: Business logic wrapper. Public-facing contract.
- **Operation**: A single CRUDL action. Shared between layers.

## Naming Conventions

| Component            | Pattern                    | Example                         |
| -------------------- | -------------------------- | ------------------------------- |
| Operation interface  | `I{Verb}Operation<T, TId>` | `ICreateOperation<Foo, string>` |
| Repository composite | `ICrud{Layer}<T, TId>`     | `ICrudRepository<Foo, string>`  |
| Service composite    | `ICrud{Layer}<T, TId>`     | `ICrudService<Foo, string>`     |
| Repository base      | `BaseRepository<T, TId>`   | `BaseRepository<Foo, string>`   |
| Service base         | `BaseService<T, TId>`      | `BaseService<Foo, string>`      |

The word after `ICrud` identifies the layer: `Repository` or `Service`. The operation
interfaces carry no layer indicator because they are shared.

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>
├── BaseEntity<TId>
├── Operations/
│   ├── ICreateOperation<T, TId>          # Shared by both layers
│   ├── IReadOperation<T, TId>
│   ├── IUpdateOperation<T, TId>
│   ├── IDeleteOperation<T, TId>
│   └── IListOperation<T, TId>
├── Repositories/
│   ├── ICrudRepository<T, TId>           # Full CRUDL composite
│   ├── IReadOnlyRepository<T, TId>       # Read + List
│   ├── IWriteOnlyRepository<T, TId>      # Create + Update + Delete
│   ├── IReadWriteRepository<T, TId>      # CRUD without List
│   └── ILookupRepository<T, TId>         # Read only
├── Services/
│   ├── ICrudService<T, TId>              # Full CRUDL composite
│   ├── IReadOnlyService<T, TId>          # Read + List
│   └── IWriteOnlyService<T, TId>         # Create + Update + Delete
├── BaseRepository<T, TId>
├── BaseService<T, TId>
```

## Interface Definitions

### Shared Operation Interfaces

These are defined once and used by both layers.
```csharp
namespace Roadbed.Crud.Operations;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// This interface is shared between repository and service layers. Both
/// <see cref="Repositories.ICrudRepository{TEntity, TId}"/> and
/// <see cref="Services.ICrudService{TEntity, TId}"/> inherit from this
/// interface. The layer distinction is in the composite, not the operation.
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
    : ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-only repository contract.
/// </summary>
public interface IReadOnlyRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Lookup repository contract providing only Read.
/// </summary>
public interface ILookupRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only repository contract.
/// </summary>
public interface IWriteOnlyRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-write repository contract (CRUD without List).
/// </summary>
public interface IReadWriteRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Service Composite Interfaces
```csharp
namespace Roadbed.Crud.Services;

using Roadbed.Crud.Operations;

/// <summary>
/// Full CRUDL service contract for business logic operations.
/// </summary>
/// <remarks>
/// Inherits the same operation interfaces as <see cref="Repositories.ICrudRepository{TEntity, TId}"/>.
/// The distinction is semantic: a repository is pure data access; a service adds
/// business logic, validation, and orchestration.
/// </remarks>
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

Identical to Option 5. Non-generic `BaseClassWithLogging`, virtual defaults. Uses
`Roadbed.Crud.Repositories` namespace for the composite.
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories;

/// <summary>
/// Base repository with logging and virtual CRUDL method defaults.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
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
/// Base service with logging and virtual CRUDL method defaults that delegate
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

## The Interesting Consequence: Interface Polymorphism

Because both layers share the same operation interfaces, code that depends on
`IReadOperation<Foo, string>` can accept either a repository or a service. This
enables interesting patterns:
```csharp
// A utility class that reads entities — doesn't care if the source is a
// repository or a service.
public sealed class FooCacheWarmer
{
    private readonly IReadOperation<Foo, string> _reader;
    private readonly IListOperation<Foo, string> _lister;

    // Could be injected with either FooRepository OR FooService
    public FooCacheWarmer(
        IReadOperation<Foo, string> reader,
        IListOperation<Foo, string> lister)
    {
        this._reader = reader;
        this._lister = lister;
    }
}
```

This is a benefit of shared operations: consuming code can program against the
operation level without caring about the layer.

## Consuming Project Examples

### Example 1: Full CRUDL with Service (No Business Logic Yet)
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
}
```

### Example 2: No Service Layer (Repository-Only)

The service layer is optional. When a consuming project has no business logic
to add and wants to keep things simple, the application layer can depend directly
on the repository interface. The shared operation interfaces make this seamless.
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories;

/// <summary>
/// Bar has no business logic — the application layer consumes the
/// repository interface directly.
/// </summary>
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

### Example 3: Service with Validation
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

public interface IBazService : ICrudService<Baz, Guid>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;
using Roadbed.Crud.Repositories;

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

    public override async Task<Baz> CreateAsync(
        Baz entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);
        this.LogInformation("Creating Baz: {Name}", entity.Name);
        return await base.CreateAsync(entity, cancellationToken);
    }

    public override async Task<Baz> UpdateAsync(
        Baz entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);

        var existing = await this.Repository.ReadAsync(
            entity.Id!.Value, cancellationToken);

        this.LogInformation(
            "Updating Baz {Id}: '{OldName}' -> '{NewName}'",
            entity.Id,
            existing.Name,
            entity.Name);

        return await base.UpdateAsync(entity, cancellationToken);
    }
}
```

### Example 4: Service with Custom Operations
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services;

public interface IQuxService : ICrudService<Qux, long>
{
    /// <summary>
    /// Deactivates a Qux entity by setting its status.
    /// </summary>
    Task DeactivateAsync(
        long id,
        CancellationToken cancellationToken = default);
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories;

internal interface IQuxRepository : ICrudRepository<Qux, long>
{
    /// <summary>
    /// Custom repository operation: updates only the status column.
    /// </summary>
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
        // Foo: full service + repository
        services.AddSingleton<ICrudRepository<Foo, string>, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Bar: repository-only (no service layer needed)
        services.AddSingleton<IBarRepository, BarRepository>();

        // Baz: service with validation
        services.AddSingleton<ICrudRepository<Baz, Guid>, BazRepository>();
        services.AddSingleton<IBazService, BazService>();

        // Qux: service with custom operations
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
| Shared operation interfaces | 5 |
| Repository composite interfaces | 5 |
| Service composite interfaces | 3 |
| Base repository class | 1 |
| Base service class | 1 |
| Core entity interface | 1 |
| Non-generic BaseClassWithLogging | 1 (in Roadbed.Common) |
| **Total Roadbed.Crud types** | **16** |

## Comparison to Option 6

| Aspect | Option 6 (Separate) | Option 7 (Shared) |
|---|---|---|
| Operation interfaces | 10 (5 repo + 5 service) | **5** (shared) |
| Total Roadbed.Crud types | 21 | **16** |
| Layer distinction | Interface name (`ICreateServiceOperation`) | **Composite name** (`ICrudService`) |
| Interface polymorphism | ❌ Separate types | ✅ Same `IReadOperation` works for both |
| Service layer required | Always | **Optional** (can use repo directly) |
| Method signature duplication | Yes | **No** |

## Pros

- **No duplicated operation interfaces** — one `ICreateOperation` serves both layers;
  5 interfaces instead of 10
- **Service layer is optional** — consuming projects choose whether to use a service,
  a repository, or both. Example 2 shows repository-only usage without any service
- **Interface polymorphism** — code that depends on `IReadOperation<Foo, string>` can
  accept either a repository or a service. Enables flexible composition
- **Smaller type count** — 16 types vs 21 in Option 6
- **Cleaner mental model** — "an operation is an operation regardless of layer" is
  simpler than "repository operations and service operations are different types
  with identical signatures"
- **Composites express intent clearly** — `ICrudRepository` vs `ICrudService` is
  self-documenting; no need for separate operation naming conventions per layer
- **All benefits from Options 5 and 6 carry forward** — non-generic
  `BaseClassWithLogging`, virtual defaults, correct logger categories, pass-through
  service defaults, granular composites

## Cons

- **No compile-time layer enforcement** — because `ICrudRepository` and `ICrudService`
  both implement `ICreateOperation`, a developer could accidentally inject a repository
  where a service is expected (or vice versa). The compiler sees both as valid
  `ICreateOperation` implementations. Convention and DI registration must prevent this.
- **Semantic ambiguity** — when reading code, seeing `ICreateOperation<Foo, string>`
  on a parameter does not tell you whether it is a repository or service. You must
  look at the composite interface or the class name for context.
- **Repository sub-namespace change** — previous options put repository composites in
  `Roadbed.Crud.Composites`. This option uses `Roadbed.Crud.Repositories`, which is
  clearer when services exist too, but is a namespace change from earlier options.
- **Service layer boilerplate still exists** — empty pass-through services (Example 1)
  still feel like boilerplate, though they are optional in this option
- **BaseService still requires ICrudRepository** — same constructor issue as Option 6
- **DI registration for custom repositories is awkward** — Example 4 requires
  registering `IQuxRepository` and then forwarding it as `ICrudRepository<Qux, long>`
  so that `BaseService` can resolve it. This double-registration pattern may confuse
  developers.

## Open Questions This Option Raises

1. **Should the composites for both layers live in the same namespace?** Currently
   `Repositories/` and `Services/` are separate sub-namespaces. If they were in the
   same namespace, consuming projects would need fewer `using` statements but would
   see both `ICrudRepository` and `ICrudService` in intellisense together.
2. **Is the interface polymorphism actually useful?** The `FooCacheWarmer` example
   shows code that accepts either layer. In practice, will consuming projects ever
   want this, or does it create confusion about which layer they are interacting with?
3. **Should there be a `ILookupService` and `IReadWriteService`?** Only 3 service
   composites are provided vs 5 repository composites. The reasoning is that services
   are less likely to need fine-grained subsets. Is this correct?
4. **Should the DI registration pattern for custom repositories be simplified?** A
   helper method or convention could reduce the double-registration pattern in
   Example 4.
5. **Does repository-only usage undermine the service layer pattern?** If some
   entities use services and others use repositories directly, the application layer
   must know which pattern each entity uses. Is this inconsistency acceptable, or
   should the service layer be all-or-nothing?