# Repository-Service Pattern

## Overview

The Repository-Service Pattern divides data access and business logic into three distinct roles. The Entity represents a single domain concept as a plain object with properties and no infrastructure concerns. The Repository provides an abstraction over the data store, exposing operations like Create, Read, Update, Delete, and List (CRUDL) through an interface that hides whether the backing store is a relational database, a REST API, a file system, or an in-memory collection. The Service sits between the consuming application and the Repository, providing a place for validation, transformation, orchestration of multiple repositories, and any business rules that don't belong inside the Entity itself.

The key insight is separation of concerns. The Entity knows nothing about how it is stored. The Repository knows nothing about business rules. The Service knows nothing about HTTP controllers, console applications, or any other entry point that consumes it. Each layer depends only on the layer directly below it, and each layer is accessed through an interface rather than a concrete class.

### History

The term Repository-Service Pattern was derived from multiple concepts. The origins can be traced back to the following:

- **Repository Pattern** comes from Eric Evans' _Domain-Driven Design_ (2003) — the idea that data access is abstracted behind a collection-like interface
- **Service Layer Pattern** comes from Martin Fowler's _Patterns of Enterprise Application Architecture_ (2002) — a thin orchestration layer between the application and the repositories
- **Entity** is just the domain model — the data representation

Neither Evans nor Fowler described the combined pattern as a single named concept. The fusion of an Entity, a Repository that manages persistence for that Entity, and a Service that orchestrates business logic on top of the Repository emerged organically in the Java and .NET enterprise communities during the mid-2000s. By the time ASP.NET MVC gained traction around 2009–2012, this three-layer structure had become the de facto standard for organizing data access in .NET applications. Microsoft's own documentation and reference architectures reinforced the pattern, and it became so ubiquitous that most .NET developers treat it as a baseline assumption rather than a deliberate architectural choice.

### How the Layers Interact

The pattern follows a strict top-down dependency flow. The application layer (a controller, a background job, a CLI tool) depends on the concrete Service class. The Service depends on the Repository interface. The Repository depends on the Entity and on whatever infrastructure it needs to persist or retrieve that Entity. No layer reaches upward or sideways.

A typical request flows like this: the application calls a method on the Service, passing an Entity or an identifier. The Service applies any business rules, then delegates to the Repository. The Repository translates the call into a data store operation (a SQL query, an HTTP request, a file read) and returns the result. The Service may transform the result before returning it to the application.

Both the Service interface and the Repository interface are `internal` to the class library. The consuming application never sees them. The concrete Service class is `public` and exposes two constructors: a `public` constructor that accepts only an `ILogger<T>` and resolves its repository dependency internally via `ServiceLocator`, and an `internal` constructor that accepts both the repository and the logger directly. Unit test projects use `InternalsVisibleTo` to access the internal constructor and inject mock repositories.

This means the application layer never knows which concrete Repository the Service is using, and the internal wiring of the class library remains hidden. The consuming application only needs to provide a logger — the Service resolves its own infrastructure dependencies.

### Why It Works

The pattern's durability comes from the fact that it solves several problems simultaneously. Testability improves because each layer can be tested in isolation by mocking the layer below it. A Service can be tested without a real database by substituting a mock Repository through the internal constructor. Maintainability improves because changes to the data store are contained within the Repository — switching from SQL Server to SQLite, or from a database to a REST API, requires no changes to the Service or the application layer. Readability improves because developers know exactly where to look for business logic (the Service), data access (the Repository), and data shape (the Entity).

The pattern also scales well with team size. In a large codebase, different developers can work on different layers without creating merge conflicts. One developer can build out the Repository while another defines the Service interface and writes tests against it using mocks.

### When It Becomes Overhead

The pattern is not free. For simple CRUD applications with no business logic beyond what the database provides, the Service layer can feel like an unnecessary pass-through that delegates every call directly to the Repository without adding value. In these cases, some teams choose to skip the Service layer and have the application layer depend on the Repository directly. This is a valid trade-off for small projects, but it makes it harder to add business logic later without restructuring the dependency chain.

The pattern also introduces a mapping cost. When the Entity (the domain model) differs from the DTO (the shape of the data as it arrives from an API or database), someone has to write the mapping code. This is deliberate — the separation between the external data shape and the internal domain model is one of the pattern's strengths — but it does require more code than simply passing raw data through.

### Relationship to Other Patterns

The Repository-Service Pattern is a subset of Layered Architecture, which can include additional layers like presentation, infrastructure, and cross-cutting concerns. It is compatible with Clean Architecture (Robert C. Martin) and Hexagonal Architecture (Alistair Cockburn), both of which emphasize dependency inversion and port/adapter separation. In those frameworks, the Repository interface acts as a "port" and the concrete Repository implementation acts as an "adapter."

