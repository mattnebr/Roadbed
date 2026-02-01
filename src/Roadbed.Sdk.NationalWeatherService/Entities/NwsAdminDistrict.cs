/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Entities was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
 */

namespace Roadbed.Sdk.NationalWeatherService;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService.Dtos;
using Roadbed.Sdk.NationalWeatherService.Repositories;

/// <summary>
/// Entity for the National Weather Service administrative district (state/territory).
/// </summary>
public sealed class NwsAdminDistrict
    : BaseClassWithLoggingFactory<NwsAdminDistrict>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsAdminDistrict"/> class.
    /// </summary>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <remarks>
    /// The Name in the <see cref="MessagingPublisher"/> is used as the User Agent string for the National Weather Service API.
    /// This string can be anything, and the more unique to your application the less likely it will be affected by a security event.
    /// If you include contact information (website or email), we can contact you if your string is associated to a security event.
    /// This will be replaced with an API key in the future.
    /// </remarks>
    public NwsAdminDistrict(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentNullException.ThrowIfNull(messagingRequest);

        this.MessagingRequest = messagingRequest;
        this.Repository = new NwsAdminDistrictRepository(messagingRequest);
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the weather stations for the administrative district.
    /// </summary>
    public IList<NwsStation>? Stations { get; internal set; }

    /// <summary>
    /// Gets the two-character code for the administrative district.
    /// </summary>
    public AdminDistrictTwoCharacterCode TwoCharacterCode { get; internal set; }

    #endregion Public Properties

    #region Internal Properties

    /// <summary>
    /// Gets or sets the Messaging request for the interactions with the National Weather Service API.
    /// </summary>
    internal MessagingMessageRequest<CommonKeyValuePair<string, string>> MessagingRequest { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="INwsAdminDistrictRepository"/> Repository.
    /// </summary>
    internal INwsAdminDistrictRepository Repository { get; set; }

    #endregion Internal Properties

    #region Public Methods

    /// <summary>
    /// Creates an administrative district entity with all weather stations for the given district code.
    /// </summary>
    /// <param name="district">Administrative district two-character code to use with the National Weather Service.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <returns>Administrative district entity with all weather stations populated.</returns>
    public static async Task<NwsAdminDistrict> FromAdminDistrict(
        AdminDistrictTwoCharacterCode district,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        // Create entity
        NwsAdminDistrict adminDistrict = new NwsAdminDistrict(messagingRequest);

        // Populate stations from API
        // The repository handles pagination and makes multiple API calls to create a single result
        NwsAdminDistrict result = await adminDistrict.ListAsync(district, cancellationToken);
        result.TwoCharacterCode = district;

        return result;
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Converts an observation station feature response into a weather station entity.
    /// </summary>
    /// <param name="feature">Observation station feature to convert.</param>
    /// <returns>Weather station entity, or null if conversion fails.</returns>
    internal static NwsStation? MapFeatureToStation(NwsStationFeaturesResponse feature)
    {
        // Validate required data
        if (feature == null)
        {
            return null;
        }

        if (feature.Geometry == null)
        {
            return null;
        }

        if (feature.Properties == null)
        {
            return null;
        }

        if (feature.Geometry.Coordinates == null || feature.Geometry.Coordinates.Length != 2)
        {
            return null;
        }

        // Validate and parse URLs
        if (!Uri.TryCreate(feature.Properties.ForecastUrl, UriKind.RelativeOrAbsolute, out Uri? forecastUri) ||
            forecastUri.Segments.Length == 0)
        {
            return null;
        }

        if (!Uri.TryCreate(feature.Properties.CountyUrl, UriKind.RelativeOrAbsolute, out Uri? countyUri) ||
            countyUri.Segments.Length == 0)
        {
            return null;
        }

        if (!Uri.TryCreate(feature.Properties.FireWeatherZoneUrl, UriKind.RelativeOrAbsolute, out Uri? fireUri) ||
            fireUri.Segments.Length == 0)
        {
            return null;
        }

        // Create station entity
        NwsStation station = new NwsStation
        {
            Id = feature.Properties.StationIdentifier,
            Name = feature.Properties.Name,
            Latitude = feature.Geometry.Coordinates[1],
            Longitude = feature.Geometry.Coordinates[0],
            TimeZone = feature.Properties.TimeZone,
            ForecastZoneBusinessKey = forecastUri.Segments[^1],
            CountyBusinessKey = countyUri.Segments[^1],
            FireZoneBusinessKey = fireUri.Segments[^1],
        };

        return station;
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Retrieves all weather stations for the administrative district from the API.
    /// </summary>
    /// <param name="district">Administrative district two-character code to use with the National Weather Service.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>This administrative district entity with stations populated.</returns>
    private async Task<NwsAdminDistrict> ListAsync(
        AdminDistrictTwoCharacterCode district,
        CancellationToken cancellationToken)
    {
        this.LogTrace("List operation called for district {District}", district);

        // Retrieve stations from API (handles pagination internally)
        this.Stations = await this.Repository.ListAsync(district, cancellationToken);

        this.LogDebug(
            "Retrieved {StationCount} stations for district {District}",
            this.Stations?.Count ?? 0,
            district);

        return this;
    }

    #endregion Private Methods
}