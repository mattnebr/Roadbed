# Option 2: Parallel Sync/Async Interface Hierarchies

## Core Philosophy

Addresses the biggest limitation of Option 1 by providing first-class support for
both synchronous and asynchronous operations. The library defines parallel interface
hierarchies — one sync, one async — and lets consuming projects choose which to
implement. This is particularly important for Roadbed.IO where file operations like
CSV reads are naturally synchronous, while Roadbed.NET's HTTP calls are naturally
asynchronous.

## Terminology

- **Entity**: Same as Option 1. Identity-bearing objects.
- **Repository**: The data access abstraction. Kept as-is.
- **No service layer**: Consuming projects decide if they need one.

## Naming Conventions

| Current Name                         | Proposed Name                   | Rationale                                             |
| ------------------------------------ | ------------------------------- | ----------------------------------------------------- |
| `IDataTransferObject<T>`             | `IEntity<T>`                    | Same as Option 1                                      |
| `IRepositoryOperationRead<T, TId>`   | `IAsyncReadOperation<T, TId>`   | Prefix distinguishes from sync variant                |
| `IRepositoryOperationCreate<T, TId>` | `IAsyncCreateOperation<T, TId>` | Same                                                  |
| `IRepositoryOperationUpdate<T, TId>` | `IAsyncUpdateOperation<T, TId>` | Same                                                  |
| `IEntityOperationDelete<T, TId>`     | `IAsyncDeleteOperation<T, TId>` | Same                                                  |
| `IRepositoryOperationList<T, TId>`   | `IAsyncListOperation<T, TId>`   | Same                                                  |
| (new)                                | `IReadOperation<T, TId>`        | Sync variant — no prefix, sync is the simpler concept |
| (new)                                | `ICreateOperation<T, TId>`      | Same                                                  |
| (new)                                | `IUpdateOperation<T, TId>`      | Same                                                  |
| (new)                                | `IDeleteOperation<T, TId>`      | Same                                                  |
| (new)                                | `IListOperation<T, TId>`        | Same                                                  |
| `IBaseRepositoryWithCrud<T, TId>`    | `IAsyncCrudRepository<T, TId>`  | Async composite                                       |
| (new)                                | `ICrudRepository<T, TId>`       | Sync composite                                        |
| (new)                                | `ICrudRepositoryFull<T, TId>`   | Both sync and async composite                         |

**Naming rationale**: Sync interfaces get the "clean" names (`IReadOperation`) because
they represent the simpler concept. Async interfaces get the `IAsync` prefix because
they are the specialized variant. This follows the .NET convention where `Stream` is
the base concept and `Task<T>` wrapping signals async behavior. An alternative
explored in this option's Open Questions is reversing this convention.

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>
├── BaseEntity<TId>
├── Operations/
│   ├── Sync/
│   │   ├── ICreateOperation<T, TId>
│   │   ├── IReadOperation<T, TId>
│   │   ├── IUpdateOperation<T, TId>
│   │   ├── IDeleteOperation<T, TId>
│   │   └── IListOperation<T, TId>
│   └── Async/
│       ├── IAsyncCreateOperation<T, TId>
│       ├── IAsyncReadOperation<T, TId>
│       ├── IAsyncUpdateOperation<T, TId>
│       ├── IAsyncDeleteOperation<T, TId>
│       └── IAsyncListOperation<T, TId>
├── ICrudRepository<T, TId>              # Sync composite
├── IAsyncCrudRepository<T, TId>         # Async composite
├── ICrudRepositoryFull<T, TId>          # Both sync + async
├── BaseSyncRepository<T, TId>           # Abstract base for sync
├── BaseAsyncRepository<T, TId>          # Abstract base for async
├── BaseRepository<T, TId>               # Abstract base for both
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

### Synchronous Operation Interfaces
```csharp
namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Create operation for an entity.
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
    /// <returns>The created entity with its assigned identifier.</returns>
    TEntity Create(TEntity entity);
}

/// <summary>
/// Defines the synchronous Read operation for an entity.
/// </summary>
public interface IReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <returns>The entity matching the identifier.</returns>
    TEntity Read(TId id);
}

/// <summary>
/// Defines the synchronous Update operation for an entity.
/// </summary>
public interface IUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <returns>The updated entity.</returns>
    TEntity Update(TEntity entity);
}

/// <summary>
/// Defines the synchronous Delete operation for an entity.
/// </summary>
public interface IDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    void Delete(TId id);
}

/// <summary>
/// Defines the synchronous List operation for an entity.
/// </summary>
public interface IListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Lists all entities.
    /// </summary>
    /// <returns>Collection of all entities.</returns>
    IList<TEntity> List();
}
```

### Asynchronous Operation Interfaces
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncCreateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created entity with its assigned identifier.</returns>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
public interface IAsyncReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier.</returns>
    Task<TEntity> ReadAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
public interface IAsyncUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
public interface IAsyncDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
public interface IAsyncListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Lists all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all entities.</returns>
    Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
