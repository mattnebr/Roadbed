namespace Roadbed.Test.Unit.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Logging;

/// <summary>
/// Unit tests for <see cref="LoggingChannel"/>.
/// </summary>
[TestClass]
public class LoggingChannelTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that enqueued entries are readable in FIFO order.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryWrite_AcceptsEntries_ReaderDrainsInFifoOrder()
    {
        // Arrange (Given)
        var options = new LoggingOptions { ChannelCapacity = 4 };
        var channel = new LoggingChannel(options);
        var first = new LoggingLogEntry { Message = "first", Application = "test" };
        var second = new LoggingLogEntry { Message = "second", Application = "test" };

        // Act (When)
        bool firstAccepted = channel.TryWrite(first);
        bool secondAccepted = channel.TryWrite(second);
        channel.CompleteWriter();

        var read = new System.Collections.Generic.List<LoggingLogEntry>();
        while (await channel.Reader.WaitToReadAsync(CancellationToken.None))
        {
            while (channel.Reader.TryRead(out var entry))
            {
                read.Add(entry);
            }
        }

        // Assert (Then)
        Assert.IsTrue(firstAccepted, "First TryWrite should accept.");
        Assert.IsTrue(secondAccepted, "Second TryWrite should accept.");
        Assert.AreEqual(2, read.Count, "Both entries should be drained.");
        Assert.AreEqual("first", read[0].Message);
        Assert.AreEqual("second", read[1].Message);
    }

    /// <summary>
    /// Verifies that BlockBriefly policy rejects writes once capacity is exceeded
    /// and the dropped counter is incremented for every rejection.
    /// </summary>
    [TestMethod]
    public void TryWrite_BlockBrieflyPolicyExceedsCapacity_RejectsAndCountsDrops()
    {
        // Arrange (Given)
        var options = new LoggingOptions
        {
            ChannelCapacity = 2,
            ChannelFullPolicy = LoggingChannelFullPolicy.BlockBriefly,
        };
        var channel = new LoggingChannel(options);

        // Act (When) - fill to capacity (these should succeed) then try a third (should fail).
        bool a = channel.TryWrite(new LoggingLogEntry { Application = "x" });
        bool b = channel.TryWrite(new LoggingLogEntry { Application = "x" });
        bool c = channel.TryWrite(new LoggingLogEntry { Application = "x" });
        long dropped = channel.ConsumeDroppedCount();

        // Assert (Then)
        Assert.IsTrue(a, "First write should accept.");
        Assert.IsTrue(b, "Second write should accept.");
        Assert.IsFalse(c, "Third write should be rejected at capacity.");
        Assert.AreEqual(1L, dropped, "Drop counter should record one rejection.");
    }

    /// <summary>
    /// Verifies that <see cref="LoggingChannel.ConsumeDroppedCount"/> resets the counter.
    /// </summary>
    [TestMethod]
    public void ConsumeDroppedCount_ResetsCounterAtomically()
    {
        // Arrange (Given)
        var options = new LoggingOptions
        {
            ChannelCapacity = 1,
            ChannelFullPolicy = LoggingChannelFullPolicy.BlockBriefly,
        };
        var channel = new LoggingChannel(options);
        channel.TryWrite(new LoggingLogEntry { Application = "x" });
        channel.TryWrite(new LoggingLogEntry { Application = "x" });

        // Act (When)
        long firstRead = channel.ConsumeDroppedCount();
        long secondRead = channel.ConsumeDroppedCount();

        // Assert (Then)
        Assert.AreEqual(1L, firstRead, "First consume should report one drop.");
        Assert.AreEqual(0L, secondRead, "Second consume should report zero (reset).");
    }

    /// <summary>
    /// Verifies that the constructor throws when options are null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullOptions_Throws()
    {
        // Arrange (Given) + Act (When) + Assert (Then)
        Assert.ThrowsExactly<ArgumentNullException>(
            () => _ = new LoggingChannel(null!));
    }

    #endregion Public Methods
}
