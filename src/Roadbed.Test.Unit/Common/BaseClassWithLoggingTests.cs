namespace Roadbed.Test.Unit.Common;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed;

/// <summary>
/// Contains unit tests for verifying the behavior of the BaseClassWithLogging class.
/// </summary>
[TestClass]
public class BaseClassWithLoggingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that BeginScope returns a valid scope when valid parameters are provided.
    /// </summary>
    [TestMethod]
    public void BeginScope_ValidParameters_ReturnsScope()
    {
        // Arrange (Given)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var testInstance = new TestClass(loggerFactory);
        string key = "testKey";
        object value = "testValue";

        // Act (When)
        var scope = testInstance.BeginScope(key, value);

        // Assert (Then)
        Assert.IsNotNull(
            scope,
            "BeginScope should return a non-null IDisposable when valid parameters are provided.");
    }

    /// <summary>
    /// Unit test to verify that LogDebug logs a message without parameters.
    /// </summary>
    [TestMethod]
    public void LogDebug_MessageOnly_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Debug message";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogDebug(message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogDebug should not throw an exception when called with a valid message.");
    }

    /// <summary>
    /// Unit test to verify that LogDebug logs a message with parameters.
    /// </summary>
    [TestMethod]
    public void LogDebug_MessageWithParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Debug message with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogDebug(message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogDebug should not throw an exception when called with a valid message and parameters.");
    }

    /// <summary>
    /// Unit test to verify that LogError logs a message without parameters.
    /// </summary>
    [TestMethod]
    public void LogError_MessageOnly_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Error message";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogError(message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogError should not throw an exception when called with a valid message.");
    }

    /// <summary>
    /// Unit test to verify that LogError logs a message with parameters.
    /// </summary>
    [TestMethod]
    public void LogError_MessageWithParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Error message with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogError(message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogError should not throw an exception when called with a valid message and parameters.");
    }

    /// <summary>
    /// Unit test to verify that LogError logs an exception with a message.
    /// </summary>
    [TestMethod]
    public void LogError_ExceptionWithMessage_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        var exception = new InvalidOperationException("Test exception");
        string message = "Error occurred";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogError(exception, message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogError should not throw an exception when called with a valid exception and message.");
    }

    /// <summary>
    /// Unit test to verify that LogError logs an exception with a message and parameters.
    /// </summary>
    [TestMethod]
    public void LogError_ExceptionWithMessageAndParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        var exception = new InvalidOperationException("Test exception");
        string message = "Error occurred with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogError(exception, message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogError should not throw an exception when called with a valid exception, message, and parameters.");
    }

    /// <summary>
    /// Unit test to verify that LogInformation logs a message without parameters.
    /// </summary>
    [TestMethod]
    public void LogInformation_MessageOnly_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Information message";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogInformation(message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogInformation should not throw an exception when called with a valid message.");
    }

    /// <summary>
    /// Unit test to verify that LogInformation logs a message with parameters.
    /// </summary>
    [TestMethod]
    public void LogInformation_MessageWithParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Information message with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogInformation(message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogInformation should not throw an exception when called with a valid message and parameters.");
    }

    /// <summary>
    /// Unit test to verify that LogTrace logs a message without parameters.
    /// </summary>
    [TestMethod]
    public void LogTrace_MessageOnly_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Trace message";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogTrace(message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogTrace should not throw an exception when called with a valid message.");
    }

    /// <summary>
    /// Unit test to verify that LogTrace logs a message with parameters.
    /// </summary>
    [TestMethod]
    public void LogTrace_MessageWithParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Trace message with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogTrace(message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogTrace should not throw an exception when called with a valid message and parameters.");
    }

    /// <summary>
    /// Unit test to verify that LogWarning logs a message without parameters.
    /// </summary>
    [TestMethod]
    public void LogWarning_MessageOnly_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Warning message";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogWarning(message);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogWarning should not throw an exception when called with a valid message.");
    }

    /// <summary>
    /// Unit test to verify that LogWarning logs a message with parameters.
    /// </summary>
    [TestMethod]
    public void LogWarning_MessageWithParameters_DoesNotThrowException()
    {
        // Arrange (Given)
        var testInstance = new TestClass();
        string message = "Warning message with {Parameter}";
        object[] parameters = new object[] { "value" };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            testInstance.LogWarning(message, parameters);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "LogWarning should not throw an exception when called with a valid message and parameters.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Test implementation of BaseClassWithLogging for testing purposes.
    /// </summary>
    private class TestClass : BaseClassWithLoggingFactory<TestClass>
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClass"/> class.
        /// </summary>
        public TestClass()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClass"/> class.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public TestClass(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClass"/> class.
        /// </summary>
        /// <param name="loggerFactory">Represents a type used to configure the logging system.</param>
        public TestClass(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }

        #endregion Public Constructors
    }

    #endregion Private Classes
}