```

### Composite Interfaces
```csharp
namespace Roadbed.Crud;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Composite contract for synchronous Create, Read, Update, Delete, and List operations.
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
```csharp
namespace Roadbed.Crud;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Composite contract for asynchronous Create, Read, Update, Delete, and List operations.
/// </summary>
public interface IAsyncCrudRepository<TEntity, TId>
    : IAsyncCreateOperation<TEntity, TId>,
      IAsyncReadOperation<TEntity, TId>,
      IAsyncUpdateOperation<TEntity, TId>,
      IAsyncDeleteOperation<TEntity, TId>,
      IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```
```csharp
namespace Roadbed.Crud;

/// <summary>
/// Composite contract combining both synchronous and asynchronous CRUDL operations.
/// </summary>
/// <remarks>
/// Use this interface when the consuming project needs to support both sync and async
/// callers. The sync methods can serve as convenience wrappers, or both variants can
/// have independent implementations optimized for their execution model.
/// </remarks>
public interface ICrudRepositoryFull<TEntity, TId>
    : ICrudRepository<TEntity, TId>,
      IAsyncCrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

## Base Abstract Classes

### Async-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base abstract repository with logging for asynchronous CRUDL operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseAsyncRepository<TEntity, TId>
    : BaseClassWithLogging<BaseAsyncRepository<TEntity, TId>>,
      IAsyncCrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAsyncRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAsyncRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    protected BaseAsyncRepository(ILoggerFactory loggerFactory)
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

### Sync-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base abstract repository with logging for synchronous CRUDL operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseSyncRepository<TEntity, TId>
    : BaseClassWithLogging<BaseSyncRepository<TEntity, TId>>,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSyncRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSyncRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    protected BaseSyncRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract TEntity Create(TEntity entity);

    /// <inheritdoc/>
    public abstract TEntity Read(TId id);

    /// <inheritdoc/>
    public abstract TEntity Update(TEntity entity);

    /// <inheritdoc/>
    public abstract void Delete(TId id);

    /// <inheritdoc/>
    public abstract IList<TEntity> List();

    #endregion Public Methods
}
```

