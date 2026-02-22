/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Dtos was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
 */

namespace Roadbed.Sdk.NationalWeatherService;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService.Dtos;
using Roadbed.Sdk.NationalWeatherService.Repositories;

/// <summary>
/// Hourly weather forecast from the National Weather Service.
/// </summary>
/// <remarks>
/// Provides hourly forecast periods for the next seven days based on NWS grid coordinates.
/// </remarks>
public sealed class NwsForecastHourly
    : BaseClassWithLoggingFactory<NwsForecastHourly>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastHourly"/> class.
    /// </summary>
    /// <param name="request">Forecast request for a specific grid coordinates.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public NwsForecastHourly(NwsForecastRequest request)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(request);

        this.ForecastRequest = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastHourly"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    public NwsForecastHourly(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(messagingRequest);

        this.MessagingRequest = messagingRequest;
        this.Repository = new NwsForecastHourlyRepository(messagingRequest);
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastHourly"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Hourly Forecast Repository to use in CRUD operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    internal NwsForecastHourly(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsForecastHourlyRepository repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(messagingRequest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        this.Repository = repository;
        this.MessagingRequest = messagingRequest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastHourly"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Hourly Forecast Repository to use in CRUD operations.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    internal NwsForecastHourly(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsForecastHourlyRepository repository,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(messagingRequest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.Repository = repository;
        this.MessagingRequest = messagingRequest;
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>
    /// Gets the timestamp when the forecast was created by the NWS.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; internal set; }

    /// <summary>
    /// Gets the forecast request containing grid coordinates.
    /// </summary>
    public NwsForecastRequest? ForecastRequest { get; internal set; }

    /// <summary>
    /// Gets the hourly forecast periods for the next seven days.
    /// </summary>
    public IList<NwsForecastPeriod>? Periods { get; internal set; }

    #endregion Public Properties

    #region Internal Properties

    /// <summary>
    /// Gets or sets the Messaging request for the interactions with the National Weather Service API.
    /// </summary>
    internal MessagingMessageRequest<CommonKeyValuePair<string, string>>? MessagingRequest { get; set; }

    /// <summary>
    /// Gets or sets the Repository.
    /// </summary>
    internal INwsForecastHourlyRepository? Repository { get; set; }

    #endregion Internal Properties

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="NwsForecastHourly"/> class from a forecast request.
    /// </summary>
    /// <param name="request">Request for National Weather Service forecast with grid coordinates.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <returns>Hourly forecast entity populated with data from the API endpoint.</returns>
    public static async Task<NwsForecastHourly> FromForecastRequest(
        NwsForecastRequest request,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        NwsForecastHourly result = new NwsForecastHourly(messagingRequest);

        NwsForecastResponse? response = await result.ReadAsync(
            request,
            cancellationToken);

        result.MapResponseToNwsForecastHourly(response);

        return result;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Maps hourly forecast response data to this forecast entity.
    /// </summary>
    /// <param name="response">Hourly forecast response to convert.</param>
    private void MapResponseToNwsForecastHourly(NwsForecastResponse? response)
    {
        if (response == null)
        {
            this.LogDebug("Unable to map hourly forecast response: response is null");
            return;
        }

        if (response.Properties == null)
        {
            this.LogDebug("Unable to map hourly forecast response: properties is null");
            return;
        }

        if (response.Properties.Periods == null)
        {
            this.LogDebug("Unable to map hourly forecast response: periods is null");
            return;
        }

        this.Periods = new List<NwsForecastPeriod>();

        foreach (NwsForecastPeriodResponse period in response.Properties.Periods)
        {
            this.Periods.Add(NwsForecastPeriod.FromPeriod(period));
        }

        // Use API Response as Forecast Created On. The API returns UTC values.
        if (!string.IsNullOrWhiteSpace(response.Properties.GeneratedAt) &&
            DateTimeOffset.TryParse(
                response.Properties.GeneratedAt,
                null,
                DateTimeStyles.AssumeUniversal,
                out DateTimeOffset createdOnResult))
        {
            this.CreatedOn = createdOnResult;
        }

        this.LogDebug(
            "Successfully mapped hourly forecast with {PeriodCount} periods",
            this.Periods.Count);
    }

    /// <summary>
    /// Retrieves hourly forecast data from the API.
    /// </summary>
    /// <param name="request">Request for National Weather Service forecast with grid coordinates.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Hourly forecast response from the API.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Repository or MessagingRequest is not initialized.</exception>
    private async Task<NwsForecastResponse?> ReadAsync(
        NwsForecastRequest request,
        CancellationToken cancellationToken)
    {
        if (this.Repository is null)
        {
            throw new InvalidOperationException("Repository is not initialized");
        }

        if (this.MessagingRequest is null)
        {
            throw new InvalidOperationException("MessagingRequest is not initialized");
        }

        return await this.Repository.ReadAsync(request, cancellationToken);
    }

    #endregion Private Methods
}