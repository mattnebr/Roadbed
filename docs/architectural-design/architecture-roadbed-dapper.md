# Roadbed.Data.Dapper Architecture

Roadbed.Data.Dapper provides Dapper configuration utilities for SQLite databases. It handles two problems that arise when using Dapper with SQLite: date/time type conversion (SQLite stores all temporal values as TEXT) and column-to-property mapping via `[Column]` attributes.

All types in this package use the `Roadbed.Data` namespace so consumers do not need additional `using` statements beyond what Roadbed.Data already requires.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Data.Dapper NuGet package. When a developer asks you to create a data access layer that uses Dapper with SQLite, use this document together with the [Roadbed.Crud Architecture](architecture-roadbed-crud.md), [Roadbed.Data Architecture](architecture-roadbed-data.md), and [Roadbed.Data.Sqlite Architecture](architecture-roadbed-data-sqlite.md) to scaffold the correct entity types, type handler registration, column mapping configuration, and repository/service patterns.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Database entities inherit from `BaseEntityClass<TId>`** — this ensures they implement `IEntity<TId>` and are compatible with Roadbed.Crud repositories.
3. **Register all 4 type handlers** in the installer — `DateTime`, `DateTime?`, `DateTimeOffset`, `DateTimeOffset?`.
4. **Call `DapperMapping.Configure()`** with all entity types before any queries execute.
5. **Use `IEntity<>` interface scanning** to discover entity types — do not manually list each type or filter by namespace.
6. **Use `[Column]` attributes** on entity properties when the database column name differs from the C# property name.
7. **All DateTime values are stored and retrieved as UTC** — non-UTC values are converted automatically.
8. **DateTimeOffset preserves timezone offset** — stored as ISO 8601 with offset (e.g., `"2024-01-15 14:30:00-06:00"`).
9. **Use Newtonsoft.Json** for serialization, not System.Text.Json.
10. **Namespace is `Roadbed.Data`**, not `Roadbed.Data.Dapper` — follows the namespace flattening convention.
11. **Registration order matters** — configure mappings and type handlers before registering repositories.
12. **Repository interfaces and service interfaces are `internal`** — the application layer depends on the concrete service class, not any internal interface.
13. **Concrete service classes are `public`** with a dual constructor pattern: a `public` constructor (takes only `ILogger<T>`, resolves the repository via `ServiceLocator`) and an `internal` constructor (takes the repository and `ILogger<T>` directly, for unit tests via `InternalsVisibleTo`).

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-dapper.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-dapper.md#type-catalog)
3. [Package Relationship](architecture-roadbed-dapper.md#package-relationship)
4. [Type Handlers](architecture-roadbed-dapper.md#type-handlers)
    - [DateTime Handling](architecture-roadbed-dapper.md#datetime-handling)
    - [DateTimeOffset Handling](architecture-roadbed-dapper.md#datetimeoffset-handling)
    - [Storage Format Reference](architecture-roadbed-dapper.md#storage-format-reference)
    - [Choosing DateTime vs DateTimeOffset](architecture-roadbed-dapper.md#choosing-datetime-vs-datetimeoffset)
5. [Column Mapping](architecture-roadbed-dapper.md#column-mapping)
    - [DapperMapping.Configure()](architecture-roadbed-dapper.md#dappermappingconfigure)
    - [Resolution Order](architecture-roadbed-dapper.md#resolution-order)
    - [Entity Example with Column Attributes](architecture-roadbed-dapper.md#entity-example-with-column-attributes)
6. [Relationship to Roadbed.Crud](architecture-roadbed-dapper.md#relationship-to-roadbedcrud)
    - [Entity Hierarchy](architecture-roadbed-dapper.md#entity-hierarchy)
    - [Why BaseEntityClass for Database Entities](architecture-roadbed-dapper.md#why-baseentityclass-for-database-entities)
    - [Repository and Service Layer](architecture-roadbed-dapper.md#repository-and-service-layer)
7. [Installer Wiring](architecture-roadbed-dapper.md#installer-wiring)
    - [Registration Order](architecture-roadbed-dapper.md#registration-order)
    - [IEntity Interface Scanning](architecture-roadbed-dapper.md#ientity-interface-scanning)
    - [Complete Installer Example](architecture-roadbed-dapper.md#complete-installer-example)
8. [Implementation Walkthrough](architecture-roadbed-dapper.md#implementation-walkthrough)
9. [Common Pitfalls](architecture-roadbed-dapper.md#common-pitfalls)

---

## Type Catalog

Roadbed.Data.Dapper contains **5 public types**, all in the `Roadbed.Data` namespace.

### Type Handlers (4 types)

| Type                                  | Handles           | Nullable | Storage Format                                     |
| ------------------------------------- | ----------------- | -------- | -------------------------------------------------- |
| `DapperDateTimeHandler`               | `DateTime`        | No       | `yyyy-MM-dd HH:mm:ss` (UTC)                        |
| `DapperNullableDateTimeHandler`       | `DateTime?`       | Yes      | `yyyy-MM-dd HH:mm:ss` (UTC) or `DBNull`            |
| `DapperDateTimeOffsetHandler`         | `DateTimeOffset`  | No       | `yyyy-MM-dd HH:mm:sszzz` (with offset)             |
| `DapperNullableDateTimeOffsetHandler` | `DateTimeOffset?` | Yes      | `yyyy-MM-dd HH:mm:sszzz` (with offset) or `DBNull` |

### Column Mapping (1 type)

| Type            | Kind         | Purpose                                                                  |
| --------------- | ------------ | ------------------------------------------------------------------------ |
| `DapperMapping` | Static class | Thread-safe configuration of `[Column]` attribute-based Dapper type maps |

---

## Package Relationship
```
┌──────────────────────────────────────────────────────────────┐
│ Your Class Library                                           │
│                                                              │
│   DbFoo : BaseEntityClass<long>    (entity)                  │
│   IDbFooRepository (internal)      (from Roadbed.Crud)       │
│   DbFooRepository  (internal)      (uses SqliteExecutor)     │
│   IDbFooService    (internal)      (from Roadbed.Crud)       │
│   DbFooService     (public)        (virtual, calls repo)     │
│   InstallFooDatabase               (wires everything)        │
└──────────┬───────────────────────────────────────────────────┘
           │ uses
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Crud                                                 │
│                                                              │
│   IEntity<TId>             → entity contract                 │
│   BaseEntityClass<TId>     → mutable entity base (Dapper)    │
│   BaseEntityRecord<TId>    → immutable entity base (APIs)    │
│   IAsyncCrudlRepository    → repository composite interface  │
│   BaseAsyncCrudlService    → service base class (virtual)    │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Data.Dapper  (namespace: Roadbed.Data)               │
│                                                              │
│   DapperMapping              → [Column] attribute mapping    │
│   DapperDateTimeHandler      → DateTime ↔ SQLite TEXT        │
│   DapperNullableDateTimeHandler                              │
│   DapperDateTimeOffsetHandler                                │
│   DapperNullableDateTimeOffsetHandler                        │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Data.Sqlite                                          │
│                                                              │
│   SqliteConnectionFactory  → creates SqliteConnection        │
│   SqliteExecutor           → Dapper + retry logic            │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Data                                                 │
│                                                              │
│   IDataConnectionFactory                                     │
│   DataConnecionString                                        │
│   DataExecutorRequest                                        │
└──────────────────────────────────────────────────────────────┘
```

---

## Type Handlers

### DateTime Handling

`DapperDateTimeHandler` and `DapperNullableDateTimeHandler` convert between C# `DateTime` / `DateTime?` and SQLite TEXT, enforcing UTC throughout.

**Parse (database → C#):**
```
Database value
    │
    ├── string → DateTime.Parse() → SpecifyKind(Utc)
    ├── DateTime (UTC) → return as-is
    ├── DateTime (non-UTC) → ToUniversalTime()
    ├── null / DBNull → null (nullable handler only)
    └── other → throw InvalidOperationException
```

**SetValue (C# → database):**
```
C# value
    │
    ├── DateTime (UTC) → format as "yyyy-MM-dd HH:mm:ss"
    ├── DateTime (non-UTC) → ToUniversalTime() → format
    ├── null → DBNull.Value (nullable handler only)
    └── DbType set to String
```

**Key behavior:** Non-UTC values are **automatically converted to UTC** before storage. This means a `DateTime` with `DateTimeKind.Local` or `DateTimeKind.Unspecified` will be converted. The returned value always has `DateTimeKind.Utc`.

### DateTimeOffset Handling

`DapperDateTimeOffsetHandler` and `DapperNullableDateTimeOffsetHandler` convert between C# `DateTimeOffset` / `DateTimeOffset?` and SQLite TEXT, **preserving the original timezone offset**.

**Parse (database → C#):**
```
Database value
    │
    ├── string → DateTimeOffset.Parse() (offset preserved)
    ├── DateTimeOffset → return as-is
    ├── null / DBNull → null (nullable handler only)
    └── other → throw InvalidOperationException
```

**SetValue (C# → database):**
```
C# value
    │
    ├── DateTimeOffset → format as "yyyy-MM-dd HH:mm:sszzz"
    ├── null → DBNull.Value (nullable handler only)
    └── DbType set to String
```

**Key behavior:** Unlike `DateTime`, `DateTimeOffset` **preserves the timezone offset**. A value created in Central Time (`-06:00`) is stored and retrieved with that offset intact.

### Storage Format Reference

| C# Type           | SQLite Type  | Storage Format              | Example                       |
| ----------------- | ------------ | --------------------------- | ----------------------------- |
| `DateTime`        | TEXT         | `yyyy-MM-dd HH:mm:ss`      | `2024-01-15 14:30:00`         |
| `DateTime?`       | TEXT or NULL | `yyyy-MM-dd HH:mm:ss`      | `2024-01-15 14:30:00`         |
| `DateTimeOffset`  | TEXT         | `yyyy-MM-dd HH:mm:sszzz`   | `2024-01-15 14:30:00-06:00`   |
| `DateTimeOffset?` | TEXT or NULL | `yyyy-MM-dd HH:mm:sszzz`   | `2024-01-15 14:30:00-06:00`   |

### Choosing DateTime vs DateTimeOffset

| Scenario                                   | Use                 | Rationale                                  |
| ------------------------------------------ | ------------------- | ------------------------------------------ |
| Created/updated timestamps                 | `DateTime` (UTC)    | System events are timezone-agnostic        |
| Scheduled events                           | `DateTimeOffset`    | Preserves the user's local time context    |
| API response timestamps                    | `DateTime` (UTC)    | Standard for machine-to-machine communication |
| User appointment times                     | `DateTimeOffset`    | "3pm Central" should stay "3pm Central"    |
| Nullable audit fields (e.g., `deleted_at`) | `DateTime?`         | May be null when not yet occurred          |

---

## Column Mapping

### DapperMapping.Configure()

Configures Dapper to map database column names to C# properties using `[Column]` attributes from `System.ComponentModel.DataAnnotations.Schema`.
```csharp
public static class DapperMapping
{
    // Configure with params array
    public static void Configure(params Type[] types);

    // Configure with IEnumerable
    public static void Configure(IEnumerable<Type> types);
}
```

**Key behaviors:**

- **Thread-safe** — uses a lock and `HashSet<Type>` to ensure each type is configured exactly once
- **Idempotent** — calling `Configure()` multiple times with the same type is safe
- **Resolution order:** `[Column]` attribute match → case-insensitive property name match → `InvalidOperationException`

### Resolution Order

When Dapper reads a column from a query result, `DapperMapping` resolves the target property in this order:
```
Database column name (e.g., "display_name")
    │
    ├── 1. [Column("display_name")] found? → use that property
    │
    ├── 2. Property name matches (case-insensitive)? → use that property
    │       e.g., column "Name" matches property "Name" or "name"
    │
    └── 3. No match → throw InvalidOperationException
```

This means:

- Properties with `[Column]` attributes always win
- Properties without `[Column]` attributes still work if the name matches
- Unmatched columns cause a clear error at runtime

### Entity Example with Column Attributes
```csharp
namespace Foo.Database;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Roadbed.Crud;

/// <summary>
/// Database entity for the foo table.
/// </summary>
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("id")]
    public override long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
```

**Key patterns:**

- Inherits from `BaseEntityClass<long>` — implements `IEntity<long>` from Roadbed.Crud, making the entity discoverable via interface scanning and compatible with Roadbed.Crud repository/service patterns
- `Id` property uses `override` — `BaseEntityClass<TId>.Id` is `virtual`
- Every property has a `[Column]` attribute matching the snake_case database column name
- `DateTime` properties for system timestamps (stored as UTC)
- `DateTime?` for nullable audit fields
- Entity class is `sealed` — no further inheritance needed
- All properties use `{ get; set; }` — required for Dapper's `CustomPropertyTypeMap` to set values during materialization

---

## Relationship to Roadbed.Crud

### Entity Hierarchy

Database entities plug directly into the Roadbed.Crud type hierarchy:
```
IEntity<TId>                    ← Roadbed.Crud interface
    │
    ├── BaseEntityRecord<TId>   ← Immutable (API responses, configuration)
    │
    └── BaseEntityClass<TId>    ← Mutable (database entities, Dapper)
            │
            └── DbFoo           ← Your database entity
```

### Why BaseEntityClass for Database Entities

Database entities **must** use `BaseEntityClass<TId>` (not `BaseEntityRecord<TId>`) because:

| Requirement                             | BaseEntityClass | BaseEntityRecord                     |
| --------------------------------------- | --------------- | ------------------------------------ |
| Dapper property setting                 | ✅ `{ get; set; }` | ❌ `{ get; init; }` not settable  |
| Mutable state after creation            | ✅ Yes          | ❌ No                                |
| `[Column]` attribute with `DapperMapping` | ✅ Compatible | ❌ CustomPropertyTypeMap fails       |
| Roadbed.Crud repository compatibility   | ✅ Yes          | ✅ Yes                               |

`BaseEntityRecord<TId>` is reserved for API response entities, configuration objects, and other immutable data — not for database entities.

### Repository and Service Layer

Database entities are consumed through the Roadbed.Crud repository/service pattern:
```
┌──────────────────────────┐
│ Application Layer         │
│                           │
│ Depends on:               │
│   DbFooService (public)   │  ← Concrete service class
│   Public ctor: ILogger    │
└──────────┬────────────────┘
           │
┌──────────▼────────────────┐
│ DbFooService (public)     │
│   : BaseAsyncCrudlService │  ← Roadbed.Crud virtual base class
│                           │
│ Public ctor:              │
│   ILogger<DbFooService>   │
│   Resolves repo via       │
│   ServiceLocator          │
│                           │
│ Internal ctor:            │
│   IDbFooRepository +      │
│   ILogger<DbFooService>   │
│   (for unit tests)        │
│                           │
│ Calls:                    │
│   this._repository.*()    │  ← Delegates to repository
└──────────┬────────────────┘
           │
┌──────────▼────────────────┐
│ DbFooRepository (internal)│
│   : BaseClassWithLogging  │  ← Roadbed.Common base class
│                           │
│ Uses:                     │
│   SqliteExecutor.*()      │  ← Roadbed.Data.Sqlite executor
│   IFooDatabaseFactory     │  ← Roadbed.Data marker interface
│   DbFoo entity            │  ← BaseEntityClass<long>
└───────────────────────────┘
```

Both the repository interface and service interface are `internal` — the application layer depends on the concrete service class, not any internal interface. See the [Roadbed.Crud Architecture](architecture-roadbed-crud.md) for full details on the repository/service separation.

---

## Installer Wiring

### Registration Order

The installer must configure Dapper **before** any repositories are resolved. Follow this exact order:
```
1. DapperMapping.Configure(entityTypes)    ← Column attribute mapping
2. SqlMapper.AddTypeHandler(...)           ← All 4 type handlers
3. services.AddSingleton<IRepo, Repo>()   ← Repository registrations (for ServiceLocator)
4. ServiceLocator.SetLocatorProvider(...)  ← Snapshot for NuGet self-containment
```

**Why order matters:**

- `DapperMapping.Configure()` sets up `SqlMapper.SetTypeMap()` for each entity type. If a repository executes a query before this runs, Dapper won't know how to map snake_case columns to PascalCase properties.
- Type handlers must be registered before any query that returns `DateTime` or `DateTimeOffset` columns. Without them, Dapper may return incorrect values or throw exceptions.

**Note:** Service interfaces are not registered in the installer. The concrete service class is `public` and the consuming application instantiates it directly (or via DI) by providing `ILogger<T>`. The service's public constructor resolves the repository via `ServiceLocator` internally.

### IEntity Interface Scanning

Instead of manually listing each entity type or filtering by namespace, use the `IEntity<>` interface from Roadbed.Crud to discover all concrete entity classes in the assembly:
```csharp
Type[] entityTypes = typeof(DbFoo).Assembly
    .GetTypes()
    .Where(t => t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEntity<>)))
    .ToArray();

DapperMapping.Configure(entityTypes);
```

**Why IEntity scanning over namespace scanning:**

- Adding a new entity doesn't require updating the installer
- No dependency on a specific namespace string that could be renamed or refactored
- Guarantees only types that implement `IEntity<TId>` are configured — no risk of accidentally including non-entity classes
- Uses a well-known anchor type (`DbFoo`) to locate the assembly
- Filters to concrete classes only (no interfaces, no abstract base classes)

### Complete Installer Example
```csharp
namespace Foo.Database;

using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;
using Roadbed.Crud;
using Roadbed.Data;

/// <summary>
/// Installer for Foo Database services.
/// </summary>
public sealed class InstallFooDatabase
    : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Step 1: Auto-discover all entity types implementing IEntity<>
        Type[] entityTypes = typeof(DbFoo).Assembly
            .GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IEntity<>)))
            .ToArray();

        // Step 2: Configure Dapper column mappings from [Column] attributes
        DapperMapping.Configure(entityTypes);

        // Step 3: Register all type handlers for SQLite TEXT ↔ C# temporal types
        SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());

        // Step 4: Register repositories (internal) for ServiceLocator resolution
        services.AddSingleton<IDbFooRepository, DbFooRepository>();
        services.AddSingleton<IDbBarRepository, DbBarRepository>();

        // Step 5: Capture ServiceLocator snapshot for NuGet self-containment
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods
}
```

---

## Implementation Walkthrough

This walkthrough shows how to add Roadbed.Data.Dapper to an existing data access project that already has Roadbed.Data.Sqlite configured.

### Step 1: Add Package References
```xml
<PackageReference Include="Roadbed.Crud" />
<PackageReference Include="Roadbed.Data.Dapper" />
<PackageReference Include="Roadbed.Data.Sqlite" />
```

### Step 2: Create the Entity
```csharp
namespace Foo.Database;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Roadbed.Crud;

/// <summary>
/// Database entity for the foo table.
/// </summary>
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("id")]
    public override long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

### Step 3: Create the Repository

The repository uses `SqliteExecutor` from Roadbed.Data.Sqlite. Dapper automatically maps results to the entity using the `[Column]` attributes configured by `DapperMapping`.
```csharp
namespace Foo.Database;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Repository implementation for DbFoo data access.
/// </summary>
internal sealed class DbFooRepository
    : BaseAsyncCrudlRepository<DbFoo, long>,
      IDbFooRepository
{
    private readonly IFooDatabaseFactory _connectionFactory;
    private readonly ILogger<DbFooRepository> _logger;

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbFooRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public DbFooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<DbFooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
        this._logger = logger;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<DbFoo> CreateAsync(
        DbFoo entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var request = new DataExecutorRequest
        {
            Sql = @"
                INSERT INTO foo
                (
                     name
                    ,display_name
                    ,created_at
                    ,updated_at
                )
                VALUES
                (
                     @Name
                    ,@DisplayName
                    ,@CreatedAt
                    ,@UpdatedAt
                )
                ;
                SELECT last_insert_rowid()
                ;",
            Parameters = entity,
        };

        long newId = await SqliteExecutor.ExecuteScalarAsync<long>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        entity.Id = newId;

        return entity;
    }

    /// <inheritdoc/>
    public override async Task<DbFoo?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                SELECT
                     f.id
                    ,f.name
                    ,f.display_name
                    ,f.created_at
                    ,f.updated_at
                FROM
                    foo AS f
                WHERE
                    f.id = @Id
                ;",
            Parameters = new { Id = id },
        };

        return await SqliteExecutor.QuerySingleOrDefaultAsync<DbFoo>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<DbFoo> UpdateAsync(
        DbFoo entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var request = new DataExecutorRequest
        {
            Sql = @"
                UPDATE foo
                SET
                     name = @Name
                    ,display_name = @DisplayName
                    ,updated_at = @UpdatedAt
                WHERE
                    id = @Id
                ;",
            Parameters = entity,
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return entity;
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                DELETE FROM foo
                WHERE
                    id = @Id
                ;",
            Parameters = new { Id = id },
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<IList<DbFoo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                SELECT
                     f.id
                    ,f.name
                    ,f.display_name
                    ,f.created_at
                    ,f.updated_at
                FROM
                    foo AS f
                ORDER BY
                    f.name
                ;",
        };

        var results = await SqliteExecutor.QueryAsync<DbFoo>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }

    #endregion Public Methods
}
```

**How mapping works at runtime:**

1. Dapper executes the query and gets column names: `id`, `name`, `display_name`, `created_at`, `updated_at`
2. For each column, `DapperMapping` looks for a `[Column]` attribute match on `DbFoo`
3. `display_name` → matches `[Column("display_name")]` on `DisplayName` property
4. `created_at` → matches `[Column("created_at")]` on `CreatedAt` property
5. The `DapperDateTimeHandler` converts the TEXT value `"2024-01-15 14:30:00"` to a `DateTime` with `DateTimeKind.Utc`

### Step 4: Create the Service

The service delegates to the repository. The concrete service class is `public sealed` with the dual constructor pattern. See the [Roadbed.Crud Architecture](architecture-roadbed-crud.md) for details on overrides and business logic.
```csharp
namespace Foo.Database;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for DbFoo business operations.
/// </summary>
public sealed class DbFooService
    : BaseAsyncCrudlService<DbFoo, long>,
      IDbFooService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbFooService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public DbFooService(
        ILogger<DbFooService> logger)
        : base(
            ServiceLocator.GetService<IDbFooRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbFooService"/> class.
    /// </summary>
    /// <param name="repository">Repository for DbFoo data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal DbFooService(
        IDbFooRepository repository,
        ILogger<DbFooService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors
}
```

### Step 5: Wire Up the Installer

Add the Dapper configuration to the installer (see [Complete Installer Example](#complete-installer-example) above).

---

## Common Pitfalls

### 1. Forgetting to Register Type Handlers
```csharp
// ❌ Wrong — DateTime columns will parse incorrectly or throw
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    DapperMapping.Configure(entityTypes);
    // Missing: SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
}

// ✅ Correct — all 4 handlers registered
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    DapperMapping.Configure(entityTypes);
    SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
}
```

### 2. Forgetting to Call DapperMapping.Configure()
```csharp
// ❌ Wrong — [Column] attributes are ignored, snake_case columns won't map
SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
services.AddSingleton<IDbFooRepository, DbFooRepository>();

// ✅ Correct — configure mapping before registering repositories
DapperMapping.Configure(entityTypes);
SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
services.AddSingleton<IDbFooRepository, DbFooRepository>();
```

### 3. Missing [Column] Attribute on Snake_Case Properties
```csharp
// ❌ Wrong — "display_name" column won't match "DisplayName" property
public sealed class DbFoo : BaseEntityClass<long>
{
    public string DisplayName { get; set; } = string.Empty;  // No [Column]
}

// ✅ Correct — explicit mapping
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
```

Note: Case-insensitive property name matching is the fallback. A column named `name` will match a property named `Name` without a `[Column]` attribute. But `display_name` will **not** match `DisplayName` — the `[Column]` attribute is required when the names differ beyond casing.

### 4. Using Namespace Scanning Instead of IEntity Scanning
```csharp
// ❌ Fragile — depends on a namespace string, includes non-entity classes
Type[] entityTypes = typeof(DbFoo).Assembly
    .GetTypes()
    .Where(t => t.Namespace == "Foo.Database.Entities" &&
                t.IsClass &&
                !t.IsAbstract)
    .ToArray();

// ✅ Better — IEntity interface scanning discovers only entity implementations
Type[] entityTypes = typeof(DbFoo).Assembly
    .GetTypes()
    .Where(t => t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEntity<>)))
    .ToArray();
```

### 5. Using BaseEntityRecord Instead of BaseEntityClass
```csharp
// ❌ Wrong — Dapper's CustomPropertyTypeMap requires settable properties
public sealed record DbFoo : BaseEntityRecord<long>
{
    [Column("id")]
    public override long Id { get; init; }  // Dapper can't set 'init' properties
}

// ✅ Correct — BaseEntityClass with { get; set; }
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("id")]
    public override long Id { get; set; }
}
```

### 6. Assuming DateTime Values Preserve Local Time
```csharp
// ❌ Wrong assumption — Local time is converted to UTC on storage
var entity = new DbFoo
{
    CreatedAt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Local),
};
// Stored as UTC equivalent, e.g., "2024-01-15 20:30:00" (if UTC-6)

// ✅ Correct — use UTC explicitly
var entity = new DbFoo
{
    CreatedAt = DateTime.UtcNow,
};

// ✅ Or use DateTimeOffset to preserve timezone
[Column("appointment_time")]
public DateTimeOffset AppointmentTime { get; set; }
// Stored as "2024-01-15 14:30:00-06:00" — offset preserved
```

### 7. Missing `override` on Id Property
```csharp
// ❌ Wrong — hides the base class property instead of overriding it
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("id")]
    public long Id { get; set; }  // Compiler warning: hides inherited member
}

// ✅ Correct — override the virtual property from BaseEntityClass
public sealed class DbFoo : BaseEntityClass<long>
{
    [Column("id")]
    public override long Id { get; set; }
}
```

### 8. Wrong Registration Order
```csharp
// ❌ Wrong — repositories registered before Dapper is configured
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
    DapperMapping.Configure(entityTypes);
    SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
}

// ✅ Correct — Dapper configuration first, then repositories
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    DapperMapping.Configure(entityTypes);
    SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
    ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
}
```

### 9. Missing `this.` on Instance Members
```csharp
// ❌ Wrong
public DbFooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<DbFooRepository> logger)
    : base(logger)
{
    _connectionFactory = connectionFactory;  // Missing this.
    _logger = logger;                        // Missing this.
}

// ✅ Correct
public DbFooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<DbFooRepository> logger)
    : base(logger)
{
    this._connectionFactory = connectionFactory;
    this._logger = logger;
}
```

### 10. Registering Service Interfaces in the Installer

Service interfaces are `internal` and the concrete service class is `public`. The installer only registers the repository (for `ServiceLocator` resolution). The consuming application resolves the concrete service class directly.
```csharp
// ❌ Wrong — service interface should not be registered
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
    services.AddSingleton<IDbFooService, DbFooService>();
    ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
}

// ✅ Correct — only register repository for ServiceLocator
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IDbFooRepository, DbFooRepository>();
    ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
}
```

---

## Quick Reference

### Required Using Statements
```csharp
using Roadbed.Data;   // Type handlers and DapperMapping (namespace flattened)
using Roadbed.Crud;   // IEntity<>, BaseEntityClass<> (in entities and installer)
using Roadbed;         // ServiceLocator, BaseClassWithLogging (in services and installer)
using Dapper;          // SqlMapper.AddTypeHandler (in installer only)
using System.ComponentModel.DataAnnotations.Schema;  // [Column] attribute (in entities only)
```

### Installer Registration Checklist

- [ ] Scan assembly for all concrete classes implementing `IEntity<>`
- [ ] Call `DapperMapping.Configure(entityTypes)` with scanned types
- [ ] Register `DapperDateTimeHandler`
- [ ] Register `DapperNullableDateTimeHandler`
- [ ] Register `DapperDateTimeOffsetHandler`
- [ ] Register `DapperNullableDateTimeOffsetHandler`
- [ ] Register all repository interfaces → implementations (for `ServiceLocator`)
- [ ] Call `ServiceLocator.SetLocatorProvider(services.BuildServiceProvider())`

### Entity Property Checklist

- [ ] Inherits from `BaseEntityClass<TId>` (not `BaseEntityRecord<TId>`)
- [ ] `Id` property uses `override` keyword
- [ ] Every property has `{ get; set; }` (not `{ get; init; }`)
- [ ] Every property with a non-matching column name has `[Column("snake_case")]`
- [ ] `DateTime` properties for UTC timestamps (created_at, updated_at)
- [ ] `DateTime?` for nullable temporal columns (deleted_at)
- [ ] `DateTimeOffset` when timezone preservation is needed
- [ ] Entity class is `sealed`

### Type Handler Decision
```
What temporal type does the column use?
    │
    ├── Non-nullable timestamp (UTC)     → DateTime   + DapperDateTimeHandler
    ├── Nullable timestamp (UTC)         → DateTime?  + DapperNullableDateTimeHandler
    ├── Non-nullable with timezone       → DateTimeOffset  + DapperDateTimeOffsetHandler
    └── Nullable with timezone           → DateTimeOffset? + DapperNullableDateTimeOffsetHandler
```

### Entity Base Class Decision
```
Where does this entity come from?
    │
    ├── Database (Dapper)    → BaseEntityClass<TId>  (mutable, { get; set; })
    ├── API response         → BaseEntityRecord<TId> (immutable, { get; init; })
    └── Configuration        → BaseEntityRecord<TId> (immutable, { get; init; })
```