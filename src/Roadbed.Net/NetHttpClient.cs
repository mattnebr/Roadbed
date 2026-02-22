namespace Roadbed.Net;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Service for managing HttpClient operations using IHttpClientFactory.
/// </summary>
public class NetHttpClient
    : BaseClassWithLogging, INetHttpClient
{
    #region Private Fields

    /// <summary>
    /// Factory for creating HttpClient instances.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClientFactory"/> is <c>null</c>.</exception>
    public NetHttpClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NetHttpClient> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this._httpClientFactory = httpClientFactory;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<NetHttpResponse<T>> MakeHttpRequestAsync<T>(
        NetHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.LogDebug(
            "HTTP {Method} {Endpoint} (Compression: {Compression})",
            request.Method,
            request.HttpEndPoint?.ToString() ?? "unknown",
            request.EnableCompression);

        // Response Container
        NetHttpResponse<T> response;

        try
        {
            HttpResponseMessage message =
                await this.MakeRequestWithBackoffRetryAsync(request, cancellationToken);

            // Add the Data
            if (message.IsSuccessStatusCode &&
                (message.StatusCode != HttpStatusCode.NotFound))
            {
                // Grab the body of the response
                string responseBody = await message.Content.ReadAsStringAsync(cancellationToken);

                this.LogDebug(
                    "HTTP {StatusCode} from {Endpoint}",
                    (int)message.StatusCode,
                    request.HttpEndPoint?.ToString() ?? "unknown");

                // Return raw string or deserialize JSON based on type parameter
                if (typeof(T) == typeof(string))
                {
                    response = NetHttpResponse<T>.Success(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        (T)(object)responseBody);
                }
                else
                {
                    try
                    {
                        T? deserialized = JsonConvert.DeserializeObject<T>(responseBody);

                        response = NetHttpResponse<T>.Success(
                            (int)message.StatusCode,
                            message.ReasonPhrase,
                            deserialized!);
                    }
                    catch (JsonException ex)
                    {
                        this.LogDebug(
                            ex,
                            "Failed to deserialize response from {Endpoint} to {Type}",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            typeof(T).Name);

                        response = NetHttpResponse<T>.Failure(
                            (int)message.StatusCode,
                            message.ReasonPhrase,
                            $"Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
            else
            {
                response = NetHttpResponse<T>.Failure(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        "Not a successful HTTP call. Status code indicates a failed HTTP Request.");
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            response = this.HandleHttpRequestWithSocketException<T>(ex, request);
        }
        catch (SocketException ex)
        {
            this.LogDebug(
                ex,
                "Socket error calling {Endpoint}",
                request.HttpEndPoint?.ToString() ?? "unknown");

            response = NetHttpResponse<T>.Failure(
                500,
                string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
                "Not a successful HTTP call. An unknown error occurred with the socket.");
        }
        catch (Exception ex)
        {
            this.LogDebug(
                ex,
                "Unexpected error calling {Endpoint}",
                request.HttpEndPoint?.ToString() ?? "unknown");

            response = NetHttpResponse<T>.Failure(
                500,
                string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
                "Not a successful HTTP call. An unknown error occurred.");
        }

        return response;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Create <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpRequestMessage"/>.</param>
    /// <returns><see cref="HttpRequestMessage"/> based on the <see cref="NetHttpRequest"/>.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <remarks>
    /// This method creates a NEW HttpRequestMessage instance each time it is called.
    /// This is required because HttpRequestMessage can only be sent once.
    /// The retry pattern calls this method for each retry attempt.
    /// </remarks>
    private static HttpRequestMessage CreateHttpRequestMessage(NetHttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpRequestMessage message = new HttpRequestMessage(
            request.Method,
            request.HttpEndPoint)
        {
            Content = request.Content,
        };

        // Add HTTP Headers
        if (request.HttpHeaders != null)
        {
            foreach (NetHttpHeader header in request.HttpHeaders)
            {
                if (!string.IsNullOrEmpty(header.Name))
                {
                    message.Headers.Add(header.Name.Trim(), header.Value);
                }
            }
        }

        // Add Authentication
        if ((request.Authentication != null) &&
            (request.Authentication.AuthenticationType != NetHttpAuthenticationType.Unknown))
        {
            string name = request.Authentication.AuthenticationType switch
            {
                NetHttpAuthenticationType.Basic => "Basic",
                NetHttpAuthenticationType.Bearer => "Bearer",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(name))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue(
                    name,
                    request.Authentication.Value);
            }
        }

        return message;
    }

    /// <summary>
    /// Implements the wait logic for the backoff strategy.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the backoff calculation.</param>
    /// <param name="attempt">Count representing which attempt.</param>
    /// <param name="cancellationToken">Token to cancel tasks.</param>
    /// <returns>Completed Task after the delay.</returns>
    private static async Task WaitAsync(
        NetHttpRequest request,
        int attempt,
        CancellationToken cancellationToken)
    {
        // Backoff strategy - increase delay with each attempt
        var amount = Math.Pow(request.RetryPattern.DelayMultiplierInSeconds, attempt);
        await Task.Delay(TimeSpan.FromSeconds(amount), cancellationToken);
    }

    /// <summary>
    /// Create <see cref="HttpClient"/> using the factory.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpClient"/>.</param>
    /// <returns><see cref="HttpClient"/> based on the <see cref="NetHttpRequest"/>.</returns>
    private HttpClient CreateHttpClient(NetHttpRequest request)
    {
        // Create client with named configuration or default
        string clientName = request.EnableCompression ? "CompressedClient" : "DefaultClient";

        HttpClient client = this._httpClientFactory.CreateClient(clientName);

        // Set timeout per request
        client.Timeout = TimeSpan.FromSeconds(request.TimeoutInSecondsPerAttempt);

        return client;
    }

    /// <summary>
    /// Implementation of the retry pattern with backoff strategy.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpClient"/>.</param>
    /// <param name="cancellationToken">Token to cancel tasks.</param>
    /// <returns>API response.</returns>
    /// <remarks>
    /// This method creates a NEW HttpRequestMessage for each retry attempt.
    /// HttpRequestMessage can only be sent once, so we must create a fresh instance
    /// for each attempt in the retry loop.
    /// </remarks>
    private async Task<HttpResponseMessage> MakeRequestWithBackoffRetryAsync(
        NetHttpRequest request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;
        int totalAttempts = request.RetryPattern.MaxAttempts + 1;

        for (int attempt = 0; attempt <= request.RetryPattern.MaxAttempts; attempt++)
        {
            // Create a NEW HttpRequestMessage for THIS attempt
            using (HttpRequestMessage message = CreateHttpRequestMessage(request))
            {
                try
                {
                    using (HttpClient client = this.CreateHttpClient(request))
                    {
                        // Send request with timeout
                        HttpResponseMessage response = await client.SendAsync(
                            message,
                            HttpCompletionOption.ResponseContentRead,
                            cancellationToken)
                            .WaitAsync(
                                TimeSpan.FromSeconds(request.TimeoutInSecondsPerAttempt),
                                cancellationToken);

                        // Determine if we want to try again
                        if ((response.StatusCode == HttpStatusCode.ServiceUnavailable) ||
                            (response.StatusCode == HttpStatusCode.RequestTimeout) ||
                            (response.StatusCode == HttpStatusCode.GatewayTimeout))
                        {
                            lastResponse = response;

                            // Don't retry on last attempt
                            if (attempt < request.RetryPattern.MaxAttempts)
                            {
                                this.LogDebug(
                                    "HTTP {StatusCode} from {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                                    (int)response.StatusCode,
                                    request.HttpEndPoint?.ToString() ?? "unknown",
                                    attempt + 1,
                                    totalAttempts);

                                await WaitAsync(request, attempt, cancellationToken);
                                continue;
                            }

                            // Last attempt with retriable status code, fall through to return below
                            this.LogWarning(
                                "All retry attempts exhausted for {Method} {Endpoint}, last status {StatusCode}",
                                request.Method,
                                request.HttpEndPoint?.ToString() ?? "unknown",
                                (int)response.StatusCode);

                            break;
                        }

                        // Success or non-retriable status code
                        return response;
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Networking issue occurred - retry if attempts remain
                    if (attempt < request.RetryPattern.MaxAttempts)
                    {
                        this.LogDebug(
                            ex,
                            "Network error calling {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            attempt + 1,
                            totalAttempts);

                        await WaitAsync(request, attempt, cancellationToken);
                        continue;
                    }

                    // No more attempts, fall through to return BadRequest below
                    this.LogWarning(
                        ex,
                        "All retry attempts exhausted for {Method} {Endpoint} due to network error",
                        request.Method,
                        request.HttpEndPoint?.ToString() ?? "unknown");

                    break;
                }
                catch (TimeoutException ex)
                {
                    // Task timeout occurred - retry if attempts remain
                    if (attempt < request.RetryPattern.MaxAttempts)
                    {
                        this.LogDebug(
                            ex,
                            "Timeout calling {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            attempt + 1,
                            totalAttempts);

                        await WaitAsync(request, attempt, cancellationToken);
                        continue;
                    }

                    // No more attempts, fall through to return BadRequest below
                    this.LogWarning(
                        ex,
                        "All retry attempts exhausted for {Method} {Endpoint} due to timeout",
                        request.Method?.ToString() ?? "unknown",
                        request.HttpEndPoint?.ToString() ?? "unknown");

                    break;
                }
            } // HttpRequestMessage is disposed here after each attempt
        }

        // Retry pattern exhausted - return last response if available, otherwise BadRequest
        return lastResponse ?? new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Unable to complete Http Request."),
        };
    }

    /// <summary>
    /// Handles an <see cref="HttpRequestException"/> whose inner exception is a
    /// <see cref="SocketException"/>. This is defensive code that guards against
    /// future changes introducing a code path where such an exception escapes the
    /// retry method.
    /// </summary>
    /// <typeparam name="T">Type of the expected response data.</typeparam>
    /// <param name="ex">The caught exception.</param>
    /// <param name="request">The original HTTP request.</param>
    /// <returns><see cref="NetHttpResponse{T}"/> indicating failure with status 500.</returns>
    [ExcludeFromCodeCoverage]
    private NetHttpResponse<T> HandleHttpRequestWithSocketException<T>(
        HttpRequestException ex,
        NetHttpRequest request)
    {
        this.LogDebug(
            ex,
            "Socket error calling {Endpoint}",
            request.HttpEndPoint?.ToString() ?? "unknown");

        return NetHttpResponse<T>.Failure(
            500,
            string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
            "Not a successful HTTP call. An unknown error occurred with the HTTP Request.");
    }

    #endregion Private Methods
}