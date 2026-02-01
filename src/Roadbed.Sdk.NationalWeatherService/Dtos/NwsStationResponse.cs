namespace Roadbed.Sdk.NationalWeatherService.Dtos;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Observation Station Detail Response from the National Weather Service.
/// </summary>
/// <remarks>
/// Represents detailed information about a single observation station from the
/// National Weather Service API. This is a GeoJSON Feature.
/// </remarks>
internal sealed record NwsStationResponse
    : BaseEntityRecord<string>
{
    /// <summary>
    /// Gets or sets the JSON-LD context.
    /// </summary>
    [JsonProperty("@context")]
    public object? Context { get; set; }

    /// <summary>
    /// Gets or sets the GeoJSON type (should be "Feature").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the geometry of the station location.
    /// </summary>
    [JsonProperty("geometry")]
    public NwsStationGeometryResponse? Geometry { get; set; }

    /// <summary>
    /// Gets or sets the station properties.
    /// </summary>
    [JsonProperty("properties")]
    public NwsStationPropertiesResponse? Properties { get; set; }
}