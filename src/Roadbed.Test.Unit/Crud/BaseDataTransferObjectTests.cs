namespace Roadbed.Test.Unit.Crud;

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Roadbed.Crud;

/// <summary>
/// Unit tests for the BaseDataTransferObject class.
/// </summary>
[TestClass]
public class BaseDataTransferObjectTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the default constructor creates a non-null instance.
    /// </summary>
    [TestMethod]
    public void Constructor_Default_CreatesNonNullInstance()
    {
        // Act
        var dto = new TestDto();

        // Assert
        Assert.IsNotNull(dto, "Constructor should create a non-null instance.");
    }

    /// <summary>
    /// Verifies that the Id property can be updated multiple times.
    /// </summary>
    [TestMethod]
    public void IdProperty_UpdatedMultipleTimes_RetainsLatestValue()
    {
        // Arrange
        var dto = new TestDto();

        // Act
        dto.Id = 1;
        dto.Id = 2;
        dto.Id = 3;

        // Assert
        Assert.AreEqual(3, dto.Id, "Id should retain the latest value.");
    }

    /// <summary>
    /// Verifies that the Id property works with Guid type.
    /// </summary>
    [TestMethod]
    public void IdProperty_WithGuidType_CanSetAndGet()
    {
        // Arrange
        var dto = new TestDtoWithGuidId();
        Guid expectedId = Guid.NewGuid();

        // Act
        dto.Id = expectedId;

        // Assert
        Assert.AreEqual(expectedId, dto.Id, "Id should return the Guid value that was set.");
    }

    /// <summary>
    /// Verifies that the Id property can be set and retrieved with integer type.
    /// </summary>
    [TestMethod]
    public void IdProperty_WithIntegerType_CanSetAndGet()
    {
        // Arrange
        var dto = new TestDto();
        int expectedId = 42;

        // Act
        dto.Id = expectedId;

        // Assert
        Assert.AreEqual(expectedId, dto.Id, "Id should return the value that was set.");
    }

    /// <summary>
    /// Verifies that the Id property works with string type.
    /// </summary>
    [TestMethod]
    public void IdProperty_WithStringType_CanSetAndGet()
    {
        // Arrange
        var dto = new TestDtoWithStringId();
        string expectedId = "test-id-123";

        // Act
        dto.Id = expectedId;

        // Assert
        Assert.AreEqual(expectedId, dto.Id, "Id should return the string value that was set.");
    }

    /// <summary>
    /// Verifies complete workflow with different Id types.
    /// </summary>
    [TestMethod]
    public void Integration_WithDifferentIdTypes_WorksCorrectly()
    {
        // Arrange & Act
        var intDto = new TestDto { Id = 42 };
        var stringDto = new TestDtoWithStringId { Id = "test-123" };
        var guidDto = new TestDtoWithGuidId { Id = Guid.NewGuid() };

        // Assert
        Assert.AreEqual(42, intDto.Id, "Integer Id should work.");
        Assert.AreEqual("test-123", stringDto.Id, "String Id should work.");
        Assert.IsTrue(guidDto.Id != Guid.Empty, "Guid Id should work.");
    }

    /// <summary>
    /// Verifies that the class implements IDataTransferObject interface.
    /// </summary>
    [TestMethod]
    public void Interface_ImplementsIDataTransferObject_Correctly()
    {
        // Arrange
        var dto = new TestDto();

        // Act
        bool implementsInterface = dto is IEntity<int>;

        // Assert
        Assert.IsTrue(implementsInterface, "Should implement IDataTransferObject<TId>.");
    }

    /// <summary>
    /// Verifies that the DTO can be deserialized from JSON.
    /// </summary>
    [TestMethod]
    public void JsonDeserialization_DeserializesDto_Successfully()
    {
        // Arrange
        string json = "{\"id\":42}";

        // Act
        var dto = JsonConvert.DeserializeObject<TestDto>(json);

        // Assert
        Assert.IsNotNull(dto, "Deserialized DTO should not be null.");
        Assert.AreEqual(42, dto.Id, "Deserialized Id should match the JSON value.");
    }

    /// <summary>
    /// Verifies that round-trip serialization preserves DTO data.
    /// </summary>
    [TestMethod]
    public void JsonRoundTrip_PreservesData_Successfully()
    {
        // Arrange
        var original = new TestDto { Id = 99 };

        // Act
        string json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<TestDto>(json);

        // Assert
        Assert.AreEqual(original.Id, deserialized!.Id, "Round-trip should preserve Id value.");
    }

    /// <summary>
    /// Verifies that the DTO can be serialized to JSON.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_SerializesDto_Successfully()
    {
        // Arrange
        var dto = new TestDto { Id = 42 };

        // Act
        string json = JsonConvert.SerializeObject(dto);

        // Assert
        Assert.Contains("\"id\":42", json, "JSON should contain the Id property.");
    }

    /// <summary>
    /// Verifies that the Id property uses the JsonProperty attribute correctly.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_UsesJsonPropertyAttribute_ForIdProperty()
    {
        // Arrange
        var dto = new TestDto { Id = 123 };

        // Act
        string json = JsonConvert.SerializeObject(dto);

        // Assert
        Assert.Contains("\"id\"", json, "JSON should use lowercase 'id' as specified in JsonProperty attribute.");
        Assert.DoesNotContain("\"Id\"", json, "JSON should not use Pascal case 'Id'.");
    }

    /// <summary>
    /// Verifies that two DTOs with different Ids are not equal.
    /// </summary>
    [TestMethod]
    public void RecordEquality_WithDifferentIds_AreNotEqual()
    {
        // Arrange
        var dto1 = new TestDto { Id = 42 };
        var dto2 = new TestDto { Id = 43 };

        // Act
        bool areEqual = dto1 == dto2;

        // Assert
        Assert.IsFalse(areEqual, "DTOs with different Ids should not be equal.");
    }

    /// <summary>
    /// Verifies that two DTOs with the same Id are equal (record equality).
    /// </summary>
    [TestMethod]
    public void RecordEquality_WithSameId_AreEqual()
    {
        // Arrange
        var dto1 = new TestDto { Id = 42 };
        var dto2 = new TestDto { Id = 42 };

        // Act
        bool areEqual = dto1 == dto2;

        // Assert
        Assert.IsTrue(areEqual, "DTOs with same Id should be equal (record equality).");
    }

    #endregion Public Methods

    /// <summary>
    /// Concrete implementation of BaseDataTransferObject for testing with integer Id.
    /// </summary>
    private record TestDto : BaseEntityRecord<int>
    {
    }

    /// <summary>
    /// Concrete implementation of BaseDataTransferObject for testing with string Id.
    /// </summary>
    private record TestDtoWithStringId : BaseEntityRecord<string>
    {
    }

    /// <summary>
    /// Concrete implementation of BaseDataTransferObject for testing with Guid Id.
    /// </summary>
    private record TestDtoWithGuidId : BaseEntityRecord<Guid>
    {
    }
}