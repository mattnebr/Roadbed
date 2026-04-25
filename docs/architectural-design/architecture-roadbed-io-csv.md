# Roadbed.IO.Csv Architecture

Roadbed.IO.Csv provides a strongly-typed CSV file handler — `IoCsvFile<T>` — that maps CSV rows to and from POCO instances using [CsvHelper](https://joshclose.github.io/CsvHelper/). It is built on top of Roadbed.IO and inherits its `IoFile` save semantics.

The mapping itself is supplied by the consumer through `ICsvEntityMapper<T>`. The library does not auto-discover mappings or use reflection-based class maps — every consumer writes one mapper per row type, which keeps the deserialization explicit and easy to debug.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.IO.Csv NuGet package. When a developer asks you to read, transform, or write CSV files in a typed manner, use this document together with the [Roadbed.IO Architecture](architecture-roadbed-io.md).

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Implement `ICsvEntityMapper<T>`** for every row type — no auto-mapping. The mapper reads named fields from `CsvReader`.
5. **Construct `IoCsvFile<T>` via the static `From*` factory methods**, never via the protected constructors. Available factories: `FromFile`, `FromFileAsync`, `FromString`, `FromStringAsync`.
6. **`From*` methods load `DataRows` synchronously as part of construction.** Do not call them on a hot path expecting lazy evaluation.
7. **The path-backed constructor enforces a `.csv` extension** (case-insensitive). `From*` methods that take a path inherit this requirement.
8. **Use `ExportDataRowsAsContentString()`** to get the CSV text without writing a file. Use `Save()` / `SaveAsync()` to write to the path captured by the constructor's `IoFileInfo`.
9. **The default `CsvConfiguration`** uses `InvariantCulture`, comma delimiter, UTF-8 encoding, and a header record. Override only when the source/destination demands it.
10. **Flatten namespaces** — only `using Roadbed.IO;` is needed (the project's `RootNamespace` is `Roadbed.IO`, not `Roadbed.IO.Csv`).
11. **`MapEntity` returning null skips the row.** The framework treats null mapper output as "do not include this row in `DataRows`."

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-io-csv.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-io-csv.md#type-catalog)
3. [Package Relationship](architecture-roadbed-io-csv.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-io-csv.md#namespace-convention)
5. [ICsvEntityMapper\<T\>](architecture-roadbed-io-csv.md#icsventitymappert)
6. [IoCsvFile\<T\>](architecture-roadbed-io-csv.md#iocsvfilet)
    - [Construction](architecture-roadbed-io-csv.md#construction)
    - [Load Methods](architecture-roadbed-io-csv.md#load-methods)
    - [Export Methods](architecture-roadbed-io-csv.md#export-methods)
    - [Save Methods](architecture-roadbed-io-csv.md#save-methods)
    - [Default CsvConfiguration](architecture-roadbed-io-csv.md#default-csvconfiguration)
7. [Implementation Walkthrough](architecture-roadbed-io-csv.md#implementation-walkthrough)
8. [Common Pitfalls](architecture-roadbed-io-csv.md#common-pitfalls)
9. [Quick Reference](architecture-roadbed-io-csv.md#quick-reference)

---

## Type Catalog

Roadbed.IO.Csv contains **2 public types**.

| Type                  | Kind          | Namespace    | Purpose                                                                                                |
| --------------------- | ------------- | ------------ | ------------------------------------------------------------------------------------------------------ |
| `ICsvEntityMapper<T>` | Interface     | `Roadbed.IO` | Contract for mapping a single CSV row (via `CsvReader`) into a typed entity.                           |
| `IoCsvFile<T>`        | Generic class | `Roadbed.IO` | Inherits `IoFile`. Holds the in-memory `DataRows` and provides factories to load from file or string.  |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Application code                                             │
│                                                              │
│   Implements ICsvEntityMapper<T>                             │
│   Calls IoCsvFile<T>.FromFile / FromString                   │
│   Iterates DataRows; manipulates collection; calls Save     │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.IO.Csv                                               │
│                                                              │
│   IoCsvFile<T>           : IoFile                            │
│   ICsvEntityMapper<T>                                        │
└──────────┬───────────────────────────────────────────────────┘
           │ inherits
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.IO                                                   │
│                                                              │
│   IoFile      (Save, SaveAsync, ValidateFileInfo)            │
│   IoFileInfo  (FullPath, Extension)                          │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ External Dependencies                                        │
│                                                              │
│   CsvHelper   (CsvReader, CsvWriter, CsvConfiguration)       │
└──────────────────────────────────────────────────────────────┘
```

The library is a thin shell over CsvHelper that adds:

- A typed in-memory representation (`DataRows : IList<T>`)
- A static-factory pattern for synchronous and asynchronous loading
- Mandatory `.csv` extension validation when path-backed
- A consistent default `CsvConfiguration`

---

## Namespace Convention

| Namespace    | Contains                                  |
| ------------ | ----------------------------------------- |
| `Roadbed.IO` | `IoCsvFile<T>`, `ICsvEntityMapper<T>`     |

The Roadbed.IO.Csv project sets `<RootNamespace>Roadbed.IO</RootNamespace>` deliberately so that `using Roadbed.IO;` covers both `IoFile`/`IoFileInfo` (from Roadbed.IO) and `IoCsvFile<T>`/`ICsvEntityMapper<T>` (from Roadbed.IO.Csv). Consumers do not need a separate `using Roadbed.IO.Csv;`.

---

## ICsvEntityMapper\<T\>

Contract for mapping one CSV row to one entity:

```csharp
namespace Roadbed.IO;

using CsvHelper;

public interface ICsvEntityMapper<out T>
{
    T? MapEntity(CsvReader reader);
}
```

| Property/Method                  | Notes                                                                                          |
| -------------------------------- | ---------------------------------------------------------------------------------------------- |
| `T? MapEntity(CsvReader reader)` | Reader is positioned at the current row. Use `reader.GetField<TField>("ColumnName")` to read.  |
| Return value `null`              | Tells `IoCsvFile<T>` to skip this row.                                                         |

Mapper instances are typically stateless and singleton-friendly. Constructing one per `IoCsvFile<T>` is fine because they are tiny.

---

## IoCsvFile\<T\>

Generic file handler that inherits `IoFile`:

```csharp
namespace Roadbed.IO;

using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper.Configuration;

public class IoCsvFile<T>
    : IoFile
{
    // Construction (factories preferred — see below)
    protected IoCsvFile(ICsvEntityMapper<T> dataMapper);
    protected IoCsvFile(IoFileInfo fileInfo, ICsvEntityMapper<T> dataMapper);

    public ICsvEntityMapper<T>? DataMapper { get; set; }
    public IList<T> DataRows { get; set; }

    // Factories
    public static IoCsvFile<T> FromFile(string path, ICsvEntityMapper<T> dataMapper);
    public static Task<IoCsvFile<T>> FromFileAsync(string path, ICsvEntityMapper<T> dataMapper);
    public static IoCsvFile<T> FromString(string content, ICsvEntityMapper<T> dataMapper);
    public static Task<IoCsvFile<T>> FromStringAsync(string content, ICsvEntityMapper<T> dataMapper);

    // Load (called by factories; rarely needed externally)
    public void LoadDataRowsFromFile();
    public Task LoadDataRowsFromFileAsync();
    public void LoadDataRowsFromString(string content);
    public Task LoadDataRowsFromStringAsync(string content);

    // Export and Save
    public string ExportDataRowsAsContentString();
    public string ExportDataRowsAsContentString(CsvConfiguration configuration);

    public string Save();
    public string Save(CsvConfiguration configuration);
    public Task<string> SaveAsync();
    public Task<string> SaveAsync(CsvConfiguration configuration);
}
```

### Construction

The two protected constructors are not meant for direct use. **Always go through a factory.**

| Factory                                | Use when                                                          | Calls                              |
| -------------------------------------- | ----------------------------------------------------------------- | ---------------------------------- |
| `FromFile(path, mapper)`               | Loading from disk synchronously                                   | `LoadDataRowsFromFile`             |
| `FromFileAsync(path, mapper)`          | Loading from disk asynchronously                                  | `LoadDataRowsFromFileAsync`        |
| `FromString(content, mapper)`          | Loading from an in-memory CSV string synchronously                | `LoadDataRowsFromString`           |
| `FromStringAsync(content, mapper)`     | Loading from an in-memory CSV string asynchronously               | `LoadDataRowsFromStringAsync`      |

**Every factory:**

- Throws `ArgumentException` for null/whitespace `path` or `content`
- Throws `ArgumentNullException` for null `dataMapper`
- File-based factories throw `ArgumentException` if the path's extension is not `.csv` (case-insensitive)

### Load Methods

The four `LoadDataRows*` methods are public so callers can re-load after mutating `FileInfo` or `DataMapper`. Each method:

```
Validate FileInfo (file-based only)  via IoFile.ValidateFileInfo
Validate DataMapper                  via ArgumentNullException.ThrowIfNull
    │
    ▼
Reset this.DataRows to a new empty List<T>
    │
    ▼
Open CsvReader (file: FileStream → StreamReader → CsvReader, string: StringReader → CsvReader)
Read header row
    │
    ▼
For each subsequent row:
    var entity = this.DataMapper.MapEntity(csvReader);
    if (entity is not null) this.DataRows.Add(entity);
```

CsvHelper is configured with `CultureInfo.InvariantCulture` for load operations. This is intentional and not configurable through the load APIs — invariant culture is the safest default for reading numeric and date values across locales.

### Export Methods

Convert `DataRows` back to a CSV-formatted string without writing a file:

```csharp
string csv = file.ExportDataRowsAsContentString();
// Uses GetDefaultConfiguration() — invariant culture, comma, UTF-8, header record.

string csv = file.ExportDataRowsAsContentString(customConfig);
// Uses your CsvConfiguration.
```

Implementation: writes records to an in-memory `MemoryStream` via `CsvWriter`, then UTF-8-decodes the bytes. Returns `string.Empty` when `DataRows` or `configuration` is null.

### Save Methods

Combine `Export*` + the inherited `IoFile.Save` / `IoFile.SaveAsync`:

| Method                     | Effect                                                                                         |
| -------------------------- | ---------------------------------------------------------------------------------------------- |
| `Save()`                   | Exports with default config, writes to `FileInfo.FullPath`, returns the path.                  |
| `Save(CsvConfiguration)`   | Exports with the supplied config, writes, returns the path.                                    |
| `SaveAsync()`              | Asynchronous variant of `Save()`.                                                              |
| `SaveAsync(CsvConfiguration)` | Asynchronous variant of `Save(CsvConfiguration)`.                                            |

All four methods inherit the `IoFile.Save` semantic of returning `string.Empty` when there is no content to write.

### Default CsvConfiguration

`GetDefaultConfiguration()` produces:

```csharp
new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    Delimiter = ",",
    Encoding = Encoding.UTF8,
};
```

| Setting           | Default               | When to override                                        |
| ----------------- | --------------------- | ------------------------------------------------------- |
| Culture           | `InvariantCulture`    | When the source uses a culture-specific decimal separator |
| `HasHeaderRecord` | `true`                | When writing a headerless CSV                           |
| `Delimiter`       | `,`                   | When the source uses semicolon, tab, or pipe            |
| `Encoding`        | UTF-8                 | When interoperating with legacy systems requiring a different encoding |

---

## Implementation Walkthrough

This walkthrough builds a typed CSV pipeline for a `Foo` entity, reads from a file, transforms, and writes back out.

### Step 1: Define the row entity

```csharp
namespace MyApp.Foo;

public sealed class Foo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
}
```

The entity is a plain POCO. It does not need attributes — the mapper handles column-to-property assignment.

### Step 2: Implement the mapper

```csharp
namespace MyApp.Foo;

using CsvHelper;
using Roadbed.IO;

public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    public Foo? MapEntity(CsvReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new Foo
        {
            Id = reader.GetField<int>("Id"),
            Name = reader.GetField<string>("Name"),
            Price = reader.GetField<decimal>("Price"),
        };
    }
}
```

Return `null` instead of constructing a `Foo` if you want the framework to skip a row (e.g., header repetitions, comment markers).

### Step 3: Read a CSV from disk

```csharp
namespace MyApp.Foo;

using System.Threading.Tasks;
using Roadbed.IO;

public sealed class FooImporter
{
    public async Task<int> ImportAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var mapper = new FooCsvMapper();
        var file = await IoCsvFile<Foo>.FromFileAsync(path, mapper);

        foreach (var foo in file.DataRows)
        {
            // Process each foo
        }

        return file.DataRows.Count;
    }
}
```

### Step 4: Read a CSV from an in-memory string

Useful for unit tests or when the CSV is fetched from a remote service:

```csharp
string content = """
Id,Name,Price
1,Widget,9.99
2,Gadget,19.99
""";

var mapper = new FooCsvMapper();
var file = IoCsvFile<Foo>.FromString(content, mapper);
// file.DataRows now has two Foo entries
```

### Step 5: Mutate and write back

```csharp
public async Task<string> ApplyDiscountAsync(string sourcePath, string targetPath, decimal discount)
{
    var mapper = new FooCsvMapper();
    var file = await IoCsvFile<Foo>.FromFileAsync(sourcePath, mapper);

    foreach (var foo in file.DataRows)
    {
        foo.Price *= (1 - discount);
    }

    // Repoint the file at the target path before saving
    file.FileInfo = new IoFileInfo(targetPath);

    return await file.SaveAsync();
    // Returns targetPath
}
```

### Step 6: Export to a string instead of a file

```csharp
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\foos.csv", new FooCsvMapper());

// Mutate file.DataRows ...

string csvContent = file.ExportDataRowsAsContentString();
// Send to an HTTP endpoint, log, etc., without touching disk.
```

### Step 7: Use a custom delimiter / encoding

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    Delimiter = ";",
    Encoding = Encoding.UTF8,
};

string semicolonCsv = file.ExportDataRowsAsContentString(config);
```

---

## Common Pitfalls

### 1. Constructing `IoCsvFile<T>` Directly

```csharp
// ❌ Wrong — constructors are protected; this won't compile from outside the assembly
var file = new IoCsvFile<Foo>(new IoFileInfo(@"C:\Data\foos.csv"), new FooCsvMapper());

// ✅ Correct — use a factory
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\foos.csv", new FooCsvMapper());
```

### 2. Loading a Non-`.csv` File via `FromFile`

```csharp
// ❌ Wrong — throws ArgumentException because extension isn't ".csv"
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\foos.txt", new FooCsvMapper());

// ✅ Correct — rename, or use FromString after reading the file yourself
string content = File.ReadAllText(@"C:\Data\foos.txt");
var file = IoCsvFile<Foo>.FromString(content, new FooCsvMapper());
```

### 3. Returning a Default-Initialized Entity Instead of Null to Skip

```csharp
// ❌ Wrong — non-null return adds a row of zeros/nulls to DataRows
public Foo? MapEntity(CsvReader reader)
{
    if (reader.GetField<string>("Status") == "ARCHIVED")
    {
        return new Foo();  // ends up in DataRows as garbage
    }
    return new Foo { /* ... */ };
}

// ✅ Correct — return null to skip
public Foo? MapEntity(CsvReader reader)
{
    if (reader.GetField<string>("Status") == "ARCHIVED")
    {
        return null;
    }
    return new Foo { /* ... */ };
}
```

### 4. Writing to the Source Path When You Meant to Save Elsewhere

```csharp
// ❌ Wrong — Save uses the FileInfo passed at construction
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\source.csv", mapper);
// ... mutate DataRows ...
file.Save();  // Overwrites source.csv

// ✅ Correct — repoint FileInfo before saving
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\source.csv", mapper);
// ... mutate DataRows ...
file.FileInfo = new IoFileInfo(@"C:\Data\output.csv");
file.Save();
```

### 5. Calling `Save()` After `FromString`

```csharp
// ❌ Wrong — FromString does not assign FileInfo; Save throws
var file = IoCsvFile<Foo>.FromString(content, mapper);
file.Save();  // throws ArgumentNullException — FileInfo is null

// ✅ Correct — assign FileInfo first, or use ExportDataRowsAsContentString
var file = IoCsvFile<Foo>.FromString(content, mapper);
file.FileInfo = new IoFileInfo(@"C:\Data\output.csv");
file.Save();

// Or, if you only need the string:
string csv = file.ExportDataRowsAsContentString();
```

### 6. Reusing a Single Mapper Across Threads When the Mapper Is Stateful

```csharp
// ❌ Wrong if the mapper has mutable state (e.g., a row counter)
public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    private int _rowsSeen;

    public Foo? MapEntity(CsvReader reader)
    {
        this._rowsSeen++;  // Race condition under concurrent loads
        return /* ... */;
    }
}

// ✅ Correct — keep mappers stateless, or create a new one per load
public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    public Foo? MapEntity(CsvReader reader) => /* pure function of reader */;
}
```

### 7. Expecting Concurrent Reads After `LoadDataRowsFromFile`

```csharp
// ❌ Wrong — DataRows is replaced on every Load* call; references go stale
var file = IoCsvFile<Foo>.FromFile(path, mapper);
IList<Foo> firstRef = file.DataRows;
file.LoadDataRowsFromFile();              // replaces DataRows
// firstRef now points at the OLD list, not what you'd expect

// ✅ Correct — read DataRows after each Load
file.LoadDataRowsFromFile();
foreach (var foo in file.DataRows) { /* ... */ }
```

### 8. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    private readonly IFooDecorator _decorator;

    public FooCsvMapper(IFooDecorator decorator)
    {
        ArgumentNullException.ThrowIfNull(decorator);
        _decorator = decorator;  // missing this.
    }
}

// ✅ Correct
public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    private readonly IFooDecorator _decorator;

    public FooCsvMapper(IFooDecorator decorator)
    {
        ArgumentNullException.ThrowIfNull(decorator);
        this._decorator = decorator;
    }
}
```

---

## Quick Reference

### Using statements

```csharp
using CsvHelper;                  // CsvReader (in mapper implementations)
using CsvHelper.Configuration;    // CsvConfiguration (only when overriding defaults)
using Roadbed.IO;                 // IoCsvFile<T>, ICsvEntityMapper<T>, IoFile, IoFileInfo
```

### Define a mapper

```csharp
public sealed class FooCsvMapper : ICsvEntityMapper<Foo>
{
    public Foo? MapEntity(CsvReader reader) => new Foo
    {
        Id = reader.GetField<int>("Id"),
        Name = reader.GetField<string>("Name"),
    };
}
```

### Load from disk

```csharp
var file = IoCsvFile<Foo>.FromFile(@"C:\Data\foos.csv", new FooCsvMapper());
// file.DataRows is populated.
```

### Load asynchronously

```csharp
var file = await IoCsvFile<Foo>.FromFileAsync(@"C:\Data\foos.csv", new FooCsvMapper());
```

### Load from a string

```csharp
var file = IoCsvFile<Foo>.FromString(csvText, new FooCsvMapper());
```

### Export to a string

```csharp
string csv = file.ExportDataRowsAsContentString();
```

### Save to disk

```csharp
string savedPath = file.Save();
// Or: await file.SaveAsync();
```

### Save with a custom configuration

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    Delimiter = ";",
    Encoding = Encoding.UTF8,
};

string savedPath = file.Save(config);
```

### Repoint the destination before saving

```csharp
file.FileInfo = new IoFileInfo(@"C:\Data\output.csv");
file.Save();
```
