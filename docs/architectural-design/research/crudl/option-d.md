# Option 4: CRTP Logging with BaseClassWithLogging + Query/Filter Operation

## Core Philosophy

Two complementary improvements. First, reuse `BaseClassWithLogging` with the Curiously
Recurring Template Pattern (CRTP) to solve the logger category problem without
duplicating any logging code. Second, introduce a generic query/filter operation so
consuming projects can express filtered list operations while keeping Roadbed.Crud
agnostic about what a "filter" looks like. Granular composites from Option 3 are
carried forward.

## CRTP Logging Pattern

### The Problem (Options 1 and 2)
```csharp
public abstract class BaseAsyncRepository<TEntity, TId>
    : BaseClassWithLogging<BaseAsyncRepository<TEntity, TId>>
//                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                         Logger category = base class name with generic args
//
// Log output:
// [DBG] Roadbed.Crud.BaseAsyncRepository`2[MyApp.Data.Foo,System.String] - Reading...
```

### The Problem (Option 3)

Solved the category name by abandoning `BaseClassWithLogging` entirely, but then
duplicated all the logging helper methods across every base class.

### The Solution (CRTP)

Add a third generic parameter `TRepository` that represents the concrete class.
Pass it to `BaseClassWithLogging<TRepository>`. The consuming class supplies its own
type as that parameter, so the logger category is always the concrete class name.
```csharp
// Base class uses TRepository as the logger category
public abstract class BaseAsyncRepository<TEntity, TId, TRepository>
    : BaseClassWithLogging<TRepository>,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    protected BaseAsyncRepository(ILogger logger)
        : base(logger)
    //  ^^^^^^^^^^^^
    //  Calls BaseClassWithLogging(ILogger logger) constructor
    {
    }
}

// Consuming class passes itself as TRepository
internal sealed class FooRepository
    : BaseAsyncRepository<Foo, string, FooRepository>
//                                     ^^^^^^^^^^^^^
//                                     CRTP: "I am FooRepository"
{
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    //  ^^^^^^^^^^^^
    //  ILogger<FooRepository> IS-A ILogger
    //  BaseClassWithLogging casts: logger as ILogger<FooRepository> ✅ succeeds
    {
    }
}

// Log output now shows the correct category:
// [DBG] MyApp.Data.Internal.FooRepository - Reading Foo: abc123
```

### Why the Cast Succeeds

Inside `BaseClassWithLogging(ILogger logger)`:
```csharp
this._logger = logger as ILogger<TCategoryName> ?? NullLogger<TCategoryName>.Instance;
```

With CRTP, `TCategoryName` = `TRepository` = `FooRepository`. The injected logger is
`ILogger<FooRepository>`. The cast `logger as ILogger<FooRepository>` succeeds because
the types match exactly. All of `BaseClassWithLogging`'s convenience methods
(`LogDebug`, `LogInformation`, `LogError`, etc.) work with the correctly categorized
logger — zero duplication.

### CRTP in the .NET Ecosystem

This pattern is well-established in .NET:
- `IComparable<T>` where `T` is the implementing type
- `IEquatable<T>` where `T` is the implementing type
- Entity Framework's `EntityTypeConfiguration<TEntity>`
- ASP.NET's `Controller<T>` patterns in some frameworks

## Terminology

- **Entity**: Identity-bearing objects. Same as previous options.
- **Repository**: The data access abstraction.
- **Query**: A filtered list operation. The filter type is defined by the consuming
  project, keeping Roadbed.Crud agnostic about filter structure.

## Naming Conventions

| Current Name | Proposed Name | Rationale |
|---|---|---|
| `IDataTransferObject<T>` | `IEntity<T>` | Same as previous options |
| `IRepositoryOperationRead<T, TId>` | `IReadOperation<T, TId>` | Same as previous options |
| `IRepositoryOperationCreate<T, TId>` | `ICreateOperation<T, TId>` | Same |
| `IRepositoryOperationUpdate<T, TId>` | `IUpdateOperation<T, TId>` | Same |
| `IEntityOperationDelete<T, TId>` | `IDeleteOperation<T, TId>` | Same |
| `IRepositoryOperationList<T, TId>` | `IListOperation<T, TId>` | Same |
| (new) | `IQueryOperation<T, TId, TFilter>` | Filtered list with generic filter type |
| `IBaseRepositoryWithCrud<T, TId>` | `ICrudRepository<T, TId>` | Same |

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
│   ├── IListOperation<T, TId>
│   └── IQueryOperation<T, TId, TFilter>
├── Composites/
│   ├── ICrudRepository<T, TId>
│   ├── IReadOnlyRepository<T, TId>
│   ├── IWriteOnlyRepository<T, TId>
│   ├── IReadWriteRepository<T, TId>
│   └── ILookupRepository<T, TId>
├── BaseAsyncRepository<T, TId, TRepo>
├── BaseReadOnlyRepository<T, TId, TRepo>
├── BaseWriteOnlyRepository<T, TId, TRepo>
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

### Individual Operation Interfaces

The five core operations are identical to Option 3. Only the new query operation
is shown here.
```csharp
namespace Roadbed.Crud.Operations;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
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