The pattern is distinct from the Active Record pattern, where the Entity itself contains persistence logic (e.g., `entity.Save()`). It is also distinct from CQRS (Command Query Responsibility Segregation), which splits read and write operations into separate models, though CQRS can be layered on top of the Repository-Service Pattern when the complexity warrants it.

---

## Recommended Implementation

This section walks through the recommended way to implement the Repository-Service Pattern using the Roadbed NuGet packages. Each sub-section builds on the previous one, following the same order you would use when scaffolding a new module from scratch: define the Entity first, then the Repository, then the Service, then the Installer that wires everything together.

The [Roadbed.Crud Architecture Overview](/docs/architectural-design/architecture-roadbed-crud.md) document provides an overview of the interfaces that are involved in our preferred framework.

---

### Entities

Every module begins with the Entity. In Roadbed.Crud, the entity layer is anchored by a single interface, two base implementations, and a clear rule about which base to use depending on where the data comes from.

#### IEntity\<TId\>

The `IEntity<TId>` interface is the root contract for all entities in the Roadbed ecosystem. It declares a single property:
```csharp
namespace Roadbed.Crud;

public interface IEntity<TId>
{
    TId? Id { get; }
}
```

The generic type parameter `TId` represents the data type of the entity's identifier. Common choices are `string` for API-sourced entities (where the remote system assigns a string-based ID), `long` for database entities with auto-increment primary keys, and `int` for smaller lookup tables.

Every Roadbed.Crud repository and service is generic over `TEntity` and `TId`, with a constraint that `TEntity : IEntity<TId>`. This means any class or record you want to pass through the CRUDL pipeline must implement this interface. The two base implementations described below handle this automatically.

#### Two Base Implementations

Roadbed.Crud provides two abstract base types that implement `IEntity<TId>`. The choice between them is not a matter of preference — it is determined by the data source and the mutability requirements of the entity.

**`BaseEntityRecord<TId>`** is an abstract record type. Records provide value-based equality, structural immutability, and concise syntax. Use this base for entities that represent data received from external APIs, configuration objects, or any data that should not be mutated after creation.
```csharp
namespace Roadbed.Crud;

public abstract record BaseEntityRecord<TId> : IEntity<TId>
{
    public virtual TId? Id { get; set; }
}
```

**`BaseEntityClass<TId>`** is an abstract class type. Classes provide reference-based identity and fully mutable state. Use this base for entities that are mapped to database tables via Dapper, managed by an ORM, or require complex inheritance hierarchies.
```csharp
namespace Roadbed.Crud;

public abstract class BaseEntityClass<TId> : IEntity<TId>
{
    public virtual TId? Id { get; set; }
}
```

Both bases declare `Id` as `virtual` so that concrete entities can override it with additional attributes such as `[JsonProperty]` or `[Column]`.

The [Roadbed.Crud Architecture Overview](/docs/architectural-design/architecture-roadbed-crud.md) document includes guidance on how you should choose between the two options.

#### Entity Implementation Example
```csharp
namespace MySolution.Sdk.MyProject;

using Roadbed.Crud;

/// <summary>
/// Represents a custom entity in my project.
/// </summary>
public sealed class CustomEntity : BaseEntityRecord<long>
{
    /// <summary>
    /// Gets or sets the entity's name.
    /// </summary>
    required public string Name { get; set; }

    /// <summary>
    /// Gets or sets the entity's description.
    /// </summary>
    required public string Description { get; set; }
}
```

Key characteristics of this entity:

- **`override` on `Id`** is optional. The base class declares `Id` as `virtual` with `{ get; set; }`, so the inherited property works as-is. Override it only when you need to add an attribute like `[Column("id")]` for Dapper column mapping or `[JsonProperty("id")]` to control Newtonsoft serialization.
- **`{ get; set; }`** on every property is mandatory. Dapper and Newtonsoft.Json need to set property values during materialization and deserialization processes. They cannot work with `{ get; init; }` or read-only properties.

#### Common Entity Pitfalls

**Using `BaseEntityRecord<TId>` for a database entity.** Dapper's `CustomPropertyTypeMap` requires mutable class properties. Records with `{ get; init; }` properties will fail during Dapper materialization. Always use `BaseEntityClass<TId>` for entities that are read from or written to a database via Dapper.

**Declaring `Id` without `override` when the intent is to override.** Both `BaseEntityRecord<TId>` and `BaseEntityClass<TId>` declare `Id` as `virtual`. If a concrete entity declares its own `Id` property without the `override` keyword, the compiler will issue a warning about hiding the inherited member, and Roadbed.Crud's generic constraints may not resolve the property correctly. If you do not need to add attributes to `Id`, simply omit the property entirely and let the base class version be used as-is. If you do need to add `[JsonProperty]` or `[Column]` to it, use `override`.

**Missing `[Column]` attributes on properties with snake_case column names.** The `DapperMapping` fallback is case-insensitive property name matching. A column named `name` will match a property named `Name`, but a column named `display_name` will not match a property named `DisplayName`. The `[Column]` attribute is required whenever the column name and property name differ beyond casing.

