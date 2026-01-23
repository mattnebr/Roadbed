# Roadbed.Data.Dapper

A utility library for configuring Dapper to work with `[Column]` attributes from `System.ComponentModel.DataAnnotations.Schema`.

## Overview

This library configures Dapper to respect `[Column]` attributes, allowing you to map snake_case database columns (like `first_name`) to PascalCase properties (like `FirstName`) without SQL aliases.

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
}
```

### 2. Configure at Startup
```csharp
using Roadbed.Data;

// Single type
DapperMapping.Configure(typeof(FooDto));

// Multiple types
DapperMapping.Configure(typeof(FooDto), typeof(BarDto));
```

### 3. Use Clean SQL
```csharp
// ✅ No aliases needed
var query = "SELECT id, first_name, last_name FROM foo_table WHERE id = @Id";
var result = await connection.QuerySingleOrDefaultAsync<FooDto>(query, new { Id = 1 });
```

## Features

- **Thread-safe**: Safe to call `Configure()` multiple times
- **Auto-discovery**: Configure all DTOs at once using reflection
- **Fallback**: Properties without `[Column]` use case-insensitive name matching

## Best Practices

1. **Configure once at startup** - not in repository constructors
2. **Use `[Column]` on all properties** - including `Id`
3. **Configure before using repositories** - in `Program.cs` or DI setup

## Example with Dependency Injection
```csharp
public static IServiceCollection AddDataAccess(this IServiceCollection services)
{
    DapperMapping.Configure(typeof(FooDto), typeof(BarDto));
    services.AddScoped<IFooRepository, FooRepository>();
    return services;
}
```

## Requirements

- .NET 10.0+
- Dapper
- System.ComponentModel.Annotations