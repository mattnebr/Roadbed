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
/// Repository for retrieving hourly weather forecasts from the National Weather Service.
/// </summary>
internal sealed class NwsForecastHourlyRepository
    : BaseNwsRepository, INwsForecastHourlyRepository
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastHourlyRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    public NwsForecastHourlyRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(request, ServiceLocator.GetService<INetHttpClient>())
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc />
    /// <remarks>
    /// Returns forecast for hourly periods over the next seven days.
    /// For 12-hour period forecasts, use <see cref="INwsForecastDailyRepository"/>.
    /// </remarks>
    public async Task<NwsForecastResponse> ReadAsync(
        NwsForecastRequest request,
        CancellationToken cancellationToken)
    {
        // URL syntax for API Endpoint:
        // https://api.weather.gov/gridpoints/{wfo}/{x},{y}/forecast/hourly
        string endpoint = string.Join(
            "/",
            BaseApiPath,
            "gridpoints",
            request.OfficeId,
            string.Concat(request.GridCoordinateX, ',', request.GridCoordinateY),
            "forecast",
            "hourly");

        this.LogDebug(
            "Fetching hourly forecast from endpoint: {Endpoint}",
            endpoint);

        // Create Request
        NetHttpRequest apiRequest = this.CreateHttpGetRequest(endpoint);

        // Make HTTP request
        NetHttpResponse<string> response =
            await this.HttpClient.MakeHttpRequestAsync<string>(apiRequest, cancellationToken);

        // Handle failure
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = $"Failed to retrieve hourly forecast from {endpoint}: " +
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
        NwsForecastResponse? result =
            JsonConvert.DeserializeObject<NwsForecastResponse>(response.Data);

        if (result == null)
        {
            this.LogDebug(
                "Failed to deserialize hourly forecast response from endpoint {Endpoint}",
                endpoint);

            throw new InvalidOperationException(
                $"Failed to deserialize forecast response from {endpoint}");
        }

        this.LogDebug(
            "Successfully retrieved hourly forecast with {PeriodCount} periods",
            result.Properties?.Periods?.Length ?? 0);

        return result;
    }

    #endregion Public Methods
}