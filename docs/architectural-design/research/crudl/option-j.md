# Option 10: Full Prescriptive — Sync/Async/Both, Compiler Enforcement, Hierarchical Composites

## Core Philosophy

Maximum compiler enforcement and IntelliSense support across every combination of execution mode (async, sync, both) and operation shape (CRUD, ReadOnly, Lookup, WriteOnly, ReadWrite). Abstract base classes force consuming classes to implement exactly the methods their composite requires — nothing more, nothing less. No `NotImplementedException`. No dead code. The compiler catches every mistake before runtime.

This option represents the full ceiling of what Roadbed.Crud could provide. It answers the question: "How many types exist when we model every valid combination with full type safety?"

## Key Differences from Option 9

|Aspect|Option 9|Option 10|
|---|---|---|
|Base class approach|Virtual with `NotImplementedException`|**Abstract** (compiler enforced)|
|Dead code|Yes (unused virtual methods)|**No** (only required methods exist)|
|Base classes per layer|1 (`BaseRepository`)|**15** (one per composite × mode)|
|Execution modes|Async only|**Async, Sync, Both**|
|Entity base types|`IEntity<TId>` only|**IEntity + BaseEntityRecord + BaseEntityClass**|
|IntelliSense stubs|Manual|**"Implement abstract class" generates all**|
|Composite hierarchy|Flat (no subset relationships)|**Hierarchical** (CRUD assignable to ReadOnly)|

## Design Principles

1. **Abstract over virtual** — the compiler says "you forgot to implement ReadAsync," not a runtime `NotImplementedException` three weeks later in production
2. **One base class per composite per mode** — `BaseAsyncReadOnlyRepository` has exactly 2 abstract methods (Read + List). `BaseAsyncCrudRepository` has exactly 5. Each base matches its composite perfectly
3. **Three execution modes** — Async for REST APIs (Roadbed.NET). Sync for file I/O (Roadbed.IO). Both for libraries that serve multiple consumers
4. **Hierarchical composites** — `ICrudRepository` IS-A `IReadOnlyRepository`. A CRUD repository can be used anywhere a read-only repository is expected. The type system enforces Liskov Substitution
5. **Shared operations across layers** — same operation interfaces for repositories and services (from Option 7/9)
6. **Three consumption levels** — composites for speed, cherry-pick for precision, composite + custom for domain-specific extensions (from Option 9)
7. **Service layer is optional** — repository bases are abstract (data access is always custom). Service bases are virtual with pass-through defaults (business logic is optional)

## Naming Conventions

|Mode|Interface Prefix|Base Class Prefix|Example|
|---|---|---|---|
|Async|`IAsync*`|`BaseAsync*`|`IAsyncCrudRepository`, `BaseAsyncCrudRepository`|
|Sync|`ISync*`|`BaseSync*`|`ISyncCrudRepository`, `BaseSyncCrudRepository`|
|Both|_(none)_|`Base*`|`ICrudRepository`, `BaseCrudRepository`|

The **unprefixed name always means both sync and async**.

## Namespace Structure

```
Roadbed.Crud
├── IEntity<TId>
├── BaseEntityRecord<TId>
├── BaseEntityClass<TId>
├── IRepository
├── IRepository<TEntity, TId>
│
├── Operations/
│   ├── ICreateOperation<T, TId>              # Combined (Sync + Async)
│   ├── IReadOperation<T, TId>
│   ├── IUpdateOperation<T, TId>
│   ├── IDeleteOperation<T, TId>
│   ├── IListOperation<T, TId>
│   ├── Async/
│   │   ├── IAsyncCreateOperation<T, TId>
│   │   ├── IAsyncReadOperation<T, TId>
│   │   ├── IAsyncUpdateOperation<T, TId>
│   │   ├── IAsyncDeleteOperation<T, TId>
│   │   └── IAsyncListOperation<T, TId>
│   └── Sync/
│       ├── ISyncCreateOperation<T, TId>
│       ├── ISyncReadOperation<T, TId>
│       ├── ISyncUpdateOperation<T, TId>
│       ├── ISyncDeleteOperation<T, TId>
│       └── ISyncListOperation<T, TId>
│
├── Repositories/
│   ├── ICrudRepository<T, TId>               # Combined composites
│   ├── IReadOnlyRepository<T, TId>
│   ├── ILookupRepository<T, TId>
│   ├── IWriteOnlyRepository<T, TId>
│   ├── IReadWriteRepository<T, TId>
│   ├── BaseCrudRepository<T, TId>            # Combined bases
│   ├── BaseReadOnlyRepository<T, TId>
│   ├── BaseLookupRepository<T, TId>
│   ├── BaseWriteOnlyRepository<T, TId>
│   ├── BaseReadWriteRepository<T, TId>
│   ├── Async/
│   │   ├── IAsyncCrudRepository<T, TId>
│   │   ├── IAsyncReadOnlyRepository<T, TId>
│   │   ├── IAsyncLookupRepository<T, TId>
│   │   ├── IAsyncWriteOnlyRepository<T, TId>
│   │   ├── IAsyncReadWriteRepository<T, TId>
│   │   ├── BaseAsyncCrudRepository<T, TId>
│   │   ├── BaseAsyncReadOnlyRepository<T, TId>
│   │   ├── BaseAsyncLookupRepository<T, TId>
│   │   ├── BaseAsyncWriteOnlyRepository<T, TId>
│   │   └── BaseAsyncReadWriteRepository<T, TId>
│   └── Sync/
│       ├── ISyncCrudRepository<T, TId>
│       ├── ISyncReadOnlyRepository<T, TId>
│       ├── ISyncLookupRepository<T, TId>
│       ├── ISyncWriteOnlyRepository<T, TId>
│       ├── ISyncReadWriteRepository<T, TId>
│       ├── BaseSyncCrudRepository<T, TId>
│       ├── BaseSyncReadOnlyRepository<T, TId>
│       ├── BaseSyncLookupRepository<T, TId>
│       ├── BaseSyncWriteOnlyRepository<T, TId>
│       └── BaseSyncReadWriteRepository<T, TId>
│
├── Services/
│   ├── ICrudService<T, TId>                  # Combined composites
│   ├── IReadOnlyService<T, TId>
│   ├── ILookupService<T, TId>
│   ├── IWriteOnlyService<T, TId>
│   ├── IReadWriteService<T, TId>
│   ├── BaseCrudService<T, TId>               # Combined bases
│   ├── BaseReadOnlyService<T, TId>
│   ├── BaseLookupService<T, TId>
│   ├── BaseWriteOnlyService<T, TId>
│   ├── BaseReadWriteService<T, TId>
│   ├── Async/
│   │   ├── IAsyncCrudService<T, TId>
│   │   ├── IAsyncReadOnlyService<T, TId>
│   │   ├── IAsyncLookupService<T, TId>
│   │   ├── IAsyncWriteOnlyService<T, TId>
│   │   ├── IAsyncReadWriteService<T, TId>
│   │   ├── BaseAsyncCrudService<T, TId>
│   │   ├── BaseAsyncReadOnlyService<T, TId>
│   │   ├── BaseAsyncLookupService<T, TId>
│   │   ├── BaseAsyncWriteOnlyService<T, TId>
│   │   └── BaseAsyncReadWriteService<T, TId>
│   └── Sync/
│       ├── ISyncCrudService<T, TId>
│       ├── ISyncReadOnlyService<T, TId>
│       ├── ISyncLookupService<T, TId>
│       ├── ISyncWriteOnlyService<T, TId>
│       ├── ISyncReadWriteService<T, TId>
│       ├── BaseSyncCrudService<T, TId>
│       ├── BaseSyncReadOnlyService<T, TId>
│       ├── BaseSyncLookupService<T, TId>
│       ├── BaseSyncWriteOnlyService<T, TId>
│       └── BaseSyncReadWriteService<T, TId>
```

