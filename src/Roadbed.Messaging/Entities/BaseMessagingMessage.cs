/*
 * The namespace Roadbed.Messaging.Entities was removed on purpose and replaced with Roadbed.Messaging so that no additional using statements are required.
 */

namespace Roadbed.Messaging;

using Newtonsoft.Json;

/// <summary>
/// Base Message.
/// </summary>
/// <typeparam name="T">Type of data in the message payload.</typeparam>
public abstract class BaseMessagingMessage<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class with default
    /// values suitable for deserialization.
    /// </summary>
    /// <remarks>
    /// Required so that <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject{T}(string)"/> can
    /// instantiate concrete subclasses without ambiguity over which parameterized constructor to use.
    /// <see cref="Publisher"/> is initialized to <see langword="null"/> via the null-forgiving operator;
    /// the deserializer is expected to populate it from the JSON immediately after construction.
    /// Code paths that do not deserialize must use one of the parameterized constructors instead.
    /// </remarks>
    protected BaseMessagingMessage()
    {
        this.Publisher = null!;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    protected BaseMessagingMessage(MessagingPublisher publisher)
    {
        this.Publisher = publisher;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the message publisher.</param>
    /// <param name="data">Message payload.</param>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data)
        : this(publisher)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = identifier;
        this.Data = data;
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the message publisher.</param>
    /// <param name="data">Message payload.</param>
    /// <param name="createdOn">Message created on according to the source.</param>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data, DateTimeOffset createdOn)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = identifier;
        this.Data = data;
        this.SourceCreatedOn = createdOn;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the time the message was created.
    /// </summary>
    [JsonProperty("message_create_on")]
    public DateTimeOffset? CreatedOn
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the object in the message.
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public T? Data
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the identifier for the message.
    /// </summary>
    [JsonProperty("message_identifier")]
    public string? Identifier
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    [JsonProperty("message_type", NullValueHandling = NullValueHandling.Ignore)]
    public string? MessageTypeCodename
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the publisher for the message.
    /// </summary>
    [JsonProperty("publisher", NullValueHandling = NullValueHandling.Ignore)]
    public MessagingPublisher Publisher
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the time the message was created according to the source.
    /// </summary>
    [JsonProperty("source_create_on", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? SourceCreatedOn
    {
        get;
        set;
    }

    #endregion Public Properties
}