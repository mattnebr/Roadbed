namespace Roadbed.Test.Unit.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Data;
using Roadbed.Data.Sqlite;
using Roadbed.Logging;

/// <summary>
/// End-to-end tests for the application-scoped stale-activity reaper on
/// <see cref="LoggingActivityService"/>, exercised against a real
/// SQLiteInMemory database so the staleness predicate, application isolation,
/// and terminal-status write are verified against SQL rather than a mock.
/// </summary>
[TestClass]
public class LoggingActivityReaperTests
{
    #region Private Fields

    private const string TableDdl = @"
        CREATE TABLE activity (
             id                TEXT NOT NULL PRIMARY KEY
            ,application       TEXT NOT NULL
            ,environment       TEXT NULL
            ,status            TEXT NOT NULL
            ,started_on        TEXT NULL
            ,last_heartbeat_on TEXT NULL
            ,completed_on      TEXT NULL
            ,metrics           TEXT NULL
            ,created_on        TEXT NOT NULL
            ,last_modified_on  TEXT NULL
        );";

    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the MSTest-supplied context (used for the cancellation token).
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Verifies a stale running row for the configured application is reaped:
    /// status becomes canceled, completed_on is stamped, the metrics reason is
    /// recorded, and its id is returned.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReapStaleActivitiesAsync_StaleRow_CanceledWithReasonAndReturned()
    {
        // Arrange (Given)
        await using Harness harness = await Harness.CreateAsync("app-a", environment: null, this.Token);
        DateTime now = DateTime.UtcNow;
        await harness.SeedAsync("01STALEXXXXXXXXXXXXXXXXXXX", "app-a", environment: null, "running", heartbeat: now.AddHours(-2), started: now.AddHours(-2), created: now.AddHours(-2));

        // Act (When)
        IReadOnlyList<string> reaped = await harness.Service.ReapStaleActivitiesAsync(
            TimeSpan.FromMinutes(30),
            reason: "startup-sweep",
            this.Token);

        // Assert (Then)
        CollectionAssert.AreEqual(new[] { "01STALEXXXXXXXXXXXXXXXXXXX" }, reaped.ToArray());

        ActivityRow row = await harness.ReadAsync("01STALEXXXXXXXXXXXXXXXXXXX");
        Assert.AreEqual("canceled", row.Status, "Stale row must be transitioned to canceled.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(row.CompletedOn), "completed_on must be stamped on reap.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(row.LastModifiedOn), "last_modified_on must be stamped on reap.");
        StringAssert.Contains(row.Metrics, "\"reaped\":true", "metrics must mark the row as reaped.");
        StringAssert.Contains(row.Metrics, "\"reason\":\"startup-sweep\"", "metrics must record the supplied reason.");
        StringAssert.Contains(row.Metrics, "\"stale_after_seconds\":1800", "metrics must record the staleness threshold.");
    }

    /// <summary>
    /// Verifies a running row whose heartbeat is fresh is not reaped even when
    /// its created_on is old.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReapStaleActivitiesAsync_FreshHeartbeat_NotReaped()
    {
        // Arrange (Given)
        await using Harness harness = await Harness.CreateAsync("app-a", environment: null, this.Token);
        DateTime now = DateTime.UtcNow;
        await harness.SeedAsync("01FRESHHEARTBEATXXXXXXXXXX", "app-a", environment: null, "running", heartbeat: now, started: now.AddHours(-2), created: now.AddHours(-2));

        // Act (When)
        IReadOnlyList<string> reaped = await harness.Service.ReapStaleActivitiesAsync(TimeSpan.FromMinutes(30), reason: null, this.Token);

        // Assert (Then)
        Assert.IsEmpty(reaped, "A row with a recent heartbeat must not be reaped.");
        Assert.AreEqual("running", (await harness.ReadAsync("01FRESHHEARTBEATXXXXXXXXXX")).Status);
    }

    /// <summary>
    /// Verifies a just-begun running row (recent created_on, no heartbeat yet)
    /// is protected by the COALESCE fallback and is not reaped.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReapStaleActivitiesAsync_JustBegunNoHeartbeat_NotReaped()
    {
        // Arrange (Given)
        await using Harness harness = await Harness.CreateAsync("app-a", environment: null, this.Token);
        DateTime now = DateTime.UtcNow;
        await harness.SeedAsync("01JUSTBEGUNXXXXXXXXXXXXXXX", "app-a", environment: null, "running", heartbeat: null, started: null, created: now);

        // Act (When)
        IReadOnlyList<string> reaped = await harness.Service.ReapStaleActivitiesAsync(TimeSpan.FromMinutes(30), reason: null, this.Token);

        // Assert (Then)
        Assert.IsEmpty(reaped, "A just-begun row (recent created_on, null heartbeat) must not be reaped.");
        Assert.AreEqual("running", (await harness.ReadAsync("01JUSTBEGUNXXXXXXXXXXXXXXX")).Status);
    }

