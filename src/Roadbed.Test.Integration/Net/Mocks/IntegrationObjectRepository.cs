namespace Roadbed.Test.Integration.Net.Mocks;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Net;

/// <summary>
/// CRUD repository for IntegrationObject entity.
/// </summary>
/// <remarks>
/// The 'live' mock API has a daily limit of requests.
/// The current limit is equal to 100 requests per day.
/// </remarks>
internal class IntegrationObjectRepository
    : BaseAsyncCrudlRepository<IntegrationObjectRow, string>
{
    #region Private Fields

    /// <summary>
    /// Base API path for the RESTful API.
    /// </summary>
    private const string BaseApiPath = "https://api.restful-api.dev/objects";

    /// <summary>
    /// Settings to instruct Newtonsoft.Json serializer to ignore null values.
    /// </summary>
    private static readonly JsonSerializerSettings newtonsoftSerializationSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    #endregion Private Fields

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObjectRepository"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal IntegrationObjectRepository(ILogger<IntegrationObjectRepository> logger)
        : base(logger)
    {
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc />
    public override async Task<IntegrationObjectRow> CreateAsync(
        IntegrationObjectRow entity,
        CancellationToken cancellationToken = default)
    {
        string jsonPayload = JsonConvert.SerializeObject(
            entity,
            newtonsoftSerializationSettings);

        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Post,
            HttpEndPoint = new Uri(BaseApiPath),
            Content = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json"),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            IntegrationObjectRow? result =
                JsonConvert.DeserializeObject<IntegrationObjectRow>(response.Data);

            return result!;
        }

        return default!;
    }

    /// <inheritdoc />
    public override async Task<IntegrationObjectRow?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, id)),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            IntegrationObjectRow? result =
                JsonConvert.DeserializeObject<IntegrationObjectRow>(response.Data);

            return result;
        }

        return default;
    }

    /// <inheritdoc />
    public override async Task<IntegrationObjectRow> UpdateAsync(
        IntegrationObjectRow entity,
        CancellationToken cancellationToken = default)
    {
        string jsonPayload = JsonConvert.SerializeObject(
            entity,
            newtonsoftSerializationSettings);

        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Put,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, entity.Id)),
            Content = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json"),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            IntegrationObjectRow? result =
                JsonConvert.DeserializeObject<IntegrationObjectRow>(response.Data);

            return result!;
        }

        return default!;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Delete,
            HttpEndPoint = new Uri(string.Join("/", BaseApiPath, id)),
        };

        // Make HTTP request
        await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IList<IntegrationObjectRow>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        NetHttpRequest request = new NetHttpRequest
        {
            Method = HttpMethod.Get,
            HttpEndPoint = new Uri(BaseApiPath),
        };

        // Make HTTP request
        NetHttpResponse<string> response =
            await NetHttpClient.MakeRequestAsync<string>(request, cancellationToken);

        // Verify Response
        if (response.IsSuccessStatusCode)
        {
            IList<IntegrationObjectRow>? result =
                JsonConvert.DeserializeObject<List<IntegrationObjectRow>>(response.Data);

            return result!;
        }

        return default!;
    }

    #endregion Public Methods
}