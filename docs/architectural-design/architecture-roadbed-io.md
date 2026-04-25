# Roadbed.IO Architecture

Roadbed.IO provides a thin, testable abstraction over file system operations. It exists to give higher-level libraries — primarily Roadbed.IO.Csv — a base class to inherit from and a strongly-typed file-info wrapper that decouples consumers from `System.IO.FileInfo`.

The public surface is intentionally small: an abstract `IoFile` base class with synchronous and asynchronous `Save` operations, and an `IoFileInfo` DTO that wraps `System.IO.FileInfo` and exposes only the properties needed for save operations and CSV-style file-extension validation.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.IO NuGet package. When a developer asks you to write a typed file abstraction (e.g., for CSV, JSON, XML, fixed-width), or needs to share file-info between layers without taking a hard dependency on `System.IO.FileInfo`, use this document.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Inherit from `IoFile`** when creating a typed file handler — do not re-implement save semantics.
5. **Always pass an `IoFileInfo` to the protected `IoFile(IoFileInfo)` constructor** when the file is backed by a path. The parameterless constructor is for in-memory-only scenarios (no path yet).
6. **Validate `IoFileInfo` with `IoFile.ValidateFileInfo`** before performing path-based operations. The framework already does this in `Save`/`SaveAsync` — your subclass should do the same in any new path-based method.
7. **Set `IoFileInfo.FullPath`** to populate the underlying `System.IO.FileInfo`. Setting `FullPath` to null or whitespace clears `FileInfo` to null.
8. **Do not write directly to `IoFileInfo.FileInfo`** from outside the assembly — its setter is `internal`. Always go through `FullPath`.
9. **Flatten namespaces** — only `using Roadbed.IO;` is needed. The `.Entities` and `.Dtos` suffixes were removed on purpose.
10. **`Save` returns `string.Empty` when content is null/whitespace** — it is a no-op, not an error. Subclasses that wrap content in a serializer should preserve this behavior.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-io.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-io.md#type-catalog)
3. [Package Relationship](architecture-roadbed-io.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-io.md#namespace-convention)
5. [IoFile](architecture-roadbed-io.md#iofile)
    - [Save Semantics](architecture-roadbed-io.md#save-semantics)
    - [ValidateFileInfo](architecture-roadbed-io.md#validatefileinfo)
6. [IoFileInfo](architecture-roadbed-io.md#iofileinfo)
    - [Property Behavior](architecture-roadbed-io.md#property-behavior)
7. [Implementation Walkthrough](architecture-roadbed-io.md#implementation-walkthrough)
8. [Common Pitfalls](architecture-roadbed-io.md#common-pitfalls)
9. [Quick Reference](architecture-roadbed-io.md#quick-reference)

---

## Type Catalog

Roadbed.IO contains **2 public types**.

| Type         | Kind           | Namespace    | Purpose                                                                                                |
| ------------ | -------------- | ------------ | ------------------------------------------------------------------------------------------------------ |
| `IoFile`     | Abstract class | `Roadbed.IO` | Base class for typed file handlers. Provides synchronous and asynchronous `Save`, plus path validation. |
| `IoFileInfo` | Class          | `Roadbed.IO` | DTO wrapping `System.IO.FileInfo`. Exposes `FullPath` and `Extension`.                                  |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────┐
│ Higher-level typed-file libraries / Application code         │
│                                                              │
│   Inherits IoFile (e.g., IoCsvFile<T>, IoJsonFile<T>)       │
│   Constructs IoFileInfo from a path                          │
│   Calls Save / SaveAsync                                     │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ Roadbed.IO                                                   │
│                                                              │
│   IoFile      (abstract base for typed file handlers)        │
│   IoFileInfo  (DTO wrapping System.IO.FileInfo)              │
└──────────┬───────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────┐
│ .NET Base Class Library                                      │
│                                                              │
│   System.IO.FileInfo, System.IO.StreamWriter                 │
└──────────────────────────────────────────────────────────────┘
```

Roadbed.IO has no third-party dependencies. It is the substrate that other typed-file libraries (Roadbed.IO.Csv today; Roadbed.IO.Json or similar in the future) build on top of.

---

## Namespace Convention

| Namespace    | Contains            |
| ------------ | ------------------- |
| `Roadbed.IO` | All 2 public types  |

The original `Roadbed.IO.Entities` and `Roadbed.IO.Dtos` namespaces were removed on purpose. Consuming code only ever needs `using Roadbed.IO;`.

---

## IoFile

Abstract base class that any typed file handler should inherit from:

```csharp
namespace Roadbed.IO;

public abstract class IoFile
{
    protected IoFile();
    protected IoFile(IoFileInfo fileInfo);

    public IoFileInfo? FileInfo { get; set; }

    public static void ValidateFileInfo(IoFileInfo? fileInfo);

    public string Save(string fileContent);
    public Task<string> SaveAsync(string fileContent);
}
```

**Constructor matrix:**

| Constructor             | When to use                                                                                               |
| ----------------------- | --------------------------------------------------------------------------------------------------------- |
| `IoFile()`              | The file has no path yet — content lives in memory only. Subclasses that load from a string use this path. |
| `IoFile(IoFileInfo)`    | The file is backed by a path. Subclasses that load from disk use this path.                                |

`FileInfo` is mutable on the public surface so subclasses or callers can swap the destination after construction (for example, "loaded from one path, save to another").

### Save Semantics

Both `Save` and `SaveAsync` follow the same flow:

```
Validate this.FileInfo via IoFile.ValidateFileInfo
    │
    ├── FileInfo is null         → throw ArgumentNullException
    ├── FileInfo.Extension blank → throw ArgumentNullException
    │
    ▼
If fileContent is null or whitespace
    │
    └── Return string.Empty (no-op, no file written)
    │
    ▼
Open StreamWriter at FileInfo.FullPath
Write content
Return FullPath
```

| Method        | Returns         | Notes                                                                       |
| ------------- | --------------- | --------------------------------------------------------------------------- |
| `Save`        | `string`        | Synchronous. Returns the saved file's full path, or `string.Empty` if no-op. |
| `SaveAsync`   | `Task<string>`  | Asynchronous (`StreamWriter.WriteAsync`). Same return semantics.             |

**Both methods throw** if `FileInfo` is null or `Extension` is null/empty. Always set `FullPath` before calling save.

**Both methods overwrite** any existing file at the path — `StreamWriter` is constructed in default mode.

### ValidateFileInfo

Public static helper that any subclass can call when implementing additional path-based methods:

```csharp
public static void ValidateFileInfo(IoFileInfo? fileInfo)
{
    ArgumentNullException.ThrowIfNull(fileInfo);

    if (string.IsNullOrWhiteSpace(fileInfo.Extension))
    {
        throw new ArgumentNullException(nameof(fileInfo), "File extension is null or empty.");
    }
}
```

**Two failure modes:**

| Condition              | Exception thrown                                          |
| ---------------------- | --------------------------------------------------------- |
| `fileInfo` is null     | `ArgumentNullException` (paramName: `fileInfo`)           |
| `Extension` is blank   | `ArgumentNullException` (paramName: `fileInfo`, message includes "File extension is null or empty.") |

A blank extension implies the path is missing or malformed (e.g., a directory rather than a file). Subclasses that gate operations on file type should check `Extension` themselves after calling this helper.

---

## IoFileInfo

DTO wrapping `System.IO.FileInfo`:

```csharp
namespace Roadbed.IO;

public class IoFileInfo
{
    public IoFileInfo();
    public IoFileInfo(string path);

    public string? Extension { get; }      // Null when FullPath is unset
    public FileInfo? FileInfo { get; }     // internal set
    public string? FullPath { get; set; }  // Setter rebuilds FileInfo
}
```

### Property Behavior

| Property     | Get                                                                         | Set                                                                                    |
| ------------ | --------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `Extension`  | Returns `FileInfo.Extension` (e.g., `.csv`). Returns `null` if not set.     | Read-only.                                                                             |
| `FileInfo`   | Returns the wrapped `System.IO.FileInfo` instance, or `null`.               | `internal set` — only writable from within the assembly.                                |
| `FullPath`   | Returns `FileInfo.FullName` (the absolute path), or `null` if no `FileInfo`. | Setting to a non-blank value constructs a new `FileInfo`. Setting to null/blank clears it. |

The single source of truth is `FileInfo`. `FullPath` and `Extension` are projections of it. Setting `FullPath` is the only way to populate `FileInfo` from outside the assembly.

**Constructor side-effects:**

- `IoFileInfo()` leaves both `FileInfo` and `FullPath` null. Caller must set `FullPath` before saving.
- `IoFileInfo(string path)` validates that `path` is non-blank (`ArgumentException.ThrowIfNullOrWhiteSpace`), then sets `FullPath`. The wrapped `FileInfo` reflects the path even if the file does not yet exist on disk.

---

## Implementation Walkthrough

This walkthrough shows how to build a typed file handler — `IoFooFile<T>` — that inherits from `IoFile` and adds typed `From*`/`Save` operations. The pattern is the same one Roadbed.IO.Csv uses for `IoCsvFile<T>`.

### Step 1: Define the entity mapper interface

```csharp
namespace MyApp.Foo;

public interface IFooEntityMapper<out T>
{
    T? MapEntity(string rawLine);
}
```

This abstraction lets each consumer plug in its own mapping from a raw file representation to a strongly-typed entity.

### Step 2: Inherit from `IoFile`

```csharp
namespace MyApp.Foo;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Roadbed.IO;

public class IoFooFile<T>
    : IoFile
{
    #region Protected Constructors

    protected IoFooFile(IFooEntityMapper<T> dataMapper)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);

        this.DataRows = new List<T>();
        this.DataMapper = dataMapper;
    }

    protected IoFooFile(IoFileInfo fileInfo, IFooEntityMapper<T> dataMapper)
        : base(fileInfo)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);

        ValidateFileInfo(fileInfo);

        if (!string.Equals(fileInfo?.Extension, ".foo", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File extension isn't '.foo'.", nameof(fileInfo));
        }

        this.DataRows = new List<T>();
        this.DataMapper = dataMapper;
    }

    #endregion Protected Constructors

    #region Public Properties

    public IFooEntityMapper<T>? DataMapper { get; set; }

    public IList<T> DataRows { get; set; }

    #endregion Public Properties

    #region Public Methods

    public static IoFooFile<T> FromFile(string path, IFooEntityMapper<T> dataMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(dataMapper);

        var file = new IoFooFile<T>(new IoFileInfo(path), dataMapper);
        file.LoadDataRowsFromFile();
        return file;
    }

    public string Save()
    {
        return this.Save(this.SerializeDataRows());
    }

    public Task<string> SaveAsync()
    {
        return this.SaveAsync(this.SerializeDataRows());
    }

    #endregion Public Methods

    #region Private Methods

    private void LoadDataRowsFromFile()
    {
        ValidateFileInfo(this.FileInfo!);
        ArgumentNullException.ThrowIfNull(this.DataMapper);

        this.DataRows = new List<T>();

        foreach (var line in File.ReadAllLines(this.FileInfo!.FullPath!))
        {
            var entity = this.DataMapper!.MapEntity(line);
            if (entity is not null)
            {
                this.DataRows.Add(entity);
            }
        }
    }

    private string SerializeDataRows()
    {
        // ... convert DataRows back to file-format text ...
        return string.Empty;
    }

    #endregion Private Methods
}
```

### Pattern observations

- **Two protected constructors.** One for in-memory (`IoFooFile()` overload — no path), one for path-backed (`IoFooFile(IoFileInfo, ...)`). This mirrors `IoFile`'s own constructor split.
- **Static `From*` factory methods** (`FromFile`, optionally `FromString`) act as the public construction surface. They validate inputs, build the instance, and trigger initial load.
- **Always call `IoFile.ValidateFileInfo`** before any path-based operation. The base class does this in `Save`/`SaveAsync`; subclasses must do it in any additional load/save method they introduce.
- **`Save()` overloads delegate to `IoFile.Save(string)`** by serializing in-memory data to the wire format first. This keeps the base class's no-content-no-write semantic.
- **`DataMapper` is mutable** so callers can swap mappers between operations if needed (rare, but cheap).

---

## Common Pitfalls

### 1. Calling `Save` Without Setting `FileInfo`

```csharp
// ❌ Wrong — Save throws because FileInfo is null
var file = new MyTypedFile();
file.Save("content");

// ✅ Correct — assign FileInfo first
var file = new MyTypedFile { FileInfo = new IoFileInfo(@"C:\Data\foo.bar") };
file.Save("content");
```

### 2. Trying to Set `IoFileInfo.FileInfo` Directly

```csharp
// ❌ Wrong — FileInfo has internal set; will not compile from outside the assembly
var info = new IoFileInfo();
info.FileInfo = new FileInfo(@"C:\Data\foo.bar");

// ✅ Correct — set FullPath, which constructs the wrapped FileInfo
var info = new IoFileInfo();
info.FullPath = @"C:\Data\foo.bar";
```

### 3. Treating Null/Whitespace Content as an Error

```csharp
// ❌ Wrong — Save returns string.Empty for blank content; you'll think it failed
var path = file.Save(content);
if (path == string.Empty)
{
    throw new InvalidOperationException("Save failed!");  // Misleading
}

// ✅ Correct — Save returns string.Empty deliberately when content is blank
var path = file.Save(content);
if (string.IsNullOrEmpty(path))
{
    // No file was written because content was blank — that's expected
    return;
}
```

### 4. Skipping `ValidateFileInfo` in Subclass Path-Based Methods

```csharp
// ❌ Wrong — subclass method assumes FileInfo is valid
public void Reload()
{
    foreach (var line in File.ReadAllLines(this.FileInfo!.FullPath!))  // NullRef if FileInfo is null
    {
        // ...
    }
}

// ✅ Correct — call IoFile.ValidateFileInfo first
public void Reload()
{
    ValidateFileInfo(this.FileInfo!);

    foreach (var line in File.ReadAllLines(this.FileInfo!.FullPath!))
    {
        // ...
    }
}
```

### 5. Reading `Extension` Before `FullPath` Is Set

```csharp
// ❌ Wrong — Extension returns null when FileInfo is null
var info = new IoFileInfo();
if (info.Extension == ".foo")  // Always false because Extension is null
{
    // ...
}

// ✅ Correct — check FullPath or FileInfo first, or set FullPath before checking Extension
var info = new IoFileInfo(@"C:\Data\foo.bar");
if (string.Equals(info.Extension, ".bar", StringComparison.OrdinalIgnoreCase))
{
    // ...
}
```

### 6. Assuming `Save` Will Append

```csharp
// ❌ Wrong — Save overwrites the file each call
file.Save("first batch");
file.Save("second batch");  // Overwrites; "first batch" is gone

// ✅ Correct — accumulate content, then save once
var sb = new StringBuilder();
sb.AppendLine("first batch");
sb.AppendLine("second batch");
file.Save(sb.ToString());
```

### 7. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public class IoFooFile<T> : IoFile
{
    private readonly IFooEntityMapper<T> _mapper;

    public IoFooFile(IFooEntityMapper<T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        _mapper = mapper;  // Missing this.
    }
}

// ✅ Correct
public class IoFooFile<T> : IoFile
{
    private readonly IFooEntityMapper<T> _mapper;

    public IoFooFile(IFooEntityMapper<T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        this._mapper = mapper;
    }
}
```

---

## Quick Reference

### Using statements

```csharp
using Roadbed.IO;     // IoFile, IoFileInfo
```

### Construct an `IoFileInfo`

```csharp
var info = new IoFileInfo(@"C:\Data\foo.bar");
// info.FullPath  → "C:\Data\foo.bar"
// info.Extension → ".bar"
```

### Save a string to a path

```csharp
public sealed class FooFile : IoFile
{
    public FooFile(IoFileInfo fileInfo) : base(fileInfo) { }
}

var file = new FooFile(new IoFileInfo(@"C:\Data\foo.bar"));
string savedPath = file.Save("hello world");
// savedPath = "C:\Data\foo.bar"
```

### Save asynchronously

```csharp
string savedPath = await file.SaveAsync("hello world");
```

### Validate file info before a custom operation

```csharp
IoFile.ValidateFileInfo(this.FileInfo!);
// Throws ArgumentNullException if FileInfo is null or Extension is blank.
```

### Subclass blueprint

```csharp
public class IoFooFile<T> : IoFile
{
    protected IoFooFile() { }                                   // in-memory
    protected IoFooFile(IoFileInfo fileInfo) : base(fileInfo) { } // path-backed

    public static IoFooFile<T> FromFile(string path) =>
        new IoFooFile<T>(new IoFileInfo(path));

    public string Save() => this.Save(this.SerializeContent());

    private string SerializeContent() => /* ... */ string.Empty;
}
```
