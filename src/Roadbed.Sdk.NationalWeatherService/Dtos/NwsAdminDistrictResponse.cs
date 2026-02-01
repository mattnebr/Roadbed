namespace Roadbed.Sdk.NationalWeatherService.Dtos;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Admin District Response from the National Weather Service.
/// </summary>
/// <remarks>
/// Represents the response from the National Weather Service API for observation stations
/// within a state or administrative district. This is a paginated GeoJSON FeatureCollection.
/// </remarks>
internal sealed record NwsAdminDistrictResponse
    : BaseEntityRecord<string>
{
    /// <summary>
    /// Gets or sets the JSON-LD context.
    /// </summary>
    [JsonProperty("@context")]
    public object? Context { get; set; }

    /// <summary>
    /// Gets or sets the GeoJSON type (should be "FeatureCollection").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the collection of observation station features.
    /// </summary>
    [JsonProperty("features")]
    public NwsStationFeaturesResponse[]? Features { get; set; }

    /// <summary>
    /// Gets or sets the list of observation station URLs.
    /// </summary>
    [JsonProperty("observationStations")]
    public string[]? ObservationStationsResponse { get; set; }

    /// <summary>
    /// Gets or sets the pagination information.
    /// </summary>
    [JsonProperty("pagination")]
    public NwsPaginationResponse? PaginationResponse { get; set; }
}