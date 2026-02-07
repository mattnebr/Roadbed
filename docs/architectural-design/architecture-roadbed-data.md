# Roadbed.Data Architecture

Roadbed.Data provides database-agnostic abstractions for connection management and query execution. Roadbed.Data.Sqlite provides the concrete SQLite implementation.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Data and Roadbed.Data.Sqlite NuGet packages. When a developer asks you to create a class library that accesses a database, use this document to scaffold the correct connection factory, executor requests, and DI registrations.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation — not `?? throw new ArgumentNullException(...)`.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Create a database-specific factory interface** — never inject `IDataConnectionFactory` directly. Create a marker interface (e.g., `IFooDatabaseFactory`) that extends it.
5. **Create a database-specific factory class** — inherit from `SqliteConnectionFactory` and implement the marker interface.
6. **Use primary constructors** for factory implementations.
7. **Use Newtonsoft.Json** for serialization, not System.Text.Json.
8. **Flatten namespaces** — remove `.Dtos`, `.Entities` suffixes so consumers don't need extra `using` statements.
9. **CancellationToken is always the last parameter** with `= default`.
10. **Connections are returned open** — callers are responsible for disposing them.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-data.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-data.md#type-catalog)
3. [Package Relationship](architecture-roadbed-data.md#package-relationship)
4. [Connection Factory Pattern](architecture-roadbed-data.md#connection-factory-pattern)
    - [IDataConnectionFactory](architecture-roadbed-data.md#idataconnectionfactory)
    - [SqliteConnectionFactory](architecture-roadbed-data.md#sqliteconnectionfactory)
    - [Creating a Database-Specific Factory](architecture-roadbed-data.md#creating-a-database-specific-factory)
5. [Connection Strings](architecture-roadbed-data.md#connection-strings)
    - [DataConnecionString](architecture-roadbed-data.md#dataconnecionstring)
    - [DataConnectionStringType](architecture-roadbed-data.md#dataconnectionstringtype)
    - [Connection String Templates](architecture-roadbed-data.md#connection-string-templates)
6. [Query Execution](architecture-roadbed-data.md#query-execution)
    - [DataExecutorRequest](architecture-roadbed-data.md#dataexecutorrequest)
    - [Retry Configuration](architecture-roadbed-data.md#retry-configuration)
7. [Implementation Walkthrough](architecture-roadbed-data.md#implementation-walkthrough)
8. [Common Pitfalls](architecture-roadbed-data.md#common-pitfalls)

---

## Type Catalog

### Roadbed.Data (4 types)

| Type                       | Kind      | Namespace      | Purpose                                                          |
| -------------------------- | --------- | -------------- | ---------------------------------------------------------------- |
| `IDataConnectionFactory`   | Interface | `Roadbed.Data` | Contract for creating open database connections                  |
| `DataConnecionString`      | Class     | `Roadbed.Data` | Connection string builder with database-type-specific templates  |
| `DataConnectionStringType` | Enum      | `Roadbed.Data` | Database type: Unknown, Sqlite, SqliteInMemory                   |
| `DataExecutorRequest`      | Class     | `Roadbed.Data` | Query + parameters + retry configuration for executor operations |

### Roadbed.Data.Sqlite (1 type)

|Type|Kind|Namespace|Purpose|
|---|---|---|---|
|`SqliteConnectionFactory`|Class|`Roadbed.Data.Sqlite`|Concrete `IDataConnectionFactory` using `Microsoft.Data.Sqlite`|

---

## Package Relationship

```
┌──────────────────────────────────────────────┐
│ Your Application / Class Library             │
│                                              │
│   IFooDatabaseFactory  (marker interface)    │
│   FooDatabaseFactory   (implementation)      │
└──────────┬───────────────────────────────────┘
           │ inherits
┌──────────▼───────────────────────────────────┐
│ Roadbed.Data.Sqlite                          │
│                                              │
│   SqliteConnectionFactory                    │
│     implements IDataConnectionFactory        │
│     uses Microsoft.Data.Sqlite               │
└──────────┬───────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────┐
│ Roadbed.Data                                 │
│                                              │
│   IDataConnectionFactory  (interface)        │
│   DataConnecionString     (connection config) │
│   DataConnectionStringType (enum)            │
│   DataExecutorRequest     (query config)     │
└──────────────────────────────────────────────┘
```

Consuming applications reference **Roadbed.Data.Sqlite** which brings in **Roadbed.Data** transitively. Application code only interacts with the database-specific marker interface — never with `IDataConnectionFactory` directly.

---

## Connection Factory Pattern

### IDataConnectionFactory

The core abstraction for database connections:

```csharp
namespace Roadbed.Data;

public interface IDataConnectionFactory
{
    DataConnecionString Connecion { get; }

    IDbConnection CreateOpenConnection();

    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

**Key behaviors:**

- Both methods return an **already-open** connection
- Callers are responsible for **disposing** the connection (use `using` statements)
- The `Connecion` property exposes the underlying connection string configuration

### SqliteConnectionFactory

The concrete SQLite implementation in `Roadbed.Data.Sqlite`:

```csharp
namespace Roadbed.Data.Sqlite;

public class SqliteConnectionFactory : IDataConnectionFactory
{
    public SqliteConnectionFactory(DataConnecionString connectionString);

    public DataConnecionString Connecion { get; init; }

    public IDbConnection CreateOpenConnection();
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

**Key behaviors:**

- Creates `Microsoft.Data.Sqlite.SqliteConnection` instances
- Opens the connection before returning it
- Throws `SqliteException` if the connection cannot be opened
- Uses `ConfigureAwait(false)` in async path
- Validates `connectionString` is not null via `ArgumentNullException.ThrowIfNull()`

### Creating a Database-Specific Factory

Every consuming application creates **two types** for each database: a marker interface and an implementation class. This enables multiple databases in the same application via DI.

#### Step 1: Marker Interface

```csharp
namespace Foo.Database;

using Roadbed.Data;

/// <summary>
/// Database factory interface for the Foo database.
/// </summary>
public interface IFooDatabaseFactory
    : IDataConnectionFactory
{
}
```

The marker interface adds no members — it exists solely for DI resolution. When an application has multiple databases (e.g., Foo and Bar), each gets its own marker interface so the DI container can distinguish between them.

#### Step 2: Factory Implementation

```csharp
namespace Foo.Database;

using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Factory to create Foo Database Connections.
/// </summary>
/// <param name="connection">Connection string for the Foo Database.</param>
public class FooDatabaseFactory(DataConnecionString connection)
    : SqliteConnectionFactory(connection), IFooDatabaseFactory
{
}
```

**Key patterns:**

- Uses **primary constructor** syntax (C# 12+)
- Inherits from `SqliteConnectionFactory` for the implementation
- Implements `IFooDatabaseFactory` for the DI marker
- No body needed — the base class provides everything

#### Step 3: DI Registration

```csharp
namespace Foo.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;
using Roadbed.Data;

public sealed class InstallFooDatabase : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
        {
            DatabaseSource = configuration["FooDatabase:Path"],
        };

        services.AddSingleton<IFooDatabaseFactory>(
            new FooDatabaseFactory(connectionString));

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

#### Step 4: Usage in Repositories

```csharp
namespace Foo.Database;

using Microsoft.Extensions.Logging;
using Roadbed;

internal sealed class FooRepository : BaseClassWithLogging
{
    private readonly IFooDatabaseFactory _connectionFactory;

    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
    }

    public async Task<FooDto?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var connection = await this._connectionFactory
            .CreateOpenConnectionAsync(cancellationToken);

        // Use Dapper or ADO.NET to query
        var result = await connection.QuerySingleOrDefaultAsync<FooDto>(
            "SELECT id, name FROM foo WHERE id = @Id",
            new { Id = id });

        return result;
    }
}
```

---

## Connection Strings

### DataConnecionString

A builder class that generates database-specific connection strings from individual properties or wraps a raw connection string.

```csharp
namespace Roadbed.Data;

public class DataConnecionString
{
    // Constructor: type only (builds from properties)
    public DataConnecionString(DataConnectionStringType connectionStringType);

    // Constructor: type + raw string (uses string as-is)
    public DataConnecionString(
        DataConnectionStringType connectionStringType,
        string connectionString);

    // Computed connection string
    public string ConnectionString { get; }

    // DbConnectionStringBuilder for programmatic access
    public DbConnectionStringBuilder ConnectionStringBuilder { get; }

    // Configuration properties
    public DataConnectionStringType ConnectionStringType { get; }
    public string? DatabaseSource { get; set; }
    public string? ServerName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutInSeconds { get; set; }  // Default: 20
}
```

**Two modes of operation:**

1. **Property-based** — Set individual properties, the class generates the connection string based on `ConnectionStringType`
2. **Raw string** — Pass a complete connection string to the constructor, the class uses it as-is

### DataConnectionStringType

|Value|Int|Description|
|---|---|---|
|`Unknown`|0|Default / unset|
|`Sqlite`|1|SQLite file-based database|
|`SqliteInMemory`|2|SQLite in-memory database (shared cache)|

### Connection String Templates

#### SQLite (file-based)

```
Data Source={DatabaseSource};
Foreign Keys=true;
Pooling=true;
Default Timeout={TimeoutInSeconds};
```

#### SQLite In-Memory

```
Data Source={DatabaseSource ?? "DefaultInMemory"};
Mode=Memory;
Cache=Shared;
Foreign Keys=true;
Pooling=true;
Default Timeout={TimeoutInSeconds};
```

**Both templates enable:**

- `Foreign Keys=true` — enforces referential integrity
- `Pooling=true` — connection pooling for performance

**In-memory additionally sets:**

- `Mode=Memory` — in-memory storage
- `Cache=Shared` — allows multiple connections to share the same in-memory database
- Falls back to `"DefaultInMemory"` if `DatabaseSource` is not set

#### Raw Connection String

When the two-parameter constructor is used with a non-empty `connectionString`, the raw string is returned as-is, ignoring all properties.

```csharp
// Property-based (builds from template):
var conn = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = "/data/foo.db",
    TimeoutInSeconds = 30,
};
// conn.ConnectionString → "Data Source=/data/foo.db;Foreign Keys=true;..."

// Raw string (used as-is):
var conn = new DataConnecionString(
    DataConnectionStringType.Sqlite,
    "Data Source=/data/foo.db;Journal Mode=WAL;");
// conn.ConnectionString → "Data Source=/data/foo.db;Journal Mode=WAL;"
```

---

## Query Execution

### DataExecutorRequest

A configuration object for database query execution with built-in retry support.

```csharp
namespace Roadbed.Data;

public class DataExecutorRequest
{
    public DataExecutorRequest(string query);

    // Query
    public string Query { get; init; }
    public object? Parameters { get; set; }

    // Retry configuration
    public bool RetriesEnabled { get; set; }              // Default: true
    public int MaxRetries { get; set; }                   // Default: 3
    public TimeSpan DelayBetweenRetries { get; set; }     // Default: 100ms
    public bool DelayMultiplierEnabled { get; set; }      // Default: true
}
```

**Constructor validation:** `query` is validated with `ArgumentException.ThrowIfNullOrWhiteSpace()`.

**Property validation:**

- `MaxRetries` — throws `ArgumentOutOfRangeException` if negative
- `DelayBetweenRetries` — throws `ArgumentOutOfRangeException` if negative

### Retry Configuration

|Property|Default|Description|
|---|---|---|
|`RetriesEnabled`|`true`|Master switch for retry behavior|
|`MaxRetries`|`3`|Maximum number of retry attempts|
|`DelayBetweenRetries`|`100ms`|Base delay between retries|
|`DelayMultiplierEnabled`|`true`|Linear backoff: delay × attempt number|

**Backoff behavior when `DelayMultiplierEnabled` is `true`:**

|Attempt|Delay (with 100ms base)|
|---|---|
|1|100ms|
|2|200ms|
|3|300ms|

**Usage:**

```csharp
// Default retry configuration
var request = new DataExecutorRequest(
    "INSERT INTO foo (name) VALUES (@Name)")
{
    Parameters = new { Name = "Bar" },
};

// Custom retry configuration
var request = new DataExecutorRequest(
    "SELECT id, name FROM foo WHERE id = @Id")
{
    Parameters = new { Id = "123" },
    MaxRetries = 5,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(200),
    DelayMultiplierEnabled = true,
};

// No retries
var request = new DataExecutorRequest(
    "SELECT COUNT(*) FROM foo")
{
    RetriesEnabled = false,
};
```

---

## Implementation Walkthrough

This walkthrough shows how to create a new class library that accesses a SQLite database using the Roadbed.Data patterns.

### Step 1: Add Package References

```xml
<PackageReference Include="Roadbed.Data.Sqlite" />
```

This brings in `Roadbed.Data` transitively.

### Step 2: Create the Database Factory

**Marker interface (public):**

```csharp
namespace Foo.Database;

using Roadbed.Data;

public interface IFooDatabaseFactory
    : IDataConnectionFactory
{
}
```

**Implementation (public — needed for DI registration):**

```csharp
namespace Foo.Database;

using Roadbed.Data;
using Roadbed.Data.Sqlite;

/// <summary>
/// Factory to create Foo Database Connections.
/// </summary>
/// <param name="connection">Connection string for the Foo Database.</param>
public class FooDatabaseFactory(DataConnecionString connection)
    : SqliteConnectionFactory(connection), IFooDatabaseFactory
{
}
```

### Step 3: Create the Installer

```csharp
namespace Foo.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;
using Roadbed.Data;

public sealed class InstallFooDatabase : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
        {
            DatabaseSource = configuration["FooDatabase:Path"],
        };

        services.AddSingleton<IFooDatabaseFactory>(
            new FooDatabaseFactory(connectionString));

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### Step 4: Use in a Repository

```csharp
namespace Foo.Database;

using Microsoft.Extensions.Logging;
using Roadbed;

internal sealed class FooRepository : BaseClassWithLogging
{
    private readonly IFooDatabaseFactory _connectionFactory;

    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
    }

    public async Task<FooDto?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        this.LogDebug("Reading foo {Id}", id);

        using var connection = await this._connectionFactory
            .CreateOpenConnectionAsync(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<FooDto>(
            "SELECT id, name FROM foo WHERE id = @Id",
            new { Id = id });

        return result;
    }
}
```

### Step 5: In-Memory Database for Testing

```csharp
// In test setup
var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
{
    DatabaseSource = "FooTestDb",
};

var factory = new FooDatabaseFactory(connectionString);

// All connections from this factory share the same in-memory database
// (Cache=Shared in the connection string)
using var connection = factory.CreateOpenConnection();
// Create tables, seed data, run tests...
```

---

## Common Pitfalls

### 1. Injecting IDataConnectionFactory Directly

```csharp
// ❌ Wrong — no way to distinguish between multiple databases
public sealed class FooRepository : BaseClassWithLogging
{
    public FooRepository(
        IDataConnectionFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
    }
}

// ✅ Correct — inject the database-specific marker interface
public sealed class FooRepository : BaseClassWithLogging
{
    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
    }
}
```

### 2. Forgetting to Dispose Connections

```csharp
// ❌ Wrong — connection leak
public async Task<FooDto?> ReadAsync(string id, CancellationToken cancellationToken = default)
{
    var connection = await this._connectionFactory
        .CreateOpenConnectionAsync(cancellationToken);

    return await connection.QuerySingleOrDefaultAsync<FooDto>(query, new { Id = id });
}

// ✅ Correct — using declaration disposes after scope
public async Task<FooDto?> ReadAsync(string id, CancellationToken cancellationToken = default)
{
    using var connection = await this._connectionFactory
        .CreateOpenConnectionAsync(cancellationToken);

    return await connection.QuerySingleOrDefaultAsync<FooDto>(query, new { Id = id });
}
```

### 3. Opening the Connection Manually

```csharp
// ❌ Wrong — connection is already open
using var connection = await this._connectionFactory
    .CreateOpenConnectionAsync(cancellationToken);
await connection.OpenAsync(cancellationToken);  // Unnecessary, may throw

// ✅ Correct — factory returns an open connection
using var connection = await this._connectionFactory
    .CreateOpenConnectionAsync(cancellationToken);
// Ready to use immediately
```

### 4. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public FooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<FooRepository> logger)
    : base(logger)
{
    _connectionFactory = connectionFactory;  // Missing this.
}

// ✅ Correct
public FooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<FooRepository> logger)
    : base(logger)
{
    this._connectionFactory = connectionFactory;
}
```

### 5. Using Raw Connection String When Properties Work

```csharp
// ❌ Fragile — easy to introduce typos, hard to maintain
var conn = new DataConnecionString(
    DataConnectionStringType.Sqlite,
    "Data Source=/data/foo.db;Foreign Keys=true;Pooling=true;Default Timeout=20;");

// ✅ Better — type-safe, template handles formatting
var conn = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = "/data/foo.db",
    TimeoutInSeconds = 20,
};
```

### 6. Negative Retry Values

```csharp
// ❌ Wrong — throws ArgumentOutOfRangeException at runtime
var request = new DataExecutorRequest("SELECT 1")
{
    MaxRetries = -1,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(-50),
};

// ✅ Correct — disable retries with the flag
var request = new DataExecutorRequest("SELECT 1")
{
    RetriesEnabled = false,
};
```

### 7. Wrong Constructor for In-Memory Testing

```csharp
// ❌ Wrong — creates a file-based database
var conn = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = "TestDb",
};

// ✅ Correct — creates a shared in-memory database
var conn = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
{
    DatabaseSource = "TestDb",
};
```

---

## Quick Reference

### Required Using Statements

```csharp
using Roadbed;              // ServiceLocator, IServiceCollectionInstaller, BaseClassWithLogging
using Roadbed.Data;         // Interfaces, connection string, executor request
using Roadbed.Data.Sqlite;  // SqliteConnectionFactory (only in factory implementation)
```

### Factory Pattern Decision

```
Creating a new database connection?
    │
    ├── Define marker interface: IFooDatabaseFactory : IDataConnectionFactory
    ├── Define implementation:   FooDatabaseFactory(DataConnecionString) : SqliteConnectionFactory, IFooDatabaseFactory
    ├── Register in installer:   services.AddSingleton<IFooDatabaseFactory>(new FooDatabaseFactory(conn))
    └── Inject in repository:    IFooDatabaseFactory (never IDataConnectionFactory)
```

### Connection String Defaults

|Property|Default|
|---|---|
|`TimeoutInSeconds`|`20`|
|`Foreign Keys`|`true` (always)|
|`Pooling`|`true` (always)|
|`Cache`|`Shared` (in-memory only)|
|`Mode`|`Memory` (in-memory only)|

### DataExecutorRequest Defaults

|Property|Default|
|---|---|
|`RetriesEnabled`|`true`|
|`MaxRetries`|`3`|
|`DelayBetweenRetries`|`100ms`|
|`DelayMultiplierEnabled`|`true`|