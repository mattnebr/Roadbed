namespace Roadbed.Sdk.NationalWeatherService.Repositories;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Net;
using Roadbed.Sdk.NationalWeatherService.Dtos;

/// <summary>
/// Repository for retrieving weather station information.
/// </summary>
internal sealed class NwsStationRepository
    : BaseNwsRepository, INwsStationRepository
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsStationRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    public NwsStationRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(request, ServiceLocator.GetService<INetHttpClient>())
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc />
    public async Task<NwsStationResponse> ReadAsync(
        string id,
        CancellationToken cancellationToken)
    {
        // URL syntax for API Endpoint:
        // https://api.weather.gov/stations/{station_id}
        string endpoint = string.Join(
            "/",
            BaseApiPath,
            "stations",
            id);

        this.LogDebug(
            "Fetching station information for: {StationId}",
            id);

        // Create Request
        NetHttpRequest apiRequest = this.CreateHttpGetRequest(endpoint);

        // Make HTTP request
        NetHttpResponse<string> response =
            await this.HttpClient.MakeHttpRequestAsync<string>(apiRequest, cancellationToken);

        // Handle failure
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = $"Failed to retrieve station information from {endpoint}: " +
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
        NwsStationResponse? result =
            JsonConvert.DeserializeObject<NwsStationResponse>(response.Data);

        if (result == null)
        {
            this.LogDebug(
                "Failed to deserialize station response from endpoint {Endpoint}",
                endpoint);

            throw new InvalidOperationException(
                $"Failed to deserialize station response from {endpoint}");
        }

        this.LogDebug(
            "Successfully retrieved station: {StationName} ({StationId})",
            result.Properties?.Name ?? "unknown",
            id);

        return result;
    }

    #endregion Public Methods
}