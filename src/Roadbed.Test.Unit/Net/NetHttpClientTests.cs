namespace Roadbed.Test.Unit.Net;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpClient"/> class.
/// </summary>
[TestClass]
public class NetHttpClientTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        ILogger<NetHttpClient> logger = NullLogger<NetHttpClient>.Instance;

        // Act (When)
        var instance = new NetHttpClient(handler, logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with valid parameters.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws when httpClientFactory is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        IHttpClientFactory? nullFactory = null;
        ILogger<NetHttpClient> logger = NullLogger<NetHttpClient>.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new NetHttpClient(nullFactory!, logger);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when httpClientFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that instance implements INetHttpClient interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsINetHttpClient()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        ILogger<NetHttpClient> logger = NullLogger<NetHttpClient>.Instance;

        // Act (When)
        var instance = new NetHttpClient(handler, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<INetHttpClient>(
            instance,
            "Instance should implement INetHttpClient.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        ILogger<NetHttpClient> logger = NullLogger<NetHttpClient>.Instance;

        // Act (When)
        var instance = new NetHttpClient(handler, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that MakeHttpRequestAsync throws when request is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        NetHttpClient client = CreateClient(handler);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await client.MakeHttpRequestAsync<string>(null!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "MakeHttpRequestAsync should throw ArgumentNullException when request is null.");
    }

    /// <summary>
    /// Unit test to verify that a successful response with string type returns raw body.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_SuccessWithStringType_ReturnsRawResponseBody()
    {
        // Arrange (Given)
        string expectedBody = "Hello, World!";
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, expectedBody);

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success for a 200 status code.");
        Assert.AreEqual(
            200,
            response.HttpStatusCode,
            "HttpStatusCode should be 200.");
        Assert.AreEqual(
            expectedBody,
            response.Data,
            "Data should contain the raw response body when type is string.");
    }

    /// <summary>
    /// Unit test to verify that a successful response with JSON type deserializes correctly.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_SuccessWithJsonType_DeserializesResponse()
    {
        // Arrange (Given)
        var expectedDto = new TestJsonDto { Name = "Test", Value = 42 };
        string jsonBody = JsonConvert.SerializeObject(expectedDto);
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, jsonBody);

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<TestJsonDto> response =
            await client.MakeHttpRequestAsync<TestJsonDto>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success for a deserialized JSON response.");
        Assert.IsNotNull(
            response.Data,
            "Data should not be null after successful deserialization.");
        Assert.AreEqual(
            "Test",
            response.Data.Name,
            "Deserialized Name should match the serialized value.");
        Assert.AreEqual(
            42,
            response.Data.Value,
            "Deserialized Value should match the serialized value.");
    }

    /// <summary>
    /// Unit test to verify that invalid JSON returns a failure response.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_SuccessWithInvalidJson_ReturnsFailure()
    {
        // Arrange (Given)
        string invalidJson = "not valid json {{{";
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, invalidJson);

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<TestJsonDto> response =
            await client.MakeHttpRequestAsync<TestJsonDto>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when JSON deserialization fails.");
        Assert.AreEqual(
            200,
            response.HttpStatusCode,
            "HttpStatusCode should reflect the original 200 from the server.");
        Assert.IsNotEmpty(
            response.Errors,
            "Errors should contain at least one entry for deserialization failure.");
        StringAssert.Contains(
            response.Errors[0],
            "deserialize",
            "Error message should mention deserialization failure.");
    }

    /// <summary>
    /// Unit test to verify that a 404 status code returns a failure response.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_NotFoundStatusCode_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.NotFound, "Not Found");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure for a 404 status code.");
        Assert.AreEqual(
            404,
            response.HttpStatusCode,
            "HttpStatusCode should be 404.");
    }

    /// <summary>
    /// Unit test to verify that a 500 status code returns a failure response.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_ServerErrorStatusCode_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError, "Server Error");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure for a 500 status code.");
        Assert.AreEqual(
            500,
            response.HttpStatusCode,
            "HttpStatusCode should be 500.");
    }

    /// <summary>
    /// Unit test to verify that Basic authentication sets the Authorization header.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_BasicAuthentication_SetsAuthorizationHeader()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.Authentication = new NetHttpAuthentication
        {
            AuthenticationType = NetHttpAuthenticationType.Basic,
            Value = "dXNlcjpwYXNz",
        };

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.AreEqual(
            1,
            handler.SendAsyncCallCount,
            "Handler should have been called exactly once.");

        var authorization = handler.SentRequests[0].Headers.Authorization;

        Assert.IsNotNull(
            authorization,
            "Authorization header should be set.");
        Assert.AreEqual(
            "Basic",
            authorization.Scheme,
            "Authorization scheme should be Basic.");
        Assert.AreEqual(
            "dXNlcjpwYXNz",
            authorization.Parameter,
            "Authorization parameter should be the encoded credentials.");
    }

    /// <summary>
    /// Unit test to verify that Bearer authentication sets the Authorization header.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_BearerAuthentication_SetsAuthorizationHeader()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.Authentication = new NetHttpAuthentication
        {
            AuthenticationType = NetHttpAuthenticationType.Bearer,
            Value = "my-jwt-token",
        };

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        var authorization = handler.SentRequests[0].Headers.Authorization;

        Assert.IsNotNull(
            authorization,
            "Authorization header should be set.");
        Assert.AreEqual(
            "Bearer",
            authorization.Scheme,
            "Authorization scheme should be Bearer.");
        Assert.AreEqual(
            "my-jwt-token",
            authorization.Parameter,
            "Authorization parameter should be the token value.");
    }

    /// <summary>
    /// Unit test to verify that Unknown authentication type does not set Authorization header.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_UnknownAuthentication_DoesNotSetAuthorizationHeader()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.Authentication = new NetHttpAuthentication
        {
            AuthenticationType = NetHttpAuthenticationType.Unknown,
            Value = "some-value",
        };

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsNull(
            handler.SentRequests[0].Headers.Authorization,
            "Authorization header should not be set for Unknown authentication type.");
    }

    /// <summary>
    /// Unit test to verify that null authentication does not set Authorization header.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_NullAuthentication_DoesNotSetAuthorizationHeader()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.Authentication = null;

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsNull(
            handler.SentRequests[0].Headers.Authorization,
            "Authorization header should not be set when authentication is null.");
    }

    /// <summary>
    /// Unit test to verify that custom headers are added to the request.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_CustomHeaders_AddsHeadersToRequest()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.HttpHeaders.Add(new NetHttpHeader("X-Custom-One", "Value1"));
        request.HttpHeaders.Add(new NetHttpHeader("X-Custom-Two", "Value2"));

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        HttpRequestMessage sentRequest = handler.SentRequests[0];

        Assert.IsTrue(
            sentRequest.Headers.Contains("X-Custom-One"),
            "Request should contain the X-Custom-One header.");
        Assert.AreEqual(
            "Value1",
            sentRequest.Headers.GetValues("X-Custom-One").First(),
            "X-Custom-One header should have the expected value.");

        Assert.IsTrue(
            sentRequest.Headers.Contains("X-Custom-Two"),
            "Request should contain the X-Custom-Two header.");
        Assert.AreEqual(
            "Value2",
            sentRequest.Headers.GetValues("X-Custom-Two").First(),
            "X-Custom-Two header should have the expected value.");
    }

    /// <summary>
    /// Unit test to verify that headers with empty names are skipped.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_EmptyHeaderName_SkipsHeader()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.HttpHeaders.Add(new NetHttpHeader(string.Empty, "SkippedValue"));
        request.HttpHeaders.Add(new NetHttpHeader("X-Valid", "ValidValue"));

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        HttpRequestMessage sentRequest = handler.SentRequests[0];

        Assert.IsTrue(
            sentRequest.Headers.Contains("X-Valid"),
            "Request should contain the valid header.");
        Assert.AreEqual(
            "ValidValue",
            sentRequest.Headers.GetValues("X-Valid").First(),
            "Valid header should have the expected value.");
    }

    /// <summary>
    /// Unit test to verify that compression enabled uses the CompressedClient.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_CompressionEnabled_UsesCompressedClient()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.EnableCompression = true;

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.AreEqual(
            "CompressedClient",
            handler.LastClientName,
            "Factory should be called with CompressedClient when compression is enabled.");
    }

    /// <summary>
    /// Unit test to verify that compression disabled uses the DefaultClient.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_CompressionDisabled_UsesDefaultClient()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.EnableCompression = false;

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.AreEqual(
            "DefaultClient",
            handler.LastClientName,
            "Factory should be called with DefaultClient when compression is disabled.");
    }

    /// <summary>
    /// Unit test to verify that a 503 ServiceUnavailable triggers retry and succeeds.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_ServiceUnavailable_RetriesAndSucceeds()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, "Unavailable");
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success after retry.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that a 408 RequestTimeout triggers retry and succeeds.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_RequestTimeout_RetriesAndSucceeds()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.RequestTimeout, "Timeout");
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success after retry.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that a 504 GatewayTimeout triggers retry and succeeds.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_GatewayTimeout_RetriesAndSucceeds()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.GatewayTimeout, "Gateway Timeout");
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success after retry.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that exhausting all retries returns a failure response.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_AllRetriesExhausted_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, "Unavailable");
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, "Unavailable");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when all retries are exhausted.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that HttpRequestException triggers retry and succeeds.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_HttpRequestException_RetriesAndSucceeds()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("Connection refused"));
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success after retrying a network error.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that exhausting retries after HttpRequestException returns failure.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_HttpRequestExceptionExhausted_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("Connection refused"));
        handler.EnqueueException(new HttpRequestException("Connection refused"));

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when all retry attempts fail with network errors.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that SocketException returns a failure response with status 500.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_SocketException_ReturnsFailureWith500()
    {
        // Arrange (Given)
        var factory = new ThrowingHttpClientFactory(
            new SocketException(10061));

        NetHttpClient client = new NetHttpClient(
            factory,
            NullLogger<NetHttpClient>.Instance);

        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when a SocketException occurs.");
        Assert.AreEqual(
            500,
            response.HttpStatusCode,
            "HttpStatusCode should be 500 for a SocketException.");
        Assert.IsNotEmpty(
            response.Errors,
            "Errors should contain at least one entry for a SocketException.");
        StringAssert.Contains(
            response.Errors[0],
            "An unknown error occurred with the socket.",
            "Error message should indicate a socket error.");
    }

    /// <summary>
    /// Unit test to verify that a general exception returns a failure response with status 500.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_GeneralException_ReturnsFailureWith500()
    {
        // Arrange (Given)
        var factory = new ThrowingHttpClientFactory(
            new InvalidOperationException("Something unexpected happened."));

        NetHttpClient client = new NetHttpClient(
            factory,
            NullLogger<NetHttpClient>.Instance);

        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when an unexpected exception occurs.");
        Assert.AreEqual(
            500,
            response.HttpStatusCode,
            "HttpStatusCode should be 500 for an unexpected exception.");
        Assert.IsNotEmpty(
            response.Errors,
            "Errors should contain at least one entry for an unexpected exception.");
        StringAssert.Contains(
            response.Errors[0],
            "An unknown error occurred.",
            "Error message should indicate an unknown error.");
    }

    /// <summary>
    /// Unit test to verify that TimeoutException triggers retry and succeeds.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_TimeoutException_RetriesAndSucceeds()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueException(new TimeoutException("Request timed out"));
        handler.EnqueueResponse(HttpStatusCode.OK, "success");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Response should indicate success after retrying a timeout.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that exhausting retries after TimeoutException returns failure.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_TimeoutExceptionExhausted_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueException(new TimeoutException("Request timed out"));
        handler.EnqueueException(new TimeoutException("Request timed out"));

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateRetryRequest(maxAttempts: 1);

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when all retry attempts fail with timeouts.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Handler should have been called twice (initial attempt plus one retry).");
    }

    /// <summary>
    /// Unit test to verify that HttpRequestException with SocketException inner
    /// is handled by the retry method and returns a failure response.
    /// </summary>
    /// <remarks>
    /// The <c>catch (HttpRequestException ex) when (ex.InnerException is SocketException)</c>
    /// block in MakeHttpRequestAsync is defensive code that is unreachable in the current
    /// implementation. The retry method catches all HttpRequestException instances and
    /// returns a response after exhaustion rather than rethrowing. This test verifies
    /// the retry exhaustion path for this specific exception type.
    /// </remarks>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_HttpRequestExceptionWithSocketInner_ReturnsFailure()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueException(
            new HttpRequestException(
                "Connection refused",
                new SocketException(10061)));

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();

        // Act (When)
        NetHttpResponse<string> response =
            await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.IsFalse(
            response.IsSuccessStatusCode,
            "Response should indicate failure when HttpRequestException with SocketException occurs.");
        Assert.AreEqual(
            1,
            handler.SendAsyncCallCount,
            "Handler should have been called exactly once with no retries.");
    }

    /// <summary>
    /// Unit test to verify that POST method is set correctly on the request.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_PostMethod_SetsCorrectMethodOnRequest()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "created");

        NetHttpClient client = CreateClient(handler);
        NetHttpRequest request = CreateNoRetryRequest();
        request.Method = HttpMethod.Post;

        // Act (When)
        await client.MakeHttpRequestAsync<string>(request);

        // Assert (Then)
        Assert.AreEqual(
            HttpMethod.Post,
            handler.SentRequests[0].Method,
            "The HTTP request should use the POST method.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a <see cref="NetHttpClient"/> with the specified mock handler.
    /// </summary>
    /// <param name="handler">Mock handler that also serves as IHttpClientFactory.</param>
    /// <returns>Configured <see cref="NetHttpClient"/> instance.</returns>
    private static NetHttpClient CreateClient(MockHttpMessageHandler handler)
    {
        return new NetHttpClient(
            handler,
            NullLogger<NetHttpClient>.Instance);
    }

    /// <summary>
    /// Creates a <see cref="NetHttpRequest"/> with retries disabled for simple tests.
    /// </summary>
    /// <returns>Request configured for a single attempt with no retry delay.</returns>
    private static NetHttpRequest CreateNoRetryRequest()
    {
        return new NetHttpRequest
        {
            HttpEndPoint = new Uri("https://api.example.com/test"),
            Method = HttpMethod.Get,
            TimeoutInSecondsPerAttempt = 30,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = 0,
                DelayMultiplierInSeconds = 1,
            },
        };
    }

    /// <summary>
    /// Creates a <see cref="NetHttpRequest"/> with retry enabled for retry tests.
    /// Uses <c>DelayMultiplierInSeconds = 1</c> to minimize test execution time
    /// (<c>Math.Pow(1, attempt)</c> = 1 second delay per retry).
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <returns>Request configured for retry testing.</returns>
    private static NetHttpRequest CreateRetryRequest(int maxAttempts = 1)
    {
        return new NetHttpRequest
        {
            HttpEndPoint = new Uri("https://api.example.com/test"),
            Method = HttpMethod.Get,
            TimeoutInSecondsPerAttempt = 30,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = maxAttempts,
                DelayMultiplierInSeconds = 1,
            },
        };
    }

    #endregion Private Methods

    #region Private Classes

    /// <summary>
    /// Simple DTO for testing JSON deserialization through NetHttpClient.
    /// </summary>
    private sealed class TestJsonDto
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }

    /// <summary>
    /// Mock <see cref="IHttpClientFactory"/> that throws a configured exception
    /// when <see cref="CreateClient"/> is called. Used to trigger exception paths
    /// that bypass the retry method's HttpRequestException and TimeoutException catches.
    /// </summary>
    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        private readonly Exception _exception;

        public ThrowingHttpClientFactory(Exception exception)
        {
            this._exception = exception;
        }

        public HttpClient CreateClient(string name)
        {
            throw this._exception;
        }
    }

    /// <summary>
    /// Custom <see cref="HttpContent"/> that throws a configured exception when
    /// the response body is read. Used to trigger exception paths in
    /// MakeHttpRequestAsync that occur after the retry method returns successfully.
    /// </summary>
    private sealed class ThrowingHttpContent : HttpContent
    {
        private readonly Exception _exception;

        public ThrowingHttpContent(Exception exception)
        {
            this._exception = exception;
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            System.Net.TransportContext? context)
        {
            throw this._exception;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    #endregion Private Classes
}