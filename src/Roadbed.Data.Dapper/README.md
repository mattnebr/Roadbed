# Roadbed.Data.Dapper

A utility library for configuring Dapper to work with `[Column]` attributes from `System.ComponentModel.DataAnnotations.Schema` and providing SQLite type handlers for DateTime and DateTimeOffset.

## Overview

This library provides two main features:

1. **Column Mapping**: Configures Dapper to respect `[Column]` attributes, allowing you to map snake_case database columns (like `first_name`) to PascalCase properties (like `FirstName`) without SQL aliases.

2. **DateTime Type Handlers**: Provides type handlers for converting between SQLite TEXT columns and C# DateTime/DateTimeOffset types with proper timezone handling.

## Installation
```bash
dotnet add package Roadbed.Data.Dapper
```

## Quick Start

### 1. Define Your Data Transfer Object (DTO)
```csharp
using System.ComponentModel.DataAnnotations.Schema;

public class FooDto
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("first_name")]
    public string? FirstName { get; set; }
    
    [Column("last_name")]
    public string? LastName { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Configure at Startup
```csharp
using Dapper;
using Roadbed.Data;

// Configure column mappings
DapperMapping.Configure(typeof(FooDto));

// Register DateTime type handlers
SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
```

### 3. Use Clean SQL
```csharp
// ✅ No aliases needed
var query = "SELECT id, first_name, last_name, created_at FROM foo_table WHERE id = @Id";
var result = await connection.QuerySingleOrDefaultAsync<FooDto>(query, new { Id = 1 });
```

## DateTime Type Handlers

### Overview

SQLite stores DateTime values as TEXT, but Dapper doesn't automatically convert between SQLite TEXT and C# DateTime/DateTimeOffset. This library provides four type handlers:

| Handler | Type | Use Case |
|---------|------|----------|
| `DapperDateTimeHandler` | `DateTime` | UTC timestamps (recommended for most cases) |
| `DapperNullableDateTimeHandler` | `DateTime?` | Nullable UTC timestamps |
| `DapperDateTimeOffsetHandler` | `DateTimeOffset` | Timezone-aware timestamps |
| `DapperNullableDateTimeOffsetHandler` | `DateTimeOffset?` | Nullable timezone-aware timestamps |

### DateTime vs DateTimeOffset

**Use `DateTime` (with UTC)** for:
- Audit timestamps (`created_at`, `updated_at`)
- Event logs
- Most database timestamps
- When timezone doesn't matter

**Use `DateTimeOffset`** for:
- User appointments or scheduled events
- Times that must preserve timezone information
- Multi-timezone applications where the original timezone matters

### DateTime Handler Example
```csharp
// DTO
public class AuditLog
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }  // Always UTC
    
    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }  // Nullable UTC
}

// Usage
var log = new AuditLog
{
    CreatedAt = DateTime.UtcNow,  // ✅ Always use UtcNow
    ModifiedAt = null
};

// SQLite stores as: "2024-01-15 14:30:00"
// Retrieved as: DateTime with Kind = Utc
```

### DateTimeOffset Handler Example
```csharp
// DTO
public class Appointment
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("scheduled_time")]
    public DateTimeOffset ScheduledTime { get; set; }  // Preserves timezone
    
    [Column("cancelled_at")]
    public DateTimeOffset? CancelledAt { get; set; }  // Nullable with timezone
}

// Usage
var appointment = new Appointment
{
    ScheduledTime = DateTimeOffset.Now,  // Includes local timezone
    CancelledAt = null
};

// SQLite stores as: "2024-01-15 14:30:00-06:00"
// Retrieved as: DateTimeOffset with Offset = -06:00
```

### Storage Format

| Type | SQLite Format | Example |
|------|---------------|---------|
| `DateTime` | `yyyy-MM-dd HH:mm:ss` | `2024-01-15 14:30:00` |
| `DateTimeOffset` | `yyyy-MM-dd HH:mm:sszzz` | `2024-01-15 14:30:00-06:00` |

### Automatic Conversions

The handlers automatically:
- Convert non-UTC `DateTime` to UTC before storing
- Convert SQLite TEXT to UTC `DateTime` when reading
- Preserve timezone offsets for `DateTimeOffset`
- Use `CultureInfo.InvariantCulture` for consistent parsing across locales

## Features

- **Thread-safe**: Safe to call `Configure()` multiple times
- **Auto-discovery**: Configure all DTOs at once using reflection
- **Fallback**: Properties without `[Column]` use case-insensitive name matching
- **UTC enforcement**: DateTime handlers ensure all timestamps are UTC
- **Timezone preservation**: DateTimeOffset handlers preserve original timezone information

## Best Practices

### Column Mapping

1. **Configure once at startup** - not in repository constructors
2. **Use `[Column]` on all properties** - including `Id`
3. **Configure before using repositories** - in `Program.cs` or DI setup

### DateTime Handling

1. **Always use `DateTime.UtcNow`** for timestamps (not `DateTime.Now`)
2. **Use `DateTime` for most timestamps** - simpler and sufficient for most cases
3. **Use `DateTimeOffset` only when timezone matters** - appointments, scheduled events
4. **Register type handlers at startup** - before any database operations

## Example with Dependency Injection
```csharp
using Dapper;
using Roadbed.Data;

public static IServiceCollection AddDataAccess(this IServiceCollection services)
{
    // Configure column mappings
    DapperMapping.Configure(typeof(FooDto), typeof(BarDto));
    
    // Register DateTime type handlers (do this ONCE at startup)
    SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
    SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
    SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
    
    // Register repositories
    services.AddScoped<IFooRepository, FooRepository>();
    services.AddScoped<IBarRepository, BarRepository>();
    
    return services;
}
```

## SQLite Table Design
```sql
CREATE TABLE foo_table (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    
    -- DateTime fields (stored as TEXT in UTC)
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- DateTimeOffset fields (stored as TEXT with timezone)
    scheduled_time TEXT NULL,
    cancelled_at TEXT NULL
);
```

## Complete Example
```csharp
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Dapper;
using Roadbed.Data;

// DTO
public class UserDto
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [Column("last_login")]
    public DateTimeOffset? LastLogin { get; set; }
}

// Startup configuration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure Dapper mappings
        DapperMapping.Configure(typeof(UserDto));
        
        // Register type handlers
        SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new DapperDateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperNullableDateTimeOffsetHandler());
    }
}

// Repository usage
public class UserRepository
{
    public async Task<long> CreateAsync(UserDto user)
    {
        user.CreatedAt = DateTime.UtcNow;  // ✅ Use UTC
        
        var sql = @"
            INSERT INTO users (username, email, created_at, updated_at)
            VALUES (@Username, @Email, @CreatedAt, @UpdatedAt)
            RETURNING id";
            
        return await connection.ExecuteScalarAsync<long>(sql, user);
    }
    
    public async Task<UserDto> GetByIdAsync(long id)
    {
        var sql = @"
            SELECT id, username, email, created_at, updated_at, last_login
            FROM users
            WHERE id = @Id";
            
        return await connection.QuerySingleOrDefaultAsync<UserDto>(sql, new { Id = id });
    }
    
    public async Task UpdateLastLoginAsync(long id)
    {
        var sql = @"
            UPDATE users
            SET 
                last_login = @LastLogin,
                updated_at = @UpdatedAt
            WHERE id = @Id";
            
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            LastLogin = DateTimeOffset.Now,  // Preserves user's timezone
            UpdatedAt = DateTime.UtcNow
        });
    }
}
```

## Requirements

- .NET 10.0+
- Dapper
- System.ComponentModel.Annotations

## License

MIT