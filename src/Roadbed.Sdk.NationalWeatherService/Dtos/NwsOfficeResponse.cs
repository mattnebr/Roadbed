namespace Roadbed.Sdk.NationalWeatherService.Dtos;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Weather Station Response from the National Weather Service.
/// </summary>
/// <remarks>
/// Represents a National Weather Service office with its contact information,
/// organizational structure, and areas of responsibility.
/// </remarks>
internal sealed record NwsOfficeResponse
    : BaseEntityRecord<string>
{
    /// <summary>
    /// Gets or sets the JSON-LD context.
    /// </summary>
    [JsonProperty("@context")]
    public object? Context { get; set; }

    /// <summary>
    /// Gets or sets the organization type (should be "GovernmentOrganization").
    /// </summary>
    [JsonProperty("@type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the office name.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the postal address.
    /// </summary>
    [JsonProperty("address")]
    public NwsMailingAddressResponse? Address { get; set; }

    /// <summary>
    /// Gets or sets the telephone number.
    /// </summary>
    [JsonProperty("telephone")]
    public string? Telephone { get; set; }

    /// <summary>
    /// Gets or sets the fax number.
    /// </summary>
    [JsonProperty("faxNumber")]
    public string? FaxNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the office website URL.
    /// </summary>
    [JsonProperty("sameAs")]
    public string? SameAs { get; set; }

    /// <summary>
    /// Gets or sets the NWS region code (e.g., "wr" for Western Region).
    /// </summary>
    [JsonProperty("nwsRegion")]
    public string? NwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the parent organization API URL.
    /// </summary>
    [JsonProperty("parentOrganization")]
    public string? ParentOrganization { get; set; }

    /// <summary>
    /// Gets or sets the list of responsible county zone URLs.
    /// </summary>
    [JsonProperty("responsibleCounties")]
    public string[]? ResponsibleCountyUrls { get; set; }

    /// <summary>
    /// Gets or sets the list of responsible forecast zone URLs.
    /// </summary>
    [JsonProperty("responsibleForecastZones")]
    public string[]? ResponsibleForecastZoneUrls { get; set; }

    /// <summary>
    /// Gets or sets the list of responsible fire zone URLs.
    /// </summary>
    [JsonProperty("responsibleFireZones")]
    public string[]? ResponsibleFireZoneUrls { get; set; }

    /// <summary>
    /// Gets or sets the list of approved observation station URLs.
    /// </summary>
    [JsonProperty("approvedObservationStations")]
    public string[]? ApprovedObservationStationUrls { get; set; }
}