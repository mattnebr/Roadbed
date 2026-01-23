# Roadbed.Crud

Base classes and interfaces for implementing the Repository and Entity patterns with CRUD operations (Create, Read, Update, Delete, List).

## Overview

This library provides a structured approach to data access using the Repository pattern with Entity wrappers. It includes interfaces for repositories, base classes for entities, and DTOs with built-in error tracking.

## Installation
```bash
dotnet add package Roadbed.Crud
```

## Architecture
```
┌─────────────┐
│   Entity    │  ← Business logic layer (your app)
└──────┬──────┘
       │ uses
┌──────▼──────┐
│ Repository  │  ← Data access layer
└──────┬──────┘
       │ returns
┌──────▼──────┐
│     DTO     │  ← Data transfer objects
└─────────────┘
```

## Key Components

### DTOs (Data Transfer Objects)

#### IDataTransferObject\<TId\>
```csharp
public interface IDataTransferObject<TIdDataType>
{
    TIdDataType? Id { get; }
    List<string>? Errors { get; }
}
```

#### BaseDataTransferObject\<TId\>

Base implementation with built-in `[Column("id")]` and `[JsonProperty("id")]` attributes:
```csharp
public class FooDto : BaseDataTransferObject<long>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

// Usage
var dto = new FooDto 
{ 
    Id = 1, 
    Name = "Test",
    Errors = new List<string>()
};
```

### Repository Interfaces

Three levels of repository contracts:

#### IBaseRepositoryWithListOnly<TDto, TId>

Read-only access with list operation:
```csharp
public interface IBaseRepositoryWithListOnly<TDto, TId>
{
    Task<IList<TDto>> ListAsync(CancellationToken cancellationToken = default);
}
```

#### IBaseRepositoryWithCrud<TDto, TId>

Full CRUD operations (no List):
```csharp
public interface IBaseRepositoryWithCrud<TDto, TId>
{
    Task<TId> CreateAsync(TDto dto, CancellationToken cancellationToken = default);
    Task<TDto> ReadAsync(TId id, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
```

#### IBaseRepositoryWithCrudl<TDto, TId>

Complete CRUDL operations (extends both interfaces above):
```csharp
public interface IBaseRepositoryWithCrudl<TDto, TId>
    : IBaseRepositoryWithCrud<TDto, TId>, IBaseRepositoryWithListOnly<TDto, TId>
{
}
```

### Entity Base Classes

Entities wrap repositories and add business logic with automatic logging.

#### BaseEntityWithListOnly<TEntity, TDto, TId>
```csharp
public class FooEntity : BaseEntityWithListOnly<FooEntity, FooDto, long>
{
    public FooEntity(
        IBaseRepositoryWithListOnly<FooDto, long> repository,
        ILoggerFactory factory)
        : base(repository, factory)
    {
    }
    
    // ListAsync
}
```

#### BaseEntityWithCrud<TEntity, TDto, TId>
```csharp
public class FooEntity : BaseEntityWithCrud<FooEntity, FooDto, long>
{
    public FooEntity(
        IBaseRepositoryWithCrud<FooDto, long> repository,
        ILoggerFactory factory)
        : base(repository, factory)
    {
    }
    
    // Inherits: CreateAsync, ReadAsync, UpdateAsync, DeleteAsync
}
```

#### BaseEntityWithCrudl<TEntity, TDto, TId>
```csharp
public class FooEntity : BaseEntityWithCrudl<FooEntity, FooDto, long>
{
    public FooEntity(
        IBaseRepositoryWithCrudl<FooDto, long> repository,
        ILoggerFactory factory)
        : base(repository, factory)
    {
    }
    
    // Inherits: CreateAsync, ReadAsync, UpdateAsync, DeleteAsync, ListAsync
}
```

## Complete Example

### 1. Define Your DTO
```csharp
using Roadbed.Crud;

public class FooDto : BaseDataTransferObject<long>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Define Repository Interface
```csharp
public interface IFooRepository : IBaseRepositoryWithCrudl<FooDto, long>
{
    // Add custom methods beyond CRUDL here
}
```

### 3. Implement Repository
```csharp
public class FooRepository : IFooRepository
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

    public async Task<long> CreateAsync(
        FooDto dto,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public async Task<FooDto> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public async Task<bool> UpdateAsync(
        FooDto dto,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public async Task<IList<FooDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### 4. Create Entity with Business Logic
```csharp
using Roadbed.Crud;

public class FooEntity : BaseEntityWithCrudl<FooEntity, FooDto, long>
{
    public FooEntity(
        IFooRepository repository,
        ILoggerFactory factory)
        : base(repository, factory)
    {
    }

    // Add business logic methods
    public async Task<FooDto?> GetActiveByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug($"Getting active foo by name: {name}");
        
        var allItems = await this.ListAsync(cancellationToken);
        return allItems.FirstOrDefault(x => x.Name == name);
    }

    public async Task<long> CreateWithValidationAsync(
        FooDto dto,
        CancellationToken cancellationToken = default)
    {
        // Business validation
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            dto.Errors ??= new List<string>();
            dto.Errors.Add("Name is required");
            return 0;
        }

        // Call base CRUD operation
        return await this.CreateAsync(dto, cancellationToken) ?? 0;
    }
}
```

## Requirements

- .NET 10.0+
- Microsoft.Extensions.Logging
- Newtonsoft.Json
- Roadbed (base library)

## Related Packages

- **Roadbed.Data** - Core data access abstractions
- **Roadbed.Data.Sqlite** - SQLite implementations
- **Roadbed.Data.Dapper** - Dapper configuration