---

## Entity Layer

### IEntity

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

### BaseEntityRecord

For entities that benefit from value-based equality, immutability, and `with` expressions. Most DTOs from APIs, configuration objects, and value-rich domain objects.

```csharp
namespace Roadbed.Crud;

/// <summary>
/// Base entity implementation as a record type.
/// </summary>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Records provide value-based equality, immutability, and <c>with</c> expressions.
/// Use for DTOs, API responses, configuration objects, and entities where structural
/// equality is meaningful.
/// </para>
/// <para>
/// The <see cref="Id"/> property is virtual to allow consuming records to override
/// it with <c>init</c> or <c>required</c> modifiers.
/// </para>
/// </remarks>
public abstract record BaseEntityRecord<TId> : IEntity<TId>
{
    /// <inheritdoc/>
    public virtual TId? Id { get; set; }
}
```

### BaseEntityClass

For entities that need reference-based identity, mutable state, or complex inheritance hierarchies. Domain entities with behavior, entities managed by ORMs.

```csharp
namespace Roadbed.Crud;

/// <summary>
/// Base entity implementation as a class type.
/// </summary>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Classes provide reference-based identity and mutable state. Use for domain
/// entities with behavior, ORM-managed entities, or entities that require complex
/// inheritance hierarchies.
/// </para>
/// <para>
/// The <see cref="Id"/> property is virtual to allow consuming classes to override
/// with custom getter/setter logic.
/// </para>
/// </remarks>
public abstract class BaseEntityClass<TId> : IEntity<TId>
{
    /// <inheritdoc/>
    public virtual TId? Id { get; set; }
}
```

### When to Use Each

|Scenario|Use|Rationale|
|---|---|---|
|API response DTO|`BaseEntityRecord<TId>`|Value equality, immutability intent, `init` properties|
|Configuration object|`BaseEntityRecord<TId>`|Snapshot semantics, `with` for variants|
|ORM-managed entity|`BaseEntityClass<TId>`|EF Core change tracking, mutable state|
|Domain entity with methods|`BaseEntityClass<TId>`|Behavior + reference identity|
|File-backed entity|Either|Depends on mutation needs|

---

## Marker Interfaces

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

---

## Operation Interfaces

### Async Operations

```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Create operation for an entity.
/// </summary>
public interface IAsyncCreateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
public interface IAsyncReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
public interface IAsyncUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
public interface IAsyncDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    Task DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
public interface IAsyncListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    Task<IList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);
}
```

### Sync Operations

```csharp
namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Create operation for an entity.
/// </summary>
public interface ISyncCreateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    TEntity Create(TEntity entity);
}

/// <summary>
/// Defines the synchronous Read operation for an entity.
/// </summary>
public interface ISyncReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    TEntity Read(TId id);
}

/// <summary>
/// Defines the synchronous Update operation for an entity.
/// </summary>
public interface ISyncUpdateOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    TEntity Update(TEntity entity);
}

/// <summary>
/// Defines the synchronous Delete operation for an entity.
/// </summary>
public interface ISyncDeleteOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    void Delete(TId id);
}

/// <summary>
/// Defines the synchronous List operation for an entity.
/// </summary>
public interface ISyncListOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    IList<TEntity> List();
}
```

### Combined Operations

Combined operation interfaces are the union of their async and sync counterparts. They carry no new methods — they exist to enable clean cherry-picking for the "both" mode.

```csharp
namespace Roadbed.Crud.Operations;

using Roadbed.Crud.Operations.Async;
using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines both synchronous and asynchronous Create operations for an entity.
/// </summary>
/// <remarks>
/// Union of <see cref="IAsyncCreateOperation{TEntity, TId}"/> and
/// <see cref="ISyncCreateOperation{TEntity, TId}"/>. Carries no new members.
/// Use for cherry-picking when you need both sync and async on a single operation.
/// </remarks>
public interface ICreateOperation<TEntity, in TId>
    : IAsyncCreateOperation<TEntity, TId>,
      ISyncCreateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IReadOperation<TEntity, in TId>
    : IAsyncReadOperation<TEntity, TId>,
      ISyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IUpdateOperation<TEntity, in TId>
    : IAsyncUpdateOperation<TEntity, TId>,
      ISyncUpdateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IDeleteOperation<TEntity, in TId>
    : IAsyncDeleteOperation<TEntity, TId>,
      ISyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IListOperation<TEntity, in TId>
    : IAsyncListOperation<TEntity, TId>,
      ISyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Complete Operation Interface Catalog

|#|Interface|Namespace Suffix|Methods|
|---|---|---|---|
|1|`IAsyncCreateOperation<T, TId>`|`.Async`|`Task<T> CreateAsync(T, CT)`|
|2|`IAsyncReadOperation<T, TId>`|`.Async`|`Task<T> ReadAsync(TId, CT)`|
|3|`IAsyncUpdateOperation<T, TId>`|`.Async`|`Task<T> UpdateAsync(T, CT)`|
|4|`IAsyncDeleteOperation<T, TId>`|`.Async`|`Task DeleteAsync(TId, CT)`|
|5|`IAsyncListOperation<T, TId>`|`.Async`|`Task<IList<T>> ListAsync(CT)`|
|6|`ISyncCreateOperation<T, TId>`|`.Sync`|`T Create(T)`|
|7|`ISyncReadOperation<T, TId>`|`.Sync`|`T Read(TId)`|
|8|`ISyncUpdateOperation<T, TId>`|`.Sync`|`T Update(T)`|
|9|`ISyncDeleteOperation<T, TId>`|`.Sync`|`void Delete(TId)`|
|10|`ISyncListOperation<T, TId>`|`.Sync`|`IList<T> List()`|
|11|`ICreateOperation<T, TId>`|_(root)_|Async + Sync Create|
|12|`IReadOperation<T, TId>`|_(root)_|Async + Sync Read|
|13|`IUpdateOperation<T, TId>`|_(root)_|Async + Sync Update|
|14|`IDeleteOperation<T, TId>`|_(root)_|Async + Sync Delete|
|15|`IListOperation<T, TId>`|_(root)_|Async + Sync List|

**Operation count: 15**

---

## Repository Composite Interfaces

### Composite Hierarchy Design

Composites form a hierarchy where broader composites inherit from narrower ones. This enables Liskov Substitution: a CRUD repository can be used anywhere a read-only repository is expected.

**Async hierarchy:**

```
IRepository<T, TId>
  ├── IAsyncLookupRepository (Read)
  │     ├── IAsyncReadOnlyRepository (Read + List)
  │     │     └── IAsyncCrudRepository ─────────┐
  │     └─┐                                     │
  │       └── IAsyncReadWriteRepository ────────┘
  └── IAsyncWriteOnlyRepository (Create + Update + Delete)
        └── IAsyncReadWriteRepository (Read + CUD)
              └── IAsyncCrudRepository (all 5)
