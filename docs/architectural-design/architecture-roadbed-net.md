# Roadbed.Net Architecture

Roadbed.Net provides a resilient HTTP client layer with automatic retry, exponential backoff, compression selection, and authentication support. It is the networking foundation used inside Roadbed.Crud repository implementations to call external RESTful APIs.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Net NuGet package. When a developer asks you to create an SDK class library that calls a REST API, use this document to construct requests, handle responses, and integrate with the Roadbed.Crud repository pattern.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation — not `?? throw new ArgumentNullException(...)`.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Inject `INetHttpClient` via constructor** in repository implementations — never instantiate `NetHttpClient` directly.
5. **Use the correct generic type parameter** — `MakeHttpRequestAsync<string>()` for raw JSON, or `MakeHttpRequestAsync<TDto>()` for automatic deserialization with Newtonsoft.Json.
6. **Always check `response.IsSuccessStatusCode`** before accessing `response.Data`.
7. **Use Newtonsoft.Json** for serialization — not System.Text.Json. When `T` is a complex type, Roadbed.Net deserializes automatically with Newtonsoft.Json internally.
8. **Create a new `NetHttpRequest` for each API call** — do not reuse request instances.
9. **Set `HttpEndPoint` as a `Uri`** — never pass raw strings.
10. **CancellationToken is always the last parameter** with `= default`.
11. **Don't register `IHttpClientFactory` or `INetHttpClient` manually** — `InstallNetHttpClient` handles this via auto-discovery.
12. **Flatten namespaces** — only `using Roadbed.Net;` is needed. The `.Dtos`, `.Entities`, and `.Enumerators` suffixes were removed on purpose.
13. **Use `this.LogDebug()`, `this.LogInformation()`, etc.** — never call `this.Logger.LogDebug()` directly. The base class methods check `IsEnabled()` before formatting.
14. **Repository interfaces and service interfaces are `internal`** — the application layer depends on the concrete service class, not any internal interface.
15. **Concrete service classes are `public sealed`** with a dual constructor pattern: a `public` constructor (takes only `ILogger<T>`, resolves the repository via `ServiceLocator`) and an `internal` constructor (takes the repository and `ILogger<T>` directly, for unit tests via `InternalsVisibleTo`).

---

## Table of Contents

