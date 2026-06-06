namespace Roadbed.Test.Unit.Logging;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Logging;

/// <summary>
/// Unit tests for <see cref="LogWriterHostedService"/>.
/// </summary>
[TestClass]
public class LogWriterHostedServiceTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that FlushAsync on an empty batch is a no-op and never touches the repository.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task FlushAsync_EmptyBatch_NoRepositoryInvocation()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingLogEntryRepository>();
        var writer = new LogWriterHostedService(
            new LoggingChannel(new LoggingOptions()),
            repository.Object,
            new LoggingOptions(),
            NullLogger<LogWriterHostedService>.Instance);

        // Act (When)
        await writer.FlushAsync(new List<LoggingLogEntry>(), CancellationToken.None);

        // Assert (Then)
        repository.Verify(
            r => r.BulkInsertAsync(It.IsAny<IReadOnlyList<LoggingLogEntry>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that FlushAsync clears the batch after a successful bulk insert.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task FlushAsync_HappyPath_ClearsBatchAfterInsert()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingLogEntryRepository>();
        repository
            .Setup(r => r.BulkInsertAsync(It.IsAny<IReadOnlyList<LoggingLogEntry>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var writer = new LogWriterHostedService(
            new LoggingChannel(new LoggingOptions()),
            repository.Object,
            new LoggingOptions(),
            NullLogger<LogWriterHostedService>.Instance);

        var batch = new List<LoggingLogEntry>
        {
            new () { Message = "a", Application = "x" },
            new () { Message = "b", Application = "x" },
            new () { Message = "c", Application = "x" },
        };

        // Act (When)
        await writer.FlushAsync(batch, CancellationToken.None);

        // Assert (Then)
        Assert.AreEqual(0, batch.Count, "Batch should be cleared after a successful flush.");
        repository.Verify(
            r => r.BulkInsertAsync(It.IsAny<IReadOnlyList<LoggingLogEntry>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that FlushAsync swallows repository exceptions and still clears the batch
    /// so a wedged DB cannot wedge the writer loop.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task FlushAsync_RepositoryThrows_BatchStillClearedAndExceptionSwallowed()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingLogEntryRepository>();
        repository
            .Setup(r => r.BulkInsertAsync(It.IsAny<IReadOnlyList<LoggingLogEntry>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.InvalidOperationException("db down"));

        var writer = new LogWriterHostedService(
            new LoggingChannel(new LoggingOptions()),
            repository.Object,
            new LoggingOptions(),
            NullLogger<LogWriterHostedService>.Instance);

        var batch = new List<LoggingLogEntry>
        {
            new () { Message = "a", Application = "x" },
            new () { Message = "b", Application = "x" },
        };

        // Act (When) - must not throw out.
        await writer.FlushAsync(batch, CancellationToken.None);

        // Assert (Then)
        Assert.AreEqual(0, batch.Count, "Batch should be cleared even when the bulk insert fails.");
    }

    #endregion Public Methods
}
