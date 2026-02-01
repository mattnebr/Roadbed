# Option 5: Non-Generic BaseClassWithLogging + Virtual Method Defaults

## Core Philosophy

Maximize simplicity by exploring two ideas simultaneously. First, introduce a
non-generic `BaseClassWithLogging` that accepts only `ILogger` — eliminating the
CRTP third type parameter from Option 4 while still getting correct logger category
names. Second, replace abstract methods with virtual methods that throw
`NotImplementedException`, so consuming classes only override the operations they
actually use. The combination of these two ideas collapses multiple base classes
into a single base class with only two generic parameters.

## Non-Generic BaseClassWithLogging

### The Proposal

A new non-generic variant of `BaseClassWithLogging` that lives alongside the existing
generic version in Roadbed.Common. It does not replace the generic version — both can
coexist. The non-generic version is simpler and serves the specific needs of
Roadbed.Crud's base classes.
```csharp
namespace Roadbed;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Base class with logging implemented via direct logger injection.
/// </summary>
/// <remarks>
/// This is the non-generic variant of <see cref="BaseClassWithLogging{TCategoryName}"/>.
/// The logger category name is determined by the concrete class that injects
/// <c>ILogger&lt;T&gt;</c> in its constructor. This avoids the need for the Curiously
/// Recurring Template Pattern (CRTP) or a third generic type parameter.
/// </remarks>
public abstract class BaseClassWithLogging
{
    #region Private Fields

    /// <summary>
    /// Container for the protected property Logger.
    /// </summary>
    private readonly ILogger _logger;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClassWithLogging"/> class
    /// with no logging.
    /// </summary>
    protected BaseClassWithLogging()
    {
        this._logger = NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClassWithLogging"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance. Pass <c>ILogger&lt;T&gt;</c> from the concrete class to
    /// ensure the correct logger category name in log output.
    /// </param>
    protected BaseClassWithLogging(ILogger logger)
    {
        this._logger = logger ?? NullLogger.Instance;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => this._logger;

    #endregion Protected Properties

    #region Protected Methods

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Trace"/> if the level is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogTrace(string message)
    {
        if (this._logger.IsEnabled(LogLevel.Trace))
        {
            this._logger.LogTrace(message);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Trace"/> if the level is enabled.
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
    /// Logs a message at <see cref="LogLevel.Debug"/> if the level is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogDebug(string message)
    {
        if (this._logger.IsEnabled(LogLevel.Debug))
        {
            this._logger.LogDebug(message);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Debug"/> if the level is enabled.
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
    /// Logs a message at <see cref="LogLevel.Information"/> if the level is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogInformation(string message)
    {
        if (this._logger.IsEnabled(LogLevel.Information))
        {
            this._logger.LogInformation(message);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Information"/> if the level is enabled.
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
    /// Logs a message at <see cref="LogLevel.Warning"/> if the level is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogWarning(string message)
    {
        if (this._logger.IsEnabled(LogLevel.Warning))
        {
            this._logger.LogWarning(message);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Warning"/> if the level is enabled.
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
    /// Logs a message at <see cref="LogLevel.Error"/> if the level is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogError(string message)
    {
        if (this._logger.IsEnabled(LogLevel.Error))
        {
            this._logger.LogError(message);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Error"/> if the level is enabled.
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
    protected void LogError(Exception exception, string message)
    {
        this._logger.LogError(exception, message);
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

### Key Differences from Generic Version

| Aspect | `BaseClassWithLogging<T>` | `BaseClassWithLogging` (new) |
|---|---|---|
| Logger type | `ILogger<T>` | `ILogger` |
| Logger visibility | Public | Protected |
| LoggerFactory | Available | Not available |
| Constructors | 3 (parameterless, ILogger, ILoggerFactory) | 2 (parameterless, ILogger) |
| Log helper visibility | Public | Protected |
| Category name source | Generic parameter `T` | Injected `ILogger<T>` from concrete class |
| CRTP needed | Yes (for correct category) | No |

### Why Protected Instead of Public

The generic version exposes `Logger` and all `Log*` methods as public because the
generic parameter `T` guarantees a typed logger — callers can reason about the
category. The non-generic version hides these as protected because `ILogger` without
a type parameter is an implementation detail. Consuming code should call repository
methods, not log on behalf of a repository.

## Virtual Method Defaults

### The Idea

Instead of abstract methods, the single base class provides virtual methods that
throw `NotImplementedException`. Consuming classes override only the operations they
actually implement. The consuming interface still controls which operations are
exposed to callers — the virtual defaults are a safety net, not a public API surface.

### Why This Works
```
┌──────────────────────────────────┐
│     IReadOnlyRepository          │ ← Interface: exposes Read + List only
├──────────────────────────────────┤
│     BarRepository                │ ← Overrides: ReadAsync + ListAsync
├──────────────────────────────────┤
│     BaseRepository               │ ← Has virtual: Create, Read, Update, Delete, List
│                                  │    (Create, Update, Delete throw NotImplementedException
│                                  │     but are never called because BarRepository's
│                                  │     interface doesn't expose them)
└──────────────────────────────────┘
```

The compiler enforces the contract at the caller boundary (the interface). The base
class's unreachable virtual methods are a development convenience — they prevent
the consuming class from needing to implement methods its interface doesn't expose.

## Terminology

- **Entity**: Identity-bearing objects. Same as previous options.
- **Repository**: The data access abstraction.

## Naming Conventions

Same as Option 4. No changes to interface naming.

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
├── BaseRepository<T, TId>              ← ONE base class (not three)
```

## Interface Definitions

All operation interfaces and composite interfaces are identical to Option 4.
They are not repeated here — refer to Option 4 for the full definitions of:

- `IEntity<TId>`
- `ICreateOperation<TEntity, TId>`
- `IReadOperation<TEntity, TId>`
- `IUpdateOperation<TEntity, TId>`
- `IDeleteOperation<TEntity, TId>`
- `IListOperation<TEntity, TId>`
- `IQueryOperation<TEntity, TId, TFilter>`
- `ICrudRepository<TEntity, TId>`
- `IReadOnlyRepository<TEntity, TId>`
- `ILookupRepository<TEntity, TId>`
- `IWriteOnlyRepository<TEntity, TId>`
- `IReadWriteRepository<TEntity, TId>`

## The Single Base Class
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Composites;

/// <summary>
/// Base repository with logging and virtual CRUDL method defaults.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// All CRUDL methods are virtual and throw <see cref="NotImplementedException"/> by
/// default. Consuming classes override only the methods required by their repository
/// interface. The interface controls which operations are exposed to callers — the
/// virtual defaults exist so that consuming classes are not forced to implement
/// methods their interface does not expose.
/// </para>
/// <para>
/// This class implements <see cref="ICrudRepository{TEntity, TId}"/> (the full
/// composite) so that it satisfies any subset interface a consuming class might
/// use: <see cref="IReadOnlyRepository{TEntity, TId}"/>,
/// <see cref="IWriteOnlyRepository{TEntity, TId}"/>,
/// <see cref="IReadWriteRepository{TEntity, TId}"/>, etc.
/// </para>
/// <para>
/// Inherits from the non-generic <see cref="BaseClassWithLogging"/> to provide
/// logging convenience methods. The consuming class injects
/// <c>ILogger&lt;T&gt;</c> to ensure the correct logger category name.
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
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override this method in the consuming class when the repository interface
    /// includes <see cref="Operations.ICreateOperation{TEntity, TId}"/>.
    /// </remarks>
    public virtual Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override this method in the consuming class when the repository interface
    /// includes <see cref="Operations.IReadOperation{TEntity, TId}"/>.
    /// </remarks>
    public virtual Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override this method in the consuming class when the repository interface
    /// includes <see cref="Operations.IUpdateOperation{TEntity, TId}"/>.
    /// </remarks>
    public virtual Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override this method in the consuming class when the repository interface
    /// includes <see cref="Operations.IDeleteOperation{TEntity, TId}"/>.
    /// </remarks>
    public virtual Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override this method in the consuming class when the repository interface
    /// includes <see cref="Operations.IListOperation{TEntity, TId}"/>.
    /// </remarks>
    public virtual Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion Public Methods
}
```

## Consuming Project Examples

### Example 1: Full CRUDL Repository

All five methods overridden because the interface exposes all five.
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
    : BaseRepository<Foo, string>,
//                   ^^^^^^^^^^^
//                   Only TWO generic parameters (no CRTP)
      IFooRepository
{
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    //  ^^^^^^^^^^^^
    //  ILogger<FooRepository> → logger category = "MyApp.Data.Internal.FooRepository"
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

### Example 2: Read-Only Repository (Only Override Read + List)
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

public interface IBarRepository : IReadOnlyRepository<Bar, int>
{
}
```
```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

/// <summary>
/// Bar is reference data — only Read and List are overridden.
/// CreateAsync, UpdateAsync, and DeleteAsync remain as the base class
/// NotImplementedException defaults, but they are unreachable because
/// IBarRepository only exposes IReadOnlyRepository.
/// </summary>
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

    // CreateAsync, UpdateAsync, DeleteAsync are NOT overridden.
    // They throw NotImplementedException in the base class.
    // IBarRepository does not expose them, so callers cannot reach them.
}
```