**Using `DateTime` with `DateTimeKind.Local`.** The `DapperDateTimeHandler` automatically converts local times to UTC before storage, which silently shifts the value. If you create a `DateTime` with `DateTimeKind.Local` at 2:30 PM Central and store it, the stored value will be 8:30 PM UTC. Always use `DateTime.UtcNow` or construct DateTime values with `DateTimeKind.Utc` explicitly.

**Using `[JsonProperty]` on a database entity or `[Column]` on an API entity.** These attributes serve different purposes and should not be mixed on the same entity. `[JsonProperty]` controls Newtonsoft.Json serialization for API communication. `[Column]` controls Dapper column mapping for database access. If the same conceptual data needs both API and database representations, create two separate entity types and map between them in the Service or Repository layer.

**Omitting the `required` keyword on non-nullable API entity properties.** Without `required`, callers can construct an entity without providing values for non-nullable properties, which leads to default values (empty strings, zero) silently propagating through the system. The `required` keyword enforces that callers provide values at construction time. The `Id` property is the exception because it is often assigned server-side.

### Set-up & Installation

#### Dapper Mapping

When the module installer registers Dapper column mappings, it needs to know which types in the assembly are database entities. Rather than maintaining a manual list of types, the recommended approach is to scan the assembly for all concrete classes that implement `IEntity<>`:
```csharp
Type[] entityTypes = typeof(CustomEntity).Assembly
    .GetTypes()
    .Where(t => t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEntity<>)))
    .ToArray();

DapperMapping.Configure(entityTypes);
```

This pattern uses a well-known anchor type to locate the assembly, filters to concrete classes only (excluding interfaces and abstract base classes), and checks for the `IEntity<>` generic interface. Adding a new entity to the assembly automatically includes it in the Dapper mapping configuration — no installer changes required.

---

### Services

The Service is the public face of a module. It is the only layer that the application (a controller, a background job, a CLI tool) depends on directly. The concrete Service class is `public`, but both the Service interface and the Repository interface are `internal` — the consuming application never interacts with them directly. The application should not be aware of the internal workings of the class library.

In Roadbed.Crud, services sit on top of repositories and serve two purposes. First, they provide a pass-through to the repository for standard CRUDL operations so the application layer does not need to know about the repository at all. Second, they provide a place to add business logic — validation, transformation, orchestration, caching — by overriding individual methods. The base class methods are `virtual`, not `abstract`, which means a service with no business logic requires zero overrides. It works immediately with nothing more than its constructors.

Services also provide two composed operations that repositories do not have: **Exists** and **Upsert**. These are built from repository primitives automatically. Exists calls Read and checks whether the result is not null. Upsert calls Exists to decide whether to delegate to Create or Update. Both can be overridden when the data source supports more efficient implementations.

#### Choosing the Service Composite Interface

Roadbed.Crud provides five async service composite interfaces (and five matching sync counterparts). Choose the one that matches the operations your module needs to expose:

| Interface               | Operations                                                  | Use When                                              |
| ----------------------- | ----------------------------------------------------------- | ----------------------------------------------------- |
| `IAsyncListOnlyService` | List                                                        | Read-only lookup data (state codes, categories)       |
| `IAsyncCrudService`     | Create, Read, Update, Delete, Exists, Upsert                | Full CRUD without List (large tables, custom queries) |
| `IAsyncCrudlService`    | Create, Read, Update, Delete, List, Exists, Upsert          | Full CRUD + List (small-to-medium tables)             |
| `IAsyncCrudaService`    | Create, Read, Update, Delete, Archive, Exists, Upsert       | CRUD + soft delete, no List                           |
| `IAsyncCrudalService`   | Create, Read, Update, Delete, Archive, List, Exists, Upsert | Full CRUDAL (the most complete composite)             |

The service composite always includes Exists and Upsert when it includes CRUD operations. This is a key difference from the repository composites, which only contain the data-access primitives.

#### Defining the Service Interface

The service interface is `internal` and inherits from the matching Roadbed.Crud service composite. It declares no members of its own unless the module needs custom operations beyond what the composite provides. Making the interface `internal` ensures the consuming application has no visibility into the class library's internal contracts — it depends only on the concrete service class.
```csharp
namespace MySolution.Sdk.MyProject;

using Roadbed.Crud.Services.Async;

/// <summary>
/// Service interface for custom entity operations.
/// </summary>
internal interface ICustomEntityService
    : IAsyncCrudlService<CustomEntity, long>
{
}
```

This single declaration gives the service layer access to all seven operations — Create, Read, Update, Delete, List, Exists, and Upsert — through the inherited interface members.