```

The sync and combined hierarchies follow the same shape.

### Async Repository Composites

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async lookup repository. Read only, no listing.
/// </summary>
public interface IAsyncLookupRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IAsyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Async read-only repository. Read and List.
/// </summary>
public interface IAsyncReadOnlyRepository<TEntity, TId>
    : IAsyncLookupRepository<TEntity, TId>,
      IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Async write-only repository. Create, Update, and Delete.
/// </summary>
public interface IAsyncWriteOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IAsyncCreateOperation<TEntity, TId>,
      IAsyncUpdateOperation<TEntity, TId>,
      IAsyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Async read-write repository. CRUD without List.
/// </summary>
public interface IAsyncReadWriteRepository<TEntity, TId>
    : IAsyncLookupRepository<TEntity, TId>,
      IAsyncWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Async CRUD repository. All five operations.
/// </summary>
public interface IAsyncCrudRepository<TEntity, TId>
    : IAsyncReadOnlyRepository<TEntity, TId>,
      IAsyncReadWriteRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Sync Repository Composites

Same hierarchy, sync operations and `ISync*` prefixes.

```csharp
namespace Roadbed.Crud.Repositories.Sync;

using Roadbed.Crud.Operations.Sync;

public interface ISyncLookupRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ISyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface ISyncReadOnlyRepository<TEntity, TId>
    : ISyncLookupRepository<TEntity, TId>,
      ISyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface ISyncWriteOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      ISyncCreateOperation<TEntity, TId>,
      ISyncUpdateOperation<TEntity, TId>,
      ISyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface ISyncReadWriteRepository<TEntity, TId>
    : ISyncLookupRepository<TEntity, TId>,
      ISyncWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface ISyncCrudRepository<TEntity, TId>
    : ISyncReadOnlyRepository<TEntity, TId>,
      ISyncReadWriteRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Combined Repository Composites

Combined composites inherit from both their async and sync counterparts. They also explicitly inherit from combined composites at lower levels to ensure full type compatibility.

```csharp
namespace Roadbed.Crud.Repositories;

using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Combined lookup repository. Both sync and async Read.
/// </summary>
public interface ILookupRepository<TEntity, TId>
    : IAsyncLookupRepository<TEntity, TId>,
      ISyncLookupRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Combined read-only repository. Both sync and async Read + List.
/// </summary>
public interface IReadOnlyRepository<TEntity, TId>
    : IAsyncReadOnlyRepository<TEntity, TId>,
      ISyncReadOnlyRepository<TEntity, TId>,
      ILookupRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Combined write-only repository. Both sync and async Create + Update + Delete.
/// </summary>
public interface IWriteOnlyRepository<TEntity, TId>
    : IAsyncWriteOnlyRepository<TEntity, TId>,
      ISyncWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Combined read-write repository. Both sync and async CRUD without List.
/// </summary>
public interface IReadWriteRepository<TEntity, TId>
    : IAsyncReadWriteRepository<TEntity, TId>,
      ISyncReadWriteRepository<TEntity, TId>,
      ILookupRepository<TEntity, TId>,
      IWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

/// <summary>
/// Combined CRUD repository. Both sync and async for all five operations.
/// </summary>
public interface ICrudRepository<TEntity, TId>
    : IAsyncCrudRepository<TEntity, TId>,
      ISyncCrudRepository<TEntity, TId>,
      IReadOnlyRepository<TEntity, TId>,
      IReadWriteRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Type Compatibility Matrix (Async)

"Can I assign a repository of type [column] to a parameter of type [row]?"

|↓ Accepts / From →|Lookup|ReadOnly|WriteOnly|ReadWrite|CRUD|
|---|:-:|:-:|:-:|:-:|:-:|
|**IAsyncLookupRepository**|✅|✅|❌|✅|✅|
|**IAsyncReadOnlyRepository**|❌|✅|❌|❌|✅|
|**IAsyncWriteOnlyRepository**|❌|❌|✅|✅|✅|
|**IAsyncReadWriteRepository**|❌|❌|❌|✅|✅|
|**IAsyncCrudRepository**|❌|❌|❌|❌|✅|

The sync and combined matrices follow the same pattern. Additionally, any combined composite is assignable to both its async and sync counterparts:

```csharp
ICrudRepository<Foo, string> combined = ...;

IAsyncCrudRepository<Foo, string> asyncRef = combined;     // ✅
ISyncCrudRepository<Foo, string> syncRef = combined;       // ✅
IAsyncReadOnlyRepository<Foo, string> asyncRo = combined;  // ✅
ISyncLookupRepository<Foo, string> syncLookup = combined;  // ✅
IReadOnlyRepository<Foo, string> combinedRo = combined;    // ✅
```

### Complete Repository Composite Catalog

|#|Interface|Mode|Inherits From|Methods|
|---|---|---|---|---|
|1|`IAsyncLookupRepository`|Async|IRepository, IAsyncRead|ReadAsync|
|2|`IAsyncReadOnlyRepository`|Async|IAsyncLookup, IAsyncList|ReadAsync, ListAsync|
|3|`IAsyncWriteOnlyRepository`|Async|IRepository, IAsyncCreate/Update/Delete|CreateAsync, UpdateAsync, DeleteAsync|
|4|`IAsyncReadWriteRepository`|Async|IAsyncLookup, IAsyncWriteOnly|ReadAsync, CreateAsync, UpdateAsync, DeleteAsync|
|5|`IAsyncCrudRepository`|Async|IAsyncReadOnly, IAsyncReadWrite|All 5 async|
|6|`ISyncLookupRepository`|Sync|IRepository, ISyncRead|Read|
|7|`ISyncReadOnlyRepository`|Sync|ISyncLookup, ISyncList|Read, List|
|8|`ISyncWriteOnlyRepository`|Sync|IRepository, ISyncCreate/Update/Delete|Create, Update, Delete|
|9|`ISyncReadWriteRepository`|Sync|ISyncLookup, ISyncWriteOnly|Read, Create, Update, Delete|
|10|`ISyncCrudRepository`|Sync|ISyncReadOnly, ISyncReadWrite|All 5 sync|
|11|`ILookupRepository`|Both|IAsyncLookup, ISyncLookup|Read, ReadAsync|
|12|`IReadOnlyRepository`|Both|IAsyncReadOnly, ISyncReadOnly, ILookup|Read + List (both)|
|13|`IWriteOnlyRepository`|Both|IAsyncWriteOnly, ISyncWriteOnly|CUD (both)|
|14|`IReadWriteRepository`|Both|IAsyncReadWrite, ISyncReadWrite, ILookup, IWriteOnly|CRUD no List (both)|
|15|`ICrudRepository`|Both|IAsyncCrud, ISyncCrud, IReadOnly, IReadWrite|All 10|

**Repository composite count: 15**

---

## Service Composite Interfaces

Service composites follow the same hierarchy as repository composites. They share the same operation interfaces.

### Async Service Composites

```csharp
namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

