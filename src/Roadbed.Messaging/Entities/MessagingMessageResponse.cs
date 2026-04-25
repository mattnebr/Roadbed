/*
 * The namespace Roadbed.Messaging.Entities was removed on purpose and replaced with Roadbed.Messaging so that no additional using statements are required.
 */

namespace Roadbed.Messaging;

using Newtonsoft.Json;

/// <summary>
/// Entity for Message Notification Response.
/// </summary>
/// <typeparam name="T">Type of data in the notification.</typeparam>
public class MessagingMessageResponse<T>
    : BaseMessagingMessage<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageResponse{T}"/> class with
    /// default values suitable for deserialization.
    /// </summary>
    /// <remarks>
    /// Required so that <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject{T}(string)"/> can
    /// instantiate this type without ambiguity over which parameterized constructor to use.
    /// Application code that publishes new messages should use one of the parameterized constructors
    /// instead.
    /// </remarks>
    public MessagingMessageResponse()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageResponse{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    public MessagingMessageResponse(MessagingPublisher publisher)
        : base(publisher)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageResponse{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename)
        : base(publisher, typeCodename)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageResponse{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the original notification request.</param>
    /// <param name="data">Notification payload.</param>
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename, string identifier, T data)
        : base(publisher, typeCodename, identifier, data)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageResponse{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the original notification request.</param>
    /// <param name="data">Notification payload.</param>
    /// <param name="createdOn">Message created on according to the source.</param>
    public MessagingMessageResponse(MessagingPublisher publisher, string typeCodename, string identifier, T data, DateTimeOffset createdOn)
        : base(publisher, typeCodename, identifier, data, createdOn)
    {
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the original identifier for the request message.
    /// </summary>
    [JsonProperty("original_request_identifier", NullValueHandling = NullValueHandling.Ignore)]
    public string? OriginalRequestIdentifier { get; set; }

    #endregion Public Properties
}