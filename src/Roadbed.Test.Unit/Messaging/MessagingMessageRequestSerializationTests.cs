namespace Roadbed.Test.Unit.Messaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roadbed.Common;
using Roadbed.Messaging;

/// <summary>
/// Verifies that <see cref="MessagingMessageRequest{T}"/> serializes and deserializes correctly.
/// </summary>
/// <remarks>
/// These tests cover both the wire-format property names and the round-trip path that
/// <c>JsonConvert.DeserializeObject&lt;MessagingMessageRequest&lt;T&gt;&gt;</c> consumers depend on.
/// </remarks>
[TestClass]
public class MessagingMessageRequestSerializationTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the envelope properties continue to use their documented snake_case names.
    /// </summary>
    [TestMethod]
    public void Serialize_EnvelopeProperties_UseDocumentedSnakeCaseNames()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var request = new MessagingMessageRequest<string>(
            publisher,
            "test.codename",
            "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
            "payload");

        // Act (When)
        string json = JsonConvert.SerializeObject(request);
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
    /// Verifies that a request envelope does NOT contain a correlation field — that is reserved
    /// for <see cref="MessagingMessageResponse{T}"/>.
    /// </summary>
    [TestMethod]
    public void Serialize_RequestEnvelope_DoesNotContainOriginalRequestIdentifier()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        var request = new MessagingMessageRequest<string>(publisher, "test.codename");

        // Act (When)
        string json = JsonConvert.SerializeObject(request);
        var jObject = JObject.Parse(json);

        // Assert (Then)
        Assert.IsFalse(
            jObject.ContainsKey("original_request_identifier"),
            "Request envelopes should not contain 'original_request_identifier'.");
        Assert.IsFalse(
            jObject.ContainsKey("OriginalRequestIdentifier"),
            "Request envelopes should not contain 'OriginalRequestIdentifier'.");
    }

    /// <summary>
    /// Verifies round-trip: serialize a request, deserialize, and confirm the core envelope
    /// properties survive.
    /// </summary>
    [TestMethod]
    public void Serialize_RoundTrip_PreservesEnvelopeProperties()
    {
        // Arrange (Given)
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TESTPUBLISHER"));
        string identifier = Ulid.NewUlid().ToString();
        var original = new MessagingMessageRequest<string>(
            publisher,
            "test.codename",
            identifier,
            "payload");

        // Act (When)
        string json = JsonConvert.SerializeObject(original);
        var roundTripped = JsonConvert.DeserializeObject<MessagingMessageRequest<string>>(json);

        // Assert (Then)
        Assert.IsNotNull(
            roundTripped,
            "Deserialized request should not be null.");
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

    /// <summary>
    /// Verifies that the parameterless constructor produces a deserializer-ready instance.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_CreatesInstance()
    {
        // Arrange (Given)

        // Act (When)
        var request = new MessagingMessageRequest<string>();

        // Assert (Then)
        Assert.IsNotNull(
            request,
            "Parameterless constructor should produce an instance.");
        Assert.IsNotNull(
            request.Identifier,
            "Parameterless constructor should still generate a ULID identifier.");
        Assert.IsNotNull(
            request.CreatedOn,
            "Parameterless constructor should set CreatedOn.");
    }

    #endregion Public Methods
}
