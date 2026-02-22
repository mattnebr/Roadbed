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
/// Repository for retrieving National Weather Service office information.
/// </summary>
internal sealed class NwsOfficeRepository
    : BaseNwsRepository, INwsOfficeRepository
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsOfficeRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    public NwsOfficeRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(request, ServiceLocator.GetService<INetHttpClient>())
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc />
    public async Task<NwsOfficeResponse> ReadAsync(
        string id,
        CancellationToken cancellationToken)
    {
        // URL syntax for API Endpoint:
        // https://api.weather.gov/offices/{office_id}
        string endpoint = string.Join(
            "/",
            BaseApiPath,
            "offices",
            id);

        this.LogDebug(
            "Fetching office information for: {OfficeId}",
            id);

        // Create Request
        NetHttpRequest apiRequest = this.CreateHttpGetRequest(endpoint);

        // Make HTTP request
        NetHttpResponse<string> response =
            await this.HttpClient.MakeHttpRequestAsync<string>(apiRequest, cancellationToken);

        // Handle failure
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = $"Failed to retrieve office information from {endpoint}: " +
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
        NwsOfficeResponse? result =
            JsonConvert.DeserializeObject<NwsOfficeResponse>(response.Data);

        if (result == null)
        {
            this.LogDebug(
                "Failed to deserialize office response from endpoint {Endpoint}",
                endpoint);

            throw new InvalidOperationException(
                $"Failed to deserialize office response from {endpoint}");
        }

        this.LogDebug(
            "Successfully retrieved office: {OfficeName} ({OfficeId})",
            result.Name ?? "unknown",
            id);

        return result;
    }

    #endregion Public Methods
}