namespace Roadbed.Sdk.NationalWeatherService.Repositories;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Net;
using Roadbed.Sdk.NationalWeatherService.Dtos;

/// <summary>
/// Repository for converting geographic coordinates to NWS grid coordinates.
/// </summary>
internal sealed class NwsGridCoordinateRepository
    : BaseNwsRepository, INwsGridCoordinateRepository
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsGridCoordinateRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    public NwsGridCoordinateRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(request, ServiceLocator.GetService<INetHttpClient>())
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc />
    /// <remarks>
    /// The National Weather Service uses a grid-based system for weather data.
    /// This method converts standard latitude/longitude coordinates into the
    /// NWS grid system (office identifier, gridX, gridY) required for forecast requests.
    /// </remarks>
    public async Task<NwsGridCoordinateResponse> ReadAsync(
        NwsPhysicalAddress coordinates,
        CancellationToken cancellationToken)
    {
        // URL syntax for API Endpoint:
        // https://api.weather.gov/points/{latitude},{longitude}
        string endpoint = string.Join(
            "/",
            BaseApiPath,
            "points",
            string.Concat(coordinates.Latitude, ',', coordinates.Longitude));

        this.LogDebug(
            "Converting coordinates to grid: Lat={Latitude}, Lon={Longitude}",
            coordinates.Latitude,
            coordinates.Longitude);

        // Create Request
        NetHttpRequest apiRequest = this.CreateHttpGetRequest(endpoint);

        // Make HTTP request
        NetHttpResponse<string> response =
            await this.HttpClient.MakeHttpRequestAsync<string>(apiRequest, cancellationToken);

        // Handle failure
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = $"Failed to retrieve grid coordinates from {endpoint}: " +
                $"{response.HttpStatusCode} - {response.HttpStatusCodeDescription}";

            if (string.IsNullOrEmpty(response.HttpStatusCodeDescription))
            {
                this.LogDebug(
                    "API request failed for endpoint {Endpoint}. Status: {StatusCode}",
                    endpoint,
                    response.HttpStatusCode);
            }
            else
            {
                this.LogDebug(
                    "API request failed for endpoint {Endpoint}. Status: {StatusCode} - {StatusDescription}",
                    endpoint,
                    response.HttpStatusCode,
                    response.HttpStatusCodeDescription);
            }

            throw new HttpRequestException(errorMessage);
        }

        // Deserialize JSON
        NwsGridCoordinateResponse? result =
            JsonConvert.DeserializeObject<NwsGridCoordinateResponse>(response.Data);

        if (result == null)
        {
            this.LogDebug(
                "Failed to deserialize grid coordinate response from endpoint {Endpoint}",
                endpoint);

            throw new InvalidOperationException(
                $"Failed to deserialize grid coordinate response from {endpoint}");
        }

        this.LogDebug(
            "Successfully converted to grid: Office={Office}, GridX={GridX}, GridY={GridY}",
            result.Properties?.ForecastOfficeId ?? "unknown",
            result.Properties?.GridCoordinateX ?? 0,
            result.Properties?.GridCoordinateY ?? 0);

        return result;
    }

    #endregion Public Methods
}