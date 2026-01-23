# Roadbed.Data.Sqlite

SQLite-specific implementations for the Roadbed data access framework, including connection management and query execution with automatic retry logic.

## Installation
```bash
dotnet add package Roadbed.Data.Sqlite
```

## Key Classes

### SqliteConnectionFactory

Creates and manages SQLite database connections. Implements `IDataConnectionFactory` from Roadbed.Data.

#### Basic Usage
```csharp
using Roadbed.Data;
using Roadbed.Data.Sqlite;

// File-based database
var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = @"C:\Data\myapp.db"
};
var factory = new SqliteConnectionFactory(connectionString);
```

#### Creating Connections
```csharp
using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
```

**Note**: Connections are returned already open. Always dispose them using `using` statements.

### SqliteExecutor

Executes SQLite commands with built-in retry logic for transient errors (database locked, I/O errors, disk full).

#### Available Methods

| Method | Returns | Use For |
|--------|---------|---------|
| `ExecuteAsync` | `int` (rows affected) | INSERT, UPDATE, DELETE |
| `ExecuteScalarAsync<T>` | `T` (single value) | INSERT with RETURNING, COUNT, MAX, etc. |
| `QueryAsync<T>` | `IEnumerable<T>` | SELECT returning multiple rows |
| `QuerySingleOrDefaultAsync<T>` | `T?` | SELECT returning 0 or 1 row |

#### Basic Queries
```csharp
using Roadbed.Data;
using Roadbed.Data.Sqlite;

// SELECT multiple rows
var request = new DataExecutorRequest("SELECT * FROM foo_table");
var results = await SqliteExecutor.QueryAsync<FooDto>(
    request,
    connectionFactory,
    logger,
    cancellationToken);

// SELECT single row
var request = new DataExecutorRequest("SELECT * FROM foo_table WHERE id = @Id")
{
    Parameters = new { Id = 123 }
};
var result = await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    connectionFactory,
    logger,
    cancellationToken);
```

#### With Retry Logic
```csharp
var request = new DataExecutorRequest("INSERT INTO foo_table (name) VALUES (@Name)")
{
    Parameters = new { Name = "Test" },
    RetriesEnabled = true,
    MaxRetries = 3,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(100),
    DelayMultiplierEnabled = false
};

int rowsAffected = await SqliteExecutor.ExecuteAsync(
    request,
    connectionFactory,
    logger,
    cancellationToken);
```

#### Transient Errors Handled Automatically

SqliteExecutor automatically retries these SQLite error codes when retries are enabled:

- **5 (SQLITE_BUSY)**: Database is locked
- **6 (SQLITE_LOCKED)**: Table is locked
- **10 (SQLITE_IOERR)**: Disk I/O error
- **13 (SQLITE_FULL)**: Disk full

## Complete Repository Example
```csharp
using Roadbed.Data;
using Roadbed.Data.Sqlite;
using Microsoft.Extensions.Logging;

public class FooRepository
{
    private readonly IDataConnectionFactory _connectionFactory;
    private readonly ILogger<FooRepository> _logger;

    public FooRepository(
        IDataConnectionFactory connectionFactory,
        ILogger<FooRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<FooDto?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            "SELECT id, name, description FROM foo_table WHERE id = @Id")
        {
            Parameters = new { Id = id }
        };

        return await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
            request,
            _connectionFactory,
            _logger,
            cancellationToken);
    }

    public async Task<long> CreateAsync(
        FooDto dto,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(@"
            INSERT INTO foo_table (name, description)
            VALUES (@Name, @Description);
            SELECT last_insert_rowid();
        ")
        {
            Parameters = new { dto.Name, dto.Description },
            RetriesEnabled = true,
            MaxRetries = 3
        };

        return await SqliteExecutor.ExecuteScalarAsync<long>(
            request,
            _connectionFactory,
            _logger,
            cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        FooDto dto,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(@"
            UPDATE foo_table
            SET name = @Name, description = @Description
            WHERE id = @Id
        ")
        {
            Parameters = new { dto.Id, dto.Name, dto.Description },
            RetriesEnabled = true,
            MaxRetries = 3
        };

        int rowsAffected = await SqliteExecutor.ExecuteAsync(
            request,
            _connectionFactory,
            _logger,
            cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            "DELETE FROM foo_table WHERE id = @Id")
        {
            Parameters = new { Id = id },
            RetriesEnabled = true,
            MaxRetries = 3
        };

        int rowsAffected = await SqliteExecutor.ExecuteAsync(
            request,
            _connectionFactory,
            _logger,
            cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<IList<FooDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            "SELECT id, name, description FROM foo_table ORDER BY id DESC");

        var results = await SqliteExecutor.QueryAsync<FooDto>(
            request,
            _connectionFactory,
            _logger,
            cancellationToken);

        return results.ToList();
    }
}
```

## Requirements

- .NET 10.0+
- Roadbed.Data
- Microsoft.Data.Sqlite
- Dapper

## Related Packages

- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Dapper** - Dapper configuration utilities