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
    /// Unit test to verify that the ILogger constructor handles incompatible logger type.
    /// </summary>
    [TestMethod]
    public void Constructor_IncompatibleILoggerType_InitializesWithNullLogger()
    {
        // Arrange (Given)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var incompatibleLogger = loggerFactory.CreateLogger<string>();

        // Act (When)
        var testInstance = new TestClass(incompatibleLogger);

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
        Assert.IsInstanceOfType(
            testInstance.Logger,
            typeof(NullLogger<TestClass>),
            "Logger should be an instance of NullLogger<TestClass> when incompatible logger type is provided.");
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.IsInstanceOfType(
            testInstance.LoggerFactory,
            typeof(NullLoggerFactory),
            "LoggerFactory should be an instance of NullLoggerFactory.");
    }

    /// <summary>
    /// Unit test to verify that the parameterless constructor initializes with NullLogger.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesWithNullLogger()
    {
        // Arrange (Given)

        // Act (When)
        var testInstance = new TestClass();

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
        Assert.IsInstanceOfType(
            testInstance.Logger,
            typeof(NullLogger<TestClass>),
            "Logger should be an instance of NullLogger<TestClass>.");
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.IsInstanceOfType(
            testInstance.LoggerFactory,
            typeof(NullLoggerFactory),
            "LoggerFactory should be an instance of NullLoggerFactory.");
    }

    /// <summary>
    /// Unit test to verify that the ILogger constructor handles null logger parameter.
    /// </summary>
    [TestMethod]
    public void Constructor_NullILogger_InitializesWithNullLogger()
    {
        // Arrange (Given)
        ILogger? nullLogger = null;

        // Act (When)
        var testInstance = new TestClass(nullLogger!);

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
        Assert.IsInstanceOfType(
            testInstance.Logger,
            typeof(NullLogger<TestClass>),
            "Logger should be an instance of NullLogger<TestClass> when null is provided.");
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.IsInstanceOfType(
            testInstance.LoggerFactory,
            typeof(NullLoggerFactory),
            "LoggerFactory should be an instance of NullLoggerFactory.");
    }

    /// <summary>
    /// Unit test to verify that the ILoggerFactory constructor handles null factory parameter.
    /// </summary>
    [TestMethod]
    public void Constructor_NullILoggerFactory_InitializesWithNullLoggerFactory()
    {
        // Arrange (Given)
        ILoggerFactory? nullLoggerFactory = null;

        // Act (When)
        var testInstance = new TestClass(nullLoggerFactory!);

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.IsInstanceOfType(
            testInstance.LoggerFactory,
            typeof(NullLoggerFactory),
            "LoggerFactory should be an instance of NullLoggerFactory when null is provided.");
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
    }

    /// <summary>
    /// Unit test to verify that the ILogger constructor initializes with the provided logger.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidILogger_InitializesWithProvidedLogger()
    {
        // Arrange (Given)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TestClass>();

        // Act (When)
        var testInstance = new TestClass(logger);

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
        Assert.AreSame(
            logger,
            testInstance.Logger,
            "Logger should be the same instance that was provided.");
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.IsInstanceOfType(
            testInstance.LoggerFactory,
            typeof(NullLoggerFactory),
            "LoggerFactory should be an instance of NullLoggerFactory when only logger is provided.");
    }

    /// <summary>
    /// Unit test to verify that the ILoggerFactory constructor initializes with the provided factory.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidILoggerFactory_InitializesWithProvidedFactory()
    {
        // Arrange (Given)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act (When)
        var testInstance = new TestClass(loggerFactory);

        // Assert (Then)
        Assert.IsNotNull(
            testInstance.LoggerFactory,
            "LoggerFactory should not be null.");
        Assert.AreSame(
            loggerFactory,
            testInstance.LoggerFactory,
            "LoggerFactory should be the same instance that was provided.");
        Assert.IsNotNull(
            testInstance.Logger,
            "Logger should not be null.");
        Assert.IsNotInstanceOfType(
            testInstance.Logger,
            typeof(NullLogger<TestClass>),
            "Logger should not be NullLogger when valid LoggerFactory is provided.");
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