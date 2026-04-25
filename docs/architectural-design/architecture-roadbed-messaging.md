# Roadbed.Messaging Architecture

Roadbed.Messaging provides standardized, strongly-typed message envelopes for pub/sub messaging systems (AWS SNS, AWS SQS, Azure Service Bus, RabbitMQ, etc.). The library is transport-agnostic — it produces and consumes JSON envelopes that any broker can carry.

Messages share a common shape regardless of payload type: a ULID identifier, publisher metadata, type codename, source and envelope timestamps, and a typed payload `T`. This makes routing, filtering, dead-letter inspection, and correlation straightforward across services.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Messaging NuGet package. When a developer asks you to publish or consume messages between services, use this document to construct the envelope, set the publisher, and serialize/deserialize correctly.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Use Newtonsoft.Json** for serialization — not `System.Text.Json`. Envelope properties are decorated with `[JsonProperty]` attributes that `System.Text.Json` ignores.
5. **Always set `MessageTypeCodename`** when publishing — use the constructor overload that takes it. Routing and filtering depend on this value.
6. **Use the ULID identifier `MessageMessageRequest`/`Response` generates** — do not pass your own `Guid` or sequential ID. ULIDs are lexicographically sortable by creation time.
7. **Set `OriginalRequestIdentifier` on response messages** to correlate them back to the originating request.
8. **Construct one `MessagingPublisher` per process** and reuse it for every message that process publishes — its `Identifier` is the per-instance ULID, its `Name` is the `CommonBusinessKey` describing the service.
9. **Always validate `Data` is not null before processing** on the consumer side — `Data` is nullable to allow envelope-only messages.
10. **Flatten namespaces** — only `using Roadbed.Messaging;` is needed. The `.Entities` suffix was removed on purpose.
11. **Payload types `T` should be plain POCOs** with `[JsonProperty]` attributes. Avoid records with non-default constructors unless you've configured Newtonsoft.Json to handle them.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-messaging.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-messaging.md#type-catalog)
3. [Package Relationship](architecture-roadbed-messaging.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-messaging.md#namespace-convention)
5. [Envelope Anatomy](architecture-roadbed-messaging.md#envelope-anatomy)
    - [BaseMessagingMessage\<T\>](architecture-roadbed-messaging.md#basemessagingmessaget)
    - [MessagingMessageRequest\<T\>](architecture-roadbed-messaging.md#messagingmessagerequestt)
    - [MessagingMessageResponse\<T\>](architecture-roadbed-messaging.md#messagingmessageresponset)
    - [MessagingPublisher](architecture-roadbed-messaging.md#messagingpublisher)
6. [JSON Wire Format](architecture-roadbed-messaging.md#json-wire-format)
7. [ULID Identifiers](architecture-roadbed-messaging.md#ulid-identifiers)
8. [Type Codename Conventions](architecture-roadbed-messaging.md#type-codename-conventions)
9. [Implementation Walkthrough](architecture-roadbed-messaging.md#implementation-walkthrough)
10. [Common Pitfalls](architecture-roadbed-messaging.md#common-pitfalls)
11. [Quick Reference](architecture-roadbed-messaging.md#quick-reference)

---

## Type Catalog

Roadbed.Messaging contains **4 public types**.

| Type                          | Kind           | Namespace           | Purpose                                                                             |
| ----------------------------- | -------------- | ------------------- | ----------------------------------------------------------------------------------- |
| `BaseMessagingMessage<T>`     | Abstract class | `Roadbed.Messaging` | Shared envelope shape: identifier, publisher, type codename, timestamps, payload.   |
| `MessagingMessageRequest<T>`  | Class          | `Roadbed.Messaging` | Concrete envelope for messages sent to a system (commands, events).                 |
| `MessagingMessageResponse<T>` | Class          | `Roadbed.Messaging` | Concrete envelope for replies. Adds `OriginalRequestIdentifier` for correlation.    |
| `MessagingPublisher`          | Class          | `Roadbed.Messaging` | Identifies the publishing process: per-instance ULID + service name (BusinessKey).  |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Your Service / SDK                                           │
│                                                              │
│   Publishes:  MessagingMessageRequest<TPayload>             │
│   Consumes:   MessagingMessageRequest<TPayload>             │
│   Responds:   MessagingMessageResponse<TPayload>            │
│   Identifies: MessagingPublisher                            │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Messaging                                            │
│                                                              │
│   BaseMessagingMessage<T>     (abstract base envelope)       │
│   MessagingMessageRequest<T>  (commands/events to a system)  │
│   MessagingMessageResponse<T> (replies, with correlation)    │
│   MessagingPublisher          (sender identity)              │
└──────────┬───────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.Common                                               │
│                                                              │
│   CommonBusinessKey   (used as MessagingPublisher.Name)      │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ External Dependencies                                        │
│                                                              │
│   Newtonsoft.Json   (envelope serialization)                 │
│   Ulid              (identifier generation)                  │
└──────────────────────────────────────────────────────────────┘
```

Roadbed.Messaging does not bind to any specific broker. The application brings its own AWS, Azure, or RabbitMQ client and calls `JsonConvert.SerializeObject(message)` / `DeserializeObject<T>(body)` around it.

---

## Namespace Convention

| Namespace            | Contains            |
| -------------------- | ------------------- |
| `Roadbed.Messaging`  | All 4 public types  |

The original `Roadbed.Messaging.Entities` namespace was removed on purpose. Consuming code only ever needs `using Roadbed.Messaging;`.

---

## Envelope Anatomy

### BaseMessagingMessage\<T\>

Abstract base providing the shared envelope shape. Both request and response envelopes inherit from it. Not constructible directly.

```csharp
namespace Roadbed.Messaging;

public abstract class BaseMessagingMessage<T>
{
    protected BaseMessagingMessage(MessagingPublisher publisher);
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename);
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data);
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data, DateTimeOffset createdOn);

    [JsonProperty("message_identifier")]
    public string? Identifier { get; internal set; }

    [JsonProperty("message_type", NullValueHandling = NullValueHandling.Ignore)]
    public string? MessageTypeCodename { get; set; }

    [JsonProperty("publisher", NullValueHandling = NullValueHandling.Ignore)]
    public MessagingPublisher Publisher { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public T? Data { get; set; }

    [JsonProperty("message_create_on")]
    public DateTimeOffset? CreatedOn { get; internal set; }

    [JsonProperty("source_create_on", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? SourceCreatedOn { get; set; }
}
```

**Constructor matrix:**

| Constructor                                              | `Identifier`         | `CreatedOn`        | `SourceCreatedOn`  | `Data`         |
| -------------------------------------------------------- | -------------------- | ------------------ | ------------------ | -------------- |
| `(publisher)`                                            | New ULID             | `UtcNow`           | `UtcNow`           | `null`         |
| `(publisher, typeCodename)`                              | New ULID             | `UtcNow`           | `UtcNow`           | `null`         |
| `(publisher, typeCodename, identifier, data)`            | Caller-supplied      | `UtcNow`           | `UtcNow`           | Caller-supplied |
| `(publisher, typeCodename, identifier, data, createdOn)` | Caller-supplied      | `UtcNow`           | Caller-supplied    | Caller-supplied |

**Property semantics:**

| Property              | Purpose                                                                                            |
| --------------------- | -------------------------------------------------------------------------------------------------- |
| `Identifier`          | Unique per-message ULID. `internal set` — do not assign after construction.                        |
| `MessageTypeCodename` | Routing/filter key. Use dot-notation (e.g., `"order.created"`).                                    |
| `Publisher`           | Identifies the sending process and service.                                                        |
| `Data`                | The typed payload. Nullable to permit envelope-only messages.                                       |
| `CreatedOn`           | When the envelope was constructed. Always `UtcNow` at construction time. `internal set`.            |
| `SourceCreatedOn`     | When the underlying domain event happened. May predate `CreatedOn` if the envelope wraps a stored event. |

### MessagingMessageRequest\<T\>

Concrete envelope for messages sent **to** a system — commands, events, notifications:

```csharp
namespace Roadbed.Messaging;

public class MessagingMessageRequest<T>
    : BaseMessagingMessage<T>
{
    public MessagingMessageRequest(MessagingPublisher publisher);
    public MessagingMessageRequest(MessagingPublisher publisher, string typeCodename);
    public MessagingMessageRequest(MessagingPublisher publisher, string typeCodename, string identifier, T data);
}
```

Adds nothing on top of `BaseMessagingMessage<T>` — the type itself is the marker that this is an inbound/outbound request, not a reply.

### MessagingMessageResponse\<T\>

Concrete envelope for replies. Adds the correlation property `OriginalRequestIdentifier`:

```csharp
namespace Roadbed.Messaging;

public class MessagingMessageResponse<T>
    : BaseMessagingMessage<T>
{
    public MessagingMessageResponse(MessagingPublisher publisher);
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename);
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename, string identifier, T data);
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename, string identifier, T data, DateTimeOffset createdOn);

    public string? OriginalRequestIdentifier { get; set; }
}
```

**Always populate `OriginalRequestIdentifier`** with the `Identifier` of the request being responded to. This is the only way consumers can correlate the reply with the request.

### MessagingPublisher

Identifies a publishing process:

```csharp
namespace Roadbed.Messaging;

public class MessagingPublisher
{
    public MessagingPublisher();
    public MessagingPublisher(CommonBusinessKey name);
    public MessagingPublisher(CommonBusinessKey name, string identifier);

    [JsonProperty("publisher_identifier", NullValueHandling = NullValueHandling.Ignore)]
    public string Identifier { get; set; }

    [JsonProperty("publisher_name", NullValueHandling = NullValueHandling.Ignore)]
    public CommonBusinessKey? Name { get; set; }
}
```

| Property     | Meaning                                                                                                   |
| ------------ | --------------------------------------------------------------------------------------------------------- |
| `Identifier` | Unique identifier of the publishing **instance** (process, container, machine). Defaults to a new ULID.   |
| `Name`       | Logical service identity — a `CommonBusinessKey` whose `Key` is a stable codename and `Value` is a label. |

**Recommended construction pattern:** create one publisher at process startup and reuse it for the lifetime of the process. The `Identifier` should distinguish this instance from other instances of the same service.

---

## JSON Wire Format

The envelope serializes to a stable, snake-cased JSON shape. Every property uses `NullValueHandling.Ignore` except `message_identifier` and `message_create_on`, so missing optional fields are absent rather than `null`.

### Request envelope

```json
{
  "message_identifier": "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
  "message_type": "foo.created",
  "publisher": {
    "publisher_identifier": "01HQRS5J0BCXQRPL3JMVQGV1WZ",
    "publisher_name": {
      "key": "foo-service",
      "value": "FooService"
    }
  },
  "message_create_on": "2026-01-15T14:30:00Z",
  "source_create_on": "2026-01-15T14:29:58Z",
  "data": {
    "FooId": 12345,
    "Name": "Bar"
  }
}
```

### Response envelope (with correlation)

```json
{
  "message_identifier": "01HQRS8M7NKXYZ1A2B3C4D5E6F",
  "message_type": "foo.processed",
  "original_request_identifier": "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
  "publisher": {
    "publisher_identifier": "01HQRS7L4DEFGHJK2N3P4Q5R6S",
    "publisher_name": {
      "key": "bar-service",
      "value": "BarService"
    }
  },
  "message_create_on": "2026-01-15T14:30:05Z",
  "data": {
    "FooId": 12345,
    "Result": "success"
  }
}
```

Every envelope property uses snake_case on the wire. Properties marked with `NullValueHandling.Ignore` (including `original_request_identifier`) are omitted entirely when their value is null, so request envelopes do not emit an empty correlation field.

---

## ULID Identifiers

Identifiers (both `Identifier` on the message and `Identifier` on the publisher) are [ULIDs](https://github.com/ulid/spec) — 26-character lexicographically sortable strings.

| Property         | ULID                                | Equivalent GUID                              |
| ---------------- | ----------------------------------- | -------------------------------------------- |
| Length           | 26 chars                            | 36 chars                                     |
| Sort order       | Chronological by creation time      | Random (no ordering meaning)                 |
| Encoding         | Crockford Base32 (URL-safe)         | Hex with hyphens                             |
| Timestamp        | First 48 bits = ms since Unix epoch | None                                         |
| Random component | 80 bits                             | 122 bits                                     |

**Why this matters operationally:** when scrolling a dead-letter queue or a log of message identifiers, sorting by ID also sorts by time. No need to join identifiers against timestamps to find "the most recent failures."

```csharp
var id1 = Ulid.NewUlid().ToString();   // e.g., 01HQRS6K2MFXVW8N9PQ2T3Y4Z5
await Task.Delay(100);
var id2 = Ulid.NewUlid().ToString();   // e.g., 01HQRS6K5GNYXM0PR4S8VBC3DA
// String.Compare(id1, id2) < 0 — id1 sorts before id2
```

---

## Type Codename Conventions

`MessageTypeCodename` is a free-form string but should follow a stable dot-notation pattern so consumers can subscribe with prefix or wildcard rules.

| Pattern                  | Examples                                                                          |
| ------------------------ | --------------------------------------------------------------------------------- |
| `entity.action`          | `foo.created`, `foo.updated`, `foo.deleted`                                       |
| `entity.action.outcome`  | `bar.processed.success`, `bar.processed.failure`                                  |
| `domain.entity.action`   | `inventory.foo.restocked`, `billing.bar.invoiced`                                 |

Whatever pattern you pick, **be consistent across services** in the same system. Mixed casing (`Foo.Created` vs `foo.created`) breaks broker-side filtering.

---

## Implementation Walkthrough

This walkthrough shows a request/response message exchange between a `FooService` (publisher of work) and a `BarService` (consumer that responds).

### Step 1: Define payload POCOs

```csharp
namespace MyApp.Contracts;

using Newtonsoft.Json;

public sealed class FooCreatedPayload
{
    [JsonProperty("foo_id")]
    public long FooId { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }
}

public sealed class FooProcessedPayload
{
    [JsonProperty("foo_id")]
    public long FooId { get; set; }

    [JsonProperty("result")]
    public string? Result { get; set; }
}
```

### Step 2: Construct one publisher per process

```csharp
namespace MyApp.FooService;

using Roadbed;
using Roadbed.Messaging;

public sealed class FooMessagingHost
{
    public static MessagingPublisher CreatePublisher()
    {
        return new MessagingPublisher(
            name: new CommonBusinessKey("foo-service", "FooService"),
            identifier: Environment.MachineName);
    }
}
```

Register the publisher as a singleton in DI so every component that publishes uses the same instance.

### Step 3: Publish a request

```csharp
public sealed class FooPublisher
{
    private readonly MessagingPublisher _publisher;
    private readonly IMyBroker _broker;

    public FooPublisher(MessagingPublisher publisher, IMyBroker broker)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(broker);

        this._publisher = publisher;
        this._broker = broker;
    }

    public async Task PublishFooCreatedAsync(
        FooCreatedPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var message = new MessagingMessageRequest<FooCreatedPayload>(
            this._publisher,
            "foo.created")
        {
            Data = payload,
        };

        string json = JsonConvert.SerializeObject(message);

        await this._broker.PublishAsync(json, cancellationToken);
    }
}
```

### Step 4: Consume the request and reply

```csharp
public sealed class BarConsumer
{
    private readonly MessagingPublisher _publisher;
    private readonly IMyBroker _broker;

    public BarConsumer(MessagingPublisher publisher, IMyBroker broker)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(broker);

        this._publisher = publisher;
        this._broker = broker;
    }

    public async Task HandleAsync(string body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var request = JsonConvert.DeserializeObject<MessagingMessageRequest<FooCreatedPayload>>(body);

        // Always validate Data is not null before processing
        if (request?.Data is null)
        {
            return;
        }

        // ...do work...
        var resultPayload = new FooProcessedPayload
        {
            FooId = request.Data.FooId,
            Result = "success",
        };

        var response = new MessagingMessageResponse<FooProcessedPayload>(
            this._publisher,
            "foo.processed")
        {
            Data = resultPayload,
            OriginalRequestIdentifier = request.Identifier,
        };

        string responseJson = JsonConvert.SerializeObject(response);
        await this._broker.PublishAsync(responseJson, cancellationToken);
    }
}
```

---

## Common Pitfalls

### 1. Using `System.Text.Json` Instead of Newtonsoft.Json

```csharp
// ❌ Wrong — System.Text.Json ignores [JsonProperty] attributes
using System.Text.Json;

string json = JsonSerializer.Serialize(message);
// Produces "Identifier" instead of "message_identifier", breaks the wire format

// ✅ Correct
using Newtonsoft.Json;

string json = JsonConvert.SerializeObject(message);
```

### 2. Forgetting to Set `OriginalRequestIdentifier` on Responses

```csharp
// ❌ Wrong — consumer can't correlate the reply with the request
var response = new MessagingMessageResponse<FooProcessedPayload>(
    this._publisher,
    "foo.processed")
{
    Data = resultPayload,
};

// ✅ Correct
var response = new MessagingMessageResponse<FooProcessedPayload>(
    this._publisher,
    "foo.processed")
{
    Data = resultPayload,
    OriginalRequestIdentifier = request.Identifier,
};
```

### 3. Not Setting `MessageTypeCodename`

```csharp
// ❌ Wrong — broker filters and dead-letter triage have no routing key
var message = new MessagingMessageRequest<FooCreatedPayload>(this._publisher)
{
    Data = payload,
};

// ✅ Correct — use the constructor overload that takes typeCodename
var message = new MessagingMessageRequest<FooCreatedPayload>(
    this._publisher,
    "foo.created")
{
    Data = payload,
};
```

### 4. Skipping the Null-Check on `Data` After Deserialization

```csharp
// ❌ Wrong — Data is nullable; envelope-only messages are valid
var request = JsonConvert.DeserializeObject<MessagingMessageRequest<FooCreatedPayload>>(body);
var fooId = request.Data.FooId;  // NullReferenceException if Data is null

// ✅ Correct
var request = JsonConvert.DeserializeObject<MessagingMessageRequest<FooCreatedPayload>>(body);
if (request?.Data is null)
{
    return;
}
var fooId = request.Data.FooId;
```

### 5. Building a New `MessagingPublisher` Per Message

```csharp
// ❌ Wrong — every message gets a different publisher Identifier ULID,
// so you cannot correlate messages that came from the same instance.
public async Task PublishAsync(FooCreatedPayload payload, CancellationToken ct)
{
    var publisher = new MessagingPublisher(
        new CommonBusinessKey("foo-service", "FooService"));
    var message = new MessagingMessageRequest<FooCreatedPayload>(publisher, "foo.created")
    {
        Data = payload,
    };
    // ...
}

// ✅ Correct — inject one publisher constructed at startup
private readonly MessagingPublisher _publisher;

public FooPublisher(MessagingPublisher publisher)
{
    ArgumentNullException.ThrowIfNull(publisher);
    this._publisher = publisher;
}

public async Task PublishAsync(FooCreatedPayload payload, CancellationToken ct)
{
    var message = new MessagingMessageRequest<FooCreatedPayload>(this._publisher, "foo.created")
    {
        Data = payload,
    };
    // ...
}
```

### 6. Assigning Your Own GUID to `Identifier`

```csharp
// ❌ Wrong — Identifier is internal-set on BaseMessagingMessage
var message = new MessagingMessageRequest<FooCreatedPayload>(this._publisher, "foo.created")
{
    Identifier = Guid.NewGuid().ToString(),  // Won't compile from outside the assembly
    Data = payload,
};

// ✅ Correct — use the constructor that accepts an identifier (still ULID-shaped)
var message = new MessagingMessageRequest<FooCreatedPayload>(
    this._publisher,
    "foo.created",
    identifier: Ulid.NewUlid().ToString(),
    data: payload);
```

### 7. Mixing Casing in `MessageTypeCodename`

```csharp
// ❌ Wrong — broker-side prefix filters won't match consistently
"Foo.Created"
"foo.created"
"FOO_CREATED"

// ✅ Correct — pick one style and stay there
"foo.created"
"foo.updated"
"foo.deleted"
```

---

## Quick Reference

### Using statements

```csharp
using Newtonsoft.Json;       // JsonConvert.SerializeObject / DeserializeObject
using Roadbed;               // CommonBusinessKey
using Roadbed.Messaging;     // BaseMessagingMessage, request/response, publisher
```

### Construct a publisher (once per process)

```csharp
var publisher = new MessagingPublisher(
    name: new CommonBusinessKey("foo-service", "FooService"),
    identifier: Environment.MachineName);
```

### Publish a request

```csharp
var message = new MessagingMessageRequest<FooPayload>(publisher, "foo.created")
{
    Data = payload,
};

string json = JsonConvert.SerializeObject(message);
await broker.PublishAsync(json, cancellationToken);
```

### Consume a request

```csharp
var request = JsonConvert.DeserializeObject<MessagingMessageRequest<FooPayload>>(body);

if (request?.Data is null)
{
    return;
}

// ... process request.Data ...
```

### Reply with correlation

```csharp
var response = new MessagingMessageResponse<BarPayload>(publisher, "bar.processed")
{
    Data = barPayload,
    OriginalRequestIdentifier = request.Identifier,
};

string json = JsonConvert.SerializeObject(response);
await broker.PublishAsync(json, cancellationToken);
```

### Wire-format property names

| C# property                            | JSON name                     |
| -------------------------------------- | ----------------------------- |
| `Identifier`                           | `message_identifier`          |
| `MessageTypeCodename`                  | `message_type`                |
| `Publisher`                            | `publisher`                   |
| `Data`                                 | `data`                        |
| `CreatedOn`                            | `message_create_on`           |
| `SourceCreatedOn`                      | `source_create_on`            |
| `OriginalRequestIdentifier` (response) | `original_request_identifier` |
| `MessagingPublisher.Identifier`        | `publisher_identifier`        |
| `MessagingPublisher.Name`              | `publisher_name`              |