    /// <summary>
    /// The critical isolation test: a stale running row belonging to a
    /// different application — and one belonging to a different environment —
    /// must never be reaped by this application's sweep.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReapStaleActivitiesAsync_ForeignApplicationOrEnvironment_NotReaped()
    {
        // Arrange (Given) — service is scoped to app-a / production.
        await using Harness harness = await Harness.CreateAsync("app-a", environment: "production", this.Token);
        DateTime stale = DateTime.UtcNow.AddHours(-2);

        await harness.SeedAsync("01MINEXXXXXXXXXXXXXXXXXXXX", "app-a", "production", "running", heartbeat: stale, started: stale, created: stale);
        await harness.SeedAsync("01OTHERAPPXXXXXXXXXXXXXXXX", "app-b", "production", "running", heartbeat: stale, started: stale, created: stale);
        await harness.SeedAsync("01OTHERENVXXXXXXXXXXXXXXXX", "app-a", "staging", "running", heartbeat: stale, started: stale, created: stale);

        // Act (When)
        IReadOnlyList<string> reaped = await harness.Service.ReapStaleActivitiesAsync(TimeSpan.FromMinutes(30), reason: null, this.Token);

        // Assert (Then)
        CollectionAssert.AreEqual(new[] { "01MINEXXXXXXXXXXXXXXXXXXXX" }, reaped.ToArray(), "Only this application+environment's row may be reaped.");
        Assert.AreEqual("canceled", (await harness.ReadAsync("01MINEXXXXXXXXXXXXXXXXXXXX")).Status);
        Assert.AreEqual("running", (await harness.ReadAsync("01OTHERAPPXXXXXXXXXXXXXXXX")).Status, "A different application's row must remain untouched.");
        Assert.AreEqual("running", (await harness.ReadAsync("01OTHERENVXXXXXXXXXXXXXXXX")).Status, "A different environment's row must remain untouched.");
    }

    /// <summary>
    /// Verifies that already-terminal rows (succeeded/failed/canceled/skipped)
    /// are never touched, even when stale.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReapStaleActivitiesAsync_AlreadyTerminalRows_NotReaped()
    {
        // Arrange (Given)
        await using Harness harness = await Harness.CreateAsync("app-a", environment: null, this.Token);
        DateTime stale = DateTime.UtcNow.AddHours(-2);
        foreach (string terminal in new[] { "succeeded", "failed", "canceled", "skipped" })
        {
            await harness.SeedAsync($"01TERMINAL-{terminal}", "app-a", null, terminal, heartbeat: stale, started: stale, created: stale);
        }

        // Act (When)
        IReadOnlyList<string> reaped = await harness.Service.ReapStaleActivitiesAsync(TimeSpan.FromMinutes(30), reason: null, this.Token);

        // Assert (Then)
        Assert.IsEmpty(reaped, "Only running rows are reapable; terminal rows must be ignored.");
    }

    /// <summary>
    /// Verifies the read-only find returns exactly the stale running ids for
    /// the configured application without modifying any row.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task FindStaleActivitiesAsync_ReturnsExactStaleSetWithoutModifying()
    {
        // Arrange (Given)
        await using Harness harness = await Harness.CreateAsync("app-a", environment: null, this.Token);
        DateTime now = DateTime.UtcNow;
        await harness.SeedAsync("01STALEONEXXXXXXXXXXXXXXXX", "app-a", null, "running", heartbeat: now.AddHours(-3), started: now.AddHours(-3), created: now.AddHours(-3));
        await harness.SeedAsync("01STALETWOXXXXXXXXXXXXXXXX", "app-a", null, "running", heartbeat: null, started: null, created: now.AddHours(-3));
        await harness.SeedAsync("01FRESHONEXXXXXXXXXXXXXXXX", "app-a", null, "running", heartbeat: now, started: now, created: now);

        // Act (When)
        IReadOnlyList<string> stale = await harness.Service.FindStaleActivitiesAsync(TimeSpan.FromMinutes(30), this.Token);

        // Assert (Then)
        CollectionAssert.AreEquivalent(
            new[] { "01STALEONEXXXXXXXXXXXXXXXX", "01STALETWOXXXXXXXXXXXXXXXX" },
            stale.ToArray(),
            "Find must return exactly the stale running ids.");
        Assert.AreEqual("running", (await harness.ReadAsync("01STALEONEXXXXXXXXXXXXXXXX")).Status, "Find must not modify any row.");
    }

    #endregion Public Methods

    #region Private Properties

    private CancellationToken Token => this.TestContext.CancellationToken;

    #endregion Private Properties

    #region Private Types

    /// <summary>
    /// Minimal <see cref="ILoggingDatabaseFactory"/> over a SQLite connection
    /// factory so the production repository can be wired against the test
    /// database (the marker interface is what the repository depends on).
    /// </summary>
    private sealed class TestLoggingDatabaseFactory : ILoggingDatabaseFactory
    {
        private readonly IDataConnectionFactory _inner;

        public TestLoggingDatabaseFactory(IDataConnectionFactory inner)
        {
            this._inner = inner;
        }

        public DataConnecionString Connecion => this._inner.Connecion;

        public System.Data.IDbConnection CreateOpenConnection() => this._inner.CreateOpenConnection();