### Example 3: Append-Only Repository (Create + Read + List)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Operations;

/// <summary>
/// Baz is append-only (e.g., audit log). No Update, no Delete.
/// Cherry-picks individual operations from Roadbed.Crud.Operations.
/// </summary>
public interface IBazRepository
    : ICreateOperation<Baz, Guid>,
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

    // UpdateAsync and DeleteAsync are NOT overridden.
    // IBazRepository does not expose them.
}
```

### Example 4: CRUDL + Query
```csharp
namespace MyApp.Data;

using Roadbed.Crud.Composites;
using Roadbed.Crud.Operations;

public interface IQuxRepository
    : ICrudRepository<Qux, long>,
      IQueryOperation<Qux, long, QuxFilter>
{
}
```
```csharp
namespace MyApp.Data;

public sealed record QuxFilter
{
    public string? SearchText { get; init; }
    public int PageSize { get; init; } = 25;
    public long? AfterId { get; init; }
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

    /// <summary>
    /// IQueryOperation is not on BaseRepository, so this is a direct
    /// interface implementation (not an override).
    /// </summary>
    public async Task<IList<Qux>> QueryAsync(
        QuxFilter filter,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug(
            "Querying Qux: SearchText={SearchText}, PageSize={PageSize}",
            filter.SearchText ?? "(none)",
            filter.PageSize);
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
        services.AddSingleton<IBarRepository, BarRepository>();
        services.AddSingleton<IBazRepository, BazRepository>();
        services.AddSingleton<IQuxRepository, QuxRepository>();
    }
}
```

## Interface and Class Count Summary

| Component | Count |
|---|---|
| Operation interfaces | 6 (5 CRUDL + 1 Query) |
| Composite interfaces | 5 |
| Base abstract classes | **1** (down from 3 in Options 3/4) |
| Core entity interface | 1 |
| Non-generic BaseClassWithLogging | 1 (in Roadbed.Common) |
| **Total Roadbed.Crud types** | **13** |

## Comparison to Option 4

| Aspect | Option 4 (CRTP) | Option 5 (This Option) |
|---|---|---|
| Base classes in Roadbed.Crud | 3 | **1** |
| Generic parameters on base | 3 (`TEntity, TId, TRepo`) | **2** (`TEntity, TId`) |
| Method style | Abstract (must override all) | **Virtual** (override only what you need) |
| Logging base | Generic `BaseClassWithLogging<T>` | **Non-generic** `BaseClassWithLogging` |
| Logger property visibility | Public | **Protected** |
| CRTP required | Yes | **No** |
| Compile-time missing override detection | ✅ Yes | ❌ No (runtime NotImplementedException) |
| Consuming class declaration | `BaseAsyncRepository<Foo, string, FooRepository>` | **`BaseRepository<Foo, string>`** |

## Pros

- **Simplest base class surface** — ONE base class with TWO generic parameters;
  consuming declarations are clean: `BaseRepository<Foo, string>`
- **No CRTP** — no confusing "pass yourself as a type parameter" pattern; logger
  category is still correct because `ILogger<FooRepository>` carries the category
- **Override only what you need** — a read-only repository overrides 2 methods instead
  of implementing 5; the unreachable methods are harmless because the interface prevents
  callers from reaching them
- **Eliminates multiple base classes** — no need for `BaseReadOnlyRepository`,
  `BaseWriteOnlyRepository`, etc.; one base class serves all composite patterns
- **Protected logging** — `LogDebug`, `LogInformation`, etc. are protected instead of
  public; consumers of the repository interface cannot call logging methods on the
  repository instance
- **Non-generic BaseClassWithLogging coexists** — the existing generic version is
  untouched; other Roadbed libraries continue using it; Roadbed.Crud uses the simpler
  non-generic version
- **Correct logger category names** — same benefit as Options 3 and 4
- **Performance-optimized logging** — same `IsEnabled` check before formatting, same
  as `BaseClassWithLogging<T>`

## Cons

- **No compile-time detection of missing overrides** — if a consuming class implements
  `ICrudRepository` but forgets to override `DeleteAsync`, the error surfaces at
  runtime as `NotImplementedException` instead of a compile error. This is the most
  significant trade-off of this option.
- **Unreachable code in base class** — for a read-only repository, the base class
  carries `CreateAsync`, `UpdateAsync`, and `DeleteAsync` implementations that will
  never be called. Some teams consider this a code smell, even though it has no
  runtime impact.
- **Two BaseClassWithLogging variants** — the Roadbed.Common project now has both a
  generic and non-generic version. Developers must understand when to use which. The
  convention would be: use the generic version when the inheriting class IS the logger
  category (e.g., services, jobs); use the non-generic version when a subclass will
  provide the logger (e.g., repository base classes).
- **Logger property type is ILogger, not ILogger\<T\>** — some libraries or analyzers
  expect the typed version. The non-generic base exposes `ILogger`, which is the
  non-typed interface. This is functionally equivalent but less specific.
- **BaseRepository is not sealed-friendly** — the class cannot be sealed because
  consuming classes must inherit from it. However, the consuming classes themselves
  should be sealed.
- **No sync support** — async-only (can be combined with Option 2)
- **Virtual methods are public** — even though the interface controls visibility,
  someone casting to `BaseRepository<Foo, string>` directly (bypassing the interface)
  could call `CreateAsync` on a read-only repository and get
  `NotImplementedException`. In practice, DI registration with the interface type
  prevents this.
- **ILoggerFactory constructor not available** — the non-generic base only accepts
  `ILogger`; consuming projects that prefer `ILoggerFactory` injection would need to
  create the logger themselves before calling `base(loggerFactory.CreateLogger<T>())`

## Mitigation for Missing Override Detection

The biggest con is the runtime error for missing overrides. Here are strategies
consuming projects can use to catch this early:

### Strategy 1: Unit Test Convention

A standard unit test that verifies every method on the interface does not throw
`NotImplementedException`:
```csharp
/// <summary>
/// Unit test to verify that all interface methods are overridden.
/// </summary>
[TestMethod]
public void AllInterfaceMethods_Invoked_DoNotThrowNotImplementedException()
{
    // Arrange (Given)
    var repository = new FooRepository(NullLogger<FooRepository>.Instance);
    bool notImplementedThrown = false;

    // Act / Assert (When / Then)
    // Each method that is part of ICrudRepository should not throw
    // NotImplementedException. Test each one.
    try
    {
        _ = repository.CreateAsync(new Foo(), CancellationToken.None);
    }
    catch (NotImplementedException)
    {
        notImplementedThrown = true;
    }

    Assert.IsFalse(
        notImplementedThrown,
        "CreateAsync should be overridden since IFooRepository includes ICreateOperation.");
}
```

### Strategy 2: Reflection-Based Test Helper

A reusable test helper in Roadbed.Crud.Test that scans a repository class and
verifies all methods from its interface are overridden:
```csharp
/// <summary>
/// Verifies that all methods declared on TInterface are overridden on TRepository
/// (not using the base class default).
/// </summary>
public static void AssertAllMethodsOverridden<TInterface, TRepository>()
    where TRepository : class
{
    var interfaceMethods = typeof(TInterface)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance);

    foreach (var method in interfaceMethods)
    {
        var repoMethod = typeof(TRepository).GetMethod(
            method.Name,
            method.GetParameters().Select(p => p.ParameterType).ToArray());

        Assert.IsNotNull(
            repoMethod,
            $"{method.Name} should exist on {typeof(TRepository).Name}.");

        Assert.AreNotEqual(
            typeof(BaseRepository<,>),
            repoMethod.DeclaringType?.GetGenericTypeDefinition(),
            $"{method.Name} should be overridden in {typeof(TRepository).Name}, " +
            $"not use the BaseRepository default.");
    }
}

