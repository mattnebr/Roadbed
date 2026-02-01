# Option 3: Granular Composites with Direct Logger Injection

## Core Philosophy

Two goals. First, solve the logger category name problem from Options 1 and 2 by
eliminating inheritance from `BaseClassWithLogging` entirely. Instead, base classes
accept `ILogger` directly from the consuming class, which means the logger category
is always the concrete class (e.g., `FooRepository`) rather than the abstract base
(e.g., `BaseAsyncRepository<Foo, String>`). Second, provide granular composite
interfaces so consuming projects can express exactly which subset of CRUDL operations
they support — read-only, write-only, read-write without delete, etc.

## Terminology

- **Entity**: Identity-bearing objects. Same as Options 1 and 2.
- **Repository**: The data access abstraction. Kept as-is.
- **Store**: Alternative name explored in the composite interfaces for read-only or
  write-only subsets where "repository" feels too heavy.
- **No service layer**: Consuming projects decide if they need one.

## Logger Category Problem and Solution

### The Problem (Options 1 and 2)
```csharp
// BaseClassWithLogging uses its own type as the logger category
public abstract class BaseAsyncRepository<TEntity, TId>
    : BaseClassWithLogging<BaseAsyncRepository<TEntity, TId>>
//                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                         This becomes the logger category name

// So when FooRepository logs a message, the output shows:
// [DBG] Roadbed.Crud.BaseAsyncRepository<MyApp.Data.Foo, System.String> - Reading Foo: abc123
//
// Instead of the desired:
// [DBG] MyApp.Data.Internal.FooRepository - Reading Foo: abc123
```

### The Solution (This Option)
```csharp
// Base class accepts ILogger directly — no generic category baked in
public abstract class BaseAsyncRepository<TEntity, TId>
{
    private readonly ILogger _logger;

    protected BaseAsyncRepository(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }
}

// Consuming class injects ILogger<FooRepository> which carries the correct category
internal sealed class FooRepository : BaseAsyncRepository<Foo, string>
{
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    //        ^^^^^^
    //        ILogger<FooRepository> IS-A ILogger, so the category is "FooRepository"
    {
    }
}

// Log output now shows the correct category:
// [DBG] MyApp.Data.Internal.FooRepository - Reading Foo: abc123
```

## Naming Conventions

| Current Name | Proposed Name | Rationale |
|---|---|---|
| `IDataTransferObject<T>` | `IEntity<T>` | Same as Options 1 and 2 |
| `IRepositoryOperationRead<T, TId>` | `IReadOperation<T, TId>` | Clean, short |
| `IRepositoryOperationCreate<T, TId>` | `ICreateOperation<T, TId>` | Same |
| `IRepositoryOperationUpdate<T, TId>` | `IUpdateOperation<T, TId>` | Same |
| `IEntityOperationDelete<T, TId>` | `IDeleteOperation<T, TId>` | Same |
| `IRepositoryOperationList<T, TId>` | `IListOperation<T, TId>` | Same |
| `IBaseRepositoryWithCrud<T, TId>` | `ICrudRepository<T, TId>` | Full CRUDL composite |
| (new) | `IReadOnlyRepository<T, TId>` | Read + List only |
| (new) | `IWriteOnlyRepository<T, TId>` | Create + Update + Delete only |
| (new) | `IReadWriteRepository<T, TId>` | CRUD without List |

**Note**: This option uses async-only interfaces (like Option 1). Sync/async duality
from Option 2 can be layered on top but is not the focus here.

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
│   ├── IReadOnlyRepository<T, TId>       # Read + List
│   ├── IWriteOnlyRepository<T, TId>      # Create + Update + Delete
│   ├── IReadWriteRepository<T, TId>      # Create + Read + Update + Delete
│   ├── ICrudRepository<T, TId>           # All five (alias for CRUDL)
│   └── ILookupRepository<T, TId>         # Read only (no List)
├── BaseAsyncRepository<T, TId>           # Full CRUDL abstract base
├── BaseReadOnlyRepository<T, TId>        # Read + List abstract base
├── BaseWriteOnlyRepository<T, TId>       # Create + Update + Delete abstract base
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

