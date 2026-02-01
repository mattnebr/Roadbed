/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Entities was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
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
/// Weather Station from the National Weather Service.
/// </summary>
public sealed class NwsStation
    : BaseClassWithLoggingFactory<NwsStation>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsStation"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Station.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    public NwsStation(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(messagingRequest);

        this.Id = id;
        this.MessagingRequest = messagingRequest;
        this.Repository = new NwsStationRepository(messagingRequest);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsStation"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Station.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Station repository to use in CRUD operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    internal NwsStation(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsStationRepository repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(messagingRequest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        this.Id = id;
        this.Repository = repository;
        this.MessagingRequest = messagingRequest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsStation"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Station.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Station repository to use in CRUD operations.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    internal NwsStation(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsStationRepository repository,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(messagingRequest);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this.Id = id;
        this.Repository = repository;
        this.MessagingRequest = messagingRequest;
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsStation"/> class.
    /// </summary>
    /// <remarks>
    /// Used for mapping API responses. Properties should be set via internal setters.
    /// </remarks>
    internal NwsStation()
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>
    /// Gets the County Identifier to the Weather Station.
    /// </summary>
    public string? CountyBusinessKey { get; internal set; }

    /// <summary>
    /// Gets the Fire Zone Identifier to the Weather Station.
    /// </summary>
    public string? FireZoneBusinessKey { get; internal set; }

    /// <summary>
    /// Gets the Forecast Identifier to the Weather Station.
    /// </summary>
    public string? ForecastZoneBusinessKey { get; internal set; }

    /// <summary>
    /// Gets the Identifier to the Weather Station.
    /// </summary>
    public string? Id { get; internal set; }

    /// <summary>
    /// Gets the Latitude to the Weather Station.
    /// </summary>
    public double? Latitude { get; internal set; }

    /// <summary>
    /// Gets the Longitude to the Weather Station.
    /// </summary>
    public double? Longitude { get; internal set; }

    /// <summary>
    /// Gets the Name to the Weather Station.
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets the TimeZone to the Weather Station.
    /// </summary>
    public string? TimeZone { get; internal set; }

    #endregion Public Properties

    #region Internal Properties

    /// <summary>
    /// Gets or sets the Messaging request for the interactions with the National Weather Service API.
    /// </summary>
    internal MessagingMessageRequest<CommonKeyValuePair<string, string>>? MessagingRequest { get; set; }

    /// <summary>
    /// Gets or sets the Repository.
    /// </summary>
    internal INwsStationRepository? Repository { get; set; }

    #endregion Internal Properties

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="NwsStation"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Station.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <returns>Entity filled with data from the API endpoint.</returns>
    public static async Task<NwsStation> FromId(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        NwsStation station = new NwsStation(id, messagingRequest);

        NwsStationResponse? response = await station.ReadAsync(cancellationToken);
        station.MapResponseToStation(response);

        return station;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Converts a response into a Station.
    /// </summary>
    /// <param name="response">Station response to convert.</param>
    private void MapResponseToStation(NwsStationResponse? response)
    {
        if (response == null)
        {
            this.LogWarning("Unable to map station response for ID {StationId}: response is null", this.Id ?? "unknown");
            return;
        }

        if (response.Properties == null)
        {
            this.LogWarning("Unable to map station response for ID {StationId}: properties is null", this.Id ?? "unknown");
            return;
        }

        if (response.Geometry == null)
        {
            this.LogWarning("Unable to map station response for ID {StationId}: geometry is null", this.Id ?? "unknown");
            return;
        }

        if (response.Geometry.Coordinates == null || response.Geometry.Coordinates.Length != 2)
        {
            this.LogWarning(
                "Unable to map station response for ID {StationId}: invalid coordinates",
                this.Id ?? "unknown");
            return;
        }

        this.Name = response.Properties.Name;
        this.TimeZone = response.Properties.TimeZone;

        // GeoJSON uses [longitude, latitude] order
        this.Latitude = response.Geometry.Coordinates[1];
        this.Longitude = response.Geometry.Coordinates[0];

        // Extract business keys from URLs
        if (Uri.TryCreate(response.Properties.ForecastUrl, UriKind.RelativeOrAbsolute, out Uri? forecastUrl) &&
            forecastUrl.Segments.Length > 0)
        {
            this.ForecastZoneBusinessKey = forecastUrl.Segments[^1];
        }

        if (Uri.TryCreate(response.Properties.CountyUrl, UriKind.RelativeOrAbsolute, out Uri? countyUrl) &&
            countyUrl.Segments.Length > 0)
        {
            this.CountyBusinessKey = countyUrl.Segments[^1];
        }

        if (Uri.TryCreate(response.Properties.FireWeatherZoneUrl, UriKind.RelativeOrAbsolute, out Uri? fireUrl) &&
            fireUrl.Segments.Length > 0)
        {
            this.FireZoneBusinessKey = fireUrl.Segments[^1];
        }
    }

    /// <summary>
    /// Retrieves the station data from the API.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Station response from the API.</returns>
    /// <exception cref="ArgumentException">Thrown when Id is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Repository or MessagingRequest is not initialized.</exception>
    private async Task<NwsStationResponse?> ReadAsync(
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(this.Id);

        if (this.Repository is null)
        {
            throw new InvalidOperationException("Repository is not initialized");
        }

        if (this.MessagingRequest is null)
        {
            throw new InvalidOperationException("MessagingRequest is not initialized");
        }

        return await this.Repository.ReadAsync(this.Id, cancellationToken);
    }

    #endregion Private Methods
}