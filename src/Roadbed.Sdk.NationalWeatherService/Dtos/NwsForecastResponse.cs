namespace Roadbed.Sdk.NationalWeatherService.Dtos;

using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Forecast response from the National Weather Service.
/// </summary>
/// <remarks>
/// Contains forecast periods with weather conditions, temperatures, and timing information.
/// This is the response from both daily and hourly forecast API endpoints.
/// </remarks>
internal sealed record NwsForecastResponse
    : BaseEntityRecord<string>
{
    /// <summary>
    /// Gets or sets the GeoJSON type (should be "Feature").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the forecast properties containing periods and metadata.
    /// </summary>
    [JsonProperty("properties")]
    public NwsForecastPropertyResponse? Properties { get; set; }
}