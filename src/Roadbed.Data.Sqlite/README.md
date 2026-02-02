# Roadbed.Data.Sqlite

SQLite-specific implementations for the Roadbed data access framework, including connection management and query execution with automatic retry logic.

For the full type catalog, retry internals, and in-memory testing patterns, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-sqlite.md).

## Installation

```bash
dotnet add package Roadbed.Data.Sqlite
```

## Key Classes

### SqliteConnectionFactory

Creates and manages SQLite database connections. Implements `IDataConnectionFactory` from Roadbed.Data.

```csharp
using Roadbed.Data;
using Roadbed.Data.Sqlite;

// File-based database
var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = @"C:\Data\foo.db",
};
var factory = new SqliteConnectionFactory(connectionString);

// Connections are returned already open. Always dispose with 'using'.
using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
```

### SqliteExecutor

Executes SQLite commands via Dapper with built-in retry logic for transient errors.

#### Available Methods

| Method                         | Returns               | Use For                                            |
| ------------------------------ | --------------------- | -------------------------------------------------- |
| `ExecuteAsync`                 | `int` (rows affected) | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `IEnumerable<T>`      | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `T?`                  | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `T?`                  | SELECT returning a single value (COUNT, MAX, etc.) |

All methods share the same parameter signature:

```csharp
SqliteExecutor.MethodAsync(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

#### Transient Errors Handled Automatically

When retries are enabled (the default), these SQLite error codes are retried:

| Code | Constant        | Meaning            |
| ---- | --------------- | ------------------ |
| 5    | `SQLITE_BUSY`   | Database is locked |
| 6    | `SQLITE_LOCKED` | Table is locked    |
| 10   | `SQLITE_IOERR`  | Disk I/O error     |
| 13   | `SQLITE_FULL`   | Disk full          |

### SqliteConnectionExtensions

`KeepAlive()` extension method for in-memory database testing. Holds a connection open to prevent the in-memory database from being destroyed.

```csharp
var connection = (SqliteConnection)factory.CreateOpenConnection();
using var keepAlive = connection.KeepAlive();
// Database persists until keepAlive is disposed
```

See the [Architecture Document](/docs/architectural-design/architecture-roadbed-sqlite.md) for full testing patterns.

## Requirements

- .NET 10.0+
- Roadbed.Data
- Microsoft.Data.Sqlite
- Dapper

## Related Packages

- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Postgresql** - PostgreSQL-specific implementations
- **Roadbed.Data.Dapper** - Dapper configuration utilities

