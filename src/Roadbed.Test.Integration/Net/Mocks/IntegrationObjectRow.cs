namespace Roadbed.Test.Integration.Net.Mocks;

using Newtonsoft.Json;
using Roadbed.Common;
using Roadbed.Common.Converters;
using Roadbed.Crud;

/// <summary>
/// Mock Data Transfer Object in the RESTful API.
/// </summary>
public record IntegrationObjectRow
    : BaseEntityRecord<string>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObjectRow"/> class.
    /// </summary>
    public IntegrationObjectRow()
    {
        this.Attributes = new List<CommonKeyValuePair<string, string>>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObjectRow"/> class.
    /// </summary>
    /// <param name="name">Name of the row.</param>
    /// <param name="attributes">Name/pair value of data attributes.</param>
    public IntegrationObjectRow(string name, IList<CommonKeyValuePair<string, string>> attributes)
    {
        this.Name = name;
        this.Attributes = attributes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObjectRow"/> class.
    /// </summary>
    /// <param name="id">ID for the row.</param>
    /// <param name="name">Name of the row.</param>
    /// <param name="attributes">Name/pair value of data attributes.</param>
    public IntegrationObjectRow(string id, string name, IList<CommonKeyValuePair<string, string>> attributes)
    {
        this.Id = id;
        this.Name = name;
        this.Attributes = attributes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObjectRow"/> class.
    /// </summary>
    /// <param name="id">ID for the row.</param>
    /// <param name="name">Name of the row.</param>
    /// <param name="attributes">Name/pair value of data attributes.</param>
    /// <param name="createdAt">Create At date/time stamp for the row.</param>
    public IntegrationObjectRow(string id, string name, IList<CommonKeyValuePair<string, string>> attributes, string createdAt)
    {
        this.Id = id;
        this.Name = name;
        this.Attributes = attributes;
        this.CreatedAt = createdAt;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the attributes of the object.
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(CommonKeyValuePairListConverter<string, string>))]
    public IList<CommonKeyValuePair<string, string>> Attributes
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the created at for the object.
    /// </summary>
    [JsonProperty("createdAt")]
    public string? CreatedAt
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the name of the object.
    /// </summary>
    [JsonProperty("name")]
    public string? Name
    {
        get;
        internal set;
    }

    #endregion Public Methods
}