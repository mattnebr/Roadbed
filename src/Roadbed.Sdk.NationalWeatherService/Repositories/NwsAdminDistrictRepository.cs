namespace Roadbed.Sdk.NationalWeatherService.Repositories;

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Net;
using Roadbed.Sdk.NationalWeatherService.Dtos;

/// <summary>
/// Repository for retrieving observation stations by administrative district.
/// </summary>
internal sealed class NwsAdminDistrictRepository
    : BaseNwsRepository, INwsAdminDistrictRepository
{
    #region Private Fields

    /// <summary>
    /// Delay between API page requests in milliseconds to avoid overwhelming the NWS API.
    /// </summary>
    private const int DelayBetweenPagesMilliseconds = 1000;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsAdminDistrictRepository"/> class.
    /// </summary>
    /// <param name="request">Messaging request for messages sent to API.</param>
    public NwsAdminDistrictRepository(
        MessagingMessageRequest<CommonKeyValuePair<string, string>> request)
        : base(request, ServiceLocator.GetService<INetHttpClient>())
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc />
    public async Task<IList<NwsStation>> ListAsync(
        AdminDistrictTwoCharacterCode district,
        CancellationToken cancellationToken)
    {
        // URL syntax for API Endpoint:
        // https://api.weather.gov/stations?state={state}
        string nextPageEndpoint = string.Concat(
            BaseApiPath,
            "/stations?state=",
            district.ToString());

        IList<NwsStation> stations = new List<NwsStation>();
        StringBuilder traceMessage = new StringBuilder();
        int pageCount = 1;

        this.LogDebug("Starting to fetch stations for district {District}", district);

        // Get Pages
        while (!string.IsNullOrEmpty(nextPageEndpoint))
        {
            // Append to Trace
            traceMessage.Append(pageCount).Append(" - ");

            nextPageEndpoint = await this.GetPageAsync(
                stations,
                nextPageEndpoint,
                traceMessage,
                cancellationToken);

            if (!string.IsNullOrEmpty(nextPageEndpoint))
            {
                // Wait before next call with incremental backoff.
                // NWS API is brittle and rate-limited.
                await Task.Delay(DelayBetweenPagesMilliseconds * pageCount, cancellationToken);
                pageCount++;
            }
        }

        this.LogDebug(
            "Successfully retrieved {StationCount} stations for district {District}. Pagination trace: {TraceMessage}",
            stations.Count,
            district,
            traceMessage.ToString());

        return stations;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Gets a single page of results from the NWS API.
    /// </summary>
    /// <param name="stations">Collection to append results to.</param>
    /// <param name="pageEndpoint">URL of the page to retrieve.</param>
    /// <param name="traceMessage">StringBuilder for tracking page requests.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>URL to the next page, or null if no more pages.</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails.</exception>
    private async Task<string> GetPageAsync(
        IList<NwsStation> stations,
        string pageEndpoint,
        StringBuilder traceMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(pageEndpoint))
        {
            return string.Empty;
        }

        // Create Request
        NetHttpRequest apiRequest = this.CreateHttpGetRequest(pageEndpoint);

        // Make HTTP request
        NetHttpResponse<string> response =
            await this.HttpClient.MakeHttpRequestAsync<string>(apiRequest, cancellationToken);

        // Handle failure
        if (!response.IsSuccessStatusCode)
        {
            traceMessage.Append("FAILED - ").AppendLine(pageEndpoint);
            traceMessage.Append(response.HttpStatusCode)
                .Append(" - ")
                .AppendLine(response.HttpStatusCodeDescription);

            string errorMessage = $"Failed to retrieve stations from {pageEndpoint}: " +
                $"{response.HttpStatusCode} - {response.HttpStatusCodeDescription}";

            if (string.IsNullOrEmpty(response.HttpStatusCodeDescription))
            {
                this.LogDebug(
                    "API request failed for endpoint {Endpoint}. Status: {StatusCode}",
                    pageEndpoint,
                    response.HttpStatusCode);
            }
            else
            {
                this.LogDebug(
                    "API request failed for endpoint {Endpoint}. Status: {StatusCode} - {StatusDescription}",
                    pageEndpoint,
                    response.HttpStatusCode,
                    response.HttpStatusCodeDescription);
            }

            // Clear partial results - we want all-or-nothing
            stations.Clear();

            throw new HttpRequestException(errorMessage);
        }

        // Success - log it
        traceMessage.Append("PASSED - ").AppendLine(pageEndpoint);

        // Deserialize JSON
        NwsAdminDistrictResponse? result =
            JsonConvert.DeserializeObject<NwsAdminDistrictResponse>(response.Data);

        if (result == null)
        {
            this.LogDebug("Received null response from API endpoint {Endpoint}", pageEndpoint);
            return string.Empty;
        }

        if (result.Features == null)
        {
            this.LogDebug("Received response with null Features from API endpoint {Endpoint}", pageEndpoint);
            return string.Empty;
        }

        if (result.Features.Length == 0)
        {
            this.LogDebug("Received empty Features array from API endpoint {Endpoint}", pageEndpoint);
            return string.Empty;
        }

        // Process each station feature
        foreach (NwsStationFeaturesResponse feature in result.Features)
        {
            NwsStation? station = NwsAdminDistrict.MapFeatureToStation(feature);

            if (station != null)
            {
                stations.Add(station);
            }
            else
            {
                this.LogDebug("Failed to map station feature to entity");
            }
        }

        // Check for next page
        if ((result.Features.Length > 0) &&
            (result.PaginationResponse != null) &&
            (!string.IsNullOrEmpty(result.PaginationResponse.Next)))
        {
            // Remember the URL to the "next" page of records
            this.LogDebug("Found next page: {NextPage}", result.PaginationResponse.Next);
            return result.PaginationResponse.Next;
        }

        // No more pages
        this.LogDebug("No more pages available");
        return string.Empty;
    }

    #endregion Private Methods
}