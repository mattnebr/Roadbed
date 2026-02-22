namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Scheduling;
using System;

/// <summary>
/// Contains unit tests for verifying the behavior of the LoggingMetricsAdapter class.
/// </summary>
[TestClass]
public class LoggingMetricsAdapterTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes with valid logger.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidLogger_CreatesInstance()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();

        // Act (When)
        var adapter = new LoggingMetricsAdapter(mockLogger.Object);

        // Assert (Then)
        Assert.IsNotNull(
            adapter,
            "Constructor should create instance with valid logger.");
    }

    /// <summary>
    /// Unit test to verify that JobStarted calls logger when Information level is enabled.
    /// </summary>
    [TestMethod]
    public void JobStarted_ValidInfo_CallsLogger()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance-123",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        // Act (When)
        adapter.JobStarted(info);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobStarted should log at Debug level.");
    }

    /// <summary>
    /// Unit test to verify that JobCompleted with ResultMessage logs the result.
    /// </summary>
    [TestMethod]
    public void JobCompleted_WithResultMessage_LogsResult()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Processed 1,234 records",
        };
        var duration = TimeSpan.FromSeconds(5);

        // Act (When)
        adapter.JobCompleted(info, duration);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobCompleted should log at Debug level when ResultMessage is present.");
    }

    /// <summary>
    /// Unit test to verify that JobCompleted without ResultMessage logs without the result.
    /// </summary>
    [TestMethod]
    public void JobCompleted_WithoutResultMessage_LogsWithoutResult()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = null,
        };
        var duration = TimeSpan.FromSeconds(5);

        // Act (When)
        adapter.JobCompleted(info, duration);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobCompleted should log at Debug level even without ResultMessage.");
    }

    /// <summary>
    /// Unit test to verify that JobFailed with ResultMessage logs the result.
    /// </summary>
    [TestMethod]
    public void JobFailed_WithResultMessage_LogsResult()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Partial result: processed 500 records",
        };
        var exception = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromSeconds(3);

        // Act (When)
        adapter.JobFailed(info, exception, duration);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobFailed should log at Warning level with exception when ResultMessage is present.");
    }

    /// <summary>
    /// Unit test to verify that JobFailed without ResultMessage logs without the result.
    /// </summary>
    [TestMethod]
    public void JobFailed_WithoutResultMessage_LogsWithoutResult()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = null,
        };
        var exception = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromSeconds(3);

        // Act (When)
        adapter.JobFailed(info, exception, duration);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobFailed should log at Warning level with exception even without ResultMessage.");
    }

    /// <summary>
    /// Unit test to verify that JobMisfired logs at Warning level.
    /// </summary>
    [TestMethod]
    public void JobMisfired_ValidInfo_LogsAtWarningLevel()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<LoggingMetricsAdapter>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        var adapter = new LoggingMetricsAdapter(mockLogger.Object);
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        // Act (When)
        adapter.JobMisfired(info);

        // Assert (Then)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "JobMisfired should log at Warning level.");
    }

    #endregion Public Methods
}