// Usage in test class:
[TestMethod]
public void FooRepository_AllMethods_AreOverridden()
{
    RepositoryTestHelper.AssertAllMethodsOverridden<IFooRepository, FooRepository>();
}
```

## Open Questions This Option Raises

1. **Should the non-generic `BaseClassWithLogging` live in Roadbed.Common alongside
   the generic version, or in Roadbed.Crud?** If it lives in Roadbed.Common, other
   libraries can use it too. If it lives in Roadbed.Crud, it stays scoped to this use
   case but creates a dependency direction issue.
2. **Is `NotImplementedException` the right exception?** `NotSupportedException` is
   the .NET convention for "this operation is not supported by this class."
   `NotImplementedException` means "this hasn't been built yet." Which semantic is
   more accurate for unoverridden base class methods?
3. **Should the base class be `abstract` or just `class`?** Currently it is not
   abstract because all methods have implementations (virtual defaults). Making it
   abstract prevents direct instantiation, which is good. But can a class be abstract
   if it has no abstract members? (Yes — C# allows abstract classes with no abstract
   members. The `abstract` keyword just prevents instantiation.)
4. **Does the non-generic `BaseClassWithLogging` need `BeginScope`?** The generic
   version has it. Should the non-generic version also provide scope methods for
   structured logging enrichment?
5. **Should the two `BaseClassWithLogging` variants share a common base?** A
   non-generic `BaseClassWithLogging` could be the root, and the generic version could
   inherit from it. This would eliminate duplication between the two variants but
   changes the existing class hierarchy.