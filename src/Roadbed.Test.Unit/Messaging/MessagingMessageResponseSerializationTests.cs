namespace Roadbed.Test.Unit.Messaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roadbed.Common;
using Roadbed.Messaging;

/// <summary>
/// Verifies that <see cref="MessagingMessageResponse{T}"/> serializes to the documented
/// snake_case wire format.
/// </summary>
/// <remarks>
/// These tests pin the JSON property names that downstream consumers depend on. Renaming
/// a property in C# without updating its <c>[JsonProperty]</c> attribute will break the
/// wire format silently — these tests fail loudly when that happens.
/// </remarks>
[TestClass]
public class MessagingMessageResponseSerializationTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that OriginalRequestIdentifier serializes to "original_request_identifier".
    /// </summary>
    [TestMethod]
    public void Serialize_WithOriginalRequestIdentifier_UsesSnakeCaseJsonName()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var response = new MessagingMessageResponse<string>(publisher, "test.codename")
        {
            OriginalRequestIdentifier = "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
        };

        // Act (When)
        string json = JsonConvert.SerializeObject(response);
        var jObject = JObject.Parse(json);

        // Assert (Then)
        Assert.IsTrue(
            jObject.ContainsKey("original_request_identifier"),
            "JSON should contain 'original_request_identifier' (snake_case).");
        Assert.IsFalse(
            jObject.ContainsKey("OriginalRequestIdentifier"),
            "JSON should NOT contain 'OriginalRequestIdentifier' (PascalCase).");
        Assert.AreEqual(
            "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
            jObject["original_request_identifier"]?.Value<string>(),
            "JSON value should match the OriginalRequestIdentifier set on the response.");
    }

    /// <summary>
    /// Verifies that null OriginalRequestIdentifier is omitted from the serialized JSON.
    /// </summary>
    [TestMethod]
    public void Serialize_WithNullOriginalRequestIdentifier_OmitsProperty()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var response = new MessagingMessageResponse<string>(publisher, "test.codename")
        {
            OriginalRequestIdentifier = null,
        };

        // Act (When)
        string json = JsonConvert.SerializeObject(response);
        var jObject = JObject.Parse(json);

        // Assert (Then)
        Assert.IsFalse(
            jObject.ContainsKey("original_request_identifier"),
            "JSON should omit 'original_request_identifier' when the value is null (NullValueHandling.Ignore).");
    }

    /// <summary>
    /// Verifies that the inherited envelope properties continue to use their documented snake_case names.
    /// Locks the wire-format property names so that future renames are caught.
    /// </summary>
    [TestMethod]
    public void Serialize_EnvelopeProperties_UseDocumentedSnakeCaseNames()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var response = new MessagingMessageResponse<string>(
            publisher,
            "test.codename",
            "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
            "payload");

        // Act (When)
        string json = JsonConvert.SerializeObject(response);
        var jObject = JObject.Parse(json);

        // Assert (Then)
        Assert.IsTrue(
            jObject.ContainsKey("message_identifier"),
            "JSON should contain 'message_identifier'.");
        Assert.IsTrue(
            jObject.ContainsKey("message_type"),
            "JSON should contain 'message_type'.");
        Assert.IsTrue(
            jObject.ContainsKey("publisher"),
            "JSON should contain 'publisher'.");
        Assert.IsTrue(
            jObject.ContainsKey("data"),
            "JSON should contain 'data'.");
        Assert.IsTrue(
            jObject.ContainsKey("message_create_on"),
            "JSON should contain 'message_create_on'.");
        Assert.IsTrue(
            jObject.ContainsKey("source_create_on"),
            "JSON should contain 'source_create_on'.");
    }

    /// <summary>
    /// Verifies that the publisher block serializes to its documented snake_case names.
    /// </summary>
    [TestMethod]
    public void Serialize_PublisherProperties_UseDocumentedSnakeCaseNames()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var response = new MessagingMessageResponse<string>(publisher, "test.codename");

        // Act (When)
        string json = JsonConvert.SerializeObject(response);
        var jObject = JObject.Parse(json);
        var publisherJson = jObject["publisher"] as JObject;

        // Assert (Then)
        Assert.IsNotNull(
            publisherJson,
            "Serialized JSON should contain a 'publisher' object.");
        Assert.IsTrue(
            publisherJson!.ContainsKey("publisher_identifier"),
            "Publisher should contain 'publisher_identifier'.");
        Assert.IsTrue(
            publisherJson.ContainsKey("publisher_name"),
            "Publisher should contain 'publisher_name'.");
    }

    /// <summary>
    /// Verifies round-trip: serialize a response with OriginalRequestIdentifier, deserialize, and
    /// confirm the value survives.
    /// </summary>
    [TestMethod]
    public void Serialize_RoundTrip_PreservesOriginalRequestIdentifier()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var original = new MessagingMessageResponse<string>(
            publisher,
            "test.codename",
            Ulid.NewUlid().ToString(),
            "payload")
        {
            OriginalRequestIdentifier = "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
        };

        // Act (When)
        string json = JsonConvert.SerializeObject(original);
        var roundTripped = JsonConvert.DeserializeObject<MessagingMessageResponse<string>>(json);

        // Assert (Then)
        Assert.IsNotNull(
            roundTripped,
            "Deserialized response should not be null.");
        Assert.AreEqual(
            "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
            roundTripped!.OriginalRequestIdentifier,
            "OriginalRequestIdentifier should round-trip through serialization.");
    }

    /// <summary>
    /// Verifies round-trip: serialize a response, deserialize, and confirm the core envelope
    /// properties survive.
    /// </summary>
    [TestMethod]
    public void Serialize_RoundTrip_PreservesEnvelopeProperties()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        string identifier = Ulid.NewUlid().ToString();
        var original = new MessagingMessageResponse<string>(
            publisher,
            "test.codename",
            identifier,
            "payload");

        // Act (When)
        string json = JsonConvert.SerializeObject(original);
        var roundTripped = JsonConvert.DeserializeObject<MessagingMessageResponse<string>>(json);

        // Assert (Then)
        Assert.IsNotNull(
            roundTripped,
            "Deserialized response should not be null.");
        Assert.AreEqual(
            identifier,
            roundTripped!.Identifier,
            "Identifier should round-trip through serialization.");
        Assert.AreEqual(
            "test.codename",
            roundTripped.MessageTypeCodename,
            "MessageTypeCodename should round-trip through serialization.");
        Assert.AreEqual(
            "payload",
            roundTripped.Data,
            "Data should round-trip through serialization.");
        Assert.IsNotNull(
            roundTripped.Publisher,
            "Publisher should be populated after deserialization.");
    }

    #endregion Public Methods
}