public interface IAsyncLookupService<TEntity, TId>
    : IAsyncReadOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IAsyncReadOnlyService<TEntity, TId>
    : IAsyncLookupService<TEntity, TId>,
      IAsyncListOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IAsyncWriteOnlyService<TEntity, TId>
    : IAsyncCreateOperation<TEntity, TId>,
      IAsyncUpdateOperation<TEntity, TId>,
      IAsyncDeleteOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IAsyncReadWriteService<TEntity, TId>
    : IAsyncLookupService<TEntity, TId>,
      IAsyncWriteOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}

public interface IAsyncCrudService<TEntity, TId>
    : IAsyncReadOnlyService<TEntity, TId>,
      IAsyncReadWriteService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Sync and Combined Service Composites

Follow the same pattern as repository composites. Sync composites use `ISync*` operations. Combined composites inherit from both async and sync, plus lower-level combined composites for full type compatibility.

### Complete Service Composite Catalog

|#|Interface|Mode|Methods|
|---|---|---|---|
|1|`IAsyncLookupService`|Async|ReadAsync|
|2|`IAsyncReadOnlyService`|Async|ReadAsync, ListAsync|
|3|`IAsyncWriteOnlyService`|Async|CreateAsync, UpdateAsync, DeleteAsync|
|4|`IAsyncReadWriteService`|Async|ReadAsync, CreateAsync, UpdateAsync, DeleteAsync|
|5|`IAsyncCrudService`|Async|All 5 async|
|6|`ISyncLookupService`|Sync|Read|
|7|`ISyncReadOnlyService`|Sync|Read, List|
|8|`ISyncWriteOnlyService`|Sync|Create, Update, Delete|
|9|`ISyncReadWriteService`|Sync|Read, Create, Update, Delete|
|10|`ISyncCrudService`|Sync|All 5 sync|
|11|`ILookupService`|Both|Read, ReadAsync|
|12|`IReadOnlyService`|Both|Read + List (both)|
|13|`IWriteOnlyService`|Both|CUD (both)|
|14|`IReadWriteService`|Both|CRUD no List (both)|
|15|`ICrudService`|Both|All 10|

**Service composite count: 15**

---

## Repository Base Classes (Abstract — Compiler Enforced)

Every repository base class is **abstract**. The consuming class MUST implement every method. Visual Studio's "Implement abstract class" generates all required stubs.

### Async Repository Base Classes

#### BaseAsyncCrudRepository — 5 abstract methods

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async CRUD repositories. All five async operations are abstract.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// The consuming class must implement all five methods. Use Visual Studio's
/// "Implement abstract class" to generate stubs. Inherits logging convenience
/// methods from <see cref="BaseClassWithLogging"/>.
/// </remarks>
public abstract class BaseAsyncCrudRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncCrudRepository()
        : base()
    {
    }

    protected BaseAsyncCrudRepository(ILogger logger)
        : base(logger)
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

#### BaseAsyncReadOnlyRepository — 2 abstract methods

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async read-only repositories. Two abstract methods: Read and List.
/// </summary>
public abstract class BaseAsyncReadOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncReadOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncReadOnlyRepository()
        : base()
    {
    }

    protected BaseAsyncReadOnlyRepository(ILogger logger)
        : base(logger)
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

#### BaseAsyncLookupRepository — 1 abstract method

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async lookup repositories. One abstract method: Read.
/// </summary>
public abstract class BaseAsyncLookupRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncLookupRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncLookupRepository()
        : base()
    {
    }

    protected BaseAsyncLookupRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
```

#### BaseAsyncWriteOnlyRepository — 3 abstract methods

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async write-only repositories.
/// Three abstract methods: Create, Update, Delete.
/// </summary>
public abstract class BaseAsyncWriteOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncWriteOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncWriteOnlyRepository()
        : base()
    {
    }

    protected BaseAsyncWriteOnlyRepository(ILogger logger)
        : base(logger)
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

#### BaseAsyncReadWriteRepository — 4 abstract methods

```csharp
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async read-write repositories.
/// Four abstract methods: Create, Read, Update, Delete.
/// </summary>
public abstract class BaseAsyncReadWriteRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncReadWriteRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncReadWriteRepository()
        : base()
    {
    }

    protected BaseAsyncReadWriteRepository(ILogger logger)
        : base(logger)
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

    #endregion Public Methods
}
```

### Sync Repository Base Classes

Follow the same pattern with sync method signatures. Each base class has the same number of abstract methods as its async counterpart.

```csharp
namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync CRUD repositories. All five sync operations are abstract.
/// </summary>
public abstract class BaseSyncCrudRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseSyncCrudRepository()
        : base()
    {
    }

    protected BaseSyncCrudRepository(ILogger logger)
        : base(logger)
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

The other four sync bases (`BaseSyncReadOnlyRepository`, `BaseSyncLookupRepository`, `BaseSyncWriteOnlyRepository`, `BaseSyncReadWriteRepository`) follow the same pattern with 2, 1, 3, and 4 abstract methods respectively.

### Combined Repository Base Classes

Combined bases implement both sync and async interfaces. **Async methods are abstract** (compiler enforced). **Sync methods are virtual** with sync-over-async bridge defaults that can be overridden for optimized sync implementations.

