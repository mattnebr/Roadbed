namespace Roadbed.Sdk.NationalWeatherService.Dtos;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Grid coordinate response from the National Weather Service.
/// </summary>
/// <remarks>
/// Contains grid coordinates and forecast endpoint URLs for a given latitude/longitude location.
/// This is the response from the /points/{latitude},{longitude} API endpoint.
/// </remarks>
internal sealed record NwsGridCoordinateResponse
    : BaseEntityRecord<string>
{
    /// <summary>
    /// Gets or sets the GeoJSON type (should be "Feature").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the location properties containing grid coordinates and forecast URLs.
    /// </summary>
    [JsonProperty("properties")]
    public NwsLocationPropertyResponse? Properties { get; set; }
}