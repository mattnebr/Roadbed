# CRUD Research

We explored multiple ways we could implement our CRUD operation framework. After evaluating each option, we selected **Option 10** as the foundation for Roadbed.Crud.

## Options Evaluated

- [Option 1: Minimal Async-First (Baseline)](option-a.md) — Single async interface, no sync support
- [Option 2: Parallel Sync/Async Interface Hierarchies](option-b.md) — Separate sync and async trees
- [Option 3: Granular Composites with Direct Logger Injection](option-c.md) — Fine-grained interfaces, logger in every composite
- [Option 4: CRTP Logging with BaseClassWithLogging + Query/Filter Operation](option-d.md) — Curiously Recurring Template Pattern for logging
- [Option 5: Non-Generic BaseClassWithLogging + Virtual Method Defaults](option-e.md) — Non-generic base, virtual defaults
- [Option 6: Service Layer with Repository + Service Separation](option-f.md) — Distinct service and repository layers
- [Option 7: Shared Operation Interfaces Across Layers](option-g.md) — Same operation interfaces for both layers
- [Option 8: Single Interface Per Entity (Marker Interface Pattern)](option-h.md) — One interface per entity, marker-based
- [Option 9: Hybrid — Shared Operations + Marker Interface + Optional Service Layer](option-i.md) — Combines Options 7 and 8
- [Option 10: Full Prescriptive — Sync/Async/Both, Compiler Enforcement, Hierarchical Composites](option-j.md) — ✅ **Selected**

## Why Option 10?

Option 10 provides the most complete framework: parallel sync/async hierarchies, granular single-operation interfaces composed into CRUD/CRUDL/CRUDA/CRUDAL composites, separate repository and service layers with shared operation contracts, and abstract base classes that enforce implementation via the compiler. It carries the most upfront type count (60 types) but eliminates entire categories of bugs by making incorrect usage a compile error rather than a runtime surprise.

## Industry Context

The service-repository separation is a well-established pattern with different names across different architectural traditions:

|Tradition|Term for Business Logic|Term for Data Access|Source|
|---|---|---|---|
|Fowler (PoEAA)|Service Layer|Repository / Data Mapper|Patterns of Enterprise Application Architecture (2002)|
|Domain-Driven Design (Evans)|Application Service|Repository|Domain-Driven Design (2003)|
|Clean Architecture (Martin)|Use Case / Interactor|Gateway|Clean Architecture (2017)|
|CQRS|Command Handler / Query Handler|Repository|Greg Young, Udi Dahan|
|.NET MediatR pattern|Request Handler|Repository|Jimmy Bogard|
|Generic .NET convention|Service|Repository|Microsoft documentation|

**Key distinction**: In all of these traditions, the repository is a thin data access layer with no business logic. Business rules, validation, authorization, orchestration, and cross-cutting concerns like logging and caching live in the layer above. The name varies — "service", "use case", "interactor", "handler" — but the responsibility is the same.

Roadbed.Crud uses **"Service"** because it is the most widely understood term in the .NET ecosystem and does not carry the domain-specific baggage of DDD or Clean Architecture terminology.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│ Application Layer (Console, Web, etc.)              │
│                                                     │
│   Depends on: IFooService (from class library)      │
│   Does NOT depend on: IFooRepository                │
└────────────────────────┬────────────────────────────┘
                         │
                         │ IFooService (public interface)
                         │
┌────────────────────────▼────────────────────────────┐
│ Class Library (implements Roadbed.Crud)              │
│                                                     │
│   public interface IFooService                      │
│       : IAsyncCrudService<Foo, string>              │
│                                                     │
│   internal class FooService                         │
│       : BaseAsyncCrudService<Foo, string>           │
│       Depends on: IFooRepository                    │
│       Contains: validation, business rules, caching │
│                                                     │
│   internal interface IFooRepository                 │ ← Note: internal
│       : IAsyncCrudRepository<Foo, string>           │
│                                                     │
│   internal class FooRepository                      │
│       : BaseAsyncCrudRepository<Foo, string>        │
│       Contains: pure data access                    │
│                                                     │
│   public class DataInstaller                        │
│       : IServiceCollectionInstaller                 │
│       Registers both service and repository in DI   │
└─────────────────────────────────────────────────────┘
```

**Key point**: The repository interface is internal. The application layer only sees the service interface. The service is the public API of the class library.