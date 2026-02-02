# Roadbed.Data.Postgresql Architecture

Roadbed.Data.Postgresql provides the concrete PostgreSQL implementation of the Roadbed.Data abstractions. It includes a connection factory and a Dapper-based query executor with built-in retry logic for transient PostgreSQL errors.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Data.Postgresql NuGet package. When a developer asks you to create a repository or data access layer that uses PostgreSQL, use this document together with the [Roadbed.Data Architecture](architecture-roadbed-data.md) to scaffold the correct factory, executor calls, and retry configuration.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Never inject `IDataConnectionFactory` directly** — create a database-specific marker interface (e.g., `IFooDatabaseFactory : IDataConnectionFactory`).
5. **Use `PostgresqlExecutor` static methods** for all query execution — they handle connection lifecycle, retry logic, and logging.
6. **Always pass `CancellationToken`** as the last parameter with `= default`.
7. **Connections are returned open** — callers must dispose them with `using`.
8. **Use `ConfigureAwait(false)`** in library code (the executor already does this).
9. **Use `DataExecutorRequest`** to configure queries — never build retry loops manually.
10. **Use PostgreSQL-style parameter placeholders** (`@ParamName`) — Dapper handles the translation to Npgsql parameters.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-postgresql.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-postgresql.md#type-catalog)
3. [Package Relationship](architecture-roadbed-postgresql.md#package-relationship)
4. [PostgresqlConnectionFactory](architecture-roadbed-postgresql.md#postgresqlconnectionfactory)
5. [PostgresqlExecutor](architecture-roadbed-postgresql.md#postgresqlexecutor)
    - [Public Methods](architecture-roadbed-postgresql.md#public-methods)
    - [Method Signatures](architecture-roadbed-postgresql.md#method-signatures)
    - [Parameter Ordering Convention](architecture-roadbed-postgresql.md#parameter-ordering-convention)
    - [Retry Logic](architecture-roadbed-postgresql.md#retry-logic)
    - [Transient Error Detection](architecture-roadbed-postgresql.md#transient-error-detection)
    - [Logging Behavior](architecture-roadbed-postgresql.md#logging-behavior)
    - [Usage Examples](architecture-roadbed-postgresql.md#usage-examples)
6. [Implementation Walkthrough](architecture-roadbed-postgresql.md#implementation-walkthrough)
7. [Common Pitfalls](architecture-roadbed-postgresql.md#common-pitfalls)

---

## Type Catalog

Roadbed.Data.Postgresql contains **2 public types**.

|Type|Kind|Purpose|
|---|---|---|
|`PostgresqlConnectionFactory`|Class|Concrete `IDataConnectionFactory` that creates `Npgsql.NpgsqlConnection` instances|
|`PostgresqlExecutor`|Static class|Dapper-based query execution with built-in retry logic for transient PostgreSQL errors|

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────────┐
│ Your Repository                                                  │
│                                                                  │
│   Uses: PostgresqlExecutor.QueryAsync<T>(request, factory, ...)  │
│   Uses: IFooDatabaseFactory (marker interface)                   │
└──────────┬───────────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────────┐
│ Roadbed.Data.Postgresql                                          │
│                                                                  │
│   PostgresqlConnectionFactory  → creates NpgsqlConnection        │
│   PostgresqlExecutor           → Dapper + retry logic            │
└──────────┬───────────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────────┐
│ Roadbed.Data                                                     │
│                                                                  │
│   IDataConnectionFactory                                         │
│   DataConnecionString                                            │
│   DataExecutorRequest                                            │
└──────────┬───────────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────────┐
│ External Dependencies                                            │
│                                                                  │
│   Npgsql                                                         │
│   Dapper                                                         │
│   Microsoft.Extensions.Logging                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## PostgresqlConnectionFactory

Creates and opens `NpgsqlConnection` instances. See the [Roadbed.Data Architecture](architecture-roadbed-data.md) for the full factory pattern (marker interface, DI registration, consuming examples).

```csharp
namespace Roadbed.Data.Postgresql;

public class PostgresqlConnectionFactory : IDataConnectionFactory
{
    public PostgresqlConnectionFactory(DataConnecionString connectionString);

    public DataConnecionString Connecion { get; init; }

    // Both return an already-open connection. Caller must dispose.
    public IDbConnection CreateOpenConnection();
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

**Key behaviors:**

- Validates `connectionString` is not null
- Creates `Npgsql.NpgsqlConnection` instances
- Opens the connection before returning
- Uses `ConfigureAwait(false)` in async path
- Throws `NpgsqlException` if the connection cannot be opened

### Connection String Configuration

PostgreSQL connection strings can be configured using the template or a custom string:

#### Using the Template

```csharp
var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
{
    ServerName = "localhost",
    DatabaseSource = "mydb",
    Username = "admin",
    Password = "secret",
    TimeoutInSeconds = 30,
};

// Produces: Host=localhost;Database=mydb;Username=admin;Password=secret;Timeout=30;
```

#### Using a Custom Connection String

```csharp
var connectionString = new DataConnecionString(
    DataConnectionStringType.PostgreSQL,
    "Host=localhost;Port=5432;Database=mydb;Username=admin;Password=secret;Timeout=30;SSL Mode=Require");
```

Use the custom connection string approach when you need parameters not covered by the template, such as `Port`, `SSL Mode`, `Pooling`, `Maximum Pool Size`, or `Search Path`.

---

## PostgresqlExecutor

The primary API for executing SQL against a PostgreSQL database. All methods are static, accept a `DataExecutorRequest` and an `IDataConnectionFactory`, and handle the full connection lifecycle (open, execute, dispose) and optional retry logic internally.

### Public Methods

|Method|Returns|Dapper Method|Use Case|
|---|---|---|---|
|`ExecuteAsync`|`Task<int>` (rows affected)|`ExecuteAsync`|INSERT, UPDATE, DELETE, DDL|
|`QueryAsync<T>`|`Task<IEnumerable<T>>`|`QueryAsync<T>`|SELECT returning multiple rows|
|`QuerySingleOrDefaultAsync<T>`|`Task<T?>`|`QuerySingleOrDefaultAsync<T>`|SELECT returning zero or one row|
|`ExecuteScalarAsync<T>`|`Task<T?>`|`ExecuteScalarAsync<T>`|SELECT returning a single value (COUNT, MAX, etc.)|

### Method Signatures

All four methods share the same parameter signature:

```csharp
public static async Task<TResult> MethodAsync<T>(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

|Parameter|Required|Description|
|---|---|---|
|`request`|Yes|Query, parameters, and retry configuration|
|`connectionFactory`|Yes|Database-specific factory (marker interface)|
|`logger`|No|Falls back to `NullLogger.Instance` if null|
|`cancellationToken`|No|Cancellation support (always last, `= default`)|

Both `request` and `connectionFactory` are validated with `ArgumentNullException.ThrowIfNull()`.

### Parameter Ordering Convention

The parameter order follows a consistent pattern across all four methods:

1. `DataExecutorRequest request` — the query and its configuration
2. `IDataConnectionFactory connectionFactory` — how to get a connection
3. `ILogger? logger = null` — optional diagnostics
4. `CancellationToken cancellationToken = default` — always last

### Retry Logic

When `request.RetriesEnabled` is `true` (the default), the executor catches transient `PostgresException` errors and retries automatically.

**Retry flow:**

```
Execute query
    │
    ├── Success → return result
    │
    ├── PostgresException (transient) AND attempts remaining
    │       │
    │       ├── Log warning with SQLSTATE code and delay
    │       ├── Wait (delay × attempt if multiplier enabled)
    │       └── Retry with new connection
    │
    ├── PostgresException (non-transient) → throw immediately
    │
    └── All retries exhausted
            │
            ├── Log error with last exception
            └── Throw InvalidOperationException (wraps last PostgresException)
```

**Key behaviors:**

- Each retry creates a **new connection** from the factory
- The delay is calculated as: `DelayBetweenRetries.TotalMilliseconds × attempt` (when `DelayMultiplierEnabled` is `true`)
- Non-transient errors are **not caught** — they propagate immediately
- After all retries are exhausted, throws `InvalidOperationException` with the last `PostgresException` as inner exception
- On successful retry, logs at `Information` level with the attempt number

**Default retry configuration (from `DataExecutorRequest`):**

|Property|Default|
|---|---|
|`RetriesEnabled`|`true`|
|`MaxRetries`|`3`|
|`DelayBetweenRetries`|`100ms`|
|`DelayMultiplierEnabled`|`true`|

**Default backoff schedule:**

|Attempt|Delay|
|---|---|
|1|100ms|
|2|200ms|
|3|300ms|
|Exhausted|Throw `InvalidOperationException`|

### Transient Error Detection

The executor retries only specific PostgreSQL SQLSTATE codes that represent temporary conditions. PostgreSQL uses 5-character string codes defined in the SQL standard.

#### Class 08 — Connection Exception

|SQLSTATE|Constant|Meaning|Retry?|
|---|---|---|---|
|`08000`|`connection_exception`|General connection error|Yes|
|`08001`|`sqlclient_unable_to_establish_sqlconnection`|Cannot establish connection|Yes|
|`08003`|`connection_does_not_exist`|Connection lost|Yes|
|`08004`|`sqlserver_rejected_establishment_of_sqlconnection`|Server rejected connection|Yes|
|`08006`|`connection_failure`|Connection failure during transaction|Yes|

#### Class 40 — Transaction Rollback

|SQLSTATE|Constant|Meaning|Retry?|
|---|---|---|---|
|`40001`|`serialization_failure`|Concurrent transaction conflict|Yes|
|`40P01`|`deadlock_detected`|Two transactions deadlocked|Yes|

#### Class 53 — Insufficient Resources

|SQLSTATE|Constant|Meaning|Retry?|
|---|---|---|---|
|`53000`|`insufficient_resources`|General resource exhaustion|Yes|
|`53100`|`disk_full`|Disk full|Yes|
|`53200`|`out_of_memory`|Server out of memory|Yes|
|`53300`|`too_many_connections`|Connection limit reached|Yes|

#### Class 57 — Operator Intervention

|SQLSTATE|Constant|Meaning|Retry?|
|---|---|---|---|
|`57P01`|`admin_shutdown`|Administrator shut down server|Yes|
|`57P02`|`crash_shutdown`|Server crashed|Yes|
|`57P03`|`cannot_connect_now`|Server starting up|Yes|

#### Class 58 — System Error

|SQLSTATE|Constant|Meaning|Retry?|
|---|---|---|---|
|`58000`|`system_error`|General system error|Yes|
|`58030`|`io_error`|Disk I/O error|Yes|

#### Non-Transient (Never Retried)

All SQLSTATE codes not listed above are considered non-transient and propagate immediately. Common examples:

|SQLSTATE|Constant|Meaning|
|---|---|---|
|`23505`|`unique_violation`|Duplicate key|
|`23503`|`foreign_key_violation`|Foreign key constraint|
|`42P01`|`undefined_table`|Table does not exist|
|`42601`|`syntax_error`|SQL syntax error|

### Logging Behavior

The executor uses level-checked logging throughout:

|Event|Level|Message Pattern|
|---|---|---|
|Query execution start|Debug|`"Executing command: {Query}"` (truncated to 200 chars)|
|Transient error, retrying|Warning|`"Transient error on attempt {Attempt}: {SqlState} - {Message}. Retrying in {DelayMs}ms..."`|
|Successful after retry|Information|`"Command succeeded on attempt {Attempt}. Rows affected: {Rows}"`|
|All retries exhausted|Error|`"Command failed after {Attempts} attempts"` (includes exception)|

All log messages check `logger.IsEnabled()` before formatting to avoid unnecessary string allocation.

### Usage Examples

#### Non-Query (INSERT, UPDATE, DELETE)

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name, description) VALUES (@Name, @Description)")
{
    Parameters = new { Name = "Bar", Description = "A bar item" },
};

int rowsAffected = await PostgresqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Multiple Rows

```csharp
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
    RetriesEnabled = false,  // Read-only, consider disabling retries
};

IEnumerable<FooDto> results = await PostgresqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Single Row

```csharp
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
};

FooDto? result = await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Scalar Query

```csharp
var request = new DataExecutorRequest(
    @"SELECT COUNT(*) FROM foo AS f WHERE f.is_active = true");

int? count = await PostgresqlExecutor.ExecuteScalarAsync<int>(
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

int rowsAffected = await PostgresqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### INSERT with RETURNING

PostgreSQL supports `RETURNING` to get inserted data back in a single round trip:

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name, description)
      VALUES (@Name, @Description)
      RETURNING
           id
          ,name
          ,description
      ;")
{
    Parameters = new { entity.Name, entity.Description },
};

FooDto? result = await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### UPSERT with ON CONFLICT

PostgreSQL supports `ON CONFLICT` for atomic upsert operations:

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (external_id, name, description)
      VALUES (@ExternalId, @Name, @Description)
      ON CONFLICT (external_id)
      DO UPDATE SET
           name = EXCLUDED.name
          ,description = EXCLUDED.description
      RETURNING
           id
          ,external_id
          ,name
          ,description
      ;")
{
    Parameters = new { entity.ExternalId, entity.Name, entity.Description },
};

FooDto? result = await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

---

## Implementation Walkthrough

This walkthrough shows how to create a repository that uses `PostgresqlExecutor` for all database operations.

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
    : PostgresqlConnectionFactory(connection), IFooDatabaseFactory { }
```

### Step 3: Implement the Repository

```csharp
namespace Foo.Sdk;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Data;
using Roadbed.Data.Postgresql;
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
              RETURNING
                   id
                  ,name
                  ,description
              ;")
        {
            Parameters = new { entity.Name, entity.Description },
        };

        var result = await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
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

        return await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
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

        await PostgresqlExecutor.ExecuteAsync(
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

        await PostgresqlExecutor.ExecuteAsync(
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

        var results = await PostgresqlExecutor.QueryAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }
}
```

### Step 4: Register Services

```csharp
public class InstallFooModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("FooDatabase")
            ?? throw new InvalidOperationException("FooDatabase connection string is required.");

        var dataConnectionString = new DataConnecionString(
            DataConnectionStringType.PostgreSQL,
            connectionString);

        services.AddSingleton(dataConnectionString);
        services.AddSingleton<IFooDatabaseFactory, FooDatabaseFactory>();
        services.AddScoped<FooRepository>();
    }
}
```

---

## Common Pitfalls

### 1. Building Retry Loops Manually

```csharp
// ❌ Wrong — duplicates what PostgresqlExecutor already does
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        using var conn = await this._connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<FooDto>(sql, parameters);
    }
    catch (PostgresException)
    {
        await Task.Delay(100 * attempt);
    }
}

// ✅ Correct — let PostgresqlExecutor handle it
var request = new DataExecutorRequest(sql)
{
    Parameters = parameters,
};

return await PostgresqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 2. Forgetting to Pass the Logger

```csharp
// ❌ Wrong — no visibility into retry behavior
var result = await PostgresqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    cancellationToken: cancellationToken);

// ✅ Correct — retry warnings and errors are logged
var result = await PostgresqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 3. Using Direct Dapper Calls Instead of PostgresqlExecutor

```csharp
// ❌ Wrong — no retry logic, manual connection management
using var connection = await this._connectionFactory.CreateOpenConnectionAsync(cancellationToken);
var results = await connection.QueryAsync<FooDto>(sql, parameters);

// ✅ Correct — PostgresqlExecutor handles connection lifecycle and retries
var request = new DataExecutorRequest(sql) { Parameters = parameters };
var results = await PostgresqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 4. Using SQLite Boolean Syntax

```csharp
// ❌ Wrong — SQLite uses 1/0 for booleans
var request = new DataExecutorRequest(
    "SELECT f.id FROM foo AS f WHERE f.is_active = 1");

// ✅ Correct — PostgreSQL uses native boolean type
var request = new DataExecutorRequest(
    "SELECT f.id FROM foo AS f WHERE f.is_active = true");
```

### 5. Missing `this.` on Instance Members

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

### 6. Using AUTOINCREMENT Instead of SERIAL/GENERATED

```csharp
// ❌ Wrong — SQLite syntax
var request = new DataExecutorRequest(
    "CREATE TABLE foo (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT)");

// ✅ Correct — PostgreSQL uses SERIAL or GENERATED
var request = new DataExecutorRequest(
    "CREATE TABLE foo (id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY, name TEXT NOT NULL)");

// ✅ Also correct — SERIAL shorthand
var request = new DataExecutorRequest(
    "CREATE TABLE foo (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL)");
```

### 7. Using RETURNING Without QuerySingleOrDefaultAsync

```csharp
// ❌ Wrong — ExecuteAsync discards RETURNING data
var request = new DataExecutorRequest(
    "INSERT INTO foo (name) VALUES (@Name) RETURNING id, name")
{
    Parameters = new { Name = "Bar" },
};

int rowsAffected = await PostgresqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
// rowsAffected = 1, but the RETURNING data is lost

// ✅ Correct — use QuerySingleOrDefaultAsync to capture RETURNING data
FooDto? result = await PostgresqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 8. Hardcoding Connection Strings

```csharp
// ❌ Wrong — hardcoded credentials in source code
var connectionString = new DataConnecionString(
    DataConnectionStringType.PostgreSQL,
    "Host=prod-server;Database=mydb;Username=admin;Password=secret");

// ✅ Correct — read from configuration
string connectionString = configuration.GetConnectionString("FooDatabase")
    ?? throw new InvalidOperationException("FooDatabase connection string is required.");

var dataConnectionString = new DataConnecionString(
    DataConnectionStringType.PostgreSQL,
    connectionString);
```

---

## Differences from Roadbed.Data.Sqlite

When migrating from SQLite to PostgreSQL, be aware of these key differences:

|Feature|SQLite (`Roadbed.Data.Sqlite`)|PostgreSQL (`Roadbed.Data.Postgresql`)|
|---|---|---|
|Connection type|`SqliteConnection`|`NpgsqlConnection`|
|Exception type|`SqliteException`|`PostgresException`|
|Error codes|Integer (`5`, `6`, `10`, `13`)|SQLSTATE strings (`"08006"`, `"40001"`)|
|Error code property|`ex.SqliteErrorCode`|`ex.SqlState`|
|Boolean type|`INTEGER` (`1`/`0`)|`BOOLEAN` (`true`/`false`)|
|Auto-increment|`AUTOINCREMENT`|`GENERATED ALWAYS AS IDENTITY` or `SERIAL`|
|In-memory testing|`KeepAlive()` pattern|Use Docker or test database instance|
|Upsert syntax|`INSERT OR REPLACE`|`INSERT ... ON CONFLICT ... DO UPDATE`|
|Type system|Dynamic (5 storage classes)|Static (rich type system)|
|Concurrency|File-level locking|MVCC with row-level locking|
|Transient error count|4 error codes|16 SQLSTATE codes across 5 error classes|
|Connection string type|`DataConnectionStringType.SQLite`|`DataConnectionStringType.PostgreSQL`|
|NuGet dependency|`Microsoft.Data.Sqlite`|`Npgsql`|

---

## Quick Reference

### PostgresqlExecutor Method Selection

```
What are you doing?
    │
    ├── INSERT / UPDATE / DELETE / DDL → PostgresqlExecutor.ExecuteAsync()
    │                                     Returns: int (rows affected)
    │
    ├── INSERT with RETURNING          → PostgresqlExecutor.QuerySingleOrDefaultAsync<T>()
    │                                     Returns: T? (the inserted row)
    │
    ├── SELECT multiple rows           → PostgresqlExecutor.QueryAsync<T>()
    │                                     Returns: IEnumerable<T>
    │
    ├── SELECT single row              → PostgresqlExecutor.QuerySingleOrDefaultAsync<T>()
    │                                     Returns: T? (null if not found)
    │
    └── SELECT single value            → PostgresqlExecutor.ExecuteScalarAsync<T>()
                                           Returns: T? (COUNT, MAX, etc.)
```

### Transient PostgreSQL SQLSTATE Codes

|Class|Category|Codes|
|---|---|---|
|08|Connection Exception|`08000`, `08001`, `08003`, `08004`, `08006`|
|40|Transaction Rollback|`40001`, `40P01`|
|53|Insufficient Resources|`53000`, `53100`, `53200`, `53300`|
|57|Operator Intervention|`57P01`, `57P02`, `57P03`|
|58|System Error|`58000`, `58030`|

### Integration Testing Checklist

Unlike SQLite, PostgreSQL has no in-memory mode. Integration tests require a running database instance.

- [ ] Use a dedicated test database (e.g., `testdb` or `foodb_test`)
- [ ] Configure connection string via environment variable or `appsettings.Test.json`
- [ ] Run schema migrations in `[TestInitialize]` or `[ClassInitialize]`
- [ ] Use transactions with rollback for test isolation
- [ ] Consider Docker (`docker run -d -p 5432:5432 postgres:17`) for CI/CD
- [ ] Consider GitHub Actions `services:` block for automated pipelines