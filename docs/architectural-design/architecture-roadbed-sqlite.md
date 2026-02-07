# Roadbed.Data.Sqlite Architecture

Roadbed.Data.Sqlite provides the concrete SQLite implementation of the Roadbed.Data abstractions. It includes a connection factory, a Dapper-based query executor with built-in retry logic, and utilities for in-memory database testing.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Data.Sqlite NuGet package. When a developer asks you to create a repository or data access layer that uses SQLite, use this document together with the [Roadbed.Data Architecture](architecture-roadbed-data.md) to scaffold the correct factory, executor calls, and retry configuration.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Never inject `IDataConnectionFactory` directly** — create a database-specific marker interface (e.g., `IFooDatabaseFactory : IDataConnectionFactory`).
5. **Use `SqliteExecutor` static methods** for all query execution — they handle connection lifecycle, retry logic, and logging.
6. **Always pass `CancellationToken`** as the last parameter with `= default`.
7. **Connections are returned open** — callers must dispose them with `using`.
8. **Use `ConfigureAwait(false)`** in library code (the executor already does this).
9. **Use `DataExecutorRequest`** to configure queries — never build retry loops manually.
10. **Use `KeepAlive()`** for in-memory database testing to prevent premature destruction.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-sqlite.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-sqlite.md#type-catalog)
3. [Package Relationship](architecture-roadbed-sqlite.md#package-relationship)
4. [SqliteConnectionFactory](architecture-roadbed-sqlite.md#sqliteconnectionfactory)
5. [SqliteExecutor](architecture-roadbed-sqlite.md#sqliteexecutor)
    - [Public Methods](architecture-roadbed-sqlite.md#public-methods)
    - [Method Signatures](architecture-roadbed-sqlite.md#method-signatures)
    - [Parameter Ordering Convention](architecture-roadbed-sqlite.md#parameter-ordering-convention)
    - [Retry Logic](architecture-roadbed-sqlite.md#retry-logic)
    - [Transient Error Detection](architecture-roadbed-sqlite.md#transient-error-detection)
    - [Logging Behavior](architecture-roadbed-sqlite.md#logging-behavior)
    - [Usage Examples](architecture-roadbed-sqlite.md#usage-examples)
6. [SqliteConnectionExtensions](architecture-roadbed-sqlite.md#sqliteconnectionextensions)
    - [KeepAlive Pattern](architecture-roadbed-sqlite.md#keepalive-pattern)
7. [Implementation Walkthrough](architecture-roadbed-sqlite.md#implementation-walkthrough)
8. [Common Pitfalls](architecture-roadbed-sqlite.md#common-pitfalls)

---

## Type Catalog

Roadbed.Data.Sqlite contains **3 public types**.

| Type                         | Kind         | Purpose                                                                                           |
| ---------------------------- | ------------ | ------------------------------------------------------------------------------------------------- |
| `SqliteConnectionFactory`    | Class        | Concrete `IDataConnectionFactory` that creates `Microsoft.Data.Sqlite.SqliteConnection` instances |
| `SqliteExecutor`             | Static class | Dapper-based query execution with built-in retry logic for transient SQLite errors                |
| `SqliteConnectionExtensions` | Static class | `KeepAlive()` extension method for in-memory database testing                                     |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Your Repository                                              │
│                                                              │
│   Uses: SqliteExecutor.QueryAsync<T>(request, factory, ...)  │
│   Uses: IFooDatabaseFactory (marker interface)               │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Data.Sqlite                                          │
│                                                              │
│   SqliteConnectionFactory  → creates SqliteConnection        │
│   SqliteExecutor           → Dapper + retry logic            │
│   SqliteConnectionExtensions → KeepAlive() for testing       │
└──────────┬───────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Data                                                 │
│                                                              │
│   IDataConnectionFactory                                     │
│   DataConnecionString                                        │
│   DataExecutorRequest                                        │
└──────────┬───────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────┐
│ External Dependencies                                        │
│                                                              │
│   Microsoft.Data.Sqlite                                      │
│   Dapper                                                     │
│   Microsoft.Extensions.Logging                               │
└──────────────────────────────────────────────────────────────┘
```

---

## SqliteConnectionFactory

Creates and opens `SqliteConnection` instances. See the [Roadbed.Data Architecture](architecture-roadbed-data.md) for the full factory pattern (marker interface, DI registration, consuming examples).

```csharp
namespace Roadbed.Data.Sqlite;

public class SqliteConnectionFactory : IDataConnectionFactory
{
    public SqliteConnectionFactory(DataConnecionString connectionString);

    public DataConnecionString Connecion { get; init; }

    // Both return an already-open connection. Caller must dispose.
    public IDbConnection CreateOpenConnection();
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

**Key behaviors:**

- Validates `connectionString` is not null
- Creates `Microsoft.Data.Sqlite.SqliteConnection` instances
- Opens the connection before returning
- Uses `ConfigureAwait(false)` in async path
- Throws `SqliteException` if the connection cannot be opened

---

## SqliteExecutor

The primary API for executing SQL against a SQLite database. All methods are static, accept a `DataExecutorRequest` and an `IDataConnectionFactory`, and handle the full connection lifecycle (open, execute, dispose) and optional retry logic internally.

### Public Methods

| Method                         | Returns                     | Dapper Method                  | Use Case                                           |
| ------------------------------ | --------------------------- | ------------------------------ | -------------------------------------------------- |
| `ExecuteAsync`                 | `Task<int>` (rows affected) | `ExecuteAsync`                 | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `Task<IEnumerable<T>>`      | `QueryAsync<T>`                | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `Task<T?>`                  | `QuerySingleOrDefaultAsync<T>` | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `Task<T?>`                  | `ExecuteScalarAsync<T>`        | SELECT returning a single value (COUNT, MAX, etc.) |

### Method Signatures

All four methods share the same parameter signature:

```csharp
public static async Task<TResult> MethodAsync<T>(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

| Parameter           | Required | Description                                     |
| ------------------- | -------- | ----------------------------------------------- |
| `request`           | Yes      | Query, parameters, and retry configuration      |
| `connectionFactory` | Yes      | Database-specific factory (marker interface)    |
| `logger`            | No       | Falls back to `NullLogger.Instance` if null     |
| `cancellationToken` | No       | Cancellation support (always last, `= default`) |

Both `request` and `connectionFactory` are validated with `ArgumentNullException.ThrowIfNull()`.

### Parameter Ordering Convention

The parameter order follows a consistent pattern across all four methods:

1. `DataExecutorRequest request` — the query and its configuration
2. `IDataConnectionFactory connectionFactory` — how to get a connection
3. `ILogger? logger = null` — optional diagnostics
4. `CancellationToken cancellationToken = default` — always last

### Retry Logic

When `request.RetriesEnabled` is `true` (the default), the executor catches transient `SqliteException` errors and retries automatically.

**Retry flow:**

```
Execute query
    │
    ├── Success → return result
    │
    ├── SqliteException (transient) AND attempts remaining
    │       │
    │       ├── Log warning with error code and delay
    │       ├── Wait (delay × attempt if multiplier enabled)
    │       └── Retry with new connection
    │
    ├── SqliteException (non-transient) → throw immediately
    │
    └── All retries exhausted
            │
            ├── Log error with last exception
            └── Throw InvalidOperationException (wraps last SqliteException)
```

**Key behaviors:**

- Each retry creates a **new connection** from the factory
- The delay is calculated as: `DelayBetweenRetries.TotalMilliseconds × attempt` (when `DelayMultiplierEnabled` is `true`)
- Non-transient errors are **not caught** — they propagate immediately
- After all retries are exhausted, throws `InvalidOperationException` with the last `SqliteException` as inner exception
- On successful retry, logs at `Information` level with the attempt number

**Default retry configuration (from `DataExecutorRequest`):**

| Property                 | Default |
| ------------------------ | ------- |
| `RetriesEnabled`         | `true`  |
| `MaxRetries`             | `3`     |
| `DelayBetweenRetries`    | `100ms` |
| `DelayMultiplierEnabled` | `true`  |

**Default backoff schedule:**

| Attempt   | Delay                             |
| --------- | --------------------------------- |
| 1         | 100ms                             |
| 2         | 200ms                             |
| 3         | 300ms                             |
| Exhausted | Throw `InvalidOperationException` |

### Transient Error Detection

The executor retries only specific SQLite error codes that represent temporary conditions:

| Error Code | Constant        | Meaning                                  | Retry? |
| ---------- | --------------- | ---------------------------------------- | ------ |
| 5          | `SQLITE_BUSY`   | Database is locked by another connection | Yes    |
| 6          | `SQLITE_LOCKED` | Table is locked                          | Yes    |
| 10         | `SQLITE_IOERR`  | Disk I/O error                           | Yes    |
| 13         | `SQLITE_FULL`   | Disk full                                | Yes    |
| Any other  | —               | Non-transient error                      | No     |

### Logging Behavior

The executor uses level-checked logging throughout:

| Event                     | Level       | Message Pattern                                                                               |
| ------------------------- | ----------- | --------------------------------------------------------------------------------------------- |
| Query execution start     | Debug       | `"Executing command: {Query}"` (truncated to 200 chars)                                       |
| Transient error, retrying | Warning     | `"Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms..."` |
| Successful after retry    | Information | `"Command succeeded on attempt {Attempt}. Rows affected: {Rows}"`                             |
| All retries exhausted     | Error       | `"Command failed after {Attempts} attempts"` (includes exception)                             |

All log messages check `logger.IsEnabled()` before formatting to avoid unnecessary string allocation.

### Usage Examples

#### Non-Query (INSERT, UPDATE, DELETE)

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name, description) VALUES (@Name, @Description)")
{
    Parameters = new { Name = "Bar", Description = "A bar item" },
};

int rowsAffected = await SqliteExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Multiple Rows

```csharp
var request = new DataExecutorRequest(
    @"SELECT f.id, f.name, f.description FROM foo AS f ORDER BY f.name ASC")
{
    RetriesEnabled = false,  // Read-only, no need for retries
};

IEnumerable<FooDto> results = await SqliteExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Single Row

```csharp
var request = new DataExecutorRequest(
    @"SELECT f.id, f.name, f.description FROM foo AS f WHERE f.id = @Id")
{
    Parameters = new { Id = id },
};

FooDto? result = await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Scalar Query

```csharp
var request = new DataExecutorRequest(
    @"SELECT COUNT(*) FROM foo AS f WHERE f.is_active = 1");

int? count = await SqliteExecutor.ExecuteScalarAsync<int>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Custom Retry Configuration

```csharp
var request = new DataExecutorRequest(
    @"UPDATE foo SET name = @Name WHERE id = @Id")
{
    Parameters = new { Id = id, Name = newName },
    MaxRetries = 5,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(200),
    DelayMultiplierEnabled = true,  // 200ms, 400ms, 600ms, 800ms, 1000ms
};

int rowsAffected = await SqliteExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

---

## SqliteConnectionExtensions

### KeepAlive Pattern

In-memory SQLite databases are destroyed when the last connection closes. The `KeepAlive()` extension method holds a connection open to prevent this.

```csharp
public static IDisposable KeepAlive(this SqliteConnection connection);
```

**Key behaviors:**

- Opens the connection if not already open
- Returns an `IDisposable` handle that **closes** (not disposes) the connection when disposed — the caller still owns the connection
- Throws `ArgumentNullException` if `connection` is null
- The internal `KeepAliveHandle` is `sealed` and tracks disposal state

**Usage (testing scenarios):**

```csharp
// Create an in-memory database factory
var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
{
    DatabaseSource = "FooTestDb",
};
var factory = new FooDatabaseFactory(connectionString);

// Open a connection and keep it alive
var connection = (SqliteConnection)factory.CreateOpenConnection();
using var keepAlive = connection.KeepAlive();

// Create schema
using (var setupConnection = factory.CreateOpenConnection())
{
    await setupConnection.ExecuteAsync("CREATE TABLE foo (id INTEGER PRIMARY KEY, name TEXT)");
}

// Run tests — the in-memory database persists because keepAlive holds a connection
using (var testConnection = factory.CreateOpenConnection())
{
    await testConnection.ExecuteAsync("INSERT INTO foo (name) VALUES ('Bar')");
    var result = await testConnection.QuerySingleOrDefaultAsync<FooDto>(
        "SELECT f.id, f.name FROM foo AS f WHERE f.name = 'Bar'");
}

// When keepAlive is disposed, the connection closes and the database is destroyed
```

**Why `Close()` instead of `Dispose()`?**

The `KeepAliveHandle` calls `Close()` on the connection, not `Dispose()`. The caller owns the `SqliteConnection` and is responsible for disposing it. The keep-alive handle only manages the open/close lifecycle to keep the in-memory database alive.

---

## Implementation Walkthrough

This walkthrough shows how to create a repository that uses `SqliteExecutor` for all database operations.

### Step 1: Define the DTO

```csharp
namespace Foo.Sdk;

using Newtonsoft.Json;

public sealed record FooDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    required public string Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }
}
```

### Step 2: Define the Database Factory

See [Roadbed.Data Architecture](architecture-roadbed-data.md) for the full factory pattern. Summary:

```csharp
// Marker interface
public interface IFooDatabaseFactory : IDataConnectionFactory { }

// Implementation
public class FooDatabaseFactory(DataConnecionString connection)
    : SqliteConnectionFactory(connection), IFooDatabaseFactory { }
```

### Step 3: Implement the Repository

```csharp
namespace Foo.Sdk;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Data;
using Roadbed.Data.Sqlite;
using Foo.Database;

internal sealed class FooRepository : BaseClassWithLogging
{
    private readonly IFooDatabaseFactory _connectionFactory;
    private readonly ILogger<FooRepository> _logger;

    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
        this._logger = logger;
    }

    public async Task<FooDto> CreateAsync(
        FooDto entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        this.LogDebug("Creating foo: {Name}", entity.Name);

        var request = new DataExecutorRequest(
            @"INSERT INTO foo (name, description)
              VALUES (@Name, @Description)
              RETURNING id, name, description")
        {
            Parameters = new { entity.Name, entity.Description },
        };

        var result = await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return result!;
    }

    public async Task<FooDto?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"SELECT
                 f.id
                ,f.name
                ,f.description
             FROM
                 foo AS f
             WHERE
                 f.id = @Id
             ;")
        {
            Parameters = new { Id = id },
            RetriesEnabled = false,
        };

        return await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    public async Task<FooDto> UpdateAsync(
        FooDto entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var request = new DataExecutorRequest(
            @"UPDATE foo
              SET
                   name = @Name
                  ,description = @Description
              WHERE
                  id = @Id
              ;")
        {
            Parameters = new { entity.Id, entity.Name, entity.Description },
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return entity;
    }

    public async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"DELETE FROM foo WHERE id = @Id;")
        {
            Parameters = new { Id = id },
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    public async Task<IList<FooDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"SELECT
                 f.id
                ,f.name
                ,f.description
             FROM
                 foo AS f
             ORDER BY
                 f.name ASC
             ;")
        {
            RetriesEnabled = false,
        };

        var results = await SqliteExecutor.QueryAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }
}
```

### Step 4: Write Tests with In-Memory Database

```csharp
[TestClass]
public class FooRepositoryTests
{
    #region Private Fields

    private IFooDatabaseFactory _factory = null!;
    private IDisposable _keepAlive = null!;
    private SqliteConnection _connection = null!;

    #endregion Private Fields

    #region Public Methods

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
        {
            DatabaseSource = "FooRepositoryTests",
        };

        this._factory = new FooDatabaseFactory(connectionString);
        this._connection = (SqliteConnection)this._factory.CreateOpenConnection();
        this._keepAlive = this._connection.KeepAlive();

        // Create schema
        using var setupConnection = this._factory.CreateOpenConnection();
        setupConnection.Execute(
            "CREATE TABLE foo (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, description TEXT)");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        this._keepAlive?.Dispose();
        this._connection?.Dispose();
    }

    /// <summary>
    /// Unit test to verify that CreateAsync inserts a row and returns the entity.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_ValidEntity_ReturnsEntityWithId()
    {
        // Arrange (Given)
        var logger = NullLogger<FooRepository>.Instance;
        var repository = new FooRepository(this._factory, logger);
        var entity = new FooDto { Name = "Bar", Description = "A bar item" };

        // Act (When)
        var result = await repository.CreateAsync(entity);

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "CreateAsync should return a non-null entity.");
        Assert.IsTrue(
            result.Id > 0,
            "Returned entity should have an assigned ID.");
        Assert.AreEqual(
            "Bar",
            result.Name,
            "Returned entity should have the correct name.");
    }

    #endregion Public Methods
}
```

---

## Common Pitfalls

### 1. Building Retry Loops Manually

```csharp
// ❌ Wrong — duplicates what SqliteExecutor already does
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        using var conn = await this._connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<FooDto>(sql, parameters);
    }
    catch (SqliteException)
    {
        await Task.Delay(100 * attempt);
    }
}

// ✅ Correct — let SqliteExecutor handle it
var request = new DataExecutorRequest(sql)
{
    Parameters = parameters,
};

return await SqliteExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 2. Forgetting to Pass the Logger

```csharp
// ❌ Wrong — no visibility into retry behavior
var result = await SqliteExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    cancellationToken: cancellationToken);

// ✅ Correct — retry warnings and errors are logged
var result = await SqliteExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 3. Missing KeepAlive in In-Memory Tests

```csharp
// ❌ Wrong — in-memory database destroyed between connections
var factory = new FooDatabaseFactory(inMemoryConnectionString);

using (var conn1 = factory.CreateOpenConnection())
{
    conn1.Execute("CREATE TABLE foo (id INTEGER PRIMARY KEY)");
}
// Database destroyed here!

using (var conn2 = factory.CreateOpenConnection())
{
    // SqliteException: no such table: foo
    conn2.Execute("INSERT INTO foo (id) VALUES (1)");
}

// ✅ Correct — KeepAlive prevents destruction
var factory = new FooDatabaseFactory(inMemoryConnectionString);
var connection = (SqliteConnection)factory.CreateOpenConnection();
using var keepAlive = connection.KeepAlive();

using (var conn1 = factory.CreateOpenConnection())
{
    conn1.Execute("CREATE TABLE foo (id INTEGER PRIMARY KEY)");
}

using (var conn2 = factory.CreateOpenConnection())
{
    conn2.Execute("INSERT INTO foo (id) VALUES (1)");  // Works
}
```

### 4. Using Direct Dapper Calls Instead of SqliteExecutor

```csharp
// ❌ Wrong — no retry logic, manual connection management
using var connection = await this._connectionFactory.CreateOpenConnectionAsync(cancellationToken);
var results = await connection.QueryAsync<FooDto>(sql, parameters);

// ✅ Correct — SqliteExecutor handles connection lifecycle and retries
var request = new DataExecutorRequest(sql) { Parameters = parameters };
var results = await SqliteExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 5. Enabling Retries for Read-Only Queries

```csharp
// ❌ Unnecessary — read-only queries on a local SQLite file rarely hit transient errors
var request = new DataExecutorRequest(
    "SELECT f.id, f.name FROM foo AS f WHERE f.id = @Id")
{
    Parameters = new { Id = id },
    // RetriesEnabled defaults to true — adds overhead for no benefit
};

// ✅ Better — disable retries for simple reads
var request = new DataExecutorRequest(
    "SELECT f.id, f.name FROM foo AS f WHERE f.id = @Id")
{
    Parameters = new { Id = id },
    RetriesEnabled = false,
};
```

### 6. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public FooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<FooRepository> logger)
    : base(logger)
{
    _connectionFactory = connectionFactory;  // Missing this.
    _logger = logger;                        // Missing this.
}

// ✅ Correct
public FooRepository(
    IFooDatabaseFactory connectionFactory,
    ILogger<FooRepository> logger)
    : base(logger)
{
    this._connectionFactory = connectionFactory;
    this._logger = logger;
}
```

### 7. Casting to SqliteConnection Without KeepAlive Context

```csharp
// ❌ Wrong — casting to SqliteConnection in production code
var connection = (SqliteConnection)await this._connectionFactory
    .CreateOpenConnectionAsync(cancellationToken);

// ✅ Correct — use IDbConnection in production (returned by factory)
using var connection = await this._connectionFactory
    .CreateOpenConnectionAsync(cancellationToken);

// ✅ Exception — casting is acceptable for KeepAlive in test setup
var connection = (SqliteConnection)this._factory.CreateOpenConnection();
using var keepAlive = connection.KeepAlive();
```

---

## Quick Reference

### SqliteExecutor Method Selection

```
What are you doing?
    │
    ├── INSERT / UPDATE / DELETE / DDL → SqliteExecutor.ExecuteAsync()
    │                                     Returns: int (rows affected)
    │
    ├── SELECT multiple rows          → SqliteExecutor.QueryAsync<T>()
    │                                     Returns: IEnumerable<T>
    │
    ├── SELECT single row             → SqliteExecutor.QuerySingleOrDefaultAsync<T>()
    │                                     Returns: T? (null if not found)
    │
    └── SELECT single value           → SqliteExecutor.ExecuteScalarAsync<T>()
                                          Returns: T? (COUNT, MAX, etc.)
```

### Transient SQLite Error Codes

| Code       | Constant        | Retried? |
| ---------- | --------------- | -------- |
| 5          | `SQLITE_BUSY`   | Yes      |
| 6          | `SQLITE_LOCKED` | Yes      |
| 10         | `SQLITE_IOERR`  | Yes      |
| 13         | `SQLITE_FULL`   | Yes      |
| All others | —               | No       |

### In-Memory Testing Checklist

- [ ] Use `DataConnectionStringType.SqliteInMemory`
- [ ] Set `DatabaseSource` to a unique name per test class
- [ ] Open a `SqliteConnection` from the factory
- [ ] Call `.KeepAlive()` on that connection and store the handle
- [ ] Create schema using a separate connection from the factory
- [ ] Run tests using connections from the same factory
- [ ] Dispose `keepAlive` then `connection` in `TestCleanup`