/// <summary>
/// Defines the asynchronous Query operation for an entity with a filter.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <typeparam name="TFilter">
/// Type of filter object. Defined by the consuming project to represent whatever
/// filtering, sorting, and pagination criteria are meaningful for the data source.
/// </typeparam>
/// <remarks>
/// The filter type is intentionally generic. Roadbed.Crud does not define what a
/// filter looks like — that is the responsibility of the consuming project. Examples:
/// <list type="bullet">
///   <item>A record with search text, sort field, page size, and cursor</item>
///   <item>A simple string keyword</item>
///   <item>A specification object for complex query composition</item>
/// </list>
/// </remarks>
public interface IQueryOperation<TEntity, in TId, in TFilter>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Queries entities matching the specified filter asynchronously.
    /// </summary>
    /// <param name="filter">Filter criteria for the query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of entities matching the filter.</returns>
    Task<IList<TEntity>> QueryAsync(
        TFilter filter,
        CancellationToken cancellationToken = default);
}
```

### Composite Interfaces
```csharp
namespace Roadbed.Crud.Composites;

using Roadbed.Crud.Operations;

/// <summary>
/// Full CRUDL repository contract.
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
/// Read-only repository contract providing Read and List operations.
/// </summary>
public interface IReadOnlyRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Lookup repository contract providing only the Read operation.
/// </summary>
public interface ILookupRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only repository contract providing Create, Update, and Delete operations.
/// </summary>
public interface IWriteOnlyRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-write repository contract providing CRUD without List.
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

**Note**: `IQueryOperation` is intentionally excluded from all composite interfaces.
Because `TFilter` is a third generic parameter, including it would force every
composite to carry `TFilter`, which most repositories don't need. Consuming projects
add `IQueryOperation` alongside their chosen composite when they need it.

## Base Abstract Classes

### Full CRUDL Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Composites;

