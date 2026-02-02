# Roadbed.Data.Dapper

Dapper configuration utilities for SQLite databases, providing column-to-property mapping via `[Column]` attributes and type handlers for DateTime/DateTimeOffset conversion.

For the full type catalog, type handler internals, and integration with Roadbed.Crud, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-dapper.md).

## Installation

```bash
dotnet add package Roadbed.Data.Dapper
```

## Quick Start

### 1. Define Your Entity

Database entities inherit from `BaseEntityClass<TId>` (from Roadbed.Crud) and use `[Column]` attributes to map snake_case database columns to PascalCase properties.

```csharp
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

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Configure in Your Installer

```csharp
using Dapper;
using Roadbed;
using Roadbed.Crud;
using Roadbed.Data;

public sealed class InstallFooDatabase : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Discover all entity types implementing IEntity<>
        Type[] entityTypes = typeof(DbFoo).Assembly
            .GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IEntity<>)))
            .ToArray();

        // Configure Dapper column mappings and type handlers
        DapperMapping.Configure(entityTypes);
        SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());

        // Register repositories (internal) and services (public)
        services.AddScoped<IDbFooRepository, DbFooRepository>();
        services.AddScoped<IDbFooService, DbFooService>();

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 3. Use Clean SQL in Repositories

The `[Column]` attributes handle the mapping — no SQL aliases needed for column names that differ from property names.

```csharp
var request = new DataExecutorRequest(
    @"SELECT
         f.id
        ,f.first_name
        ,f.last_name
        ,f.created_at
        ,f.updated_at
     FROM
         foo AS f
     WHERE
         f.id = @Id
     ;")
{
    Parameters = new { Id = 1 },
    RetriesEnabled = false,
};

var result = await SqliteExecutor.QuerySingleOrDefaultAsync<DbFoo>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

## DateTime Type Handlers

SQLite stores DateTime values as TEXT. These handlers convert between SQLite TEXT and C# temporal types automatically.

|Handler|Type|Storage Format|Use Case|
|---|---|---|---|
|`DapperDateTimeHandler`|`DateTime`|`yyyy-MM-dd HH:mm:ss`|UTC timestamps|
|`DapperNullableDateTimeHandler`|`DateTime?`|`yyyy-MM-dd HH:mm:ss` or NULL|Nullable UTC timestamps|
|`DapperDateTimeOffsetHandler`|`DateTimeOffset`|`yyyy-MM-dd HH:mm:sszzz`|Timezone-aware timestamps|
|`DapperNullableDateTimeOffsetHandler`|`DateTimeOffset?`|`yyyy-MM-dd HH:mm:sszzz` or NULL|Nullable timezone-aware timestamps|

### When to Use Which

**Use `DateTime` (UTC)** for system timestamps — `created_at`, `updated_at`, event logs, and most database columns where timezone doesn't matter.

**Use `DateTimeOffset`** for user-facing times — appointments, scheduled events, and any time that must preserve the original timezone.

### Automatic Conversions

The handlers automatically convert non-UTC `DateTime` to UTC before storage, convert SQLite TEXT to UTC `DateTime` when reading, preserve timezone offsets for `DateTimeOffset`, and use `CultureInfo.InvariantCulture` for consistent parsing.

## Column Mapping

`DapperMapping` configures Dapper to use `[Column]` attributes for property mapping. It is thread-safe, idempotent, and falls back to case-insensitive property name matching when no `[Column]` attribute is found.

```csharp
// Preferred: IEntity<> interface scanning (discovers all entities automatically)
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

## SQLite Table Design

```sql
CREATE TABLE foo (
     id INTEGER PRIMARY KEY AUTOINCREMENT
    ,first_name TEXT NOT NULL
    ,last_name TEXT NOT NULL
    ,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
    ,updated_at TEXT NULL
    ,scheduled_time TEXT NULL
    ,cancelled_at TEXT NULL
)
;
```

## Requirements

- .NET 10.0+
- Roadbed.Crud
- Roadbed.Data
- Dapper
- System.ComponentModel.Annotations

## Related Packages

- **Roadbed.Crud** - Entity types, repository/service patterns
- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Sqlite** - SQLite connection factory and executor