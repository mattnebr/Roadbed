# Option 8: Single Interface Per Entity (Marker Interface Pattern)

## Core Philosophy

Radically simplify the consuming project's surface area. Instead of composing
multiple granular operation interfaces into a composite, each entity gets one
interface that declares exactly the methods it supports — directly. Roadbed.Crud
provides marker interfaces and a base class, but the consuming project is not
required to use any composite. The entity's repository interface IS the contract.

This option challenges a core assumption of Options 1-7: that Roadbed.Crud should
pre-define composite interfaces like `ICrudRepository` or `IReadOnlyRepository`.
Instead, it asks: what if the consuming project just writes the methods it needs
on its own interface, and Roadbed.Crud only provides the base class and entity
contract?

## The Problem with Composites

In Options 1-7, a consuming project that needs Create + Read + List (but not Update
or Delete) must either:

- Use `ICrudRepository` and leave Update/Delete unimplemented (Options 5-7)
- Cherry-pick `ICreateOperation + IReadOperation + IListOperation` (Options 3-7)
- Hope a pre-defined composite like `IReadOnlyRepository` matches (it doesn't —
  that one lacks Create)

The combinatorial nature of 5 operations means 31 possible subsets (2^5 - 1). No
library can pre-define all of them. Consuming projects always end up cherry-picking
or accepting methods they don't need.

## The Alternative: Just Declare What You Need
```csharp
// Instead of composing library interfaces...
public interface IFooRepository
    : ICreateOperation<Foo, string>,
      IReadOperation<Foo, string>,
      IListOperation<Foo, string>  // Cherry-picking 3 of 5
{
}

// ...just declare the methods directly:
public interface IFooRepository
{
    Task<Foo> CreateAsync(Foo entity, CancellationToken cancellationToken = default);
    Task<Foo> ReadAsync(string id, CancellationToken cancellationToken = default);
    Task<IList<Foo>> ListAsync(CancellationToken cancellationToken = default);
}
```

The second version is self-documenting, has no dependencies on Roadbed.Crud operation
interfaces, and supports any combination of methods without fighting the type system.

But this raises a question: if the consuming project declares its own methods, what
value does Roadbed.Crud provide? The answer: the base class (with logging and virtual
defaults) and a marker interface to identify repositories in the system.

## Terminology

- **Entity**: Identity-bearing objects. Same as previous options.
- **Repository**: The data access contract. Defined entirely by the consuming project.
- **Marker Interface**: An empty interface that identifies a class as a Roadbed.Crud
  repository without prescribing any methods.

## Namespace Structure
```
Roadbed.Crud
├── IEntity<TId>
├── BaseEntity<TId>
├── IRepository                           # Non-generic marker
├── IRepository<TEntity, TId>             # Generic marker
├── BaseRepository<TEntity, TId>          # Virtual defaults + logging
```

That's it. Five types in Roadbed.Crud.

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
/// This interface carries no methods. Its purpose is to enable:
/// <list type="bullet">
///   <item>Assembly scanning for repository registration in DI</item>
///   <item>Generic constraints in utility classes</item>
///   <item>Identification of repositories in reflection-based tooling</item>
/// </list>
/// </remarks>
public interface IRepository
{
}

/// <summary>
/// Generic marker interface identifying a repository for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// This interface carries no methods. The consuming project's repository interface
/// declares exactly the methods it supports. Roadbed.Crud does not prescribe which
/// CRUDL operations a repository must implement.
/// </remarks>
public interface IRepository<TEntity, TId> : IRepository
    where TEntity : IEntity<TId>
{
}
```

### No Operation Interfaces

This option does not define `ICreateOperation`, `IReadOperation`, etc. The consuming
project declares its own method signatures directly on its repository interface.

### No Composite Interfaces

This option does not define `ICrudRepository`, `IReadOnlyRepository`, etc. There are
no pre-composed subsets to choose from or fight against.

## Base Class
```csharp
namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base repository with logging and virtual CRUDL method defaults.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Provides virtual implementations of all five CRUDL operations that throw
/// <see cref="NotImplementedException"/>. The consuming class overrides only
/// the methods declared on its repository interface.
/// </para>
/// <para>
/// The consuming project's repository interface is independent of this base class.
/// The interface declares whatever methods the entity needs. The base class
/// provides convenient defaults so the consuming class only writes the code
/// that matters.
/// </para>
/// <para>
/// Inherits from <see cref="BaseClassWithLogging"/> (non-generic) for logging
/// convenience methods.
/// </para>
/// </remarks>
public class BaseRepository<TEntity, TId>
    : BaseClassWithLogging,
      IRepository<TEntity, TId>
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

    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override when your repository interface includes a Create method.
    /// </remarks>
    public virtual Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads an entity by its identifier asynchronously.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override when your repository interface includes a Read method.
    /// </remarks>
    public virtual Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override when your repository interface includes an Update method.
    /// </remarks>
    public virtual Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override when your repository interface includes a Delete method.
    /// </remarks>
    public virtual Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Lists all entities asynchronously.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// Override when your repository interface includes a List method.
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

