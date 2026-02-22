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
/// Weather Forecast Office from the National Weather Service.
/// </summary>
public sealed class NwsOffice
    : BaseClassWithLoggingFactory<NwsOffice>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsOffice"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Office.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    public NwsOffice(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(messagingRequest);

        this.Id = id;
        this.MessagingRequest = messagingRequest;
        this.Repository = new NwsOfficeRepository(messagingRequest);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsOffice"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Office.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Office repository to use in CRUD operations.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    internal NwsOffice(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsOfficeRepository repository,
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
    /// Initializes a new instance of the <see cref="NwsOffice"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Office.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="repository">Office repository to use in CRUD operations.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of ILogger from the registered ILoggerProviders.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    internal NwsOffice(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        INwsOfficeRepository repository,
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
    /// Initializes a new instance of the <see cref="NwsOffice"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Office.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <remarks>
    /// Used for mapping API responses. Properties should be set via internal setters.
    /// </remarks>
    internal NwsOffice(string id)
        : base(ServiceLocator.GetService<ILoggerFactory>())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        this.Id = id;
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>
    /// Gets the city or locality.
    /// </summary>
    public string? AddressLocality { get; internal set; }

    /// <summary>
    /// Gets the state or region (e.g., "CA").
    /// </summary>
    public string? AddressRegion { get; internal set; }

    /// <summary>
    /// Gets the list of approved observation station business keys.
    /// </summary>
    public List<CommonBusinessKey>? ApprovedObservationStationBusinessKeys { get; internal set; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string? Email { get; internal set; }

    /// <summary>
    /// Gets the fax number.
    /// </summary>
    public string? FaxNumber { get; internal set; }

    /// <summary>
    /// Gets the identifier to the Weather Office.
    /// </summary>
    public string? Id { get; internal set; }

    /// <summary>
    /// Gets the name of the Weather Office.
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets the NWS region code (e.g., "wr" for Western Region).
    /// </summary>
    public string? NwsRegion { get; internal set; }

    /// <summary>
    /// Gets the parent organization identifier.
    /// </summary>
    public string? ParentOrganization { get; internal set; }

    /// <summary>
    /// Gets the postal code (ZIP code).
    /// </summary>
    public string? PostalCode { get; internal set; }

    /// <summary>
    /// Gets the list of responsible county zone business keys.
    /// </summary>
    public List<CommonBusinessKey>? ResponsibleCountyBusinessKeys { get; internal set; }

    /// <summary>
    /// Gets the list of responsible fire zone business keys.
    /// </summary>
    public List<CommonBusinessKey>? ResponsibleFireZoneBusinessKeys { get; internal set; }

    /// <summary>
    /// Gets the list of responsible forecast zone business keys.
    /// </summary>
    public List<CommonBusinessKey>? ResponsibleForecastZoneBusinessKeys { get; internal set; }

    /// <summary>
    /// Gets the street address.
    /// </summary>
    public string? StreetAddress { get; internal set; }

    /// <summary>
    /// Gets the telephone number.
    /// </summary>
    public string? Telephone { get; internal set; }

    /// <summary>
    /// Gets the time zone of the Weather Office.
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
    internal INwsOfficeRepository? Repository { get; set; }

    #endregion Internal Properties

    #region Public Methods

    /// <summary>
    /// Creates a new instance of the <see cref="NwsOffice"/> class.
    /// </summary>
    /// <param name="id">Identifier to the Weather Office.</param>
    /// <param name="messagingRequest">Messaging request for messages sent to API.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when messagingRequest is null.</exception>
    /// <returns>Entity filled with data from the API endpoint.</returns>
    public static async Task<NwsOffice> FromId(
        string id,
        MessagingMessageRequest<CommonKeyValuePair<string, string>> messagingRequest,
        CancellationToken cancellationToken)
    {
        NwsOffice office = new NwsOffice(id, messagingRequest);

        NwsOfficeResponse? response = await office.ReadAsync(cancellationToken);
        office.MapResponseToOffice(response);

        return office;
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Creates an office from a grid coordinate response.
    /// </summary>
    /// <param name="response">Grid coordinate response containing office information.</param>
    /// <returns>Office with basic information, or null if required data is missing.</returns>
    internal static NwsOffice? MapResponseToOffice(NwsGridCoordinateResponse? response)
    {
        if (response == null)
        {
            return null;
        }

        if (response.Properties == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(response.Properties.ForecastOfficeId))
        {
            return null;
        }

        NwsOffice office = new NwsOffice(response.Properties.ForecastOfficeId)
        {
            TimeZone = response.Properties.TimeZone,
        };

        return office;
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Extracts business keys from a list of API URLs.
    /// </summary>
    /// <param name="apiList">List of URLs from the API response.</param>
    /// <returns>List of business keys extracted from the URLs.</returns>
    private List<CommonBusinessKey> FillList(string[] apiList)
    {
        List<CommonBusinessKey> result = new List<CommonBusinessKey>();

        if (apiList is not null)
        {
            foreach (string apiUrl in apiList)
            {
                if (Uri.TryCreate(apiUrl, UriKind.RelativeOrAbsolute, out Uri? encodedUrl) &&
                    encodedUrl.Segments.Length > 0)
                {
                    result.Add(CommonBusinessKey.FromString(encodedUrl.Segments[^1], true));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Maps office response data to this office entity.
    /// </summary>
    /// <param name="response">Office response to convert.</param>
    private void MapResponseToOffice(NwsOfficeResponse? response)
    {
        if (response == null)
        {
            this.LogDebug("Unable to map office response for ID {OfficeId}: response is null", this.Id ?? "unknown");
            return;
        }

        if (response.Address == null)
        {
            this.LogDebug("Unable to map office response for ID {OfficeId}: address is null", this.Id ?? "unknown");
            return;
        }

        this.Name = response.Name;
        this.Telephone = response.Telephone;
        this.FaxNumber = response.FaxNumber;
        this.Email = response.Email;
        this.NwsRegion = response.NwsRegion;

        // Extract parent organization identifier from URL
        if (Uri.TryCreate(response.ParentOrganization, UriKind.RelativeOrAbsolute, out Uri? parentOrgUrl) &&
            parentOrgUrl.Segments.Length > 0)
        {
            this.ParentOrganization = parentOrgUrl.Segments[^1];
        }

        // Map address fields
        this.StreetAddress = response.Address.StreetAddress;
        this.AddressLocality = response.Address.AddressLocality;
        this.AddressRegion = response.Address.AddressRegion;
        this.PostalCode = response.Address.PostalCode;

        // Extract business keys from URL arrays
        this.ResponsibleCountyBusinessKeys = this.FillList(response.ResponsibleCountyUrls!);
        this.ResponsibleForecastZoneBusinessKeys = this.FillList(response.ResponsibleForecastZoneUrls!);
        this.ResponsibleFireZoneBusinessKeys = this.FillList(response.ResponsibleFireZoneUrls!);
        this.ApprovedObservationStationBusinessKeys = this.FillList(response.ApprovedObservationStationUrls!);
    }

    /// <summary>
    /// Retrieves the office data from the API.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Office response from the API.</returns>
    /// <exception cref="ArgumentException">Thrown when Id is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Repository or MessagingRequest is not initialized.</exception>
    private async Task<NwsOfficeResponse?> ReadAsync(
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