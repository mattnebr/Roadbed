/*
 * The namespace Roadbed.Messaging.Entities was removed on purpose and replaced with Roadbed.Messaging so that no additional using statements are required.
 */

namespace Roadbed.Messaging;

/// <summary>
/// Entity for Message Notification related operations.
/// </summary>
/// <typeparam name="T">Type of data in the notification.</typeparam>
public class MessagingMessageRequest<T>
    : BaseMessagingMessage<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageRequest{T}"/> class with
    /// default values suitable for deserialization.
    /// </summary>
    /// <remarks>
    /// Required so that <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject{T}(string)"/> can
    /// instantiate this type without ambiguity over which parameterized constructor to use.
    /// Application code that publishes new messages should use one of the parameterized constructors
    /// instead.
    /// </remarks>
    public MessagingMessageRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageRequest{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    public MessagingMessageRequest(MessagingPublisher publisher)
        : base(publisher)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageRequest{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    public MessagingMessageRequest(MessagingPublisher publisher, string typeCodename)
        : base(publisher, typeCodename)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingMessageRequest{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the event.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the original notification request.</param>
    /// <param name="data">Notification payload.</param>
    public MessagingMessageRequest(MessagingPublisher publisher, string typeCodename, string identifier, T data)
        : base(publisher, typeCodename, identifier, data)
    {
    }

    #endregion Public Constructors
}