1. [For AI Assistants](https://claude.ai/chat/architecture-roadbed-net.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-net.md#type-catalog)
3. [Package Relationship](architecture-roadbed-net.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-net.md#namespace-convention)
5. [HTTP Client Architecture](architecture-roadbed-net.md#http-client-architecture)
    - [Named HTTP Clients](architecture-roadbed-net.md#named-http-clients)
    - [INetHttpClient Interface](architecture-roadbed-net.md#inethttpclient-interface)
    - [NetHttpClient Implementation](architecture-roadbed-net.md#nethttpclient-implementation)
    - [Request Flow](architecture-roadbed-net.md#request-flow)
6. [Request Configuration](architecture-roadbed-net.md#request-configuration)
    - [NetHttpRequest](architecture-roadbed-net.md#nethttprequest)
    - [NetHttpRequest Defaults](architecture-roadbed-net.md#nethttprequest-defaults)
    - [NetHttpHeader](architecture-roadbed-net.md#nethttpheader)
    - [NetHttpAuthentication](architecture-roadbed-net.md#nethttpauthentication)
    - [NetHttpAuthenticationType](architecture-roadbed-net.md#nethttpauthenticationtype)
    - [NetHttpRetryPattern](architecture-roadbed-net.md#nethttpretrypattern)
7. [Response Handling](architecture-roadbed-net.md#response-handling)
    - [NetHttpResponse\<T\>](architecture-roadbed-net.md#nethttpresponset)
    - [Success vs Failure](architecture-roadbed-net.md#success-vs-failure)
    - [Generic Type Parameter Behavior](architecture-roadbed-net.md#generic-type-parameter-behavior)
    - [Deserialization Error Handling](architecture-roadbed-net.md#deserialization-error-handling)
8. [Retry and Backoff Strategy](architecture-roadbed-net.md#retry-and-backoff-strategy)
    - [Retriable Conditions](architecture-roadbed-net.md#retriable-conditions)
    - [Backoff Calculation](architecture-roadbed-net.md#backoff-calculation)
    - [HttpRequestMessage Lifecycle](architecture-roadbed-net.md#httprequestmessage-lifecycle)
9. [Logging](architecture-roadbed-net.md#logging)
10. [Module Auto-Discovery](architecture-roadbed-net.md#module-auto-discovery)
11. [Implementation Walkthrough](architecture-roadbed-net.md#implementation-walkthrough)
12. [Common Pitfalls](architecture-roadbed-net.md#common-pitfalls)
13. [Quick Reference](architecture-roadbed-net.md#quick-reference)

---

## Type Catalog

Roadbed.Net contains **9 public types** organized into five groups. All types live at the project root — there are no sub-folders.

### HTTP Client (2 types)

| Type             | Kind      | Namespace     | Purpose                                                           |
| ---------------- | --------- | ------------- | ----------------------------------------------------------------- |
| `INetHttpClient` | Interface | `Roadbed.Net` | Contract for making HTTP requests with retry and backoff support. |
| `NetHttpClient`  | Class     | `Roadbed.Net` | Concrete implementation. Inherits `BaseClassWithLogging`.         |

### Request Configuration (4 types)

| Type                    | Kind   | Namespace     | Purpose                                                                 |
| ----------------------- | ------ | ------------- | ----------------------------------------------------------------------- |
| `NetHttpRequest`        | Class  | `Roadbed.Net` | Request configuration: endpoint, method, headers, auth, retry, timeout. |
| `NetHttpHeader`         | Record | `Roadbed.Net` | HTTP header name/value pair.                                            |
| `NetHttpAuthentication` | Class  | `Roadbed.Net` | Authentication type and credential value.                               |
| `NetHttpRetryPattern`   | Class  | `Roadbed.Net` | Retry configuration: max attempts and delay multiplier.                 |

### Response (1 type)

| Type                 | Kind   | Namespace     | Purpose                                                                      |
| -------------------- | ------ | ------------- | ---------------------------------------------------------------------------- |
| `NetHttpResponse<T>` | Record | `Roadbed.Net` | Generic response wrapper: status code, success flag, typed data, error list. |

### Enumeration (1 type)

| Type                        | Kind | Namespace     | Purpose                                        |
| --------------------------- | ---- | ------------- | ---------------------------------------------- |
| `NetHttpAuthenticationType` | Enum | `Roadbed.Net` | Authentication scheme: Unknown, Basic, Bearer. |

### Module Auto-Discovery (1 type)

| Type                   | Kind  | Namespace                | Purpose                                                                                 |
| ---------------------- | ----- | ------------------------ | --------------------------------------------------------------------------------------- |
| `InstallNetHttpClient` | Class | `Roadbed.Net.Installers` | Auto-discovered installer. Registers named `HttpClient` instances and `INetHttpClient`. |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Your SDK (e.g., Roadbed.Sdk.NationalWeatherService)          │
│                                                              │
│   Entity     → BaseEntityRecord<string> (Roadbed.Crud)      │
│   Repository → BaseAsyncCrudlRepository (Roadbed.Crud)      │
│                injects INetHttpClient (Roadbed.Net)          │
│                calls MakeHttpRequestAsync<TDto>()            │
│   Service    → BaseAsyncCrudlService (Roadbed.Crud)          │
│   Installer  → IServiceCollectionInstaller (Roadbed.Common)  │
└──────────┬─────────────────────┬─────────────────────────────┘
           │                     │
           │ injects             │ inherits from
           ▼                     ▼
┌──────────────────────┐  ┌─────────────────────────────────────┐
│ Roadbed.Net          │  │ Roadbed.Crud                        │
│                      │  │                                     │
│  INetHttpClient      │  │  BaseEntityRecord<TId>              │
│  NetHttpClient       │  │  BaseAsyncCrudlRepository<T, TId>   │
│  NetHttpRequest      │  │  BaseAsyncCrudlService<T, TId>      │
│  NetHttpResponse<T>  │  │  IAsyncCrudlRepository<T, TId>      │
│  InstallNetHttpClient│  │                                     │
└──────────┬───────────┘  └──────────┬──────────────────────────┘
           │                         │
           │ depends on              │ depends on
           ▼                         ▼
┌──────────────────────────────────────────────────────────────┐
│ Roadbed.Common                                               │
│                                                              │
│  BaseClassWithLogging      (logging base class)              │
│  IServiceCollectionInstaller (module auto-discovery)         │
│  ServiceLocator            (NuGet self-containment)          │
└──────────────────────────────────────────────────────────────┘
```

**Roadbed.Net** provides the HTTP transport layer. **Roadbed.Crud** provides the repository/service pattern. SDK class libraries combine both: the repository inherits from a Roadbed.Crud base class and injects `INetHttpClient` to call the external API.

---

## Namespace Convention

All original sub-namespaces have been flattened and all files live at the project root so that consuming code only needs `using Roadbed.Net;`. The exception to this is the installer and enumerators, which retains its sub-namespace:

| Namespace                 | Contains                           |
| ------------------------- | ---------------------------------- |
| `Roadbed.Net`             | All 8 public types                 |
| `Roadbed.Net.Enumerators` | Enum available without extra using |
| `Roadbed.Net.Installers`  | `InstallNetHttpClient`             |

---

## HTTP Client Architecture

### Named HTTP Clients

Roadbed.Net registers two named `HttpClient` instances via `IHttpClientFactory`:

| Client Name          | Compression                            | Selected When                                |
| -------------------- | -------------------------------------- | -------------------------------------------- |
| `"DefaultClient"`    | `DecompressionMethods.None`            | `request.EnableCompression = false`          |
| `"CompressedClient"` | `DecompressionMethods.Deflate \| GZip` | `request.EnableCompression = true` (default) |

The named client approach follows Microsoft's `IHttpClientFactory` best practices: the factory manages `HttpMessageHandler` lifetimes and pooling, avoiding socket exhaustion.

### INetHttpClient Interface

The contract for making HTTP requests:

```csharp
namespace Roadbed.Net;

public interface INetHttpClient
{
    Task<NetHttpResponse<T>> MakeHttpRequestAsync<T>(
        NetHttpRequest request,
        CancellationToken cancellationToken = default);
}
```

**Key behaviors:**

- The generic type parameter `T` can be `string` for raw response body, or a complex DTO type for automatic JSON deserialization
- Throws `ArgumentNullException` if `request` is null
- Never throws for HTTP failures — all errors are wrapped in `NetHttpResponse<T>.Failure()`
- Inject this interface in repository constructors

### NetHttpClient Implementation

The concrete implementation inherits from `BaseClassWithLogging` for level-checked logging:

```csharp
namespace Roadbed.Net;

public class NetHttpClient
    : BaseClassWithLogging, INetHttpClient
{
    public NetHttpClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NetHttpClient> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        this._httpClientFactory = httpClientFactory;
    }

    public async Task<NetHttpResponse<T>> MakeHttpRequestAsync<T>(
        NetHttpRequest request,
        CancellationToken cancellationToken = default);
}
```

**Constructor parameters:**

| Parameter           | Type                     | Source                                   |
| ------------------- | ------------------------ | ---------------------------------------- |
| `httpClientFactory` | `IHttpClientFactory`     | Registered by `InstallNetHttpClient`     |
| `logger`            | `ILogger<NetHttpClient>` | Registered by `InstallExtensionsLogging` |

Both are resolved automatically by DI — SDK code never creates `NetHttpClient` directly.

### Request Flow

```
Repository method calls:
    this._httpClient.MakeHttpRequestAsync<TDto>(request, cancellationToken)
        │
        ▼
    Validates request is not null
    Logs: HTTP {Method} {Endpoint} (Compression: {Compression})
        │
        ▼
    MakeRequestWithBackoffRetryAsync(request, cancellationToken)
        │
        ▼
    Retry loop (0 to MaxAttempts):
        │
        ├── Create NEW HttpRequestMessage (headers, auth)
        ├── Create HttpClient from factory (DefaultClient or CompressedClient)
        ├── Send request with timeout
        │
        ├── Success (2xx, not 404) → continue to deserialization
        ├── Non-retriable error (4xx, 500, 502) → return response
        ├── Retriable (503, 408, 504) → log warning → wait with backoff → retry
        ├── HttpRequestException → log warning → wait with backoff → retry
        └── TimeoutException → log warning → wait with backoff → retry
        │
        ▼
    All retries exhausted → log error → return last response or 400 BadRequest
        │
        ▼
    Read response body as string
        │
        ├── T is string → wrap raw body in Success()
        └── T is complex type → JsonConvert.DeserializeObject<T>()
            ├── Deserialization succeeds → wrap in Success()
            └── JsonException → log error → wrap in Failure()
        │
        ▼
    Return NetHttpResponse<T> to repository
```

---

## Request Configuration

### NetHttpRequest

The primary configuration object for an HTTP request. Every API call starts by creating a `NetHttpRequest`:

```csharp
namespace Roadbed.Net;

public class NetHttpRequest
{
    // Defaults set in constructor:
    public HttpMethod Method { get; set; }                     // HttpMethod.Get
    public bool EnableCompression { get; set; }                // true
    public int TimeoutInSecondsPerAttempt { get; set; }        // 15
    public NetHttpRetryPattern RetryPattern { get; set; }      // MaxAttempts=3, Delay=5s

    // Must be set by caller:
    public Uri? HttpEndPoint { get; set; }

    // Optional:
    public HttpContent? Content { get; set; }
    public NetHttpAuthentication? Authentication { get; set; }
    public List<NetHttpHeader> HttpHeaders { get; set; }       // Initialized to empty list
}
```

### NetHttpRequest Defaults

| Property                                | Default Value               | Notes                                       |
| --------------------------------------- | --------------------------- | ------------------------------------------- |
| `Method`                                | `HttpMethod.Get`            | Override for POST, PUT, DELETE, etc.        |
| `EnableCompression`                     | `true`                      | Selects `CompressedClient` (GZip + Deflate) |
| `TimeoutInSecondsPerAttempt`            | `15` seconds                | Per-attempt timeout, not total              |
| `RetryPattern.MaxAttempts`              | `3`                         | 4 total attempts (1 initial + 3 retries)    |
| `RetryPattern.DelayMultiplierInSeconds` | `5`                         | Base for exponential backoff                |
| `HttpHeaders`                           | Empty `List<NetHttpHeader>` | Initialized in constructor                  |
| `Content`                               | `null`                      | Set for POST/PUT requests                   |
| `Authentication`                        | `null`                      | Set when API requires auth                  |

### NetHttpHeader

A record representing an HTTP header:

```csharp
namespace Roadbed.Net;

public record NetHttpHeader
{
    public NetHttpHeader();
    public NetHttpHeader(string name, string value);

    public string? Name { get; set; }
    public string? Value { get; set; }
}
```

**Usage:**

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/data"),
    HttpHeaders =
    {
        new NetHttpHeader("Accept", "application/geo+json"),
        new NetHttpHeader("User-Agent", "(MyApp, contact@example.com)"),
    },
};
```

Headers with a null or empty `Name` are silently skipped when building the `HttpRequestMessage`.

### NetHttpAuthentication

Holds the authentication scheme and credential value:

```csharp
namespace Roadbed.Net;

public class NetHttpAuthentication
{
    public NetHttpAuthenticationType AuthenticationType { get; set; }
    public string? Value { get; set; }
}
```

**Usage:**

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/data"),
    Authentication = new NetHttpAuthentication
    {
        AuthenticationType = NetHttpAuthenticationType.Bearer,
        Value = "eyJhbGciOiJIUzI1NiIs...",
    },
};
```

This produces the HTTP header: `Authorization: Bearer eyJhbGciOiJIUzI1NiIs...`

### NetHttpAuthenticationType

| Value     | Int | HTTP Header Value               |
| --------- | --- | ------------------------------- |
| `Unknown` | 0   | (no Authorization header added) |
| `Basic`   | 1   | `Basic {Value}`                 |
| `Bearer`  | 2   | `Bearer {Value}`                |

When `AuthenticationType` is `Unknown`, no `Authorization` header is added to the request.

### NetHttpRetryPattern

Configuration for the retry-with-backoff strategy:

```csharp
namespace Roadbed.Net;

public class NetHttpRetryPattern
{
    public int MaxAttempts { get; set; }
    public int DelayMultiplierInSeconds { get; set; }
}
```

**Important:** `MaxAttempts` is the number of _retries_, not the total number of attempts. The total number of attempts is `MaxAttempts + 1` (1 initial attempt + N retries).

---

## Response Handling

### NetHttpResponse\<T\>

A generic response record wrapping the result of an HTTP call:

```csharp
namespace Roadbed.Net;

public record NetHttpResponse<T>
{
    // Read-only properties:
    public bool IsSuccessStatusCode { get; }
    public T Data { get; internal set; }
    public List<string> Errors { get; }
    public int HttpStatusCode { get; internal set; }
    public string? HttpStatusCodeDescription { get; internal set; }

    // Internal factory methods:
    internal static NetHttpResponse<T> Success(int statusCode, string? statusDescription, T value);
    internal static NetHttpResponse<T> Failure(int statusCode, string? statusDescription, string error);
}
```

The constructor and factory methods are `internal`. SDK code cannot create `NetHttpResponse<T>` instances directly — they are produced exclusively by `NetHttpClient`.

### Success vs Failure

| Scenario                   | Factory Method | `IsSuccessStatusCode` | `Data`           | `Errors`         |
| -------------------------- | -------------- | --------------------- | ---------------- | ---------------- |
| HTTP 2xx (not 404)         | `Success()`    | `true`                | Deserialized `T` | Empty list       |
| HTTP 4xx, 5xx              | `Failure()`    | `false`               | `default!`       | Contains message |
| HTTP 404 Not Found         | `Failure()`    | `false`               | `default!`       | Contains message |
| JSON deserialization error | `Failure()`    | `false`               | `default!`       | Contains message |
| `SocketException`          | `Failure()`    | `false`               | `default!`       | Contains message |
| `HttpRequestException`     | `Failure()`    | `false`               | `default!`       | Contains message |
| Generic `Exception`        | `Failure()`    | `false`               | `default!`       | Contains message |
| All retries exhausted      | `Failure()`    | `false`               | `default!`       | Contains message |

**Key behavior:** HTTP 404 is treated as a failure, even though the HTTP call itself succeeded. This aligns with the Roadbed.Crud convention where `ReadAsync` returns `null` for missing entities rather than throwing.

### Generic Type Parameter Behavior

The generic type parameter `T` controls how the response body is processed:

| `T` is         | Behavior                                                                |
| -------------- | ----------------------------------------------------------------------- |
| `string`       | Raw response body returned as-is — no deserialization                   |
| Any other type | Response body deserialized via `JsonConvert.DeserializeObject<T>(body)` |

**Single-step pattern (preferred for most SDK repositories):**

```csharp
// Deserializes JSON directly into the DTO
NetHttpResponse<CustomApiResponse> response =
    await this._httpClient.MakeHttpRequestAsync<CustomApiResponse>(
        request, cancellationToken);

if (response.IsSuccessStatusCode)
{
    var obj = response.Data.List.First();
}
```

**Raw string pattern (when manual control is needed):**

```csharp
// Returns raw JSON string — caller handles deserialization
NetHttpResponse<string> response =
    await this._httpClient.MakeHttpRequestAsync<string>(request, cancellationToken);

if (response.IsSuccessStatusCode)
{
    var dto = JsonConvert.DeserializeObject<MyDto>(response.Data);
}
```

### Deserialization Error Handling

When `T` is a complex type and the response body cannot be deserialized, `NetHttpClient` catches the `JsonException` and returns a `Failure()` response instead of throwing. The error message includes the target type name and the exception message:

```
Failed to deserialize response to CustomApiResponse: Unexpected character encountered...
```

This means SDK code does **not** need to wrap `MakeHttpRequestAsync<TDto>()` in a try/catch for JSON errors — the `IsSuccessStatusCode` check handles it:

```csharp
NetHttpResponse<MyDto> response =
    await this._httpClient.MakeHttpRequestAsync<MyDto>(request, cancellationToken);

// This check covers BOTH HTTP failures AND deserialization failures
if (!response.IsSuccessStatusCode)
{
    this.LogWarning("Request failed: {Error}", response.Errors.FirstOrDefault());
    return null;
}

// Safe to access — Data is a valid MyDto instance
return response.Data;
```

---

## Retry and Backoff Strategy

### Retriable Conditions

The retry loop retries under these conditions:

| Condition                              | HTTP Status Code | Retried?                    |
| -------------------------------------- | ---------------- | --------------------------- |
| Service Unavailable                    | 503              | ✅ Yes                       |
| Request Timeout                        | 408              | ✅ Yes                       |
| Gateway Timeout                        | 504              | ✅ Yes                       |
| `HttpRequestException` (network error) | —                | ✅ Yes                       |
| `TimeoutException` (task timeout)      | —                | ✅ Yes                       |
| Bad Request                            | 400              | ❌ No                        |
| Unauthorized                           | 401              | ❌ No                        |
| Forbidden                              | 403              | ❌ No                        |
| Not Found                              | 404              | ❌ No                        |
| Internal Server Error                  | 500              | ❌ No                        |
| Bad Gateway                            | 502              | ❌ No                        |
| Any 2xx success                        | 200–299          | ❌ No (returned immediately) |

### Backoff Calculation

The backoff uses exponential growth based on `Math.Pow(DelayMultiplierInSeconds, attempt)`:

```csharp
var amount = Math.Pow(request.RetryPattern.DelayMultiplierInSeconds, attempt);
await Task.Delay(TimeSpan.FromSeconds(amount), cancellationToken);
```

**With default values** (`DelayMultiplierInSeconds = 5`, `MaxAttempts = 3`):

| After Attempt | Delay Calculation               | Delay      |
| ------------- | ------------------------------- | ---------- |
| 0 (initial)   | 5^0 = 1                         | 1 second   |
| 1 (1st retry) | 5^1 = 5                         | 5 seconds  |
| 2 (2nd retry) | 5^2 = 25                        | 25 seconds |
| 3 (3rd retry) | (last attempt — no delay after) | —          |

**Total maximum wait time** (default): 1 + 5 + 25 = **31 seconds** of delay, plus up to 4 × 15 = 60 seconds of request time = **~91 seconds worst case**.

### HttpRequestMessage Lifecycle

**CRITICAL:** `HttpRequestMessage` can only be sent once. The retry loop creates a **new** `HttpRequestMessage` for each attempt:

```csharp
for (int attempt = 0; attempt <= request.RetryPattern.MaxAttempts; attempt++)
{
    using (HttpRequestMessage message = CreateHttpRequestMessage(request))
    {
        using (HttpClient client = this.CreateHttpClient(request))
        {
            HttpResponseMessage response = await client.SendAsync(message, ...);
        }
    }
    // HttpRequestMessage is disposed here after each attempt
}
```

The `CreateHttpRequestMessage` static method builds a fresh `HttpRequestMessage` each time, copying headers and authentication from the `NetHttpRequest`. This is why you should not set headers directly on `HttpRequestMessage` — always configure `NetHttpRequest` and let the factory method handle it.

---

## Logging

`NetHttpClient` inherits from `BaseClassWithLogging` and logs at appropriate levels throughout the request lifecycle:

### Log Levels by Event

| Event                                 | Level     | Example Message                                                        |
| ------------------------------------- | --------- | ---------------------------------------------------------------------- |
| Request start                         | `Debug`   | `HTTP GET https://api.example.com/data (Compression: True)`            |
| Successful response                   | `Debug`   | `HTTP 200 from https://api.example.com/data`                           |
| Retriable HTTP status (503, 408, 504) | `Warning` | `HTTP 503 from ..., attempt 1 of 4, retrying after backoff`            |
| Network error during retry            | `Warning` | `Network error calling ..., attempt 2 of 4, retrying after backoff`    |
| Timeout during retry                  | `Warning` | `Timeout calling ..., attempt 1 of 4, retrying after backoff`          |
| All retries exhausted                 | `Error`   | `All retry attempts exhausted for GET ..., last status 503`            |
| JSON deserialization failure          | `Error`   | `Failed to deserialize response from ... to WeatherStationApiResponse` |
| Socket exception (outer catch)        | `Error`   | `Socket error calling ...`                                             |
| Unexpected exception (outer catch)    | `Error`   | `Unexpected error calling ...`                                         |

### Structured Logging Parameters

All log messages use structured logging with named parameters for filtering and correlation:

- `{Method}` — HTTP method (GET, POST, etc.)
- `{Endpoint}` — Request URI
- `{Compression}` — Whether compression is enabled
- `{StatusCode}` — HTTP status code (integer)
- `{Attempt}` — Current attempt number (1-based)
- `{TotalAttempts}` — Total number of attempts configured
- `{Type}` — Target deserialization type name

---

## Module Auto-Discovery

### InstallNetHttpClient

The installer is auto-discovered by `services.InstallModulesInAppDomain(configuration)` at application startup. It registers both named HTTP clients, the `INetHttpClient` implementation, and updates `ServiceLocator`:

```csharp
namespace Roadbed.Net.Installers;

public class InstallNetHttpClient : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // No compression
        services.AddHttpClient("DefaultClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.None,
            });

        // GZip + Deflate compression
        services.AddHttpClient("CompressedClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            });

        // Register INetHttpClient for dependency injection
        services.AddScoped<INetHttpClient, NetHttpClient>();

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

**Consuming applications do not need to register HTTP clients manually.** The single call to `services.InstallModulesInAppDomain(configuration)` discovers this installer (along with all other Roadbed module installers) automatically.

---

## Implementation Walkthrough

This walkthrough shows how to create an SDK class library that calls a REST API using Roadbed.Net and Roadbed.Crud together. The example creates an async CRUDL repository for a `Foo` entity retrieved from an external API.

### Step 1: Define the Entity

```csharp
namespace Roadbed.Sdk.FooService;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Represents a Foo entity.
/// </summary>
public sealed record Foo : BaseEntityRecord<string>
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public override string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [JsonProperty("name")]
    required public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonProperty("description")]
    required public string Description { get; set; }

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    [JsonProperty("category")]
    required public string Category { get; set; }
}
```

### Step 2: Define the API Response DTOs

```csharp
namespace Roadbed.Sdk.FooService;

using Newtonsoft.Json;

/// <summary>
/// Root response from the Foo API.
/// </summary>
public sealed record FooApiResponse
{
    [JsonProperty("items")]
    required public FooItem[] Items { get; set; }
}

/// <summary>
/// A single item in the API response.
/// </summary>
public sealed record FooItem
{
    [JsonProperty("id")]
    required public string Id { get; set; }

    [JsonProperty("attributes")]
    required public FooAttributes Attributes { get; set; }
}

/// <summary>
/// Attributes of a Foo item.
/// </summary>
public sealed record FooAttributes
{
    [JsonProperty("name")]
    required public string Name { get; set; }

    [JsonProperty("description")]
    required public string Description { get; set; }

    [JsonProperty("category")]
    required public string Category { get; set; }
}
```

### Step 3: Define the Repository Interface (internal)

```csharp
namespace Roadbed.Sdk.FooService;

using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Repository interface for Foo data access.
/// </summary>
internal interface IFooRepository
    : IAsyncCrudlRepository<Foo, string>
{
}
```

### Step 4: Implement the Repository

This is where Roadbed.Net is used. The repository injects `INetHttpClient` and calls `MakeHttpRequestAsync<TDto>()` with the API response DTO as the generic type:

```csharp
namespace Roadbed.Sdk.FooService;

using Microsoft.Extensions.Logging;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Net;

/// <summary>
/// Repository implementation for Foo data access.
/// </summary>
internal sealed class FooRepository
    : BaseAsyncCrudlRepository<Foo, string>,
      IFooRepository
{
    #region Private Fields

    /// <summary>
    /// Base URL for the Foo API.
    /// </summary>
    private const string BaseUrl = "https://api.example.com";

    /// <summary>
    /// HTTP client for making API requests.
    /// </summary>
    private readonly INetHttpClient _httpClient;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooRepository"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making API requests.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public FooRepository(
        INetHttpClient httpClient,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this._httpClient = httpClient;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task<Foo?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        this.LogDebug("Reading Foo {Id}", id);

        // Build the request
        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri($"{BaseUrl}/foos/{id}"),
            HttpHeaders =
            {
                new NetHttpHeader("Accept", "application/json"),
                new NetHttpHeader("User-Agent", "(MyApp, contact@example.com)"),
            },
        };

        // Make the HTTP call — deserializes JSON automatically
        NetHttpResponse<FooItem> response =
            await this._httpClient.MakeHttpRequestAsync<FooItem>(
                request, cancellationToken);

        // Handle failure — return null (Roadbed.Crud convention)
        if (!response.IsSuccessStatusCode)
        {
            this.LogWarning(
                "Failed to read Foo {Id}: {StatusCode} {Description}",
                id,
                response.HttpStatusCode,
                response.HttpStatusCodeDescription);
            return null;
        }

        var item = response.Data;

        // Map DTO to entity
        return new Foo
        {
            Id = item.Id,
            Name = item.Attributes.Name,
            Description = item.Attributes.Description,
            Category = item.Attributes.Category,
        };
    }

    /// <inheritdoc/>
    public override async Task<IList<Foo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Listing Foos");

        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri($"{BaseUrl}/foos"),
            HttpHeaders =
            {
                new NetHttpHeader("Accept", "application/json"),
                new NetHttpHeader("User-Agent", "(MyApp, contact@example.com)"),
            },
        };

        NetHttpResponse<FooApiResponse> response =
            await this._httpClient.MakeHttpRequestAsync<FooApiResponse>(
                request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            this.LogWarning(
                "Failed to list Foos: {StatusCode}",
                response.HttpStatusCode);
            return new List<Foo>();
        }

        if (response.Data?.Items == null)
        {
            return new List<Foo>();
        }

        return response.Data.Items.Select(f => new Foo
        {
            Id = f.Id,
            Name = f.Attributes.Name,
            Description = f.Attributes.Description,
            Category = f.Attributes.Category,
        }).ToList<Foo>();
    }

    /// <inheritdoc/>
    public override Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Foo API is read-only.");
    }

    /// <inheritdoc/>
    public override Task<Foo> UpdateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Foo API is read-only.");
    }

    /// <inheritdoc/>
    public override Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Foo API is read-only.");
    }

    #endregion Public Methods
}
```

### Step 5: Define the Service Interface and Implementation

```csharp
namespace Roadbed.Sdk.FooService;

using Roadbed.Crud.Services.Async;

/// <summary>
/// Service interface for Foo business operations.
/// </summary>
internal interface IFooService
    : IAsyncCrudlService<Foo, string>
{
}
```

```csharp
namespace Roadbed.Sdk.FooService;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Service implementation for Foo business operations.
/// </summary>
public sealed class FooService
    : BaseAsyncCrudlService<Foo, string>,
      IFooService
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public FooService(
        ILogger<FooService> logger)
        : base(
            ServiceLocator.GetService<IFooRepository>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooService"/> class.
    /// </summary>
    /// <param name="repository">Repository for Foo data access.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal FooService(
        IFooRepository repository,
        ILogger<FooService> logger)
        : base(repository, logger)
    {
    }

    #endregion Internal Constructors
}
```

### Step 6: Register in DI

```csharp
namespace Roadbed.Sdk.FooService;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;

/// <summary>
/// Installer for Foo Service SDK.
/// </summary>
public sealed class InstallFooService : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register repository (internal) for ServiceLocator resolution
        services.AddScoped<IFooRepository, FooRepository>();

        // Capture ServiceLocator snapshot for NuGet self-containment
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods
}
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────┐
│ Application Layer                                │
│                                                  │
│ Depends on:                                      │
│   FooService (public)                            │
│   Public ctor: ILogger<FooService>               │
└────────────────────┬─────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────┐
│ Roadbed.Sdk.FooService                           │
│                                                  │
│   FooService (public sealed)                     │
│       : BaseAsyncCrudlService (Roadbed.Crud)     │
│       Public ctor: ILogger (resolves repo via    │
│           ServiceLocator)                        │
│       Internal ctor: IFooRepository + ILogger    │
│           (for unit tests via InternalsVisibleTo)│
│       Delegates to repository                    │
│                                                  │
│   FooRepository (internal sealed)                │
│       : BaseAsyncCrudlRepository (Roadbed.Crud)  │
│       Injects INetHttpClient (Roadbed.Net)       │
│       Calls MakeHttpRequestAsync<TDto>()         │
│                                                  │
│   InstallFooService                              │
│       : IServiceCollectionInstaller              │
│       Registers IFooRepository for ServiceLocator│
└──────────┬──────────────────┬────────────────────┘
           │                  │
     injects Roadbed.Net inherits Roadbed.Crud
           │                  │
           ▼                  ▼
    INetHttpClient      BaseAsyncCrudlRepository
    NetHttpClient       BaseAsyncCrudlService
    NetHttpRequest      BaseEntityRecord
    NetHttpResponse<T>
```

---

## Common Pitfalls

### 1. Not Checking `IsSuccessStatusCode` Before Accessing `Data`

```csharp
// ❌ Wrong — Data is default! (null for reference types) on failure
var items = response.Data.Items;

// ✅ Correct — check success first (covers HTTP errors AND deserialization errors)
if (response.IsSuccessStatusCode)
{
    var items = response.Data.Items;
}
```

### 2. Catching JsonException Around MakeHttpRequestAsync

```csharp
// ❌ Unnecessary — NetHttpClient catches JsonException internally
try
{
    var response = await this._httpClient.MakeHttpRequestAsync<MyDto>(request, cancellationToken);
    var data = response.Data;
}
catch (JsonException ex)
{
    // This never fires — deserialization errors are wrapped in Failure()
}

// ✅ Correct — the IsSuccessStatusCode check handles everything
var response = await this._httpClient.MakeHttpRequestAsync<MyDto>(request, cancellationToken);

if (!response.IsSuccessStatusCode)
{
    this.LogWarning("Request failed: {Error}", response.Errors.FirstOrDefault());
    return null;
}

return response.Data;
```

### 3. Passing a Raw String Instead of a Uri

```csharp
// ❌ Wrong — HttpEndPoint is typed as Uri?
request.HttpEndPoint = "https://api.example.com/data";  // Won't compile

// ✅ Correct — always create a Uri
request.HttpEndPoint = new Uri("https://api.example.com/data");
```

### 4. Instantiating NetHttpClient Directly

```csharp
// ❌ Wrong — bypasses DI, requires manual IHttpClientFactory and ILogger resolution
var client = new NetHttpClient(factory, logger);
var response = await client.MakeHttpRequestAsync<MyDto>(request, cancellationToken);

// ✅ Correct — inject INetHttpClient via constructor
public FooRepository(
    INetHttpClient httpClient,
    ILogger<FooRepository> logger)
    : base(logger)
{
    ArgumentNullException.ThrowIfNull(httpClient);
    this._httpClient = httpClient;
}
```

### 5. Injecting `NetHttpClient` Instead of `INetHttpClient`

```csharp
// ❌ Wrong — depends on concrete class
public FooRepository(
    NetHttpClient httpClient,
    ILogger<FooRepository> logger)

// ✅ Correct — depends on interface
public FooRepository(
    INetHttpClient httpClient,
    ILogger<FooRepository> logger)
```

### 6. Registering IHttpClientFactory or INetHttpClient Manually

```csharp
// ❌ Wrong — manual registration in Program.cs
builder.Services.AddHttpClient();
builder.Services.AddScoped<INetHttpClient, NetHttpClient>();

// ✅ Correct — auto-discovery handles everything
builder.Services.InstallModulesInAppDomain(builder.Configuration);
// InstallNetHttpClient registers named clients and INetHttpClient automatically
```

### 7. Using System.Text.Json Instead of Newtonsoft.Json

```csharp
// ❌ Wrong — DTOs must use Newtonsoft.Json attributes for automatic deserialization
using System.Text.Json.Serialization;

[JsonPropertyName("items")]
public FooItem[] Items { get; set; }

// ✅ Correct — Roadbed.Net uses Newtonsoft.Json internally
using Newtonsoft.Json;

[JsonProperty("items")]
public FooItem[] Items { get; set; }
```

### 8. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public FooRepository(
    INetHttpClient httpClient,
    ILogger<FooRepository> logger)
    : base(logger)
{
    _httpClient = httpClient;  // Missing this.
}

// ✅ Correct
public FooRepository(
    INetHttpClient httpClient,
    ILogger<FooRepository> logger)
    : base(logger)
{
    this._httpClient = httpClient;
}
```

### 9. Calling `this.Logger.LogDebug()` Instead of `this.LogDebug()`

```csharp
// ❌ Wrong — formats string even if Debug level is disabled
this.Logger.LogDebug("Reading Foo {Id}", id);

// ✅ Correct — checks IsEnabled(LogLevel.Debug) before formatting
this.LogDebug("Reading Foo {Id}", id);
```

### 10. Wrong CancellationToken Position

```csharp
// ❌ Wrong — CancellationToken is not the last parameter
public async Task<Foo?> ReadAsync(
    CancellationToken cancellationToken = default,
    string id = "")

// ✅ Correct — CancellationToken is always last
public async Task<Foo?> ReadAsync(
    string id,
    CancellationToken cancellationToken = default)
```

### 11. Setting Authentication When AuthenticationType Is Unknown

```csharp
// ❌ Wrong — Unknown type means no Authorization header is added
request.Authentication = new NetHttpAuthentication
{
    AuthenticationType = NetHttpAuthenticationType.Unknown,
    Value = "my-api-key",
};

// ✅ Correct — use Bearer or Basic
request.Authentication = new NetHttpAuthentication
{
    AuthenticationType = NetHttpAuthenticationType.Bearer,
    Value = "my-api-key",
};
```

### 12. Registering Service Interfaces in the SDK Installer

Service interfaces are `internal` and the concrete service class is `public`. The installer only registers the repository (for `ServiceLocator` resolution). The consuming application resolves the concrete service class directly.

```csharp
// ❌ Wrong — service interface should not be registered
public sealed class InstallFooService : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFooRepository, FooRepository>();
        services.AddScoped<IFooService, FooService>();
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}

// ✅ Correct — only register repository for ServiceLocator
public sealed class InstallFooService : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFooRepository, FooRepository>();
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 13. Forgetting SetLocatorProvider in the SDK Installer

```csharp
// ❌ Wrong — service's public constructor can't resolve repository via ServiceLocator
public sealed class InstallFooService : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFooRepository, FooRepository>();
        // Missing: ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}

// ✅ Correct
public sealed class InstallFooService : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFooRepository, FooRepository>();
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

---

## Quick Reference

### Using Statements

```csharp
using Roadbed;           // ServiceLocator, BaseClassWithLogging, IServiceCollectionInstaller
using Roadbed.Net;       // INetHttpClient, request/response types, enums
using Newtonsoft.Json;    // DTO attributes (JsonProperty) — deserialization is handled internally
```

### Minimal Request Pattern (DTO Deserialization)

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/resource/123"),
};

NetHttpResponse<MyDto> response =
    await this._httpClient.MakeHttpRequestAsync<MyDto>(request, cancellationToken);

if (response.IsSuccessStatusCode)
{
    // response.Data is already a deserialized MyDto instance
    return response.Data;
}
```

### Raw String Pattern (Manual Deserialization)

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/resource/123"),
};

NetHttpResponse<string> response =
    await this._httpClient.MakeHttpRequestAsync<string>(request, cancellationToken);

if (response.IsSuccessStatusCode)
{
    var dto = JsonConvert.DeserializeObject<MyDto>(response.Data);
}
```

### POST Request Pattern

```csharp
var payload = JsonConvert.SerializeObject(entity);

var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/resource"),
    Method = HttpMethod.Post,
    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
    Authentication = new NetHttpAuthentication
    {
        AuthenticationType = NetHttpAuthenticationType.Bearer,
        Value = this._apiToken,
    },
};

NetHttpResponse<CreateResourceResponse> response =
    await this._httpClient.MakeHttpRequestAsync<CreateResourceResponse>(
        request, cancellationToken);
```

### Custom Retry Configuration

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/resource"),
    TimeoutInSecondsPerAttempt = 30,
    RetryPattern = new NetHttpRetryPattern
    {
        MaxAttempts = 5,
        DelayMultiplierInSeconds = 2,
    },
};
// Backoff: 2^0=1s, 2^1=2s, 2^2=4s, 2^3=8s, 2^4=16s
```

### Disable Retries

```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/resource"),
    RetryPattern = new NetHttpRetryPattern
    {
        MaxAttempts = 0,
        DelayMultiplierInSeconds = 0,
    },
};
```

### Repository Constructor Pattern

```csharp
internal sealed class FooRepository
    : BaseAsyncCrudlRepository<Foo, string>,
      IFooRepository
{
    private readonly INetHttpClient _httpClient;

    public FooRepository(
        INetHttpClient httpClient,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this._httpClient = httpClient;
    }
}
```

### NetHttpRequest Defaults Summary

|Property|Default|
|---|---|
|`Method`|GET|
|`EnableCompression`|`true`|
|`TimeoutInSecondsPerAttempt`|15 seconds|
|`RetryPattern.MaxAttempts`|3|
|`RetryPattern.DelayMultiplierInSeconds`|5|
|`Content`|`null`|
|`Authentication`|`null`|
|`HttpHeaders`|Empty list|