### Composite Interfaces
```csharp
namespace Roadbed.Crud.Composites;

using Roadbed.Crud.Operations;

/// <summary>
/// Read-only repository contract providing Read and List operations.
/// </summary>
/// <remarks>
/// Use this interface when the consuming project should not be able to modify
/// entities through this contract. Common for reporting, dashboards, or
/// reference data lookups.
/// </remarks>
public interface IReadOnlyRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>,
      IListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Lookup repository contract providing only the Read operation.
/// </summary>
/// <remarks>
/// Use this when a consuming project needs to retrieve a single entity by ID
/// but has no need to list or enumerate. Common for foreign key resolution
/// or single-entity detail views.
/// </remarks>
public interface ILookupRepository<TEntity, TId>
    : IReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Write-only repository contract providing Create, Update, and Delete operations.
/// </summary>
/// <remarks>
/// Use this for CQRS-style separation where the write side does not need to
/// read or list entities. The write side receives commands and mutates state.
/// </remarks>
public interface IWriteOnlyRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Read-write repository contract providing Create, Read, Update, and Delete
/// operations without List.
/// </summary>
/// <remarks>
/// Use this when individual entity operations are needed but bulk listing is
/// not appropriate — for example, large datasets where List would be dangerous
/// without filtering.
/// </remarks>
public interface IReadWriteRepository<TEntity, TId>
    : ICreateOperation<TEntity, TId>,
      IReadOperation<TEntity, TId>,
      IUpdateOperation<TEntity, TId>,
      IDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Full CRUDL repository contract providing Create, Read, Update, Delete,
/// and List operations.
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

## Base Abstract Classes

### Logging Helper Methods

All base classes include protected logging helper methods that delegate to the
injected `ILogger`. These methods check if the log level is enabled before
formatting the message, matching the performance behavior of `BaseClassWithLogging`.
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Full CRUDL abstract base with logging via direct logger injection.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseAsyncRepository<TEntity, TId>
    : ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ILogger _logger;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncRepository()
    {
        this._logger = NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for the concrete repository.</param>
    protected BaseAsyncRepository(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the logger instance for the repository.
    /// </summary>
    protected ILogger Logger => this._logger;

    #endregion Protected Properties

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

    #region Protected Methods

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Trace"/> if enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogTrace(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Trace))
        {
            this._logger.LogTrace(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Debug"/> if enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogDebug(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
            this._logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Information"/> if enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogInformation(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Information))
        {
            this._logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Warning"/> if enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogWarning(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Warning))
        {
            this._logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Error"/> if enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogError(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Error))
        {
            this._logger.LogError(message, args);
        }
    }

    /// <summary>
    /// Logs an exception at <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="args">Message parameters.</param>
    protected void LogError(Exception exception, string message, params object[] args)
    {
        this._logger.LogError(exception, message, args);
    }

    #endregion Protected Methods
}
```

### Read-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Roadbed.Crud.Composites;