### Example 1: Full CRUDL
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

/// <summary>
/// Foo supports all five CRUDL operations. Each method is declared explicitly.
/// </summary>
public interface IFooRepository : IRepository<Foo, string>
{
    Task<Foo> CreateAsync(Foo entity, CancellationToken cancellationToken = default);
    Task<Foo> ReadAsync(string id, CancellationToken cancellationToken = default);
    Task<Foo> UpdateAsync(Foo entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IList<Foo>> ListAsync(CancellationToken cancellationToken = default);
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

### Example 2: Read-Only
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

/// <summary>
/// Bar is reference data. Only Read and List. No need to inherit from
/// IReadOnlyRepository — just declare what you need.
/// </summary>
public interface IBarRepository : IRepository<Bar, int>
{
    Task<Bar> ReadAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Bar>> ListAsync(CancellationToken cancellationToken = default);
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

### Example 3: Append-Only with Custom Operations
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

/// <summary>
/// Baz is an audit log. Create, Read, List, plus a custom query.
/// No Update, no Delete. The custom method is declared alongside CRUDL methods
/// with no friction — it's just another method on the interface.
/// </summary>
public interface IBazRepository : IRepository<Baz, Guid>
{
    Task<Baz> CreateAsync(Baz entity, CancellationToken cancellationToken = default);
    Task<Baz> ReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IList<Baz>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns audit entries within the specified date range.
    /// </summary>
    Task<IList<Baz>> ListByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
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

    /// <summary>
    /// Custom query — not an override of BaseRepository. This is a direct
    /// implementation of the interface method.
    /// </summary>
    public async Task<IList<Baz>> ListByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug(
            "Listing Baz by date range: {Start} to {End}",
            startDate,
            endDate);
        throw new NotImplementedException();
    }
}
```

### Example 4: Lookup Only (Single Read)
```csharp
namespace MyApp.Data;

using Roadbed.Crud;

/// <summary>
/// Qux is resolved by ID only. No listing, no mutations.
/// </summary>
public interface IQuxRepository : IRepository<Qux, long>
{
    Task<Qux> ReadAsync(long id, CancellationToken cancellationToken = default);
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

    public override async Task<Qux> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Qux: {Id}", id);
        throw new NotImplementedException();
    }
}
```

### Example 5: Assembly Scanning DI Registration

The non-generic `IRepository` marker enables automatic registration:
```csharp
namespace MyApp.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public sealed class DataInstaller : IServiceCollectionInstaller
{
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Manual registration (always works)
        services.AddSingleton<IFooRepository, FooRepository>();
        services.AddSingleton<IBarRepository, BarRepository>();
        services.AddSingleton<IBazRepository, BazRepository>();
        services.AddSingleton<IQuxRepository, QuxRepository>();
    }
}
```
```csharp
// Or: a utility method that scans for IRepository implementations
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all internal repository implementations found in the assembly.
    /// Matches each class to its corresponding public interface.
    /// </summary>
    public static IServiceCollection AddRepositoriesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var repositoryTypes = assembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(IRepository).IsAssignableFrom(t));

        foreach (var implementationType in repositoryTypes)
        {
            var interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i != typeof(IRepository)
                    && typeof(IRepository).IsAssignableFrom(i)
                    && !i.IsGenericType);

            if (interfaceType is not null)
            {
                services.AddSingleton(interfaceType, implementationType);
            }
        }

        return services;
    }
}
```

## Interface and Class Count Summary

| Component | Count |
|---|---|
| Operation interfaces | **0** |
| Composite interfaces | **0** |
| Marker interfaces | 2 (generic + non-generic) |
| Base class | 1 |
| Core entity interface | 1 |
| Non-generic BaseClassWithLogging | 1 (in Roadbed.Common) |
| **Total Roadbed.Crud types** | **4** (+ 1 in Roadbed.Common) |

## Comparison Across All Options

| Aspect | Opt 1 | Opt 5 | Opt 7 | **Opt 8** |
|---|---|---|---|---|
| Types in Roadbed.Crud | 8 | 13 | 16 | **4** |
| Operation interfaces | 5 | 5 | 5 | **0** |
| Composite interfaces | 1 | 5 | 8 | **0** |
| Base classes | 1 | 1 | 2 | **1** |
| Generic params on base | 2 | 2 | 2 | **2** |
| Method signature defined by | Library | Library | Library | **Consumer** |
| Custom methods require | New interface | New interface | New interface | **Same interface** |

## Pros

- **Absolute minimum library surface** — 4 types. Nothing to learn, nothing to fight
- **No combinatorial problem** — consuming projects declare exactly the methods they
  need; no pre-defined composites to search through or cherry-pick from
- **Custom methods are first-class** — `ListByDateRangeAsync` sits next to `ReadAsync`
  on the same interface. No separate `IQueryOperation` needed. No friction between
  standard and custom methods
- **Self-documenting interfaces** — reading `IBazRepository` tells you exactly what
  operations Baz supports. No need to trace through composite inheritance chains
- **Assembly scanning** — the `IRepository` marker enables automatic DI registration
- **No using statements for Roadbed.Crud.Operations** — consuming projects only need
  `using Roadbed.Crud` for `IEntity<TId>` and `IRepository<TEntity, TId>`
- **Easiest to adopt** — consuming projects can start using Roadbed.Crud by adding
  `IEntity<TId>` to their entities and inheriting `BaseRepository`. No need to learn
  the operation/composite interface taxonomy
- **Method signature freedom** — if a consuming project wants `DeleteAsync` to return
  `Task<bool>` instead of `Task`, they just declare it that way. No conflict with a
  library-defined interface

## Cons

- **No shared type for polymorphism** — without `IReadOperation<Foo, string>`, there
  is no way to write code that accepts "any class that can read a Foo." Each
  repository interface is unique. Utility classes like the `FooCacheWarmer` from
  Option 7 are not possible
- **Method signature inconsistency risk** — without library-defined interfaces,
  different consuming projects may declare `ReadAsync` with slightly different
  signatures (e.g., one uses `CancellationToken` with a default, another makes it
  required). Roadbed.Crud cannot enforce consistency across consuming projects
- **Base class virtual methods may not match interface** — if a consuming project
  declares `Task<bool> DeleteAsync(string id, ...)` but the base class has
  `Task DeleteAsync(TId id, ...)`, the signatures don't match and the override
  won't compile. The developer must implement the method directly instead of
  overriding. The base class defaults only help when signatures match exactly
- **No IntelliSense guidance** — when creating a new repository interface, the
  developer must remember the standard CRUDL method signatures. With operation
  interfaces, IntelliSense shows the required methods. Without them, the developer
  is writing from memory or copying from another repository
- **Duplicated method signatures across interfaces** — `ReadAsync` is declared on
  every repository interface that supports reading. In Options 1-7, it is defined
  once on `IReadOperation` and inherited. Here, it is written out each time
- **Marker interface adds minimal value** — `IRepository` and `IRepository<T, TId>`
  carry no methods. Some architects consider marker interfaces an anti-pattern,
  preferring attributes for metadata
- **No service layer guidance** — this option focuses entirely on repositories and
  does not address the service-repository separation from Options 6-7. It could be
  combined with a service layer, but that is not shown here
- **Testing convention still needed** — like Option 5, there is no compile-time check
  that all interface methods are implemented (though the compiler DOES enforce that
  interface methods exist on the class — the risk is only that the base class default
  is used instead of a real implementation)

## Open Questions This Option Raises

1. **Is the polymorphism loss acceptable?** Options 3-7 let consuming code depend on
   `IReadOperation<Foo, string>` to accept any class that reads Foo. Without it,
   every dependency is on a specific repository interface. Does your codebase need
   this polymorphism?
2. **Should the marker interfaces exist at all?** If they only enable assembly
   scanning, an `[Repository]` attribute might be simpler and more idiomatic in
   modern .NET. Marker interfaces are debated in the community.
3. **Can the base class and marker approach be combined with operation interfaces?**
   A hybrid where Roadbed.Crud provides operation interfaces as optional building
   blocks (not required) alongside the marker interface. Consuming projects could
   choose: inherit from `IReadOperation` for consistency, or declare methods directly
   for freedom.
4. **How do you enforce method signature consistency across a team?** Without library
   interfaces to inherit from, code review or a custom Roslyn analyzer would be
   needed to ensure all repositories use the same `ReadAsync` signature.
5. **Is the reduced Roadbed.Crud surface worth the increased consuming project
   responsibility?** Roadbed.Crud shrinks to 4 types, but every consuming interface
   must manually declare its methods. For a solution with 20 entities, that is 20
   interfaces each manually listing their CRUDL methods.