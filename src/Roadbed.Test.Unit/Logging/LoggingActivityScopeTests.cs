namespace Roadbed.Test.Unit.Logging;

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Logging;

/// <summary>
/// Unit tests for <see cref="LoggingActivityScope"/>.
/// </summary>
[TestClass]
public class LoggingActivityScopeTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that disposing the scope disposes both the inner activity and the MEL scope.
    /// </summary>
    [TestMethod]
    public void Dispose_FirstCall_DisposesActivityAndLogScopeExactlyOnce()
    {
        // Arrange (Given)
        int logScopeDisposeCount = 0;
        var fakeScope = new ActionDisposable(() => logScopeDisposeCount++);

        var activity = new Activity("test.activity");
        activity.Start();

        var scope = new LoggingActivityScope("01TESTACTIVITYIDXXXXXXXXXX", activity, fakeScope);

        // Act (When)
        scope.Dispose();
        scope.Dispose(); // double-dispose must be a no-op

        // Assert (Then)
        Assert.AreEqual(1, logScopeDisposeCount, "Log scope should dispose exactly once.");
        Assert.IsFalse(activity.IsAllDataRequested && activity.IsStopped == false, "Activity should be stopped after scope dispose.");
    }

    /// <summary>
    /// Verifies that the scope exposes ActivityId, TraceId, and SpanId when an Activity is present.
    /// </summary>
    [TestMethod]
    public void TraceIdAndSpanId_ActivityPresent_ReturnsHexEncodedValues()
    {
        // Arrange (Given)
        const string ActivityId = "01ACTIVITYIDABCDEFGHIJKLMN";
        var activity = new Activity("test.activity");
        activity.Start();
        var scope = new LoggingActivityScope(ActivityId, activity, null);

        // Act (When)
        string? traceId = scope.TraceId;
        string? spanId = scope.SpanId;

        // Assert (Then)
        Assert.AreEqual(ActivityId, scope.ActivityId);
        Assert.IsFalse(string.IsNullOrEmpty(traceId), "TraceId should be non-empty when Activity is started.");
        Assert.AreEqual(32, traceId!.Length, "W3C TraceId is 32 hex characters.");
        Assert.IsFalse(string.IsNullOrEmpty(spanId), "SpanId should be non-empty when Activity is started.");
        Assert.AreEqual(16, spanId!.Length, "W3C SpanId is 16 hex characters.");

        scope.Dispose();
    }

    /// <summary>
    /// Verifies that the scope returns nullable trace/span values when no Activity was started.
    /// </summary>
    [TestMethod]
    public void TraceIdAndSpanId_NoActivity_ReturnsNull()
    {
        // Arrange (Given)
        var scope = new LoggingActivityScope("01NOACTIVITYIDXXXXXXXXXXXX", null, null);

        // Act (When) + Assert (Then)
        Assert.IsNull(scope.TraceId);
        Assert.IsNull(scope.SpanId);
    }

    /// <summary>
    /// Verifies that the constructor rejects whitespace activity identifiers.
    /// </summary>
    [TestMethod]
    public void Constructor_WhitespaceActivityId_Throws()
    {
        // Arrange (Given) + Act (When) + Assert (Then)
        Assert.ThrowsExactly<ArgumentException>(
            () => _ = new LoggingActivityScope("   ", null, null));
    }

    #endregion Public Methods

    #region Private Types

    /// <summary>
    /// Disposable test double that runs an action on Dispose.
    /// </summary>
    private sealed class ActionDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public ActionDisposable(Action onDispose)
        {
            this._onDispose = onDispose;
        }

        public void Dispose()
        {
            this._onDispose();
        }
    }

    #endregion Private Types
}
