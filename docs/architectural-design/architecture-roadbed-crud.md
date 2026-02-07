# Roadbed.Crud Architecture

We [researched multiple options and concepts](research/crudl/index.md). You can review it to learn more about the CRUDAL pattern we selected.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Crud NuGet package. When a developer asks you to create a class library that uses Roadbed.Crud, use this document to scaffold the correct interfaces, base classes, and DI registrations.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation — not `?? throw new ArgumentNullException(...)`.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Repositories are abstract** — every method must be implemented by the consuming class.
5. **Services are virtual** — override only when adding business logic. Exists and Upsert come free.
6. **Repository interfaces and service interfaces should be `internal`** — the application layer depends on the concrete service class, not any internal interface.
7. **Concrete service classes are `public`** with a dual constructor pattern: a `public` constructor (takes only `ILogger<T>`, resolves the repository via `ServiceLocator`) and an `internal` constructor (takes the repository and `ILogger<T>` directly, for unit tests via `InternalsVisibleTo`).
8. **Use Newtonsoft.Json** for serialization, not System.Text.Json.
9. **Use arrays** (not `IList<T>`) for DTO collections from APIs.
10. **CancellationToken is always the last parameter** with `= default`.

See [Implementation Walkthrough](architecture-roadbed-crud.md#implementation-walkthrough) for step-by-step instructions on scaffolding a new module.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-crud.md#for-ai-assistants)
2. [File Directory Structure](architecture-roadbed-crud.md#file-directory-structure)
3. [Type Count Summary](architecture-roadbed-crud.md#type-count-summary)
4. [Composite Hierarchy](architecture-roadbed-crud.md#composite-hierarchy)
5. [Entity Layer](architecture-roadbed-crud.md#entity-layer)
6. [Marker Interface](architecture-roadbed-crud.md#marker-interface)
7. [Async Operation Interfaces](architecture-roadbed-crud.md#async-operation-interfaces)
8. [Sync Operation Interfaces](architecture-roadbed-crud.md#sync-operation-interfaces)
9. [Async Repository Composite Interfaces](architecture-roadbed-crud.md#async-repository-composite-interfaces)
10. [Sync Repository Composite Interfaces](architecture-roadbed-crud.md#sync-repository-composite-interfaces)
11. [Async Repository Base Classes (Abstract)](architecture-roadbed-crud.md#async-repository-base-classes-abstract)
12. [Sync Repository Base Classes (Abstract)](architecture-roadbed-crud.md#sync-repository-base-classes-abstract)
13. [Async Service Composite Interfaces](architecture-roadbed-crud.md#async-service-composite-interfaces)
14. [Sync Service Composite Interfaces](architecture-roadbed-crud.md#sync-service-composite-interfaces)
15. [Async Service Base Classes (Virtual)](architecture-roadbed-crud.md#async-service-base-classes-virtual)
16. [Sync Service Base Classes (Virtual)](architecture-roadbed-crud.md#sync-service-base-classes-virtual)
17. [Consuming Interface Decision Tree](architecture-roadbed-crud.md#consuming-interface-decision-tree)
18. [Implementation Walkthrough](architecture-roadbed-crud.md#implementation-walkthrough)
19. [Consuming Project Examples](architecture-roadbed-crud.md#consuming-project-examples)
20. [Common Pitfalls](architecture-roadbed-crud.md#common-pitfalls)

---

## File Directory Structure
```
Roadbed.Common/
└── src/
    └── BaseClassWithLogging.cs

Roadbed.Crud/
└── src/
    ├── IEntity.cs
    ├── IRepository.cs
    ├── BaseEntityRecord.cs
    ├── BaseEntityClass.cs
    │
    ├── Operations/
    │   ├── Async/
    │   │   ├── IAsyncCreateOperation.cs
    │   │   ├── IAsyncReadOperation.cs
    │   │   ├── IAsyncUpdateOperation.cs
    │   │   ├── IAsyncDeleteOperation.cs
    │   │   ├── IAsyncArchiveOperation.cs
    │   │   ├── IAsyncListOperation.cs
    │   │   ├── IAsyncExistsOperation.cs
    │   │   └── IAsyncUpsertOperation.cs
    │   └── Sync/
    │       ├── ISyncCreateOperation.cs
    │       ├── ISyncReadOperation.cs
    │       ├── ISyncUpdateOperation.cs
    │       ├── ISyncDeleteOperation.cs
    │       ├── ISyncArchiveOperation.cs
    │       ├── ISyncListOperation.cs
    │       ├── ISyncExistsOperation.cs
    │       └── ISyncUpsertOperation.cs
    │
    ├── Repositories/
    │   ├── Async/
    │   │   ├── IAsyncListOnlyRepository.cs
    │   │   ├── IAsyncCrudRepository.cs
    │   │   ├── IAsyncCrudlRepository.cs
    │   │   ├── IAsyncCrudaRepository.cs
    │   │   ├── IAsyncCrudalRepository.cs
    │   │   ├── BaseAsyncListOnlyRepository.cs
    │   │   ├── BaseAsyncCrudRepository.cs
    │   │   ├── BaseAsyncCrudlRepository.cs
    │   │   ├── BaseAsyncCrudaRepository.cs
    │   │   └── BaseAsyncCrudalRepository.cs
    │   └── Sync/
    │       ├── ISyncListOnlyRepository.cs
    │       ├── ISyncCrudRepository.cs
    │       ├── ISyncCrudlRepository.cs
    │       ├── ISyncCrudaRepository.cs
    │       ├── ISyncCrudalRepository.cs
    │       ├── BaseSyncListOnlyRepository.cs
    │       ├── BaseSyncCrudRepository.cs
    │       ├── BaseSyncCrudlRepository.cs
    │       ├── BaseSyncCrudaRepository.cs
    │       └── BaseSyncCrudalRepository.cs
    │
    └── Services/
        ├── Async/
        │   ├── IAsyncListOnlyService.cs
        │   ├── IAsyncCrudService.cs
        │   ├── IAsyncCrudlService.cs
        │   ├── IAsyncCrudaService.cs
        │   ├── IAsyncCrudalService.cs
        │   ├── BaseAsyncListOnlyService.cs
        │   ├── BaseAsyncCrudService.cs
        │   ├── BaseAsyncCrudlService.cs
        │   ├── BaseAsyncCrudaService.cs
        │   └── BaseAsyncCrudalService.cs
        └── Sync/
            ├── ISyncListOnlyService.cs
            ├── ISyncCrudService.cs
            ├── ISyncCrudlService.cs
            ├── ISyncCrudaService.cs
            ├── ISyncCrudalService.cs
            ├── BaseSyncListOnlyService.cs
            ├── BaseSyncCrudService.cs
            ├── BaseSyncCrudlService.cs
            ├── BaseSyncCrudaService.cs
            └── BaseSyncCrudalService.cs
```

---

## Type Count Summary

| Category                              | Async  | Sync   | Shared | Total  |
| ------------------------------------- | ------ | ------ | ------ | ------ |
| Entity types                          | —      | —      | 3      | 3      |
| Marker interface                      | —      | —      | 1      | 1      |
| Operation interfaces                  | 8      | 8      | —      | 16     |
| Repository composites                 | 5      | 5      | —      | 10     |
| Repository bases (abstract)           | 5      | 5      | —      | 10     |
| Service composites                    | 5      | 5      | —      | 10     |
| Service bases (virtual)               | 5      | 5      | —      | 10     |
| **Total in Roadbed.Crud**             | **28** | **28** | **4**  | **60** |
| BaseClassWithLogging (Roadbed.Common) | —      | —      | 1      | 1      |
| **Grand total**                       |        |        |        | **61** |

### Abstract Method Count per Repository Base

| Base Class                         | Abstract Methods     |
| ---------------------------------- | -------------------- |
| `BaseAsync/SyncListOnlyRepository` | 1 (List)             |
| `BaseAsync/SyncCrudRepository`     | 4 (C, R, U, D)       |
| `BaseAsync/SyncCrudlRepository`    | 5 (C, R, U, D, L)    |
| `BaseAsync/SyncCrudaRepository`    | 5 (C, R, U, D, A)    |
| `BaseAsync/SyncCrudalRepository`   | 6 (C, R, U, D, A, L) |

### Virtual Method Count per Service Base

| Base Class                      | Pass-Through         | Composed           | Total |
| ------------------------------- | -------------------- | ------------------ | ----- |
| `BaseAsync/SyncListOnlyService` | 1 (L)                | 0                  | 1     |
| `BaseAsync/SyncCrudService`     | 4 (C, R, U, D)       | 2 (Exists, Upsert) | 6     |
| `BaseAsync/SyncCrudlService`    | 5 (C, R, U, D, L)    | 2 (Exists, Upsert) | 7     |
| `BaseAsync/SyncCrudaService`    | 5 (C, R, U, D, A)    | 2 (Exists, Upsert) | 7     |
| `BaseAsync/SyncCrudalService`   | 6 (C, R, U, D, A, L) | 2 (Exists, Upsert) | 8     |

### Method Signatures Quick Reference

Understanding the return types is critical for correct implementation.

| Operation | Async Signature                                                | Sync Signature                   |
| --------- | -------------------------------------------------------------- | -------------------------------- |
| Create    | `Task<TEntity> CreateAsync(TEntity entity, CancellationToken)` | `TEntity Create(TEntity entity)` |
| Read      | `Task<TEntity?> ReadAsync(TId id, CancellationToken)`          | `TEntity? Read(TId id)`          |
| Update    | `Task<TEntity> UpdateAsync(TEntity entity, CancellationToken)` | `TEntity Update(TEntity entity)` |
| Delete    | `Task DeleteAsync(TId id, CancellationToken)`                  | `void Delete(TId id)`            |
| Archive   | `Task<TEntity> ArchiveAsync(TId id, CancellationToken)`        | `TEntity Archive(TId id)`        |
| List      | `Task<IList<TEntity>> ListAsync(CancellationToken)`            | `IList<TEntity> List()`          |
| Exists    | `Task<bool> ExistsAsync(TId id, CancellationToken)`            | `bool Exists(TId id)`            |
| Upsert    | `Task<TEntity> UpsertAsync(TEntity entity, CancellationToken)` | `TEntity Upsert(TEntity entity)` |

**Key points:**

- **Create** returns the created entity (with any server-assigned values like auto-increment ID).
- **Read** returns `null` when not found — never throws for missing entities.
- **Update** returns the updated entity.
- **Delete** returns `void` (`Task` for async) — throw on failure, not return `bool`.
- **Archive** returns the archived entity with updated archival state.
- **Exists** and **Upsert** are service-level only — not on repository interfaces.

---

## Composite Hierarchy

### Repository Hierarchy (Async shown; Sync is identical)
```
IAsyncListOnlyRepository<T, TId>
│   IRepository<T, TId> + IAsyncListOperation
│
IAsyncCrudRepository<T, TId>
│   IRepository<T, TId> + IAsyncCreate/Read/Update/DeleteOperation
│
├── IAsyncCrudlRepository<T, TId>
│       inherits: IAsyncCrudRepository + IAsyncListOnlyRepository
│
├── IAsyncCrudaRepository<T, TId>
│       inherits: IAsyncCrudRepository + IAsyncArchiveOperation
│
└── IAsyncCrudalRepository<T, TId>
        inherits: IAsyncCrudaRepository + IAsyncCrudlRepository
```

### Service Hierarchy (Async shown; Sync is identical)
```
IAsyncListOnlyService<T, TId>
│   IAsyncListOperation
│
IAsyncCrudService<T, TId>
│   IAsyncCreate/Read/Update/DeleteOperation
│   + IAsyncExistsOperation + IAsyncUpsertOperation
│
├── IAsyncCrudlService<T, TId>
│       inherits: IAsyncCrudService + IAsyncListOnlyService
│
├── IAsyncCrudaService<T, TId>
│       inherits: IAsyncCrudService + IAsyncArchiveOperation
│
└── IAsyncCrudalService<T, TId>
        inherits: IAsyncCrudaService + IAsyncCrudlService
```

### Key Difference: Repository vs Service

| Aspect               | Repository                           | Service                                                                              |
| -------------------- | ------------------------------------ | ------------------------------------------------------------------------------------ |
| Operations           | C, R, U, D, A, L (data primitives)   | C, R, U, D, A, L + **Exists** + **Upsert**                                           |
| Base class methods   | `abstract` (must implement)          | `virtual` (override to add logic)                                                    |
| Marker interface     | Inherits `IRepository<TEntity, TId>` | Does not                                                                             |
| Interface visibility | `internal`                           | `internal`                                                                           |
| Class visibility     | `internal sealed`                    | `public sealed`                                                                      |
| Constructor          | `ILogger` (optional)                 | Dual: public (`ILogger` + `ServiceLocator`) and internal (`IRepository` + `ILogger`) |

### Type Compatibility (Can assign [column] to [row]?)

| ↓ Accepts / From → | ListOnly | Crud | Crudl | Cruda | Crudal |
| ------------------ | :------: | :--: | :---: | :---: | :----: |
| **ListOnly**       |    ✅     |  ❌   |   ✅   |   ❌   |   ✅    |
| **Crud**           |    ❌     |  ✅   |   ✅   |   ✅   |   ✅    |
| **Crudl**          |    ❌     |  ❌   |   ✅   |   ❌   |   ✅    |
| **Cruda**          |    ❌     |  ❌   |   ❌   |   ✅   |   ✅    |
| **Crudal**         |    ❌     |  ❌   |   ❌   |   ❌   |   ✅    |

---

## Entity Layer

### IEntity.cs
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

### BaseEntityRecord.cs
```csharp
namespace Roadbed.Crud;

/// <summary>
/// Base entity implementation as a record type.
/// </summary>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Records provide value-based equality, immutability, and <c>with</c> expressions.
/// Use for DTOs, API responses, configuration objects, and entities where structural
/// equality is meaningful.
/// </remarks>
public abstract record BaseEntityRecord<TId> : IEntity<TId>
{
    /// <inheritdoc/>
    public virtual TId? Id { get; set; }
}
```

### BaseEntityClass.cs
```csharp
namespace Roadbed.Crud;

/// <summary>
/// Base entity implementation as a class type.
/// </summary>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Classes provide reference-based identity and mutable state. Use for domain
/// entities with behavior, ORM-managed entities, or entities that require complex
/// inheritance hierarchies.
/// </remarks>
public abstract class BaseEntityClass<TId> : IEntity<TId>
{
    /// <inheritdoc/>
    public virtual TId? Id { get; set; }
}
```

### When to Use Record vs Class

| Scenario                    | Use                     | Reason                                   |
| --------------------------- | ----------------------- | ---------------------------------------- |
| DTO from API                | `BaseEntityRecord<TId>` | Immutable, value equality, serialization |
| Configuration object        | `BaseEntityRecord<TId>` | Immutable after creation                 |
| Domain entity with behavior | `BaseEntityClass<TId>`  | Mutable, reference identity              |
| ORM-managed entity          | `BaseEntityClass<TId>`  | EF Core/Dapper compatibility             |
| Entity with inheritance     | `BaseEntityClass<TId>`  | Class inheritance hierarchies            |

---

## Marker Interface

### IRepository.cs

Only the generic marker exists. There is no non-generic `IRepository` interface.
```csharp
namespace Roadbed.Crud;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Generic marker interface identifying a repository for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Type parameters required for consistency with operation interfaces.")]
public interface IRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

Repository composite interfaces inherit from this marker. Service interfaces do not.

---

## Async Operation Interfaces

### IAsyncCreateOperation.cs
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
    Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
```

### IAsyncReadOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Read operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Implementations should return <c>null</c> when the entity is not found rather
/// than throwing an exception. This convention enables the service-level
/// <see cref="IAsyncExistsOperation{TEntity, TId}"/> to compose from Read
/// without exception-driven control flow.
/// </remarks>
public interface IAsyncReadOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Reads an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The entity matching the identifier, or <c>null</c> if not found.</returns>
    Task<TEntity?> ReadAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
```

### IAsyncUpdateOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Update operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncUpdateOperation<TEntity, in TId>
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
```

### IAsyncDeleteOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Delete operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Delete is a hard removal — the entity is physically removed from the data source.
/// For soft removal, see <see cref="IAsyncArchiveOperation{TEntity, TId}"/>.
/// Implementations should throw on failure rather than returning a boolean.
/// </remarks>
public interface IAsyncDeleteOperation<TEntity, in TId>
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
```

### IAsyncArchiveOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Archive operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Archive is a soft delete — the entity is marked as inactive or archived in the
/// data source rather than being physically removed. The repository implementation
/// determines the archival mechanism (status column, archived_at timestamp, etc.).
/// For hard removal, see <see cref="IAsyncDeleteOperation{TEntity, TId}"/>.
/// </remarks>
public interface IAsyncArchiveOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Archives an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">Identifier of the entity to archive.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The archived entity with its updated archival state.</returns>
    Task<TEntity> ArchiveAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
```

### IAsyncListOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous List operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncListOperation<TEntity, in TId>
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

### IAsyncExistsOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Exists operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Exists is a service-level operation. It is not implemented at the repository
/// level. The default service implementation composes from
/// <see cref="IAsyncReadOperation{TEntity, TId}.ReadAsync"/> and returns
/// <c>true</c> when the entity is not null.
/// </para>
/// <para>
/// This operation is included in service composites that contain CRUD operations
/// (CrudService, CrudlService, CrudaService, CrudalService).
/// </para>
/// </remarks>
public interface IAsyncExistsOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">Identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
```

### IAsyncUpsertOperation.cs
```csharp
namespace Roadbed.Crud.Operations.Async;

/// <summary>
/// Defines the asynchronous Upsert operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// <para>
/// Upsert is a service-level operation. It is not implemented at the repository
/// level. The default service implementation composes from
/// <see cref="IAsyncExistsOperation{TEntity, TId}.ExistsAsync"/>,
/// <see cref="IAsyncCreateOperation{TEntity, TId}.CreateAsync"/>, and
/// <see cref="IAsyncUpdateOperation{TEntity, TId}.UpdateAsync"/>.
/// </para>
/// <para>
/// When the entity's Id is null, Create is called. When the entity's Id is not
/// null and Exists returns true, Update is called. When the entity's Id is not
/// null and Exists returns false, Create is called.
/// </para>
/// <para>
/// Override the default implementation when the data source supports native upsert
/// (SQL Server MERGE, PostgreSQL ON CONFLICT, SQLite ON CONFLICT).
/// </para>
/// </remarks>
public interface IAsyncUpsertOperation<TEntity, in TId>
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Creates or updates an entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to create or update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created or updated entity.</returns>
    Task<TEntity> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
```

---

## Sync Operation Interfaces

Sync operation interfaces mirror the async versions without `Task`, `CancellationToken`, or the `Async` suffix. Delete returns `void` instead of `Task`.

| Interface                             | Method                           |
| ------------------------------------- | -------------------------------- |
| `ISyncCreateOperation<TEntity, TId>`  | `TEntity Create(TEntity entity)` |
| `ISyncReadOperation<TEntity, TId>`    | `TEntity? Read(TId id)`          |
| `ISyncUpdateOperation<TEntity, TId>`  | `TEntity Update(TEntity entity)` |
| `ISyncDeleteOperation<TEntity, TId>`  | `void Delete(TId id)`            |
| `ISyncArchiveOperation<TEntity, TId>` | `TEntity Archive(TId id)`        |
| `ISyncListOperation<TEntity, TId>`    | `IList<TEntity> List()`          |
| `ISyncExistsOperation<TEntity, TId>`  | `bool Exists(TId id)`            |
| `ISyncUpsertOperation<TEntity, TId>`  | `TEntity Upsert(TEntity entity)` |

All are in the `Roadbed.Crud.Operations.Sync` namespace. Same XML documentation conventions apply (return null from Read, throw from Delete, Exists/Upsert are service-level only).

---

## Async Repository Composite Interfaces

All are in the `Roadbed.Crud.Repositories.Async` namespace and inherit from `IRepository<TEntity, TId>`.

| Interface                  | Inherits                                            | Operations       |
| -------------------------- | --------------------------------------------------- | ---------------- |
| `IAsyncListOnlyRepository` | `IRepository` + `IAsyncListOperation`               | L                |
| `IAsyncCrudRepository`     | `IRepository` + C/R/U/D operations                  | C, R, U, D       |
| `IAsyncCrudlRepository`    | `IAsyncCrudRepository` + `IAsyncListOnlyRepository` | C, R, U, D, L    |
| `IAsyncCrudaRepository`    | `IAsyncCrudRepository` + `IAsyncArchiveOperation`   | C, R, U, D, A    |
| `IAsyncCrudalRepository`   | `IAsyncCrudaRepository` + `IAsyncCrudlRepository`   | C, R, U, D, A, L |
```csharp
// Example: IAsyncCrudlRepository.cs
namespace Roadbed.Crud.Repositories.Async;

/// <summary>
/// Async CRUDL repository. Create, Read, Update, Delete, List.
/// </summary>
public interface IAsyncCrudlRepository<TEntity, TId>
    : IAsyncCrudRepository<TEntity, TId>,
      IAsyncListOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

---

## Sync Repository Composite Interfaces

Identical structure in `Roadbed.Crud.Repositories.Sync`. All inherit from `IRepository<TEntity, TId>` and compose sync operation interfaces.

| Interface                 | Inherits                                          | Operations       |
| ------------------------- | ------------------------------------------------- | ---------------- |
| `ISyncListOnlyRepository` | `IRepository` + `ISyncListOperation`              | L                |
| `ISyncCrudRepository`     | `IRepository` + C/R/U/D operations                | C, R, U, D       |
| `ISyncCrudlRepository`    | `ISyncCrudRepository` + `ISyncListOnlyRepository` | C, R, U, D, L    |
| `ISyncCrudaRepository`    | `ISyncCrudRepository` + `ISyncArchiveOperation`   | C, R, U, D, A    |
| `ISyncCrudalRepository`   | `ISyncCrudaRepository` + `ISyncCrudlRepository`   | C, R, U, D, A, L |

---

## Async Repository Base Classes (Abstract)

All repository base classes inherit from `BaseClassWithLogging` (in `Roadbed.Common`) and offer two constructors: a parameterless constructor and one accepting an `ILogger`. Every method is `abstract` — the compiler enforces that the consuming class implements all data access logic.

| Base Class                                  | Abstract Methods                                         |
| ------------------------------------------- | -------------------------------------------------------- |
| `BaseAsyncListOnlyRepository<TEntity, TId>` | `ListAsync`                                              |
| `BaseAsyncCrudRepository<TEntity, TId>`     | `CreateAsync`, `ReadAsync`, `UpdateAsync`, `DeleteAsync` |
| `BaseAsyncCrudlRepository<TEntity, TId>`    | C, R, U, D + `ListAsync`                                 |
| `BaseAsyncCrudaRepository<TEntity, TId>`    | C, R, U, D + `ArchiveAsync`                              |
| `BaseAsyncCrudalRepository<TEntity, TId>`   | C, R, U, D + `ArchiveAsync` + `ListAsync`                |
```csharp
// Example: BaseAsyncCrudlRepository.cs
namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async CRUDL repositories.
/// </summary>
public abstract class BaseAsyncCrudlRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudlRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    protected BaseAsyncCrudlRepository()
        : base()
    {
    }

    protected BaseAsyncCrudlRepository(ILogger logger)
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
    public abstract Task<TEntity?> ReadAsync(
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

---

## Sync Repository Base Classes (Abstract)

Identical structure to async — two constructors, all methods `abstract`. Methods are synchronous (no `Task`, no `CancellationToken`, `Delete` returns `void`).

| Base Class                                 | Abstract Methods                     |
| ------------------------------------------ | ------------------------------------ |
| `BaseSyncListOnlyRepository<TEntity, TId>` | `List`                               |
| `BaseSyncCrudRepository<TEntity, TId>`     | `Create`, `Read`, `Update`, `Delete` |
| `BaseSyncCrudlRepository<TEntity, TId>`    | C, R, U, D + `List`                  |
| `BaseSyncCrudaRepository<TEntity, TId>`    | C, R, U, D + `Archive`               |
| `BaseSyncCrudalRepository<TEntity, TId>`   | C, R, U, D + `Archive` + `List`      |

---

## Async Service Composite Interfaces

All are in `Roadbed.Crud.Services.Async`. Service interfaces do **not** inherit from `IRepository<TEntity, TId>`. They directly inherit operation interfaces and include Exists and Upsert.

| Interface               | Inherits                                       | Operations             |
| ----------------------- | ---------------------------------------------- | ---------------------- |
| `IAsyncListOnlyService` | `IAsyncListOperation`                          | L                      |
| `IAsyncCrudService`     | C/R/U/D + Exists + Upsert operations           | C, R, U, D, E, U       |
| `IAsyncCrudlService`    | `IAsyncCrudService` + `IAsyncListOnlyService`  | C, R, U, D, L, E, U    |
| `IAsyncCrudaService`    | `IAsyncCrudService` + `IAsyncArchiveOperation` | C, R, U, D, A, E, U    |
| `IAsyncCrudalService`   | `IAsyncCrudaService` + `IAsyncCrudlService`    | C, R, U, D, A, L, E, U |
```csharp
// Example: IAsyncCrudlService.cs
namespace Roadbed.Crud.Services.Async;

/// <summary>
/// Async CRUDL service. Create, Read, Update, Delete, List, Exists, Upsert.
/// </summary>
public interface IAsyncCrudlService<TEntity, TId>
    : IAsyncCrudService<TEntity, TId>,
      IAsyncListOnlyService<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

---

## Sync Service Composite Interfaces

Identical structure in `Roadbed.Crud.Services.Sync`.

| Interface              | Inherits                                     | Operations             |
| ---------------------- | -------------------------------------------- | ---------------------- |
| `ISyncListOnlyService` | `ISyncListOperation`                         | L                      |
| `ISyncCrudService`     | C/R/U/D + Exists + Upsert operations         | C, R, U, D, E, U       |
| `ISyncCrudlService`    | `ISyncCrudService` + `ISyncListOnlyService`  | C, R, U, D, L, E, U    |
| `ISyncCrudaService`    | `ISyncCrudService` + `ISyncArchiveOperation` | C, R, U, D, A, E, U    |
| `ISyncCrudalService`   | `ISyncCrudaService` + `ISyncCrudlService`    | C, R, U, D, A, L, E, U |

---

## Async Service Base Classes (Virtual)

All service base classes inherit from `BaseClassWithLogging` and accept two constructor parameters: the matching repository interface and an `ILogger`. The repository is validated with `ArgumentNullException.ThrowIfNull()` and exposed via a `protected` property. All methods are `virtual` — override only when adding business logic.

**Exists** and **Upsert** are composed from repository primitives automatically:

- `ExistsAsync` calls `ReadAsync` and returns `entity is not null`.
- `UpsertAsync` calls `ExistsAsync` to decide between `CreateAsync` and `UpdateAsync`. Override when the data source supports native upsert (SQL MERGE, ON CONFLICT).

| Base Class                 | Repository Type            | Virtual Methods                       |
| -------------------------- | -------------------------- | ------------------------------------- |
| `BaseAsyncListOnlyService` | `IAsyncListOnlyRepository` | L (1)                                 |
| `BaseAsyncCrudService`     | `IAsyncCrudRepository`     | C, R, U, D + Exists, Upsert (6)       |
| `BaseAsyncCrudlService`    | `IAsyncCrudlRepository`    | C, R, U, D, L + Exists, Upsert (7)    |
| `BaseAsyncCrudaService`    | `IAsyncCrudaRepository`    | C, R, U, D, A + Exists, Upsert (7)    |
| `BaseAsyncCrudalService`   | `IAsyncCrudalRepository`   | C, R, U, D, A, L + Exists, Upsert (8) |
```csharp
// Example: BaseAsyncCrudlService.cs
namespace Roadbed.Crud.Services.Async;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Base for async CRUDL services. Five CRUDL operations delegate to the repository.
/// Exists and Upsert are composed from repository primitives.
/// </summary>
public class BaseAsyncCrudlService<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncCrudlService<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Private Fields

    private readonly IAsyncCrudlRepository<TEntity, TId> _repository;

    #endregion Private Fields

    #region Protected Constructors

    protected BaseAsyncCrudlService(
        IAsyncCrudlRepository<TEntity, TId> repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this._repository = repository;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the repository for data access operations.
    /// </summary>
    protected IAsyncCrudlRepository<TEntity, TId> Repository => this._repository;

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
    public virtual async Task<TEntity?> ReadAsync(
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

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await this._repository.ReadAsync(id, cancellationToken);
        return entity is not null;
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id is not null
            && await this.ExistsAsync(entity.Id!, cancellationToken))
        {
            return await this._repository.UpdateAsync(entity, cancellationToken);
        }

        return await this._repository.CreateAsync(entity, cancellationToken);
    }

    #endregion Public Methods
}
```

---

## Sync Service Base Classes (Virtual)

Identical structure to async service bases. The repository parameter is the sync counterpart. Methods are synchronous (no `Task`, no `CancellationToken`, `Delete` returns `void`). Exists and Upsert are composed identically.

| Base Class                | Repository Type           | Virtual Methods                       |
| ------------------------- | ------------------------- | ------------------------------------- |
| `BaseSyncListOnlyService` | `ISyncListOnlyRepository` | L (1)                                 |
| `BaseSyncCrudService`     | `ISyncCrudRepository`     | C, R, U, D + Exists, Upsert (6)       |
| `BaseSyncCrudlService`    | `ISyncCrudlRepository`    | C, R, U, D, L + Exists, Upsert (7)    |
| `BaseSyncCrudaService`    | `ISyncCrudaRepository`    | C, R, U, D, A + Exists, Upsert (7)    |
| `BaseSyncCrudalService`   | `ISyncCrudalRepository`   | C, R, U, D, A, L + Exists, Upsert (8) |

---

## Consuming Interface Decision Tree
```
Step 1: Choose your execution mode
├── Async (REST APIs, databases, HTTP clients) → IAsync* prefix
└── Sync (file I/O, in-memory, CSV) → ISync* prefix

Step 2: Choose your consumption level
├── Level 1: Use a pre-built composite
│   ├── List only (dimension/lookup tables) → ListOnly
│   ├── CRUD, no List (large tables, custom queries) → Crud
│   ├── CRUD + List (small-to-medium tables) → Crudl
│   ├── CRUD + Archive, no List → Cruda
│   └── CRUD + Archive + List (full) → Crudal ("crud-al")
├── Level 2: Cherry-pick operations (no composite matches)
│   └── Inherit IRepository<T, TId> + individual operation interfaces
└── Level 3: Composite + custom methods
    └── Inherit from a composite + declare custom methods

Step 3: Choose your base class
├── Level 1/3: Use the matching base (e.g., BaseAsyncCrudRepository)
│   └── Compiler forces implementation of exactly the right methods
└── Level 2: Use BaseClassWithLogging directly
    └── Implement interface methods manually

Step 4: Do you need a service layer?
├── Yes → Create service interface (internal) + concrete class (public)
│   ├── Service interface inherits from matching service composite
│   ├── Concrete service class inherits from matching service base
│   ├── Concrete service class is public sealed with dual constructors:
│   │   ├── Public constructor: takes ILogger<T>, resolves repository via ServiceLocator
│   │   └── Internal constructor: takes repository + ILogger<T> (for unit tests)
│   ├── Service provides Exists and Upsert automatically (virtual)
│   └── Override individual methods to add business logic
└── No → Application layer depends on repository interface directly
         (repository interface must be public in this case)
```

---

## Implementation Walkthrough

When a developer asks you to scaffold a new module, follow these steps in order. This example creates an async CRUDL module for a `Customer` entity with `string` ID.

### Step 1: Define the Entity

Choose `BaseEntityRecord<TId>` for DTOs/immutable data or `BaseEntityClass<TId>` for mutable domain entities.
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Represents a customer entity.
/// </summary>
public sealed record Customer : BaseEntityRecord<string>
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public override string? Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [JsonProperty("name")]
    required public string Name { get; set; }

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    [JsonProperty("email")]
    required public string Email { get; set; }
}
```

### Step 2: Define the Repository Interface (internal)
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Repository interface for Customer data access.
/// </summary>
internal interface ICustomerRepository
    : IAsyncCrudlRepository<Customer, string>
{
}
```

For Level 3 consumption, add custom methods to the interface:
```csharp
internal interface ICustomerRepository
    : IAsyncCrudlRepository<Customer, string>
{
    Task<IList<Customer>> ListByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);
}
```

### Step 3: Implement the Repository
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Repository implementation for Customer data access.
/// </summary>
internal sealed class CustomerRepository
    : BaseAsyncCrudlRepository<Customer, string>,
      ICustomerRepository
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerRepository"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomerRepository(ILogger<CustomerRepository> logger)
        : base(logger)
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<Customer> CreateAsync(
        Customer entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // TODO: Implement data access logic (Dapper, EF Core, etc.)
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task<Customer?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        // TODO: Return null when not found — never throw for missing entities.
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task<Customer> UpdateAsync(
        Customer entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // TODO: Implement data access logic.
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        // TODO: Throw on failure — do not return bool.
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task<IList<Customer>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement data access logic.
        throw new NotImplementedException();
    }

    #endregion Public Methods
}
```

### Step 4: Define the Service Interface (internal)
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Roadbed.Crud.Services.Async;

/// <summary>
/// Service interface for Customer business operations.
/// </summary>
internal interface ICustomerService
    : IAsyncCrudlService<Customer, string>
{
}
```

### Step 5: Implement the Service

The concrete service class is `public sealed` and uses the dual constructor pattern. The `public` constructor accepts only `ILogger<T>` and resolves the repository via `ServiceLocator` — the consuming application uses this constructor and never sees the internal repository interface. The `internal` constructor accepts both the repository and the logger directly — unit test projects use this constructor via `InternalsVisibleTo` to inject mock repositories.
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for Customer business operations.
/// </summary>
public sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomerService(
        ILogger<CustomerService> logger)
        : base(
            ServiceLocator.GetService<ICustomerRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    /// <param name="repository">Repository for Customer data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal CustomerService(
        ICustomerRepository repository,
        ILogger<CustomerService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors

    // All 7 methods (C, R, U, D, L + Exists + Upsert) work with zero overrides.
    // Override only when adding business logic, e.g.:
    //
    // public override async Task<Customer> CreateAsync(
    //     Customer entity,
    //     CancellationToken cancellationToken = default)
    // {
    //     // Validate business rules before delegating
    //     ArgumentNullException.ThrowIfNull(entity);
    //     ArgumentException.ThrowIfNullOrWhiteSpace(entity.Email);
    //
    //     this.LogInformation("Creating customer: {Name}", entity.Name);
    //     return await base.CreateAsync(entity, cancellationToken);
    // }
}
```

### Step 6: Register in DI

The installer registers the repository against its internal interface so that `ServiceLocator` can resolve it inside the service's public constructor. The concrete service class is `public` and can be resolved by the DI container directly — the consuming application only needs to provide `ILogger<CustomerService>`.
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;

/// <summary>
/// Registers Customer module services in the DI container.
/// </summary>
public sealed class CustomerModuleInstaller : IServiceCollectionInstaller
{
    /// <inheritdoc/>
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICustomerRepository, CustomerRepository>();

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### Architecture Diagram
```
┌─────────────────────────────────────────────────────┐
│ Application Layer (Console, Web, etc.)              │
│                                                     │
│   Depends on: CustomerService (public class)        │
│   Does NOT see: ICustomerService, ICustomerRepository│
└────────────────────────┬────────────────────────────┘
                         │
                         │ CustomerService (public class)
                         │ Public constructor: ILogger<CustomerService>
                         │
┌────────────────────────▼────────────────────────────┐
│ Roadbed.Sdk.CustomerModule (class library)          │
│                                                     │
│   internal interface ICustomerService               │ ← internal
│       : IAsyncCrudlService<Customer, string>        │
│                                                     │
│   public class CustomerService                      │ ← public
│       : BaseAsyncCrudlService<Customer, string>     │
│       Public ctor: ILogger (resolves repo via       │
│           ServiceLocator internally)                │
│       Internal ctor: ICustomerRepository + ILogger  │
│           (for unit tests via InternalsVisibleTo)   │
│       Contains: validation, business rules, caching │
│                                                     │
│   internal interface ICustomerRepository            │ ← internal
│       : IAsyncCrudlRepository<Customer, string>     │
│                                                     │
│   internal class CustomerRepository                 │ ← internal
│       : BaseAsyncCrudlRepository<Customer, string>  │
│       Contains: pure data access                    │
│                                                     │
│   public class CustomerModuleInstaller              │
│       : IServiceCollectionInstaller                 │
│       Registers repository for ServiceLocator       │
└─────────────────────────────────────────────────────┘
```

**Key points**: Both the repository interface and service interface are `internal`. The concrete service class is `public` — it is the only surface the application layer sees. The application provides `ILogger<CustomerService>` and the service resolves its own repository dependency internally.

---

## Consuming Project Examples

### Example 1: Sync ListOnly, No Service

For reference data that only needs listing (state codes, country codes, etc.), skip the service layer entirely. When the service layer is skipped, the repository interface is `public` because the application layer depends on it directly.
```csharp
// Entity
namespace Roadbed.Sdk.ReferenceTables;

using Roadbed.Crud;

/// <summary>
/// Represents a US state code.
/// </summary>
public sealed class StateCode : BaseEntityClass<string>
{
    /// <summary>
    /// Gets or sets the state name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the two-letter abbreviation.
    /// </summary>
    public string? Abbreviation { get; set; }
}

// Repository interface (public — no service layer)
public interface IStateCodeRepository
    : ISyncListOnlyRepository<StateCode, string>
{
}

// Repository implementation
internal sealed class StateCodeRepository
    : BaseSyncListOnlyRepository<StateCode, string>,
      IStateCodeRepository
{
    public StateCodeRepository(ILogger<StateCodeRepository> logger)
        : base(logger)
    {
    }

    public override IList<StateCode> List()
    {
        this.LogDebug("Loading state codes from CSV");
        // Implementation here
        throw new NotImplementedException();
    }
}
```

### Example 2: Async CRUD + Custom Queries (Level 3)

For entities where `ListAll` is impractical but filtered queries are needed.
```csharp
// Repository interface — Crud composite + custom filtered queries
internal interface IOrderRepository
    : IAsyncCrudRepository<Order, long>
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

### DI Registration
```csharp
namespace Roadbed.Sdk.CustomerModule;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;

/// <summary>
/// Registers module services in the DI container.
/// </summary>
public sealed class DataInstaller : IServiceCollectionInstaller
{
    /// <inheritdoc/>
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // CRUDL with service layer — register repository for ServiceLocator
        services.AddSingleton<ICustomerRepository, CustomerRepository>();

        // Sync ListOnly, no service layer — register repository for direct DI resolution
        services.AddSingleton<IStateCodeRepository, StateCodeRepository>();

        // CRUD + custom queries, no service layer — register repository for direct DI resolution
        services.AddSingleton<IOrderRepository, OrderRepository>();

        // Capture point-in-time snapshot for ServiceLocator
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

---

## Common Pitfalls

These are mistakes that have been encountered in practice. AI assistants should avoid these when generating code for Roadbed.Crud consumers.

### 1. Wrong Return Types

The old codebase used `Task<int>` for Create (returning ID) and `Task<bool>` for Update/Delete. The new interfaces return:

- **Create**: `Task<TEntity>` — the full entity
- **Update**: `Task<TEntity>` — the full entity
- **Delete**: `Task` (void) — throw on failure

### 2. Missing `this.` Keyword

All instance member access must use `this.`:
```csharp
// ✅ Correct
this._repository.CreateAsync(entity, cancellationToken);
this.LogDebug("Creating entity");

// ❌ Wrong
_repository.CreateAsync(entity, cancellationToken);
LogDebug("Creating entity");
```

### 3. Wrong Null Validation Pattern
```csharp
// ✅ Correct
ArgumentNullException.ThrowIfNull(repository);
ArgumentException.ThrowIfNullOrWhiteSpace(id);

// ❌ Wrong
this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(...);
```

### 4. Using ILoggerFactory Instead of ILogger

Service base classes accept `ILogger`, not `ILoggerFactory`:
```csharp
// ✅ Correct
public CustomerService(
    ILogger<CustomerService> logger)
    : base(
        ServiceLocator.GetService<ICustomerRepository>(),
        logger)

// ❌ Wrong — no ILoggerFactory overload exists
public CustomerService(
    IAsyncCrudlRepository<Customer, string> repository,
    ILoggerFactory factory)
    : base(repository, factory)
```

### 5. Accessing Protected Repository from Outside

`Repository` is `protected` — test classes cannot access it:
```csharp
// ❌ Won't compile — Repository is protected
Assert.IsNotNull(entity.Repository);
```

### 6. Read Throwing for Missing Entities

Read should return `null` when not found, not throw. This enables the composed `ExistsAsync` to work without exception-driven control flow.

### 7. CancellationToken Not Last

`CancellationToken` must always be the last parameter:
```csharp
// ✅ Correct
public async Task ProcessAsync(
    string id,
    ILogger? logger = null,
    CancellationToken cancellationToken = default)

// ❌ Wrong
public async Task ProcessAsync(
    string id,
    CancellationToken cancellationToken = default,
    ILogger? logger = null)
```

### 8. Using `this.Logger.LogDebug()` Instead of `this.LogDebug()`

When inheriting from `BaseClassWithLogging`, use the convenience methods that check the log level before formatting:
```csharp
// ✅ Correct — checks IsEnabled first, avoids unnecessary string formatting
this.LogDebug("Processing {Count} items", items.Count);

// ❌ Wrong — formats string even if Debug is disabled
this.Logger.LogDebug("Processing {Count} items", items.Count);
```

### 9. Declaring the Service Interface as `public`

Service interfaces should be `internal`. The consuming application depends on the concrete service class (which is `public`), not the interface. Making the interface `public` exposes internal contracts that the application layer should not see.
```csharp
// ✅ Correct
internal interface ICustomerService
    : IAsyncCrudlService<Customer, string>
{
}

// ❌ Wrong — exposes internal contract to consuming application
public interface ICustomerService
    : IAsyncCrudlService<Customer, string>
{
}
```

### 10. Declaring the Concrete Service as `internal`

The concrete service class must be `public` so the consuming application can depend on it. The application does not see the internal interfaces — it only sees the concrete class and its public constructor.
```csharp
// ✅ Correct
public sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService

// ❌ Wrong — consuming application cannot access the service
internal sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService
```

### 11. Single Constructor on Concrete Service

Concrete services require two constructors: a `public` one for the consuming application (resolves repository via `ServiceLocator`) and an `internal` one for unit tests (accepts repository directly via `InternalsVisibleTo`).
```csharp
// ✅ Correct — dual constructor pattern
public sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService
{
    public CustomerService(
        ILogger<CustomerService> logger)
        : base(
            ServiceLocator.GetService<ICustomerRepository>(),
            logger)
    {
    }

    internal CustomerService(
        ICustomerRepository repository,
        ILogger<CustomerService> logger)
        : base(repository, logger)
    {
    }
}

// ❌ Wrong — single constructor exposes internal repository interface
public sealed class CustomerService
    : BaseAsyncCrudlService<Customer, string>,
      ICustomerService
{
    public CustomerService(
        IAsyncCrudlRepository<Customer, string> repository,
        ILogger<CustomerService> logger)
        : base(repository, logger)
    {
    }
}
```

### 12. Missing `using Roadbed;` in Service Implementation

The public constructor calls `ServiceLocator.GetService<T>()`, which requires the `Roadbed` namespace:
```csharp
// ✅ Correct
using Roadbed;

// ❌ Wrong — compile error on ServiceLocator reference
// (missing using Roadbed;)
```

### 13. Missing `ServiceLocator.SetLocatorProvider()` in Installer

The installer must call `ServiceLocator.SetLocatorProvider()` after registering services, otherwise the service's public constructor cannot resolve the repository:
```csharp
// ✅ Correct
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<ICustomerRepository, CustomerRepository>();
    ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
}

// ❌ Wrong — ServiceLocator cannot resolve ICustomerRepository
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<ICustomerRepository, CustomerRepository>();
}
```