```csharp
namespace Roadbed.Crud.Repositories;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for combined CRUD repositories. Five abstract async methods plus
/// five virtual sync methods with sync-over-async bridge defaults.
/// </summary>
/// <remarks>
/// <para>
/// The consuming class MUST implement all five async methods. The sync methods
/// bridge to async by default using <c>GetAwaiter().GetResult()</c>. Override
/// the sync methods when you have a sync-native data source and want to avoid
/// the async bridge overhead.
/// </para>
/// <para>
/// <b>Deadlock warning:</b> The default sync-over-async bridge can deadlock in
/// environments with a synchronization context (ASP.NET Classic, WPF, WinForms).
/// If your consuming code runs in such an environment, override the sync methods
/// with native sync implementations or use the async-only base class instead.
/// </para>
/// </remarks>
public abstract class BaseCrudRepository<TEntity, TId>
    : BaseClassWithLogging,
      ICrudRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseCrudRepository()
        : base()
    {
    }

    protected BaseCrudRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods — Async (Abstract)

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

    #endregion Public Methods — Async (Abstract)

    #region Public Methods — Sync (Virtual Bridge)

    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation bridges to <see cref="CreateAsync"/>. Override for
    /// native sync implementation.
    /// </remarks>
    public virtual TEntity Create(TEntity entity) =>
        this.CreateAsync(entity, CancellationToken.None)
            .GetAwaiter().GetResult();

    /// <inheritdoc/>
    public virtual TEntity Read(TId id) =>
        this.ReadAsync(id, CancellationToken.None)
            .GetAwaiter().GetResult();

    /// <inheritdoc/>
    public virtual TEntity Update(TEntity entity) =>
        this.UpdateAsync(entity, CancellationToken.None)
            .GetAwaiter().GetResult();

    /// <inheritdoc/>
    public virtual void Delete(TId id) =>
        this.DeleteAsync(id, CancellationToken.None)
            .GetAwaiter().GetResult();

    /// <inheritdoc/>
    public virtual IList<TEntity> List() =>
        this.ListAsync(CancellationToken.None)
            .GetAwaiter().GetResult();

    #endregion Public Methods — Sync (Virtual Bridge)
}
```

The other four combined bases follow the same pattern:

|Combined Base|Async Abstract|Sync Virtual Bridge|
|---|---|---|
|`BaseCrudRepository`|5|5|
|`BaseReadOnlyRepository`|2 (Read, List)|2|
|`BaseLookupRepository`|1 (Read)|1|
|`BaseWriteOnlyRepository`|3 (Create, Update, Delete)|3|
|`BaseReadWriteRepository`|4 (CRUD no List)|4|

### Complete Repository Base Class Catalog

|#|Class|Mode|Abstract Methods|Virtual Methods|
|---|---|---|---|---|
|1|`BaseAsyncCrudRepository`|Async|5|0|
|2|`BaseAsyncReadOnlyRepository`|Async|2|0|
|3|`BaseAsyncLookupRepository`|Async|1|0|
|4|`BaseAsyncWriteOnlyRepository`|Async|3|0|
|5|`BaseAsyncReadWriteRepository`|Async|4|0|
|6|`BaseSyncCrudRepository`|Sync|5|0|
|7|`BaseSyncReadOnlyRepository`|Sync|2|0|
|8|`BaseSyncLookupRepository`|Sync|1|0|
|9|`BaseSyncWriteOnlyRepository`|Sync|3|0|
|10|`BaseSyncReadWriteRepository`|Sync|4|0|
|11|`BaseCrudRepository`|Both|5 async|5 sync bridge|
|12|`BaseReadOnlyRepository`|Both|2 async|2 sync bridge|
|13|`BaseLookupRepository`|Both|1 async|1 sync bridge|
|14|`BaseWriteOnlyRepository`|Both|3 async|3 sync bridge|
|15|`BaseReadWriteRepository`|Both|4 async|4 sync bridge|

**Repository base class count: 15**

---

## Service Base Classes (Virtual — Pass-Through Defaults)

Service base classes are **not abstract**. They provide virtual pass-through defaults that delegate to the injected repository. The consuming service overrides only the methods where it adds business logic.

### Design Decision: Why Virtual, Not Abstract?

Repository implementations are always custom — every data source is different. Making repository bases abstract forces developers to write the data access code. That is the correct compiler enforcement.

Service implementations often just delegate to the repository. Making service bases abstract would force developers to write `return await this.Repository.CreateAsync(entity, ct);` for every method in every service. That is boilerplate, not enforcement. The virtual pass-through default IS the correct implementation for services with no business logic to add for that specific operation.

The compiler still enforces correctness: the service interface is satisfied by the base class. If the base class signature doesn't match the interface, the compiler catches it.

### Async Service Base Classes

#### BaseAsyncCrudService

