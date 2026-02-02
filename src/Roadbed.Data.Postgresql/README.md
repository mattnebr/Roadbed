# Roadbed.Data.Postgresql

PostgreSQL-specific implementations for the Roadbed data access framework, including connection management and query execution with automatic retry logic.

For the full type catalog, retry internals, and in-memory testing patterns, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-postgresql.md).

## Installation

```bash
dotnet add package Roadbed.Data.Postgresql
```

## Key Classes

### PostgresqlConnectionFactory

Creates and manages PostgreSQL database connections. Implements `IDataConnectionFactory` from Roadbed.Data.

```csharp
using Roadbed.Data;
using Roadbed.Data.Postgresql;

// Using the connection string template
var connectionString = new DataConnecionString(DataConnectionStringType.PostgreSQL)
{
    ServerName = "localhost",
    DatabaseSource = "mydb",
    Username = "admin",
    Password = "secret",
    TimeoutInSeconds = 30,
};
var factory = new PostgresqlConnectionFactory(connectionString);

// Or using a custom connection string for advanced options
var connectionString = new DataConnecionString(
    DataConnectionStringType.PostgreSQL,
    "Host=localhost;Port=5432;Database=mydb;Username=admin;Password=secret;Timeout=30;SSL Mode=Require");
var factory = new PostgresqlConnectionFactory(connectionString);

// Connections are returned already open. Always dispose with 'using'.
using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
```

### PostgresqlExecutor

Executes PostgreSQL commands via Dapper with built-in retry logic for transient errors.

#### Available Methods

| Method                         | Returns               | Use For                                            |
| ------------------------------ | --------------------- | -------------------------------------------------- |
| `ExecuteAsync`                 | `int` (rows affected) | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `IEnumerable<T>`      | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `T?`                  | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `T?`                  | SELECT returning a single value (COUNT, MAX, etc.) |

All methods share the same parameter signature:

```csharp
PostgresqlExecutor.MethodAsync(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

#### Transient Errors Handled Automatically

When retries are enabled (the default), these PostgreSQL SQLSTATE codes are retried:

|Class|Category|Codes|
|---|---|---|
|08|Connection Exception|`08000`, `08001`, `08003`, `08004`, `08006`|
|40|Transaction Rollback|`40001`, `40P01`|
|53|Insufficient Resources|`53000`, `53100`, `53200`, `53300`|
|57|Operator Intervention|`57P01`, `57P02`, `57P03`|
|58|System Error|`58000`, `58030`|

See the [Architecture Document](/docs/architectural-design/architecture-roadbed-postgresql.md) for the full list with descriptions.
## Requirements

- .NET 10.0+
- Roadbed.Data
- Npgsql
- Dapper

## Related Packages

- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Sqlite** - SQLite-specific implementations
- **Roadbed.Data.Dapper** - Dapper configuration utilities

