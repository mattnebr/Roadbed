namespace Roadbed.Test.Unit.Scheduling.Services;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using Roadbed.Scheduling;
using Roadbed.Scheduling.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contains unit tests for verifying the behavior of the SchedulingMetricsListener class.
/// </summary>
[TestClass]
public class SchedulingMetricsListenerTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentNullException when metrics is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullMetrics_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        ISchedulingMetrics? nullMetrics = null;
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        ArgumentNullException? caughtException = null;

        // Act (When)
        try
        {
            var listener = new SchedulingMetricsListener(nullMetrics!, mockLogger.Object);
        }
        catch (ArgumentNullException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Constructor should throw ArgumentNullException when metrics is null.");
        Assert.AreEqual(
            "metrics",
            caughtException.ParamName,
            "Exception should indicate correct parameter name.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        ILogger<SchedulingMetricsListener>? nullLogger = null;
        ArgumentNullException? caughtException = null;

        // Act (When)
        try
        {
            var listener = new SchedulingMetricsListener(mockMetrics.Object, nullLogger!);
        }
        catch (ArgumentNullException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Constructor should throw ArgumentNullException when logger is null.");
        Assert.AreEqual(
            "logger",
            caughtException.ParamName,
            "Exception should indicate correct parameter name.");
    }

    /// <summary>
    /// Unit test to verify that Name property returns correct value.
    /// </summary>
    [TestMethod]
    public void Name_Property_ReturnsCorrectValue()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        // Act (When)
        string name = listener.Name;

        // Assert (Then)
        Assert.AreEqual(
            "SchedulingMetricsListener",
            name,
            "Name property should return 'SchedulingMetricsListener'.");
    }

    /// <summary>
    /// Unit test to verify that JobToBeExecuted calls metrics.JobStarted.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobToBeExecuted_ValidContext_CallsMetricsJobStarted()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");

        // Act (When)
        await listener.JobToBeExecuted(mockContext.Object, CancellationToken.None);

        // Assert (Then)
        mockMetrics.Verify(
            m => m.JobStarted(It.Is<JobExecutionInfo>(info =>
                info.JobName == "TestJob" &&
                info.JobGroup == "TestGroup")),
            Times.Once,
            "JobToBeExecuted should call metrics.JobStarted with correct JobExecutionInfo.");
    }

    /// <summary>
    /// Unit test to verify that JobToBeExecuted stores timestamp in context.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobToBeExecuted_ValidContext_StoresTimestampInContext()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        long? storedTimestamp = null;

        // Note: Put method signature is Put(object key, object value)
        mockContext.Setup(c => c.Put(It.IsAny<object>(), It.IsAny<object>()))
            .Callback<object, object>((key, value) =>
            {
                if (key.ToString() == "metrics_start_time" && value is long timestamp)
                {
                    storedTimestamp = timestamp;
                }
            });

        // Act (When)
        await listener.JobToBeExecuted(mockContext.Object, CancellationToken.None);

        // Assert (Then)
        Assert.IsNotNull(
            storedTimestamp,
            "JobToBeExecuted should store timestamp in context.");
        Assert.IsGreaterThan(
            0,
            storedTimestamp.Value,
            "Stored timestamp should be a positive value.");
    }

    /// <summary>
    /// Unit test to verify that JobToBeExecuted does not propagate metrics exceptions.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobToBeExecuted_MetricsThrows_DoesNotPropagateException()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        mockMetrics.Setup(m => m.JobStarted(It.IsAny<JobExecutionInfo>()))
            .Throws(new InvalidOperationException("Metrics error"));

        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await listener.JobToBeExecuted(mockContext.Object, CancellationToken.None);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobToBeExecuted should not propagate exceptions from metrics.");
    }

    /// <summary>
    /// Unit test to verify that JobToBeExecuted logs warning when metrics throws.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobToBeExecuted_MetricsThrows_LogsDebug()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        mockMetrics.Setup(m => m.JobStarted(It.IsAny<JobExecutionInfo>()))
            .Throws(new InvalidOperationException("Metrics error"));

        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");

        // Act (When)
        await listener.JobToBeExecuted(mockContext.Object, CancellationToken.None);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobToBeExecuted should log debug when metrics throws exception.");
    }

    /// <summary>
    /// Unit test to verify that JobWasExecuted calls metrics.JobCompleted on success.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobWasExecuted_Success_CallsMetricsJobCompleted()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        mockContext.Setup(c => c.Get("metrics_start_time")).Returns(100L);

        // Act (When)
        await listener.JobWasExecuted(mockContext.Object, null, CancellationToken.None);

        // Assert (Then)
        mockMetrics.Verify(
            m => m.JobCompleted(
                It.Is<JobExecutionInfo>(info => info.JobName == "TestJob"),
                It.IsAny<TimeSpan>()),
            Times.Once,
            "JobWasExecuted should call metrics.JobCompleted when job succeeds.");
    }

    /// <summary>
    /// Unit test to verify that JobWasExecuted calls metrics.JobFailed on failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobWasExecuted_Failure_CallsMetricsJobFailed()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        mockContext.Setup(c => c.Get("metrics_start_time")).Returns(100L);

        var jobException = new JobExecutionException("Job failed");

        // Act (When)
        await listener.JobWasExecuted(mockContext.Object, jobException, CancellationToken.None);

        // Assert (Then)
        mockMetrics.Verify(
            m => m.JobFailed(
                It.Is<JobExecutionInfo>(info => info.JobName == "TestJob"),
                It.Is<Exception>(ex => ex == jobException),
                It.IsAny<TimeSpan>()),
            Times.Once,
            "JobWasExecuted should call metrics.JobFailed when job fails.");
    }

    /// <summary>
    /// Unit test to verify that JobWasExecuted includes ResultMessage when context.Result is set.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobWasExecuted_WithContextResult_IncludesResultMessage()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        mockContext.Setup(c => c.Get("metrics_start_time")).Returns(100L);
        mockContext.Setup(c => c.Result).Returns("Processed 1,234 records");

        JobExecutionInfo? capturedInfo = null;
        mockMetrics.Setup(m => m.JobCompleted(It.IsAny<JobExecutionInfo>(), It.IsAny<TimeSpan>()))
            .Callback<JobExecutionInfo, TimeSpan>((info, duration) => capturedInfo = info);

        // Act (When)
        await listener.JobWasExecuted(mockContext.Object, null, CancellationToken.None);

        // Assert (Then)
        Assert.IsNotNull(
            capturedInfo,
            "JobExecutionInfo should be captured.");
        Assert.AreEqual(
            "Processed 1,234 records",
            capturedInfo.ResultMessage,
            "ResultMessage should contain context.Result value.");
    }

    /// <summary>
    /// Unit test to verify that JobWasExecuted handles missing timestamp gracefully.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobWasExecuted_MissingTimestamp_UsesZeroDuration()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        mockContext.Setup(c => c.Get("metrics_start_time")).Returns((object?)null);

        TimeSpan capturedDuration = TimeSpan.FromSeconds(-1);
        mockMetrics.Setup(m => m.JobCompleted(It.IsAny<JobExecutionInfo>(), It.IsAny<TimeSpan>()))
            .Callback<JobExecutionInfo, TimeSpan>((info, duration) => capturedDuration = duration);

        // Act (When)
        await listener.JobWasExecuted(mockContext.Object, null, CancellationToken.None);

        // Assert (Then)
        Assert.AreEqual(
            TimeSpan.Zero,
            capturedDuration,
            "Duration should be TimeSpan.Zero when timestamp is missing.");
    }

    /// <summary>
    /// Unit test to verify that JobWasExecuted does not propagate metrics exceptions.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task JobWasExecuted_MetricsThrows_DoesNotPropagateException()
    {
        // Arrange (Given)
        var mockMetrics = new Mock<ISchedulingMetrics>();
        mockMetrics.Setup(m => m.JobCompleted(It.IsAny<JobExecutionInfo>(), It.IsAny<TimeSpan>()))
            .Throws(new InvalidOperationException("Metrics error"));

        var mockLogger = new Mock<ILogger<SchedulingMetricsListener>>();
        var listener = new SchedulingMetricsListener(mockMetrics.Object, mockLogger.Object);

        var mockContext = this.CreateMockContext("TestJob", "TestGroup");
        mockContext.Setup(c => c.Get("metrics_start_time")).Returns(100L);

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await listener.JobWasExecuted(mockContext.Object, null, CancellationToken.None);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobWasExecuted should not propagate exceptions from metrics.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a mock IJobExecutionContext for testing.
    /// </summary>
    /// <param name="jobName">Job name.</param>
    /// <param name="jobGroup">Job group.</param>
    /// <returns>Mock context configured with test job details and trigger information.</returns>
    private Mock<IJobExecutionContext> CreateMockContext(string jobName, string jobGroup)
    {
        var mockContext = new Mock<IJobExecutionContext>();

        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.Setup(j => j.Key).Returns(new JobKey(jobName, jobGroup));

        var mockTrigger = new Mock<ITrigger>();
        mockTrigger.Setup(t => t.Key).Returns(new TriggerKey("TestTrigger", "TestTriggerGroup"));

        mockContext.Setup(c => c.JobDetail).Returns(mockJobDetail.Object);
        mockContext.Setup(c => c.Trigger).Returns(mockTrigger.Object);
        mockContext.Setup(c => c.FireInstanceId).Returns("test-instance-123");
        mockContext.Setup(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);
        mockContext.Setup(c => c.ScheduledFireTimeUtc).Returns(DateTimeOffset.UtcNow);
        mockContext.Setup(c => c.PreviousFireTimeUtc).Returns((DateTimeOffset?)null);
        mockContext.Setup(c => c.NextFireTimeUtc).Returns(DateTimeOffset.UtcNow.AddHours(1));

        return mockContext;
    }

    #endregion Private Methods
}