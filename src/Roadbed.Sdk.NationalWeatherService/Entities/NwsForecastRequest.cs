/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Dtos was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
 */

namespace Roadbed.Sdk.NationalWeatherService;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService.Dtos;
using Roadbed.Sdk.NationalWeatherService.Repositories;

/// <summary>
/// Request for National Weather Service forecast based on latitude and longitude.
/// </summary>
/// <remarks>
/// This class handles the conversion from geographic coordinates (latitude/longitude)
/// to NWS grid coordinates (office ID, gridX, gridY) required for forecast requests.
/// </remarks>
public sealed class NwsForecastRequest
    : BaseClassWithLoggingFactory<NwsForecastRequest>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastRequest"/> class.
    /// </summary>
    /// <param name="officeId">Weather Forecast Office Identifier.</param>
    /// <param name="gridCoordinateX">X Grid Coordinate of the request.</param>
    /// <param name="gridCoordinateY">Y Grid Coordinate of the request.</param>
    /// <exception cref="ArgumentException">Thrown when officeId is null or whitespace.</exception>
    public NwsForecastRequest(
        string officeId,
        int gridCoordinateX,
        int gridCoordinateY)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(officeId);

        this.OfficeId = officeId;
        this.GridCoordinateX = gridCoordinateX;
        this.GridCoordinateY = gridCoordinateY;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastRequest"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    public NwsForecastRequest(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(messagingRequest);

        this.MessagingRequest = messagingRequest;
        this.Repository = new NwsGridCoordinateRepository(messagingRequest);
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastRequest"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Grid coordinate repository to use in CRUD operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    internal NwsForecastRequest(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsGridCoordinateRepository repository,
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
    /// Initializes a new instance of the <see cref="NwsForecastRequest"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Grid coordinate repository to use in CRUD operations.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    internal NwsForecastRequest(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsGridCoordinateRepository repository,
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
    /// Gets the X Grid Coordinate of the request.
    /// </summary>
    public int? GridCoordinateX { get; internal set; }

    /// <summary>
    /// Gets the Y Grid Coordinate of the request.
    /// </summary>
    public int? GridCoordinateY { get; internal set; }

    /// <summary>
    /// Gets the Office ID for the request.
    /// </summary>
    public string? OfficeId { get; internal set; }

    /// <summary>
    /// Gets the Physical Address for the Forecast.
    /// </summary>
    public NwsPhysicalAddress? PhysicalAddress { get; internal set; }

    /// <summary>
    /// Gets the Weather Forecast Office for the Forecast.
    /// </summary>
    public NwsOffice? WeatherForecastOffice { get; internal set; }

    #endregion Public Properties

    #region Internal Properties

    /// <summary>
    /// Gets or sets the Messaging request for the interactions with the National Weather Service API.
    /// </summary>
    internal MessagingMessageRequest<CommonKeyValuePair<string, string>>? MessagingRequest { get; set; }

    /// <summary>
    /// Gets or sets the Repository.
    /// </summary>
    internal INwsGridCoordinateRepository? Repository { get; set; }

    #endregion Internal Properties

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="NwsForecastRequest"/> class from geographic coordinates.
    /// </summary>
    /// <param name="latitude">Latitude of the coordinate.</param>
    /// <param name="longitude">Longitude of the coordinate.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <returns>Forecast request with grid coordinates populated from the API.</returns>
    public static async Task<NwsForecastRequest> FromLocation(
        double latitude,
        double longitude,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        NwsForecastRequest result = new NwsForecastRequest(messagingRequest);

        NwsPhysicalAddress coordinates = new NwsPhysicalAddress(latitude, longitude);
        NwsGridCoordinateResponse? response = await result.ReadAsync(
            coordinates,
            cancellationToken);

        result.MapResponseToForecastRequest(response);
        result.PhysicalAddress = coordinates;

        return result;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Maps grid coordinate response data to this forecast request.
    /// </summary>
    /// <param name="response">Grid coordinate response to convert.</param>
    private void MapResponseToForecastRequest(NwsGridCoordinateResponse? response)
    {
        if (response == null)
        {
            this.LogDebug("Unable to map grid coordinate response: response is null");
            return;
        }

        if (response.Properties == null)
        {
            this.LogDebug("Unable to map grid coordinate response: properties is null");
            return;
        }

        this.OfficeId = response.Properties.ForecastOfficeId;
        this.GridCoordinateX = response.Properties.GridCoordinateX;
        this.GridCoordinateY = response.Properties.GridCoordinateY;

        // Create Weather Forecast Office entity from response
        this.WeatherForecastOffice = NwsOffice.MapResponseToOffice(response);
    }

    /// <summary>
    /// Retrieves grid coordinate data from the API for the given physical location.
    /// </summary>
    /// <param name="coordinates">Geographic coordinates for the forecast.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Grid coordinate response from the API.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Repository or MessagingRequest is not initialized.</exception>
    private async Task<NwsGridCoordinateResponse?> ReadAsync(
        NwsPhysicalAddress coordinates,
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

        return await this.Repository.ReadAsync(coordinates, cancellationToken);
    }

    #endregion Private Methods
}