/// <summary>
/// Base abstract repository with logging for all CRUDL operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <typeparam name="TRepository">
/// Type of the concrete repository class. Used as the logger category name via
/// the Curiously Recurring Template Pattern (CRTP). The consuming class passes
/// itself as this parameter.
/// </typeparam>
public abstract class BaseAsyncRepository<TEntity, TId, TRepository>
    : BaseClassWithLogging<TRepository>,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncRepository{TEntity, TId, TRepository}"/> class
    /// with no logging.
    /// </summary>
    protected BaseAsyncRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncRepository{TEntity, TId, TRepository}"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;TRepository&gt;</c> from the concrete
    /// class to ensure the correct logger category name.
    /// </param>
    protected BaseAsyncRepository(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncRepository{TEntity, TId, TRepository}"/> class.
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

### Read-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Composites;

/// <summary>
/// Base abstract repository with logging for Read and List operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <typeparam name="TRepository">
/// Type of the concrete repository class (CRTP).
/// </typeparam>
public abstract class BaseReadOnlyRepository<TEntity, TId, TRepository>
    : BaseClassWithLogging<TRepository>,
      IReadOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseReadOnlyRepository{TEntity, TId, TRepository}"/> class
    /// with no logging.
    /// </summary>
    protected BaseReadOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseReadOnlyRepository{TEntity, TId, TRepository}"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;TRepository&gt;</c> from the concrete
    /// class to ensure the correct logger category name.
    /// </param>
    protected BaseReadOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseReadOnlyRepository{TEntity, TId, TRepository}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    protected BaseReadOnlyRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
```

### Write-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Composites;

/// <summary>
/// Base abstract repository with logging for Create, Update, and Delete operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <typeparam name="TRepository">
/// Type of the concrete repository class (CRTP).
/// </typeparam>
public abstract class BaseWriteOnlyRepository<TEntity, TId, TRepository>
    : BaseClassWithLogging<TRepository>,
      IWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseWriteOnlyRepository{TEntity, TId, TRepository}"/> class
    /// with no logging.
    /// </summary>
    protected BaseWriteOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseWriteOnlyRepository{TEntity, TId, TRepository}"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;TRepository&gt;</c> from the concrete
    /// class to ensure the correct logger category name.
    /// </param>
    protected BaseWriteOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseWriteOnlyRepository{TEntity, TId, TRepository}"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    protected BaseWriteOnlyRepository(ILoggerFactory loggerFactory)
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
    public abstract Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
```

## Consuming Project Examples

### Example 1: Full CRUDL Repository
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

using Roadbed.Crud.Composites;

public interface IFooRepository : ICrudRepository<Foo, string>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class FooRepository
    : BaseAsyncRepository<Foo, string, FooRepository>,
//                                     ^^^^^^^^^^^^^
//                                     CRTP: passes itself as logger category
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
        // this.LogDebug() is inherited from BaseClassWithLogging<FooRepository>
        // Logger category in output: "MyApp.Data.Internal.FooRepository"
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

### Example 2: CRUDL + Query with Custom Filter
```csharp
namespace MyApp.Data;

/// <summary>
/// Filter criteria for querying Bar entities.
/// Defined by the consuming project — not by Roadbed.Crud.
/// </summary>
public sealed record BarFilter
{
    /// <summary>
    /// Gets the search text to match against Name or Description.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the cursor for keyset pagination (last ID from previous page).
    /// </summary>
    public int? AfterId { get; init; }

    /// <summary>
    /// Gets the sort field name.
    /// </summary>
    public string SortBy { get; init; } = "Name";
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Composites;
using Roadbed.Crud.Operations;

/// <summary>
/// Bar repository with full CRUDL plus filtered query support.
/// </summary>
/// <remarks>
/// IQueryOperation is added alongside ICrudRepository. The composite
/// interfaces do not include IQueryOperation because TFilter varies
/// per consuming project.
/// </remarks>
public interface IBarRepository
    : ICrudRepository<Bar, int>,
      IQueryOperation<Bar, int, BarFilter>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class BarRepository
    : BaseAsyncRepository<Bar, int, BarRepository>,
      IBarRepository
{
    internal BarRepository(ILogger<BarRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Bar> CreateAsync(
        Bar entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Bar: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task<Bar> ReadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Bar: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<Bar> UpdateAsync(
        Bar entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IList<Bar>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Bar entities");
        throw new NotImplementedException();
    }

    /// <summary>
    /// Queries Bar entities using the provided filter criteria.
    /// </summary>
    /// <remarks>
    /// This method is not abstract in a base class because IQueryOperation
    /// is added at the consuming interface level, not in Roadbed.Crud composites.
    /// </remarks>
    public async Task<IList<Bar>> QueryAsync(
        BarFilter filter,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug(
            "Querying Bar entities: SearchText={SearchText}, PageSize={PageSize}, AfterId={AfterId}",
            filter.SearchText ?? "(none)",
            filter.PageSize,
            filter.AfterId?.ToString() ?? "(first page)");
        throw new NotImplementedException();
    }
}
```

### Example 3: Read-Only + Query (Reporting View)
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;
using Roadbed.Crud.Operations;

/// <summary>
/// Baz is a reporting view — read, list, and query only. No mutations.
/// </summary>
public interface IBazRepository
    : IReadOnlyRepository<Baz, Guid>,
      IQueryOperation<Baz, Guid, BazReportFilter>
{
}
```
```csharp
namespace MyApp.Data;

/// <summary>
/// Filter criteria for Baz reporting queries.
/// </summary>
public sealed record BazReportFilter
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int PageSize { get; init; } = 50;
    public Guid? AfterCursor { get; init; }
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

internal sealed class BazRepository
    : BaseReadOnlyRepository<Baz, Guid, BazRepository>,
      IBazRepository
{
    internal BazRepository(ILogger<BazRepository> logger)
        : base(logger)
    {
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

    public async Task<IList<Baz>> QueryAsync(
        BazReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug(
            "Querying Baz reports: {StartDate} to {EndDate}",
            filter.StartDate,
            filter.EndDate);
        throw new NotImplementedException();
    }
}
```

### Example 4: Query-Only Repository (No CRUDL)
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Operations;

/// <summary>
/// Qux supports only filtered queries — no CRUD, no unfiltered list.
/// Useful for search-oriented data sources (e.g., external search API).
/// </summary>
public interface IQuxRepository
    : IQueryOperation<Qux, string, QuxSearchCriteria>
{
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
        // Full CRUDL
        services.AddSingleton<IFooRepository, FooRepository>();

        // CRUDL + Query
        services.AddSingleton<IBarRepository, BarRepository>();

        // Read-only + Query
        services.AddSingleton<IBazRepository, BazRepository>();

        // Query-only
        services.AddSingleton<IQuxRepository, QuxRepository>();
    }
}
```

## Interface and Class Count Summary

| Component | Count |
|---|---|
| Operation interfaces | 6 (5 CRUDL + 1 Query) |
| Composite interfaces | 5 |
| Base abstract classes | 3 |
| Core entity interface | 1 |
| **Total** | **15** |

## Pros

- **Correct logger category names** — CRTP ensures log output shows `FooRepository`,
  not `BaseAsyncRepository<Foo, String>`
- **Zero logging code duplication** — all convenience methods (`LogDebug`,
  `LogInformation`, etc.) are inherited from `BaseClassWithLogging`; no copy-paste
- **Consistent with Roadbed ecosystem** — uses the same `BaseClassWithLogging` base
  that all other Roadbed libraries use; same patterns, same constructor conventions
- **Flexible filtering** — `IQueryOperation<T, TId, TFilter>` lets each consuming
  project define its own filter type with whatever pagination, sorting, and search
  semantics it needs
- **No result standardization** — `QueryAsync` returns `IList<TEntity>`, not a
  library-defined paged result type; consuming projects wrap results however they want
- **Granular composites** — read-only, write-only, lookup, and full CRUDL
- **IQueryOperation is opt-in** — consuming projects add it only when needed; it
  doesn't pollute the standard composites with an extra `TFilter` parameter
- **Three constructor options** — parameterless, `ILogger`, and `ILoggerFactory` all
  inherited from `BaseClassWithLogging`

## Cons

- **Three generic parameters on base classes** — `BaseAsyncRepository<TEntity, TId,
  TRepository>` is verbose; consuming class declarations get long:
  `BaseAsyncRepository<Foo, string, FooRepository>`
- **CRTP is unfamiliar to some developers** — the "pass yourself as a type parameter"
  pattern may confuse team members who haven't seen it before
- **No compile-time enforcement of CRTP** — nothing prevents a developer from writing
  `BaseAsyncRepository<Foo, string, SomeOtherClass>`, which would give incorrect
  logger category names; this is a convention, not a constraint
- **Query returns IList, not paged metadata** — consuming projects that need total
  count, has-more flags, or cursor tokens must build that into their own wrappers;
  Roadbed.Crud provides no pagination result type
- **No base class for IQueryOperation** — since `TFilter` varies, there is no
  `BaseQueryableRepository`; consuming projects implement `QueryAsync` directly on
  their concrete class
- **No sync support** — async-only (can be combined with Option 2's sync hierarchy)
- **Public logging methods inherited** — `BaseClassWithLogging` defines `LogDebug`,
  `LogInformation`, etc. as public; they are accessible on the concrete class instance
  (though not through the repository interface, so in practice this is benign)
- **Composites namespace requires using statement** — `Roadbed.Crud.Composites` is
  separate from `Roadbed.Crud.Operations`; consuming projects may need both

## Open Questions This Option Raises

1. **Should `IQueryOperation` return something richer than `IList<TEntity>`?** A
   generic `TResult` return type would let consuming projects return paged results,
   but adds a fourth type parameter: `IQueryOperation<TEntity, TId, TFilter, TResult>`.
   Is that too many generics?
2. **Should the composites live in a sub-namespace or the root?** Moving them to
   `Roadbed.Crud` eliminates one `using` statement but mixes composites with
   individual operations in intellisense.
3. **Is three base classes enough?** There is no `BaseReadWriteRepository` (CRUD
   without List) or `BaseLookupRepository` (Read only). Should these exist, or are
   the two-method/three-method base classes simple enough to skip?
4. **Can the CRTP constraint be tightened?** A `where TRepository : class` constraint
   prevents value types but doesn't enforce that `TRepository` is the actual concrete
   class. Is that sufficient, or is a runtime check warranted?
5. **Should `IQueryOperation` support multiple filter types per entity?** A repository
   might need different query methods for different use cases (e.g., search by name
   vs. search by date range). Should it implement `IQueryOperation` twice with
   different `TFilter` types, or should the filter type be a discriminated union?