# Roadbed.Data

Core data access abstractions and utilities for building database-agnostic data layers.

## Overview

This library provides foundational types for working with databases in a consistent, testable way. It includes connection string management, execution request configuration, and retry logic support.

## Installation
```bash
dotnet add package Roadbed.Data
```

## Key Classes

### DataExecutorRequest

Encapsulates a database command with parameters and retry configuration. This is the primary class you'll work with when executing queries.

#### Basic Usage
```csharp
using Roadbed.Data;

// Simple query
var request = new DataExecutorRequest("SELECT * FROM foo_table");

// Query with parameters
var request = new DataExecutorRequest("SELECT * FROM foo_table WHERE id = @Id")
{
    Parameters = new { Id = 123 }
};
```

#### With Retry Configuration
```csharp
// Enable retries for transient errors
var request = new DataExecutorRequest("INSERT INTO foo_table (name) VALUES (@Name)")
{
    Parameters = new { Name = "Test" },
    RetriesEnabled = true,
    MaxRetries = 3,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(100)
};

// With exponential backoff
var request = new DataExecutorRequest(query)
{
    RetriesEnabled = true,
    MaxRetries = 5,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(50),
    DelayMultiplierEnabled = true  // 50ms, 100ms, 150ms, 200ms, 250ms
};
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Query` | `string` | (required) | SQL query or command to execute |
| `Parameters` | `object?` | `null` | Parameters for the query (Dapper-style) |
| `RetriesEnabled` | `bool` | `false` | Enable automatic retry logic |
| `MaxRetries` | `int` | `3` | Maximum number of retry attempts |
| `DelayBetweenRetries` | `TimeSpan` | `100ms` | Delay between retry attempts |
| `DelayMultiplierEnabled` | `bool` | `false` | Use exponential backoff (delay × attempt) |

### DataConnecionString

Type-safe connection string builder supporting multiple database types.
```csharp
using Roadbed.Data;

// SQLite in-memory database
var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
{
    DatabaseSource = "MyDatabase"
};

// SQLite file database
var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = @"C:\Data\myapp.db"
};
```

### IDataConnectionFactory

Interface for creating database connections. Implement this for your specific database type.
```csharp
public interface IDataConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
```

## Requirements

- .NET 10.0+
- System.Data.Common

## Related Packages

- **Roadbed.Data.Sqlite** - SQLite-specific implementations
- **Roadbed.Data.Dapper** - Dapper configuration utilities