When a module needs operations beyond the standard CRUDL set, declare them directly on the service interface:
```csharp
/// <summary>
/// Service interface for custom entity operations.
/// </summary>
internal interface ICustomEntityService
    : IAsyncCrudlService<CustomEntity, long>
{
    /// <summary>
    /// Lists entities within a date range.
    /// </summary>
    /// <param name="range">Date range for list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of entities within the range.</returns>
    Task<IList<CustomEntity>> ListByDateRangeAsync(
        CustomRangeRequest range,
        CancellationToken cancellationToken = default);
}
```

Custom methods follow the same conventions as the built-in operations: `CancellationToken` is always the last parameter with `= default`, and the method name describes the operation clearly.

#### Implementing the Service

The service implementation is `public sealed` and inherits from two types: the matching Roadbed.Crud service base class and the module's service interface. The base class provides the virtual pass-through implementations. The interface satisfies the internal contract used for dependency injection within the class library and for unit testing.

##### The Dual Constructor Pattern

Every concrete service class exposes two constructors:

- **A `public` constructor** that accepts only `ILogger<T>`. This is the constructor the consuming application uses. It resolves the repository dependency internally via `ServiceLocator.GetService<T>()`, keeping the application unaware of the class library's internal interfaces and wiring.
- **An `internal` constructor** that accepts both the repository interface and `ILogger<T>` directly. This is the constructor unit test projects use via `InternalsVisibleTo` to inject mock repositories. It also supports dependency injection within the class library itself.

This separation ensures the consuming application only needs to provide a logger, while unit tests retain full control over all dependencies.

##### The Zero-Override Service