/// <summary>
/// Read-only abstract base with logging via direct logger injection.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseReadOnlyRepository<TEntity, TId>
    : IReadOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ILogger _logger;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseReadOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseReadOnlyRepository()
    {
        this._logger = NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseReadOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for the concrete repository.</param>
    protected BaseReadOnlyRepository(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the logger instance for the repository.
    /// </summary>
    protected ILogger Logger => this._logger;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Trace"/> if enabled.
    /// </summary>
    protected void LogTrace(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Trace))
        {
            this._logger.LogTrace(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Debug"/> if enabled.
    /// </summary>
    protected void LogDebug(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
            this._logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Information"/> if enabled.
    /// </summary>
    protected void LogInformation(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Information))
        {
            this._logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Warning"/> if enabled.
    /// </summary>
    protected void LogWarning(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Warning))
        {
            this._logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Error"/> if enabled.
    /// </summary>
    protected void LogError(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Error))
        {
            this._logger.LogError(message, args);
        }
    }

    /// <summary>
    /// Logs an exception at <see cref="LogLevel.Error"/>.
    /// </summary>
    protected void LogError(Exception exception, string message, params object[] args)
    {
        this._logger.LogError(exception, message, args);
    }

    #endregion Protected Methods
}
```

### Write-Only Base
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Roadbed.Crud.Composites;

/// <summary>
/// Write-only abstract base with logging via direct logger injection.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public abstract class BaseWriteOnlyRepository<TEntity, TId>
    : IWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ILogger _logger;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseWriteOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseWriteOnlyRepository()
    {
        this._logger = NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseWriteOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for the concrete repository.</param>
    protected BaseWriteOnlyRepository(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the logger instance for the repository.
    /// </summary>
    protected ILogger Logger => this._logger;

    #endregion Protected Properties

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

    #region Protected Methods

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Trace"/> if enabled.
    /// </summary>
    protected void LogTrace(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Trace))
        {
            this._logger.LogTrace(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Debug"/> if enabled.
    /// </summary>
    protected void LogDebug(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
            this._logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Information"/> if enabled.
    /// </summary>
    protected void LogInformation(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Information))
        {
            this._logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Warning"/> if enabled.
    /// </summary>
    protected void LogWarning(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Warning))
        {
            this._logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Error"/> if enabled.
    /// </summary>
    protected void LogError(string message, params object[] args)
    {
        if (this._logger.IsEnabled(LogLevel.Error))
        {
            this._logger.LogError(message, args);
        }
    }

    /// <summary>
    /// Logs an exception at <see cref="LogLevel.Error"/>.
    /// </summary>
    protected void LogError(Exception exception, string message, params object[] args)
    {
        this._logger.LogError(exception, message, args);
    }

    #endregion Protected Methods
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

using Roadbed.Crud;

public interface IFooRepository : ICrudRepository<Foo, string>
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
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    //        ^^^^^^
    //        Logger category is now "MyApp.Data.Internal.FooRepository"
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

### Example 2: Read-Only Repository (Reference Data)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Composites;

public sealed record Bar : IEntity<int>
{
    public int? Id { get; init; }
    public string? Name { get; init; }
}
```
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;

/// <summary>
/// Bar is reference data — read and list only, no mutations.
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
    : BaseReadOnlyRepository<Bar, int>,
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

### Example 3: Lookup Repository (Single Entity Only)
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;

/// <summary>
/// Baz is resolved by ID only — no listing, no mutations.
/// Used for foreign key resolution.
/// </summary>
public interface IBazRepository : ILookupRepository<Baz, Guid>
{
}
```

### Example 4: Custom Composite (Cherry-Pick Operations)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Operations;

/// <summary>
/// Qux supports Create, Read, and List — but not Update or Delete.
/// This is an append-only data source (e.g., audit log).
/// </summary>
public interface IQuxRepository
    : ICreateOperation<Qux, long>,
      IReadOperation<Qux, long>,
      IListOperation<Qux, long>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Custom composite — no matching base class, so logging is set up manually.
/// </summary>
internal sealed class QuxRepository : IQuxRepository
{
    private readonly ILogger _logger;

    internal QuxRepository(ILogger<QuxRepository> logger)
    {
        this._logger = logger ?? NullLogger<QuxRepository>.Instance;
    }

    public async Task<Qux> CreateAsync(
        Qux entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Qux> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<Qux>> ListAsync(
        CancellationToken cancellationToken = default)
    {
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
        // Full CRUDL
        services.AddSingleton<IFooRepository, FooRepository>();

        // Read-only
        services.AddSingleton<IBarRepository, BarRepository>();

        // Lookup (single read)
        services.AddSingleton<IBazRepository, BazRepository>();

        // Custom composite (append-only)
        services.AddSingleton<IQuxRepository, QuxRepository>();
    }
}
```

## Interface and Class Count Summary

| Component | Count |
|---|---|
| Operation interfaces | 5 |
| Composite interfaces | 5 |
| Base abstract classes | 3 |
| Core entity interface | 1 |
| **Total** | **14** |

## Composite Interface Quick Reference

| Interface | Create | Read | Update | Delete | List |
|---|---|---|---|---|---|
| `ICrudRepository` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IReadOnlyRepository` | ❌ | ✅ | ❌ | ❌ | ✅ |
| `IWriteOnlyRepository` | ✅ | ❌ | ✅ | ✅ | ❌ |
| `IReadWriteRepository` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `ILookupRepository` | ❌ | ✅ | ❌ | ❌ | ❌ |

## Pros

- **Correct logger category names** — log output shows `FooRepository` instead of
  `BaseAsyncRepository<Foo, String>`
- **Same logging convenience** — protected `LogDebug()`, `LogInformation()`, etc.
  methods with level-check optimization, just like `BaseClassWithLogging`
- **Granular composites** — consuming projects express intent (read-only, write-only,
  lookup) directly in their interface declarations
- **Type safety at the DI boundary** — an application layer with `IReadOnlyRepository`
  cannot call Create or Delete; the compiler enforces the restriction
- **Cherry-pick individual operations** — for uncommon combinations like append-only,
  consuming projects compose `ICreateOperation` + `IReadOperation` + `IListOperation`
  directly without needing a library-defined composite
- **Matching base classes** — `BaseReadOnlyRepository` and `BaseWriteOnlyRepository`
  prevent consuming projects from having to implement unused abstract methods

## Cons

- **Duplicated logging methods** — the `LogDebug`, `LogInformation`, etc. methods are
  copy-pasted across `BaseAsyncRepository`, `BaseReadOnlyRepository`, and
  `BaseWriteOnlyRepository` since there is no shared base class
- **No `BaseClassWithLogging` reuse** — existing code in other Roadbed libraries that
  inherits from `BaseClassWithLogging` would use a different pattern than repository
  classes, creating inconsistency across the solution
- **Custom composites lose logging helpers** — Example 4 (`QuxRepository`) has no
  base class to inherit from, so the consuming project must set up `ILogger` manually
  and loses the convenience methods
- **No sync support** — same limitation as Option 1
- **No filtering or pagination** — same limitation as Options 1 and 2
- **No service layer** — same limitation as Options 1 and 2
- **Composite proliferation risk** — the 5 provided composites may not cover every
  combination; teams might request `ICreateReadRepository`, `ICreateListRepository`,
  etc., leading to a combinatorial explosion (2^5 = 32 possible combinations)

## Open Questions This Option Raises

1. **Can the duplicated logging methods be extracted?** A non-generic
   `BaseLoggableRepository` base class with just the `ILogger` field and protected
   methods could sit between the base classes and reduce duplication — but adds
   another layer of inheritance.
2. **Should `BaseClassWithLogging` be updated instead?** Adding a constructor
   overload to `BaseClassWithLogging` that accepts `ILogger` (non-generic) would
   let the base repository classes inherit from it again while still getting correct
   category names. This would eliminate the duplication and maintain consistency
   across the Roadbed solution.
3. **How many composite interfaces are enough?** The 5 provided here cover the most
   common patterns. Should uncommon combinations always be left to consuming projects
   to compose manually from individual operations?
4. **Is `ILookupRepository` too granular?** It wraps a single interface
   (`IReadOperation`). Some may argue the consuming project should just use
   `IReadOperation` directly rather than having an alias.
5. **Does the `Composites` sub-namespace add value?** It separates composites from
   individual operations, but adds another `using` statement. Alternatively, all
   composites could live in the root `Roadbed.Crud` namespace.