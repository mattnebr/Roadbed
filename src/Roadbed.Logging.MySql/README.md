# Roadbed.Logging.MySql

MySQL / MariaDB provider for [Roadbed.Logging](../Roadbed.Logging/README.md). It
brings the MySqlConnector client (via `Roadbed.Data.MySql`) and supplies the
`ILoggingDataExecutor` adapter plus the `InstallLoggingMySql` installer that
selects MySQL as the logging backend.

Reference this package **and** the provider-neutral `Roadbed.Logging` core. Do
not also reference `Roadbed.Logging.Sqlite` — pick exactly one provider.

## Wiring (the one authoritative call)

```csharp
using Roadbed.Logging;
using Roadbed.Logging.MySql;

// Register these two singletons FIRST.
builder.Services.AddSingleton(new LoggingOptions { Schema = "logging", Application = "Foo", Environment = env });
builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

// ONE call: wire the OTel exporter AND select MySQL by naming the installer.
builder.Logging.AddRoadbedDbLogging<InstallLoggingMySql>();
```

That single call wires the exporter, the executor, the repositories, the shared
`LoggingChannel`, and the background writer. Naming `InstallLoggingMySql` as the
type argument compile-pins this assembly, so it loads and wires deterministically.

**You do not need**, and should not use, any of the following — they were
workarounds for an earlier auto-discovery gap that the typed call removes:

```csharp
_ = typeof(InstallLoggingMySql);                 // ❌ not needed
_ = new InstallLoggingMySql();                   // ❌ not needed
Assembly.Load("Roadbed.Logging.MySql");          // ❌ not needed
// relying on InstallModulesInAppDomain to find the satellite  // ❌ not needed for logging
```

`InstallModulesInAppDomain` is still fine for your host's *other* installers; it
is simply no longer how the logging provider is selected.

## Vendored (HintPath) consumers

The typed call works identically when the DLLs are vendored via `<Reference>` /
`HintPath` (no NuGet). Because `InstallLoggingMySql` appears as a real type
argument in your code, the C# compiler keeps the assembly reference in your
manifest and the runtime loads it — there is nothing to elide.

See the [Roadbed.Logging reference](../../skills/code-roadbed-csharp/references/reference-roadbed-logging.md)
for the full contract, schema, and activity-tracking usage.
