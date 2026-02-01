/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Dtos was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
 */

namespace Roadbed.Sdk.NationalWeatherService;

using System.Globalization;
using Microsoft.Extensions.Logging;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService.Dtos;
using Roadbed.Sdk.NationalWeatherService.Repositories;

/// <summary>
/// Request for National Weather Service forecast based on latitude and longitude.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NwsForecastDaily"/> class.
/// </remarks>
public sealed class NwsForecastDaily
    : BaseClassWithLoggingFactory<NwsForecastDaily>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastDaily"/> class.
    /// </summary>
    /// <param name="request">Forecast request for a specific grid coordinates.</param>
    /// <exception cref="ArgumentException">Thrown when request is null or whitespace.</exception>
    public NwsForecastDaily(
        NwsForecastRequest request)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(request);

        this.ForecastRequest = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastDaily"/> class.
    /// </summary>
    /// <param name="messagingRquest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    public NwsForecastDaily(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRquest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(messagingRquest);

        this.MessagingRequest = messagingRquest;
        this.Repository = new NwsForecastDailyRepository(messagingRquest);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastDaily"/> class.
    /// </summary>
    /// <param name="messagingRquest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Daily Forecast Repository to use in CRUD operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    internal NwsForecastDaily(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRquest,
        INwsForecastDailyRepository repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(messagingRquest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        this.Repository = repository;
        this.MessagingRequest = messagingRquest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastDaily"/> class.
    /// </summary>
    /// <param name="messagingRquest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Daily Forecast Repository to use in CRUD operations.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    internal NwsForecastDaily(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRquest,
        INwsForecastDailyRepository repository,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(messagingRquest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.Repository = repository;
        this.MessagingRequest = messagingRquest;
    }

    /// <summary>
    /// Gets the Office ID for the request.
    /// </summary>
    public NwsForecastRequest? ForecastRequest { get; internal set; }

    /// <summary>
    /// Gets the Created On for the Forecast.
    /// </summary>
    public DateTimeOffset? CreateOn { get; internal set; }

    /// <summary>
    /// Gets the Daily Forecast Points for the Forecast.
    /// </summary>
    public IList<NwsForecastPeriod>? Periods { get; internal set; }

    /// <summary>
    /// Gets or sets the Messaging request for the interactions with the National Weather Service API.
    /// </summary>
    internal MessagingMessageRequest<CommonKeyValuePair<string, string>>? MessagingRequest { get; set; }

    /// <summary>
    /// Gets or sets the Repository.
    /// </summary>
    internal INwsForecastDailyRepository? Repository { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="NwsForecastRequest"/> class.
    /// </summary>
    /// <param name="request">Request for National Weather Service forecast.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <returns>Entity filled with data from the API endpoint.</returns>
    public static async Task<NwsForecastDaily> FromForecastRequest(
        NwsForecastRequest request,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        NwsForecastDaily result = new NwsForecastDaily(messagingRequest);

        NwsForecastResponse? response = await result.ReadAsync(
            request,
            cancellationToken);

        result.MapResponseToNwsForecastDaily(response);

        return result;
    }

    /// <summary>
    /// Converts a response into a Forecast Request.
    /// </summary>
    /// <param name="response">Location response to convert.</param>
    private void MapResponseToNwsForecastDaily(NwsForecastResponse? response)
    {
        if ((response == null) ||
            (response.Properties == null) ||
            (response.Properties.Periods == null))
        {
            return;
        }

        this.Periods = new List<NwsForecastPeriod>();

        foreach (NwsForecastPeriodResponse period in response.Properties.Periods)
        {
            this.Periods.Add(NwsForecastPeriod.FromPeriod(period));
        }

        // Use API Response as Forecast Created On
        if (DateTimeOffset.TryParse(
            response!.Properties.GeneratedAt,
            null,
            DateTimeStyles.AssumeUniversal,
            out DateTimeOffset createdOnResult))
        {
            this.CreateOn = createdOnResult;
        }
    }

    /// <summary>
    /// Get the forecast data for a physical location.
    /// </summary>
    /// <param name="request">Request for National Weather Service forecast.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Forecast API response.</returns>
    private async Task<NwsForecastResponse?> ReadAsync(
        NwsForecastRequest request,
        CancellationToken cancellationToken)
    {
        if ((this.Repository is null) || (this.MessagingRequest == null))
        {
            return default;
        }

        return await this.Repository.ReadAsync(request, cancellationToken);
    }
}