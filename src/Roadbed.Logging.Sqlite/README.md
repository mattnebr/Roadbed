# Roadbed.Logging.Sqlite

SQLite provider for [Roadbed.Logging](../Roadbed.Logging/README.md) (local/dev
and test). It brings the Microsoft.Data.Sqlite client (via `Roadbed.Data.Sqlite`)
and supplies the `ILoggingDataExecutor` adapter plus the `InstallLoggingSqlite`
installer that selects SQLite as the logging backend.

Reference this package **and** the provider-neutral `Roadbed.Logging` core. Do
not also reference `Roadbed.Logging.MySql` — pick exactly one provider.

## Wiring (the one authoritative call)

```csharp
using Roadbed.Logging;
using Roadbed.Logging.Sqlite;

// Register these two singletons FIRST.
builder.Services.AddSingleton(new LoggingOptions { Schema = "", Application = "Foo", Environment = env });
builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

// ONE call: wire the OTel exporter AND select SQLite by naming the installer.
builder.Logging.AddRoadbedDbLogging<InstallLoggingSqlite>();
```

That single call wires the exporter, the executor, the repositories, the shared
`LoggingChannel`, and the background writer. Naming `InstallLoggingSqlite` as the
type argument compile-pins this assembly, so it loads and wires deterministically
— no `typeof(...)` discard, no manual `Assembly.Load`, no reliance on
`InstallModulesInAppDomain` to auto-discover the satellite.

Leave `LoggingOptions.Schema` empty for SQLite (no `ATTACH`), and remember the
in-memory variant only persists while a connection is held open.

See the [Roadbed.Logging reference](../../skills/code-roadbed-csharp/references/reference-roadbed-logging.md)
for the full contract, schema, and activity-tracking usage.