### Full (Sync + Async) Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base abstract repository with logging for both synchronous and asynchronous
/// CRUDL operations.
/// </summary>
/// <remarks>
/// Sync methods delegate to the async implementations by default. Override the
/// sync methods if you need an optimized synchronous code path instead of the
/// async-over-sync bridge.
/// </remarks>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseRepository<TEntity, TId>
    : BaseClassWithLogging<BaseRepository<TEntity, TId>>,
      ICrudRepositoryFull<TEntity, TId>
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

    #region Public Methods - Async (must override)

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

    #endregion Public Methods - Async (must override)

    #region Public Methods - Sync (default bridges to async, can override)

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to <see cref="CreateAsync"/>.
    /// Override for an optimized synchronous code path.
    /// </remarks>
    public virtual TEntity Create(TEntity entity)
    {
        return this.CreateAsync(entity, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to <see cref="ReadAsync"/>.
    /// Override for an optimized synchronous code path.
    /// </remarks>
    public virtual TEntity Read(TId id)
    {
        return this.ReadAsync(id, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to <see cref="UpdateAsync"/>.
    /// Override for an optimized synchronous code path.
    /// </remarks>
    public virtual TEntity Update(TEntity entity)
    {
        return this.UpdateAsync(entity, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to <see cref="DeleteAsync"/>.
    /// Override for an optimized synchronous code path.
    /// </remarks>
    public virtual void Delete(TId id)
    {
        this.DeleteAsync(id, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation delegates to <see cref="ListAsync"/>.
    /// Override for an optimized synchronous code path.
    /// </remarks>
    public virtual IList<TEntity> List()
    {
        return this.ListAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    #endregion Public Methods - Sync (default bridges to async, can override)
}
```

## Consuming Project Examples

### Example 1: Async-Only Repository (Roadbed.NET / API consumer)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public interface IFooRepository : IAsyncCrudRepository<Foo, string>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class FooRepository
    : BaseAsyncRepository<Foo, string>,
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
        // HTTP POST to API
        throw new NotImplementedException();
    }

    public override async Task<Foo> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Foo: {Id}", id);
        // HTTP GET from API
        throw new NotImplementedException();
    }

    public override async Task<Foo> UpdateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Foo: {Id}", entity.Id);
        // HTTP PUT to API
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Deleting Foo: {Id}", id);
        // HTTP DELETE to API
        throw new NotImplementedException();
    }

    public override async Task<IList<Foo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Foo entities");
        // HTTP GET collection from API
        throw new NotImplementedException();
    }
}
```

### Example 2: Sync-Only Repository (Roadbed.IO / CSV file)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public interface IBarRepository : ICrudRepository<Bar, int>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class BarRepository
    : BaseSyncRepository<Bar, int>,
      IBarRepository
{
    private readonly string _filePath;

    internal BarRepository(string filePath, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        this._filePath = filePath;
    }

    public override Bar Create(Bar entity)
    {
        this.LogDebug("Creating Bar: {Id}", entity.Id);
        // Append row to CSV
        throw new NotImplementedException();
    }

    public override Bar Read(int id)
    {
        this.LogDebug("Reading Bar: {Id}", id);
        // Parse CSV to find row
        throw new NotImplementedException();
    }

    public override Bar Update(Bar entity)
    {
        this.LogDebug("Updating Bar: {Id}", entity.Id);
        // Rewrite CSV row
        throw new NotImplementedException();
    }

    public override void Delete(int id)
    {
        this.LogDebug("Deleting Bar: {Id}", id);
        // Remove row from CSV
        throw new NotImplementedException();
    }

    public override IList<Bar> List()
    {
        this.LogDebug("Listing all Bar entities");
        // Parse all CSV rows
        throw new NotImplementedException();
    }
}
```

### Example 3: Full (Both) Repository
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public interface IBazRepository : ICrudRepositoryFull<Baz, Guid>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

/// <summary>
/// Repository that provides optimized sync and async paths.
/// Sync methods override the default bridge to avoid async-over-sync.
/// </summary>
internal sealed class BazRepository
    : BaseRepository<Baz, Guid>,
      IBazRepository
{
    internal BazRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    // Async implementations (required)
    public override async Task<Baz> CreateAsync(
        Baz entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Async creating Baz: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task<Baz> ReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Async reading Baz: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<Baz> UpdateAsync(
        Baz entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IList<Baz>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    // Sync overrides (optional — avoids async-over-sync bridge)
    public override Baz Read(Guid id)
    {
        this.LogDebug("Sync reading Baz: {Id}", id);
        // Optimized synchronous path
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
        // Async-only: API-backed
        services.AddSingleton<IFooRepository, FooRepository>();

        // Sync-only: file-backed
        services.AddSingleton<IBarRepository>(sp =>
            new BarRepository(
                configuration["BarFilePath"]!,
                sp.GetRequiredService<ILoggerFactory>()));

        // Full: register as full interface
        services.AddSingleton<IBazRepository, BazRepository>();
    }
}
```

## Interface Count Summary

| Component | Count |
|---|---|
| Sync operation interfaces | 5 |
| Async operation interfaces | 5 |
| Sync composite interface | 1 |
| Async composite interface | 1 |
| Full composite interface | 1 |
| Base abstract classes | 3 |
| Core entity interface | 1 |
| **Total** | **17** |

## Pros

- **First-class sync and async support** — consuming projects pick what fits naturally
- **No forced async-over-sync** — Roadbed.IO can use pure sync implementations
- **No forced sync-over-async** — Roadbed.NET can use pure async implementations
- **Full base provides bridge** — `BaseRepository` offers default sync-to-async
  bridging with the option to override for optimized sync paths
- **Consistent naming** — `IAsync` prefix clearly distinguishes the two hierarchies
- **Mix and match** — a consuming project could implement `IAsyncReadOperation` +
  `ICreateOperation` if only some operations warrant async
- **Same entity interface** — `IEntity<TId>` is shared; entities don't care about
  sync vs async

## Cons

- **Interface explosion** — 12 operation interfaces (5 sync + 5 async + 2 composites)
  before consuming projects add their own
- **Namespace complexity** — `Operations.Sync` and `Operations.Async` sub-namespaces
  add cognitive load and extra `using` statements
- **Naming debate** — giving sync the "clean" name and async the prefix is arguable;
  some teams would reverse this since async is more common in modern .NET
- **GetAwaiter().GetResult() risk** — the default sync bridge in `BaseRepository` can
  deadlock in UI or ASP.NET classic synchronization contexts; documentation must warn
  consumers to override these methods
- **No filtering or pagination** — same limitation as Option 1
- **No service layer** — same limitation as Option 1
- **No transaction support** — same limitation as Option 1
- **Logger category name** — same issue as Option 1; the base class name becomes the
  category instead of the concrete class name
- **Testing burden** — consuming projects that use `ICrudRepositoryFull` must test
  both sync and async code paths

## Open Questions This Option Raises

1. **Should the naming convention be reversed?** Give async the clean names
   (`ICreateOperation`) and prefix sync with `ISync`? Modern .NET is async-first,
   so async could be considered the default. Counter-argument: sync is the simpler
   concept and doesn't need Task wrapping, so it deserves the simpler name.
2. **Should `BaseRepository` provide default sync bridges at all?** The
   `GetAwaiter().GetResult()` pattern is risky. An alternative is to make all 10
   methods abstract in `BaseRepository` and force consuming projects to implement
   both paths explicitly.
3. **Is `ICrudRepositoryFull` useful in practice?** Will consuming projects actually
   need both sync and async on the same repository, or will they always pick one?
4. **Should the sub-namespaces be flattened?** Instead of `Operations.Sync` and
   `Operations.Async`, could all 10 interfaces live in `Roadbed.Crud.Operations`
   since the `IAsync` prefix already distinguishes them?
5. **Does the interface count justify the flexibility?** If 90% of consumers only
   use async, the sync hierarchy exists primarily for Roadbed.IO. Is a library-level
   solution warranted, or should Roadbed.IO define its own sync interfaces?