The most common case is a service that adds no business logic. It simply delegates every operation to the repository through the base class. This requires nothing beyond the dual constructors:
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for custom entity operations.
/// </summary>
public sealed class CustomEntityService
    : BaseAsyncCrudlService<CustomEntity, long>,
      ICustomEntityService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityService(
        ILogger<CustomEntityService> logger)
        : base(
            ServiceLocator.GetService<ICustomEntityRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="repository">Repository for custom entity data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal CustomEntityService(
        ICustomEntityRepository repository,
        ILogger<CustomEntityService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors
}
```

With this implementation, all seven operations work immediately. Create, Read, Update, Delete, and List delegate to the repository. Exists composes from Read. Upsert composes from Exists, Create, and Update. There is no code to write and no methods to override.

Key characteristics of the constructors:

- **The public constructor accepts only `ILogger<CustomEntityService>`.** The consuming application provides a logger and nothing else. The repository is resolved internally via `ServiceLocator.GetService<ICustomEntityRepository>()`, which retrieves the registered implementation from the DI container. This keeps the internal repository interface hidden from the application layer.
- **The internal constructor accepts `ICustomEntityRepository` and `ILogger<CustomEntityService>`.** Unit test projects access this constructor via `InternalsVisibleTo` and pass a mock repository directly, enabling isolated testing without `ServiceLocator` or a real DI container.
- **The repository parameter type is `ICustomEntityRepository`**, the module's custom repository interface — not the generic Roadbed.Crud composite (e.g., `IAsyncCrudlRepository<CustomEntity, long>`). This is intentional. The custom repository interface inherits from the generic composite, so passing it to `base(repository, logger)` satisfies the base class constructor. Using the custom interface in `ServiceLocator.GetService<T>()` ensures the correct DI registration is resolved.
- **The logger parameter type is `ILogger<CustomEntityService>`**, not `ILoggerFactory`. Service base classes inherit from `BaseClassWithLogging`, which takes `ILogger`. Only use `ILoggerFactory` when the class genuinely needs to create loggers for other categories.
- **No null validation is needed in the constructor body.** The base class constructor (`BaseAsyncCrudlService`) validates the repository parameter with `ArgumentNullException.ThrowIfNull()` and the `BaseClassWithLogging` constructor handles the logger. The concrete service constructors only need to call `base(...)`.
- **`using Roadbed;`** is required for access to `ServiceLocator`.

##### Adding Business Logic via Overrides

When a service needs to enforce business rules, add logging, apply transformations, or perform validation, override the specific method and call `base` to delegate to the repository:
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for custom entity operations.
/// </summary>
public sealed class CustomEntityService
    : BaseAsyncCrudlService<CustomEntity, long>,
      ICustomEntityService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityService(
        ILogger<CustomEntityService> logger)
        : base(
            ServiceLocator.GetService<ICustomEntityRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="repository">Repository for custom entity data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal CustomEntityService(
        ICustomEntityRepository repository,
        ILogger<CustomEntityService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<CustomEntity> CreateAsync(
        CustomEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);

        this.LogInformation("Creating custom entity: {Name}", entity.Name);

        return await base.CreateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.LogInformation("Deleting custom entity: {Id}", id);

        // Business rule: verify the entity exists before deleting
        bool exists = await this.ExistsAsync(id, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"Cannot delete custom entity '{id}' because it does not exist.");
        }

        await base.DeleteAsync(id, cancellationToken);
    }

    #endregion Public Methods
}
```

Only the methods that need business logic are overridden. The remaining operations continue to delegate directly to the repository through the base class. This keeps the service focused — each override represents a deliberate business decision rather than boilerplate.

Note the use of `this.LogInformation()` and `this.ExistsAsync()`. Because the service inherits from `BaseClassWithLogging`, the level-checked logging convenience methods are available directly. And because the service inherits from the Roadbed.Crud service base, the composed `ExistsAsync` method is also available and can be called from other overrides.

#### How Exists and Upsert Work

Exists and Upsert are service-level operations that do not exist on the repository interface. They are composed from repository primitives inside the service base class.

**Exists** calls the repository's `ReadAsync` and checks whether the result is not null:
```csharp
public virtual async Task<bool> ExistsAsync(
    TId id,
    CancellationToken cancellationToken = default)
{
    var entity = await this._repository.ReadAsync(id, cancellationToken);
    return entity is not null;
}
```

This is why the Roadbed.Crud convention requires `ReadAsync` to return `null` for missing entities rather than throwing an exception. If Read threw a `NotFoundException`, the composed Exists would need exception-driven control flow, which is both slower and harder to reason about.

**Upsert** calls Exists to decide between Create and Update:
```csharp
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
```

The logic follows three branches: when the entity's Id is null, Create is called because there is no identifier to check. When the Id is not null and Exists returns true, Update is called. When the Id is not null and Exists returns false, Create is called because the entity has an Id but does not yet exist in the data store.

Both methods are `virtual` and can be overridden. The most common reason to override Upsert is when the data source supports a native upsert operation (SQL Server's `MERGE`, PostgreSQL's `ON CONFLICT`, or SQLite's `INSERT OR REPLACE`). In that case, overriding Upsert to call the repository directly with a single native query eliminates the two-step Exists-then-Create/Update round trip:
```csharp
public override async Task<CustomEntity> UpsertAsync(
    CustomEntity entity,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(entity);

    this.LogDebug("Upserting custom entity: {Id}", entity.Id);

    // Delegate to a custom repository method that uses INSERT OR REPLACE
    return await this.Repository.CreateAsync(entity, cancellationToken);
}
```

#### The Protected Repository Property

The service base class exposes the repository through a `protected` property:
```csharp
protected IAsyncCrudlRepository<TEntity, TId> Repository => this._repository;
```

This property is available inside method overrides but is not visible to external consumers. It exists so that overrides can call repository methods directly when the base class pass-through is not sufficient. In the Upsert override above, `this.Repository.CreateAsync()` calls the repository directly, bypassing the service-level `CreateAsync` override (if one exists).

The distinction matters: calling `base.CreateAsync()` invokes the base class virtual method, which delegates to the repository. Calling `this.Repository.CreateAsync()` invokes the repository directly. In most cases `base.CreateAsync()` is the correct choice because it preserves any business logic added by other overrides. Use `this.Repository` directly only when you intentionally need to bypass the service layer — for example, when implementing a native upsert that should not trigger the service-level Create validation.

#### Custom Service Methods

When the service interface declares custom methods beyond the CRUDL composite, implement them in the service class directly. These methods typically call the protected `Repository` property or coordinate between multiple operations:
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for custom entity operations.
/// </summary>
public sealed class CustomEntityService
    : BaseAsyncCrudlService<CustomEntity, long>,
      ICustomEntityService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityService(
        ILogger<CustomEntityService> logger)
        : base(
            ServiceLocator.GetService<ICustomEntityRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="repository">Repository for custom entity data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal CustomEntityService(
        ICustomEntityRepository repository,
        ILogger<CustomEntityService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<IList<CustomEntity>> ListByDateRangeAsync(
        CustomRangeRequest range,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug(
            "Listing entities in range: ({Start}) to ({End})",
            range.Start,
            range.End);

        // Delegate to the full list and filter in memory,
        // or delegate to a custom repository method
        var allEntities = await this.Repository.ListAsync(cancellationToken);

        return allEntities
            .Where(s => s.CreatedAt >= range.Start
                     && s.CreatedAt <= range.End)
            .ToList();
    }

    #endregion Public Methods
}
```

If the custom method requires a query that the standard repository composite does not expose (for example, a filtered database query instead of an in-memory filter), the repository interface should declare the custom method (Level 3 consumption in the Roadbed.Crud decision tree), and the service should access it through a separate field. The protected `Repository` property is typed as the generic Roadbed.Crud composite and does not have access to custom repository methods. In this scenario, store a typed reference to the custom repository interface alongside the base class parameter:
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for custom entity operations.
/// </summary>
public sealed class CustomEntityService
    : BaseAsyncCrudlService<CustomEntity, long>,
      ICustomEntityService
{
    private readonly ICustomEntityRepository _customRepository;

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityService(
        ILogger<CustomEntityService> logger)
        : this(
            ServiceLocator.GetService<ICustomEntityRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityService"/> class.
    /// </summary>
    /// <param name="repository">Repository for custom entity data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal CustomEntityService(
        ICustomEntityRepository repository,
        ILogger<CustomEntityService> logger)
        : base(repository, logger)
    {
        this._customRepository = repository;
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<IList<CustomEntity>> ListByDateRangeAsync(
        CustomRangeRequest range,
        CancellationToken cancellationToken = default)
    {
        return await this._customRepository.ListByDateRangeAsync(
            range,
            cancellationToken);
    }

    #endregion Public Methods
}
```

This works because `ICustomEntityRepository` inherits from `IAsyncCrudlRepository<CustomEntity, long>`, so passing it to `base(repository, logger)` satisfies the base class constructor. Storing the same instance in `this._customRepository` gives the service access to the custom methods without requiring a second DI registration.

Note that when a service needs the `_customRepository` field, the public constructor chains to the internal constructor using `: this(...)` instead of calling `: base(...)` directly. This ensures the `_customRepository` field is assigned in a single place — the internal constructor — and avoids resolving from `ServiceLocator` twice.

#### Skipping the Service Layer

For modules that serve as pure data lookups with no business logic and no foreseeable need for validation or transformation, the service layer can be skipped entirely. In this case, the application layer depends on the repository interface directly, and the repository interface is declared `public` instead of `internal`:
```csharp
// Repository interface (public — no service layer)
namespace MySolution.Sdk.ReferenceTables;

using Roadbed.Crud.Repositories.Sync;

public interface IDimensionRepository
    : ISyncListOnlyRepository<DimensionEntity, string>
{
}
```

This is a valid trade-off for read-only lookup data like state codes, country codes, or other dimension tables. The cost is that if business logic needs to be added later, the application layer's dependency must change from the repository interface to a new service class, which is a breaking change.

#### Common Service Pitfalls

**Passing `ILoggerFactory` instead of `ILogger<T>` to the service constructor.** Service base classes inherit from `BaseClassWithLogging`, which takes `ILogger` in its constructor. There is no `ILoggerFactory` overload. Passing `ILoggerFactory` will not compile. Use `ILogger<TService>` and let the constructors call `base(...)`.

**Using `this.Logger.LogDebug()` instead of `this.LogDebug()`.** The base class convenience methods check `IsEnabled()` before formatting the log message, which avoids unnecessary string allocation when the log level is disabled. Calling `this.Logger.LogDebug()` (if `Logger` is accessible) bypasses this check and formats the string unconditionally.

**Overriding every method when no business logic is needed.** If a service method does nothing beyond calling `base.CreateAsync(entity, cancellationToken)`, the override adds noise without value. Only override methods that need validation, transformation, logging, or other business rules. The base class pass-through is the correct behavior for operations that need no additional logic.

**Forgetting that Upsert calls Exists, which calls Read.** The composed Upsert performs two or three repository calls: one Read (via Exists), then either one Create or one Update. For high-throughput scenarios or when the data source supports native upsert, override `UpsertAsync` to eliminate the extra round trips.

**Calling `this.Repository.CreateAsync()` when `base.CreateAsync()` is intended.** The protected `Repository` property calls the repository directly, bypassing any service-level overrides. If the service has a `CreateAsync` override that adds validation, calling `this.Repository.CreateAsync()` from another method will skip that validation. Use `base.CreateAsync()` to go through the base class virtual dispatch, which preserves the override chain.

**Declaring the service interface as `public` instead of `internal`.** Service interfaces should be `internal`. The consuming application depends on the concrete service class (which is `public`), not the interface. Making the interface `public` exposes internal contracts that the application layer should not see. The concrete service class's public constructor — which accepts only `ILogger<T>` and resolves the repository via `ServiceLocator` — is the only surface the application needs.

**Declaring the repository implementation as `public` instead of `internal`.** Repository implementations should be `internal sealed`. The application layer depends on the service (which is `public`), not the repository. Making the repository `public` breaks the encapsulation that the Repository-Service Pattern is designed to provide. Only the repository interface should be `public`, and only when the service layer is being skipped entirely (see "Skipping the Service Layer" above).

**Trying to access `this.Repository` from outside the service.** The `Repository` property is `protected` — it is available inside the service class and its overrides, but not from external code such as controllers or test classes. Test classes should interact with the service through the internal constructor and the service's public methods, not by accessing the repository directly.

**Forgetting `using Roadbed;` in the service implementation file.** The public constructor calls `ServiceLocator.GetService<T>()`, which requires the `Roadbed` namespace. Omitting this using statement will produce a compile error on the `ServiceLocator` reference.

**Using `: base(...)` instead of `: this(...)` when the service has a `_customRepository` field.** When the service stores a typed reference to the custom repository interface (for custom methods), the public constructor should chain to the internal constructor with `: this(ServiceLocator.GetService<ICustomEntityRepository>(), logger)`. This ensures the `_customRepository` field is assigned in a single place and avoids resolving from `ServiceLocator` twice.

---

### Repositories

The Repository is the data access layer. It translates CRUDL operations into concrete infrastructure calls — SQL queries for a database, HTTP requests for a REST API, file reads for a flat-file store. The repository interface is `internal` and the implementation is `internal sealed`. The application layer never touches either one; it interacts with the service, which delegates to the repository.

In Roadbed.Crud, repository base classes are the opposite of service base classes. Every method is `abstract`, not `virtual`. There are no default implementations. This is intentional — the base class cannot know how to talk to your data store, so you must provide every operation. If a method does not apply (for example, Delete on a read-only API), throw `NotSupportedException` in the implementation.

The repository composite interfaces mirror the service composites but without Exists and Upsert, which are composed at the service level:

| Repository Composite       | Operations                                  |
| -------------------------- | ------------------------------------------- |
| `IAsyncListOnlyRepository` | List                                        |
| `IAsyncCrudRepository`     | Create, Read, Update, Delete                |
| `IAsyncCrudlRepository`    | Create, Read, Update, Delete, List          |
| `IAsyncCrudaRepository`    | Create, Read, Update, Delete, Archive       |
| `IAsyncCrudalRepository`   | Create, Read, Update, Delete, Archive, List |

The choice of composite should match the service composite. If the service is `IAsyncCrudlService`, the repository should be `IAsyncCrudlRepository`.

The two examples below show the most common repository patterns: one backed by a SQLite database and one backed by a REST API. Both follow the same structure — an internal interface, an internal sealed implementation, and abstract method overrides that delegate to the appropriate infrastructure.

For a complete reference on the interfaces, base classes, and composite hierarchy, see the [Roadbed.Crud Architecture Overview](/docs/architectural-design/architecture-roadbed-crud.md).

#### Database Repository (Roadbed.Data)

A database repository uses Roadbed.Data for connection management and Roadbed.Data.Sqlite for query execution. The repository injects a database-specific marker interface that extends `IDataConnectionFactory`, and calls `SqliteExecutor` static methods to execute parameterized SQL.

For a complete reference on connection factories, executor requests, retry logic, and Dapper type handlers, see the [Roadbed.Data Architecture Overview](/docs/architectural-design/architecture-roadbed-data.md), [Roadbed.Data.Sqlite Architecture Overview](/docs/architectural-design/architecture-roadbed-data-sqlite.md), and [Roadbed.Data.Dapper Architecture Overview](/docs/architectural-design/architecture-roadbed-data-dapper.md).

##### Interface
```csharp
namespace MySolution.Sdk.MyProject;

using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Repository interface for custom entity data access.
/// </summary>
internal interface ICustomEntityRepository
    : IAsyncCrudlRepository<CustomEntity, long>
{
}
```

##### Implementation
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Repository implementation for custom entity data access.
/// </summary>
internal sealed class CustomEntityRepository
    : BaseAsyncCrudlRepository<CustomEntity, long>,
      ICustomEntityRepository
{
    private readonly ICustomDatabaseFactory _connectionFactory;
    private readonly ILogger<CustomEntityRepository> _logger;

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityRepository(
        ICustomDatabaseFactory connectionFactory,
        ILogger<CustomEntityRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);

        this._connectionFactory = connectionFactory;
        this._logger = logger;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<CustomEntity> CreateAsync(
        CustomEntity entity,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                INSERT INTO custom_entities
                (
                     name
                    ,description
                    ,created_at
                    ,updated_at
                )
                VALUES
                (
                     @Name
                    ,@Description
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
    public override async Task<CustomEntity?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                SELECT
                     ce.id
                    ,ce.name
                    ,ce.description
                    ,ce.created_at
                    ,ce.updated_at
                FROM
                    custom_entities AS ce
                WHERE
                    ce.id = @Id
                ;",
            Parameters = new { Id = id },
        };

        return await SqliteExecutor.QuerySingleOrDefaultAsync<CustomEntity>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<CustomEntity> UpdateAsync(
        CustomEntity entity,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                UPDATE custom_entities
                SET
                     name = @Name
                    ,description = @Description
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
                DELETE FROM custom_entities
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
    public override async Task<IList<CustomEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest
        {
            Sql = @"
                SELECT
                     ce.id
                    ,ce.name
                    ,ce.description
                    ,ce.created_at
                    ,ce.updated_at
                FROM
                    custom_entities AS ce
                ORDER BY
                    ce.name
                ;",
        };

        var results = await SqliteExecutor.QueryAsync<CustomEntity>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }

    #endregion Public Methods
}
```

Every method follows the same pattern: build a `DataExecutorRequest` with the SQL and parameters, call the appropriate `SqliteExecutor` static method, and return the result. The `SqliteExecutor` handles connection management, retry logic for transient SQLite errors, and Dapper materialization internally.

Note that `ReadAsync` returns `null` when no row matches the query. This is not an error — it is the Roadbed.Crud convention that enables the composed `ExistsAsync` at the service level.

The `ICustomDatabaseFactory` marker interface and its implementation are part of the module's infrastructure setup. See the [Roadbed.Data Architecture Overview](/docs/architectural-design/architecture-roadbed-data.md) for the connection factory pattern.

#### REST API Repository (Roadbed.Net)

A REST API repository uses Roadbed.Net to make HTTP requests to an external service. The repository injects `INetHttpClient` and calls `MakeHttpRequestAsync<T>()` to send requests and deserialize responses.

For a complete reference on request configuration, retry policies, authentication, and response handling, see the [Roadbed.Net Architecture Overview](/docs/architectural-design/architecture-roadbed-net.md).

##### Interface
```csharp
namespace MySolution.Sdk.MyProject;

using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Repository interface for custom entity API access.
/// </summary>
internal interface ICustomEntityRepository
    : IAsyncCrudlRepository<CustomEntity, string>
{
}
```

##### Implementation
```csharp
namespace MySolution.Sdk.MyProject;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Net;

/// <summary>
/// Repository implementation for custom entity API access.
/// </summary>
internal sealed class CustomEntityRepository
    : BaseAsyncCrudlRepository<CustomEntity, string>,
      ICustomEntityRepository
{
    private readonly INetHttpClient _httpClient;
    private readonly ILogger<CustomEntityRepository> _logger;

    private const string BaseUrl = "https://api.example.com/v1/entities";

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityRepository"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CustomEntityRepository(
        INetHttpClient httpClient,
        ILogger<CustomEntityRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        this._httpClient = httpClient;
        this._logger = logger;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<CustomEntity?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var request = new NetHttpRequest
        {
            Endpoint = new Uri($"{BaseUrl}/{id}"),
        };

        var response = await this._httpClient
            .MakeHttpRequestAsync<CustomEntity>(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return response.Data;
    }

    /// <inheritdoc/>
    public override async Task<IList<CustomEntity>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new NetHttpRequest
        {
            Endpoint = new Uri(BaseUrl),
        };

        var response = await this._httpClient
            .MakeHttpRequestAsync<CustomEntityListResponse>(request, cancellationToken);

        if (!response.IsSuccessStatusCode || response.Data is null)
        {
            return Array.Empty<CustomEntity>();
        }

        return response.Data.Items.ToList();
    }

    /// <inheritdoc/>
    public override async Task<CustomEntity> CreateAsync(
        CustomEntity entity,
        CancellationToken cancellationToken = default)
    {
        var request = new NetHttpRequest
        {
            Endpoint = new Uri(BaseUrl),
            Method = HttpMethod.Post,
            Body = entity,
        };

        var response = await this._httpClient
            .MakeHttpRequestAsync<CustomEntity>(request, cancellationToken);

        if (!response.IsSuccessStatusCode || response.Data is null)
        {
            throw new InvalidOperationException(
                "Failed to create entity via API.");
        }

        return response.Data;
    }

    /// <inheritdoc/>
    public override async Task<CustomEntity> UpdateAsync(
        CustomEntity entity,
        CancellationToken cancellationToken = default)
    {
        var request = new NetHttpRequest
        {
            Endpoint = new Uri($"{BaseUrl}/{entity.Id}"),
            Method = HttpMethod.Put,
            Body = entity,
        };

        var response = await this._httpClient
            .MakeHttpRequestAsync<CustomEntity>(request, cancellationToken);

        if (!response.IsSuccessStatusCode || response.Data is null)
        {
            throw new InvalidOperationException(
                $"Failed to update entity '{entity.Id}' via API.");
        }

        return response.Data;
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var request = new NetHttpRequest
        {
            Endpoint = new Uri($"{BaseUrl}/{id}"),
            Method = HttpMethod.Delete,
        };

        var response = await this._httpClient
            .MakeHttpRequestAsync<string>(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to delete entity '{id}' via API.");
        }
    }

    #endregion Public Methods
}
```

The pattern is the same across all methods: build a `NetHttpRequest` with the endpoint and HTTP method, call `MakeHttpRequestAsync<T>()`, check the response, and return the result. The `NetHttpClient` handles retry logic, compression, timeout, and JSON deserialization internally.

Note that `ReadAsync` returns `null` when the API returns a non-success status code (including 404). This follows the same Roadbed.Crud convention as the database repository — the service-level `ExistsAsync` depends on Read returning null for missing entities.

Also note that the `TId` generic parameter is `string` rather than `long`. REST APIs commonly use string-based identifiers (UUIDs, slugs, or opaque tokens), while databases commonly use numeric auto-increment keys. The choice of `TId` should match whatever the data source uses.