        public Task<System.Data.IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken) =>
            this._inner.CreateOpenConnectionAsync(cancellationToken);
    }

    /// <summary>
    /// Test <see cref="ILoggingDataExecutor"/> that delegates straight to the
    /// real <see cref="SqliteExecutor"/> — the same delegation the
    /// <c>Roadbed.Logging.Sqlite</c> satellite performs — so the repository
    /// runs genuine SQL against the in-memory database.
    /// </summary>
    private sealed class SqlitePassthroughExecutor : ILoggingDataExecutor
    {
        public Task<int> ExecuteAsync(DataExecutorRequest request, ILoggingDatabaseFactory factory, ILogger logger, CancellationToken cancellationToken) =>
            SqliteExecutor.ExecuteAsync(request, factory, logger, cancellationToken);

        public Task<IEnumerable<T>> QueryAsync<T>(DataExecutorRequest request, ILoggingDatabaseFactory factory, ILogger logger, CancellationToken cancellationToken) =>
            SqliteExecutor.QueryAsync<T>(request, factory, logger, cancellationToken);
    }

    /// <summary>
    /// Projection of the columns the assertions read back.
    /// </summary>
    private sealed class ActivityRow
    {
        public string Status { get; set; } = string.Empty;

        public string? CompletedOn { get; set; }

        public string? Metrics { get; set; }

        public string? LastModifiedOn { get; set; }
    }

    /// <summary>
    /// Owns a unique in-memory SQLite database (kept alive for the test
    /// lifetime), the created <c>activity</c> table, and a
    /// <see cref="LoggingActivityService"/> wired to a real repository.
    /// </summary>
    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _keepAliveConnection;
        private readonly IDisposable _keepAlive;

        private Harness(
            ILoggingDatabaseFactory factory,
            LoggingActivityService service,
            SqliteConnection keepAliveConnection,
            IDisposable keepAlive)
        {
            this.Factory = factory;
            this.Service = service;
            this._keepAliveConnection = keepAliveConnection;
            this._keepAlive = keepAlive;
        }

        public ILoggingDatabaseFactory Factory { get; }

        public LoggingActivityService Service { get; }

        public static async Task<Harness> CreateAsync(string application, string? environment, CancellationToken cancellationToken)
        {
            var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory)
            {
                DatabaseSource = $"reaper_{Guid.NewGuid():N}",
            };
            var factory = new TestLoggingDatabaseFactory(new SqliteConnectionFactory(connectionString));

            // Hold one connection open so the shared in-memory database
            // survives the separate connections the repository opens per call.
            var keepAliveConnection = (SqliteConnection)await factory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            IDisposable keepAlive = keepAliveConnection.KeepAlive();

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest(TableDdl) { RetriesEnabled = false },
                factory,
                logger: null,
                cancellationToken).ConfigureAwait(false);

            var options = new LoggingOptions
            {
                Schema = string.Empty,
                Application = application,
                Environment = environment,
            };

            var repository = new LoggingActivityRepository(
                new SqlitePassthroughExecutor(),
                factory,
                options,
                NullLogger<LoggingActivityRepository>.Instance);

            var service = new LoggingActivityService(
                repository,
                Mock.Of<ILoggingActivityInputRepository>(),
                options,
                NullLogger<LoggingActivityService>.Instance);

            return new Harness(factory, service, keepAliveConnection, keepAlive);
        }

        public async Task SeedAsync(
            string id,
            string application,
            string? environment,
            string status,
            DateTime? heartbeat,
            DateTime? started,
            DateTime created)
        {
            const string sql = @"
                INSERT INTO activity
                    (id, application, environment, status, started_on, last_heartbeat_on, created_on, last_modified_on)
                VALUES
                    (@Id, @Application, @Environment, @Status, @Started, @Heartbeat, @Created, @Created);";

            await SqliteExecutor.ExecuteAsync(
                new DataExecutorRequest(sql)
                {
                    RetriesEnabled = false,
                    Parameters = new
                    {
                        Id = id,
                        Application = application,
                        Environment = environment,
                        Status = status,
                        Started = started,
                        Heartbeat = heartbeat,
                        Created = created,
                    },
                },
                this.Factory,
                logger: null).ConfigureAwait(false);
        }

        public async Task<ActivityRow> ReadAsync(string id)
        {
            const string sql = @"
                SELECT status AS Status
                      ,completed_on AS CompletedOn
                      ,metrics AS Metrics
                      ,last_modified_on AS LastModifiedOn
                FROM activity
                WHERE id = @Id;";

            ActivityRow? row = await SqliteExecutor.QuerySingleOrDefaultAsync<ActivityRow>(
                new DataExecutorRequest(sql)
                {
                    RetriesEnabled = false,
                    Parameters = new { Id = id },
                },
                this.Factory,
                logger: null).ConfigureAwait(false);

            Assert.IsNotNull(row, $"Expected a seeded row with id '{id}'.");
            return row!;
        }

        public ValueTask DisposeAsync()
        {
            this._keepAlive.Dispose();
            this._keepAliveConnection.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    #endregion Private Types
}