```csharp
namespace Roadbed.Crud.Services.Async;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Base for async CRUD services. All five operations delegate to the repository
/// by default. Override individual methods to add business logic.
/// </summary>
public class BaseAsyncCrudService<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly IAsyncCrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseAsyncCrudService(
        IAsyncCrudRepository<TEntity, TId> repository,
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
    protected IAsyncCrudRepository<TEntity, TId> Repository => this._repository;

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

#### BaseAsyncReadOnlyService

```csharp
namespace Roadbed.Crud.Services.Async;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Base for async read-only services. Read and List delegate to the repository.
/// </summary>
public class BaseAsyncReadOnlyService<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncReadOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly IAsyncReadOnlyRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseAsyncReadOnlyService(
        IAsyncReadOnlyRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        this._repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion Protected Constructors

    #region Protected Properties

    protected IAsyncReadOnlyRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual async Task<TEntity> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        return await this._repository.ReadAsync(id, cancellationToken);
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

The other async service bases (`BaseAsyncLookupService`, `BaseAsyncWriteOnlyService`, `BaseAsyncReadWriteService`) follow the same pattern, each accepting the corresponding async repository composite.

### Sync Service Base Classes

Sync service bases accept sync repository composites and delegate with sync methods.

```csharp
namespace Roadbed.Crud.Services.Sync;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Base for sync CRUD services. All five operations delegate to the repository.
/// </summary>
public class BaseSyncCrudService<TEntity, TId>
    : BaseClassWithLogging,
      ISyncCrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ISyncCrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseSyncCrudService(
        ISyncCrudRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        this._repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion Protected Constructors

    #region Protected Properties

    protected ISyncCrudRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual TEntity Create(TEntity entity)
    {
        return this._repository.Create(entity);
    }

    /// <inheritdoc/>
    public virtual TEntity Read(TId id)
    {
        return this._repository.Read(id);
    }

    /// <inheritdoc/>
    public virtual TEntity Update(TEntity entity)
    {
        return this._repository.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void Delete(TId id)
    {
        this._repository.Delete(id);
    }

    /// <inheritdoc/>
    public virtual IList<TEntity> List()
    {
        return this._repository.List();
    }

    #endregion Public Methods
}
```

### Combined Service Base Classes

Combined service bases accept the combined repository composite. Async methods delegate to the repository's async methods. Sync methods delegate to the repository's sync methods. Both are virtual.

```csharp
namespace Roadbed.Crud.Services;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories;

/// <summary>
/// Base for combined CRUD services. All ten methods (5 async + 5 sync) delegate
/// to the combined repository.
/// </summary>
public class BaseCrudService<TEntity, TId>
    : BaseClassWithLogging,
      ICrudService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly ICrudRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseCrudService(
        ICrudRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        this._repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion Protected Constructors

    #region Protected Properties

    protected ICrudRepository<TEntity, TId> Repository => this._repository;

    #endregion Protected Properties

    #region Public Methods — Async

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

    #endregion Public Methods — Async

    #region Public Methods — Sync

    /// <inheritdoc/>
    public virtual TEntity Create(TEntity entity)
    {
        return this._repository.Create(entity);
    }

    /// <inheritdoc/>
    public virtual TEntity Read(TId id)
    {
        return this._repository.Read(id);
    }

    /// <inheritdoc/>
    public virtual TEntity Update(TEntity entity)
    {
        return this._repository.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void Delete(TId id)
    {
        this._repository.Delete(id);
    }

    /// <inheritdoc/>
    public virtual IList<TEntity> List()
    {
        return this._repository.List();
    }

    #endregion Public Methods — Sync
}
```

### Complete Service Base Class Catalog

| #   | Class                       | Mode  | Repository Dependency       | Virtual Methods |
| --- | --------------------------- | ----- | --------------------------- | --------------- |
| 1   | `BaseAsyncCrudService`      | Async | `IAsyncCrudRepository`      | 5               |
| 2   | `BaseAsyncReadOnlyService`  | Async | `IAsyncReadOnlyRepository`  | 2               |
| 3   | `BaseAsyncLookupService`    | Async | `IAsyncLookupRepository`    | 1               |
| 4   | `BaseAsyncWriteOnlyService` | Async | `IAsyncWriteOnlyRepository` | 3               |
| 5   | `BaseAsyncReadWriteService` | Async | `IAsyncReadWriteRepository` | 4               |
| 6   | `BaseSyncCrudService`       | Sync  | `ISyncCrudRepository`       | 5               |
| 7   | `BaseSyncReadOnlyService`   | Sync  | `ISyncReadOnlyRepository`   | 2               |
| 8   | `BaseSyncLookupService`     | Sync  | `ISyncLookupRepository`     | 1               |
| 9   | `BaseSyncWriteOnlyService`  | Sync  | `ISyncWriteOnlyRepository`  | 3               |
| 10  | `BaseSyncReadWriteService`  | Sync  | `ISyncReadWriteRepository`  | 4               |
| 11  | `BaseCrudService`           | Both  | `ICrudRepository`           | 10              |
| 12  | `BaseReadOnlyService`       | Both  | `IReadOnlyRepository`       | 4               |
| 13  | `BaseLookupService`         | Both  | `ILookupRepository`         | 2               |
| 14  | `BaseWriteOnlyService`      | Both  | `IWriteOnlyRepository`      | 6               |
| 15  | `BaseReadWriteService`      | Both  | `IReadWriteRepository`      | 8               |

**Service base class count: 15**

---

## Three Ways to Consume (Choose Your Level)

### Level 1: Composite Interface (Fastest Path)

Pick a pre-built composite that matches your needs. Combines mode + shape.

```csharp
// Async CRUD — most common for REST APIs
public interface IFooRepository : IAsyncCrudRepository<Foo, string> { }

// Sync read-only — for file-backed reference data
public interface IBarRepository : ISyncReadOnlyRepository<Bar, int> { }

// Combined CRUD — for libraries serving multiple consumers
public interface IBazRepository : ICrudRepository<Baz, Guid> { }
```

### Level 2: Cherry-Pick Operations (Precise Control)

Compose from individual operation interfaces when no composite matches.

```csharp
// Async append-only: Create + Read + List (no composite matches this)
public interface IAuditRepository
    : IRepository<AuditEntry, Guid>,
      IAsyncCreateOperation<AuditEntry, Guid>,
      IAsyncReadOperation<AuditEntry, Guid>,
      IAsyncListOperation<AuditEntry, Guid>
{
}

// Mixed mode: Async Read + Sync Read (both) + Async List only
public interface ICacheRepository
    : IRepository<CacheItem, string>,
      IReadOperation<CacheItem, string>,
      IAsyncListOperation<CacheItem, string>
{
}
```

**Note**: When cherry-picking, include `IRepository<TEntity, TId>` manually for the marker interface. Composites include it automatically.

### Level 3: Composite + Custom Methods (Extend Standard)

Start with a composite and add domain-specific methods.

```csharp
// Async CRUD plus custom queries
public interface IOrderRepository : IAsyncCrudRepository<Order, long>
{
    Task<IList<Order>> ListByCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<IList<Order>> ListByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
```

---

## Consuming Project Examples

### Example 1: Async CRUD with Service (Roadbed.NET Pattern)

```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public sealed record Foo : BaseEntityRecord<string>
{
    required public string Name { get; init; }
    public string? Description { get; init; }
}
```

```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services.Async;

public interface IFooService : IAsyncCrudService<Foo, string>
{
}
```

```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories.Async;

internal interface IFooRepository : IAsyncCrudRepository<Foo, string>
{
}
```

```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Async CRUD repository for Foo. Inherits from BaseAsyncCrudRepository which
/// has 5 abstract methods — the compiler forces implementation of all five.
/// </summary>
internal sealed class FooRepository
    : BaseAsyncCrudRepository<Foo, string>,
      IFooRepository
{
    internal FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    {
    }

    // All 5 methods are REQUIRED by the compiler. "Implement abstract class"
    // in Visual Studio generates these stubs automatically.

    public override async Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Foo: {Name}", entity.Name);
        // SQL INSERT implementation
        throw new NotImplementedException();
    }

    public override async Task<Foo> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Foo: {Id}", id);
        // SQL SELECT implementation
        throw new NotImplementedException();
    }

    public override async Task<Foo> UpdateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Foo: {Id}", entity.Id);
        // SQL UPDATE implementation
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Deleting Foo: {Id}", id);
        // SQL DELETE implementation
        throw new NotImplementedException();
    }

    public override async Task<IList<Foo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Foo entities");
        // SQL SELECT ALL implementation
        throw new NotImplementedException();
    }
}
```

```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Async CRUD service for Foo. Overrides CreateAsync to add validation.
/// All other methods pass through to the repository via BaseAsyncCrudService defaults.
/// </summary>
internal sealed class FooService
    : BaseAsyncCrudService<Foo, string>,
      IFooService
{
    internal FooService(
        IAsyncCrudRepository<Foo, string> repository,
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

### Example 2: Sync Read-Only, No Service (Roadbed.IO Pattern)

```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public sealed class Bar : BaseEntityClass<int>
{
    public string? Code { get; set; }
    public string? Description { get; set; }
}
```

```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Bar is reference data loaded from a CSV file. Sync read-only.
/// </summary>
public interface IBarRepository : ISyncReadOnlyRepository<Bar, int>
{
}
```

```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Sync read-only repository for Bar. BaseSyncReadOnlyRepository has exactly
/// 2 abstract methods: Read and List. No dead code.
/// </summary>
internal sealed class BarRepository
    : BaseSyncReadOnlyRepository<Bar, int>,
      IBarRepository
{
    internal BarRepository(ILogger<BarRepository> logger)
        : base(logger)
    {
    }

    public override Bar Read(int id)
    {
        this.LogDebug("Reading Bar: {Id}", id);
        // File-based read implementation
        throw new NotImplementedException();
    }

    public override IList<Bar> List()
    {
        this.LogDebug("Listing all Bar entities");
        // CSV file read implementation
        throw new NotImplementedException();
    }
}
```

### Example 3: Combined CRUD with Custom Methods + Service

```csharp
namespace MyApp.Data;

using Roadbed.Crud;

public sealed record Qux : BaseEntityRecord<long>
{
    required public string Name { get; init; }
    required public string Status { get; init; }
}
```

```csharp
namespace MyApp.Data;

using Roadbed.Crud.Services.Async;

public interface IQuxService : IAsyncCrudService<Qux, long>
{
    Task DeactivateAsync(long id, CancellationToken cancellationToken = default);
}
```

```csharp
namespace MyApp.Data;

using Roadbed.Crud.Repositories.Async;

internal interface IQuxRepository : IAsyncCrudRepository<Qux, long>
{
    Task UpdateStatusAsync(
        long id,
        string status,
        CancellationToken cancellationToken = default);
}
```

```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

internal sealed class QuxRepository
    : BaseAsyncCrudRepository<Qux, long>,
      IQuxRepository
{
    internal QuxRepository(ILogger<QuxRepository> logger)
        : base(logger)
    {
    }

    public override async Task<Qux> CreateAsync(
        Qux entity, CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating Qux: {Name}", entity.Name);
        throw new NotImplementedException();
    }

    public override async Task<Qux> ReadAsync(
        long id, CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading Qux: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<Qux> UpdateAsync(
        Qux entity, CancellationToken cancellationToken = default)
    {
        this.LogDebug("Updating Qux: {Id}", entity.Id);
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(
        long id, CancellationToken cancellationToken = default)
    {
        this.LogDebug("Deleting Qux: {Id}", id);
        throw new NotImplementedException();
    }

    public override async Task<IList<Qux>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing all Qux entities");
        throw new NotImplementedException();
    }

    /// <summary>
    /// Custom operation — not an override, directly implements interface.
    /// </summary>
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
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

internal sealed class QuxService
    : BaseAsyncCrudService<Qux, long>,
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

### Example 4: Cherry-Picked Async Operations

```csharp
namespace MyApp.Data;

using Roadbed.Crud;
using Roadbed.Crud.Operations.Async;

/// <summary>
/// Audit log: append-only. Create + Read + List. No Update, no Delete.
/// No pre-built composite matches this combination.
/// </summary>
public interface IAuditRepository
    : IRepository<AuditEntry, Guid>,
      IAsyncCreateOperation<AuditEntry, Guid>,
      IAsyncReadOperation<AuditEntry, Guid>,
      IAsyncListOperation<AuditEntry, Guid>
{
}
```

```csharp
namespace MyApp.Data.Internal;

using Microsoft.Extensions.Logging;

/// <summary>
/// Cannot use any pre-built base class (no base matches Create + Read + List).
/// Inherits BaseClassWithLogging directly and implements the interface manually.
/// </summary>
internal sealed class AuditRepository
    : BaseClassWithLogging,
      IAuditRepository
{
    internal AuditRepository(ILogger<AuditRepository> logger)
        : base(logger)
    {
    }

    public async Task<AuditEntry> CreateAsync(
        AuditEntry entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating audit entry");
        throw new NotImplementedException();
    }

    public async Task<AuditEntry> ReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Reading audit entry: {Id}", id);
        throw new NotImplementedException();
    }

    public async Task<IList<AuditEntry>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing audit entries");
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
        // Example 1: Async CRUD with service
        services.AddSingleton<IAsyncCrudRepository<Foo, string>, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Example 2: Sync read-only, no service
        services.AddSingleton<IBarRepository, BarRepository>();

        // Example 3: Custom methods + service
        services.AddSingleton<IQuxRepository, QuxRepository>();
        // Forward custom repository as base type for service constructor
        services.AddSingleton<IAsyncCrudRepository<Qux, long>>(sp =>
            sp.GetRequiredService<IQuxRepository>());
        services.AddSingleton<IQuxService, QuxService>();

        // Example 4: Cherry-picked operations
        services.AddSingleton<IAuditRepository, AuditRepository>();

        // Type hierarchy enables this: register CRUD, resolve as ReadOnly
        services.AddSingleton<IAsyncReadOnlyRepository<Foo, string>>(sp =>
            sp.GetRequiredService<IAsyncCrudRepository<Foo, string>>());
    }
}
```

---

## Full Type Count

### By Category

|Category|Count|
|---|---|
|Entity types (`IEntity`, `BaseEntityRecord`, `BaseEntityClass`)|3|
|Marker interfaces (`IRepository`, `IRepository<T, TId>`)|2|
|Async operation interfaces|5|
|Sync operation interfaces|5|
|Combined operation interfaces|5|
|Async repository composites|5|
|Sync repository composites|5|
|Combined repository composites|5|
|Async service composites|5|
|Sync service composites|5|
|Combined service composites|5|
|Async repository base classes|5|
|Sync repository base classes|5|
|Combined repository base classes|5|
|Async service base classes|5|
|Sync service base classes|5|
|Combined service base classes|5|
|**Total in Roadbed.Crud**|**80**|
|Non-generic `BaseClassWithLogging` (Roadbed.Common)|1|
|**Grand total**|**81**|

### By Dimension

|Dimension|Multiplier|
|---|---|
|Execution modes|× 3 (Async, Sync, Both)|
|Composite shapes|× 5 (Crud, ReadOnly, Lookup, WriteOnly, ReadWrite)|
|Layers|× 2 (Repository, Service)|
|Artifact types per combo|× 2 (Interface + Base class)|

3 modes × 5 shapes × 2 layers × 2 artifacts = **60** (composites + bases) Plus 15 operations + 3 entity + 2 markers = **80 total**

### Pragmatic Subsets

Not every project needs all 80 types. Here's what you'd build for common scenarios:

#### Async Only (Most Common — Roadbed.NET)

|Category|Types|
|---|---|
|Entity|3|
|Markers|2|
|Async operations|5|
|Async repository composites|5|
|Async service composites|5|
|Async repository bases|5|
|Async service bases|5|
|**Total**|**30**|

#### Sync Only (Roadbed.IO)

|Category|Types|
|---|---|
|Entity|3|
|Markers|2|
|Sync operations|5|
|Sync repository composites|5|
|Sync service composites|5|
|Sync repository bases|5|
|Sync service bases|5|
|**Total**|**30**|

#### Async + Service Composites Only (No Sync, No Service Layer)

|Category|Types|
|---|---|
|Entity|3|
|Markers|2|
|Async operations|5|
|Async repository composites|5|
|Async repository bases|5|
|**Total**|**20**|

---

## Consuming Interface Decision Tree

```
Step 1: Choose your execution mode
├── Async only (REST APIs, databases) → IAsync* prefix
├── Sync only (file I/O, in-memory) → ISync* prefix
└── Both (libraries, shared components) → No prefix

Step 2: Choose your consumption level
├── Level 1: Use a pre-built composite
│   ├── All five operations → Crud composite
│   ├── Read + List → ReadOnly composite
│   ├── Read only → Lookup composite
│   ├── Create + Update + Delete → WriteOnly composite
│   └── CRUD without List → ReadWrite composite
├── Level 2: Cherry-pick operations (no composite matches)
│   └── Inherit IRepository<T, TId> + individual I*Operation interfaces
└── Level 3: Composite + custom methods
    └── Inherit from a composite + declare custom methods

Step 3: Choose your base class
├── Level 1/3: Use the matching base (e.g., BaseAsyncCrudRepository)
│   └── Compiler forces implementation of exactly the right methods
└── Level 2: Use BaseClassWithLogging directly
    └── Implement interface methods manually

Step 4: Do you need a service layer?
├── Yes → Create service interface + class
│   ├── Service interface inherits from matching service composite
│   ├── Service class inherits from matching service base
│   └── Service base provides pass-through defaults; override for business logic
└── No → Application layer depends on repository interface directly
```

---

## Comparison Across All Options

|Aspect|Opt 1|Opt 2|Opt 5|Opt 7|Opt 8|Opt 9|**Opt 10**|
|---|---|---|---|---|---|---|---|
|Total types|8|17|13|16|4|18|**80**|
|Execution modes|Async|Both|Async|Async|Async|Async|**All 3**|
|Compiler enforcement|❌|❌|❌|❌|❌|❌|**✅**|
|Dead code in base|Yes|Yes|Yes|Yes|No|Yes|**No**|
|Composite hierarchy|Flat|Flat|Flat|Flat|None|Flat|**Hierarchical**|
|Base classes|1|3|1|2|1|2|**30**|
|Entity base types|1|1|1|1|1|1|**3**|
|IntelliSense stubs|Manual|Manual|Manual|Manual|Manual|Manual|**Auto**|
|Service layer|No|No|No|Optional|No|Optional|**Optional**|
|Marker interfaces|No|No|No|No|Yes|Yes|**Yes**|
|Consumption levels|1|1|1|2|1|3|**3**|

---

## Pros

- **Full compiler enforcement** — abstract base classes force implementing exactly the methods each composite requires. Forgetting `ReadAsync` on a ReadOnly repository is a compiler error, not a runtime `NotImplementedException`
- **Zero dead code** — `BaseAsyncReadOnlyRepository` has 2 abstract methods, not 5 virtual methods where 3 throw `NotImplementedException`. Every method in the base class is meaningful
- **IntelliSense generates stubs** — "Implement abstract class" in Visual Studio produces all required method signatures instantly. No guessing, no looking up documentation
- **Three execution modes** — pure async for Roadbed.NET, pure sync for Roadbed.IO, combined for shared libraries. Each mode has dedicated types
- **Hierarchical composites** — `ICrudRepository` IS-A `IReadOnlyRepository`. Register a CRUD repository and resolve it as ReadOnly for services that only need to read. The type system handles substitution correctly
- **Full type compatibility** — a combined repository is assignable to async composites, sync composites, and narrower combined composites. No casting, no forwarding — just interface inheritance
- **Service bases match repository shapes** — `BaseAsyncReadOnlyService` takes `IAsyncReadOnlyRepository`. The service can only call Read and List on its repository, even if the actual repository is CRUD. Type safety at the service layer
- **BaseEntityRecord + BaseEntityClass** — consuming projects choose between record semantics (value equality, `with` expressions) and class semantics (reference identity, mutation) without losing `IEntity<TId>` compatibility
- **Three consumption levels preserved** — composites for speed, cherry-pick for precision, composite + custom for extensions (from Option 9)
- **Marker interfaces preserved** — assembly scanning for DI registration (from Options 8-9)

## Cons

- **80 types** — this is the single largest criticism. 80 types in a CRUDL abstraction library is extraordinary. Even organized into clear namespaces, the sheer volume is daunting for onboarding and maintenance
- **30 base classes** — each composite × mode gets its own base class. These are mostly identical in structure, differing only in which abstract methods they declare. High maintenance cost for low marginal value over the first few
- **Sync-over-async bridge risks deadlocks** — combined base classes use `GetAwaiter().GetResult()` for sync defaults. This can deadlock in environments with a synchronization context (ASP.NET Classic, WPF). Documented but still a footgun
- **Cherry-picked operations lose base class** — Level 2 consumption (cherry-pick) cannot use any pre-built base class because no base matches arbitrary subsets. The consuming class falls back to `BaseClassWithLogging` directly and implements the interface manually — no compiler enforcement beyond the interface itself
- **Mode coupling** — services and repositories must match execution modes. An async service cannot wrap a sync repository. A combined service requires a combined repository. This is by design but limits flexibility
- **DI forwarding for custom repositories** — same as Options 6-7-9. When a service needs a typed repository interface (Example 3), the DI container needs a forwarding registration
- **Three execution modes may be overkill** — most Roadbed projects are either fully async or fully sync. The "both" mode adds 25 types (5 operations + 5 repo composites
    - 5 service composites + 5 repo bases + 5 service bases) that may rarely be used
- **Learning curve** — new developers must understand: modes, shapes, layers, consumption levels, hierarchy, and base class selection. The decision tree helps but the matrix is large
- **Interface hierarchy is complex** — `ICrudRepository` has 10+ interfaces in its inheritance chain. While this is all compile-time with no runtime cost, reading the definition requires tracing multiple levels

## Open Questions

1. **Is 80 types the right ceiling to actually build?** This option is intentionally the full prescriptive picture. A pragmatic implementation might start with the async-only subset (30 types) and add sync/combined later if needed. Should Roadbed.Crud ship all 80 from day one, or grow incrementally?
2. **Should the "both" mode exist at all?** If Roadbed.NET is always async and Roadbed.IO is always sync, the combined mode may never be used. Cutting it saves 25 types (80 → 55).
3. **Should service composites match repository composites exactly?** Both layers have 5 shapes × 3 modes = 15 composites. If services rarely need Lookup or ReadWrite granularity, cutting to 3 shapes per mode saves 12 types.
4. **Can source generators reduce the 30 base classes?** A Roslyn source generator could auto-generate the appropriate abstract base class from the composite interface at compile time, eliminating the need to hand-write 30 classes.
5. **Should the hierarchy go the other direction?** Currently broader composites (CRUD) inherit from narrower ones (ReadOnly). An alternative is narrower composites inherit from broader ones and hide methods — but this violates Liskov and is anti-pattern in .NET.
6. **Is the `BaseClassWithLogging` dependency acceptable?** All 30 base classes inherit from the non-generic `BaseClassWithLogging` (in Roadbed.Common). This couples Roadbed.Crud to Roadbed.Common. Should logging be optional or injected differently?


