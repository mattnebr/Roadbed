namespace Roadbed.Sdk.NationalWeatherService.Repositories;

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Net;

/// <summary>
/// Base repository for National Weather Service SDK.
/// </summary>
internal abstract class BaseNwsRepository
    : BaseClassWithLoggingFactory<BaseNwsRepository>
{
    #region Public Fields

    /// <summary>
    /// Base API path for the National Weather Service RESTful API.
    /// </summary>
    public const string BaseApiPath = "https://api.weather.gov";

    #endregion Public Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseNwsRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    protected BaseNwsRepository(MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(request);
        this.Request = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseNwsRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    protected BaseNwsRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.Request = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseNwsRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    protected BaseNwsRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.Request = request;
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>
    /// Gets the message request.
    /// </summary>
    protected MessagingMessageRequest<CommonKeyValuePair<string, string>> Request { get; private set; }

    #endregion Protected Properties

    #region Protected Methods

    /// <summary>
    /// Creates a GET HTTP request for the specified endpoint path.
    /// </summary>
    /// <param name="endPointPath">API endpoint path (must be a valid absolute URI).</param>
    /// <returns>HTTP GET request for the National Weather Service API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when endPointPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when endPointPath is not a valid absolute URI.</exception>
    protected NetHttpRequest CreateHttpGetRequest(string endPointPath)
    {
        ArgumentNullException.ThrowIfNull(endPointPath);

        if (!Uri.IsWellFormedUriString(endPointPath, UriKind.Absolute))
        {
            throw new ArgumentException(
                "Endpoint path must be a valid absolute URI.",
                nameof(endPointPath));
        }

        // Get User-Agent from publisher (with fallback)
        string appName = "NWS-SDK-Client";

        if (!string.IsNullOrWhiteSpace(this.Request.Publisher.Identifier))
        {
            appName = this.Request.Publisher.Identifier;
        }

        if (!string.IsNullOrWhiteSpace(this.Request.Publisher.Name?.Key))
        {
            appName = this.Request.Publisher.Name.Key;
        }

        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri(endPointPath),
            HttpHeaders = new List<NetHttpHeader>
            {
                new NetHttpHeader("User-Agent", appName),
                new NetHttpHeader("Accept", "application/geo+json"),
            },
            EnableCompression = false,
        };

        return request;
    }

    #endregion Protected Methods
}